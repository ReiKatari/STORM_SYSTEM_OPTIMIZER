using System;
using System.IO;
using System.Text.Json;
using System.Windows;
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
            catch { }
        }

        public void ApplyTheme(ThemeType theme, Window? window = null)
        {
            CurrentTheme = theme;
            SaveCurrentTheme();

            try
            {
                string themePath = theme switch
                {
                    ThemeType.StormDark => "Themes/StormDarkTheme.xaml",
                    ThemeType.StormNight => "Themes/StormNightTheme.xaml",
                    ThemeType.StormDay => "Themes/StormDayTheme.xaml",
                    ThemeType.StormMidnight => "Themes/StormMidnightTheme.xaml",
                    _ => "Themes/StormDarkTheme.xaml"
                };

                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.RelativeOrAbsolute)
                };

                var iconsDict = new ResourceDictionary
                {
                    Source = new Uri("Themes/StormIcons.xaml", UriKind.RelativeOrAbsolute)
                };

                if (Application.Current != null)
                {
                    var merged = Application.Current.Resources.MergedDictionaries;
                    merged.Clear();
                    merged.Add(themeDict);
                    merged.Add(iconsDict);
                }

                if (window != null)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(window);
                    bool isDark = theme != ThemeType.StormDay;
                    NativeMethods.SetWindowImmersiveDarkMode(helper.Handle, isDark);
                }
            }
            catch { }

            ThemeChanged?.Invoke(this, theme);
        }
    }
}
