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

$setupExePath = Join-Path $filesDir "StormSystemOptimizer_Setup_v$appVersion.exe"

# Step 0: Terminate any running instances to release file locks
Write-Host "[0/6] Closing running instances to release file locks..." -ForegroundColor Yellow
Get-Process "StormSystemOptimizer", "StormInstaller" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# Step 1: Clean build outputs
Write-Host "[1/6] Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path "$appProjDir\bin") { Remove-Item "$appProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$appProjDir\obj") { Remove-Item "$appProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\bin") { Remove-Item "$installerProjDir\bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "$installerProjDir\obj") { Remove-Item "$installerProjDir\obj" -Recurse -Force -ErrorAction SilentlyContinue }

# Step 2: Publish Single-File App Executable
Write-Host "[2/6] Publishing App (WPF .NET 8 Single-File)..." -ForegroundColor Yellow
$appPublishDir = Join-Path $appProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$appProjDir\StormSystemOptimizer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedExe = Join-Path $appPublishDir "StormSystemOptimizer.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Error: Published executable $publishedExe was not created!"
}

# Step 3: Copy to Assembling & Installer Resources
Write-Host "[3/6] Copying to Assembling and Installer Resources..." -ForegroundColor Yellow
if (-not (Test-Path $assemblingDir)) { New-Item -ItemType Directory -Path $assemblingDir | Out-Null }
Copy-Item $publishedExe "$assemblingDir\StormSystemOptimizer.exe" -Force

$installerResDir = Join-Path $installerProjDir "Resources"
if (-not (Test-Path $installerResDir)) { New-Item -ItemType Directory -Path $installerResDir | Out-Null }
Copy-Item $publishedExe "$installerResDir\StormSystemOptimizer.exe" -Force

# Step 4: Publish Installer
Write-Host "[4/6] Publishing Installer v$appVersion..." -ForegroundColor Yellow
$installerPublishDir = Join-Path $installerProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$installerProjDir\StormInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedInstaller = Join-Path $installerPublishDir "StormInstaller.exe"
if (-not (Test-Path $publishedInstaller)) {
    throw "Error: Published installer $publishedInstaller was not created!"
}

Copy-Item $publishedInstaller $setupExePath -Force

# Step 5: Digital Signature (Authenticode)
Write-Host "[5/6] Applying digital signature (Authenticode SHA-256)..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM Software Root CA*" } | Select-Object -First 1
if ($cert) {
    Set-AuthenticodeSignature -FilePath "$assemblingDir\StormSystemOptimizer.exe" -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    Set-AuthenticodeSignature -FilePath $setupExePath -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    Write-Host "Authenticode signatures successfully applied!" -ForegroundColor Green
} else {
    Write-Host "Code signing certificate not found in CurrentUser\My." -ForegroundColor DarkYellow
}

# Step 6: Unblock Files
Write-Host "[6/6] Unblocking output files..." -ForegroundColor Yellow
Unblock-File -Path "$assemblingDir\StormSystemOptimizer.exe" -ErrorAction SilentlyContinue
Unblock-File -Path $setupExePath -ErrorAction SilentlyContinue

Write-Host "============================================================" -ForegroundColor Green
Write-Host " RELEASE v$appVersion SUCCESSFULLY BUILT AND PACKAGED! " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host " 1. Portable EXE: $assemblingDir\StormSystemOptimizer.exe" -ForegroundColor Cyan
Write-Host " 2. Installer:    $setupExePath" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green
