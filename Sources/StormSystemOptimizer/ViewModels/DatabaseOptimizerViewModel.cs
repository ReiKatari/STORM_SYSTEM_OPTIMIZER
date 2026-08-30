using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class DatabaseOptimizerViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к дефрагментации баз данных SQLite";

        [ObservableProperty]
        private string _reclaimedSpaceText = "0 МБ";

        public ObservableCollection<SqliteDbTarget> Databases { get; } = new();

        public ICommand ScanCommand { get; }
        public ICommand OptimizeAllCommand { get; }

        public DatabaseOptimizerViewModel()
        {
            ScanCommand = new RelayCommand(async () => await ExecuteScanAsync());
            OptimizeAllCommand = new RelayCommand(async () => await ExecuteOptimizeAllAsync(), () => !IsBusy);

            _ = ExecuteScanAsync();
        }

        public async Task ExecuteScanAsync()
        {
            IsBusy = true;
            StatusMessage = "Сканирование баз данных браузеров, мессенджеров и лаунчеров...";
            Databases.Clear();

            var list = await DatabaseOptimizerService.Instance.ScanDatabasesAsync();
            long totalBytes = 0;
            foreach (var d in list)
            {
                Databases.Add(d);
                totalBytes += d.OriginalSizeBytes;
            }

            StatusMessage = $"Найдено баз данных SQLite: {Databases.Count} (Общий объем: {FormatHelper.FormatBytes(totalBytes)})";
            IsBusy = false;
        }

        public async Task ExecuteOptimizeAllAsync()
        {
            IsBusy = true;
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var res = await DatabaseOptimizerService.Instance.OptimizeAllDatabasesAsync(progress);

            Databases.Clear();
            foreach (var t in res.Targets) Databases.Add(t);

            ReclaimedSpaceText = FormatHelper.FormatBytes(res.BytesReclaimed);
            StatusMessage = $"Дефрагментация завершена! Обработано {res.TotalDatabasesOptimized} баз. Освобождено места: {ReclaimedSpaceText}.";
            IsBusy = false;
        }
    }
}
