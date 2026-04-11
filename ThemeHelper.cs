using Microsoft.Win32;

namespace ProPresenterTimer;

internal static class ThemeHelper
{
    /// <summary>True when Windows is set to dark mode for apps (AppsUseLightTheme = 0).</summary>
    public static bool IsDarkMode()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = k?.GetValue("AppsUseLightTheme");
            return v is int i && i == 0;
        }
        catch
        {
            return true;
        }
    }
}
