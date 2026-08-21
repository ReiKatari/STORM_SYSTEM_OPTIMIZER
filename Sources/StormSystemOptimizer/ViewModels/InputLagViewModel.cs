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
        private string _statusMessage = "Готов к оптимизации задержки ввода (Input Lag) и прерываний";

        public InputLagViewModel()
        {
            IsMouseSmoothingDisabled = InputLagService.Instance.IsEnhancedPointerPrecisionDisabled();
        }

        [RelayCommand]
        public async Task ApplyUltraLowLatencyInputAsync()
        {
            StatusMessage = "Применение твиков прямого ввода (Raw Input 1:1) и таймеров...";
            bool ok = await InputLagService.Instance.ApplyZeroInputLagTweaksAsync();
            if (ok)
            {
                IsMouseSmoothingDisabled = true;
                StatusMessage = "⚡ Режим Ultra-Low Latency Input активирован! Мышь и клавиатура работают с нулевой задержкой.";
                TrayService.Instance.ShowNotification("Input Lag 🖱️", "Твики прямого 1:1 ввода мыши и приоритета прерываний успешно применены!");
            }
            else
            {
                StatusMessage = "Ошибка применения твиков реестра.";
            }
        }
    }
}
