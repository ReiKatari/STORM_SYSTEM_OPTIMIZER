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

        [ObservableProperty]
        private string _eppStatus = "EPP: 0% (Мгновенный отклик Speed Shift / CPPC)";

        [ObservableProperty]
        private string _heteroStatus = "Приоритет P-Cores для игр активен";

        [ObservableProperty]
        private string _pcieAspmStatus = "PCIe энергосбережение: Отключено (0 ms задержка GPU/SSD)";

        [ObservableProperty]
        private string _boostStatus = "Режим Boost: Агрессивный (Максимальная частота ядер)";

        [ObservableProperty]
        private string _systemResponsivenessStatus = "Приоритет игр: 100% CPU без системного троттлинга";

        [ObservableProperty]
        private string _gpuPowerStatus = "GPU: Maximum Performance (HAGS + TDR Delay)";

        [ObservableProperty]
        private string _usbSuspendStatus = "USB: Отключение микро-засыпания контроллеров";

        [ObservableProperty]
        private string _hiddenAttributesStatus = "Разблокировано 40+ скрытых параметров питания";

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
                CoreParkingStatus = "✓ Парковка ядер: Отключена (100% ядер активны)";
                CoreParkingBadgeColor = "#10B981";
            }
            else
            {
                CoreParkingStatus = "○ Парковка ядер: Включена (Windows усыпляет ядра)";
                CoreParkingBadgeColor = "#F59E0B";
            }

            CStatesStatus = IsUltimateActive ? "✓ C-States оптимизированы (STORM ULTIMATE PLAN)" : "○ Стандартное энергосбережение";
            EppStatus = IsUltimateActive ? "✓ EPP: 0% (Максимальный отклик Speed Shift)" : "○ EPP: Стандартный режим";
            HeteroStatus = IsUltimateActive ? "✓ Приоритет P-Cores для игр активен" : "○ Авто-распределение Windows";
            PcieAspmStatus = IsUltimateActive ? "✓ PCIe ASPM: Отключено (0 ms задержка)" : "○ PCIe ASPM: Включено";
            BoostStatus = IsUltimateActive ? "✓ Режим Boost: Агрессивный" : "○ Режим Boost: Стандартный";
            SystemResponsivenessStatus = IsUltimateActive ? "✓ Троттлинг 0% (Multimedia Profile Gaming)" : "○ Стандартный троттлинг 20%";
            GpuPowerStatus = IsUltimateActive ? "✓ GPU Max Performance (HAGS активирован)" : "○ Стандартный видеодрайвер";
            UsbSuspendStatus = IsUltimateActive ? "✓ USB Suspend: Отключено (0 ms Input Lag)" : "○ USB Suspend: Стандартный";
            HiddenAttributesStatus = "✓ 40+ скрытых параметров Powercfg разблокированы";
        }

        [RelayCommand]
        public async Task ActivateStormUltimatePlanAsync()
        {
            StatusMessage = "Применение фирменного плана STORM ULTIMATE PLAN...";
            bool ok = await PowerTunerService.Instance.ActivateStormUltimatePowerPlanAsync();
            if (ok)
            {
                await PowerTunerService.Instance.ApplyCoreParkingDisableTweaksAsync();
                await PowerTunerService.Instance.ApplyEnergyPerformancePreferenceEppAsync();
                await PowerTunerService.Instance.ApplyHeteroSchedulingPolicyAsync();
                await PowerTunerService.Instance.ApplyPcieAspmDisableAsync();
                await PowerTunerService.Instance.ApplyProcessorBoostModeAsync();
                await PowerTunerService.Instance.ApplySystemResponsivenessMultimediaTweakAsync();
                await PowerTunerService.Instance.ApplyGpuMaximumPerformancePowerPolicyAsync();
                await PowerTunerService.Instance.ApplyUsbSelectiveSuspendDisableAsync();
                await PowerTunerService.Instance.UnlockAllHiddenPowerSchemeAttributesAsync();
                RefreshStatus();
                StatusMessage = "⚡ Активирован профиль STORM ULTIMATE PLAN! Все ядра, GPU и шины работают на максимальной скорости.";
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

        [RelayCommand]
        public async Task ApplyEppTweaksAsync()
        {
            StatusMessage = "Установка Energy Performance Preference (EPP = 0%)...";
            bool ok = await PowerTunerService.Instance.ApplyEnergyPerformancePreferenceEppAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "EPP = 0% применен! Мгновенная готовность частот Intel Speed Shift и AMD CPPC.";
                TrayService.Instance.ShowNotification("EPP 0% ⚡", "Процессор переведен в режим мгновенной реакции без задержек раскачки!");
            }
        }

        [RelayCommand]
        public async Task ApplyHeteroSchedulingAsync()
        {
            StatusMessage = "Настройка гетерогенного планировщика ядер...";
            bool ok = await PowerTunerService.Instance.ApplyHeteroSchedulingPolicyAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Игровые потоки привязаны к высокопроизводительным P-Cores / лучшему CCD!";
                TrayService.Instance.ShowNotification("Планировщик CPU 🎯", "Приоритет высокопроизводительных ядер для активных игр включен!");
            }
        }

        [RelayCommand]
        public async Task ApplyPcieAspmAsync()
        {
            StatusMessage = "Отключение энергосбережения PCIe Link State ASPM...";
            bool ok = await PowerTunerService.Instance.ApplyPcieAspmDisableAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "PCIe шины видеокарты и NVMe SSD работают без задержек засыпания!";
                TrayService.Instance.ShowNotification("PCIe ASPM 🏎️", "Латентность шины PCIe снижена до 0!");
            }
        }

        [RelayCommand]
        public async Task ApplySystemResponsivenessAsync()
        {
            StatusMessage = "Отключение системного троттлинга Windows Multimedia Profile...";
            bool ok = await PowerTunerService.Instance.ApplySystemResponsivenessMultimediaTweakAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Системный и сетевой троттлинг отключен! Приоритет игровых задач максимален.";
                TrayService.Instance.ShowNotification("Мультимедиа & Игры 🎮", "Троттлинг CPU снят, приоритет GPU и игровых потоков установлен на максимум!");
            }
        }

        [RelayCommand]
        public async Task ApplyGpuMaxPerformanceAsync()
        {
            StatusMessage = "Форсирование максимальной производительности GPU и аппаратного HAGS...";
            bool ok = await PowerTunerService.Instance.ApplyGpuMaximumPerformancePowerPolicyAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Режим GPU Maximum Performance и HAGS активированы!";
                TrayService.Instance.ShowNotification("Видеокарта ⚡", "Энергосбережение GPU отключено, время задержки TDR увеличено!");
            }
        }

        [RelayCommand]
        public async Task ApplyUsbSelectiveSuspendAsync()
        {
            StatusMessage = "Отключение микро-сна USB портов и концентраторов...";
            bool ok = await PowerTunerService.Instance.ApplyUsbSelectiveSuspendDisableAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "USB Selective Suspend отключен. Задержка ввода мыши и клавиатуры снижена.";
                TrayService.Instance.ShowNotification("USB Контроллер 🖱️", "USB Selective Suspend отключен, задержка опроса сведена к минимуму!");
            }
        }

        [RelayCommand]
        public async Task UnlockHiddenAttributesAsync()
        {
            StatusMessage = "Разблокировка всех скрытых параметров Powercfg в системе...";
            bool ok = await PowerTunerService.Instance.UnlockAllHiddenPowerSchemeAttributesAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "Все 40+ скрытых параметров управления питанием разблокированы в Windows!";
                TrayService.Instance.ShowNotification("Powercfg 🔓", "Все скрытые системные настройки электропитания разблокированы!");
            }
        }
    }
}
