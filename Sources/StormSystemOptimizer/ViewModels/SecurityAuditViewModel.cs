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
        private string _shredFolderPath = string.Empty;

        [ObservableProperty]
        private string _selectedDrive = "C:";

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
        public ObservableCollection<string> AvailableDrives { get; } = new();

        public ICommand ScanThreatsCommand { get; }
        public ICommand ResolveThreatCommand { get; }
        public ICommand ScanFirewallCommand { get; }
        public ICommand PurgeOrphanedFirewallCommand { get; }
        public ICommand ShredFileCommand { get; }
        public ICommand BrowseShredFileCommand { get; }
        public ICommand BrowseShredFolderCommand { get; }
        public ICommand ShredFolderCommand { get; }
        public ICommand WipeFreeSpaceCommand { get; }

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
            BrowseShredFolderCommand = new RelayCommand(() => ExecuteBrowseFolder());
            ShredFolderCommand = new RelayCommand(async () => await ExecuteShredFolderAsync());
            WipeFreeSpaceCommand = new RelayCommand(async () => await ExecuteWipeFreeSpaceAsync());

            try
            {
                foreach (var d in System.IO.DriveInfo.GetDrives())
                {
                    if (d.IsReady && (d.DriveType == System.IO.DriveType.Fixed || d.DriveType == System.IO.DriveType.Removable))
                    {
                        AvailableDrives.Add(d.Name.TrimEnd('\\'));
                    }
                }
                if (AvailableDrives.Count > 0) SelectedDrive = AvailableDrives[0];
            }
            catch { }

            _ = ExecuteScanThreatsAsync();
            _ = ExecuteScanFirewallAsync();
        }

        public async Task ExecuteScanThreatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
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
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сканировании: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteResolveThreatAsync(SecurityThreatItem threat)
        {
            IsBusy = true;
            try
            {
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
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка устранения: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteScanFirewallAsync()
        {
            try
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
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка брандмауэра: {ex.Message}";
            }
        }

        private async Task ExecutePurgeFirewallAsync()
        {
            IsBusy = true;
            try
            {
                StatusMessage = "Удаление устаревших правил Брандмауэра...";
                int purged = await FirewallAuditService.Instance.PurgeOrphanedRulesAsync();
                await ExecuteScanFirewallAsync();

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    StatusMessage = $"Очистка завершена: удалено {purged} сиротских правил.";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка очистки правил: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
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

        private void ExecuteBrowseFolder()
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку для гарантированного уничтожения (Шредер)"
            };
            if (dlg.ShowDialog() == true)
            {
                ShredFolderPath = dlg.FolderName;
            }
        }

        private async Task ExecuteShredFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(ShredFolderPath) || !System.IO.Directory.Exists(ShredFolderPath))
            {
                MessageBox.Show("Пожалуйста, выберите существующую папку для уничтожения.", "STORM File Shredder", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Вы уверены, что хотите БЕЗВОЗВРАТНО уничтожить ВСЮ папку и её содержимое:\n{ShredFolderPath}\n\nВосстановление будет НЕВОЗМОЖНО.", "STORM File Shredder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            ShredProgress = 0;
            StatusMessage = "Рекурсивное многопроходное уничтожение файлов в папке...";

            var prog = new Progress<double>(p =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ShredProgress = p);
            });

            bool ok = await FileShredderService.Instance.ShredDirectoryAsync(ShredFolderPath, SelectedAlgorithm, prog);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusMessage = ok ? "✅ Папка и все вложенные файлы успешно уничтожены!" : "Ошибка при уничтожении папки";
                if (ok) ShredFolderPath = string.Empty;
                IsBusy = false;
            });
        }

        private async Task ExecuteWipeFreeSpaceAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedDrive))
            {
                MessageBox.Show("Пожалуйста, выберите диск для затирания свободного места.", "STORM Free Space Wiper", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Запустить затирание свободного места на диске {SelectedDrive}?\n\nВсе ранее удаленные файлы будут перезаписаны нулями, исключая возможность их восстановления через Recuva / R-Studio.\nСуществующие файлы затронуты НЕ будут.", "STORM Free Space Wiper", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            ShredProgress = 0;
            StatusMessage = $"Затирание неразмеченного свободного пространства на диске {SelectedDrive}...";

            var prog = new Progress<double>(p =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ShredProgress = p);
            });

            bool ok = await FileShredderService.Instance.WipeFreeSpaceAsync(SelectedDrive, prog);

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusMessage = ok ? $"✅ Свободное пространство на диске {SelectedDrive} успешно очищено!" : "Ошибка очистки свободного места";
                IsBusy = false;
            });
        }
    }
}
