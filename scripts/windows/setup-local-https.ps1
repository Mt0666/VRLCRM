# Yerel HTTPS (Let's Encrypt) — once obtain-letsencrypt.ps1 calistirin
# Router DNS: CRM_HOSTNAME -> LOCAL_LAN_IP

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $ProjectRoot

function Read-EnvValue {
    param([string]$Name, [string]$Default = "")
    if (-not (Test-Path ".env")) { return $Default }
    $line = Get-Content ".env" | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -First 1
    if (-not $line) { return $Default }
    return ($line -split "=", 2)[1].Trim()
}

if (-not (Test-Path ".env")) {
    Write-Error ".env bulunamadi."
}

$hostname = Read-EnvValue "CRM_HOSTNAME" "crm.metevarol.com.tr"
$certPath = Join-Path $ProjectRoot "certs\live\$hostname\fullchain.pem"

Write-Host "=== Yerel HTTPS kontrol listesi (simdilik devre disi) ==="
Write-Host "Ofis offline erisimi icin router DNS + --profile local-https gerekir."
Write-Host ""
Write-Host "1) .env icinde CLOUDFLARE_API_TOKEN ve CRM_HOSTNAME olmali"
Write-Host "2) Sertifika al:"
Write-Host "   powershell -ExecutionPolicy Bypass -File scripts\windows\obtain-letsencrypt.ps1"
Write-Host "3) Router DNS override:"
Write-Host "   $hostname -> (LOCAL_LAN_IP)"
Write-Host "4) Stack baslat:"
Write-Host "   docker compose --profile tunnel up -d"
Write-Host ""

if (-not (Test-Path $certPath)) {
    Write-Warning "Sertifika henuz yok: $certPath"
    Write-Host "Once obtain-letsencrypt.ps1 calistirin."
} else {
    Write-Host "Sertifika mevcut: $certPath"
}
