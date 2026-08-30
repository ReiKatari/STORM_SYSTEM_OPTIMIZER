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
    public partial class DatabaseOptimizerViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к сканированию баз данных SQLite";

        [ObservableProperty]
        private string _totalFoundCount = "0";

        [ObservableProperty]
        private string _totalOriginalSize = "0 МБ";

        [ObservableProperty]
        private string _reclaimedSpaceText = "0 МБ";

        public ObservableCollection<SqliteDbTarget> Databases { get; } = new();

        public ICommand ScanCommand { get; }
        public ICommand OptimizeAllCommand { get; }

        public DatabaseOptimizerViewModel()
        {
            ScanCommand = new RelayCommand(async () => await ExecuteScanAsync());
            OptimizeAllCommand = new RelayCommand(async () => await ExecuteOptimizeAllAsync());

            _ = ExecuteScanAsync();
        }

        public async Task ExecuteScanAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Сканирование баз данных браузеров, мессенджеров и лаунчеров...";

            var progress = new Progress<string>(msg =>
            {
                Application.Current?.Dispatcher?.Invoke(() => StatusMessage = msg);
            });

            var list = await DatabaseOptimizerService.Instance.ScanDatabasesAsync(progress);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Databases.Clear();
                long totalBytes = 0;
                foreach (var d in list)
                {
                    Databases.Add(d);
                    totalBytes += d.OriginalSizeBytes;
                }

                TotalFoundCount = Databases.Count.ToString();
                TotalOriginalSize = FormatHelper.FormatBytes(totalBytes);
                StatusMessage = $"Найдено баз данных: {Databases.Count} (Общий объем: {TotalOriginalSize})";
                IsBusy = false;
            });
        }

        public async Task ExecuteOptimizeAllAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var progress = new Progress<string>(msg =>
            {
                Application.Current?.Dispatcher?.Invoke(() => StatusMessage = msg);
            });

            var res = await DatabaseOptimizerService.Instance.OptimizeAllDatabasesAsync(progress);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Databases.Clear();
                foreach (var t in res.Targets) Databases.Add(t);

                ReclaimedSpaceText = FormatHelper.FormatBytes(res.BytesReclaimed);
                StatusMessage = $"Дефрагментация завершена! Оптимизировано {res.TotalDatabasesOptimized} баз. Освобождено: {ReclaimedSpaceText}.";
                IsBusy = false;
            });
        }
    }
}
