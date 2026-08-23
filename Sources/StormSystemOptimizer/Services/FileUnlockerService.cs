using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class LockingProcessItem
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = "Win32 Приложение";
        public string Status { get; set; } = "Блокирует файл";
        public DateTime StartTime { get; set; }
    }

    public class FileUnlockerService
    {
        private static FileUnlockerService? _instance;
        public static FileUnlockerService Instance => _instance ??= new FileUnlockerService();

        #region Native Restart Manager API

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
            uint nApplications, [In] RM_UNIQUE_PROCESS[]? rgApplications, uint nServices, [In] string[]? rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
            ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[]? rgAffectedApps, ref uint lpdwRebootReasons);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        #endregion

        public async Task<List<LockingProcessItem>> FindLockingProcessesAsync(string targetPath)
        {
            return await Task.Run(() =>
            {
                var list = new List<LockingProcessItem>();
                if (string.IsNullOrWhiteSpace(targetPath) || (!File.Exists(targetPath) && !Directory.Exists(targetPath)))
                {
                    return list;
                }

                uint sessionHandle;
                string sessionKey = Guid.NewGuid().ToString();
                int res = RmStartSession(out sessionHandle, 0, sessionKey);
                if (res != 0) return list;

                try
                {
                    string[] resources = new string[] { targetPath };
                    res = RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, null, 0, null);
                    if (res != 0) return list;

                    uint pnProcInfoNeeded = 0;
                    uint pnProcInfo = 0;
                    uint lpdwRebootReasons = 0;

                    res = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);
                    if (res == 234) // ERROR_MORE_DATA
                    {
                        var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                        pnProcInfo = pnProcInfoNeeded;
                        res = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                        if (res == 0)
                        {
                            for (int i = 0; i < pnProcInfo; i++)
                            {
                                int pid = processInfo[i].Process.dwProcessId;
                                string name = processInfo[i].strAppName;
                                string exePath = string.Empty;
                                string appType = processInfo[i].ApplicationType switch
                                {
                                    RM_APP_TYPE.RmExplorer => "Проводник Windows (Explorer)",
                                    RM_APP_TYPE.RmService => "Системная служба Windows",
                                    RM_APP_TYPE.RmMainWindow => "Оконное приложение",
                                    RM_APP_TYPE.RmConsole => "Консольный процесс",
                                    _ => "Системный дескриптор"
                                };

                                try
                                {
                                    var proc = Process.GetProcessById(pid);
                                    if (string.IsNullOrEmpty(name)) name = proc.ProcessName;
                                    exePath = proc.MainModule?.FileName ?? string.Empty;
                                }
                                catch { }

                                list.Add(new LockingProcessItem
                                {
                                    ProcessId = pid,
                                    ProcessName = string.IsNullOrEmpty(name) ? $"PID: {pid}" : name,
                                    ExecutablePath = exePath,
                                    ApplicationType = appType,
                                    Status = "Удерживает дескриптор файла"
                                });
                            }
                        }
                    }
                }
                catch { }
                finally
                {
                    RmEndSession(sessionHandle);
                }

                // Fallback: Check if file itself can be opened with FileShare.None
                if (list.Count == 0 && File.Exists(targetPath))
                {
                    try
                    {
                        using var fs = File.Open(targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException)
                    {
                        list.Add(new LockingProcessItem
                        {
                            ProcessId = 0,
                            ProcessName = "Системный процесс ядра NT",
                            ExecutablePath = targetPath,
                            ApplicationType = "Файловая блокировка ОС",
                            Status = "Эксклюзивно занят другим потоком"
                        });
                    }
                    catch { }
                }

                return list;
            });
        }

        public async Task<bool> UnlockTargetAsync(string targetPath, bool terminateLockingProcesses = true)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var lockers = await FindLockingProcessesAsync(targetPath);
                    if (terminateLockingProcesses)
                    {
                        foreach (var locker in lockers)
                        {
                            if (locker.ProcessId > 4) // Skip System and Idle
                            {
                                try
                                {
                                    var proc = Process.GetProcessById(locker.ProcessId);
                                    proc.Kill();
                                    proc.WaitForExit(1000);
                                }
                                catch { }
                            }
                        }
                    }

                    // Reset Read-Only and System attributes
                    if (File.Exists(targetPath))
                    {
                        File.SetAttributes(targetPath, FileAttributes.Normal);
                    }
                    else if (Directory.Exists(targetPath))
                    {
                        var di = new DirectoryInfo(targetPath);
                        di.Attributes = FileAttributes.Normal;
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> UnlockAndDeleteAsync(string targetPath)
        {
            return await Task.Run(async () =>
            {
                await UnlockTargetAsync(targetPath, true);
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                        return true;
                    }
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Delete(targetPath, true);
                        return true;
                    }
                }
                catch
                {
                    // Fallback to MoveFileEx delay delete on reboot
                    MoveFileEx(targetPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                return !File.Exists(targetPath) && !Directory.Exists(targetPath);
            });
        }

        public async Task<bool> UnlockAndRenameAsync(string targetPath, string newName)
        {
            return await Task.Run(async () =>
            {
                await UnlockTargetAsync(targetPath, true);
                try
                {
                    string dir = Path.GetDirectoryName(targetPath) ?? "";
                    string dest = Path.Combine(dir, newName);
                    if (File.Exists(targetPath))
                    {
                        File.Move(targetPath, dest, true);
                        return true;
                    }
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Move(targetPath, dest);
                        return true;
                    }
                }
                catch { }
                return false;
            });
        }

        public async Task<bool> UnlockAndMoveAsync(string targetPath, string targetDirectory)
        {
            return await Task.Run(async () =>
            {
                await UnlockTargetAsync(targetPath, true);
                try
                {
                    if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
                    string name = Path.GetFileName(targetPath);
                    string dest = Path.Combine(targetDirectory, name);
                    if (File.Exists(targetPath))
                    {
                        File.Move(targetPath, dest, true);
                        return true;
                    }
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Move(targetPath, dest);
                        return true;
                    }
                }
                catch { }
                return false;
            });
        }

        public void KillProcess(int processId)
        {
            if (processId <= 4) return;
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill();
                proc.WaitForExit(1000);
            }
            catch { }
        }

        #region Context Menu Integration

        public bool IsContextMenuRegistered()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\StormUnlocker");
                return key != null;
            }
            catch { return false; }
        }

        public void SetContextMenuRegistered(bool register)
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StormSystemOptimizer.exe");
                }

                string menuTitle = "⚡ Разблокировать через STORM Optimizer";
                string iconParam = $"\"{exePath}\",0";
                string commandParam = $"\"{exePath}\" /unlock \"%1\"";

                string[] targetKeys = new[]
                {
                    @"Software\Classes\*\shell\StormUnlocker",
                    @"Software\Classes\Directory\shell\StormUnlocker",
                    @"Software\Classes\Drive\shell\StormUnlocker"
                };

                if (register)
                {
                    foreach (var path in targetKeys)
                    {
                        using var key = Registry.CurrentUser.CreateSubKey(path);
                        key.SetValue("", menuTitle, RegistryValueKind.String);
                        key.SetValue("Icon", iconParam, RegistryValueKind.String);
                        using var cmd = key.CreateSubKey("command");
                        cmd.SetValue("", commandParam, RegistryValueKind.String);
                    }
                }
                else
                {
                    foreach (var path in targetKeys)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(path, false);
                    }
                }
            }
            catch { }
        }

        #endregion
    }
}
