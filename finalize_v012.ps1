# Finalize: import cert + verify
$cerPath = "E:\STORM SYSTEM OPTIMIZER\Files\STORM_Certificate.cer"
try {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
    $storeRoot = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
    $storeRoot.Open("ReadWrite")
    $storeRoot.Add($cert)
    $storeRoot.Close()
    $storePub = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
    $storePub.Open("ReadWrite")
    $storePub.Add($cert)
    $storePub.Close()
    Write-Host "Certificate trusted OK" -ForegroundColor Green
} catch {
    Write-Host "Cert: $($_.Exception.Message)" -ForegroundColor Yellow
}

$sig1 = Get-AuthenticodeSignature "E:\STORM SYSTEM OPTIMIZER\Assembling\StormSystemOptimizer.exe"
Write-Host "Main EXE: $($sig1.Status) - $($sig1.SignerCertificate.Subject)"

$sig2 = Get-AuthenticodeSignature "E:\STORM SYSTEM OPTIMIZER\Files\StormSystemOptimizer_Setup_v0.1.2.exe"
Write-Host "Installer: $($sig2.Status) - $($sig2.SignerCertificate.Subject)"

Write-Host "=== BUILD v0.1.2 COMPLETE ===" -ForegroundColor Green
