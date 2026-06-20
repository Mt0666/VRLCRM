# SIMDIK KULLANILMIYOR — router yerel DNS gerekir. Ileride: --profile local-https
# Let's Encrypt sertifikasi alir (telefona ozel kurulum gerekmez).
# Kullanim: powershell -ExecutionPolicy Bypass -File scripts\windows\obtain-letsencrypt.ps1

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
$email = Read-EnvValue "ACME_EMAIL" "admin@vrlcrm.local"
$token = Read-EnvValue "CLOUDFLARE_API_TOKEN" ""

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Error "CLOUDFLARE_API_TOKEN .env icinde yok."
}

$cfDir = Join-Path $ProjectRoot "cloudflare"
$certsDir = Join-Path $ProjectRoot "certs"
New-Item -ItemType Directory -Force -Path $cfDir | Out-Null
New-Item -ItemType Directory -Force -Path $certsDir | Out-Null

$iniPath = Join-Path $cfDir "cloudflare.ini"
"dns_cloudflare_api_token = $token" | Set-Content -Path $iniPath -Encoding ASCII

Write-Host "[VRLCRM] Let's Encrypt sertifikasi aliniyor: $hostname"
Write-Host "[VRLCRM] Internet baglantisi ve Cloudflare API token gerekli."

docker run --rm `
    -v "${certsDir}:/etc/letsencrypt" `
    -v "${cfDir}:/cloudflare:ro" `
    certbot/dns-cloudflare certonly `
    --dns-cloudflare `
    --dns-cloudflare-credentials /cloudflare/cloudflare.ini `
    --non-interactive `
    --agree-tos `
    --no-eff-email `
    -m $email `
    -d $hostname

if ($LASTEXITCODE -ne 0) {
    Write-Error "Certbot basarisiz. Token ve domain DNS (Cloudflare Active) kontrol edin."
}

$livePath = Join-Path $certsDir "live\$hostname\fullchain.pem"
$archivePath = Join-Path $certsDir "archive\$hostname\fullchain1.pem"
if (-not (Test-Path $livePath) -and -not (Test-Path $archivePath)) {
    Write-Error "Sertifika dosyasi olusturulamadi."
}

& (Join-Path $PSScriptRoot "sync-caddy-config.ps1")

Write-Host ""
Write-Host "=== Sertifika hazir ==="
Write-Host "Simdi stack'i baslatin:"
Write-Host "  docker compose --profile tunnel up -d"
