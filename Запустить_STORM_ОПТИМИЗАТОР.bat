@echo off
chcp 65001 >nul
title STORM SYSTEM OPTIMIZER - Быстрый запуск

net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell.exe -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "SCRIPT_DIR=%~dp0"

:: Быстрое снятие меток блокировки Mark-of-the-Web
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%SCRIPT_DIR%' -Recurse -Include *.exe,*.dll,*.cer,*.bat -ErrorAction SilentlyContinue | ForEach-Object { Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue }" >nul 2>&1

:: Запуск установленной программы, инсталлятора или портативной сборки
if exist "C:\Program Files\StormSystemOptimizer\StormSystemOptimizer.exe" (
    start "" "C:\Program Files\StormSystemOptimizer\StormSystemOptimizer.exe"
) else if exist "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_4.6.9_Setup.exe" (
    start "" "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_4.6.9_Setup.exe"
) else if exist "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe" (
    start "" "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe"
) else (
    echo [!] Исполняемый файл не найден.
    pause
)

exit /b
