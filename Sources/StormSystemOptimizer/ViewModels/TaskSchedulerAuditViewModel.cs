using System;
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
    public partial class TaskSchedulerAuditViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к аудиту задач планировщика и WMI";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Все";

        [ObservableProperty]
        private string _totalTasksCount = "0";

        [ObservableProperty]
        private string _suspiciousTasksCount = "0";

        [ObservableProperty]
        private string _wmiCount = "0";

        public ObservableCollection<ScheduledTaskItem> Tasks { get; } = new();
        public ObservableCollection<ScheduledTaskItem> FilteredTasks { get; } = new();

        public ICommand ScanTasksCommand { get; }
        public ICommand ToggleTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DeleteAllSuspiciousCommand { get; }
        public ICommand FilterCategoryCommand { get; }

        public TaskSchedulerAuditViewModel()
        {
            ScanTasksCommand = new RelayCommand(async () => await ScanTasksAsync());
            ToggleTaskCommand = new RelayCommand<ScheduledTaskItem>(async task =>
            {
                if (task != null) await ToggleTaskAsync(task);
            });
            DeleteTaskCommand = new RelayCommand<ScheduledTaskItem>(async task =>
            {
                if (task != null) await DeleteTaskAsync(task);
            });
            DeleteAllSuspiciousCommand = new RelayCommand(async () => await DeleteAllSuspiciousAsync());
            FilterCategoryCommand = new RelayCommand<string>(cat =>
            {
                SelectedFilter = cat ?? "Все";
                ApplyFilters();
            });

            _ = ScanTasksAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        public async Task ScanTasksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Глубокое сканирование Task Scheduler и скрытых WMI подписок...";

            try
            {
                var list = await TaskSchedulerAuditService.Instance.ScanAllTasksAndWmiAsync();

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Tasks.Clear();
                    foreach (var t in list) Tasks.Add(t);

                    TotalTasksCount = Tasks.Count.ToString();
                    SuspiciousTasksCount = Tasks.Count(t => t.RiskLevel != TaskRiskLevel.Safe).ToString();
                    WmiCount = Tasks.Count(t => t.IsWmi).ToString();

                    ApplyFilters();

                    StatusMessage = $"Аудит завершён: проанализировано {Tasks.Count} задач и WMI-триггеров.";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка аудита: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            var query = Tasks.AsEnumerable();

            if (!string.Equals(SelectedFilter, "Все", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedFilter.Equals("Подозрительные", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(t => t.RiskLevel != TaskRiskLevel.Safe);
                else if (SelectedFilter.Equals("Планировщик", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(t => !t.IsWmi);
                else if (SelectedFilter.Equals("WMI", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(t => t.IsWmi);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(t =>
                    t.TaskName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.ActionCommand.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            FilteredTasks.Clear();
            foreach (var item in query)
            {
                FilteredTasks.Add(item);
            }
        }

        private async Task ToggleTaskAsync(ScheduledTaskItem item)
        {
            if (item.IsWmi)
            {
                MessageBox.Show("WMI-потребители событий не поддерживают временное отключение. Их можно только удалить.", "WMI Триггер", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool ok = await TaskSchedulerAuditService.Instance.ToggleTaskStateAsync(item);
            if (ok)
            {
                StatusMessage = item.IsEnabled ? $"Задача «{item.TaskName}» включена!" : $"Задача «{item.TaskName}» отключена!";
                ApplyFilters();
            }
            else
            {
                MessageBox.Show($"Не удалось изменить состояние задачи «{item.TaskName}». Требуются права Администратора.", "STORM Task Audit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task DeleteTaskAsync(ScheduledTaskItem item)
        {
            var res = MessageBox.Show($"Вы действительно хотите удалить {item.TypeBadge} «{item.TaskName}»?\n\nКоманда: {item.ActionCommand}", "Удаление задачи", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            bool ok = await TaskSchedulerAuditService.Instance.DeleteTaskAsync(item);
            if (ok)
            {
                Tasks.Remove(item);
                FilteredTasks.Remove(item);
                TotalTasksCount = Tasks.Count.ToString();
                SuspiciousTasksCount = Tasks.Count(t => t.RiskLevel != TaskRiskLevel.Safe).ToString();
                WmiCount = Tasks.Count(t => t.IsWmi).ToString();
                StatusMessage = $"Элемент «{item.TaskName}» успешно удален!";
            }
            else
            {
                MessageBox.Show($"Не удалось удалить «{item.TaskName}». Требуются права Администратора.", "STORM Task Audit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task DeleteAllSuspiciousAsync()
        {
            var suspicious = Tasks.Where(t => t.RiskLevel != TaskRiskLevel.Safe).ToList();
            if (suspicious.Count == 0)
            {
                MessageBox.Show("Подозрительных задач или скрытых WMI-триггеров не обнаружено.", "STORM Task Audit", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Обнаружено {suspicious.Count} подозрительных задач со скрытым запуском скриптов или из каталога Temp.\n\nУдалить их все в 1 клик?", "Удаление подозрительных задач", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            int deleted = 0;
            foreach (var task in suspicious)
            {
                if (await TaskSchedulerAuditService.Instance.DeleteTaskAsync(task))
                {
                    Tasks.Remove(task);
                    deleted++;
                }
            }

            ApplyFilters();
            TotalTasksCount = Tasks.Count.ToString();
            SuspiciousTasksCount = Tasks.Count(t => t.RiskLevel != TaskRiskLevel.Safe).ToString();
            WmiCount = Tasks.Count(t => t.IsWmi).ToString();
            StatusMessage = $"Удалено {deleted} подозрительных задач!";
        }
    }
}
