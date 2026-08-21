using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class ProcessManagerService
    {
        private static ProcessManagerService? _instance;
        public static ProcessManagerService Instance => _instance ??= new ProcessManagerService();

        // Knowledge Base of Known Processes
        private static readonly Dictionary<string, (string Desc, string Pub, ProcessSafetyStatus Status, string Rec)> _knowledgeBase = new(StringComparer.OrdinalIgnoreCase)
        {
            // Windows Critical System Processes
            { "System", ("Ядро операционной системы Windows NT", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Критический системный процесс ядра. Не завершать.") },
            { "Registry", ("Системный процесс управления реестром Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Критический системный компонент.") },
            { "smss.exe", ("Диспетчер сеансов Windows NT (Session Manager)", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Критический процесс инициализации сеансов.") },
            { "csrss.exe", ("Исполняющий процесс клиент-серверной подсистемы Win32", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Критический процесс Win32. Завершение вызовет BSOD.") },
            { "wininit.exe", ("Процесс инициализации Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Запускает системные службы и LSA.") },
            { "services.exe", ("Диспетчер управления службами Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Управляет всеми фоновыми службами Windows.") },
            { "lsass.exe", ("Сервер проверки подлинности локальной безопасности LSA", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обрабатывает вход в систему и безопасность.") },
            { "winlogon.exe", ("Процесс входа в систему Windows Logon", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обеспечивает вход и блокировку сеанса.") },
            { "dwm.exe", ("Диспетчер окон рабочего стола (Desktop Window Manager)", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Отвечает за отрисовку и аппаратное ускорение окон.") },
            { "explorer.exe", ("Проводник Windows (Рабочий стол и панель задач)", "Microsoft Windows", ProcessSafetyStatus.UserApp, "Перезапуск проводника может устранить визуальные лаги.") },
            { "svchost.exe", ("Хост-процесс для системных служб Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Хостинг системных служб. Завершение отдельных служб выполнять в разделе «Службы».") },
            { "fontdrvhost.exe", ("Хост драйвера шрифтов режима пользователя", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обеспечивает отрисовку шрифтов в системе.") },
            { "sihost.exe", ("Хост инфраструктуры оболочки Shell Infrastructure", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обеспечивает работу меню Пуск и центра уведомлений.") },
            { "taskhostw.exe", ("Хост-процесс для задач Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Выполняет системные фоновые задачи по расписанию.") },
            { "spoolsv.exe", ("Диспетчер очереди печати Windows Print Spooler", "Microsoft Windows", ProcessSafetyStatus.SafeToKill, "Служба печати. Если принтер не используется, можно отключить.") },
            { "ctfmon.exe", ("Монитор текстовых служб и языковой панели", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обеспечивает переключение раскладки клавиатуры и ввод текста.") },
            { "SearchHost.exe", ("Индексатор и хост поиска Windows Search", "Microsoft Windows", ProcessSafetyStatus.SafeToKill, "Фоновый поиск. Можно завершить для снижения нагрузки на диск и RAM.") },
            { "StartMenuExperienceHost.exe", ("Хост меню «Пуск» Windows 10/11", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Обеспечивает работу интерфейса меню Пуск.") },
            { "ShellExperienceHost.exe", ("Хост элементов интерфейса Windows Shell", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Отрисовка всплывающих панелей оболочки.") },
            { "SecurityHealthSystray.exe", ("Значок Центра безопасности Windows в трее", "Microsoft Windows", ProcessSafetyStatus.UserApp, "Отображает статус Защитника Windows в трее.") },
            { "smartscreen.exe", ("Служба фильтрации SmartScreen Windows", "Microsoft Windows", ProcessSafetyStatus.CriticalSystem, "Проверка безопасности загружаемых приложений.") },
            { "SystemSettings.exe", ("Параметры Windows 10/11", "Microsoft Windows", ProcessSafetyStatus.UserApp, "Окно параметров системы.") },

            // Web Browsers
            { "chrome.exe", ("Веб-браузер Google Chrome", "Google LLC", ProcessSafetyStatus.SafeToKill, "Потребляет много RAM. Завершение освободит значительный объем памяти.") },
            { "msedge.exe", ("Веб-браузер Microsoft Edge", "Microsoft Corporation", ProcessSafetyStatus.SafeToKill, "Фоновые процессы Edge. Можно безопасно завершить при закрытом браузере.") },
            { "firefox.exe", ("Веб-браузер Mozilla Firefox", "Mozilla Foundation", ProcessSafetyStatus.SafeToKill, "Браузер Firefox. Закрытие освободит оперативную память.") },
            { "opera.exe", ("Веб-браузер Opera", "Opera Software", ProcessSafetyStatus.SafeToKill, "Браузер Opera и фоновые процессы обновления.") },
            { "browser.exe", ("Яндекс Браузер", "Yandex LLC", ProcessSafetyStatus.SafeToKill, "Фоновые процессы Яндекс Браузера.") },
            { "brave.exe", ("Веб-браузер Brave", "Brave Software", ProcessSafetyStatus.SafeToKill, "Браузер Brave.") },

            // Gaming & Launchers
            { "steam.exe", ("Игровая платформа Steam", "Valve Corporation", ProcessSafetyStatus.SafeToKill, "Клиент Steam. Если игры не запущены, можно завершить для экономии RAM.") },
            { "steamwebhelper.exe", ("Фоновый браузерный рендерер Steam", "Valve Corporation", ProcessSafetyStatus.SafeToKill, "Многочисленные фоновые процессы веб-интерфейса Steam. Потребляют 300-800 МБ RAM.") },
            { "EpicGamesLauncher.exe", ("Игровой лаунчер Epic Games Store", "Epic Games Inc.", ProcessSafetyStatus.SafeToKill, "Тяжелый фоновый клиент Epic Games. Рекомендуется завершить вне игр.") },
            { "EpicWebHelper.exe", ("Веб-движок Epic Games Launcher", "Epic Games Inc.", ProcessSafetyStatus.SafeToKill, "Фоновый рендерер Epic Games.") },
            { "Battle.net.exe", ("Игровой клиент Blizzard Battle.net", "Blizzard Entertainment", ProcessSafetyStatus.SafeToKill, "Лаунчер Battle.net. Безопасно завершить вне игровых сессий.") },
            { "GalaxyClient.exe", ("Игровой лаунчер GOG Galaxy", "GOG sp. z o.o.", ProcessSafetyStatus.SafeToKill, "Клиент GOG Galaxy. Можно закрыть для освобождения ресурсов.") },
            { "RiotClientServices.exe", ("Клиент Riot Games (Valorant / LoL)", "Riot Games", ProcessSafetyStatus.SafeToKill, "Фоновый сервис Riot Games.") },
            { "vgtray.exe", ("Античит Riot Vanguard Tray", "Riot Games", ProcessSafetyStatus.UserApp, "Системный сервис античита Vanguard.") },
            { "Discord.exe", ("Мессенджер и голосовой чат Discord", "Discord Inc.", ProcessSafetyStatus.SafeToKill, "Клиент Discord (Electron). Фоновые процессы потребляют от 400 МБ RAM.") },
            { "Telegram.exe", ("Мессенджер Telegram Desktop", "Telegram FZ-LLC", ProcessSafetyStatus.UserApp, "Приложение Telegram.") },
            { "Spotify.exe", ("Музыкальный стриминговый плеер Spotify", "Spotify AB", ProcessSafetyStatus.SafeToKill, "Фоновый плеер Spotify.") },

            // Cloud & Background Sync
            { "OneDrive.exe", ("Облачный сервис синхронизации Microsoft OneDrive", "Microsoft Corporation", ProcessSafetyStatus.SafeToKill, "Фоновая синхронизация файлов. Если не используется постоянно, безопасно завершить.") },
            { "Dropbox.exe", ("Облачное хранилище Dropbox", "Dropbox Inc.", ProcessSafetyStatus.SafeToKill, "Клиент синхронизации Dropbox.") },
            { "GoogleDriveFS.exe", ("Google Диск для компьютеров", "Google LLC", ProcessSafetyStatus.SafeToKill, "Синхронизация Google Диска.") },

            // Hardware & Utility Tools
            { "NVDisplay.Container.exe", ("Контейнер драйвера дисплея NVIDIA", "NVIDIA Corporation", ProcessSafetyStatus.UserApp, "Обеспечивает работу панели управления и оверлея NVIDIA.") },
            { "NVIDIA GeForce Experience.exe", ("Утилита настроек NVIDIA GeForce Experience", "NVIDIA Corporation", ProcessSafetyStatus.SafeToKill, "Оверлей и запись видео. Можно закрыть для максимального FPS.") },
            { "RadeonSoftware.exe", ("Программное обеспечение AMD Radeon Software", "AMD Inc.", ProcessSafetyStatus.UserApp, "Панель управления графикой AMD.") },
            { "RTSS.exe", ("RivaTuner Statistics Server", "Guru3D", ProcessSafetyStatus.UserApp, "Оверлей мониторинга FPS и температур.") },
            { "MSIAfterburner.exe", ("MSI Afterburner GPU Overclocking Tool", "MSI / Guru3D", ProcessSafetyStatus.UserApp, "Управление частотами и вентиляторами видеокарты.") }
        };

        private ProcessManagerService() { }

        public async Task<List<ProcessInfoItem>> GetAllProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<ProcessInfoItem>();
                var rawList = Process.GetProcesses();

                foreach (var proc in rawList)
                {
                    try
                    {
                        string name = proc.ProcessName;
                        string exeName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
                        string title = proc.MainWindowTitle;
                        long memBytes = proc.WorkingSet64;
                        double memMb = memBytes / (1024.0 * 1024.0);
                        int threads = proc.Threads.Count;
                        int pid = proc.Id;

                        string desc = "Фоновый процесс приложения";
                        string publisher = "Сторонний разработчик";
                        ProcessSafetyStatus status = ProcessSafetyStatus.UserApp;
                        string recommendation = "Штатный рабочий процесс.";

                        if (_knowledgeBase.TryGetValue(exeName, out var info) || _knowledgeBase.TryGetValue(name, out info))
                        {
                            desc = info.Desc;
                            publisher = info.Pub;
                            status = info.Status;
                            recommendation = info.Rec;
                        }
                        else if (name.StartsWith("System", StringComparison.OrdinalIgnoreCase) || pid <= 4)
                        {
                            status = ProcessSafetyStatus.CriticalSystem;
                            desc = "Системный сервис Windows NT";
                            publisher = "Microsoft Windows";
                            recommendation = "Критический системный компонент.";
                        }
                        else if (!string.IsNullOrEmpty(title))
                        {
                            desc = $"Оконное приложение: {title}";
                        }

                        string path = string.Empty;
                        try { path = proc.MainModule?.FileName ?? string.Empty; } catch { }

                        list.Add(new ProcessInfoItem
                        {
                            ProcessId = pid,
                            ProcessName = exeName,
                            WindowTitle = title,
                            Description = desc,
                            Publisher = publisher,
                            MemoryMegabytes = memMb,
                            ThreadsCount = threads,
                            ExecutablePath = path,
                            SafetyStatus = status,
                            RecommendationText = recommendation
                        });
                    }
                    catch
                    {
                        // Some protected system processes cannot be inspected
                    }
                }

                // Sort descending by RAM usage by default
                return list.OrderByDescending(p => p.MemoryMegabytes).ToList();
            });
        }

        public bool TerminateProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                if (proc.Id <= 4 || proc.ProcessName.Equals("csrss", StringComparison.OrdinalIgnoreCase) ||
                    proc.ProcessName.Equals("wininit", StringComparison.OrdinalIgnoreCase) ||
                    proc.ProcessName.Equals("services", StringComparison.OrdinalIgnoreCase) ||
                    proc.ProcessName.Equals("lsass", StringComparison.OrdinalIgnoreCase) ||
                    proc.ProcessName.Equals("smss", StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Safety lock for core system processes
                }

                proc.Kill();
                proc.WaitForExit(2000);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TerminateProcessTree(int processId)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = $"/F /T /PID {processId}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(2000);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SetProcessPriority(int processId, ProcessPriorityClass priority)
        {
            try
            {
                using var p = Process.GetProcessById(processId);
                p.PriorityClass = priority;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SuspendProcess(int processId)
        {
            try
            {
                IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, processId);
                if (hProcess != IntPtr.Zero)
                {
                    NativeMethods.NtSuspendProcess(hProcess);
                    NativeMethods.CloseHandle(hProcess);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool ResumeProcess(int processId)
        {
            try
            {
                IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, processId);
                if (hProcess != IntPtr.Zero)
                {
                    NativeMethods.NtResumeProcess(hProcess);
                    NativeMethods.CloseHandle(hProcess);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool SearchOnline(string processName)
        {
            try
            {
                string url = $"https://www.google.com/search?q={Uri.EscapeDataString(processName + " процесс windows что это")}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        public bool OpenProcessLocation(string executablePath)
        {
            try
            {
                if (File.Exists(executablePath))
                {
                    Process.Start("explorer.exe", $"/select,\"{executablePath}\"");
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
