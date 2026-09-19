using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;
using VoiceDictation.Infrastructure.Audio;

namespace VoiceDictation.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _originalSettings;
    private readonly IAudioCaptureService _audioCaptureService;

    private string _selectedShortcutKey;
    private TriggerMode _selectedTriggerMode;
    private InjectionMode _selectedInjectionMode;
    private string _selectedLanguage;
    private string _selectedModel;
    private AudioDeviceInfo? _selectedAudioDevice;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<AppSettings>? SettingsSaved;
    public event EventHandler? CancelRequested;

    public ObservableCollection<string> AvailableShortcutKeys { get; } = new()
    {
        "F8",
        "CapsLock",
        "F7",
        "F9",
        "F10",
        "F11",
        "F12",
        "Insert",
        "ScrollLock",
        "Pause"
    };

    public ObservableCollection<TriggerMode> AvailableTriggerModes { get; } = new()
    {
        TriggerMode.PushToTalk,
        TriggerMode.Toggle
    };

    public ObservableCollection<InjectionMode> AvailableInjectionModes { get; } = new()
    {
        InjectionMode.ClipboardHybrid,
        InjectionMode.DirectSendInput
    };

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new()
    {
        new LanguageOption("tr", "Türkçe (Turkish)"),
        new LanguageOption("en", "İngilizce (English)"),
        new LanguageOption("auto", "Otomatik Algıla (Auto Detect)"),
        new LanguageOption("de", "Almanca (German)"),
        new LanguageOption("fr", "Fransızca (French)"),
        new LanguageOption("es", "İspanyolca (Spanish)")
    };

    public ObservableCollection<string> AvailableModels { get; } = new()
    {
        "ggml-base.bin",
        "ggml-small.bin",
        "ggml-tiny.bin"
    };

    public ObservableCollection<AudioDeviceInfo> AvailableAudioDevices { get; } = new();

    public string SelectedShortcutKey
    {
        get => _selectedShortcutKey;
        set => SetField(ref _selectedShortcutKey, value);
    }

    public TriggerMode SelectedTriggerMode
    {
        get => _selectedTriggerMode;
        set => SetField(ref _selectedTriggerMode, value);
    }

    public InjectionMode SelectedInjectionMode
    {
        get => _selectedInjectionMode;
        set => SetField(ref _selectedInjectionMode, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetField(ref _selectedLanguage, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => SetField(ref _selectedModel, value);
    }

    public AudioDeviceInfo? SelectedAudioDevice
    {
        get => _selectedAudioDevice;
        set => SetField(ref _selectedAudioDevice, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public SettingsViewModel(AppSettings currentSettings)
    {
        _originalSettings = currentSettings;
        _audioCaptureService = new WasapiAudioCaptureService();

        _selectedShortcutKey = currentSettings.ShortcutKey;
        _selectedTriggerMode = currentSettings.TriggerMode;
        _selectedInjectionMode = currentSettings.InjectionMode;
        _selectedLanguage = currentSettings.SelectedLanguage;
        _selectedModel = currentSettings.ModelName;

        LoadAudioDevices(currentSettings.SelectedAudioDeviceId);

        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(() => CancelRequested?.Invoke(this, EventArgs.Empty));
    }

    private void LoadAudioDevices(string selectedDeviceId)
    {
        AvailableAudioDevices.Clear();
        var devices = _audioCaptureService.GetInputDevices();
        AudioDeviceInfo? toSelect = null;

        foreach (var device in devices)
        {
            AvailableAudioDevices.Add(device);
            if (string.Equals(device.Id, selectedDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                toSelect = device;
            }
            else if (toSelect is null && device.IsDefault)
            {
                toSelect = device;
            }
        }

        SelectedAudioDevice = toSelect ?? AvailableAudioDevices.FirstOrDefault();
    }

    private void OnSave()
    {
        var newSettings = new AppSettings
        {
            ShortcutKey = SelectedShortcutKey,
            TriggerMode = SelectedTriggerMode,
            InjectionMode = SelectedInjectionMode,
            SelectedLanguage = SelectedLanguage,
            ModelName = SelectedModel,
            SelectedAudioDeviceId = SelectedAudioDevice?.Id ?? string.Empty
        };

        SettingsSaved?.Invoke(this, newSettings);
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
}

public sealed record LanguageOption(string Code, string DisplayName);
