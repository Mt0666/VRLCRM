# .env'den Caddyfile uretir + certbot symlinklerini Windows icin duzeltir
# Kullanim: powershell -ExecutionPolicy Bypass -File scripts\windows\sync-caddy-config.ps1

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

$hostname = Read-EnvValue "CRM_HOSTNAME" "crm.metevarol.com.tr"
if ([string]::IsNullOrWhiteSpace($hostname)) {
    Write-Error "CRM_HOSTNAME .env icinde tanimli degil."
}

$certsDir = Join-Path $ProjectRoot "certs"
$activeDir = Join-Path $certsDir "active"
New-Item -ItemType Directory -Force -Path $activeDir | Out-Null

$archiveFull = Join-Path $certsDir "archive\$hostname\fullchain1.pem"
$archiveKey  = Join-Path $certsDir "archive\$hostname\privkey1.pem"
$liveFull    = Join-Path $certsDir "live\$hostname\fullchain.pem"
$liveKey     = Join-Path $certsDir "live\$hostname\privkey.pem"

$sourceFull = $null
$sourceKey  = $null

if (Test-Path $archiveFull) {
    $sourceFull = $archiveFull
    $sourceKey  = $archiveKey
} elseif (Test-Path $liveFull) {
    $sourceFull = $liveFull
    $sourceKey  = $liveKey
}

if (-not $sourceFull -or -not (Test-Path $sourceFull)) {
    Write-Error "Sertifika bulunamadi. Once obtain-letsencrypt.ps1 calistirin."
}

Copy-Item $sourceFull (Join-Path $activeDir "fullchain.pem") -Force
Copy-Item $sourceKey  (Join-Path $activeDir "privkey.pem") -Force

$runtimeDir = Join-Path $ProjectRoot "caddy\runtime"
New-Item -ItemType Directory -Force -Path $runtimeDir | Out-Null

$caddyfile = @"
{
	auto_https off
}

(crm_proxy) {
	reverse_proxy web:8080 {
		header_up X-Forwarded-Proto {scheme}
		header_up X-Forwarded-Host {host}
	}
}

https://$hostname {
	tls /etc/certs/active/fullchain.pem /etc/certs/active/privkey.pem
	import crm_proxy
}

http://$hostname {
	redir https://{host}{uri} permanent
}
"@

$caddyPath = Join-Path $runtimeDir "Caddyfile"
$caddyfile | Set-Content -Path $caddyPath -Encoding UTF8

Write-Host "[VRLCRM] Caddy config guncellendi: $caddyPath"
Write-Host "[VRLCRM] Hostname: $hostname"
Write-Host "[VRLCRM] Sertifika: certs\active\"
