using System;
using System.Collections.Generic;
using System.Linq;

namespace StormSystemOptimizer.Services
{
    public class SearchResultItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TargetTag { get; set; } = string.Empty;
        public string IconKey { get; set; } = "GeoDashboard";
        public string Keywords { get; set; } = string.Empty;
    }

    public class SearchIndexService
    {
        private static SearchIndexService? _instance;
        public static SearchIndexService Instance => _instance ??= new SearchIndexService();

        private readonly List<SearchResultItem> _items = new();

        private SearchIndexService()
        {
            InitializeIndex();
        }

        private void InitializeIndex()
        {
            _items.Clear();

            // 1. Быстрый старт и мониторинг
            AddItem("Быстрое обслуживание", "Комплексная оптимизация системы в 1 клик", "Быстрый старт", "QuickMaintenance", "GeoQuickMaintenance", "быстрое обслуживание очистка оптимизация 1 клик smart booster");
            AddItem("Панель состояния", "Обзор загрузки процессора, оперативной памяти, GPU и дисков", "Мониторинг", "Dashboard", "GeoDashboard", "панель состояния дашборд мониторинг cpu ram gpu диск нагрузка");
            AddItem("Информация о системе", "Детальные характеристики процессора, материнской платы, BIOS, видеокарты и ОС", "Система", "SystemInfo", "GeoSystemInfo", "информация о системе спецификация пк bios материнская плата процессор видеокарта directx");
            AddItem("Тесты и сенсоры оборудования", "Бенчмарки процессора, памяти, замер задержек DPC и датчики температуры", "Тестирование", "Benchmarks", "GeoBenchmarks", "тесты бенчмарк сенсоры температура dpc latency стресс тест");

            // 2. Накопители и очистка
            AddItem("Диски и SMART накопители", "Состояние здоровья SSD/HDD, температура, ресурс перезаписи и дефрагментация", "Диски", "Disks", "GeoDisks", "диски smart ssd hdd здоровье накопителей ресурс дефрагментация trim");
            AddItem("Глубокая очистка дисков", "Очистка системного кэша, дампов, временных файлов, логов и кэша DirectX", "Очистка", "Scanner", "GeoScanner", "глубокая очистка дисков сканер мусор temp кэш логи directx winsxs дампы");
            AddItem("Оптимизация баз данных SQLite", "Дефрагментация и компактизация баз данных браузеров, Telegram, Discord, Steam (VACUUM & REINDEX)", "Базы данных", "DatabaseOptimizer", "GeoDatabase", "sqlite базы данных vacuum reindex дефрагментация chrome edge firefox telegram discord steam");
            AddItem("Защита личных папок", "Блокировка и скрытие конфиденциальных каталогов и файлов", "Безопасность", "FolderProtection", "GeoFolderLock", "защита папок блокировка пароль скрыть конфиденциальность");

            // 3. Производительность и игры
            AddItem("Управление памятью и файлом подкачки", "Очистка Standby списка, настройка размера файла подкачки (Pagefile) и сжатия памяти", "Память", "MemoryMaster", "GeoPagefile", "память файл подкачки pagefile очистка standby сжатие ram memory master");
            AddItem("Управление автозагрузкой", "Менеджер автозапуска программ, служб, запланированных задач и реестра", "Оптимизация", "Startup", "GeoStartup", "автозагрузка стартап автозапуск реестр службы задачи msconfig");
            AddItem("Время загрузки и автозапуск", "Анализ времени старта Windows и оптимизация задержек при включении ПК", "Оптимизация", "BootProfiler", "GeoTimer", "время загрузки boot profiler старт windows профиль ускорение старта");
            AddItem("Оптимизация задержки ввода", "Снижение Input Lag мыши и клавиатуры, отключение акселерации, MarkC Fix, размер очередей", "Input Lag", "InputLag", "GeoDashboard", "input lag задержка ввода мышь клавиатура markc акселерация mousedataqueuesize keyboarddataqueuesize отклик");
            AddItem("Задержки аудио и MMCSS", "Тюнинг приоритетов звукового стека Windows, устранение треска и щелчков звука", "Аудио", "AudioLatency", "GeoAudio", "задержки аудио mmcss звук dpc latency задержка звуковой карты треск щелчки");
            AddItem("Питание USB и отклик", "Отключение энергосбережения контроллеров USB и ускорение опроса портов", "Оборудование", "UsbPolling", "GeoUsb", "питание usb usb polling selective suspend частота опроса мыши отклик");
            AddItem("Ядра и прерывания DPC/ISR", "Изоляция прерываний GPU/NIC/USB по ядрам CPU, модерация XHCI IMOD, сетевой приоритет QoS DSCP 46", "Прерывания", "InterruptAffinity", "GeoInterrupts", "прерывания dpc isr affinity core cpu mask gpu nic usb xhci imod qos dscp 46 latency микрофризы");

            // 4. Сеть и интернет
            AddItem("Ускорение интернета и DNS", "Тюнинг сетевого стека TCP/IP, DNS-бенчмарк, защита от телеметрии Blackhole", "Сеть", "Network", "GeoNetwork", "ускорение интернета dns tcpip nagle blackhole ping пинг замер скорости");
            AddItem("Инструменты и порты ОС", "Запуск от TrustedInstaller, центр библиотек VC++/DirectX/.NET, сканер активных портов", "Инструменты", "SystemTools", "GeoSystemTools", "инструменты и порты ос trustedinstaller доверенный установщик библиотеки vcredist directx dotnet порты sfc dism");

            // 5. Оболочка, интерфейс и приложения
            AddItem("Визуализация и эффекты DWM", "Настройка визуальных эффектов рабочего стола, теней, анимаций и быстродействия", "Интерфейс", "VisualPerformance", "GeoVisual", "визуализация dwm эффекты анимации тени быстродействие интерфейс");
            AddItem("Проводник и оболочка Windows", "Классическое контекстное меню Windows 11, скрытие стрелок ярлыков, MenuShowDelay = 0", "Проводник", "ExplorerTweaks", "GeoExplorer", "проводник explorer классическое меню windows 11 menushowdelay стрелки ярлыков aero shake расширения");
            AddItem("Контекстное меню Windows", "Расширения контекстного меню: Копировать путь, Открыть в Блокноте, Заблокировать в брандмауэре, Take Ownership", "Интерфейс", "ContextMenu", "GeoSettings", "контекстное меню take ownership копировать путь блокнот брандмауэр проводник");
            AddItem("Тюнинг и очистка браузеров", "Очистка кэша, куки, истории и SQLite-вакуумирование 7 популярных браузеров", "Браузеры", "BrowserTurbo", "GeoBrowser", "браузеры chrome yandex edge firefox opera brave очистка кэша sqlite вакуум");
            AddItem("Менеджер игровых лаунчеров", "Очистка кэша шейдеров и временных файлов Steam, Epic Games, GOG, Battle.net, EA, Ubisoft", "Игры", "GameLaunchers", "GeoLaunchers", "лаунчеры steam epic games gog battlenet ea ubisoft кэш шейдеров игры");

            // 6. Безопасность и компоненты
            AddItem("Управление защитником и безопасность", "Настройка Microsoft Defender, исключения, SmartScreen, изоляция ядра VBS/HVCI", "Безопасность", "DefenderTweaker", "GeoDefender", "защитник defender антивирус smartscreen vbs hvci изоляция ядра исключения");
            AddItem("Твики приватности и слежки", "Блокировка 1400+ хостов слежки, отключение телеметрии NVIDIA/Intel, Copilot, Recall, кейлоггеров", "Приватность", "Privacy", "GeoPrivacy", "приватность телеметрия слежка hosts брандмауэр nvidia intel copilot recall кейлоггер реклама cortana");
            AddItem("Службы Windows", "Управление системными службами, готовые безопасные и игровые профили", "Службы", "Services", "GeoServices", "службы windows services diagtrack профили отключение служб оптимизация");
            AddItem("Менеджер обновлений и компонентов", "Управление центром обновлений Windows, компонентами Windows Sandbox, Hyper-V, WSL", "Компоненты", "UpdateComponent", "GeoComponent", "обновления windows update компоненты sandbox hyper-v wsl directplay net framework");
            AddItem("Тюнинг питания и ядер", "Схема «Максимальная производительность» (Ultimate Performance), парковка ядер, HAGS, GameDVR", "Питание", "PowerTuning", "GeoPower", "питание тюнинг ядер ultimate performance максимальная производительность hags gamedvr парковка ядер");
            AddItem("Безопасность и аудит", "Сканер скрытых майнеров, аудит правил Брандмауэра и безвозвратный шредер файлов DoD 5220.22-M", "Безопасность", "SecurityAudit", "GeoSecurityAudit", "майнеры вирусы шредер файлов уничтожить файл брандмауэр firewall audit hosts wmi инжекты");

            // 7. Обслуживание и утилиты
            AddItem("Центр Microsoft Office", "Выборочная C2R-установка Office 2016-2024, KMS-активатор, GVLK лицензии и глубокая зачистка", "Office", "OfficeDeployer", "GeoOffice", "office ворд эксель поверпоинт word excel powerpoint visio project c2r kms активация лицензия gvlk удалить офис");
            AddItem("Обновление программ", "Сканирование установленного софта (реестр 32/64-bit, WinGet, AppX) и обновление в 1 клик", "Обновление", "SoftwareUpdater", "GeoAppUpdate", "обновление программ software updater winget софт апдейтер новые версии");
            AddItem("Удаление программ", "Глубокое удаление приложений, вырезание Edge, OneDrive, UWP-хлама с очисткой хвостов", "Удаление", "Uninstaller", "GeoUninstaller", "удаление программ деинсталлятор uninstaller edge onedrive uwp debloat вырезание");
            AddItem("Обновление драйверов и BIOS", "Анализ версий установленных драйверов устройств и поиск актуальных обновлений", "Драйверы", "Drivers", "GeoDrivers", "драйверы обновление драйверов bios видеокарта чипсет звук сеть");
            AddItem("Настройки BIOS и материнской платы", "Рекомендации по настройке UEFI, XMP/EXPO, TPM, Secure Boot и ReBAR", "BIOS", "BiosOptimizer", "GeoBios", "bios uefi материнская плата xmp expo rebar tpm secure boot");
            AddItem("Менеджер процессов", "Диспетчер задач нового поколения с отображением потоков, дескрипторов и приоритетов", "Процессы", "Processes", "GeoProcesses", "процессы диспетчер задач cpu память завершить процесс потоки");
            AddItem("Центр бэкапов и восстановление", "Создание точек восстановления Windows, бэкап реестра и системных настроек", "Резервные копии", "BackupVault", "GeoShield", "бэкап восстановление точка восстановления snapshot реестр backup vault");
            AddItem("Разблокировка файлов и папок", "Освобождение заблокированных файлов и завершение удерживающих процессов (Unlocker)", "Утилиты", "FileUnlocker", "GeoUnlocker", "разблокировка файлов unlocker занят другим процессом удалить заблокированный файл");
            AddItem("Настройки приложения", "Настройка автозапуска STORM Optimizer, темы оформления, горячих клавиш и уведомлений", "Настройки", "Settings", "GeoSettings", "настройки тема автозапуск горячие клавиши overlay hud tray трей");
        }

        private void AddItem(string title, string desc, string category, string targetTag, string iconKey, string keywords)
        {
            _items.Add(new SearchResultItem
            {
                Title = title,
                Description = desc,
                Category = category,
                TargetTag = targetTag,
                IconKey = iconKey,
                Keywords = (title + " " + desc + " " + category + " " + keywords).ToLowerInvariant()
            });
        }

        public List<SearchResultItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchResultItem>();

            string q = query.Trim().ToLowerInvariant();
            string[] words = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return _items.Where(item =>
            {
                return words.All(w => item.Keywords.Contains(w));
            }).Take(12).ToList();
        }
    }
}
