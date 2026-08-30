# Build and Package Script for STORM SYSTEM OPTIMIZER v0.1.2 with Smart App Control / SmartScreen Trust
$ErrorActionPreference = "Stop"

# Terminate running instances to prevent file lock
Get-Process -Name "StormSystemOptimizer","StormInstaller" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$root = "E:\STORM SYSTEM OPTIMIZER"
$sourcesDir = Join-Path $root "Sources\StormSystemOptimizer"
$installerDir = Join-Path $root "Sources\StormInstaller"
$assemblingDir = Join-Path $root "Assembling"
$filesDir = Join-Path $root "Files"
$pfxPath = Join-Path $filesDir "STORM_CodeSign.pfx"
$cerPath = Join-Path $filesDir "STORM_Certificate.cer"
$outputInstaller = Join-Path $filesDir "StormSystemOptimizer_Setup_v0.1.2.exe"

Write-Host "=== 1. Publishing Standalone Native Single-File Executable v0.1.2 ===" -ForegroundColor Cyan
dotnet publish "$sourcesDir\StormSystemOptimizer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "$assemblingDir"

$exePath = Join-Path $assemblingDir "StormSystemOptimizer.exe"

Write-Host "=== 2. Signing StormSystemOptimizer.exe with Authenticode SHA-256 ===" -ForegroundColor Cyan
$pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, "StormCodeSign2026!")
try {
    Set-AuthenticodeSignature -FilePath $exePath -Certificate $pfx -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com"
} catch {
    Set-AuthenticodeSignature -FilePath $exePath -Certificate $pfx -HashAlgorithm SHA256
}

# Unblock file
Unblock-File -Path $exePath -ErrorAction SilentlyContinue

$sig = Get-AuthenticodeSignature $exePath
Write-Host "Signed Main Binary: $($sig.Status) by $($sig.SignerCertificate.Subject)" -ForegroundColor Green

Write-Host "=== 3. Publishing Standalone Installer v0.1.2 ===" -ForegroundColor Cyan
$tempInstallerOut = Join-Path $installerDir "bin\Release\net8.0-windows\win-x64\publish"
dotnet publish "$installerDir\StormInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "$tempInstallerOut"

$builtInstaller = Join-Path $tempInstallerOut "StormInstaller.exe"
Copy-Item -Path $builtInstaller -Destination $outputInstaller -Force

Write-Host "=== 4. Signing Setup Package with Authenticode SHA-256 ===" -ForegroundColor Cyan
try {
    Set-AuthenticodeSignature -FilePath $outputInstaller -Certificate $pfx -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com"
} catch {
    Set-AuthenticodeSignature -FilePath $outputInstaller -Certificate $pfx -HashAlgorithm SHA256
}

# Unblock installer package
Unblock-File -Path $outputInstaller -ErrorAction SilentlyContinue

# Auto-trust certificate in CurrentUser store for seamless instant execution without SmartScreen prompts
try {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
    $storeRoot = New-Object System.Security.Cryptography.X509Certificates.X509Store([System.Security.Cryptography.X509Certificates.StoreName]::Root, [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    $storeRoot.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $storeRoot.Add($cert)
    $storeRoot.Close()

    $storePub = New-Object System.Security.Cryptography.X509Certificates.X509Store([System.Security.Cryptography.X509Certificates.StoreName]::TrustedPublisher, [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    $storePub.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $storePub.Add($cert)
    $storePub.Close()
    Write-Host "Certificate successfully trusted in CurrentUser Root and TrustedPublisher stores." -ForegroundColor Green
} catch {
    Write-Host "Certificate import notification: $($_.Exception.Message)" -ForegroundColor Yellow
}

$setupSig = Get-AuthenticodeSignature $outputInstaller
Write-Host "Installer Signed: $($setupSig.Status) by $($setupSig.SignerCertificate.Subject)" -ForegroundColor Green

Write-Host "=== ALL TASKS COMPLETED SUCCESSFULLY! ===" -ForegroundColor Green
Write-Host "Standalone Executable: $exePath" -ForegroundColor Yellow
Write-Host "Installer Package: $outputInstaller" -ForegroundColor Yellow
