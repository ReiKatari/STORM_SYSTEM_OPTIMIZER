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
        private string _statusMessage = "Готов к оптимизации задержки ввода мыши и клавиатуры";

        public InputLagViewModel()
        {
            IsMouseSmoothingDisabled = InputLagService.Instance.IsEnhancedPointerPrecisionDisabled();
            if (IsMouseSmoothingDisabled)
            {
                StatusMessage = "Прямой ввод 1:1 активен (скрытая акселерация Windows отключена).";
            }
        }

        [RelayCommand]
        public async Task ApplyUltraLowLatencyInputAsync()
        {
            StatusMessage = "Применение твиков прямого ввода (Raw Input 1:1) и таймеров...";
            var (ok, msg) = await InputLagService.Instance.ApplyZeroInputLagTweaksAsync();
            if (ok)
            {
                IsMouseSmoothingDisabled = true;
                StatusMessage = "⚡ Режим минимальной задержки ввода активирован! Мышь и клавиатура работают с точностью 1:1.";
                TrayService.Instance.ShowNotification("Задержка ввода 🖱️", "Твики прямого ввода 1:1 и приоритета отклика успешно применены!");
            }
            else
            {
                StatusMessage = msg;
            }
        }
    }
}
