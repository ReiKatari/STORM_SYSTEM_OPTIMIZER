<h1 align="center">STORM SYSTEM OPTIMIZER ⚡</h1>

<p align="center">
  <img src="Files/StormLogo.png" alt="STORM SYSTEM OPTIMIZER Logo" width="220" />
</p>

<p align="center">
  <b>Высокопроизводительное, профессиональное и 100% безопасное приложение для полного сканирования системы Windows, глубокого анализа процессов, реального мониторинга температур комплектующих, игрового бустера Game Boost (0.5 мс таймер), аппаратных бенчмарков, MSI PCIe тюнинга, DirectStorage 1.2, замера скорости интернета, DNS-бенчмарка, выявления тормозов, очистки Standby памяти ядра и оптимизации производительности в 1 клик.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011-0078D4?style=for-the-badge&logo=windows" />
  <img src="https://img.shields.io/badge/Framework-WPF%20%2F%20.NET%208%20LTS-00D2FF?style=for-the-badge&logo=windows-terminal" />
  <img src="https://img.shields.io/badge/Language-C%23%20%2F%20.NET-7928CA?style=for-the-badge&logo=c-sharp" />
  <img src="https://img.shields.io/badge/Version-v0.1.0-10B981?style=for-the-badge" />
</p>

---

## ⚡ О проекте

**STORM SYSTEM OPTIMIZER v0.1.0** — это системный оптимизатор нового поколения на нативном движке **WPF (.NET 8 LTS)**. Программа проводит глубокий многопоточный анализ всей системы, предоставляет интерактивный менеджер процессов с базой знаний и рекомендациями, замер реальных температур комплектующих, полный набор аппаратных бенчмарков (видеокарта GPU Direct3D, VRAM, процессор Multi/Single-Core, RAM, диск последовательный и 4K IOPS, STORM Overall Performance Index), безопасный стресс-тест с защитой от перегрева, игровой режим **Game Boost 1-Click** с субмиллисекундным таймером прерываний (0.5 мс), режим **MSI (Message Signaled Interrupts)** для GPU и USB, тюнинг **DirectStorage 1.2 & BypassIO**, очистку **Standby Memory** ядра, встроенный **DNS Benchmark**, сетевой экран **Blackhole Shield**, **Smart Autopilot Daemon** и компактный игровой **HUD Оверлей (Ctrl+Shift+O)**.

---

## 🚀 Новые возможности и модули в версии 0.1.0

1. **🎮 Игровой режим «STORM Game Boost 1-Click»**:
   - Автоматическое распознавание активных 3D-игр.
   - Выделение приоритета и привязка к производительным P-Cores, фоновая изоляция сторонних процессов на E-Cores.
2. **⏱️ Аппаратный таймер прерываний Windows (0.500 мс)**:
   - Включение субмиллисекундного таймера через `NtSetTimerResolution` для исключения микрофризов и максимальной плавности.
3. **⚡ Режим MSI (Message Signaled Interrupts)**:
   - Перевод видеокарты и USB-контроллеров мыши/клавиатуры в режим MSI на шине PCIe для снижения Input Lag.
4. **🪟 DWM Latency Tweak**:
   - Оптимизация композиции DWM и очередей кадров для 3D приложений.
5. **🧠 Очистка Standby Memory & Modified Page List**:
   - Вызов низкоуровневого API ядра `NtSetSystemInformation` для мгновенного освобождения кэша памяти без сброса рабочих данных.
6. **🗜️ Smart RAM Auto-Compressor**:
   - Сжатие памяти неактивных приложений с освобождением физической RAM для активного окна.
7. **🚀 DirectStorage 1.2 & NVMe BypassIO Tuning**:
   - Оптимизация очередей `Win32 IoRing` и прямого доступа к хранилищу для мгновенной загрузки игр.
8. **⏱️ Интеллектуальный Idle-TRIM**:
   - Автоматический TRIM SSD в периоды системного простоя.
9. **🌐 TCP NoDelay & Nagle’s Algorithm Toggle**:
   - Отключение задержки сетевых пакетов для минимизации онлайн-пинга.
10. **⚡ Smart DNS Benchmark & 1-Click Fast Switcher**:
    - Замер задержек DNS (Cloudflare, Google, Quad9, AdGuard) и переключение в 1 клик.
11. **🛡️ Сетевой экран Blackhole Shield**:
    - Блокировка серверов телеметрии и рекламы на уровне сети.
12. **🤖 Фоновый автопилот STORM (Smart Daemon)**:
    - Автоматический контроль температур, утечек памяти и состояния системы.
13. **🔄 Мастер контрольных снимков реестра (Snapshot & Rollback)**:
    - Резервное копирование и откат параметров в 1 клик.
14. **📊 Компактный игровой оверлей (STORM Mini HUD `Ctrl+Shift+O`)**:
    - Полупрозрачный экранный виджет поверх всех окон (FPS, CPU/GPU, температуры, RAM).
15. **📈 Детальный мониторинг по ядрам процессора (Per-Core Load)**:
    - Индивидуальная раскладка нагрузки по всем потокам и ядрам CPU.
16. **📄 Экспорт диагностического отчета системы в HTML/PDF**:
    - Формирование стильного темного отчета со всей спецификацией и показателями ПК.

---

## 📁 Структура репозитория

```text
E:\STORM SYSTEM OPTIMIZER\
├── Sources\                  # Полные исходные коды C# WPF .NET 8 решения
│   ├── StormSystemOptimizer\
│   │   ├── Assets\           # Мультиформатные иконки и полноформатный кибер-логотип
│   │   ├── Controls\         # HUD Оверлей и кастомные диалоги StormMessageBox
│   │   ├── Themes\           # 4 XAML словаря тем + векторные иконки StormIcons.xaml
│   │   ├── Models\           # Модели данных, диски, процессы, службы, бенчмарки
│   │   ├── Services\         # Сервисы Game Boost, памяти, DNS, твиков, бенчмарков
│   │   ├── ViewModels\       # MVVM архитектура CommunityToolkit
│   │   └── Views\            # Все страницы интерфейса WPF
│   └── StormInstaller\       # Проект автономного инсталлятора
├── Files\                    # Каталог версий установщиков и сертификатов Authenticode
│   ├── StormSystemOptimizer_Setup_v0.0.3.exe
│   ├── StormSystemOptimizer_Setup_v0.0.4.exe
│   ├── StormSystemOptimizer_Setup_v0.0.5.exe
│   ├── StormSystemOptimizer_Setup_v0.0.6.exe
│   ├── StormSystemOptimizer_Setup_v0.0.7.exe
│   ├── StormSystemOptimizer_Setup_v0.0.8.exe
│   ├── StormSystemOptimizer_Setup_v0.0.9.exe
│   └── StormSystemOptimizer_Setup_v0.1.0.exe  <-- Актуальный релиз
└── Assembling\               # Опубликованный нативный бинарник StormSystemOptimizer.exe
```

---

## 🔒 Безопасность и цифровая подпись
Все бинарные файлы и инсталляторы подписаны сертификатом Authenticode SHA-256 (`CN=STORM Software Root CA`).
