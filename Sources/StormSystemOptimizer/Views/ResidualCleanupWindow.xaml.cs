using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.Views
{
    public class ResidualItemEntry : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string Path { get; set; } = string.Empty;
        public bool IsFolder { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class ResidualCleanupWindow : Window
    {
        private readonly InstalledAppItem _app;
        private readonly ObservableCollection<ResidualItemEntry> _regItems = new();
        private readonly ObservableCollection<ResidualItemEntry> _folderItems = new();

        public int CleanedDirsCount { get; private set; }
        public int CleanedRegsCount { get; private set; }

        public ResidualCleanupWindow(InstalledAppItem app, List<string> folders, List<string> registryKeys, double sizeMb)
        {
            InitializeComponent();
            _app = app;

            TxtAppTitle.Text = $"Остаточные следы «{app.DisplayName}»";
            TxtSubtitle.Text = $"Деинсталлятор завершил работу. STORM обнаружил следующие неудаленные компоненты ({folders.Count} папок, {registryKeys.Count} ключей реестра, {sizeMb:F1} МБ):";

            TxtRegistryHeader.Text = $"Реестр Windows ({registryKeys.Count})";
            TxtFoldersHeader.Text = $"Файлы и папки ({folders.Count}, {sizeMb:F1} МБ)";

            foreach (var r in registryKeys)
            {
                _regItems.Add(new ResidualItemEntry { Path = r, IsFolder = false, IsSelected = true });
            }
            ListRegistryKeys.ItemsSource = _regItems;
            PnlRegistrySection.Visibility = registryKeys.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var f in folders)
            {
                _folderItems.Add(new ResidualItemEntry { Path = f, IsFolder = true, IsSelected = true });
            }
            ListFolders.ItemsSource = _folderItems;
            PnlFoldersSection.Visibility = folders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            UpdateSelectionCount();
        }

        private void UpdateSelectionCount()
        {
            int total = _regItems.Count + _folderItems.Count;
            int selected = _regItems.Count(i => i.IsSelected) + _folderItems.Count(i => i.IsSelected);
            TxtSelectedCount.Text = $"Выбрано: {selected} из {total} элементов";
            TxtTotals.Text = $"{selected} выбрано";
            BtnClean.IsEnabled = selected > 0;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool select = ChkSelectAll.IsChecked ?? false;
            foreach (var item in _regItems) item.IsSelected = select;
            foreach (var item in _folderItems) item.IsSelected = select;
            UpdateSelectionCount();
        }

        private void ItemCheck_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectionCount();
        }

        private async void BtnClean_Click(object sender, RoutedEventArgs e)
        {
            BtnClean.IsEnabled = false;
            var selectedFolders = _folderItems.Where(i => i.IsSelected).Select(i => i.Path).ToList();
            var selectedKeys = _regItems.Where(i => i.IsSelected).Select(i => i.Path).ToList();

            var (dDirs, dRegs) = await SoftwareUninstallerService.Instance.CleanSpecificResidualsAsync(_app, selectedFolders, selectedKeys);
            CleanedDirsCount = dDirs;
            CleanedRegsCount = dRegs;

            TrayService.Instance.ShowNotification("Очистка остатков ⚡", $"Успешно зачищено {dDirs} папок и {dRegs} ключей реестра программы «{_app.DisplayName}».");

            DialogResult = true;
            Close();
        }
    }
}