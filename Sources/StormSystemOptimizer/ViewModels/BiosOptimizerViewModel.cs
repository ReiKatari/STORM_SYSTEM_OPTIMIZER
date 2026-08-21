using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class BiosOptimizerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _motherboardManufacturer = "ASUS / MSI / Gigabyte";

        [ObservableProperty]
        private string _motherboardModel = "Gaming Series Motherboard";

        [ObservableProperty]
        private string _biosVersion = "UEFI 2.0";

        [ObservableProperty]
        private string _biosDate = "2024-2026";

        [ObservableProperty]
        private string _uefiStatus = "Режим UEFI GOP активен";

        [ObservableProperty]
        private string _secureBootStatus = "Secure Boot включен";

        [ObservableProperty]
        private string _statusSummary = "Загрузка рекомендаций BIOS...";

        [ObservableProperty]
        private string _selectedFilter = "Все настройки";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        private ObservableCollection<BiosSettingItem> _allSettings = new();
        public ObservableCollection<BiosSettingItem> FilteredSettings { get; } = new();

        public BiosOptimizerViewModel()
        {
            LoadMotherboardAndSettings();
        }

        [RelayCommand]
        public async Task LoadMotherboardAndSettingsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusSummary = "Опрос оборудования материнской платы и SMBIOS...";

            var info = BiosOptimizerService.Instance.GetMotherboardBiosInfo();
            MotherboardManufacturer = info.Manufacturer;
            MotherboardModel = info.Model;
            BiosVersion = $"Версия: {info.BiosVersion}";
            BiosDate = $"Дата: {info.BiosReleaseDate}";
            UefiStatus = info.IsUefiBoot ? "Режим UEFI GOP: Активен" : "Режим Legacy: Не рекомендуется";
            SecureBootStatus = info.IsSecureBootEnabled ? "Secure Boot: Включен" : "Secure Boot: Отключен";

            var settings = await BiosOptimizerService.Instance.GetRecommendedSettingsAsync();
            _allSettings.Clear();
            foreach (var item in settings) _allSettings.Add(item);

            ApplyFilter(SelectedFilter);
            StatusSummary = $"Найдено безопасных рекомендаций для платы {MotherboardManufacturer}: {_allSettings.Count}";
            IsBusy = false;
        }

        public void LoadMotherboardAndSettings()
        {
            _ = LoadMotherboardAndSettingsAsync();
        }

        [RelayCommand]
        public void FilterCategory(string category)
        {
            SelectedFilter = category;
            ApplyFilter(category);
        }

        private void ApplyFilter(string category)
        {
            FilteredSettings.Clear();
            var list = string.IsNullOrEmpty(category) || category == "Все настройки"
                ? _allSettings
                : _allSettings.Where(s => s.Category.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var item in list) FilteredSettings.Add(item);
        }

        [RelayCommand]
        public void RebootToBios()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /fw /t 0",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch
            {
                TrayService.Instance.ShowNotification("Перезагрузка в BIOS", "Запустите перезагрузку вручную: shutdown /r /fw /t 0");
            }
        }

        [RelayCommand]
        public void ExportBiosGuide()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "STORM_BIOS_Recommendations.txt");
                var lines = _allSettings.Select(s => 
                    $"[{s.Category}] {s.Title}\r\n" +
                    $"  Рекомендуемое значение: {s.RecommendedValue}\r\n" +
                    $"  Прирост: {s.PerformanceImpact}\r\n" +
                    $"  Путь в меню {MotherboardManufacturer}: {s.ActiveBoardPath(MotherboardManufacturer)}\r\n" +
                    $"  Описание: {s.Explanation}\r\n" +
                    new string('-', 70));

                string content = $"STORM SYSTEM OPTIMIZER - РЕКОМЕНДАЦИИ НАСТРОЕК BIOS\r\n" +
                                 $"Материнская плата: {MotherboardManufacturer} {MotherboardModel}\r\n" +
                                 $"Версия BIOS: {BiosVersion} ({BiosDate})\r\n" +
                                 $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\r\n" +
                                 new string('=', 70) + "\r\n\r\n" +
                                 string.Join("\r\n\r\n", lines);

                File.WriteAllText(path, content, System.Text.Encoding.UTF8);
                TrayService.Instance.ShowNotification("Руководство по BIOS сохранено 📄", $"Файл сохранен на рабочий стол: STORM_BIOS_Recommendations.txt");
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
            }
            catch { }
        }

        [RelayCommand]
        public void PrintBiosGuide()
        {
            try
            {
                string htmlPath = Path.Combine(Path.GetTempPath(), "STORM_BIOS_PrintGuide.html");

                var rows = string.Join("\n", _allSettings.Select((s, idx) => $@"
                    <tr>
                        <td style='font-weight: bold; width: 40px; text-align: center;'>{idx + 1}</td>
                        <td style='width: 140px;'><span class='badge'>{s.Category}</span><br/><strong style='color:#0284c7;'>{s.Title}</strong></td>
                        <td style='width: 160px; font-family: Consolas, monospace; font-size: 11px; background: #f8fafc;'>{s.ActiveBoardPath(MotherboardManufacturer)}</td>
                        <td style='width: 130px; font-weight: bold; color: #15803d;'>{s.RecommendedValue}</td>
                        <td style='width: 120px; color: #b45309; font-weight: 600;'>{s.PerformanceImpact}</td>
                        <td style='font-size: 11.5px; color: #334155;'>{s.Explanation}</td>
                    </tr>"));

                string html = $@"<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='utf-8'>
    <title>STORM SYSTEM OPTIMIZER - Памятка настроек BIOS</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; color: #0f172a; background: #fff; font-size: 12px; }}
        .header {{ border-bottom: 2px solid #0284c7; padding-bottom: 10px; margin-bottom: 15px; }}
        h1 {{ margin: 0 0 6px 0; font-size: 18px; color: #0284c7; }}
        .meta {{ font-size: 11px; color: #64748b; }}
        .meta strong {{ color: #0f172a; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th, td {{ border: 1px solid #cbd5e1; padding: 6px 8px; text-align: left; vertical-align: top; }}
        th {{ background: #f1f5f9; font-weight: bold; font-size: 11px; color: #1e293b; }}
        .badge {{ display: inline-block; padding: 2px 5px; background: #e0f2fe; color: #0369a1; border-radius: 4px; font-size: 10px; font-weight: bold; }}
        @media print {{
            body {{ margin: 10px; }}
            .no-print {{ display: none; }}
            tr {{ page-break-inside: avoid; }}
        }}
    </style>
</head>
<body onload='window.print()'>
    <div class='header'>
        <h1>⚡ STORM SYSTEM OPTIMIZER — РЕКОМЕНДАЦИИ НАСТРОЕК BIOS</h1>
        <div class='meta'>
            Материнская плата: <strong>{MotherboardManufacturer} {MotherboardModel}</strong> &bull; 
            {BiosVersion} ({BiosDate}) &bull; 
            {UefiStatus} &bull; 
            Дата печати: <strong>{DateTime.Now:dd.MM.yyyy HH:mm}</strong>
        </div>
    </div>

    <table>
        <thead>
            <tr>
                <th>#</th>
                <th>Параметр / Раздел</th>
                <th>Путь в BIOS ({MotherboardManufacturer})</th>
                <th>Рекомендуемое значение</th>
                <th>Эффект</th>
                <th>Пояснение</th>
            </tr>
        </thead>
        <tbody>
            {rows}
        </tbody>
    </table>

    <p style='margin-top: 15px; font-size: 10px; color: #64748b; text-align: center;'>
        Все представленные настройки на 100% безопасны и не вызывают перегрева компонентов. Сохраните профиль в BIOS перед перезагрузкой.
    </p>
</body>
</html>";

                File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);
                TrayService.Instance.ShowNotification("Печать памятки BIOS 🖨️", "Открыт диалог печати памятки настроек BIOS.");
                Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                TrayService.Instance.ShowNotification("Ошибка печати", ex.Message);
            }
        }
    }
}
