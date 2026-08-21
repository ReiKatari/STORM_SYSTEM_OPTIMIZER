using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class PowerTuningViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _activeSchemeName = "Определение...";

        [ObservableProperty]
        private bool _isUltimateActive = false;

        [ObservableProperty]
        private bool _isCoreParkingDisabled = true;

        [ObservableProperty]
        private bool _isThrottlingDisabled = true;

        [ObservableProperty]
        private string _statusMessage = "Готов к тюнингу схемы электропитания и ядер CPU";

        public PowerTuningViewModel()
        {
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            ActiveSchemeName = PowerTunerService.Instance.GetActivePowerSchemeName();
            IsUltimateActive = ActiveSchemeName.Contains("STORM", StringComparison.OrdinalIgnoreCase) ||
                               ActiveSchemeName.Contains("Ultimate", StringComparison.OrdinalIgnoreCase);
        }

        [RelayCommand]
        public async Task ActivateStormUltimatePlanAsync()
        {
            StatusMessage = "Применение фирменного плана STORM ULTIMATE PERFORMANCE...";
            bool ok = await PowerTunerService.Instance.ActivateStormUltimatePowerPlanAsync();
            if (ok)
            {
                await PowerTunerService.Instance.ApplyCoreParkingDisableTweaksAsync();
                RefreshStatus();
                StatusMessage = "⚡ Активирован профиль STORM ULTIMATE PERFORMANCE! Парковка ядер отключена.";
                TrayService.Instance.ShowNotification("Электропитание ⚡", "Схема STORM ULTIMATE PERFORMANCE успешно активирована!");
            }
            else
            {
                StatusMessage = "Не удалось применить схему. Требуются права администратора.";
            }
        }

        [RelayCommand]
        public async Task ActivateBalancedPlanAsync()
        {
            StatusMessage = "Переключение на стандартную сбалансированную схему...";
            bool ok = await PowerTunerService.Instance.ActivateBalancedPowerPlanAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Установлена сбалансированная схема Windows.";
            }
        }

        [RelayCommand]
        public async Task ApplyCoreParkingTweaksAsync()
        {
            StatusMessage = "Разблокировка и принудительное отключение Core Parking в реестре...";
            bool ok = await PowerTunerService.Instance.ApplyCoreParkingDisableTweaksAsync();
            if (ok)
            {
                StatusMessage = "100% ядер процессора активны постоянно (Core Parking отключен).";
                TrayService.Instance.ShowNotification("Процессор 🚀", "Парковка ядер отключена, все ядра работают на полной мощности!");
            }
        }
    }
}
