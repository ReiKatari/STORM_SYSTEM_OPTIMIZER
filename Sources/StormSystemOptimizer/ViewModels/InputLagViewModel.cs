using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class InputLagViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRawInputActive = true;

        [ObservableProperty]
        private bool _isMouseSmoothingDisabled = true;

        [ObservableProperty]
        private bool _isMultimediaPriorityHigh = true;

        [ObservableProperty]
        private string _mouseStatus = "✓ 1:1 Raw Input активен (Акселерация отключена)";

        [ObservableProperty]
        private string _mouseBadgeColor = "#10B981";

        [ObservableProperty]
        private string _keyboardStatus = "✓ Задержка 0, Скорость 31 (Мгновенный отклик)";

        [ObservableProperty]
        private string _keyboardBadgeColor = "#10B981";

        [ObservableProperty]
        private string _systemResponsivenessStatus = "✓ 100% CPU приоритет для игр (SystemResponsiveness = 0)";

        [ObservableProperty]
        private string _responsivenessBadgeColor = "#10B981";

        [ObservableProperty]
        private string _usbStatus = "✓ USB Low Latency активен (Selective Suspend = Off)";

        [ObservableProperty]
        private string _usbBadgeColor = "#10B981";

        [ObservableProperty]
        private string _statusMessage = "✓ Режим минимальной задержки ввода (1:1 Raw Input) активен!";

        public InputLagViewModel()
        {
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            bool mouseTweak = InputLagService.Instance.IsEnhancedPointerPrecisionDisabled();
            IsMouseSmoothingDisabled = mouseTweak;
            if (mouseTweak)
            {
                MouseStatus = "✓ Применено (1:1 Raw Input активен)";
                MouseBadgeColor = "#10B981";
            }
            else
            {
                MouseStatus = "○ Стандартный режим (Акселерация Windows)";
                MouseBadgeColor = "#F59E0B";
            }

            bool keybTweak = InputLagService.Instance.IsKeyboardTweakApplied();
            if (keybTweak)
            {
                KeyboardStatus = "✓ Применено (Задержка 0, Скорость 31)";
                KeyboardBadgeColor = "#10B981";
            }
            else
            {
                KeyboardStatus = "○ Стандартная задержка повтора";
                KeyboardBadgeColor = "#F59E0B";
            }

            bool respTweak = InputLagService.Instance.IsSystemResponsivenessApplied();
            if (respTweak)
            {
                SystemResponsivenessStatus = "✓ Применено (100% CPU для игр и ввода)";
                ResponsivenessBadgeColor = "#10B981";
            }
            else
            {
                SystemResponsivenessStatus = "○ Стандартный троттлинг Windows (20%)";
                ResponsivenessBadgeColor = "#F59E0B";
            }

            bool usbTweak = InputLagService.Instance.IsUsbPowerSavingDisabled();
            if (usbTweak)
            {
                UsbStatus = "✓ Применено (USB энергосбережение выключено)";
                UsbBadgeColor = "#10B981";
            }
            else
            {
                UsbStatus = "○ USB Selective Suspend включен";
                UsbBadgeColor = "#F59E0B";
            }

            if (mouseTweak && keybTweak && respTweak)
            {
                StatusMessage = "✓ Все твики минимальной задержки ввода (Raw Input 1:1) применены и активны!";
            }
            else
            {
                StatusMessage = "Готов к оптимизации задержки ввода мыши, клавиатуры и USB-портов.";
            }
        }

        [RelayCommand]
        public async Task ApplyUltraLowLatencyInputAsync()
        {
            StatusMessage = "Применение твиков прямого ввода (Raw Input 1:1) и таймеров...";
            var (ok, msg) = await InputLagService.Instance.ApplyZeroInputLagTweaksAsync();
            if (ok)
            {
                RefreshStatus();
                StatusMessage = "✓ Применено! Режим минимальной задержки ввода активирован (1:1 Raw Input, 0ms задержки).";
                TrayService.Instance.ShowNotification("Задержка ввода 🖱️", "Твики прямого ввода 1:1 и приоритета отклика успешно применены!");
            }
            else
            {
                StatusMessage = msg;
            }
        }

        [RelayCommand]
        public async Task ApplySmoothnessTuningAsync()
        {
            StatusMessage = "Тюнинг плавности интерфейса: DWM высокий приоритет, распаковка ядер, 0x26 квантование...";
            var (ok, msg) = await InputLagService.Instance.ApplySmoothnessAndTimerResolutionTweakAsync();
            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Плавность интерфейса ✨", "DWM приоритет повышен, парковка ядер отключена, ядро зафиксировано в RAM!");
        }

        [RelayCommand]
        public async Task PurgeShaderCachesAsync()
        {
            StatusMessage = "Очистка кэша скомпилированных шейдеров Direct3D (DirectX 11/12, Vulkan, NVIDIA, AMD)...";
            var (count, bytes) = await InputLagService.Instance.PurgeDirect3DShaderCachesAsync();
            string sizeStr = FormatHelper.FormatSize(bytes);
            StatusMessage = $"✓ Очищено {count} файлов кэша шейдеров ({sizeStr})! Фризы и статтеры в играх ликвидированы.";
            TrayService.Instance.ShowNotification("Очистка шейдеров Direct3D 🎮", $"Очищено {count} кэшированных файлов ({sizeStr}). Шейдеры будут пересобраны начисто.");
        }
    }
}
