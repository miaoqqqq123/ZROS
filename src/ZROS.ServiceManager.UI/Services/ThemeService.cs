namespace ZROS.ServiceManager.UI.Services
{
    /// <summary>Manages the application theme (light / dark).</summary>
    public class ThemeService
    {
        public enum Theme { Light, Dark }

        private Theme _currentTheme = Theme.Light;

        public Theme CurrentTheme => _currentTheme;

        public void SetTheme(Theme theme)
        {
            _currentTheme = theme;
            ApplyTheme(theme);
        }

        public void ToggleTheme() => SetTheme(_currentTheme == Theme.Light ? Theme.Dark : Theme.Light);

        private void ApplyTheme(Theme theme)
        {
            // Theme application is handled via resource dictionary swapping in WPF.
            // Extend here with DevExpress theme API: ThemedWindow.Theme = ...
        }
    }
}
