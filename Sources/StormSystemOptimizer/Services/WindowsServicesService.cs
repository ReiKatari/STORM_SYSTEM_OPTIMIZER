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
            ("icssvc", "Служба мобильной точки доступа Windows", "Раздача интернета по Wi-Fi (Mobile Hotspot).", false, false, true),
            ("SysMain", "SysMain (SuperFetch)", "Предварительная загрузка приложений в оперативную память. На быстрых NVMe SSD только создает фоновую нагрузку.", false, true, true),
            ("WSearch", "Windows Search", "Служба индексирования файлов для поиска. Нагружает диск постоянным фоновым сканированием.", false, true, true),
            ("PcaSvc", "Служба помощника по совместимости программ", "Отслеживает запуск старых программ. Замедляет запуск игр и лаунчеров.", false, true, true),
            ("DPS", "Служба политики диагностики", "Позволяет обнаруживать и устранять проблемы компонентов Windows.", false, true, true),
            ("WdiServiceHost", "Узел службы диагностики", "Сбор диагностических журналов для поиска неполадок.", true, true, true),
            ("WdiSystemHost", "Узел системы диагностики", "Сбор телеметрии о производительности системы.", true, true, true),
            ("PrintNotify", "Служба уведомлений о печати", "Уведомления локальных и сетевых принтеров.", true, true, true),
            ("Spooler", "Диспетчер печати", "Очередь печати документов. Если у вас нет принтера, служба не требуется.", false, false, true),
            ("BluetoothUserService", "Служба поддержки пользователей Bluetooth", "Работает с профилями Bluetooth-устройств.", false, false, true),
            ("bthserv", "Служба поддержки Bluetooth", "Управляет обнаружением и настройкой удаленных устройств Bluetooth.", false, false, true)
        };

        private WindowsServicesService() { }

        public List<ServiceEntry> GetUnnecessaryServices()
        {
            var result = new List<ServiceEntry>();

            foreach (var item in _knownServices)
            {
                int regStart = GetRegistryStartValue(item.Name);
                bool isOptimized = (regStart == 4);

                string startType = regStart switch
                {
                    2 => "Автоматически",
                    3 => "Вручную",
                    4 => "Отключено",
                    _ => "Вручную"
                };

                string status = isOptimized ? "Отключена" : (regStart == 2 ? "Работает" : "Остановлена");

                result.Add(new ServiceEntry
                {
                    ServiceName = item.Name,
                    DisplayName = item.Display,
                    Description = item.Desc,
                    Status = status,
                    StartupType = startType,
                    IsOptimized = isOptimized,
                    RecommendedAction = "Отключить для прироста FPS"
                });
            }

            return result;
        }

        public bool ShouldDisableInPreset(string serviceName, string presetName)
        {
            var known = _knownServices.Find(s => s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(known.Name)) return false;

            return presetName.ToLowerInvariant() switch
            {
                "gaming" => known.Gaming,
                "extreme" => known.Extreme,
                "balanced" or "safe" => known.Balanced,
                "default" => false,
                _ => known.Balanced
            };
        }

        public int GetRegistryStartValue(string serviceName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                if (key != null)
                {
                    object? val = key.GetValue("Start");
                    if (val is int intVal) return intVal;
                }
            }
            catch { }
            return -1;
        }

        public bool SetServiceState(string serviceName, bool disable)
        {
            try
            {
                int newStart = disable ? 4 : 3; // 4 = Disabled, 3 = Manual

                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Start", newStart, RegistryValueKind.DWord);
                    }
                }

                if (disable)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "net.exe",
                            Arguments = $"stop {serviceName} /y",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(500);
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

        public void ApplyProfile(string profileName) => ApplyPreset(profileName);

        public void ApplyPreset(string presetName)
        {
            foreach (var svc in _knownServices)
            {
                bool shouldDisable = presetName.ToLowerInvariant() switch
                {
                    "gaming" => svc.Gaming,
                    "extreme" => svc.Extreme,
                    "balanced" or "safe" => svc.Balanced,
                    "default" => false,
                    _ => svc.Balanced
                };

                SetServiceState(svc.Name, shouldDisable);
            }
        }
    }
}
