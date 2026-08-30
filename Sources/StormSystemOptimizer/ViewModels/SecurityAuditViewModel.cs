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
        private string _statusMessage = "Готов к проверке безопасности и аудиту";

        [ObservableProperty]
        private string _shredFilePath = string.Empty;

        [ObservableProperty]
        private double _shredProgress = 0;

        [ObservableProperty]
        private ShredAlgorithm _selectedAlgorithm = ShredAlgorithm.DoD5220;

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
            ScanThreatsCommand = new RelayCommand(async () => await ExecuteScanThreatsAsync(), () => !IsBusy);
            ResolveThreatCommand = new RelayCommand<SecurityThreatItem>(async threat =>
            {
                if (threat != null) await ExecuteResolveThreatAsync(threat);
            });
            ScanFirewallCommand = new RelayCommand(async () => await ExecuteScanFirewallAsync(), () => !IsBusy);
            PurgeOrphanedFirewallCommand = new RelayCommand(async () => await ExecutePurgeFirewallAsync(), () => !IsBusy);
            ShredFileCommand = new RelayCommand(async () => await ExecuteShredFileAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(ShredFilePath));
            BrowseShredFileCommand = new RelayCommand(() => ExecuteBrowseFile());

            _ = ExecuteScanThreatsAsync();
        }

        public async Task ExecuteScanThreatsAsync()
        {
            IsBusy = true;
            Threats.Clear();
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var list = await MalwareHeuristicsService.Instance.ScanSystemThreatsAsync(progress);
            foreach (var t in list) Threats.Add(t);
            StatusMessage = $"Сканирование завершено. Обнаружено угроз: {Threats.Count}";
            IsBusy = false;
        }

        private async Task ExecuteResolveThreatAsync(SecurityThreatItem threat)
        {
            IsBusy = true;
            StatusMessage = $"Устранение угрозы: {threat.Title}...";
            bool ok = await MalwareHeuristicsService.Instance.ResolveThreatAsync(threat);
            if (ok)
            {
                Threats.Remove(threat);
                StatusMessage = "Угроза успешно нейтрализована!";
            }
            else
            {
                StatusMessage = "Не удалось устранить угрозу (возможно, файл заблокирован).";
            }
            IsBusy = false;
        }

        public async Task ExecuteScanFirewallAsync()
        {
            IsBusy = true;
            FirewallRules.Clear();
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var list = await FirewallAuditService.Instance.ScanFirewallRulesAsync(progress);
            foreach (var r in list) FirewallRules.Add(r);
            StatusMessage = $"Правил загружено: {FirewallRules.Count}";
            IsBusy = false;
        }

        private async Task ExecutePurgeFirewallAsync()
        {
            IsBusy = true;
            StatusMessage = "Удаление устаревших правил Брандмауэра...";
            int purged = await FirewallAuditService.Instance.PurgeOrphanedRulesAsync();
            await ExecuteScanFirewallAsync();
            StatusMessage = $"Удалено сиротских правил: {purged}";
            IsBusy = false;
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
            if (string.IsNullOrWhiteSpace(ShredFilePath) || !System.IO.File.Exists(ShredFilePath)) return;

            var res = MessageBox.Show($"Вы уверены, что хотите БЕЗВОЗВРАТНО уничтожить файл:\n{ShredFilePath}\n\nВосстановление будет НЕВОЗМОЖНО даже специализированным ПО.", "STORM File Shredder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            ShredProgress = 0;
            StatusMessage = "Уничтожение файла методом перезаписи...";

            var prog = new Progress<double>(p => ShredProgress = p);
            bool ok = await FileShredderService.Instance.ShredFileAsync(ShredFilePath, SelectedAlgorithm, prog);

            StatusMessage = ok ? "Файл успешно и безвозвратно уничтожен!" : "Ошибка при уничтожении файла";
            if (ok) ShredFilePath = string.Empty;
            IsBusy = false;
        }
    }
}
