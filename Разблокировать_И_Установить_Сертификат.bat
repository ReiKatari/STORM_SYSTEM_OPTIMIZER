@echo off
chcp 65001 >nul
title STORM SYSTEM OPTIMIZER - Разблокировка и Доверие Сертификата

:: Проверка прав Администратора
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Запрос прав администратора...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo ============================================================
echo   STORM SYSTEM OPTIMIZER - УСТАНОВКА СЕРТИФИКАТА И РАЗБЛОКИРОВКА
echo ============================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "CERT_FILE=%SCRIPT_DIR%Files\STORM_Certificate.cer"

if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%STORM_Certificate.cer"
)

if exist "%CERT_FILE%" (
    echo [1/3] Установка сертификата STORM в Доверенные корневые центры...
    certutil.exe -addstore -f "Root" "%CERT_FILE%" >nul 2>&1
    echo [2/3] Добавление STORM Software в список Доверенных издателей Windows...
    certutil.exe -addstore -f "TrustedPublisher" "%CERT_FILE%" >nul 2>&1
    echo [OK] Сертификат успешно зарегистрирован в системе!
) else (
    echo [!] Файл сертификата не найден: %CERT_FILE%
)

echo.
echo [3/3] Снятие метки загрузки из интернета (Unblock Mark-of-the-Web)...
powershell -Command "Get-ChildItem '%SCRIPT_DIR%' -Recurse -Filter '*.exe' | Unblock-File" >nul 2>&1
echo [OK] Все исполняемые файлы успешно разблокированы!

echo.
echo ============================================================
echo   ГОТОВО! Теперь приложения STORM запускаются без блокировок.
echo ============================================================
echo.
echo Запуск установщика STORM SYSTEM OPTIMIZER v0.1.6...
if exist "%SCRIPT_DIR%Files\StormSystemOptimizer_Setup_v0.1.6.exe" (
    start "" "%SCRIPT_DIR%Files\StormSystemOptimizer_Setup_v0.1.6.exe"
) else if exist "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe" (
    start "" "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe"
)

timeout /t 3 >nul
exit /b
