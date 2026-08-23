using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class FileUnlockerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _targetPath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Перетащите файл или папку в область ниже либо выберите через обзор...";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _hasLockers;

        [ObservableProperty]
        private bool _isContextMenuEnabled;

        [ObservableProperty]
        private LockingProcessItem? _selectedProcess;

        public ObservableCollection<LockingProcessItem> LockingProcesses { get; } = new();

        public FileUnlockerViewModel()
        {
            IsContextMenuEnabled = FileUnlockerService.Instance.IsContextMenuRegistered();
        }

        public void LoadPathFromCommandLine(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                TargetPath = path;
                _ = AnalyzeTargetAsync();
            }
        }

        [RelayCommand]
        public void BrowseFile()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Выберите заблокированный файл для анализа и освобождения:",
                Filter = "Все файлы (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                TargetPath = ofd.FileName;
                _ = AnalyzeTargetAsync();
            }
        }

        [RelayCommand]
        public void BrowseFolder()
        {
            var ofd = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите заблокированную папку для анализа и освобождения:"
            };
            if (ofd.ShowDialog() == true)
            {
                TargetPath = ofd.FolderName;
                _ = AnalyzeTargetAsync();
            }
        }

        [RelayCommand]
        public async Task AnalyzeTargetAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetPath)) return;

            IsBusy = true;
            StatusMessage = "🔍 Сканирование активных дескрипторов и процессов...";
            LockingProcesses.Clear();

            try
            {
                var lockers = await FileUnlockerService.Instance.FindLockingProcessesAsync(TargetPath);
                foreach (var l in lockers)
                {
                    LockingProcesses.Add(l);
                }

                HasLockers = LockingProcesses.Count > 0;
                if (HasLockers)
                {
                    StatusMessage = $"⚠️ Обнаружено процессов, удерживающих файл: {LockingProcesses.Count}";
                }
                else
                {
                    StatusMessage = "✓ Объект свободен. Дескрипторы блокировки не обнаружены.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка анализа: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task UnlockAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetPath)) return;

            IsBusy = true;
            StatusMessage = "⚡ Принудительное снятие блокировки и сброс дескрипторов...";

            try
            {
                bool ok = await FileUnlockerService.Instance.UnlockTargetAsync(TargetPath, true);
                await AnalyzeTargetAsync();
                StatusMessage = ok ? "✓ Файл / Папка успешно разблокированы!" : "Не удалось полностью освободить объект.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка разблокировки: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task UnlockAndDeleteAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetPath)) return;

            var res = MessageBox.Show($"Вы уверены, что хотите безвозвратно удалить объект:\n{TargetPath}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = "🗑️ Разблокировка и удаление объекта...";

            try
            {
                bool deleted = await FileUnlockerService.Instance.UnlockAndDeleteAsync(TargetPath);
                if (deleted)
                {
                    StatusMessage = "✓ Объект успешно удален из системы!";
                    TargetPath = string.Empty;
                    LockingProcesses.Clear();
                    HasLockers = false;
                }
                else
                {
                    StatusMessage = "⚠️ Файл запланирован к удалению при следующей перезагрузке системы.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка удаления: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task UnlockAndRenameAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetPath)) return;

            string currentName = Path.GetFileName(TargetPath);
            string? newName = PromptInput("Введите новое имя для файла / папки:", "Переименование объекта", currentName);
            if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;

            IsBusy = true;
            StatusMessage = "✏️ Разблокировка и переименование...";

            try
            {
                bool ok = await FileUnlockerService.Instance.UnlockAndRenameAsync(TargetPath, newName);
                if (ok)
                {
                    string dir = Path.GetDirectoryName(TargetPath) ?? "";
                    TargetPath = Path.Combine(dir, newName);
                    await AnalyzeTargetAsync();
                    StatusMessage = $"✓ Объект успешно переименован в: {newName}";
                }
                else
                {
                    StatusMessage = "Не удалось переименовать объект.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task UnlockAndMoveAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetPath)) return;

            var ofd = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите целевую папку для перемещения:"
            };
            if (ofd.ShowDialog() != true) return;

            string targetDir = ofd.FolderName;
            IsBusy = true;
            StatusMessage = "📦 Разблокировка и перемещение...";

            try
            {
                bool ok = await FileUnlockerService.Instance.UnlockAndMoveAsync(TargetPath, targetDir);
                if (ok)
                {
                    string name = Path.GetFileName(TargetPath);
                    TargetPath = Path.Combine(targetDir, name);
                    await AnalyzeTargetAsync();
                    StatusMessage = $"✓ Объект успешно перемещен в: {targetDir}";
                }
                else
                {
                    StatusMessage = "Не удалось переместить объект.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка перемещения: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void KillSelectedProcess(LockingProcessItem item)
        {
            if (item == null) return;
            try
            {
                FileUnlockerService.Instance.KillProcess(item.ProcessId);
                LockingProcesses.Remove(item);
                HasLockers = LockingProcesses.Count > 0;
                StatusMessage = $"✓ Процесс {item.ProcessName} (PID: {item.ProcessId}) принудительно завершен.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось завершить процесс: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ToggleContextMenu()
        {
            IsContextMenuEnabled = !IsContextMenuEnabled;
            FileUnlockerService.Instance.SetContextMenuRegistered(IsContextMenuEnabled);
            StatusMessage = IsContextMenuEnabled
                ? "✓ Пункт «Разблокировать через STORM Optimizer» добавлен в контекстное меню Windows!"
                : "Пункт удален из контекстного меню Windows.";
        }

        private string? PromptInput(string prompt, string title, string defaultText)
        {
            var win = new Window
            {
                Title = title,
                Width = 440,
                Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var border = new Border
            {
                Background = (Brush)Application.Current.FindResource("CardBackgroundBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("AccentPrimaryBrush"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            var sp = new StackPanel();
            var lbl = new TextBlock
            {
                Text = prompt,
                Foreground = (Brush)Application.Current.FindResource("TextPrimaryBrush"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var txt = new TextBox
            {
                Text = defaultText,
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = (Brush)Application.Current.FindResource("AppBackgroundBrush"),
                Foreground = (Brush)Application.Current.FindResource("TextPrimaryBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("CardBorderBrush"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button { Content = "Отмена", Width = 85, Margin = new Thickness(0, 0, 8, 0), Style = (Style)Application.Current.FindResource("StormSecondaryButton") };
            var btnOk = new Button { Content = "Применить ⚡", Width = 110, Style = (Style)Application.Current.FindResource("StormHeroButton") };

            string? result = null;
            btnCancel.Click += (s, e) => win.Close();
            btnOk.Click += (s, e) => { result = txt.Text.Trim(); win.Close(); };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnOk);
            sp.Children.Add(lbl);
            sp.Children.Add(txt);
            sp.Children.Add(btnPanel);
            border.Child = sp;
            win.Content = border;
            win.ShowDialog();
            return result;
        }
    }
}
