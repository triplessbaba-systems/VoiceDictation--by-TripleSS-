using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;
using VoiceDictation.Core.State;
using VoiceDictation.Infrastructure.Audio;
using VoiceDictation.Infrastructure.Configuration;
using VoiceDictation.Infrastructure.Input;
using VoiceDictation.Infrastructure.Native;
using VoiceDictation.Infrastructure.Transcription;

namespace VoiceDictation.App.ViewModels;

public sealed class CapsuleViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly DictationStateMachine _stateMachine;
    private readonly IAudioCaptureService _audioCapture;
    private readonly ITranscriptionEngine _transcriptionEngine;
    private readonly IInputInjectionService _inputInjection;
    private readonly IGlobalHookService _globalHook;
    private readonly JsonSettingsService _settingsService;
    private readonly DispatcherTimer _idleTimer;

    private AppSettings _currentSettings;
    private IntPtr _targetWindowHandle = IntPtr.Zero;
    private string _statusText = "Hazır";
    private string _statusBrush = "#58A6FF";
    private float _audioLevel;
    private double _barHeight1 = 6;
    private double _barHeight2 = 10;
    private double _barHeight3 = 16;
    private double _barHeight4 = 10;
    private double _barHeight5 = 6;
    private bool _isVisible = true;
    private string _lastTranscription = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RequestOpenSettings;
    public event EventHandler? RequestCloseApp;

    public DictationState CurrentState => _stateMachine.CurrentState;

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string StatusBrush
    {
        get => _statusBrush;
        private set => SetField(ref _statusBrush, value);
    }

    public float AudioLevel
    {
        get => _audioLevel;
        private set => SetField(ref _audioLevel, value);
    }

    public double BarHeight1
    {
        get => _barHeight1;
        private set => SetField(ref _barHeight1, value);
    }

    public double BarHeight2
    {
        get => _barHeight2;
        private set => SetField(ref _barHeight2, value);
    }

    public double BarHeight3
    {
        get => _barHeight3;
        private set => SetField(ref _barHeight3, value);
    }

    public double BarHeight4
    {
        get => _barHeight4;
        private set => SetField(ref _barHeight4, value);
    }

    public double BarHeight5
    {
        get => _barHeight5;
        private set => SetField(ref _barHeight5, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public string LastTranscription
    {
        get => _lastTranscription;
        private set => SetField(ref _lastTranscription, value);
    }

    public ICommand OpenSettingsCommand { get; }
    public ICommand CloseAppCommand { get; }

    public CapsuleViewModel()
    {
        _stateMachine = new DictationStateMachine();
        _audioCapture = new WasapiAudioCaptureService();
        _transcriptionEngine = new WhisperTranscriptionEngine();
        _inputInjection = new WindowsInputInjectionService();
        _globalHook = new LowLevelKeyboardHookService();
        _settingsService = new JsonSettingsService();

        _currentSettings = _settingsService.LoadSettings();

        _idleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _idleTimer.Tick += (_, _) =>
        {
            if (_stateMachine.CurrentState == DictationState.Idle)
            {
                IsVisible = false;
            }
            _idleTimer.Stop();
        };

        OpenSettingsCommand = new RelayCommand(() => RequestOpenSettings?.Invoke(this, EventArgs.Empty));
        CloseAppCommand = new RelayCommand(() => RequestCloseApp?.Invoke(this, EventArgs.Empty));

        _stateMachine.StateChanged += OnStateChanged;
        _audioCapture.AudioChunkAvailable += OnAudioChunkAvailable;
        _globalHook.TriggerPressed += OnTriggerPressed;
        _globalHook.TriggerReleased += OnTriggerReleased;

        UpdateStatusPresentation(DictationState.Idle);
    }

    public async Task InitializeAsync()
    {
        var modelsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoiceDictation", "models");
        if (!Directory.Exists(modelsFolder))
        {
            Directory.CreateDirectory(modelsFolder);
        }

        var modelPath = Path.Combine(modelsFolder, _currentSettings.ModelName);
        await _transcriptionEngine.InitializeAsync(modelPath).ConfigureAwait(true);

        _globalHook.StartHook(_currentSettings.ShortcutKey, _currentSettings.TriggerMode);
    }

    public void ApplyNewSettings(AppSettings newSettings)
    {
        _currentSettings = newSettings;
        _settingsService.SaveSettings(newSettings);
        _globalHook.UpdateTriggerKey(newSettings.ShortcutKey, newSettings.TriggerMode);
        UpdateStatusPresentation(_stateMachine.CurrentState);
    }

    public AppSettings GetSettings() => _currentSettings;

    private void OnTriggerPressed(object? sender, EventArgs e)
    {
        _targetWindowHandle = Win32Imports.GetForegroundWindow();

        Application.Current?.Dispatcher.InvokeAsync(async () =>
        {
            if (_stateMachine.CurrentState == DictationState.Idle)
            {
                _idleTimer.Stop();
                IsVisible = true;

                if (_stateMachine.TryTransition(DictationState.Recording))
                {
                    _audioCapture.StartCapture(_currentSettings.SelectedAudioDeviceId);
                }
            }
            else if (_stateMachine.CurrentState == DictationState.Recording && _currentSettings.TriggerMode == TriggerMode.Toggle)
            {
                await StopRecordingAndProcessAsync();
            }
        });
    }

    private void OnTriggerReleased(object? sender, EventArgs e)
    {
        if (_currentSettings.TriggerMode == TriggerMode.PushToTalk)
        {
            Application.Current?.Dispatcher.InvokeAsync(async () =>
            {
                if (_stateMachine.CurrentState == DictationState.Recording)
                {
                    await StopRecordingAndProcessAsync();
                }
            });
        }
    }

    private async Task StopRecordingAndProcessAsync()
    {
        if (!_stateMachine.TryTransition(DictationState.Transcribing))
        {
            return;
        }

        var wavData = await _audioCapture.StopCaptureAndGetWavAsync().ConfigureAwait(true);

        if (wavData.Length == 0)
        {
            _stateMachine.TryTransition(DictationState.Idle);
            return;
        }

        var result = await _transcriptionEngine.TranscribeAsync(wavData, _currentSettings.SelectedLanguage).ConfigureAwait(true);

        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Text))
        {
            LastTranscription = result.Text;

            if (_stateMachine.TryTransition(DictationState.Injecting))
            {
                await _inputInjection.InjectTextAsync(result.Text, _currentSettings.InjectionMode, _targetWindowHandle).ConfigureAwait(true);
            }
        }

        _stateMachine.TryTransition(DictationState.Idle);
        _idleTimer.Start();
    }

    private void OnAudioChunkAvailable(object? sender, AudioChunk chunk)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            AudioLevel = chunk.PeakAmplitude;

            var amp = Math.Clamp(chunk.PeakAmplitude, 0.05f, 1.0f);
            BarHeight1 = 6 + (amp * 16);
            BarHeight2 = 10 + (amp * 26);
            BarHeight3 = 14 + (amp * 36);
            BarHeight4 = 10 + (amp * 26);
            BarHeight5 = 6 + (amp * 16);
        });
    }

    private void OnStateChanged(object? sender, DictationStateChangedEventArgs e)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            UpdateStatusPresentation(e.NewState, e.ErrorMessage);
        });
    }

    private void UpdateStatusPresentation(DictationState state, string? error = null)
    {
        switch (state)
        {
            case DictationState.Idle:
                StatusText = $"Hazır ({_currentSettings.ShortcutKey})";
                StatusBrush = "#58A6FF";
                BarHeight1 = 6;
                BarHeight2 = 8;
                BarHeight3 = 10;
                BarHeight4 = 8;
                BarHeight5 = 6;
                break;
            case DictationState.Recording:
                StatusText = "Dinleniyor...";
                StatusBrush = "#F85149";
                break;
            case DictationState.Transcribing:
                StatusText = "Dönüştürülüyor...";
                StatusBrush = "#E3B341";
                break;
            case DictationState.Injecting:
                StatusText = "Yazılıyor...";
                StatusBrush = "#3FB950";
                break;
            case DictationState.Faulted:
                StatusText = error ?? "Hata!";
                StatusBrush = "#F85149";
                break;
        }
        OnPropertyChanged(nameof(CurrentState));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async ValueTask DisposeAsync()
    {
        _globalHook.Dispose();
        _audioCapture.Dispose();
        await _transcriptionEngine.DisposeAsync().ConfigureAwait(false);
    }
}
