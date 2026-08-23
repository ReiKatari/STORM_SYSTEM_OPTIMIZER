@echo off
chcp 65001 >nul
title STORM SYSTEM OPTIMIZER - ������� ������

net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell.exe -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "SCRIPT_DIR=%~dp0"

:: ������� ������ ����� ����������
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%SCRIPT_DIR%' -Recurse -Include *.exe,*.dll | ForEach-Object { Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue }" >nul 2>&1

:: ������ ������������� ���������, ����������� ��� ����������� ������
if exist "C:\Program Files\StormSystemOptimizer\StormSystemOptimizer.exe" (
    start "" "C:\Program Files\StormSystemOptimizer\StormSystemOptimizer.exe"
) else if exist "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe" (
    start "" "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe"
) else if exist "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe" (
    start "" "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe"
) else (
    echo [!] ����������� ���� �� ������.
    pause
)

exit /b
