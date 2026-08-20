using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class WindowsServicesService
    {
        private static WindowsServicesService? _instance;
        public static WindowsServicesService Instance => _instance ??= new WindowsServicesService();

        private readonly List<(string Name, string Display, string Desc, bool Gaming, bool Extreme)> _knownServices = new()
        {
            ("DiagTrack", "Connected User Experiences and Telemetry", "Служба сбора и отправки телеметрии в Microsoft.", true, true),
            ("dmwappushservice", "WAP Push Service Routing", "Служба маршрутизации сообщений push и телеметрии.", true, true),
            ("RemoteRegistry", "Дистанционный реестр", "Позволяет удаленным пользователям изменять настройки реестра.", true, true),
            ("WerSvc", "Служба регистрации ошибок Windows", "Формирует отчеты об ошибках и дампы сбоев программ.", false, true),
            ("RetailDemo", "Служба демонстрационного режима", "Используется магазинами электроники для демонстрации Windows.", true, true),
            ("MapsBroker", "Диспетчер скачанных карт", "Фоновая служба офлайн-карт Windows Maps.", true, true),
            ("XblAuthManager", "Диспетчер проверки подлинности Xbox Live", "Требуется только для игр Microsoft Store / Xbox Live.", false, true),
            ("XblGameSave", "Служба сохранения игр на Xbox Live", "Синхронизация сохранений для игр Xbox Store.", false, true),
            ("XboxNetApiSvc", "Сетевая служба Xbox Live", "Сетевые подключения приложений Xbox.", false, true),
            ("WbioSrvc", "Биометрическая служба Windows", "Требуется только при использовании сканера отпечатков или Face ID.", false, true)
        };

        private WindowsServicesService() { }

        public List<ServiceEntry> GetUnnecessaryServices()
        {
            var result = new List<ServiceEntry>();

            foreach (var item in _knownServices)
            {
                try
                {
                    using var sc = new ServiceController(item.Name);
                    string status = sc.Status == ServiceControllerStatus.Running ? "Работает" : "Остановлена";
                    string startType = GetServiceStartupType(item.Name);

                    result.Add(new ServiceEntry
                    {
                        ServiceName = item.Name,
                        DisplayName = item.Display,
                        Description = item.Desc,
                        Status = status,
                        StartupType = startType,
                        IsSafeToDisable = true,
                        IsOptimized = startType == "Disabled" || sc.Status != ServiceControllerStatus.Running
                    });
                }
                catch
                {
                    // Service not found on this edition of Windows, skip
                }
            }

            return result;
        }

        public bool SetServiceState(string serviceName, bool disable)
        {
            try
            {
                string startMode = disable ? "disabled" : "demand";
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{serviceName}\" start= {startMode}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);

                if (disable)
                {
                    var stopPsi = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"stop \"{serviceName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var stopP = Process.Start(stopPsi);
                    stopP?.WaitForExit(3000);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ApplyProfile(string profileName)
        {
            foreach (var s in _knownServices)
            {
                bool shouldDisable = profileName switch
                {
                    "Extreme" => s.Extreme,
                    "Gaming" => s.Gaming,
                    "Balanced" => s.Name == "DiagTrack" || s.Name == "dmwappushservice" || s.Name == "RemoteRegistry" || s.Name == "RetailDemo",
                    "Default" => false,
                    _ => false
                };

                SetServiceState(s.Name, shouldDisable);
            }
        }

        private string GetServiceStartupType(string serviceName)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                if (key != null)
                {
                    object? start = key.GetValue("Start");
                    if (start != null)
                    {
                        return Convert.ToInt32(start) switch
                        {
                            2 => "Автоматически",
                            3 => "Вручную",
                            4 => "Отключено",
                            _ => "Вручную"
                        };
                    }
                }
            }
            catch { }
            return "Неизвестно";
        }
    }
}
