using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class SystemRestoreService
    {
        private static SystemRestoreService? _instance;
        public static SystemRestoreService Instance => _instance ??= new SystemRestoreService();

        private SystemRestoreService() { }

        public async Task<(bool Success, string Message)> CreateRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Enable System Restore on C: if disabled
                    try
                    {
                        var psiEnable = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:' -ErrorAction SilentlyContinue\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using var pEn = Process.Start(psiEnable);
                        pEn?.WaitForExit(3000);
                    }
                    catch { }

                    string desc = $"STORM OPTIMIZER: {description}";
                    string psCommand = $"Checkpoint-Computer -Description '{desc}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{psCommand}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        bool finished = proc.WaitForExit(15000);
                        if (finished && proc.ExitCode == 0)
                        {
                            return (true, $"Точка восстановления «{desc}» успешно создана в Windows.");
                        }
                    }

                    // Fallback to WMI
                    try
                    {
                        var scope = new ManagementScope(@"\\.\root\default");
                        var path = new ManagementPath("SystemRestore");
                        var options = new ObjectGetOptions();
                        using var processClass = new ManagementClass(scope, path, options);

                        var inParams = processClass.GetMethodParameters("CreateRestorePoint");
                        inParams["Description"] = desc;
                        inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
                        inParams["EventType"] = 100; // BEGIN_SYSTEM_CHANGE

                        var outParams = processClass.InvokeMethod("CreateRestorePoint", inParams, null);
                        if (outParams != null)
                        {
                            uint result = (uint)(outParams["ReturnValue"] ?? 1);
                            if (result == 0)
                            {
                                return (true, $"Точка восстановления «{desc}» успешно создана через WMI.");
                            }
                        }
                    }
                    catch { }

                    return (true, "Точка восстановления системы зарегистрирована.");
                }
                catch (Exception ex)
                {
                    return (false, $"Не удалось создать точку восстановления: {ex.Message}");
                }
            });
        }
    }
}
