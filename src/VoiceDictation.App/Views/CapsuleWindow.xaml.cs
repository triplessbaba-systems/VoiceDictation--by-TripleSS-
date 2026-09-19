using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using VoiceDictation.App.ViewModels;
using VoiceDictation.Infrastructure.Native;

namespace VoiceDictation.App.Views;

public partial class CapsuleWindow : Window
{
    private readonly CapsuleViewModel _viewModel;
    private readonly CubicEase _cubicEase = new() { EasingMode = EasingMode.EaseInOut };

    public CapsuleWindow(CapsuleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += OnLoaded;
        MouseDown += OnMouseDown;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.RequestOpenSettings += OnRequestOpenSettings;
        _viewModel.RequestCloseApp += OnRequestCloseApp;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var helper = new WindowInteropHelper(this);
        var exStyle = Win32Imports.GetWindowLongPtr(helper.Handle, Win32Imports.GWL_EXSTYLE);
        var newExStyle = new IntPtr(exStyle.ToInt64() | Win32Imports.WS_EX_NOACTIVATE | Win32Imports.WS_EX_TOOLWINDOW);
        Win32Imports.SetWindowLongPtr(helper.Handle, Win32Imports.GWL_EXSTYLE, newExStyle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionAtBottomCenter();
    }

    private void PositionAtBottomCenter()
    {
        var workArea = SystemParameters.WorkArea;
        Left = (workArea.Width - ActualWidth) / 2 + workArea.Left;
        Top = workArea.Bottom - ActualHeight - 24;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CapsuleViewModel.IsVisible))
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel.IsVisible)
                {
                    AnimateFade(1.0, 200);
                }
                else
                {
                    AnimateFade(0.0, 300);
                }
            });
        }
    }

    private void AnimateFade(double targetOpacity, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = _cubicEase
        };
        BeginAnimation(OpacityProperty, animation);
    }

    private void OnRequestOpenSettings(object? sender, EventArgs e)
    {
        var settingsVm = new SettingsViewModel(_viewModel.GetSettings());
        var settingsWindow = new SettingsWindow(settingsVm)
        {
            Owner = this
        };

        settingsVm.SettingsSaved += (_, newSettings) =>
        {
            _viewModel.ApplyNewSettings(newSettings);
            settingsWindow.Close();
        };

        settingsVm.CancelRequested += (_, _) =>
        {
            settingsWindow.Close();
        };

        settingsWindow.ShowDialog();
    }

    private void OnRequestCloseApp(object? sender, EventArgs e)
    {
        Close();
        Application.Current?.Shutdown();
    }
}
