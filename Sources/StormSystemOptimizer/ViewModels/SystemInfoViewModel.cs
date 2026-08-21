using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class SystemInfoViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusText = "Спецификация комплектующих и оборудования ПК";

        public ObservableCollection<HardwareDetailCategory> SpecCategories { get; } = new();

        public SystemInfoViewModel()
        {
            _ = LoadSpecsAsync();
        }

        [RelayCommand]
        public async Task LoadSpecsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Опрос датчиков и системных интерфейсов WMI/ACPI/SMBIOS...";

            var specs = await SystemInfoService.Instance.GetCompleteSystemSpecsAsync();

            SpecCategories.Clear();
            foreach (var cat in specs)
            {
                SpecCategories.Add(cat);
            }

            StatusText = $"Диагностика завершена: {SpecCategories.Count} категорий комплектующих.";
            IsBusy = false;
        }

        [RelayCommand]
        public void CopySpecsToClipboard()
        {
            try
            {
                string text = SystemInfoService.Instance.ExportSpecsToPlainText(new System.Collections.Generic.List<HardwareDetailCategory>(SpecCategories));
                Clipboard.SetText(text);
                TrayService.Instance.ShowNotification("Спецификация скопирована 📋", "Полный отчет о конфигурации компьютера скопирован в буфер обмена.");
            }
            catch { }
        }

        [RelayCommand]
        public async Task ExportSpecsToFileAsync()
        {
            try
            {
                string text = SystemInfoService.Instance.ExportSpecsToPlainText(new System.Collections.Generic.List<HardwareDetailCategory>(SpecCategories));
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM_System_Specs.txt");
                await File.WriteAllTextAsync(path, text);
                TrayService.Instance.ShowNotification("Спецификация сохранена 📄", $"Отчет успешно сохранен на Рабочий стол: {path}");
            }
            catch { }
        }
    }
}
