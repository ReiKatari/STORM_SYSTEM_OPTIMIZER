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
    public partial class OfficeDeployerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _installedOfficeInfo = "Проверка...";

        [ObservableProperty]
        private OfficeProductEdition? _selectedEdition;

        [ObservableProperty]
        private string _selectedEditionVersionText = "Версия билда: 16.0.17932.20162 (LTSC 2024)";

        [ObservableProperty]
        private string _selectedEditionDesc = "LTSC бессрочная корпоративная лицензия";

        [ObservableProperty]
        private string _updateStatusText = "Проверка статуса обновлений...";

        [ObservableProperty]
        private string _selectedArchitecture = "64";

        [ObservableProperty]
        private string _selectedLanguage = "ru-ru";

        [ObservableProperty]
        private bool _installWord = true;

        [ObservableProperty]
        private bool _installExcel = true;

        [ObservableProperty]
        private bool _installPowerPoint = true;

        [ObservableProperty]
        private bool _installAccess = true;

        [ObservableProperty]
        private bool _installOutlook = false;

        [ObservableProperty]
        private bool _installOneNote = false;

        [ObservableProperty]
        private bool _installPublisher = false;

        [ObservableProperty]
        private bool _installVisio = false;

        [ObservableProperty]
        private bool _installProject = false;

        [ObservableProperty]
        private bool _excludeTeams = true;

        [ObservableProperty]
        private bool _excludeOneDrive = true;

        [ObservableProperty]
        private bool _excludeBing = true;

        [ObservableProperty]
        private bool _autoActivate = true;

        [ObservableProperty]
        private string _selectedKmsServer = "kms.digiboy.ir";

        [ObservableProperty]
        private string _statusLog = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<OfficeProductEdition> Editions { get; } = new();
        public ObservableCollection<string> KmsServers { get; } = new();

        public ICommand InstallCommand { get; }
        public ICommand ActivateCommand { get; }
        public ICommand CleanKeysCommand { get; }
        public ICommand ForceRemoveCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand TriggerUpdateCommand { get; }
        public ICommand SelectAllAppsCommand { get; }
        public ICommand SelectBasicAppsCommand { get; }

        public OfficeDeployerViewModel()
        {
            foreach (var ed in OfficeDeployerService.Instance.SupportedEditions) Editions.Add(ed);
            if (Editions.Count > 0)
            {
                SelectedEdition = Editions[0];
                UpdateEditionDetails(SelectedEdition);
            }

            foreach (var s in OfficeDeployerService.Instance.KmsServers) KmsServers.Add(s);

            InstallCommand = new RelayCommand(async () => await ExecuteInstallAsync());
            ActivateCommand = new RelayCommand(async () => await ExecuteActivateAsync());
            CleanKeysCommand = new RelayCommand(async () => await ExecuteCleanKeysAsync());
            ForceRemoveCommand = new RelayCommand(async () => await ExecuteForceRemoveAsync());
            RefreshStatusCommand = new RelayCommand(() => RefreshStatus());
            CheckUpdateCommand = new RelayCommand(async () => await ExecuteCheckUpdateAsync());
            TriggerUpdateCommand = new RelayCommand(async () => await ExecuteTriggerUpdateAsync());
            SelectAllAppsCommand = new RelayCommand(() => SetAllApps(true));
            SelectBasicAppsCommand = new RelayCommand(() => SetBasicApps());

            RefreshStatus();
        }

        partial void OnSelectedEditionChanged(OfficeProductEdition? value)
        {
            if (value != null)
            {
                UpdateEditionDetails(value);
                _ = ExecuteCheckUpdateAsync();
            }
        }

        private void UpdateEditionDetails(OfficeProductEdition ed)
        {
            SelectedEditionVersionText = $"Билд CDN: v{ed.TargetVersion} • Выпуск {ed.ReleaseDate} ({ed.Channel})";
            SelectedEditionDesc = ed.Description;
        }

        public void RefreshStatus()
        {
            InstalledOfficeInfo = OfficeDeployerService.Instance.GetInstalledOfficeInfo();
            _ = ExecuteCheckUpdateAsync();
        }

        private async Task ExecuteCheckUpdateAsync()
        {
            if (SelectedEdition == null) return;
            UpdateStatusText = "Проверка наличия обновлений на серверах Microsoft CDN...";
            string status = await OfficeDeployerService.Instance.CheckUpdateStatusAsync(SelectedEdition);
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                UpdateStatusText = status;
            });
        }

        private async Task ExecuteTriggerUpdateAsync()
        {
            IsBusy = true;
            AppendLog("Запуск диспетчера обновлений Microsoft Office C2R...");
            var progress = new Progress<string>(AppendLog);
            await OfficeDeployerService.Instance.TriggerOnlineUpdateAsync(progress);
            RefreshStatus();
            IsBusy = false;
        }

        private void SetAllApps(bool val)
        {
            InstallWord = val;
            InstallExcel = val;
            InstallPowerPoint = val;
            InstallAccess = val;
            InstallOutlook = val;
            InstallOneNote = val;
            InstallPublisher = val;
            InstallVisio = val;
            InstallProject = val;
        }

        private void SetBasicApps()
        {
            InstallWord = true;
            InstallExcel = true;
            InstallPowerPoint = true;
            InstallAccess = false;
            InstallOutlook = false;
            InstallOneNote = false;
            InstallPublisher = false;
            InstallVisio = false;
            InstallProject = false;
        }

        private void AppendLog(string message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            });
        }

        private async Task ExecuteInstallAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            AppendLog("Запуск процесса развертывания Microsoft Office...");

            var options = new OfficeDeployOptions
            {
                EditionId = SelectedEdition?.Id ?? "ProPlus2024Volume",
                Architecture = SelectedArchitecture,
                Language = SelectedLanguage,
                InstallWord = InstallWord,
                InstallExcel = InstallExcel,
                InstallPowerPoint = InstallPowerPoint,
                InstallAccess = InstallAccess,
                InstallOutlook = InstallOutlook,
                InstallOneNote = InstallOneNote,
                InstallPublisher = InstallPublisher,
                InstallVisio = InstallVisio,
                InstallProject = InstallProject,
                ExcludeTeams = ExcludeTeams,
                ExcludeOneDrive = ExcludeOneDrive,
                ExcludeBing = ExcludeBing,
                AutoActivate = AutoActivate,
                KmsServer = SelectedKmsServer
            };

            var progress = new Progress<string>(AppendLog);
            bool ok = await OfficeDeployerService.Instance.InstallOfficeAsync(options, progress);

            RefreshStatus();
            IsBusy = false;
        }

        private async Task ExecuteActivateAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            AppendLog($"Запуск KMS-активации через {SelectedKmsServer}...");
            var progress = new Progress<string>(AppendLog);
            await OfficeDeployerService.Instance.ActivateOfficeKmsAsync(SelectedKmsServer, progress);
            RefreshStatus();
            IsBusy = false;
        }

        private async Task ExecuteCleanKeysAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            AppendLog("Очистка устаревших лицензионных ключей Office...");
            var progress = new Progress<string>(AppendLog);
            await OfficeDeployerService.Instance.CleanLegacyKeysAsync(progress);
            RefreshStatus();
            IsBusy = false;
        }

        private async Task ExecuteForceRemoveAsync()
        {
            var res = MessageBox.Show("Вы действительно хотите принудительно удалить все версии Microsoft Office и очистить все следы и службы?", "STORM Force Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            IsBusy = true;
            AppendLog("Запуск принудительного удаления Microsoft Office...");
            var progress = new Progress<string>(AppendLog);
            await OfficeDeployerService.Instance.ForceRemoveOfficeAsync(progress);
            RefreshStatus();
            IsBusy = false;
        }
    }
}
