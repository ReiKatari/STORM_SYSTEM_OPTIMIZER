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
    public partial class InterruptAffinityViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Готов к настройке прерываний и задержек";

        [ObservableProperty]
        private int _currentImodInterval = 1000;

        [ObservableProperty]
        private PciDeviceInterruptInfo? _selectedDevice;

        public ObservableCollection<PciDeviceInterruptInfo> Devices { get; } = new();
        public ObservableCollection<GameQosProfile> QosProfiles { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand ApplyEsportsPresetCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand ApplyImodCommand { get; }
        public ICommand ApplyQosCommand { get; }

        public InterruptAffinityViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ApplyEsportsPresetCommand = new RelayCommand(async () => await ExecuteEsportsPresetAsync());
            ResetDefaultsCommand = new RelayCommand(async () => await ExecuteResetDefaultsAsync());
            ApplyImodCommand = new RelayCommand(async () => await ExecuteApplyImodAsync());
            ApplyQosCommand = new RelayCommand(async () => await ExecuteApplyQosAsync());

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            Devices.Clear();
            QosProfiles.Clear();

            var devs = await InterruptAffinityService.Instance.GetInterruptDevicesAsync();
            foreach (var d in devs) Devices.Add(d);

            var qos = await QosTrafficService.Instance.GetGameQosProfilesAsync();
            foreach (var q in qos) QosProfiles.Add(q);

            CurrentImodInterval = XhciImodService.Instance.GetCurrentImodInterval();

            StatusMessage = $"Загружено устройств: {Devices.Count} | Логических ядер CPU: {InterruptAffinityService.Instance.LogicalCoreCount}";
            IsBusy = false;
        }

        private async Task ExecuteEsportsPresetAsync()
        {
            IsBusy = true;
            StatusMessage = "Применение киберспортивного пресета изоляции ядер (GPU: Core 2,4 | Net: Core 3 | USB: Core 1)...";
            bool ok = await InterruptAffinityService.Instance.ApplyEsportsAffinityPresetAsync();
            await LoadDataAsync();
            StatusMessage = ok ? "⚡ Киберспортивный профиль прерываний успешно активирован!" : "Ошибка применения некоторых масок прерываний";
            IsBusy = false;
        }

        private async Task ExecuteResetDefaultsAsync()
        {
            IsBusy = true;
            StatusMessage = "Сброс прерываний на системные значения по умолчанию...";
            bool ok = await InterruptAffinityService.Instance.ResetAllDevicesToDefaultAsync();
            await LoadDataAsync();
            StatusMessage = ok ? "Значения прерываний сброшены на стандартные Windows" : "Ошибка сброса";
            IsBusy = false;
        }

        private async Task ExecuteApplyImodAsync()
        {
            IsBusy = true;
            StatusMessage = $"Установка XHCI IMOD интервала: {CurrentImodInterval} µs...";
            bool ok = await XhciImodService.Instance.SetImodIntervalAsync(CurrentImodInterval);
            StatusMessage = ok ? $"✅ USB IMOD интервал установлен на {CurrentImodInterval} мкс (Перезагрузите ПК для полного эффекта)" : "Ошибка записи IMOD";
            IsBusy = false;
        }

        private async Task ExecuteApplyQosAsync()
        {
            IsBusy = true;
            StatusMessage = "Применение политик QoS DSCP 46 для соревновательных игр...";
            bool ok = await QosTrafficService.Instance.ApplyAllGamesQosAsync();
            await LoadDataAsync();
            StatusMessage = ok ? "🚀 Сетевой приоритет DSCP 46 назначен для всех игр!" : "Ошибка настройки политик QoS";
            IsBusy = false;
        }
    }
}
