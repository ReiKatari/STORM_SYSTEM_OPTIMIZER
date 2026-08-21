using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class ContextMenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string KeyPath { get; set; } = string.Empty;
        public string Location { get; set; } = "Рабочий стол / Папки";
        public bool IsEnabled { get; set; } = true;
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

                    // Restart explorer to apply immediately
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
                new() { Title = "Передать на устройство (Cast to Device)", KeyPath = @"*\shellex\ContextMenuHandlers\PlayTo", Location = "Файлы" },
                new() { Title = "Предоставить доступ к (Share)", KeyPath = @"*\shellex\ContextMenuHandlers\Sharing", Location = "Файлы / Папки" },
                new() { Title = "Восстановить прежнюю версию (Previous Versions)", KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\{596ab062-b4d2-4215-9f74-e9109b0a8153}", Location = "Файлы" },
                new() { Title = "Отправить в (Send To)", KeyPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", Location = "Файлы / Папки" },
                new() { Title = "Включить в библиотеку (Include in Library)", KeyPath = @"Folder\ShellEx\ContextMenuHandlers\Library Location", Location = "Папки" },
                new() { Title = "Закрепить на панели быстрого доступа", KeyPath = @"Folder\ShellEx\ContextMenuHandlers\PintoHome", Location = "Папки" }
            };
            return list;
        }

        public async Task<bool> CleanContextMenuClutterAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Disable 3D Print, Cast to Device, Open With clutter
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
                    p.Kill();
                }
                Process.Start("explorer.exe");
            }
            catch { }
        }
    }
}
