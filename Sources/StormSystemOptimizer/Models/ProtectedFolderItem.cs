using System;

namespace StormSystemOptimizer.Models
{
    public enum ProtectionMode
    {
        StealthOnly,        // 1. Скрыто без пароля
        PasswordLockOnly,   // 2. Запаролено без скрытия (Блокировка доступа)
        StealthAndPassword  // 3. Скрыто и запаролено (Максимальная защита)
    }

    public class ProtectedFolderItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FolderPath { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public ProtectionMode Mode { get; set; } = ProtectionMode.StealthAndPassword;
        public bool IsLocked { get; set; } = true;
        public string SizeFormatted { get; set; } = "—";
        public int FileCount { get; set; } = 0;
        public DateTime ProtectedAt { get; set; } = DateTime.Now;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;

        public string ModeTitle => Mode switch
        {
            ProtectionMode.StealthOnly => "👁️ Только скрытие",
            ProtectionMode.PasswordLockOnly => "🔒 Только пароль (Блокировка доступа)",
            ProtectionMode.StealthAndPassword => "🛡️ Скрыто и Запаролено (Max Security)",
            _ => "Защита"
        };

        public string StatusBadge => IsLocked ? "🔒 ЗАБЛОКИРОВАНО" : "🔓 ДОСТУПНО";

        public string StatusColor => IsLocked ? "#EF4444" : "#10B981";
        public string StatusBgColor => IsLocked ? "#26EF4444" : "#2610B981";

        public string ModeDescription => Mode switch
        {
            ProtectionMode.StealthOnly => "Папка скрыта от проводника и системного поиска через системные атрибуты.",
            ProtectionMode.PasswordLockOnly => "Папка видима, но при попытке открытия Windows блокирует доступ без пароля.",
            ProtectionMode.StealthAndPassword => "Папка полностью невидима и защищена строгим запретом доступа на уровне NTFS.",
            _ => ""
        };
    }
}
