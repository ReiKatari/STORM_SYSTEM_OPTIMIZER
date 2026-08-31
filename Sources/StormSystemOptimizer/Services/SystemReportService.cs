using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class SystemReportService
    {
        private static SystemReportService? _instance;
        public static SystemReportService Instance => _instance ??= new SystemReportService();

        private SystemReportService() { }

        public async Task<string> GenerateHtmlReportAsync()
        {
            return await Task.Run(async () =>
            {
                string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"STORM_System_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");

                var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
                var sensors = await HardwareTemperatureService.Instance.GetAllTemperaturesAsync();
                var drives = await DiskInfoService.Instance.GetAllDrivesInfoAsync();

                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html lang='ru'>");
                sb.AppendLine("<head>");
                sb.AppendLine("  <meta charset='UTF-8'>");
                sb.AppendLine("  <title>STORM SYSTEM OPTIMIZER — Диагностический отчет системы</title>");
                sb.AppendLine("  <style>");
                sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #0d1117; color: #e6edf3; margin: 0; padding: 30px; }");
                sb.AppendLine("    .container { max-width: 960px; margin: 0 auto; background-color: #161b22; border-radius: 12px; border: 1px solid #30363d; padding: 32px; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }");
                sb.AppendLine("    .header { display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid #30363d; padding-bottom: 20px; margin-bottom: 24px; }");
                sb.AppendLine("    .title { font-size: 24px; font-weight: bold; color: #00D2FF; margin: 0; }");
                sb.AppendLine("    .subtitle { color: #8b949e; font-size: 13px; margin-top: 4px; }");
                sb.AppendLine("    .badge { background: rgba(0, 210, 255, 0.15); color: #00D2FF; padding: 4px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; border: 1px solid rgba(0, 210, 255, 0.3); }");
                sb.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; margin-bottom: 24px; }");
                sb.AppendLine("    .card { background-color: #0d1117; border: 1px solid #30363d; border-radius: 8px; padding: 16px; }");
                sb.AppendLine("    .card-title { font-size: 12px; font-weight: 600; text-transform: uppercase; color: #8b949e; margin-bottom: 8px; }");
                sb.AppendLine("    .card-val { font-size: 18px; font-weight: bold; color: #58a6ff; }");
                sb.AppendLine("    h2 { font-size: 16px; border-left: 4px solid #00D2FF; padding-left: 10px; margin-top: 28px; margin-bottom: 16px; color: #f0f6fc; }");
                sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 13px; }");
                sb.AppendLine("    th, td { text-align: left; padding: 10px; border-bottom: 1px solid #21262d; }");
                sb.AppendLine("    th { background-color: #0d1117; color: #8b949e; }");
                sb.AppendLine("    .status-ok { color: #3fb950; font-weight: bold; }");
                sb.AppendLine("    .footer { margin-top: 32px; padding-top: 16px; border-top: 1px solid #21262d; text-align: center; color: #8b949e; font-size: 12px; }");
                sb.AppendLine("  </style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("  <div class='container'>");
                sb.AppendLine("    <div class='header'>");
                sb.AppendLine("      <div>");
                sb.AppendLine("        <div class='title'>⚡ STORM SYSTEM OPTIMIZER</div>");
                sb.AppendLine($"        <div class='subtitle'>Официальный отчет диагностики и производительности • {DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <div class='badge'>STORM Engine 2.0.5</div>");
                sb.AppendLine("    </div>");

                // Summary Cards
                sb.AppendLine("    <div class='grid'>");
                sb.AppendLine("      <div class='card'>");
                sb.AppendLine("        <div class='card-title'>Операционная система</div>");
                sb.AppendLine($"        <div class='card-val'>{Environment.OSVersion.VersionString} (64-bit)</div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <div class='card'>");
                sb.AppendLine("        <div class='card-title'>Процессор и Потоки</div>");
                sb.AppendLine($"        <div class='card-val'>{HardwareTemperatureService.Instance.GetProcessorName()} ({Environment.ProcessorCount} потоков)</div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <div class='card'>");
                sb.AppendLine("        <div class='card-title'>Видеокарта</div>");
                sb.AppendLine($"        <div class='card-val'>{HardwareTemperatureService.Instance.GetGpuName()}</div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <div class='card'>");
                sb.AppendLine("        <div class='card-title'>Оперативная память</div>");
                sb.AppendLine($"        <div class='card-val'>{metrics.RamUsagePercentage:F0}% загружено ({metrics.RamAvailableGb:F1} ГБ свободно)</div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");

                // Sensors Table
                sb.AppendLine("    <h2>🌡️ Аппаратные датчики и температуры</h2>");
                sb.AppendLine("    <table>");
                sb.AppendLine("      <tr><th>Компонент</th><th>Устройство</th><th>Датчик</th><th>Температура</th><th>Статус</th></tr>");
                foreach (var s in sensors)
                {
                    sb.AppendLine($"      <tr><td><b>{s.DeviceType}</b></td><td>{s.DeviceName}</td><td>{s.SensorDetail}</td><td><b>{s.TemperatureText}</b></td><td class='status-ok'>{s.StatusLabel}</td></tr>");
                }
                sb.AppendLine("    </table>");

                // Drives Table
                sb.AppendLine("    <h2>💾 Накопители и разделы</h2>");
                sb.AppendLine("    <table>");
                sb.AppendLine("      <tr><th>Диск</th><th>Модель</th><th>Тип / ФС</th><th>Объем</th><th>Свободно</th><th>Состояние</th></tr>");
                foreach (var d in drives)
                {
                    sb.AppendLine($"      <tr><td><b>{d.VolumeLetter}</b></td><td>{d.Model}</td><td>{d.DriveType} • {d.FileSystem}</td><td>{d.TotalSizeText}</td><td>{d.FreeSpaceText}</td><td class='status-ok'>{d.HealthStatus}</td></tr>");
                }
                sb.AppendLine("    </table>");

                sb.AppendLine("    <div class='footer'>");
                sb.AppendLine($"      Сгенерировано автоматически приложением STORM SYSTEM OPTIMIZER • Компьютер: {Environment.MachineName} • Пользователь: {Environment.UserName}");
                sb.AppendLine("    </div>");
                sb.AppendLine("  </div>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);

                // Open report in browser
                try
                {
                    Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
                }
                catch { }

                return reportPath;
            });
        }
    }
}
