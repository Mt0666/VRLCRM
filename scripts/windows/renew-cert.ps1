# Let's Encrypt sertifikasini yeniler (certbot renew).
# certbot renew SADECE bitise <30 gun kalinca gercekten yeniler; aksi halde no-op.
# Sertifika gercekten degisirse: certs\active guncellenir ve Caddy yeniden baslatilir.
# Haftalik zamanlanmis gorev icindir: install-renew-task.ps1
# Elle calistirmak icin: powershell -ExecutionPolicy Bypass -File scripts\windows\renew-cert.ps1

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $ProjectRoot

$certsDir = Join-Path $ProjectRoot "certs"
$cfDir    = Join-Path $ProjectRoot "cloudflare"

if (-not (Test-Path (Join-Path $cfDir "cloudflare.ini"))) {
    Write-Error "cloudflare\cloudflare.ini bulunamadi. Once obtain-letsencrypt.ps1 calistirin."
}

$activeCert = Join-Path $certsDir "active\fullchain.pem"
$before = if (Test-Path $activeCert) { (Get-FileHash $activeCert).Hash } else { "" }

Write-Host "[VRLCRM] Sertifika yenileme kontrolu (certbot renew)..."
docker run --rm `
    -v "${certsDir}:/etc/letsencrypt" `
    -v "${cfDir}:/cloudflare:ro" `
    certbot/dns-cloudflare renew --non-interactive

if ($LASTEXITCODE -ne 0) {
    Write-Error "certbot renew basarisiz."
}

# certs\active'i en yeni sertifikayla guncelle (Caddyfile de uretir)
& (Join-Path $PSScriptRoot "sync-caddy-config.ps1")

$after = if (Test-Path $activeCert) { (Get-FileHash $activeCert).Hash } else { "" }

if ($before -ne $after) {
    docker restart vrlcrm-caddy | Out-Null
    Write-Host "[VRLCRM] Yeni sertifika yuklendi, Caddy yeniden baslatildi."
} else {
    Write-Host "[VRLCRM] Sertifika degismedi (henuz yenileme gerekmiyor), Caddy'ye dokunulmadi."
}
