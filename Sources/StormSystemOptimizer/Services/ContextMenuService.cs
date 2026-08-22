using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public partial class ContextMenuItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _keyPath = string.Empty;

        [ObservableProperty]
        private string _location = "Файлы и папки";

        [ObservableProperty]
        private string _category = "Полезные инструменты";

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private string _statusText = "Включен";

        [ObservableProperty]
        private string _statusColor = "#10B981";
    }

    public class ContextMenuService
    {
        private static ContextMenuService? _instance;
        public static ContextMenuService Instance => _instance ??= new ContextMenuService();

        private const string Win11ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

        private ContextMenuService() { }

        public bool IsClassicWindows10MenuEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(Win11ClassicMenuKey);
                return key != null;
            }
            catch { }
            return false;
        }

        public async Task<bool> ToggleWindows11ClassicMenuAsync(bool enableClassic)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (enableClassic)
                    {
                        using var key = Registry.CurrentUser.CreateSubKey(Win11ClassicMenuKey);
                        key?.SetValue("", "");
                    }
                    else
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
                    }

                    RestartExplorer();
                    return true;
                }
                catch { return false; }
            });
        }

        public List<ContextMenuItem> GetPopularContextMenuItems()
        {
            var items = new List<ContextMenuItem>
            {
                // 1. POWER TOOLS
                new() {
                    Title = "Открыть в PowerShell (Администратор)",
                    Description = "Мгновенный запуск консоли с повышенными правами в выбранной папке",
                    KeyPath = @"Directory\Background\shell\PowerShellAdmin",
                    Location = "Папки и Рабочий стол",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"Directory\Background\shell\PowerShellAdmin")
                },
                new() {
                    Title = "Копировать путь к файлу",
                    Description = "Копирование чистого полного пути файла в буфер обмена без кавычек",
                    KeyPath = @"*\shell\CopyPath",
                    Location = "Все файлы",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"*\shell\CopyPath")
                },
                new() {
                    Title = "Владелец и Полный доступ (Take Ownership)",
                    Description = "Получение полных прав NTFS на системные файлы и папки без ограничений TrustedInstaller",
                    KeyPath = @"*\shell\runas",
                    Location = "Все файлы и папки",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"*\shell\runas")
                },
                new() {
                    Title = "Открыть в Блокноте",
                    Description = "Быстрый просмотр исходного кода и логов в стандартном Блокноте",
                    KeyPath = @"*\shell\OpenWithNotepad",
                    Location = "Все файлы",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"*\shell\OpenWithNotepad")
                },
                new() {
                    Title = "Перезапустить Проводник",
                    Description = "Мгновенный перезапуск explorer.exe при зависании оболочки или панели задач",
                    KeyPath = @"DesktopBackground\Shell\RestartExplorer",
                    Location = "Рабочий стол",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"DesktopBackground\Shell\RestartExplorer")
                },
                new() {
                    Title = "Сбросить кэш ОЗУ (Flush RAM)",
                    Description = "Быстрая выгрузка списков ожидания Standby List из контекстного меню рабочего стола",
                    KeyPath = @"DesktopBackground\Shell\FlushRAM",
                    Location = "Рабочий стол",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"DesktopBackground\Shell\FlushRAM")
                },
                new() {
                    Title = "Режим Бога (Панель управления God Mode)",
                    Description = "Все 200+ системных настроек и оснасток Windows в одном удобном окне",
                    KeyPath = @"DesktopBackground\Shell\GodMode",
                    Location = "Рабочий стол",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"DesktopBackground\Shell\GodMode")
                },
                new() {
                    Title = "Хэш-сумма файла (SHA-256 / MD5)",
                    Description = "Быстрый расчет контрольной суммы файла через встроенную утилиту CertUtil",
                    KeyPath = @"*\shell\GetFileHash",
                    Location = "Все файлы",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"*\shell\GetFileHash")
                },
                new() {
                    Title = "Компактная очистка диска (Cleanmgr)",
                    Description = "Запуск встроенной очистки мусора для выбранного диска или раздела",
                    KeyPath = @"Drive\shell\Cleanmgr",
                    Location = "Локальные диски",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"Drive\shell\Cleanmgr")
                },
                new() {
                    Title = "Заблокировать в Брандмауэре Windows",
                    Description = "Блокировка исходящего сетевого трафика для выбранной программы или игры",
                    KeyPath = @"exefile\shell\BlockFirewall",
                    Location = "Файлы (.exe)",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"exefile\shell\BlockFirewall")
                },
                new() {
                    Title = "Создать список файлов папки в TXT",
                    Description = "Экспорт дерева файлов и структуры каталога в текстовый файл list.txt",
                    KeyPath = @"Directory\shell\FileListToTxt",
                    Location = "Папки",
                    Category = "⭐ Инструменты",
                    IsEnabled = CheckRegistryKeyExists(@"Directory\shell\FileListToTxt")
                },

                // 2. DECLUTTER & BLOATWARE REMOVAL
                new() {
                    Title = "Редактировать в Paint 3D (3D Edit)",
                    Description = "Устаревший пункт редактирования 3D для графических изображений (BMP, JPG, PNG)",
                    KeyPath = @"SystemFileAssociations\.png\Shell\3D Edit",
                    Location = "Изображения",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"SystemFileAssociations\.png\Shell\3D Edit")
                },
                new() {
                    Title = "Передать на устройство (PlayTo / DLNA)",
                    Description = "Пункт беспроводной передачи медиапотока на Smart TV",
                    KeyPath = @"*\shellex\ContextMenuHandlers\PlayTo",
                    Location = "Медиафайлы",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"*\shellex\ContextMenuHandlers\PlayTo")
                },
                new() {
                    Title = "Предоставить общий доступ (Modern Sharing)",
                    Description = "Всплывающее меню общего доступа Windows Share",
                    KeyPath = @"*\shellex\ContextMenuHandlers\ModernSharing",
                    Location = "Файлы и папки",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"*\shellex\ContextMenuHandlers\ModernSharing")
                },
                new() {
                    Title = "Восстановить прежнюю версию",
                    Description = "История файлов и предыдущие теневые копии томов",
                    KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\{596ab062-b4d2-4215-9f74-e9109b0a8153}",
                    Location = "Все объекты",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"AllFilesystemObjects\shellex\ContextMenuHandlers\{596ab062-b4d2-4215-9f74-e9109b0a8153}")
                },
                new() {
                    Title = "Пункт «Отправить» (SendTo)",
                    Description = "Вложенное меню отправки на рабочий стол, факс, почту и съемные диски",
                    KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
                    Location = "Файлы и папки",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo")
                },
                new() {
                    Title = "Закрепить на панели быстрого доступа",
                    Description = "Закрепление выбранной папки в левом сайдбаре Проводника",
                    KeyPath = @"Folder\ShellEx\ContextMenuHandlers\PintoHome",
                    Location = "Папки",
                    Category = "🧹 Очистка меню",
                    IsEnabled = CheckRegistryKeyExists(@"Folder\ShellEx\ContextMenuHandlers\PintoHome")
                },

                // 3. 3RD-PARTY APP MENUS
                new() {
                    Title = "Меню видеодрайвера (Intel / NVIDIA на Рабочем столе)",
                    Description = "Пункты настроек графики и панели управления на рабочем столе",
                    KeyPath = @"DesktopBackground\shellex\ContextMenuHandlers\NvCplDesktopContext",
                    Location = "Рабочий стол",
                    Category = "⚙️ Сторонние меню",
                    IsEnabled = CheckRegistryKeyExists(@"DesktopBackground\shellex\ContextMenuHandlers\NvCplDesktopContext")
                },
                new() {
                    Title = "Контекстное меню Git GUI & Git Bash",
                    Description = "Пункты открытия Git-репозитория в текущей папке",
                    KeyPath = @"Directory\Background\shell\git_gui",
                    Location = "Папки и Рабочий стол",
                    Category = "⚙️ Сторонние меню",
                    IsEnabled = CheckRegistryKeyExists(@"Directory\Background\shell\git_gui")
                }
            };

            foreach (var item in items)
            {
                item.StatusText = item.IsEnabled ? "Включен" : "Отключен";
                item.StatusColor = item.IsEnabled ? "#10B981" : "#94A3B8";
            }

            return items;
        }

        public async Task<bool> ToggleItemStateAsync(ContextMenuItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    bool enable = item.IsEnabled;
                    switch (item.Title)
                    {
                        case "Открыть в PowerShell (Администратор)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"Directory\Background\shell\PowerShellAdmin");
                                k?.SetValue("", "Открыть в PowerShell (Администратор)");
                                k?.SetValue("Icon", "powershell.exe");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "powershell.exe -NoExit -Command \"Set-Location '%V'\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\Background\shell\PowerShellAdmin", false);
                            }
                            break;

                        case "Копировать путь к файлу":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"*\shell\CopyPath");
                                k?.SetValue("", "Копировать путь к файлу");
                                k?.SetValue("Icon", "imageres.dll,-5302");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "cmd.exe /c echo \"%1\"|clip");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\CopyPath", false);
                            }
                            break;

                        case "Владелец и Полный доступ (Take Ownership)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"*\shell\runas");
                                k?.SetValue("", "Получить права владельца (Take Ownership)");
                                k?.SetValue("NoWorkingDirectory", "");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "cmd.exe /c takeown /f \"%1\" && icacls \"%1\" /grant administrators:F");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\runas", false);
                            }
                            break;

                        case "Открыть в Блокноте":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"*\shell\OpenWithNotepad");
                                k?.SetValue("", "Открыть в Блокноте");
                                k?.SetValue("Icon", "notepad.exe");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "notepad.exe \"%1\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\OpenWithNotepad", false);
                            }
                            break;

                        case "Перезапустить Проводник":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\RestartExplorer");
                                k?.SetValue("", "Перезапустить Проводник");
                                k?.SetValue("Icon", "explorer.exe");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "powershell.exe -NoProfile -Command \"Stop-Process -ProcessName explorer -Force; Start-Process explorer\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\RestartExplorer", false);
                            }
                            break;

                        case "Сбросить кэш ОЗУ (Flush RAM)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\FlushRAM");
                                k?.SetValue("", "Очистить кэш памяти (Flush RAM)");
                                k?.SetValue("Icon", "imageres.dll,-28");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "powershell.exe -NoProfile -Command \"[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\FlushRAM", false);
                            }
                            break;

                        case "Режим Бога (Панель управления God Mode)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\GodMode");
                                k?.SetValue("", "Режим Бога (Панель управления)");
                                k?.SetValue("Icon", "control.exe");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "explorer.exe shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\GodMode", false);
                            }
                            break;

                        case "Хэш-сумма файла (SHA-256 / MD5)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"*\shell\GetFileHash");
                                k?.SetValue("", "Контрольная сумма файла (SHA-256)");
                                k?.SetValue("Icon", "certmgr.cpl");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "powershell.exe -NoExit -Command \"Get-FileHash -Path '%1' -Algorithm SHA256 | Format-List\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\GetFileHash", false);
                            }
                            break;

                        case "Компактная очистка диска (Cleanmgr)":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"Drive\shell\Cleanmgr");
                                k?.SetValue("", "Очистка диска (Cleanmgr)");
                                k?.SetValue("Icon", "cleanmgr.exe");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "cleanmgr.exe /d %1");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"Drive\shell\Cleanmgr", false);
                            }
                            break;

                        case "Заблокировать в Брандмауэре Windows":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"exefile\shell\BlockFirewall");
                                k?.SetValue("", "Заблокировать в Брандмауэре Windows");
                                k?.SetValue("Icon", "FirewallControlPanel.dll");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "powershell.exe -NoProfile -Command \"New-NetFirewallRule -DisplayName 'STORM Block: %1' -Direction Outbound -Program '%1' -Action Block\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"exefile\shell\BlockFirewall", false);
                            }
                            break;

                        case "Создать список файлов папки в TXT":
                            if (enable)
                            {
                                using var k = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\FileListToTxt");
                                k?.SetValue("", "Создать список файлов в TXT");
                                k?.SetValue("Icon", "imageres.dll,-102");
                                using var cmd = k?.CreateSubKey("command");
                                cmd?.SetValue("", "cmd.exe /c dir \"%1\" /b /s > \"%1\\file_list.txt\"");
                            }
                            else
                            {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\FileListToTxt", false);
                            }
                            break;

                        default:
                            if (!enable)
                            {
                                try { Registry.ClassesRoot.DeleteSubKeyTree(item.KeyPath, false); } catch { }
                            }
                            break;
                    }

                    item.StatusText = item.IsEnabled ? "Включен" : "Отключен";
                    item.StatusColor = item.IsEnabled ? "#10B981" : "#94A3B8";

                    RestartExplorer();
                    return true;
                }
                catch { return false; }
            });
        }

        private static bool CheckRegistryKeyExists(string relativePath)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(relativePath);
                return key != null;
            }
            catch { return false; }
        }

        public async Task<bool> CleanContextMenuClutterAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string[] keysToRemove = new[]
                    {
                        @"SystemFileAssociations\.bmp\Shell\3D Edit",
                        @"SystemFileAssociations\.png\Shell\3D Edit",
                        @"SystemFileAssociations\.jpg\Shell\3D Edit",
                        @"SystemFileAssociations\.jpeg\Shell\3D Edit",
                        @"*\shellex\ContextMenuHandlers\ModernSharing",
                        @"*\shellex\ContextMenuHandlers\PlayTo"
                    };

                    foreach (var k in keysToRemove)
                    {
                        try { Registry.ClassesRoot.DeleteSubKeyTree(k, false); } catch { }
                        try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{k}", false); } catch { }
                    }

                    RestartExplorer();
                    return true;
                }
                catch { return false; }
            });
        }

        private static void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); p.WaitForExit(1000); } catch { }
                }
                Process.Start("explorer.exe");
            }
            catch { }
        }
    }
}
