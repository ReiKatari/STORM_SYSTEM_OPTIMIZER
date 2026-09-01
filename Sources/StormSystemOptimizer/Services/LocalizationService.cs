using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace StormSystemOptimizer.Services
{
    public class LanguageInfo
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string Flag { get; set; } = "";
    }

    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? LanguageChanged;

        private string _currentLanguage = "ru";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveSettings();
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnPropertyChanged("Item[]");
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string this[string key] => GetString(key);

        public static readonly LanguageInfo[] SupportedLanguages =
        [
            new() { Code = "ru", Name = "Русский", NativeName = "Русский", Flag = "🇷🇺" },
            new() { Code = "en", Name = "English", NativeName = "English", Flag = "🇬🇧" },
            new() { Code = "de", Name = "Deutsch", NativeName = "Deutsch", Flag = "🇩🇪" },
            new() { Code = "fr", Name = "Français", NativeName = "Français", Flag = "🇫🇷" },
            new() { Code = "zh", Name = "Chinese", NativeName = "简体中文", Flag = "🇨🇳" },
            new() { Code = "ja", Name = "Japanese", NativeName = "日本語", Flag = "🇯🇵" }
        ];

        private readonly Dictionary<string, Dictionary<string, string>> _dict = new();

        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StormSystemOptimizer",
            "settings.json"
        );

        private LocalizationService()
        {
            InitTranslations();
            LoadSavedLanguage();
        }

        public void LoadSavedLanguage()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var prop))
                    {
                        string? lang = prop.GetString();
                        if (!string.IsNullOrEmpty(lang) && _dict.ContainsKey(lang))
                        {
                            _currentLanguage = lang;
                        }
                    }
                }
            }
            catch
            {
                _currentLanguage = "ru";
            }
        }

        public void SaveSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string currentTheme = "StormDark";
                if (File.Exists(_configPath))
                {
                    try
                    {
                        string existingJson = File.ReadAllText(_configPath);
                        using var doc = JsonDocument.Parse(existingJson);
                        if (doc.RootElement.TryGetProperty("Theme", out var tProp))
                        {
                            currentTheme = tProp.GetString() ?? "StormDark";
                        }
                    }
                    catch { }
                }

                var data = new
                {
                    Theme = currentTheme,
                    Language = _currentLanguage
                };

                File.WriteAllText(_configPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public string GetString(string key)
        {
            if (_dict.TryGetValue(_currentLanguage, out var d) && d.TryGetValue(key, out var v))
                return v;
            if (_dict.TryGetValue("ru", out var ru) && ru.TryGetValue(key, out var rv))
                return rv;
            if (_dict.TryGetValue("en", out var en) && en.TryGetValue(key, out var ev))
                return ev;
            return key;
        }

        public string GetString(string key, params object[] args)
        {
            string format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void InitTranslations()
        {
            var ru = new Dictionary<string, string>(); _dict["ru"] = ru;
            var en = new Dictionary<string, string>(); _dict["en"] = en;
            var de = new Dictionary<string, string>(); _dict["de"] = de;
            var fr = new Dictionary<string, string>(); _dict["fr"] = fr;
            var zh = new Dictionary<string, string>(); _dict["zh"] = zh;
            var ja = new Dictionary<string, string>(); _dict["ja"] = ja;

            Action<string, string, string, string, string, string, string> a = (k, r, e, d_val, f, z, j) =>
            {
                ru[k] = r; en[k] = e; de[k] = d_val; fr[k] = f; zh[k] = z; ja[k] = j;
            };

            // Global App & Header
            a("AppTitle", "STORM SYSTEM OPTIMIZER", "STORM SYSTEM OPTIMIZER", "STORM SYSTEM OPTIMIZER", "STORM SYSTEM OPTIMIZER", "STORM SYSTEM OPTIMIZER", "STORM SYSTEM OPTIMIZER");
            a("AppSubtitle", "Флагманский комплекс оптимизации Windows", "Flagship Windows optimization suite", "Flaggschiff-Windows-Optimierungssuite", "Suite phare d'optimisation de Windows", "旗舰级 Windows 系统深度优化工具箱", "Windows 最適化フラッグシップスイート");
            a("EngineReady", "STORM Engine {0} готов • Система оптимизирована и стабильна", "STORM Engine {0} ready • System optimized and stable", "STORM Engine {0} bereit • System optimiert und stabil", "STORM Engine {0} prêt • Système optimisé et stable", "STORM 引擎 {0} 就绪 • 系统优化且运行稳定", "STORM Engine {0} 準備完了 • システムは最適化され安定");
            a("SafeOptimizationBadge", "100% Безопасная Оптимизация • WPF .NET 8", "100% Safe Optimization • WPF .NET 8", "100% Sichere Optimierung • WPF .NET 8", "Optimisation 100% sûre • WPF .NET 8", "100% 安全深度优化 • WPF .NET 8", "100% 安全最適化 • WPF .NET 8");

            // Navigation
            a("NavDashboard", "Панель управления", "Dashboard", "Dashboard", "Tableau de bord", "控制仪表盘", "ダッシュボード");
            a("NavOneClick", "Оптимизация в 1 клик", "1-Click optimization", "1-Klick-Optimierung", "Optimisation en 1 clic", "一键极速优化", "ワンクリック最適化");
            a("NavScanner", "Глубокая очистка", "Deep cleaner", "Tiefenreinigung", "Nettoyage approfondi", "深度垃圾清理", "ディープクリーン");
            a("NavDisks", "Диски и SMART накопители", "Disks and SMART drives", "Datenträger und SMART", "Disques et SMART", "磁盘与 SMART 驱动器", "ディスクとSMART");
            a("NavServices", "Службы и процессы", "Services and processes", "Dienste und Prozesse", "Services et processus", "系统服务与进程", "サービスとプロセス");
            a("NavNetwork", "Сеть и пинг", "Network and ping", "Netzwerk und Ping", "Réseau et ping", "网络加速与 Ping", "ネットワークとPing");
            a("NavPrivacy", "Конфиденциальность", "Privacy and telemetry", "Datenschutz", "Confidentialité", "隐私保护与遥测", "プライバシーと保護");
            a("NavPower", "Электропитание и GPU", "Power and GPU", "Energie und GPU", "Alimentation et GPU", "电源与 GPU 调优", "電源とGPU");
            a("NavMemory", "Оперативная память", "RAM management", "Arbeitsspeicher", "Mémoire vive", "内存智能清理", "メモリ管理");
            a("NavStartup", "Автозагрузка", "Startup manager", "Autostart", "Démarrage", "开机自启管理", "スタートアップ");
            a("NavSecurity", "Безопасность и аудит", "Security and audit", "Sicherheit und Audit", "Sécurité et audit", "安全审计与防御", "セキュリティと監査");
            a("NavOffice", "Центр Microsoft Office", "Microsoft Office center", "Microsoft Office Center", "Centre Microsoft Office", "Microsoft Office 部署中心", "Microsoft Office センター");
            a("NavSoftwareUpdater", "Обновление ПО", "Software updater", "Software-Updater", "Mise à jour logiciels", "软件更新中心", "ソフトウェア更新");
            a("NavDpcLatency", "Прерывания DPC и ISR", "DPC and ISR latency", "DPC- und ISR-Latenz", "Latence DPC et ISR", "DPC/ISR 中断延迟调优", "DPC/ISR 割り込み遅延");
            a("NavUsbImod", "Модерация USB (IMOD)", "USB moderation (IMOD)", "USB-Moderation (IMOD)", "Modération USB (IMOD)", "USB 中断节流 (IMOD)", "USB 割り込み制御 (IMOD)");
            a("NavSettings", "Настройки и обновления", "Settings and updates", "Einstellungen und Updates", "Paramètres et mises à jour", "系统设置与更新", "設定とアップデート");

            // Settings & Language
            a("SettingsHeader", "Настройки и обновления", "Settings and updates", "Einstellungen und Updates", "Paramètres et mises à jour", "系统设置与更新", "設定とアップデート");
            a("SettingsSubtitle", "Выбор языка, темы оформления, параметры интеграции и проверка обновлений.", "Language selection, theme styles, integration options and update checks.", "Sprachauswahl, Designthemen, Integrationsoptionen und Update-Prüfung.", "Sélection de la langue, thèmes visuels, intégration et recherche de mises à jour.", "选择语言、主题外观、系统集成选项与检查软件更新。", "言語設定、テーマスタイル、統合オプション、アップデート確認。");
            a("LanguageHeader", "Язык интерфейса (6 основных языков)", "Interface language (6 major languages)", "Oberflächensprache (6 Hauptsprachen)", "Langue de l'interface (6 langues principales)", "界面语言 (支持 6 种主流语言)", "表示言語 (主要6言語対応)");
            a("ThemesHeader", "Визуальные темы оформления (8 стилей)", "Visual themes (8 styles)", "Visuelle Designthemen (8 Stile)", "Thèmes visuels (8 styles)", "视觉主题风格 (8 种精品主题)", "ビジュアルテーマ (8つのスタイル)");
            a("CheckUpdates", "Проверить обновления", "Check for updates", "Nach Updates suchen", "Vérifier les mises à jour", "检查更新", "更新を確認");
            a("UpdateNow", "Обновить сейчас", "Update now", "Jetzt aktualisieren", "Mettre à jour", "立即更新", "今すぐ更新");

            // Common Actions
            a("Apply", "Применить", "Apply", "Anwenden", "Appliquer", "应用", "適用");
            a("Save", "Сохранить", "Save", "Speichern", "Enregistrer", "保存", "保存");
            a("Cancel", "Отмена", "Cancel", "Abbrechen", "Annuler", "取消", "キャンセル");
            a("Optimize", "Оптимизировать", "Optimize", "Optimieren", "Optimiser", "优化", "最適化");
            a("Clean", "Очистить", "Clean", "Bereinigen", "Nettoyer", "清理", "クリーンアップ");
            a("Scan", "Сканировать", "Scan", "Scannen", "Analyser", "扫描", "スキャン");
            a("Restore", "Восстановить", "Restore", "Wiederherstellen", "Restaurer", "还原", "復元");
            a("Refresh", "Обновить", "Refresh", "Aktualisieren", "Actualiser", "刷新", "更新");
            a("Success", "Успешно", "Success", "Erfolgreich", "Succès", "成功", "成功");
            a("Ready", "Готов", "Ready", "Bereit", "Prêt", "就绪", "準備完了");
        }
    }
}
