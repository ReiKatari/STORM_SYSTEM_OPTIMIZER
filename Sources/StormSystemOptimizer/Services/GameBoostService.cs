using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class GameBoostService
    {
        private static GameBoostService? _instance;
        public static GameBoostService Instance => _instance ??= new GameBoostService();

        private DispatcherTimer? _gameDetectTimer;
        private DispatcherTimer? _gameWatcherTimer;

        private bool _isGameBoostActive = false;
        private int _boostedGameProcessId = 0;
        private string _boostedGameName = string.Empty;
        private bool _isTimerResolutionEnabled = false;

        // CPU Sets settings & state
        public bool IsCpuSetsIsolationEnabled { get; set; } = true;
        public bool IsNoSmtEnabled { get; set; } = false;
        public bool IsDynamicCpuDemoteEnabled { get; set; } = true;

        public string ActiveCpuMaskName { get; private set; } = string.Empty;
        public int ActiveChildrenCount => _governedChildPids.Count;
        public int DemotedAppsCount => _governedBackgroundPids.Count;

        public bool IsGameBoostActive => _isGameBoostActive;
        public bool IsTimerResolutionEnabled => _isTimerResolutionEnabled;
        public string ActiveGameName => _boostedGameName;

        public event Action<bool, string>? GameBoostStateChanged;

        // Tracking sets
        private readonly ConcurrentDictionary<int, DateTime> _governedChildPids = new();
        private readonly ConcurrentDictionary<int, DateTime> _governedBackgroundPids = new();
        private readonly ConcurrentDictionary<int, int> _heavyProcessCpuTicks = new();

        private static readonly string JournalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StormSystemOptimizer",
            "applied.journal"
        );

        private static readonly HashSet<string> ExcludedProcessNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "dwm", "csrss", "audiodg", "explorer", "services", "svchost", "lsass", "wininit", "winlogon",
            "system", "idle", "smss", "fontdrvhost", "sihost", "taskhostw",
            "easyanticheat", "easyanticheat_eos", "beservice", "vgc", "vgtray", "battleye",
            "nvdisplay.container", "nvcontainer", "amdrsserv", "radeonsoftware", "amd3dvcacheuser",
            "steam", "epicgameslauncher", "battle.net", "stormsystemoptimizer", "stormlauncher"
        };

        private static readonly HashSet<string> KnownGames = new(StringComparer.OrdinalIgnoreCase)
        {
            "cs2", "csgo", "dota2", "valorant", "valorant-win64-shipping", "r5apex", "fortniteclient-win64-shipping",
            "gta5", "gta_sa", "cyberpunk2077", "witcher3", "overwatch", "pubg", "tslgame", "warzone", "cod",
            "rocketleague", "rainbowsix", "destiny2", "genshinimpact", "honkaistarrail", "forza", "minecraft",
            "javaw", "hl2", "tarkov", "escapefromtarkov", "deadbydaylight", "rust", "rustclient", "seaofthieves",
            "helldivers2", "starfield", "blackmythwukong", "eldenring", "bg3", "baldursgate3", "apex"
        };

        private GameBoostService()
        {
            // Recover any leftover CPU sets from previous sessions or unexpected terminations
            Task.Run(() => RecoverAndCleanJournal());
        }

        public void EnableHighResolutionTimer() => SetHighResolutionTimer(true);
        public void DisableHighResolutionTimer() => SetHighResolutionTimer(false);

        public void ActivateGameBoost()
        {
            try
            {
                var cur = Process.GetCurrentProcess();
                BoostGameProcess(cur);
                _isGameBoostActive = true;
                GameBoostStateChanged?.Invoke(true, "STORM GAME BOOST: Активен (CPU Sets + Таймер 0.5мс + Фокус)");
            }
            catch { }
        }

        public void DeactivateGameBoost() => DisableGameBoost();

        // 1. Timer Resolution (0.500 ms)
        public bool SetHighResolutionTimer(bool enable)
        {
            try
            {
                if (enable)
                {
                    uint desired = 5000; // 5000 * 100ns = 0.500ms
                    int status = NativeMethods.NtSetTimerResolution(desired, true, out uint current);
                    _isTimerResolutionEnabled = status == 0;
                    return _isTimerResolutionEnabled;
                }
                else
                {
                    uint desired = 5000;
                    NativeMethods.NtSetTimerResolution(desired, false, out _);
                    _isTimerResolutionEnabled = false;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // 2. Auto-Detection Engine
        public void StartAutoGameDetection()
        {
            if (_gameDetectTimer == null)
            {
                _gameDetectTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2.5)
                };
                _gameDetectTimer.Tick += (s, e) => CheckForegroundGame();
            }
            _gameDetectTimer.Start();
        }

        public void StopAutoGameDetection()
        {
            _gameDetectTimer?.Stop();
        }

        private void CheckForegroundGame()
        {
            try
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid <= 4 || pid == Process.GetCurrentProcess().Id) return;

                if (pid != _boostedGameProcessId)
                {
                    Process? proc = null;
                    try { proc = Process.GetProcessById((int)pid); } catch { return; }

                    if (proc != null && IsGameProcess(proc.ProcessName, proc))
                    {
                        BoostGameProcess(proc);
                    }
                }
            }
            catch { }
        }

        public bool IsGameProcess(string name, Process proc)
        {
            if (ExcludedProcessNames.Contains(name)) return false;
            if (KnownGames.Contains(name)) return true;

            try
            {
                if (proc.MainWindowHandle != IntPtr.Zero && proc.WorkingSet64 > 450 * 1024 * 1024)
                {
                    string title = proc.MainWindowTitle.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(title) &&
                        !title.Contains("visual studio") &&
                        !title.Contains("storm") &&
                        !title.Contains("browser") &&
                        !title.Contains("chrome") &&
                        !title.Contains("edge") &&
                        !title.Contains("firefox"))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        // 3. Modern Boost with Windows CPU Sets API
        public bool BoostGameProcess(Process gameProc)
        {
            try
            {
                _boostedGameProcessId = gameProc.Id;
                _boostedGameName = gameProc.ProcessName;

                // Set High Priority
                try { gameProc.PriorityClass = ProcessPriorityClass.High; } catch { }

                // Enable 0.5ms Timer Resolution
                SetHighResolutionTimer(true);

                // Purge Standby memory for clean RAM headroom
                MemoryOptimizerService.Instance.PurgeStandbyList();

                // Apply CPU Sets Isolation
                if (IsCpuSetsIsolationEnabled)
                {
                    var topo = CpuTopologyService.Instance.CurrentTopology;
                    CpuNamedMask? targetMask = null;

                    if (IsNoSmtEnabled)
                    {
                        targetMask = topo.DerivedMasks.FirstOrDefault(m => m.Name.Contains("No SMT")) ?? topo.DefaultGameMask;
                    }
                    else
                    {
                        targetMask = topo.DefaultGameMask ?? topo.DerivedMasks.FirstOrDefault();
                    }

                    if (targetMask != null && targetMask.CpuSetIds.Count > 0)
                    {
                        ActiveCpuMaskName = targetMask.Name;
                        ApplyCpuSetsToProcess(gameProc.Id, targetMask.CpuSetIds.ToArray(), gameProc.ProcessName);
                    }
                    else
                    {
                        ActiveCpuMaskName = "Стандартная (All)";
                    }
                }

                _isGameBoostActive = true;

                // Start Child Watcher and Dynamic Background CPU Demoter
                StartGameWatcherTimer();

                GameBoostStateChanged?.Invoke(true, $"Игровой режим: {_boostedGameName} • CPU Sets [{ActiveCpuMaskName}] • Таймер 0.5мс");
                TrayService.Instance.ShowNotification("STORM GAME BOOST ⚡", $"Игра «{_boostedGameName}» оптимизирована! Назначена маска CPU Sets [{ActiveCpuMaskName}], таймер 0.5мс.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GameBoostService] Error boosting process: {ex.Message}");
                return false;
            }
        }

        public void DisableGameBoost()
        {
            _isGameBoostActive = false;
            _boostedGameProcessId = 0;
            _boostedGameName = string.Empty;
            ActiveCpuMaskName = string.Empty;

            StopGameWatcherTimer();

            // Reset Timer Resolution
            SetHighResolutionTimer(false);

            // Revert all governed processes cleanly
            Task.Run(() =>
            {
                RecoverAndCleanJournal();
            });

            _governedChildPids.Clear();
            _governedBackgroundPids.Clear();
            _heavyProcessCpuTicks.Clear();

            GameBoostStateChanged?.Invoke(false, "Игровой режим выключен • CPU Sets сброшены");
        }

        // 4. Watcher Loop: Child Process Tree & Dynamic Background App Demotion
        private void StartGameWatcherTimer()
        {
            if (_gameWatcherTimer == null)
            {
                _gameWatcherTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _gameWatcherTimer.Tick += (s, e) => WatcherTick();
            }
            _gameWatcherTimer.Start();
        }

        private void StopGameWatcherTimer()
        {
            _gameWatcherTimer?.Stop();
        }

        private void WatcherTick()
        {
            if (!_isGameBoostActive || _boostedGameProcessId == 0) return;

            // Check if game is still running
            try
            {
                var mainProc = Process.GetProcessById(_boostedGameProcessId);
                if (mainProc.HasExited)
                {
                    DisableGameBoost();
                    return;
                }
            }
            catch
            {
                DisableGameBoost();
                return;
            }

            var topo = CpuTopologyService.Instance.CurrentTopology;
            var gameMask = topo.DefaultGameMask ?? topo.DerivedMasks.FirstOrDefault();
            var bgMask = topo.DefaultBackgroundMask ?? topo.DerivedMasks.LastOrDefault();

            if (gameMask == null || gameMask.CpuSetIds.Count == 0) return;

            // 1. Scan for new Child Processes of the Game
            try
            {
                var allProcs = Process.GetProcesses();
                var childPids = new HashSet<int>();

                foreach (var p in allProcs)
                {
                    try
                    {
                        if (p.Id == _boostedGameProcessId || p.Id <= 4) continue;
                        if (ExcludedProcessNames.Contains(p.ProcessName)) continue;

                        int parentId = GetParentProcessId(p.Id);
                        if (parentId == _boostedGameProcessId || _governedChildPids.ContainsKey(parentId))
                        {
                            childPids.Add(p.Id);
                            if (!_governedChildPids.ContainsKey(p.Id))
                            {
                                _governedChildPids[p.Id] = DateTime.UtcNow;
                                ApplyCpuSetsToProcess(p.Id, gameMask.CpuSetIds.ToArray(), p.ProcessName);
                            }
                        }
                    }
                    catch { }
                }

                // Clean exited children
                foreach (var childPid in _governedChildPids.Keys)
                {
                    if (!childPids.Contains(childPid))
                    {
                        _governedChildPids.TryRemove(childPid, out _);
                    }
                }

                // 2. Dynamic CPU% Demoter for Heavy Background Tasks (if enabled)
                if (IsDynamicCpuDemoteEnabled && bgMask != null && bgMask.CpuSetIds.Count > 0)
                {
                    var knownHeavy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "onedrive", "dropbox", "discord", "spotify", "telegram", "steamwebhelper",
                        "chrome", "msedge", "firefox", "brave", "opera", "epicgameslauncher",
                        "battle.net", "razer synapse", "armoury crate", "icue"
                    };

                    foreach (var p in allProcs)
                    {
                        try
                        {
                            if (p.Id == _boostedGameProcessId || p.Id <= 4) continue;
                            if (childPids.Contains(p.Id)) continue;
                            if (ExcludedProcessNames.Contains(p.ProcessName)) continue;

                            if (knownHeavy.Contains(p.ProcessName))
                            {
                                if (!_governedBackgroundPids.ContainsKey(p.Id))
                                {
                                    _governedBackgroundPids[p.Id] = DateTime.UtcNow;
                                    try { p.PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
                                    ApplyCpuSetsToProcess(p.Id, bgMask.CpuSetIds.ToArray(), p.ProcessName);
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch { }
        }

        // 5. Win32 CPU Sets Application & Journaling
        public static bool ApplyCpuSetsToProcess(int pid, uint[] cpuSetIds, string processName)
        {
            IntPtr hProc = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_SET_LIMITED_INFORMATION | NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                pid
            );

            if (hProc == IntPtr.Zero) return false;

            try
            {
                bool success = NativeMethods.SetProcessDefaultCpuSets(hProc, cpuSetIds, (uint)cpuSetIds.Length);
                if (success)
                {
                    AppendJournalEntry(pid, processName);
                    return true;
                }
                return false;
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
        }

        public static bool ClearCpuSetsFromProcess(int pid)
        {
            IntPtr hProc = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_SET_LIMITED_INFORMATION | NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                pid
            );

            if (hProc == IntPtr.Zero) return false;

            try
            {
                // Passing NULL pointer and count = 0 cleanly clears CPU Sets in Windows
                return NativeMethods.SetProcessDefaultCpuSets(hProc, null, 0);
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
        }

        private static void AppendJournalEntry(int pid, string name)
        {
            try
            {
                string? dir = Path.GetDirectoryName(JournalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string line = $"{pid}\t{DateTime.UtcNow.Ticks}\t{name}\n";
                File.AppendAllText(JournalPath, line, Encoding.UTF8);
            }
            catch { }
        }

        public static void RecoverAndCleanJournal()
        {
            try
            {
                if (!File.Exists(JournalPath)) return;

                string[] lines = File.ReadAllLines(JournalPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('\t');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out int pid) && pid > 4)
                    {
                        try
                        {
                            ClearCpuSetsFromProcess(pid);
                        }
                        catch { }
                    }
                }

                File.Delete(JournalPath);
            }
            catch { }
        }

        private static int GetParentProcessId(int pid)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {pid}"
                );
                foreach (var obj in searcher.Get())
                {
                    return Convert.ToInt32(obj["ParentProcessId"]);
                }
            }
            catch { }
            return 0;
        }

        // 6. DWM & Multimedia Latency Tweaks
        public bool ApplyDwmLatencyTweaks()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    key.SetValue("MaxFramesAllowed", 1, RegistryValueKind.DWord);
                    key.SetValue("EnableWindowColorization", 1, RegistryValueKind.DWord);
                }

                using var sysKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games");
                if (sysKey != null)
                {
                    sysKey.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                    sysKey.SetValue("Priority", 6, RegistryValueKind.DWord);
                    sysKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                    sysKey.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
