using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class BrowserExtensionsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к сканированию расширений браузеров";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedBrowserFilter = "Все";

        [ObservableProperty]
        private string _selectedRiskFilter = "Все";

        [ObservableProperty]
        private string _totalExtensionsCount = "0";

        [ObservableProperty]
        private string _suspiciousCount = "0";

        [ObservableProperty]
        private string _safeCount = "0";

        public ObservableCollection<BrowserExtensionItem> Extensions { get; } = new();
        public ObservableCollection<BrowserExtensionItem> FilteredExtensions { get; } = new();

        public ICommand ScanExtensionsCommand { get; }
        public ICommand ToggleExtensionCommand { get; }
        public ICommand RemoveExtensionCommand { get; }
        public ICommand RemoveAllSuspiciousCommand { get; }
        public ICommand FilterBrowserCommand { get; }
        public ICommand FilterRiskCommand { get; }

        public BrowserExtensionsViewModel()
        {
            ScanExtensionsCommand = new RelayCommand(async () => await ScanExtensionsAsync());
            ToggleExtensionCommand = new RelayCommand<BrowserExtensionItem>(async item =>
            {
                if (item != null) await ToggleExtensionAsync(item);
            });
            RemoveExtensionCommand = new RelayCommand<BrowserExtensionItem>(async item =>
            {
                if (item != null) await RemoveExtensionAsync(item);
            });
            RemoveAllSuspiciousCommand = new RelayCommand(async () => await RemoveAllSuspiciousAsync());
            FilterBrowserCommand = new RelayCommand<string>(b =>
            {
                SelectedBrowserFilter = b ?? "Все";
                ApplyFilters();
            });
            FilterRiskCommand = new RelayCommand<string>(r =>
            {
                SelectedRiskFilter = r ?? "Все";
                ApplyFilters();
            });

            _ = ScanExtensionsAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        public async Task ScanExtensionsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Сканирование профилей Chrome, Edge, Яндекс Браузера, Firefox, Brave, Opera...";

            try
            {
                var list = await BrowserExtensionsService.Instance.ScanAllExtensionsAsync();

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Extensions.Clear();
                    foreach (var ext in list) Extensions.Add(ext);

                    TotalExtensionsCount = Extensions.Count.ToString();
                    SuspiciousCount = Extensions.Count(e => e.RiskLevel != ExtensionRiskLevel.Safe).ToString();
                    SafeCount = Extensions.Count(e => e.RiskLevel == ExtensionRiskLevel.Safe).ToString();

                    ApplyFilters();

                    StatusMessage = $"Сканирование завершено: найдено {Extensions.Count} расширений в системе.";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сканирования: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            var query = Extensions.AsEnumerable();

            if (!string.Equals(SelectedBrowserFilter, "Все", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.BrowserName.Contains(SelectedBrowserFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(SelectedRiskFilter, "Все", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedRiskFilter.Equals("Подозрительные", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(e => e.RiskLevel != ExtensionRiskLevel.Safe);
                else if (SelectedRiskFilter.Equals("Безопасные", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(e => e.RiskLevel == ExtensionRiskLevel.Safe);
                else if (SelectedRiskFilter.Equals("Отключенные", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(e => !e.IsEnabled);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(e =>
                    e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.BrowserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            FilteredExtensions.Clear();
            foreach (var item in query)
            {
                FilteredExtensions.Add(item);
            }
        }

        private async Task ToggleExtensionAsync(BrowserExtensionItem item)
        {
            bool ok = await BrowserExtensionsService.Instance.ToggleExtensionStateAsync(item);
            if (ok)
            {
                StatusMessage = item.IsEnabled ? $"Расширение «{item.Name}» включено!" : $"Расширение «{item.Name}» отключено!";
                ApplyFilters();
            }
            else
            {
                MessageBox.Show($"Не удалось изменить состояние расширения «{item.Name}». Закройте браузер и повторите попытку.", "STORM Browser Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task RemoveExtensionAsync(BrowserExtensionItem item)
        {
            var res = MessageBox.Show($"Вы действительно хотите удалить расширение «{item.Name}» из {item.BrowserName}?\n\nФайлы расширения будут полностью удалены.", "Удаление расширения", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            bool ok = await BrowserExtensionsService.Instance.RemoveExtensionAsync(item);
            if (ok)
            {
                Extensions.Remove(item);
                FilteredExtensions.Remove(item);
                TotalExtensionsCount = Extensions.Count.ToString();
                SuspiciousCount = Extensions.Count(e => e.RiskLevel != ExtensionRiskLevel.Safe).ToString();
                SafeCount = Extensions.Count(e => e.RiskLevel == ExtensionRiskLevel.Safe).ToString();
                StatusMessage = $"Расширение «{item.Name}» успешно удалено!";
            }
            else
            {
                MessageBox.Show($"Не удалось удалить расширение «{item.Name}». Возможно, браузер запущен и блокирует файлы.", "STORM Browser Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task RemoveAllSuspiciousAsync()
        {
            var suspicious = Extensions.Where(e => e.RiskLevel != ExtensionRiskLevel.Safe).ToList();
            if (suspicious.Count == 0)
            {
                MessageBox.Show("Подозрительных или вредоносных расширений не обнаружено.", "STORM Browser Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Обнаружено {suspicious.Count} подозрительных расширений с правами перехвата трафика и чтения всех сайтов.\n\nУдалить их все в 1 клик?", "Удаление подозрительных расширений", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            int deleted = 0;
            foreach (var ext in suspicious)
            {
                if (await BrowserExtensionsService.Instance.RemoveExtensionAsync(ext))
                {
                    Extensions.Remove(ext);
                    deleted++;
                }
            }

            ApplyFilters();
            TotalExtensionsCount = Extensions.Count.ToString();
            SuspiciousCount = Extensions.Count(e => e.RiskLevel != ExtensionRiskLevel.Safe).ToString();
            SafeCount = Extensions.Count(e => e.RiskLevel == ExtensionRiskLevel.Safe).ToString();
            StatusMessage = $"Удалено {deleted} подозрительных расширений!";
        }
    }
}
