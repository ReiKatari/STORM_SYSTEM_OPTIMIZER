using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class SecurityAuditViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к проверке безопасности и аудиту системы";

        [ObservableProperty]
        private string _shredFilePath = string.Empty;

        [ObservableProperty]
        private double _shredProgress = 0;

        [ObservableProperty]
        private ShredAlgorithm _selectedAlgorithm = ShredAlgorithm.DoD5220;

        [ObservableProperty]
        private string _totalThreatsCount = "0";

        [ObservableProperty]
        private string _firewallRulesCount = "0";

        [ObservableProperty]
        private string _orphanedRulesCount = "0";

        public ObservableCollection<SecurityThreatItem> Threats { get; } = new();
        public ObservableCollection<FirewallRuleItem> FirewallRules { get; } = new();

        public ICommand ScanThreatsCommand { get; }
        public ICommand ResolveThreatCommand { get; }
        public ICommand ScanFirewallCommand { get; }
        public ICommand PurgeOrphanedFirewallCommand { get; }
        public ICommand ShredFileCommand { get; }
        public ICommand BrowseShredFileCommand { get; }

        public SecurityAuditViewModel()
        {
            ScanThreatsCommand = new RelayCommand(async () => await ExecuteScanThreatsAsync());
            ResolveThreatCommand = new RelayCommand<SecurityThreatItem>(async threat =>
            {
                if (threat != null) await ExecuteResolveThreatAsync(threat);
            });
            ScanFirewallCommand = new RelayCommand(async () => await ExecuteScanFirewallAsync());
            PurgeOrphanedFirewallCommand = new RelayCommand(async () => await ExecutePurgeFirewallAsync());
            ShredFileCommand = new RelayCommand(async () => await ExecuteShredFileAsync());
            BrowseShredFileCommand = new RelayCommand(() => ExecuteBrowseFile());

            _ = ExecuteScanThreatsAsync();
            _ = ExecuteScanFirewallAsync();
        }

        public async Task ExecuteScanThreatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var progress = new Progress<string>(msg =>
            {
                Application.Current?.Dispatcher?.Invoke(() => StatusMessage = msg);
            });

            var list = await MalwareHeuristicsService.Instance.ScanSystemThreatsAsync(progress);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Threats.Clear();
                foreach (var t in list) Threats.Add(t);
                TotalThreatsCount = Threats.Count.ToString();
                StatusMessage = $"Сканирование угроз завершено. Обнаружено угроз: {Threats.Count}";
                IsBusy = false;
            });
        }

        private async Task ExecuteResolveThreatAsync(SecurityThreatItem threat)
        {
            IsBusy = true;
            StatusMessage = $"Устранение угрозы: {threat.Title}...";
            bool ok = await MalwareHeuristicsService.Instance.ResolveThreatAsync(threat);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (ok)
                {
                    Threats.Remove(threat);
                    TotalThreatsCount = Threats.Count.ToString();
                    StatusMessage = "Угроза успешно нейтрализована!";
                }
                else
                {
                    StatusMessage = "Не удалось устранить угрозу (возможно, объект защищен системой).";
                }
                IsBusy = false;
            });
        }

        public async Task ExecuteScanFirewallAsync()
        {
            var progress = new Progress<string>(msg =>
            {
                Application.Current?.Dispatcher?.Invoke(() => StatusMessage = msg);
            });

            var list = await FirewallAuditService.Instance.ScanFirewallRulesAsync(progress);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                FirewallRules.Clear();
                int orphaned = 0;
                foreach (var r in list)
                {
                    FirewallRules.Add(r);
                    if (r.IsOrphaned) orphaned++;
                }

                FirewallRulesCount = FirewallRules.Count.ToString();
                OrphanedRulesCount = orphaned.ToString();
                StatusMessage = $"Брандмауэр: загружено {FirewallRules.Count} правил (сиротских: {orphaned})";
            });
        }

        private async Task ExecutePurgeFirewallAsync()
        {
            IsBusy = true;
            StatusMessage = "Удаление устаревших правил Брандмауэра...";
            int purged = await FirewallAuditService.Instance.PurgeOrphanedRulesAsync();
            await ExecuteScanFirewallAsync();

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusMessage = $"Очистка завершена! Удалено сиротских правил: {purged}";
                IsBusy = false;
            });
        }

        private void ExecuteBrowseFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл для гарантированного уничтожения (Шредер)"
            };
            if (dlg.ShowDialog() == true)
            {
                ShredFilePath = dlg.FileName;
            }
        }

        private async Task ExecuteShredFileAsync()
        {
            if (string.IsNullOrWhiteSpace(ShredFilePath) || !System.IO.File.Exists(ShredFilePath))
            {
                MessageBox.Show("Пожалуйста, выберите существующий файл для уничтожения.", "STORM File Shredder", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Вы уверены, что хотите БЕЗВОЗВРАТНО уничтожить файл:\n{ShredFilePath}\n\nВосстановление будет НЕВОЗМОЖНО даже специализированным ПО.", "STORM File Shredder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            ShredProgress = 0;
            StatusMessage = "Уничтожение файла методом многопроходной перезаписи...";

            var prog = new Progress<double>(p =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ShredProgress = p);
            });

            bool ok = await FileShredderService.Instance.ShredFileAsync(ShredFilePath, SelectedAlgorithm, prog);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusMessage = ok ? "✅ Файл успешно и безвозвратно уничтожен!" : "Ошибка при уничтожении файла";
                if (ok) ShredFilePath = string.Empty;
                IsBusy = false;
            });
        }
    }
}
