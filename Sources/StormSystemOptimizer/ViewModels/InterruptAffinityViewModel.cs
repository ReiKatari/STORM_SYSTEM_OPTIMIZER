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

        [ObservableProperty]
        private string _totalDevicesCount = "0";

        [ObservableProperty]
        private string _totalMsiEnabledCount = "0";

        public ObservableCollection<PciDeviceInterruptInfo> Devices { get; } = new();
        public ObservableCollection<GameQosProfile> QosProfiles { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand ApplyEsportsPresetCommand { get; }
        public ICommand ApplyBalancedMsiPresetCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand ApplyImodCommand { get; }
        public ICommand ApplyQosCommand { get; }
        public ICommand ToggleDeviceMsiCommand { get; }

        public InterruptAffinityViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ApplyEsportsPresetCommand = new RelayCommand(async () => await ExecuteEsportsPresetAsync());
            ApplyBalancedMsiPresetCommand = new RelayCommand(async () => await ExecuteBalancedMsiPresetAsync());
            ResetDefaultsCommand = new RelayCommand(async () => await ExecuteResetDefaultsAsync());
            ApplyImodCommand = new RelayCommand(async () => await ExecuteApplyImodAsync());
            ApplyQosCommand = new RelayCommand(async () => await ExecuteApplyQosAsync());
            ToggleDeviceMsiCommand = new RelayCommand<PciDeviceInterruptInfo>(async dev =>
            {
                if (dev != null) await ExecuteToggleMsiAsync(dev);
            });

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var devs = await InterruptAffinityService.Instance.GetInterruptDevicesAsync();
            var qos = await QosTrafficService.Instance.GetGameQosProfilesAsync();
            int imod = XhciImodService.Instance.GetCurrentImodInterval();

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Devices.Clear();
                int msiCount = 0;
                foreach (var d in devs)
                {
                    Devices.Add(d);
                    if (d.MsiSupported && d.MsiEnabled) msiCount++;
                }

                QosProfiles.Clear();
                foreach (var q in qos) QosProfiles.Add(q);

                CurrentImodInterval = imod;
                TotalDevicesCount = Devices.Count.ToString();
                TotalMsiEnabledCount = msiCount.ToString();
                StatusMessage = $"Загружено устройств: {Devices.Count} (MSI активно: {msiCount}) | CPU: {InterruptAffinityService.Instance.LogicalCoreCount} логических ядер";
                IsBusy = false;
            });
        }

        private async Task ExecuteToggleMsiAsync(PciDeviceInterruptInfo dev)
        {
            IsBusy = true;
            bool newState = !dev.MsiEnabled;
            StatusMessage = $"Переключение MSI для {dev.Name}...";
            bool ok = await InterruptAffinityService.Instance.SetDeviceMsiStateAsync(dev, newState);
            await LoadDataAsync();
            StatusMessage = ok ? $"Режим MSI для {dev.Name} успешно изменен!" : "Ошибка изменения режима MSI";
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

        private async Task ExecuteBalancedMsiPresetAsync()
        {
            IsBusy = true;
            StatusMessage = "Активация сбалансированного MSI-режима для всех поддерживаемых PCI устройств...";
            bool ok = true;
            foreach (var d in Devices)
            {
                if (d.MsiSupported && !d.MsiEnabled)
                {
                    ok &= await InterruptAffinityService.Instance.SetDeviceMsiStateAsync(d, true);
                }
            }
            await LoadDataAsync();
            StatusMessage = ok ? "✅ Сбалансированный MSI режим включен для всех контроллеров!" : "Частично применен MSI";
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
            StatusMessage = $"Установка XHCI IMOD интервала: {CurrentImodInterval} мкс...";
            bool ok = await XhciImodService.Instance.SetImodIntervalAsync(CurrentImodInterval);
            StatusMessage = ok ? $"✅ USB IMOD интервал установлен на {CurrentImodInterval} мкс (Перезагрузите ПК для эффекта)" : "Ошибка записи IMOD";
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
