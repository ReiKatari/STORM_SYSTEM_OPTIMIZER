# STORM SYSTEM OPTIMIZER ⚡

<p align="center">
  <img src="Files/StormLogo.png" alt="STORM SYSTEM OPTIMIZER Logo" width="220" />
</p>

<p align="center">
  <b>Высокопроизводительное, профессиональное и 100% безопасное приложение для полного сканирования системы Windows, выявления тормозов, очистки кэшей и оптимизации производительности в 1 клик.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/Framework-WinUI%203%20%2F%20Windows%20App%20SDK-00D2FF?style=for-the-badge&logo=windows-terminal" />
  <img src="https://img.shields.io/badge/Language-C%23%20%2F%20.NET-7928CA?style=for-the-badge&logo=c-sharp" />
  <img src="https://img.shields.io/badge/Version-v0.0.1-10B981?style=for-the-badge" />
</p>

---

## ⚡ О проекте

**STORM SYSTEM OPTIMIZER** — это современное системное приложение нового поколения, разработанное на стеке **WinUI 3 (Windows App SDK / .NET)**. Программа проводит глубокий многопоточный анализ всей системы, выявляет ресурсоемкие фоновые службы, мусор, скрытую телеметрию и троттлинг сети, предлагая интеллектуальное и безопасное устранение проблем без риска нарушения работы Windows.

---

## 🎨 4 Эксклюзивные темы оформления

Приложение включает полностью кастомизированную систему стилей для всех окон, карточек, списков, элементов управления и модальных диалогов:

1. **`STORM DARK`** (По умолчанию) — Кибер-тёмная тема (`#10141D`) с неоновыми акцентами электрического шторма (`#00D2FF`) и ультрафиолета (`#7928CA`).
2. **`STORM NIGHT`** — Глубокий чистый OLED чёрный (`#000000`) с ярким неоновым свечением шторма (`#00F0FF`) и повышенным контрастом.
3. **`STORM DAY`** — Элегантная дневная светлая тема (`#F4F7FB`) с высокой четкостью текста и синими акцентами океанического шторма.
4. **`STORM MIDNIGHT`** — Космическая фиолетовая тема (`#0C0A1D`) с неоновым свечением аметиста и ультрафиолета (`#A855F7`).

---

## 🚀 Основные возможности

### 1. ⚡ Панель состояния (Live Dashboard) & Экспресс-буст
- Мониторинг CPU, RAM (включая Standby-кэш), системного диска C: и времени аптайма в реальном времени.
- Индекс здоровья системы (Health Score 0-100).
- Кнопка **«STORM BOOST»** — мгновенное освобождение неиспользуемой оперативной памяти и сброс DNS в 1 клик.

### 2. 🔍 Глубокое многопоточное сканирование всей системы
- **Системный мусор и кэши**: Windows Temp, User Temp, Prefetch, Crash Dumps, Windows Error Reporting, Chromium/Edge/Brave кэши, Delivery Optimization.
- **Оперативная память**: Сброс Standby-листа и оптимизация Working Set запущенных процессов через нативные API Windows NT.
- **Менеджер автозагрузки**: Анализ влияния программ при запуске (High / Medium / Low), удобное включение/отключение без удаления файлов.
- **Оптимизация служб Windows**: Готовые пресеты (*Gaming, Extreme, Balanced, Default Windows*) для безопасного отключения телеметрии (`DiagTrack`, `dmwappushservice`, `RemoteRegistry`, `WerSvc`, `MapsBroker`).
- **Сетевой стек и DNS**: Сброс кэша DNS Resolver, тюнинг TCP Window Auto-Tuning, RSS и Congestion Provider (CTCP), отключение сетевого троттлинга Windows.
- **Приватность и анти-слежение**: Отключение сбора диагностических данных (`AllowTelemetry`), Advertising ID и Activity History.
- **Здоровье накопителей (TRIM)**: Запуск команды TRIM для поддержания максимальной скорости записи на SSD.
- **План электропитания**: Активация скрытого плана «Максимальная производительность» (Ultimate Performance) и снижение задержки интерфейса до 10 мс.

### 3. 🛡️ 100% Безопасность и Защита ОС
- Все найденные проблемы классифицируются по категориям риска: `[100% БЕЗОПАСНО]`, `[РЕКОМЕНДУЕТСЯ]` и `[ПРОДВИНУТЫЙ]`.
- Функция создания контрольной точки восстановления Windows (System Restore Point).

### 4. 🎛️ Интеграция с System Tray
- Сворачивание в область уведомлений панели задач с фирменной иконкой.
- Фоновый мониторинг и всплывающие уведомления об освобожденной памяти.

---

## 📁 Структура репозитория

```text
E:\STORM SYSTEM OPTIMIZER\
├── Sources\                  # Полные исходные коды C# WinUI 3 решения (StormSystemOptimizer.sln)
│   ├── StormSystemOptimizer\
│   │   ├── Assets\           # Мультиформатные иконки и логотипы
│   │   ├── Themes\           # 4 XAML словаря тем (Dark, Night, Day, Midnight) и ThemeManager
│   │   ├── Models\           # Модели данных и классификаторы рисков
│   │   ├── Services\         # Высокопроизводительные движки сканирования и Win32 API
│   │   ├── ViewModels\       # MVVM модели представления
│   │   ├── Views\            # Страницы WinUI 3 (Dashboard, Scanner, Services, etc.)
│   │   └── Controls\         # Стилизованные диалоги и компоненты
├── Assembling\               # Скомпилированная автономная программа (StormSystemOptimizer.exe)
├── Files\                    # Готовый инсталлятор (StormSystemOptimizer_Setup_v0.0.1.exe)
└── README.md
```

---

## 🛠️ Сборка из исходников

### Требования:
- Windows 10 (версия 1809+) или Windows 11
- .NET 8 / 9 / 10 SDK
- Windows App SDK / WinUI 3

### Команды для сборки:
```powershell
# Клонирование репозитория
git clone https://github.com/ReiKatari/STORM_SYSTEM_OPTIMIZER.git
cd STORM_SYSTEM_OPTIMIZER/Sources

# Сборка Release
dotnet build -c Release

# Публикация автономного релиза (Self-Contained)
dotnet publish StormSystemOptimizer/StormSystemOptimizer.csproj -c Release -r win-x64 --self-contained true -o "../Assembling"
```

---

## 📦 Установка

Запустите установочный файл `Files\StormSystemOptimizer_Setup_v0.0.1.exe`:
- Автоматическая установка в `C:\Program Files\Storm System Optimizer`.
- Создание ярлыков на Рабочем столе и в меню «Пуск» с фирменной иконкой.
- Полная регистрация в реестре Windows (Параметры -> Установленные приложения) с поддержкой чистого удаления.

---

## 👤 Автор

- **ReiKatari** — [GitHub](https://github.com/ReiKatari)
- **STORM Software** © 2026. Все права защищены.
