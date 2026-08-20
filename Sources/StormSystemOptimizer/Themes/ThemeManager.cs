using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.Themes
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<ThemeType>? ThemeChanged;

        public ThemeType CurrentTheme { get; private set; } = ThemeType.StormDark;

        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StormSystemOptimizer",
            "settings.json"
        );

        private ThemeManager()
        {
            LoadSavedTheme();
        }

        public void LoadSavedTheme()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Theme", out var prop))
                    {
                        if (Enum.TryParse<ThemeType>(prop.GetString(), out var savedTheme))
                        {
                            CurrentTheme = savedTheme;
                        }
                    }
                }
            }
            catch
            {
                CurrentTheme = ThemeType.StormDark;
            }
        }

        public void SaveCurrentTheme()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var data = new { Theme = CurrentTheme.ToString() };
                File.WriteAllText(_configPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Ignore config write failures
            }
        }

        public void ApplyTheme(ThemeType theme, Window? window = null)
        {
            CurrentTheme = theme;
            SaveCurrentTheme();

            string themeUri = theme switch
            {
                ThemeType.StormDark => "ms-appx:///Themes/StormDarkTheme.xaml",
                ThemeType.StormNight => "ms-appx:///Themes/StormNightTheme.xaml",
                ThemeType.StormDay => "ms-appx:///Themes/StormDayTheme.xaml",
                ThemeType.StormMidnight => "ms-appx:///Themes/StormMidnightTheme.xaml",
                _ => "ms-appx:///Themes/StormDarkTheme.xaml"
            };

            var newDict = new ResourceDictionary { Source = new Uri(themeUri) };

            // Update merged dictionaries
            var merged = Application.Current.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var d = merged[i];
                if (d.Source != null && d.Source.ToString().Contains("/Themes/Storm"))
                {
                    merged.RemoveAt(i);
                }
            }
            merged.Add(newDict);

            // Set FrameworkElement RequestedTheme on root if window exists
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme == ThemeType.StormDay
                    ? ElementTheme.Light
                    : ElementTheme.Dark;
            }

            // Update TitleBar styling if window is active
            if (window != null)
            {
                UpdateWindowTitleBar(window, theme);
            }

            ThemeChanged?.Invoke(this, theme);
        }

        public void UpdateWindowTitleBar(Window window, ThemeType theme)
        {
            try
            {
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                bool isDark = theme != ThemeType.StormDay;
                NativeMethods.SetWindowImmersiveDarkMode(hwnd, isDark);

                if (appWindow?.TitleBar != null)
                {
                    var titleBar = appWindow.TitleBar;
                    titleBar.ExtendsContentIntoTitleBar = true;

                    Windows.UI.Color bgColor = theme switch
                    {
                        ThemeType.StormDark => Windows.UI.Color.FromArgb(255, 16, 20, 29),
                        ThemeType.StormNight => Windows.UI.Color.FromArgb(255, 0, 0, 0),
                        ThemeType.StormDay => Windows.UI.Color.FromArgb(255, 244, 247, 251),
                        ThemeType.StormMidnight => Windows.UI.Color.FromArgb(255, 12, 10, 29),
                        _ => Windows.UI.Color.FromArgb(255, 16, 20, 29)
                    };

                    Windows.UI.Color fgColor = isDark
                        ? Windows.UI.Color.FromArgb(255, 241, 245, 249)
                        : Windows.UI.Color.FromArgb(255, 15, 23, 42);

                    Windows.UI.Color hoverBg = theme switch
                    {
                        ThemeType.StormDark => Windows.UI.Color.FromArgb(255, 32, 41, 58),
                        ThemeType.StormNight => Windows.UI.Color.FromArgb(255, 24, 27, 39),
                        ThemeType.StormDay => Windows.UI.Color.FromArgb(255, 226, 232, 240),
                        ThemeType.StormMidnight => Windows.UI.Color.FromArgb(255, 29, 23, 61),
                        _ => Windows.UI.Color.FromArgb(255, 32, 41, 58)
                    };

                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonForegroundColor = fgColor;
                    titleBar.ButtonHoverBackgroundColor = hoverBg;
                    titleBar.ButtonHoverForegroundColor = fgColor;
                    titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                    titleBar.ButtonPressedForegroundColor = fgColor;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(120, fgColor.R, fgColor.G, fgColor.B);
                }
            }
            catch
            {
                // Fallback if AppWindow custom TitleBar is not supported
            }
        }
    }
}
