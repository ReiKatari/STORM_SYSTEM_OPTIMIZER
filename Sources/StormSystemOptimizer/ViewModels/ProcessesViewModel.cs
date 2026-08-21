using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private string _currentSort = "Память";

        [ObservableProperty]
        private bool _isSortAscending = false;

        [ObservableProperty]
        private string _sortDisplayName = "ОЗУ (по убыванию) ▼";

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
        public void SetSort(string sortName)
        {
            if (CurrentSort == sortName)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                CurrentSort = sortName;
                IsSortAscending = false;
            }

            string arrow = IsSortAscending ? "▲" : "▼";
            SortDisplayName = $"{CurrentSort} {arrow}";
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
                TrayService.Instance.ShowNotification("Процесс завершен", $"Процесс {SelectedProcess.ProcessName} (PID: {SelectedProcess.ProcessId}) остановлен.");
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
                TrayService.Instance.ShowNotification("Дерево процессов", $"Дерево процессов {SelectedProcess.ProcessName} остановлено.");
                await RefreshProcessesAsync();
            }
        }

        [RelayCommand]
        public void SuspendSelectedProcess()
        {
            if (SelectedProcess == null) return;
            bool ok = ProcessManagerService.Instance.SuspendProcess(SelectedProcess.ProcessId);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Процесс заморожен", $"Процесс {SelectedProcess.ProcessName} приостановлен.");
            }
        }

        [RelayCommand]
        public void ResumeSelectedProcess()
        {
            if (SelectedProcess == null) return;
            bool ok = ProcessManagerService.Instance.ResumeProcess(SelectedProcess.ProcessId);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Процесс возобновлен", $"Процесс {SelectedProcess.ProcessName} возобновил работу.");
            }
        }

        [RelayCommand]
        public void SetHighPriority()
        {
            if (SelectedProcess == null) return;
            bool ok = ProcessManagerService.Instance.SetProcessPriority(SelectedProcess.ProcessId, ProcessPriorityClass.High);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Приоритет", $"Процессу {SelectedProcess.ProcessName} назначен Высокий приоритет.");
            }
        }

        [RelayCommand]
        public void SetNormalPriority()
        {
            if (SelectedProcess == null) return;
            bool ok = ProcessManagerService.Instance.SetProcessPriority(SelectedProcess.ProcessId, ProcessPriorityClass.Normal);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Приоритет", $"Процессу {SelectedProcess.ProcessName} назначен Обычный приоритет.");
            }
        }

        [RelayCommand]
        public void SetIdlePriority()
        {
            if (SelectedProcess == null) return;
            bool ok = ProcessManagerService.Instance.SetProcessPriority(SelectedProcess.ProcessId, ProcessPriorityClass.Idle);
            if (ok)
            {
                TrayService.Instance.ShowNotification("Приоритет", $"Процессу {SelectedProcess.ProcessName} назначен Низкий приоритет.");
            }
        }

        [RelayCommand]
        public void SearchSelectedOnline()
        {
            if (SelectedProcess == null) return;
            ProcessManagerService.Instance.SearchOnline(SelectedProcess.ProcessName);
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

            filtered = CurrentSort switch
            {
                "ЦП" or "CPU" => IsSortAscending 
                    ? filtered.OrderBy(p => p.CpuPercentage) 
                    : filtered.OrderByDescending(p => p.CpuPercentage),
                "Имя" or "Name" => IsSortAscending 
                    ? filtered.OrderBy(p => p.ProcessName) 
                    : filtered.OrderByDescending(p => p.ProcessName),
                "PID" => IsSortAscending 
                    ? filtered.OrderBy(p => p.ProcessId) 
                    : filtered.OrderByDescending(p => p.ProcessId),
                _ => IsSortAscending 
                    ? filtered.OrderBy(p => p.MemoryMegabytes) 
                    : filtered.OrderByDescending(p => p.MemoryMegabytes)
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
