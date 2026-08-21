using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool _isEnabled = true;
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
                        key?.SetValue("", ""); // Empty default value
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
            var list = new List<ContextMenuItem>
            {
                new() { Title = "Открыть в Блокноте", Description = "Быстрый просмотр любого файла в блокноте", KeyPath = @"*\shell\OpenWithNotepad", Location = "Все файлы", IsEnabled = CheckRegistryKeyExists(@"*\shell\OpenWithNotepad") },
                new() { Title = "Копировать путь к файлу", Description = "Копирование полного пути в буфер обмена без кавычек", KeyPath = @"*\shell\CopyPath", Location = "Все файлы", IsEnabled = CheckRegistryKeyExists(@"*\shell\CopyPath") },
                new() { Title = "Панель управления God Mode", Description = "Доступ ко всем системным настройкам Windows в один клик", KeyPath = @"DesktopBackground\Shell\GodMode", Location = "Рабочий стол", IsEnabled = CheckRegistryKeyExists(@"DesktopBackground\Shell\GodMode") },
                new() { Title = "Открыть Командную строку здесь", Description = "Запуск классической консоли cmd в текущей папке", KeyPath = @"Directory\Background\shell\cmdprompt", Location = "Папки и Рабочий стол", IsEnabled = CheckRegistryKeyExists(@"Directory\Background\shell\cmdprompt") },
                new() { Title = "Передать на устройство воспроизведения", Description = "Потоковая трансляция медиафайлов по сети DLNA", KeyPath = @"*\shellex\ContextMenuHandlers\PlayTo", Location = "Медиафайлы", IsEnabled = !CheckRegistryKeyExists(@"*\shellex\ContextMenuHandlers\PlayTo\Blocked") },
                new() { Title = "Предоставить общий доступ", Description = "Мастер публикации файлов в локальной сети", KeyPath = @"*\shellex\ContextMenuHandlers\Sharing", Location = "Файлы и папки", IsEnabled = true },
                new() { Title = "Восстановить прежнюю версию", Description = "История файлов и теневые копии тома", KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\{596ab062-b4d2-4215-9f74-e9109b0a8153}", Location = "Файлы", IsEnabled = true },
                new() { Title = "Отправить в сторонние приложения", Description = "Меню быстрой отправки в адресаты", KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", Location = "Файлы и папки", IsEnabled = true },
                new() { Title = "Закрепить на панели быстрого доступа", Description = "Закрепление в левой панели Проводника", KeyPath = @"Folder\ShellEx\ContextMenuHandlers\PintoHome", Location = "Папки", IsEnabled = true }
            };
            return list;
        }

        public async Task<bool> ToggleItemStateAsync(ContextMenuItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    bool enable = item.IsEnabled;
                    if (item.Title == "Открыть в Блокноте")
                    {
                        if (enable)
                        {
                            using var key = Registry.ClassesRoot.CreateSubKey(@"*\shell\OpenWithNotepad");
                            key?.SetValue("", "Открыть в Блокноте");
                            key?.SetValue("Icon", "notepad.exe");
                            using var cmd = key?.CreateSubKey("command");
                            cmd?.SetValue("", "notepad.exe \"%1\"");
                        }
                        else
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\OpenWithNotepad", false);
                        }
                    }
                    else if (item.Title == "Копировать путь к файлу")
                    {
                        if (enable)
                        {
                            using var key = Registry.ClassesRoot.CreateSubKey(@"*\shell\CopyPath");
                            key?.SetValue("", "Копировать путь к файлу");
                            key?.SetValue("Icon", "imageres.dll,-5302");
                            using var cmd = key?.CreateSubKey("command");
                            cmd?.SetValue("", "cmd.exe /c echo \"%1\"|clip");
                        }
                        else
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\CopyPath", false);
                        }
                    }
                    else if (item.Title == "Панель управления God Mode")
                    {
                        if (enable)
                        {
                            using var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\GodMode");
                            key?.SetValue("", "Режим Бога (Панель управления)");
                            key?.SetValue("Icon", "control.exe");
                            using var cmd = key?.CreateSubKey("command");
                            cmd?.SetValue("", "explorer.exe shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
                        }
                        else
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\GodMode", false);
                        }
                    }
                    else if (item.Title == "Открыть Командную строку здесь")
                    {
                        if (enable)
                        {
                            using var key = Registry.ClassesRoot.CreateSubKey(@"Directory\Background\shell\cmdprompt");
                            key?.SetValue("", "Открыть Командную строку здесь");
                            key?.SetValue("Icon", "cmd.exe");
                            using var cmd = key?.CreateSubKey("command");
                            cmd?.SetValue("", "cmd.exe /s /k pushd \"%V\"");
                        }
                        else
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\Background\shell\cmdprompt", false);
                        }
                    }

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
                        @"*\shellex\ContextMenuHandlers\ModernSharing"
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
