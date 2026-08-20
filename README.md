# STORM SYSTEM OPTIMIZER ⚡

<p align="center">
  <img src="Files/StormLogo.png" alt="STORM SYSTEM OPTIMIZER Logo" width="220" />
</p>

<p align="center">
  <b>Высокопроизводительное, профессиональное и 100% безопасное приложение для полного сканирования системы Windows, глубокого анализа процессов, ускорения интернета, выявления тормозов, очистки кэшей и оптимизации производительности в 1 клик.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/Framework-WPF%20%2F%20.NET%208%20LTS-00D2FF?style=for-the-badge&logo=windows-terminal" />
  <img src="https://img.shields.io/badge/Language-C%23%20%2F%20.NET-7928CA?style=for-the-badge&logo=c-sharp" />
  <img src="https://img.shields.io/badge/Version-v0.0.2-10B981?style=for-the-badge" />
</p>

---

## ⚡ О проекте

**STORM SYSTEM OPTIMIZER v0.0.2** — это системный оптимизатор нового поколения на нативном движке **WPF (.NET 8 LTS)**. Программа проводит глубокий многопоточный анализ всей системы, предоставляет интерактивный менеджер процессов с базой знаний и рекомендациями, осуществляет глубокую оптимизацию TCP/IP и DNS сетевого стека, очищает скрытый мусор и оптимизирует службы без риска повреждения Windows.

---

## 🚀 Новые возможности в версии 0.0.2

1. **🌐 Глубокая оптимизация и ускорение интернета**:
   - Комплексный тюнинг TCP Window Auto-Tuning (`autotuninglevel=normal`), аппаратной разгрузки (`RSS`/`RSC`), `CTCP Congestion Provider`, `ECN Capability` и `TCPNoDelay` (снижение игрового инпут-лага).
   - Снятие скрытого 20% резервирования полосы пропускания Windows QoS (`NonBestEffortLimit = 0`).
   - Отключение мультимедийного троттлинга сети (`NetworkThrottlingIndex = 0xFFFFFFFF`, `SystemResponsiveness = 0`).
   - Быстрое переключение проверенных сверхскоростных DNS в 1 клик: **Cloudflare (1.1.1.1)**, **Google (8.8.8.8)**, **Quad9 Security (9.9.9.9)**, **AdGuard (94.140.14.14)** и автоматический DHCP.
   - Встроенный бенчмарк задержки DNS-серверов и замер пинга в реальном времени.

2. **📊 Менеджер процессов с глубоким исследованием и рекомендациями**:
   - Встроенная база знаний сотен процессов Windows, лаунчеров, браузеров и утилит с подробными описаниями на русском языке.
   - Классификация по уровням безопасности:
     - 🟢 **Безопасно завершить** (фоновые игровые клиенты Steam/Epic, bloatware, тяжелые вкладки)
     - 🟡 **Пользовательское ПО** (активные прикладные программы)
     - 🔴 **Системный процесс Windows** (критические системные компоненты `csrss`, `dwm`, `services` — встроенная защита от случайного завершения)
   - Функции «⚡ Очистить все фоновые процессы в 1 клик», «Завершить дерево процессов» и «Открыть расположение файла».

3. **🔄 Система автообновления с GitHub**:
   - Автоматическая проверка обновлений при старте и ручная кнопка в заголовке / настройках.
   - Корректный сценарий обновления: загрузка новой версии, аккуратное завершение текущего приложения, установка и автоматический перезапуск.

4. **✨ Новый стиль элементов управления**:
   - Все чекбоксы заменены на минималистичные **неоновые залитые квадраты (Solid Filled Glow Squares)** без галочек.

---

## 🎨 4 Эксклюзивные темы оформления

1. **`STORM DARK`** (По умолчанию) — Кибер-тёмная тема (`#10141D`) с неоновыми акцентами электрического шторма (`#00D2FF`).
2. **`STORM NIGHT`** — Глубокий чистый OLED чёрный (`#000000`) с ярким неоновым свечением шторма (`#00F0FF`).
3. **`STORM DAY`** — Дневная светлая тема (`#F4F7FB`) с высокой контрастностью и акцентами океанического шторма.
4. **`STORM MIDNIGHT`** — Космическая фиолетовая тема (`#0C0A1D`) с неоновым свечением аметиста (`#A855F7`).

---

## 📁 Структура репозитория

```text
E:\STORM SYSTEM OPTIMIZER\
├── Sources\                  # Полные исходные коды C# WPF .NET 8 решения
│   ├── StormSystemOptimizer\
│   │   ├── Assets\           # Мультиформатные иконки и логотипы
│   │   ├── Themes\           # 4 XAML словаря тем и ThemeManager
│   │   ├── Models\           # Модели данных, процессы, классификаторы рисков
│   │   ├── Services\         # Движки сканирования, сетевой тюнинг, UpdateService
│   │   ├── ViewModels\       # MVVM модели представления
│   │   └── Views\            # Страницы (Dashboard, Processes, Network, Scanner, etc.)
├── Assembling\               # Скомпилированная автономная программа (StormSystemOptimizer.exe)
├── Files\                    # Готовый инсталлятор (StormSystemOptimizer_Setup_v0.0.2.exe)
└── README.md
```

---

## 🛠️ Сборка из исходников

### Требования:
- Windows 10 (версия 1809+) или Windows 11
- .NET 8.0 SDK

### Команды для сборки:
```powershell
# Клонирование репозитория
git clone https://github.com/ReiKatari/STORM_SYSTEM_OPTIMIZER.git
cd STORM_SYSTEM_OPTIMIZER/Sources/StormSystemOptimizer

# Сборка Release
dotnet build -c Release

# Публикация автономного релиза (Self-Contained)
dotnet publish -c Release -r win-x64 --self-contained true -o "../../Assembling"
```

---

## 📦 Установка

Запустите установочный файл `Files\StormSystemOptimizer_Setup_v0.0.2.exe`:
- Автоматическая распаковка в `C:\Program Files\Storm System Optimizer`.
- Создание ярлыков на Рабочем столе и в меню «Пуск» с фирменной иконкой.
- Полная регистрация в реестре Windows с поддержкой чистого удаления через деинсталлятор.

---

## 👤 Автор

- **ReiKatari** — [GitHub](https://github.com/ReiKatari)
- **STORM Software** © 2026. Все права защищены.
