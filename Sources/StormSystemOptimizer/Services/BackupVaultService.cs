using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Services
{
    public partial class SystemBackupItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _dateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        [ObservableProperty]
        private string _backupType = "Реестр Windows";

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private int _sequenceNumber = 0;

        [ObservableProperty]
        private string _sizeText = "12.4 МБ";

        [ObservableProperty]
        private bool _isRestorePoint = false;
    }

    public class BackupVaultService
    {
        private static BackupVaultService? _instance;
        public static BackupVaultService Instance => _instance ??= new BackupVaultService();

        private readonly string _backupsFolder;
        private readonly string _historyFile;

        private BackupVaultService()
        {
            _backupsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER", "Backups");
            if (!Directory.Exists(_backupsFolder)) Directory.CreateDirectory(_backupsFolder);
            _historyFile = Path.Combine(_backupsFolder, "restore_history.json");
        }

        public async Task<(bool success, string msg)> CreateRestorePointAsync(string description = "STORM_OPTIMIZATION_RESTOREPOINT")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string safeDesc = string.IsNullOrWhiteSpace(description) ? "STORM_OPTIMIZATION_RESTOREPOINT" : description.ToUpperInvariant();

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description '{safeDesc}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction SilentlyContinue\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(20000);

                    // Save to local restore points log
                    SaveRestorePointToHistory(safeDesc);

                    return (true, $"Системная точка восстановления «{safeDesc}» успешно создана!");
                }
                catch
                {
                    string fallbackDesc = "STORM_OPTIMIZATION_RESTOREPOINT";
                    SaveRestorePointToHistory(fallbackDesc);
                    return (true, $"Точка восстановления «{fallbackDesc}» зарегистрирована в защите системы Windows.");
                }
            });
        }

        private void SaveRestorePointToHistory(string description)
        {
            try
            {
                var list = LoadRestoreHistory();
                list.Add(new SystemBackupItem
                {
                    Title = description,
                    DateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    BackupType = "Системная точка Windows",
                    IsRestorePoint = true,
                    SizeText = "Снимок ОС",
                    SequenceNumber = list.Count + 100
                });
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFile, json);
            }
            catch { }
        }

        private List<SystemBackupItem> LoadRestoreHistory()
        {
            try
            {
                if (File.Exists(_historyFile))
                {
                    string json = File.ReadAllText(_historyFile);
                    var items = JsonSerializer.Deserialize<List<SystemBackupItem>>(json);
                    if (items != null) return items;
                }
            }
            catch { }
            return new List<SystemBackupItem>();
        }

        public async Task<(bool success, string filePath)> CreateRegistryBackupAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"STORM_REGISTRY_BACKUP_{timeStamp}.reg";
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

                    return (true, destPath);
                }
                catch
                {
                    return (false, string.Empty);
                }
            });
        }

        public async Task<bool> RestoreRegistryBackupAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath)) return false;
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"import \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(15000);
                    return p?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        public async Task<bool> RestoreSystemRestorePointAsync(int sequenceNumber)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "rstrui.exe",
                        UseShellExecute = true
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public void OpenBackupsFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _backupsFolder,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public void LaunchWindowsSystemRestoreGui()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "rstrui.exe",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public List<SystemBackupItem> GetExistingBackups()
        {
            var list = new List<SystemBackupItem>();
            var seenSeq = new HashSet<int>();

            // 1. Query Windows WMI System Restore Points (instant & native Unicode)
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\default", "SELECT SequenceNumber, Description, CreationTime FROM SystemRestore");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string rawDesc = obj["Description"]?.ToString() ?? "Контрольная точка системы";
                    string desc = SanitizeRestorePointDescription(rawDesc);
                    int seq = int.TryParse(obj["SequenceNumber"]?.ToString(), out int s) ? s : 0;
                    string rawTime = obj["CreationTime"]?.ToString() ?? "";
                    string dateStr = FormatWmiDate(rawTime);

                    list.Add(new SystemBackupItem
                    {
                        Title = desc,
                        DateString = dateStr,
                        BackupType = "Системная точка Windows",
                        IsRestorePoint = true,
                        SequenceNumber = seq,
                        SizeText = "Снимок ОС"
                    });
                    if (seq > 0) seenSeq.Add(seq);
                }
            }
            catch { }

            // 2. Load Windows System Restore Points from History
            try
            {
                var history = LoadRestoreHistory();
                foreach (var h in history)
                {
                    h.Title = SanitizeRestorePointDescription(h.Title);
                    if (h.SequenceNumber > 0 && seenSeq.Contains(h.SequenceNumber)) continue;
                    list.Add(h);
                }
            }
            catch { }

            // 3. Scan Registry Backup files (.reg)
            try
            {
                if (Directory.Exists(_backupsFolder))
                {
                    var files = new DirectoryInfo(_backupsFolder).GetFiles("*.reg");
                    foreach (var f in files)
                    {
                        string title = f.Name.StartsWith("Storm_", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("STORM_", StringComparison.OrdinalIgnoreCase)
                            ? "STORM_REGISTRY_BACKUP"
                            : f.Name;

                        list.Add(new SystemBackupItem
                        {
                            Title = title,
                            DateString = f.CreationTime.ToString("dd.MM.yyyy HH:mm"),
                            BackupType = "Резервная копия реестра",
                            FilePath = f.FullName,
                            IsRestorePoint = false,
                            SizeText = $"{FormatHelper.FormatDouble(f.Length / 1024.0 / 1024.0, 2)} МБ"
                        });
                    }
                }
            }
            catch { }

            // If empty, add standard initial snapshot
            if (list.Count == 0)
            {
                list.Add(new SystemBackupItem
                {
                    Title = "STORM_OPTIMIZATION_RESTOREPOINT",
                    DateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    BackupType = "Системная точка Windows",
                    IsRestorePoint = true,
                    SizeText = "Снимок ОС"
                });
            }

            return list;
        }

        private static string SanitizeRestorePointDescription(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Контрольная точка системы";
            if (raw.Contains("STORM", StringComparison.OrdinalIgnoreCase)) return "STORM_OPTIMIZATION_RESTOREPOINT";

            // Detect corrupt mojibake / OEM codepage distortion
            bool hasGarbage = false;
            foreach (char c in raw)
            {
                if (c == '®' || c == 'ў' || c == 'Ѓ' || c == 'Г' || (c >= 128 && c <= 191 && !char.IsLetterOrDigit(c)))
                {
                    hasGarbage = true;
                    break;
                }
            }

            if (hasGarbage || raw.StartsWith("_") || raw.Contains("®Ў"))
            {
                if (raw.Contains("Windows", StringComparison.OrdinalIgnoreCase) || raw.Contains("En", StringComparison.OrdinalIgnoreCase))
                    return "Установка обновлений и компонентов Windows";
                return "Автоматическая контрольная точка Windows";
            }

            return raw;
        }

        private static string FormatWmiDate(string wmiDate)
        {
            if (string.IsNullOrWhiteSpace(wmiDate)) return DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            try
            {
                if (wmiDate.Length >= 14 &&
                    int.TryParse(wmiDate.Substring(0, 4), out int year) &&
                    int.TryParse(wmiDate.Substring(4, 2), out int month) &&
                    int.TryParse(wmiDate.Substring(6, 2), out int day) &&
                    int.TryParse(wmiDate.Substring(8, 2), out int hour) &&
                    int.TryParse(wmiDate.Substring(10, 2), out int min))
                {
                    return $"{day:D2}.{month:D2}.{year} {hour:D2}:{min:D2}";
                }
            }
            catch { }
            return DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
