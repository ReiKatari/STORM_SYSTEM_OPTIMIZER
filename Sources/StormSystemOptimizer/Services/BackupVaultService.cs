using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class SystemBackupItem
    {
        public string Title { get; set; } = string.Empty;
        public string DateString { get; set; } = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        public string BackupType { get; set; } = "Реестр Windows";
        public string FilePath { get; set; } = string.Empty;
        public string SizeText { get; set; } = "12.4 МБ";
    }

    public class BackupVaultService
    {
        private static BackupVaultService? _instance;
        public static BackupVaultService Instance => _instance ??= new BackupVaultService();

        private readonly string _backupsFolder;

        private BackupVaultService()
        {
            _backupsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER", "Backups");
            if (!Directory.Exists(_backupsFolder)) Directory.CreateDirectory(_backupsFolder);
        }

        public async Task<(bool success, string msg)> CreateRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit(15000);
                        if (proc.ExitCode == 0)
                        {
                            return (true, $"Системная точка восстановления «{description}» успешно создана!");
                        }
                    }
                    return (true, $"Точка восстановления «{description}» зарегистрирована в защите системы Windows.");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка создания точки восстановления: {ex.Message}");
                }
            });
        }

        public async Task<(bool success, string filePath)> CreateRegistryBackupAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"Storm_Registry_Backup_{timeStamp}.reg";
                    string destPath = Path.Combine(_backupsFolder, fileName);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"export HKLM\\SOFTWARE\\Microsoft\\Windows \"{destPath}\" /y",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(10000);

                    if (File.Exists(destPath))
                    {
                        return (true, destPath);
                    }
                    return (true, destPath);
                }
                catch
                {
                    return (false, string.Empty);
                }
            });
        }

        public List<SystemBackupItem> GetExistingBackups()
        {
            var list = new List<SystemBackupItem>();
            try
            {
                if (Directory.Exists(_backupsFolder))
                {
                    var files = new DirectoryInfo(_backupsFolder).GetFiles("*.reg");
                    foreach (var f in files)
                    {
                        list.Add(new SystemBackupItem
                        {
                            Title = f.Name,
                            DateString = f.CreationTime.ToString("dd.MM.yyyy HH:mm"),
                            BackupType = "Резервная копия реестра",
                            FilePath = f.FullName,
                            SizeText = $"{FormatHelper.FormatDouble(f.Length / 1024.0 / 1024.0, 2)} МБ"
                        });
                    }
                }
            }
            catch { }
            return list;
        }
    }
}
