namespace ProPresenterTimer;

/// <summary>User settings; persisted as JSON under LocalApplicationData.</summary>
public sealed class AppSettings
{
    public bool CenterScreenEnabled { get; set; }
    public string CenterScreenUses { get; set; } = "ProPresenter";
    public string CenterScreenText { get; set; } = "Center Screen Countdown";
    public string CenterScreenIP { get; set; } = "";

    public string SideScreenText { get; set; } = "Side Screen Countdown";
    public string SideScreenIP { get; set; } = "";

    public bool SideAudioEnabled { get; set; }
    public string SideAudioText { get; set; } = "Side Screen Audio";
    /// <summary>When non-empty, audio transport APIs use this base URL instead of <see cref="SideScreenIP"/> (second ProPresenter instance).</summary>
    public string SideAudioIP { get; set; } = "";

    public int TimeoutMs { get; set; } = 1000;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SideScreenIP);

    public string EffectiveAudioBase =>
        SideAudioEnabled && !string.IsNullOrWhiteSpace(SideAudioIP)
            ? SideAudioIP.Trim()
            : SideScreenIP.Trim();
}
