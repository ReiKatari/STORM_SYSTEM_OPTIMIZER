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
        private string _coreParkingStatus = "Парковка ядер: ОТКЛЮЧЕНА (100% ядер активны)";

        [ObservableProperty]
        private string _coreParkingBadgeColor = "#10B981";

        [ObservableProperty]
        private string _cStatesStatus = "Оптимизированы (Ультра-низкая латентность CPU)";

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

            bool isUnparked = PowerTunerService.Instance.IsCoreParkingDisabled();
            if (isUnparked)
            {
                CoreParkingStatus = "Парковка ядер: ОТКЛЮЧЕНА (100% ядер активны)";
                CoreParkingBadgeColor = "#10B981";
            }
            else
            {
                CoreParkingStatus = "Парковка ядер: ВКЛЮЧЕНА (Windows усыпляет ядра)";
                CoreParkingBadgeColor = "#F59E0B";
            }

            CStatesStatus = IsUltimateActive ? "C-States отключены (STORM ULTIMATE PLAN)" : "Стандартное энергосбережение";
        }

        [RelayCommand]
        public async Task ActivateStormUltimatePlanAsync()
        {
            StatusMessage = "Применение фирменного плана STORM ULTIMATE PLAN...";
            bool ok = await PowerTunerService.Instance.ActivateStormUltimatePowerPlanAsync();
            if (ok)
            {
                await PowerTunerService.Instance.ApplyCoreParkingDisableTweaksAsync();
                RefreshStatus();
                StatusMessage = "⚡ Активирован профиль STORM ULTIMATE PLAN! Парковка ядер отключена.";
                TrayService.Instance.ShowNotification("Электропитание ⚡", "Схема STORM ULTIMATE PLAN успешно активирована!");
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
                RefreshStatus();
                StatusMessage = "100% ядер процессора активны постоянно (Core Parking отключен).";
                TrayService.Instance.ShowNotification("Процессор 🚀", "Парковка ядер отключена, все ядра работают на полной мощности!");
            }
        }

        [RelayCommand]
        public async Task ApplyCStatesTweaksAsync()
        {
            StatusMessage = "Отключение задержек переходов C-States...";
            bool ok = await PowerTunerService.Instance.ApplyCStatesTweaksAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Задержки C-States устранены! Микро-фризы при пробуждении CPU ликвидированы.";
                TrayService.Instance.ShowNotification("C-States Тюнинг", "Задержки энергосбережения процессора отключены!");
            }
        }
    }
}
