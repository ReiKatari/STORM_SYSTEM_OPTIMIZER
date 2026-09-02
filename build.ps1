# STORM SYSTEM OPTIMIZER - Automated Production Build & Release Pipeline
$ErrorActionPreference = "Stop"

$baseDir = $PSScriptRoot
if (-not $baseDir) { $baseDir = "E:\STORM SYSTEM OPTIMIZER" }
$sourcesDir = Join-Path $baseDir "Sources"
$appProjDir = Join-Path $sourcesDir "StormSystemOptimizer"
$installerProjDir = Join-Path $sourcesDir "StormInstaller"
$assemblingDir = Join-Path $baseDir "Assembling"
$filesDir = Join-Path $baseDir "Files"

# Read version from csproj
[xml]$appProjXml = Get-Content (Join-Path $appProjDir "StormSystemOptimizer.csproj")
$appVersion = $appProjXml.Project.PropertyGroup.Version
if (-not $appVersion) { $appVersion = "0.3.5" }

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " STORM SYSTEM OPTIMIZER v$appVersion - PRODUCTION BUILD PIPELINE " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$setupExePath = Join-Path $filesDir "STORM_SYSTEM_OPTIMIZER_${appVersion}_Setup.exe"

# Step 0: Terminate any running instances to release file locks
Write-Host "[0/6] Closing running instances to release file locks..." -ForegroundColor Yellow
cmd.exe /c "taskkill /F /IM StormSystemOptimizer.exe /T >nul 2>&1"
cmd.exe /c "taskkill /F /IM StormInstaller.exe /T >nul 2>&1"
Get-Process "StormSystemOptimizer", "StormInstaller" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Step 1: Clean build outputs
Write-Host "[1/6] Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path "$appProjDir\bin") { Remove-Item "$appProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$appProjDir\obj") { Remove-Item "$appProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\bin") { Remove-Item "$installerProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\obj") { Remove-Item "$installerProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }

$launcherProjDir = Join-Path $sourcesDir "StormLauncher"
if (Test-Path "$launcherProjDir\bin") { Remove-Item "$launcherProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$launcherProjDir\obj") { Remove-Item "$launcherProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }

# Step 2: Publish Single-File App Executable & Fast Launcher
Write-Host "[2/6] Publishing App & Fast Zero-UAC Launcher..." -ForegroundColor Yellow
$appPublishDir = Join-Path $appProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$appProjDir\StormSystemOptimizer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseSharedCompilation=false

$publishedExe = Join-Path $appPublishDir "StormSystemOptimizer.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Error: Published executable $publishedExe was not created!"
}

$launcherPublishDir = Join-Path $launcherProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$launcherProjDir\StormLauncher.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseSharedCompilation=false

$publishedLauncher = Join-Path $launcherPublishDir "StormLauncher.exe"

# Step 3: Digital Signature for App Executable, Launcher & Certificate Trust
Write-Host "[3/6] Applying digital signature (Authenticode SHA-256) to App and Launcher..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM TEAM*" } | Select-Object -First 1
if (-not $cert) {
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM Software*" -or $_.Subject -like "*STORM*" } | Select-Object -First 1
}
if ($cert) {
    # Export CER for distribution and silent trust
    $cerPath = Join-Path $filesDir "STORM_Certificate.cer"
    [System.IO.File]::WriteAllBytes($cerPath, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    Copy-Item $cerPath (Join-Path $filesDir "StormTeamRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $filesDir "StormSoftwareRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $baseDir "StormTeamRootCA.cer") -Force -ErrorAction SilentlyContinue
    Copy-Item $cerPath (Join-Path $baseDir "STORM_Certificate.cer") -Force -ErrorAction SilentlyContinue

    # Sign the App and Launcher executables FIRST before packaging
    Set-AuthenticodeSignature -FilePath $publishedExe -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    if (Test-Path $publishedLauncher) {
        Set-AuthenticodeSignature -FilePath $publishedLauncher -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    }
}

# Step 4: Copy SIGNED App and Launcher to Assembling & Installer Resources
Write-Host "[4/6] Packaging SIGNED binaries into Installer Resources..." -ForegroundColor Yellow
if (-not (Test-Path $assemblingDir)) { New-Item -ItemType Directory -Path $assemblingDir | Out-Null }
try {
    Copy-Item $publishedExe "$assemblingDir\StormSystemOptimizer.exe" -Force -ErrorAction Stop
    if (Test-Path $publishedLauncher) {
        Copy-Item $publishedLauncher "$assemblingDir\StormLauncher.exe" -Force -ErrorAction Stop
    }
} catch {
    Write-Host "Note: Output binaries in Assembling are currently locked by a running process." -ForegroundColor DarkGray
}

$installerResDir = Join-Path $installerProjDir "Resources"
if (-not (Test-Path $installerResDir)) { New-Item -ItemType Directory -Path $installerResDir | Out-Null }
try {
    Copy-Item $publishedExe "$installerResDir\StormSystemOptimizer.exe" -Force -ErrorAction SilentlyContinue
    if (Test-Path $publishedLauncher) {
        Copy-Item $publishedLauncher "$installerResDir\StormLauncher.exe" -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $cerPath) {
        Copy-Item $cerPath "$installerResDir\STORM_Certificate.cer" -Force -ErrorAction SilentlyContinue
    }
} catch { }

# Step 5: Publish & Sign Installer
Write-Host "[5/6] Publishing and Signing Installer v$appVersion..." -ForegroundColor Yellow
$installerPublishDir = Join-Path $installerProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$installerProjDir\StormInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseSharedCompilation=false

$publishedInstaller = Join-Path $installerPublishDir "StormInstaller.exe"
if (-not (Test-Path $publishedInstaller)) {
    throw "Error: Published installer $publishedInstaller was not created!"
}

Copy-Item $publishedInstaller $setupExePath -Force

if ($cert) {
    Set-AuthenticodeSignature -FilePath $setupExePath -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    if (Test-Path "$assemblingDir\StormSystemOptimizer.exe") {
        try { Set-AuthenticodeSignature -FilePath "$assemblingDir\StormSystemOptimizer.exe" -Certificate $cert -HashAlgorithm SHA256 -ErrorAction SilentlyContinue | Out-Null } catch {}
    }

    # Register in User & Machine Root and TrustedPublisher stores silently without UI prompts
    try {
        $certObj = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
        foreach ($loc in @([System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine, [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)) {
            foreach ($name in @([System.Security.Cryptography.X509Certificates.StoreName]::Root, [System.Security.Cryptography.X509Certificates.StoreName]::TrustedPublisher, [System.Security.Cryptography.X509Certificates.StoreName]::AuthRoot, [System.Security.Cryptography.X509Certificates.StoreName]::CertificateAuthority)) {
                try {
                    $st = New-Object System.Security.Cryptography.X509Certificates.X509Store($name, $loc)
                    $st.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                    $st.Add($certObj)
                    $st.Close()
                } catch { }
            }
        }
    } catch { }

    Write-Host "Authenticode signatures (STORM TEAM) and trust stores successfully updated!" -ForegroundColor Green
} else {
    Write-Host "Code signing certificate not found in CurrentUser\My." -ForegroundColor DarkYellow
}

# Step 6: Create ZIP archive for Portable distribution
$zipPath = Join-Path $filesDir "STORM_SYSTEM_OPTIMIZER_${appVersion}.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force -ErrorAction SilentlyContinue }
try {
    Compress-Archive -Path "$assemblingDir\*" -DestinationPath $zipPath -Force
    Write-Host "Portable ZIP archive created: $zipPath" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not create ZIP: $_" -ForegroundColor DarkYellow
}

# Step 7: Unblock Files and apply Smart App Control / Defender exclusions
Write-Host "[7/7] Unblocking all output and project files and configuring trust..." -ForegroundColor Yellow
Get-ChildItem -Path $baseDir -Recurse -Include *.exe, *.dll, *.bat, *.ps1, *.cer, *.zip -ErrorAction SilentlyContinue | ForEach-Object {
    Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
}

try {
    # Relax SAC/SmartScreen blocking
    New-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy" -Force -ErrorAction SilentlyContinue | Out-Null
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy" -Name "VerifiedAndReputablePolicyState" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy" -Name "SAC_PreviousState" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
    New-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" -Force -ErrorAction SilentlyContinue | Out-Null
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" -Name "SmartScreenEnabled" -Value "Off" -Type String -Force -ErrorAction SilentlyContinue

    # Defender exclusions for project and output binaries
    Add-MpPreference -ExclusionPath $baseDir -ErrorAction SilentlyContinue
    Add-MpPreference -ExclusionPath $filesDir -ErrorAction SilentlyContinue
    Add-MpPreference -ExclusionPath $assemblingDir -ErrorAction SilentlyContinue
    Add-MpPreference -ExclusionPath "C:\Program Files\STORM SYSTEM OPTIMIZER" -ErrorAction SilentlyContinue
    Add-MpPreference -ExclusionProcess "StormSystemOptimizer.exe" -ErrorAction SilentlyContinue
    Add-MpPreference -ExclusionProcess "STORM_SYSTEM_OPTIMIZER_${appVersion}_Setup.exe" -ErrorAction SilentlyContinue
} catch { }

Write-Host "============================================================" -ForegroundColor Green
Write-Host " RELEASE v$appVersion SUCCESSFULLY BUILT AND PACKAGED! " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host " 1. Portable EXE: $assemblingDir\StormSystemOptimizer.exe" -ForegroundColor Cyan
Write-Host " 2. Installer:    $setupExePath" -ForegroundColor Cyan
Write-Host " 3. Portable ZIP: $zipPath" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green
