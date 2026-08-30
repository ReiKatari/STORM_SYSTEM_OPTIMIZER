# STORM SYSTEM OPTIMIZER - Automated Production Build & Release Script v0.1.4
$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " STORM SYSTEM OPTIMIZER v0.1.4 - PRODUCTION BUILD PIPELINE " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$baseDir = "E:\STORM SYSTEM OPTIMIZER"
$sourcesDir = Join-Path $baseDir "Sources"
$appProjDir = Join-Path $sourcesDir "StormSystemOptimizer"
$installerProjDir = Join-Path $sourcesDir "StormInstaller"
$assemblingDir = Join-Path $baseDir "Assembling"
$filesDir = Join-Path $baseDir "Files"
$setupV014 = Join-Path $filesDir "StormSystemOptimizer_Setup_v0.1.4.exe"

# Step 1: Clean build outputs
Write-Host "[1/6] Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path "$appProjDir\bin") { Remove-Item "$appProjDir\bin" -Recurse -Force }
if (Test-Path "$appProjDir\obj") { Remove-Item "$appProjDir\obj" -Recurse -Force }
if (Test-Path "$installerProjDir\bin") { Remove-Item "$installerProjDir\bin" -Recurse -Force }
if (Test-Path "$installerProjDir\obj") { Remove-Item "$installerProjDir\obj" -Recurse -Force }

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
Write-Host "[4/6] Publishing Installer v0.1.4..." -ForegroundColor Yellow
$installerPublishDir = Join-Path $installerProjDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$installerProjDir\StormInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$publishedInstaller = Join-Path $installerPublishDir "StormInstaller.exe"
if (-not (Test-Path $publishedInstaller)) {
    throw "Error: Published installer $publishedInstaller was not created!"
}

Copy-Item $publishedInstaller $setupV014 -Force

# Step 5: Digital Signature (Authenticode)
Write-Host "[5/6] Applying digital signature (Authenticode SHA-256)..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*STORM Software Root CA*" } | Select-Object -First 1
if ($cert) {
    Set-AuthenticodeSignature -FilePath "$assemblingDir\StormSystemOptimizer.exe" -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    Set-AuthenticodeSignature -FilePath $setupV014 -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    Write-Host "Authenticode signatures successfully applied!" -ForegroundColor Green
} else {
    Write-Host "Code signing certificate not found in CurrentUser\My." -ForegroundColor DarkYellow
}

# Step 6: Unblock Files
Write-Host "[6/6] Unblocking output files..." -ForegroundColor Yellow
Unblock-File -Path "$assemblingDir\StormSystemOptimizer.exe" -ErrorAction SilentlyContinue
Unblock-File -Path $setupV014 -ErrorAction SilentlyContinue

Write-Host "============================================================" -ForegroundColor Green
Write-Host " RELEASE v0.1.4 SUCCESSFULLY BUILT AND PACKAGED! " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host " 1. Portable EXE: $assemblingDir\StormSystemOptimizer.exe" -ForegroundColor Cyan
Write-Host " 2. Installer:    $setupV014" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Green
