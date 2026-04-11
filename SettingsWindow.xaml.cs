using System.Windows;

namespace ProPresenterTimer;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        ChkAudio.Checked += (_, _) => UpdateVisibility();
        ChkAudio.Unchecked += (_, _) => UpdateVisibility();
        ChkCenter.Checked += (_, _) => UpdateVisibility();
        ChkCenter.Unchecked += (_, _) => UpdateVisibility();
        TxtSideIp.TextChanged += (_, _) => UpdateSaveEnabled();
        Loaded += (_, _) => LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        ChkCenter.IsChecked = _settings.CenterScreenEnabled;
        TxtCenterLabel.Text = _settings.CenterScreenText;
        TxtCenterIp.Text = _settings.CenterScreenIP;
        CmbCenterMode.SelectedIndex = string.Equals(_settings.CenterScreenUses, "Resolume", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        TxtSideLabel.Text = _settings.SideScreenText;
        TxtSideIp.Text = _settings.SideScreenIP;

        ChkAudio.IsChecked = _settings.SideAudioEnabled;
        TxtAudioLabel.Text = _settings.SideAudioText;
        TxtAudioIp.Text = _settings.SideAudioIP;

        TxtTimeout.Text = _settings.TimeoutMs.ToString();
        UpdateVisibility();
        UpdateSaveEnabled();
    }

    private void UpdateVisibility()
    {
        CenterFields.Visibility = ChkCenter.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        AudioFields.Visibility = ChkAudio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSaveEnabled()
    {
        var ok = !string.IsNullOrWhiteSpace(TxtSideIp.Text);
        BtnSave.IsEnabled = ok;
        TxtValidation.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSideIp.Text))
            return;

        _settings.CenterScreenEnabled = ChkCenter.IsChecked == true;
        _settings.CenterScreenText = string.IsNullOrWhiteSpace(TxtCenterLabel.Text)
            ? "Center Screen Countdown"
            : TxtCenterLabel.Text.Trim();
        _settings.CenterScreenIP = TxtCenterIp.Text.Trim();
        _settings.CenterScreenUses = CmbCenterMode.SelectedIndex == 1 ? "Resolume" : "ProPresenter";

        _settings.SideScreenText = string.IsNullOrWhiteSpace(TxtSideLabel.Text)
            ? "Side Screen Countdown"
            : TxtSideLabel.Text.Trim();
        _settings.SideScreenIP = TxtSideIp.Text.Trim();

        _settings.SideAudioEnabled = ChkAudio.IsChecked == true;
        _settings.SideAudioText = string.IsNullOrWhiteSpace(TxtAudioLabel.Text)
            ? "Side Screen Audio"
            : TxtAudioLabel.Text.Trim();
        _settings.SideAudioIP = TxtAudioIp.Text.Trim();

        if (!int.TryParse(TxtTimeout.Text.Trim(), out var to) || to < 1)
            to = 1000;
        _settings.TimeoutMs = to;

        DialogResult = true;
        Close();
    }
}
