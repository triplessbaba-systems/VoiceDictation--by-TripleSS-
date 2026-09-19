using System.IO;
using System.Windows;
using VoiceDictation.App.Services;
using VoiceDictation.App.ViewModels;
using VoiceDictation.App.Views;
using VoiceDictation.Infrastructure.Native;

namespace VoiceDictation.App;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;
    private CapsuleViewModel? _capsuleViewModel;
    private CapsuleWindow? _capsuleWindow;
    private TrayIconManager? _trayIconManager;

    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoiceDictation");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var logPath = Path.Combine(dir, "startup.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch
        {
        }
    }

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Log($"[FATAL] AppDomain: {ex}");
            MessageBox.Show(ex?.ToString() ?? "Bilinmeyen hata", "VoiceDictation Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, e) =>
        {
            Log($"[FATAL] Dispatcher: {e.Exception}");
            MessageBox.Show(e.Exception.ToString(), "VoiceDictation Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log("OnStartup started");

        try
        {
            _singleInstanceMutex = new Mutex(true, "VoiceDictationSingleInstanceMutex", out var isNewInstance);
            Log($"Mutex checked. isNewInstance={isNewInstance}");

            if (!isNewInstance)
            {
                Log("Another instance already running. Exiting.");
                MessageBox.Show("VoiceDictation zaten arka planda çalışıyor. Lütfen sağ alttaki gizli simgeleri (ok işareti) kontrol edin.", "VoiceDictation", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Log("Ensuring native Whisper libraries...");
            NativeLibraryBootstrapper.EnsureNativeLibrariesInstalled();
            Log("Native Whisper libraries initialized.");

            Log("Creating ViewModel...");
            _capsuleViewModel = new CapsuleViewModel();

            Log("Creating CapsuleWindow...");
            _capsuleWindow = new CapsuleWindow(_capsuleViewModel);
            _capsuleWindow.Show();
            Log("CapsuleWindow shown.");

            Log("Creating TrayIconManager...");
            _trayIconManager = new TrayIconManager(
                onShowCapsule: () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_capsuleViewModel is not null)
                        {
                            _capsuleViewModel.IsVisible = true;
                        }
                        _capsuleWindow?.Show();
                    });
                },
                onOpenSettings: () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _capsuleViewModel?.OpenSettingsCommand.Execute(null);
                    });
                },
                onExit: () =>
                {
                    Dispatcher.Invoke(Shutdown);
                }
            );
            Log("TrayIconManager created.");

            Log("Calling InitializeAsync...");
            await _capsuleViewModel.InitializeAsync();
            Log("InitializeAsync completed successfully.");
        }
        catch (Exception ex)
        {
            Log($"Exception in OnStartup: {ex}");
            MessageBox.Show(ex.ToString(), "VoiceDictation Baslatma Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log("OnExit started");
        _trayIconManager?.Dispose();
        _trayIconManager = null;

        if (_capsuleViewModel is not null)
        {
            await _capsuleViewModel.DisposeAsync();
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
        Log("OnExit finished");
    }
}
