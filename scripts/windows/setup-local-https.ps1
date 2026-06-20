# Yerel HTTPS (Let's Encrypt) icin Cloudflare API token kontrolu
# Kullanim: powershell -ExecutionPolicy Bypass -File scripts\windows\setup-local-https.ps1

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

function Get-LanIPv4 {
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            ($_.PrefixOrigin -eq "Dhcp" -or $_.PrefixOrigin -eq "Manual")
        } |
        Select-Object -First 1 -ExpandProperty IPAddress
}

if (-not (Test-Path ".env")) {
    Write-Error ".env bulunamadi. Once .env.example dosyasindan .env olusturun."
}

$hostname = Read-EnvValue "CRM_HOSTNAME" "crm.metevarol.com.tr"
$apiToken = Read-EnvValue "CLOUDFLARE_API_TOKEN" ""
$lanIp = Read-EnvValue "LOCAL_LAN_IP" ""
if ([string]::IsNullOrWhiteSpace($lanIp)) {
    $lanIp = Get-LanIPv4
}

Write-Host "=== Yerel HTTPS (Let's Encrypt) ==="
Write-Host ""
Write-Host "Telefona tek tek sertifika YUKLEME gerekmez."
Write-Host "Caddy gercek Let's Encrypt sertifikasi alir; tum cihazlar otomatik guvenir."
Write-Host ""

if ([string]::IsNullOrWhiteSpace($apiToken)) {
    Write-Host "1) Cloudflare API Token olustur:"
    Write-Host "   dash.cloudflare.com > My Profile > API Tokens > Create Token"
    Write-Host "   Template: Edit zone DNS"
    Write-Host "   Zone: metevarol.com.tr (domain'in)"
    Write-Host ""
    Write-Host "2) .env dosyasina ekle:"
    Write-Host "   CLOUDFLARE_API_TOKEN=..."
    Write-Host "   CRM_HOSTNAME=$hostname"
    if ($lanIp) { Write-Host "   LOCAL_LAN_IP=$lanIp" }
    Write-Host ""
    Write-Error "CLOUDFLARE_API_TOKEN .env icinde yok. Once token olusturup ekleyin."
}

Write-Host "Hostname : $hostname"
Write-Host "LAN IP   : $lanIp"
Write-Host ""
Write-Host "3) Router'da yerel DNS override (tek seferlik):"
Write-Host "   $hostname  ->  $lanIp"
Write-Host ""
Write-Host "4) Stack'i baslat (ilk seferde sertifika alinir, internet gerekir):"
Write-Host "   powershell -ExecutionPolicy Bypass -File scripts\windows\start-vrlcrm.ps1"
Write-Host ""
Write-Host "5) Ofisten test: https://$hostname"
Write-Host ""
Write-Host "Internet kesilince: sertifika zaten alinmissa ~90 gun calismaya devam eder."
Write-Host "Telefona ekstra bir sey yuklemen gerekmez."
