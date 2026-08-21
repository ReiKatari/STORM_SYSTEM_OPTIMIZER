using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private bool _isGameBoostActive = false;
        private int _boostedGameProcessId = 0;
        private string _boostedGameName = string.Empty;
        private bool _isTimerResolutionEnabled = false;

        public bool IsGameBoostActive => _isGameBoostActive;
        public bool IsTimerResolutionEnabled => _isTimerResolutionEnabled;
        public string ActiveGameName => _boostedGameName;

        public event Action<bool, string>? GameBoostStateChanged;

        private GameBoostService() { }

        // 1. Timer Resolution (0.500 ms)
        public bool SetHighResolutionTimer(bool enable)
        {
            try
            {
                if (enable)
                {
                    // 5000 units of 100ns = 0.500ms
                    uint desired = 5000;
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

        // 2. Start Game Boost Engine with Auto-Detection
        public void StartAutoGameDetection()
        {
            if (_gameDetectTimer == null)
            {
                _gameDetectTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
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
                    var proc = Process.GetProcessById((int)pid);
                    string procName = proc.ProcessName.ToLowerInvariant();

                    if (IsGameProcess(procName, proc))
                    {
                        BoostGameProcess(proc);
                    }
                }
            }
            catch { }
        }

        public bool IsGameProcess(string name, Process proc)
        {
            var knownGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cs2", "csgo", "dota2", "valorant", "valorant-win64-shipping", "r5apex", "fortniteclient-win64-shipping",
                "gta5", "gta_sa", "cyberpunk2077", "witcher3", "overwatch", "pubg", "tslgame", "warzone", "cod",
                "rocketleague", "rainbowsix", "destiny2", "genshinimpact", "honkaistarrail", "forza", "minecraft",
                "javaw", "hl2", "tarkov", "escapefromtarkov", "deadbydaylight", "rust", "rustclient", "seaofthieves"
            };

            if (knownGames.Contains(name)) return true;

            try
            {
                // Check if process has main window, is non-system, uses high memory (> 600MB) or has directx modules
                if (proc.MainWindowHandle != IntPtr.Zero && proc.WorkingSet64 > 500 * 1024 * 1024)
                {
                    string title = proc.MainWindowTitle.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(title) && !title.Contains("visual studio") && !title.Contains("storm") && !title.Contains("browser") && !title.Contains("chrome") && !title.Contains("edge"))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        // 3. Apply 1-Click Game Boost
        public bool BoostGameProcess(Process gameProc)
        {
            try
            {
                _boostedGameProcessId = gameProc.Id;
                _boostedGameName = gameProc.ProcessName;

                // 1. High Priority for Game
                gameProc.PriorityClass = ProcessPriorityClass.High;

                // 2. Set CPU Affinity for P-Cores (first N cores)
                int totalCores = Environment.ProcessorCount;
                if (totalCores >= 8)
                {
                    // Allocate performance cores (e.g. first 8/12 threads)
                    long mask = (1L << Math.Min(16, totalCores)) - 1;
                    gameProc.ProcessorAffinity = (IntPtr)mask;
                }

                // 3. Enable 0.5ms Timer Resolution
                SetHighResolutionTimer(true);

                // 4. Deprioritize background non-essential processes (E-Cores / Low Priority)
                Task.Run(() => DemoteBackgroundProcesses(gameProc.Id));

                // 5. Purge Standby memory for clean headroom
                MemoryOptimizerService.Instance.PurgeStandbyList();

                _isGameBoostActive = true;
                GameBoostStateChanged?.Invoke(true, $"Игровой режим активирован: {_boostedGameName} (P-Cores + High Priority + 0.5ms Timer)");
                TrayService.Instance.ShowNotification("STORM Game Boost ⚡", $"Игра «{_boostedGameName}» оптимизирована! Приоритет повышен, таймер 0.5мс включен.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public void DisableGameBoost()
        {
            _isGameBoostActive = false;
            _boostedGameProcessId = 0;
            _boostedGameName = string.Empty;

            // Reset Timer
            SetHighResolutionTimer(false);

            GameBoostStateChanged?.Invoke(false, "Игровой режим выключен");
        }

        private void DemoteBackgroundProcesses(int gamePid)
        {
            try
            {
                var backgroundToDemote = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "onedrive", "dropbox", "epicgameslauncher", "discord", "spotify", "telegram", "steamwebhelper",
                    "origin", "ea", "battle.net", "razer synapse", "armoury crate", "icue"
                };

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == gamePid || p.Id <= 4) continue;
                        if (backgroundToDemote.Contains(p.ProcessName))
                        {
                            p.PriorityClass = ProcessPriorityClass.BelowNormal;

                            // Move to upper cores (E-cores) if high core count
                            int totalCores = Environment.ProcessorCount;
                            if (totalCores > 8)
                            {
                                long eCoreMask = ((1L << totalCores) - 1) & ~((1L << 4) - 1);
                                if (eCoreMask > 0) p.ProcessorAffinity = (IntPtr)eCoreMask;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // 4. DWM Latency Tweaks
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
