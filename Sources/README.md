# STORM SYSTEM OPTIMIZER 1.0.7 — Исходный код решения

<p align="center">
  <img src="../Files/StormLogo.png" alt="STORM SYSTEM OPTIMIZER Logo" width="220" />
</p>

<p align="center">
  <b>Архитектура решения C# WPF .NET 8 LTS, структура сервисов, ViewModels и сборка.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/Framework-WPF%20%2F%20.NET%208%20LTS-00D2FF?style=for-the-badge&logo=windows-terminal" />
  <img src="https://img.shields.io/badge/Version-1.0.7-10B981?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Publisher-STORM%20TEAM-orange?style=for-the-badge" />
</p>

---

## ⚡ О проекте

**STORM SYSTEM OPTIMIZER 1.0.7** — системный оптимизатор нового поколения от команды **STORM TEAM**, разработанный на нативном высокопроизводительном движке **WPF (.NET 8 LTS)**. Программа предоставляет комплексный набор из 34 специализированных разделов для глубокой настройки, ускорения игр, освобождения оперативной памяти ядра, устранения системных задержек, разблокировки файлов и поддержания максимальной стабильности ПК.

---

## 🔒 Автоматическая цифровая подпись
Все релизные сборки автоматически подписываются сертификатом Authenticode SHA-256 (`CN=STORM TEAM`).

Скрипт `build.ps1` производит:
  - Компиляцию проекта `StormSystemOptimizer.csproj` в единый исполняемый файл `.exe` (Single-File).
  - Сборку инсталлятора `StormInstaller.exe` и его упаковку в `STORM_SYSTEM_OPTIMIZER_1.0.7_Setup.exe`.
  - Автоматическую подпись SHA-256 цифровым сертификатом Authenticode (`CN=STORM TEAM`).
  - Установку доверенного сертификата `STORM TEAM` в хранилища Windows.

---

## 🏛️ Архитектура проектов

### 1. `StormSystemOptimizer` (Главное приложение)
- **Фреймворк**: WPF (.NET 8.0 LTS, C# 12, `net8.0-windows`)
- **Паттерн**: MVVM (Model-View-ViewModel) с использованием `CommunityToolkit.Mvvm` (Source Generators, `[ObservableProperty]`, `[RelayCommand]`)
- **Основные слои**:
  - `Assets/` — Иконки, шрифты и растровые логотипы.
  - `Controls/` — Интерактивный STORM HUD Overlay (`HudWindow.xaml`), кастомные диалоги `StormMessageBox`.
  - `Themes/` — 8 динамических XAML словарей тем (`StormDark`, `StormNight`, `StormDay`, `StormMidnight`, `StormCyberpunk`, `StormMatrix`, `StormFantasy`, `StormWarhammer`) + векторная библиотека иконок `StormIcons.xaml`.
  - `Models/` — Структуры данных и модели процессов, дисков, обновлений, служб, лаунчеров, активных портов и дескрипторов блокировки.
  - `Services/` — Ядро сервисов (Win32 API, `IOCTL_STORAGE_GET_DEVICE_NUMBER`, `NtSetTimerResolution`, `NtSetSystemInformation`, Restart Manager `rstrtmgr.dll`, WMI, драйверы, DNS, Winget).
  - `ViewModels/` — Реактивные модели представления (Dashboard, ProcessManager, Disks, Network, PowerTuning, InputLag, ContextMenu, BackupVault, QuickMaintenance, MemoryMaster, GameLaunchers, FileUnlocker и др.).
  - `Views/` — 34 XAML страницы пользовательского интерфейса.

### 2. `StormInstaller` (Автономный установщик)
- **Фреймворк**: Windows Forms (.NET 8.0 LTS Single-File)
- **Функционал**:
  - Распаковка и установка исполняемых файлов в `Program Files\StormSystemOptimizer` или портативную папку.
  - Автоматическая установка цифрового сертификата `STORM TEAM`.
  - Создание ярлыков на Рабочем столе и в меню Пуск.
  - Регистрация в реестре Windows «Установка и удаление программ».

---