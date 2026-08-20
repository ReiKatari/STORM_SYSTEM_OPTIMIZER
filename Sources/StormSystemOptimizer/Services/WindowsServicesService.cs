using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class WindowsServicesService
    {
        private static WindowsServicesService? _instance;
        public static WindowsServicesService Instance => _instance ??= new WindowsServicesService();

        private readonly List<(string Name, string Display, string Desc, bool Balanced, bool Gaming, bool Extreme)> _knownServices = new()
        {
            ("DiagTrack", "Connected User Experiences and Telemetry", "Служба сбора и отправки диагностических данных телеметрии в Microsoft.", true, true, true),
            ("dmwappushservice", "WAP Push Service Routing", "Маршрутизация сообщений push и сбор телеметрических логов.", true, true, true),
            ("RemoteRegistry", "Дистанционный реестр", "Позволяет удаленным пользователям изменять настройки реестра на данном ПК.", true, true, true),
            ("RetailDemo", "Служба демонстрационного режима", "Демо-режим для магазинов электроники. Абсолютно бесполезна на личном ПК.", true, true, true),
            ("MapsBroker", "Диспетчер скачанных карт", "Фоновая синхронизация офлайн-карт приложения Windows Maps.", true, true, true),
            ("WerSvc", "Служба регистрации ошибок Windows", "Формирует объемные дампы сбоев и отправляет отчеты об ошибках.", false, true, true),
            ("XblAuthManager", "Диспетчер проверки подлинности Xbox Live", "Авторизация Xbox Live (не требуется, если вы играете в Steam/Epic).", false, false, true),
            ("XblGameSave", "Служба сохранения игр на Xbox Live", "Облачные сохранения Xbox (не нужны для сторонних лаунчеров).", false, false, true),
            ("XboxNetApiSvc", "Сетевая служба Xbox Live", "Сетевые подключения приложений Xbox.", false, false, true),
            ("XboxGipSvc", "Служба поддержки периферийных устройств Xbox", "Поддержка геймпадов и аксессуаров Xbox через Xbox Accessories.", false, false, true),
            ("WbioSrvc", "Биометрическая служба Windows", "Требуется только при сканировании отпечатков пальцев или Face ID.", false, false, true),
            ("wisvc", "Служба программы предварительной оценки Windows", "Фоновые проверки сборок Windows Insider Preview.", true, true, true),
            ("Fax", "Служба факса", "Служба отправки и приема факсов по телефонным линиям.", true, true, true),
            ("PhoneSvc", "Служба телефонной связи", "Управляет состоянием телефонии на ПК.", true, true, true),
            ("SensorService", "Служба датчиков", "Управляет датчиками освещенности и ориентации (на стационарном ПК не нужна).", false, true, true),
            ("TroubleshootingSvc", "Служба рекомендаций по устранению неполадок", "Фоновый запуск средств устранения неполадок.", false, true, true),
            ("icssvc", "Служба мобильной точки доступа Windows", "Раздача интернета по Wi-Fi (Mobile Hotspot).", false, false, true)
        };

        private WindowsServicesService() { }

        public List<ServiceEntry> GetUnnecessaryServices()
        {
            var result = new List<ServiceEntry>();

            foreach (var item in _knownServices)
            {
                try
                {
                    string status = "Остановлена";
                    string startType = "Вручную";
                    bool isOptimized = false;

                    // Check Registry First (Fastest & Safest)
                    int regStart = GetRegistryStartValue(item.Name);
                    if (regStart == -1)
                    {
                        // Service key does not exist on this OS
                        continue;
                    }

                    startType = regStart switch
                    {
                        2 => "Автоматически",
                        3 => "Вручную",
                        4 => "Отключено",
                        _ => "Вручную"
                    };

                    // Check Live Controller
                    try
                    {
                        using var sc = new ServiceController(item.Name);
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            status = "Работает";
                        }
                        else if (regStart == 4)
                        {
                            status = "Отключено";
                        }
                        else
                        {
                            status = "Остановлена";
                        }
                    }
                    catch
                    {
                        status = regStart == 4 ? "Отключено" : "Остановлена";
                    }

                    isOptimized = (regStart == 4 || regStart == 3);

                    result.Add(new ServiceEntry
                    {
                        ServiceName = item.Name,
                        DisplayName = item.Display,
                        Description = item.Desc,
                        Status = status,
                        StartupType = startType,
                        IsSafeToDisable = true,
                        IsOptimized = isOptimized
                    });
                }
                catch
                {
                    // Safe bypass for individual service inspection
                }
            }

            return result;
        }

        public bool SetServiceState(string serviceName, bool disable)
        {
            try
            {
                int startVal = disable ? 4 : 3; // 4 = Disabled, 3 = Manual (Demand)
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true))
                {
                    key?.SetValue("Start", startVal, RegistryValueKind.DWord);
                }

                if (disable)
                {
                    try
                    {
                        using var sc = new ServiceController(serviceName);
                        if (sc.Status == ServiceControllerStatus.Running && sc.CanStop)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(2));
                        }
                    }
                    catch { }
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
                    "Balanced" => s.Balanced,
                    "Default" => false,
                    _ => s.Balanced
                };

                SetServiceState(s.Name, shouldDisable);
            }
        }

        private int GetRegistryStartValue(string serviceName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                if (key != null)
                {
                    object? val = key.GetValue("Start");
                    if (val != null)
                    {
                        return Convert.ToInt32(val);
                    }
                }
            }
            catch { }
            return -1;
        }
    }
}
