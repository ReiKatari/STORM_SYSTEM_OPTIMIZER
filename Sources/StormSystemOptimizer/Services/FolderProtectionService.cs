using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class FolderProtectionService
    {
        private static FolderProtectionService? _instance;
        public static FolderProtectionService Instance => _instance ??= new FolderProtectionService();

        private readonly string _dbPath;
        private List<ProtectedFolderItem> _protectedFolders = new();

        private FolderProtectionService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StormSystemOptimizer");
            Directory.CreateDirectory(appData);
            _dbPath = Path.Combine(appData, "vault.json");
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    string json = File.ReadAllText(_dbPath);
                    _protectedFolders = JsonSerializer.Deserialize<List<ProtectedFolderItem>>(json) ?? new();
                }
            }
            catch
            {
                _protectedFolders = new();
            }
        }

        private void SaveDatabase()
        {
            try
            {
                string json = JsonSerializer.Serialize(_protectedFolders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dbPath, json);
            }
            catch { }
        }

        public List<ProtectedFolderItem> GetProtectedFolders()
        {
            return _protectedFolders.ToList();
        }

        public async Task<(bool Success, string Message)> ProtectFolderAsync(string folderPath, ProtectionMode mode, string password)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(folderPath))
                {
                    return (false, "Указанная папка не существует.");
                }

                if (_protectedFolders.Any(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "Данная папка уже находится под защитой.");
                }

                // Compute size and file count
                long totalBytes = 0;
                int filesCount = 0;
                try
                {
                    var dir = new DirectoryInfo(folderPath);
                    var files = dir.GetFiles("*", SearchOption.AllDirectories);
                    filesCount = files.Length;
                    totalBytes = files.Sum(f => f.Length);
                }
                catch { }

                string sizeFormatted = totalBytes >= (1024 * 1024 * 1024)
                    ? $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F1} ГБ"
                    : $"{totalBytes / (1024.0 * 1024.0):F1} МБ";

                string salt = Guid.NewGuid().ToString("N");
                string hash = HashPassword(password, salt);

                var item = new ProtectedFolderItem
                {
                    FolderPath = folderPath,
                    FolderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    Mode = mode,
                    IsLocked = true,
                    SizeFormatted = sizeFormatted,
                    FileCount = filesCount,
                    ProtectedAt = DateTime.Now,
                    PasswordHash = hash,
                    Salt = salt
                };

                // Apply Lock
                ApplyLock(item);

                _protectedFolders.Add(item);
                SaveDatabase();

                return (true, $"Папка «{item.FolderName}» успешно защищена в режиме «{item.ModeTitle}».");
            });
        }

        public async Task<(bool Success, string Message)> UnlockFolderAsync(ProtectedFolderItem item, string password)
        {
            return await Task.Run(() =>
            {
                if (item.Mode != ProtectionMode.StealthOnly)
                {
                    string verifyHash = HashPassword(password, item.Salt);
                    if (!string.Equals(verifyHash, item.PasswordHash, StringComparison.Ordinal))
                    {
                        return (false, "Неверный пароль разблокировки!");
                    }
                }

                RemoveLock(item);
                item.IsLocked = false;
                SaveDatabase();

                return (true, $"Папка «{item.FolderName}» успешно разблокирована и доступна.");
            });
        }

        public async Task<(bool Success, string Message)> LockFolderAsync(ProtectedFolderItem item)
        {
            return await Task.Run(() =>
            {
                ApplyLock(item);
                item.IsLocked = true;
                SaveDatabase();

                return (true, $"Папка «{item.FolderName}» повторно заблокирована.");
            });
        }

        public async Task<(bool Success, string Message)> RemoveProtectionAsync(ProtectedFolderItem item, string password)
        {
            return await Task.Run(() =>
            {
                if (item.IsLocked && item.Mode != ProtectionMode.StealthOnly)
                {
                    string verifyHash = HashPassword(password, item.Salt);
                    if (!string.Equals(verifyHash, item.PasswordHash, StringComparison.Ordinal))
                    {
                        return (false, "Неверный пароль для снятия защиты!");
                    }
                }

                RemoveLock(item);
                _protectedFolders.Remove(item);
                SaveDatabase();

                return (true, $"Защита с папки «{item.FolderName}» полностью снята.");
            });
        }

        private void ApplyLock(ProtectedFolderItem item)
        {
            try
            {
                // 1. Stealth mode: Set Hidden + System attributes
                if (item.Mode == ProtectionMode.StealthOnly || item.Mode == ProtectionMode.StealthAndPassword)
                {
                    var di = new DirectoryInfo(item.FolderPath);
                    di.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                }

                // 2. Access Lock: Deny access via NTFS ACLs (icacls)
                if (item.Mode == ProtectionMode.PasswordLockOnly || item.Mode == ProtectionMode.StealthAndPassword)
                {
                    string user = Environment.UserName;
                    RunIcacls($"\"{item.FolderPath}\" /deny \"{user}\":(OI)(CI)(F)");
                }
            }
            catch { }
        }

        private void RemoveLock(ProtectedFolderItem item)
        {
            try
            {
                // 1. Remove NTFS ACL deny
                if (item.Mode == ProtectionMode.PasswordLockOnly || item.Mode == ProtectionMode.StealthAndPassword)
                {
                    string user = Environment.UserName;
                    RunIcacls($"\"{item.FolderPath}\" /remove:d \"{user}\"");
                    RunIcacls($"\"{item.FolderPath}\" /grant \"{user}\":(OI)(CI)(F)");
                }

                // 2. Remove Hidden + System attributes
                if (item.Mode == ProtectionMode.StealthOnly || item.Mode == ProtectionMode.StealthAndPassword)
                {
                    var di = new DirectoryInfo(item.FolderPath);
                    di.Attributes &= ~FileAttributes.Hidden;
                    di.Attributes &= ~FileAttributes.System;
                }
            }
            catch { }
        }

        private void RunIcacls(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "icacls.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(3000);
            }
            catch { }
        }

        private string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password + salt + "STORM_VAULT_SECURE_2026");
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
