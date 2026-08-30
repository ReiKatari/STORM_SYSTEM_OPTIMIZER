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

        public OfficeDeployerViewModel()
        {
            foreach (var ed in OfficeDeployerService.Instance.SupportedEditions) Editions.Add(ed);
            if (Editions.Count > 0) SelectedEdition = Editions[0];

            foreach (var s in OfficeDeployerService.Instance.KmsServers) KmsServers.Add(s);

            InstallCommand = new RelayCommand(async () => await ExecuteInstallAsync(), () => !IsBusy);
            ActivateCommand = new RelayCommand(async () => await ExecuteActivateAsync(), () => !IsBusy);
            CleanKeysCommand = new RelayCommand(async () => await ExecuteCleanKeysAsync(), () => !IsBusy);
            ForceRemoveCommand = new RelayCommand(async () => await ExecuteForceRemoveAsync(), () => !IsBusy);
            RefreshStatusCommand = new RelayCommand(() => RefreshStatus());

            RefreshStatus();
        }

        public void RefreshStatus()
        {
            InstalledOfficeInfo = OfficeDeployerService.Instance.GetInstalledOfficeInfo();
        }

        private void AppendLog(string message)
        {
            StatusLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        private async Task ExecuteInstallAsync()
        {
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
            IsBusy = true;
            AppendLog($"Запуск KMS-активации через {SelectedKmsServer}...");
            var progress = new Progress<string>(AppendLog);
            await OfficeDeployerService.Instance.ActivateOfficeKmsAsync(SelectedKmsServer, progress);
            RefreshStatus();
            IsBusy = false;
        }

        private async Task ExecuteCleanKeysAsync()
        {
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
