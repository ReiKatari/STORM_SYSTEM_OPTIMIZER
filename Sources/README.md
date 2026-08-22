# STORM SYSTEM OPTIMIZER ⚡ — Исходный код решения

<p align="center">
  <img src="../Files/StormLogo.png" alt="STORM SYSTEM OPTIMIZER Logo" width="220" />
</p>

<p align="center">
  <b>Архитектура решения C# WPF .NET 8 LTS, структура сервисов, ViewModels и сборка.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/Framework-WPF%20%2F%20.NET%208%20LTS-00D2FF?style=for-the-badge&logo=windows-terminal" />
  <img src="https://img.shields.io/badge/Version-v0.3.6-10B981?style=for-the-badge" />
</p>

---

## 🏛️ Архитектура проектов

### 1. `StormSystemOptimizer` (Главное приложение)
- **Фреймворк**: WPF (.NET 8.0 LTS, C# 12/13, `net8.0-windows`)
- **Паттерн**: MVVM (Model-View-ViewModel) с использованием `CommunityToolkit.Mvvm` (Source Generators, `[ObservableProperty]`, `[RelayCommand]`)
- **Основные слои**:
  - `Assets/` — Иконки, шрифты и растровые логотипы.
  - `Controls/` — Интерактивный STORM HUD Overlay (`HudWindow.xaml`), кастомные диалоги `StormMessageBox`.
  - `Themes/` — 4 динамических XAML словаря тем (`StormDark`, `StormNight`, `StormDay`, `StormMidnight`) + векторная библиотека иконок `StormIcons.xaml`.
  - `Models/` — Структуры данных и модели процессов, дисков, обновлений, служб.
  - `Services/` — Ядро приложения (низкоуровневые Win32 API, `IOCTL_STORAGE_GET_DEVICE_NUMBER`, `NtSetTimerResolution`, `NtSetSystemInformation`, WMI, драйверы, DNS, Winget).
  - `ViewModels/` — Реактивные модели представления (Dashboard, ProcessManager, Disks, Network, PowerTuning, InputLag, ContextMenu, BackupVault и др.).
  - `Views/` — 20 XAML страниц пользовательского интерфейса.

### 2. `StormInstaller` (Автономный установщик)
- **Фреймворк**: Windows Forms (.NET 8.0 LTS Single-File)
- **Функционал**:
  - Распаковка и установка исполняемых файлов в `Program Files` или портативную папку.
  - Установка доверенного сертификата `STORM Software Root CA`.
  - Создание ярлыков на Рабочем столе и в меню Пуск.
  - Регистрация в реестре Windows «Установка и удаление программ».

---

## 🛠️ Сборка через .NET CLI

```powershell
# Сборка основного приложения в Single-File Release
dotnet publish .\StormSystemOptimizer\StormSystemOptimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

# Сборка инсталлятора
dotnet publish .\StormInstaller\StormInstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```
