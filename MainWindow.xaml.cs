using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace ProPresenterTimer;

public partial class MainWindow : Window
{
    private AppSettings _settings = SettingsStore.Load();
    private TimerPollService? _poller;
    private bool _dark = true;

    private TimerUrgency _urgencyCenter = (TimerUrgency)(-1);
    private TimerUrgency _urgencySide = (TimerUrgency)(-1);
    private TimerUrgency _urgencyAudio = (TimerUrgency)(-1);

    private bool _fullScreen;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Maximized)
                RootBorder.CornerRadius = new CornerRadius(0);
            else
                RootBorder.CornerRadius = new CornerRadius(8);
        };
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _poller?.Dispose();
        _poller = null;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        ApplyTheme();
        LblCenter.Text = _settings.CenterScreenText;
        LblSide.Text = _settings.SideScreenText;
        LblAudio.Text = _settings.SideAudioText;
        UpdateCardVisibility();
        _poller = new TimerPollService(() => _settings, RefreshFromPoller);
        _poller.Start();
        if (!_settings.IsConfigured)
            OpenSettings(force: true);
        KeyDown += MainWindow_OnKeyDown;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
            Dispatcher.Invoke(ApplyTheme);
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        if (_fullScreen)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResize;
            _fullScreen = false;
        }
        else
        {
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _fullScreen = true;
        }
    }

    private void ApplyTheme()
    {
        _dark = ThemeHelper.IsDarkMode();
        BackgroundHost.Children.Clear();
        var g = new Grid();
        var grad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        if (_dark)
        {
            grad.GradientStops.Add(new GradientStop(Color.FromRgb(10, 10, 20), 0));
            grad.GradientStops.Add(new GradientStop(Color.FromRgb(15, 20, 36), 1));
        }
        else
        {
            grad.GradientStops.Add(new GradientStop(Color.FromRgb(230, 235, 245), 0));
            grad.GradientStops.Add(new GradientStop(Color.FromRgb(209, 222, 242), 1));
        }

        var rect = new System.Windows.Shapes.Rectangle { Fill = grad };
        Grid.SetRowSpan(rect, 2);
        g.Children.Add(rect);
        BackgroundHost.Children.Add(g);

        var labelFg = new SolidColorBrush(_dark ? Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x99, 0x33, 0x33, 0x33));
        LblCenter.Foreground = labelFg;
        LblSide.Foreground = labelFg;
        LblAudio.Foreground = labelFg;

        var glass = _dark ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF);
        var b0 = _dark ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
        var b1 = _dark ? Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF);
        SetCardGlass(CardCenter, glass, b0, b1);
        SetCardGlass(CardSide, glass, b0, b1);
        SetCardGlass(CardAudio, glass, b0, b1);

        var sh = _dark ? 0.4 : 0.15;
        SetShadow(CardCenterShadow, sh);
        SetShadow(CardSideShadow, sh);
        SetShadow(CardAudioShadow, sh);

        _urgencyCenter = (TimerUrgency)(-1);
        _urgencySide = (TimerUrgency)(-1);
        _urgencyAudio = (TimerUrgency)(-1);
        if (_poller != null)
            RefreshFromPoller();
    }

    private static void SetCardGlass(Border card, Color fill, Color edge0, Color edge1)
    {
        card.Background = new SolidColorBrush(fill);
        if (card.BorderBrush is LinearGradientBrush lg)
        {
            lg.GradientStops[0].Color = edge0;
            lg.GradientStops[1].Color = edge1;
        }

        card.BorderThickness = new Thickness(1);
    }

    private static void SetShadow(DropShadowEffect effect, double opacity)
    {
        effect.Opacity = opacity;
    }

    private void UpdateCardVisibility()
    {
        CardCenter.Visibility = _settings.CenterScreenEnabled ? Visibility.Visible : Visibility.Collapsed;
        CardAudio.Visibility = _settings.SideAudioEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshFromPoller()
    {
        if (_poller == null) return;

        TxtCenter.Text = _poller.CenterTimer;
        TxtSide.Text = _poller.SideTimer;
        TxtAudio.Text = _poller.SideAudioTimer;

        ApplyUrgency(_poller.CenterTimer, TxtCenter, GlowCenter, ref _urgencyCenter,
            "PulseCriticalCenter", "PulseWarningCenter", "PulseNormalCenter");
        ApplyUrgency(_poller.SideTimer, TxtSide, GlowSide, ref _urgencySide,
            "PulseCritical", "PulseWarning", "PulseNormal");
        ApplyUrgency(_poller.SideAudioTimer, TxtAudio, GlowAudio, ref _urgencyAudio,
            "PulseCriticalAudio", "PulseWarningAudio", "PulseNormalAudio");
    }

    private void ApplyUrgency(string time, TextBlock txt, DropShadowEffect glow, ref TimerUrgency last,
        string critKey, string warnKey, string normKey)
    {
        var u = TimerUrgencyHelper.FromTimeString(time);
        var (fg, glowC) = ColorsForUrgency(u, _dark);
        txt.Foreground = fg;
        glow.Color = glowC;

        if (u == last) return;
        last = u;

        StopSb(critKey);
        StopSb(warnKey);
        StopSb(normKey);

        switch (u)
        {
            case TimerUrgency.Critical:
                BeginSb(critKey);
                break;
            case TimerUrgency.Warning:
                BeginSb(warnKey);
                break;
            default:
                BeginSb(normKey);
                break;
        }
    }

    private (Brush fg, Color glow) ColorsForUrgency(TimerUrgency u, bool dark)
    {
        return u switch
        {
            TimerUrgency.Critical => (
                new SolidColorBrush(Color.FromRgb(255, 59, 48)),
                Color.FromRgb(255, 59, 48)),
            TimerUrgency.Warning => (
                new SolidColorBrush(Color.FromRgb(255, 158, 10)),
                Color.FromRgb(255, 158, 10)),
            _ => dark
                ? (new SolidColorBrush(Colors.White), Colors.White)
                : (new SolidColorBrush(Color.FromRgb(26, 26, 38)), Color.FromRgb(26, 26, 38)),
        };
    }

    private void BeginSb(string key)
    {
        if (Resources[key] is Storyboard sb)
            sb.Begin(this, true);
    }

    private void StopSb(string key)
    {
        if (Resources[key] is Storyboard sb)
            sb.Stop(this);
    }

    private void BtnSettings_OnClick(object sender, RoutedEventArgs e) => OpenSettings(force: false);

    private void OpenSettings(bool force)
    {
        var dlg = new SettingsWindow(_settings) { Owner = this };
        var ok = dlg.ShowDialog() == true;
        if (!ok && force && !_settings.IsConfigured)
        {
            MessageBox.Show(this,
                "Side Screen IP is required. Open Settings (gear) when you are ready to connect.",
                "ProPresenter Timer",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (ok)
        {
            SettingsStore.Save(_settings);
            LblCenter.Text = _settings.CenterScreenText;
            LblSide.Text = _settings.SideScreenText;
            LblAudio.Text = _settings.SideAudioText;
            UpdateCardVisibility();
            _poller?.Stop();
            _poller?.Dispose();
            _poller = new TimerPollService(() => _settings, RefreshFromPoller);
            _poller.Start();
            _urgencyCenter = (TimerUrgency)(-1);
            _urgencySide = (TimerUrgency)(-1);
            _urgencyAudio = (TimerUrgency)(-1);
            RefreshFromPoller();
        }
    }
}
