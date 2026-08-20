using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class ProcessesViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Все процессы";

        [ObservableProperty]
        private string _statusText = "Загрузка списка процессов...";

        [ObservableProperty]
        private int _totalProcessesCount = 0;

        [ObservableProperty]
        private string _totalMemoryText = "0 ГБ";

        [ObservableProperty]
        private int _safeToKillCount = 0;

        [ObservableProperty]
        private string _safeReclaimableText = "0 МБ";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private ProcessInfoItem? _selectedProcess;

        public ObservableCollection<ProcessInfoItem> AllProcesses { get; } = new();
        public ObservableCollection<ProcessInfoItem> FilteredProcesses { get; } = new();

        public bool IsFilterAllSelected => SelectedFilter == "Все процессы";
        public bool IsFilterSafeSelected => SelectedFilter == "Безопасно завершить";
        public bool IsFilterUserSelected => SelectedFilter == "Пользовательские";
        public bool IsFilterSystemSelected => SelectedFilter == "Системные Windows";

        public ProcessesViewModel()
        {
            _ = RefreshProcessesAsync();
        }

        [RelayCommand]
        public void SetFilter(string filterName)
        {
            SelectedFilter = filterName;
            OnPropertyChanged(nameof(IsFilterAllSelected));
            OnPropertyChanged(nameof(IsFilterSafeSelected));
            OnPropertyChanged(nameof(IsFilterUserSelected));
            OnPropertyChanged(nameof(IsFilterSystemSelected));
            ApplyFilters();
        }

        [RelayCommand]
        public async Task RefreshProcessesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Анализ и сбор метрик процессов...";

            var list = await ProcessManagerService.Instance.GetAllProcessesAsync();

            AllProcesses.Clear();
            foreach (var item in list)
            {
                AllProcesses.Add(item);
            }

            ApplyFilters();
            UpdateStats();

            IsBusy = false;
            StatusText = $"Всего активных процессов: {AllProcesses.Count}";
        }

        [RelayCommand]
        public async Task TerminateSelectedProcessAsync()
        {
            if (SelectedProcess == null || !SelectedProcess.CanBeTerminated) return;

            bool ok = ProcessManagerService.Instance.TerminateProcess(SelectedProcess.ProcessId);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Процесс завершен", $"Процесс {SelectedProcess.ProcessName} (PID: {SelectedProcess.ProcessId}) успешно остановлен.");
                await RefreshProcessesAsync();
            }
        }

        [RelayCommand]
        public async Task TerminateProcessTreeAsync()
        {
            if (SelectedProcess == null || !SelectedProcess.CanBeTerminated) return;

            bool ok = ProcessManagerService.Instance.TerminateProcessTree(SelectedProcess.ProcessId);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Дерево процессов завершено", $"Все процессы ветки {SelectedProcess.ProcessName} остановлены.");
                await RefreshProcessesAsync();
            }
        }

        [RelayCommand]
        public async Task KillAllSafeBackgroundAsync()
        {
            if (IsBusy) return;
            var safeList = AllProcesses.Where(p => p.SafetyStatus == ProcessSafetyStatus.SafeToKill).ToList();
            if (safeList.Count == 0) return;

            IsBusy = true;
            StatusText = "Остановка фоновых неиспользуемых процессов...";

            int killed = 0;
            double freedMb = 0;
            await Task.Run(() =>
            {
                foreach (var p in safeList)
                {
                    if (ProcessManagerService.Instance.TerminateProcess(p.ProcessId))
                    {
                        killed++;
                        freedMb += p.MemoryMegabytes;
                    }
                }
            });

            await RefreshProcessesAsync();
            TrayService.Instance.ShowNotification("Фоновые процессы очищены", $"Завершено {killed} фоновых процессов. Освобождено ~{freedMb:F0} МБ RAM.");
        }

        [RelayCommand]
        public void OpenFileLocation()
        {
            if (SelectedProcess != null && !string.IsNullOrEmpty(SelectedProcess.ExecutablePath))
            {
                ProcessManagerService.Instance.OpenProcessLocation(SelectedProcess.ExecutablePath);
            }
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilters();
        partial void OnSelectedFilterChanged(string value) => ApplyFilters();

        public void ApplyFilters()
        {
            FilteredProcesses.Clear();
            var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = AllProcesses.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(p =>
                    p.ProcessName.ToLowerInvariant().Contains(query) ||
                    p.Description.ToLowerInvariant().Contains(query) ||
                    p.WindowTitle.ToLowerInvariant().Contains(query) ||
                    p.ProcessId.ToString().Contains(query));
            }

            filtered = SelectedFilter switch
            {
                "Безопасно завершить" => filtered.Where(p => p.SafetyStatus == ProcessSafetyStatus.SafeToKill),
                "Пользовательские" => filtered.Where(p => p.SafetyStatus == ProcessSafetyStatus.UserApp),
                "Системные Windows" => filtered.Where(p => p.SafetyStatus == ProcessSafetyStatus.CriticalSystem),
                _ => filtered
            };

            foreach (var item in filtered)
            {
                FilteredProcesses.Add(item);
            }
        }

        private void UpdateStats()
        {
            TotalProcessesCount = AllProcesses.Count;
            double totalMb = AllProcesses.Sum(p => p.MemoryMegabytes);
            TotalMemoryText = $"{totalMb / 1024.0:F2} ГБ";

            var safe = AllProcesses.Where(p => p.SafetyStatus == ProcessSafetyStatus.SafeToKill).ToList();
            SafeToKillCount = safe.Count;
            double safeMb = safe.Sum(p => p.MemoryMegabytes);
            SafeReclaimableText = safeMb >= 1024 ? $"{safeMb / 1024.0:F2} ГБ" : $"{safeMb:F0} МБ";
        }
    }
}
