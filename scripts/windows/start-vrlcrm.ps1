# VRLCRM Docker stack + Cloudflare Tunnel baslatma scripti
# Kullanim: powershell -ExecutionPolicy Bypass -File scripts\windows\start-vrlcrm.ps1

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $ProjectRoot

Write-Host "[VRLCRM] Proje klasoru: $ProjectRoot"

function Wait-DockerReady {
    param([int]$MaxAttempts = 60)

    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $null = docker info 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "[VRLCRM] Docker hazir."
                return $true
            }
        } catch {
            # Docker henuz acilmamis olabilir
        }

        Write-Host "[VRLCRM] Docker bekleniyor... ($i/$MaxAttempts)"
        Start-Sleep -Seconds 5
    }

    Write-Error "[VRLCRM] Docker baslatilamadi. Docker Desktop'in acik oldugundan emin olun."
    return $false
}

if (-not (Wait-DockerReady)) {
    exit 1
}

if (-not (Test-Path ".env")) {
    Write-Error "[VRLCRM] .env dosyasi bulunamadi. Once .env.example dosyasindan .env olusturun."
    exit 1
}

$envContent = Get-Content ".env" -Raw
$hasTunnelToken = $envContent -match '(?m)^CLOUDFLARE_TUNNEL_TOKEN=\S+'

# Yerel HTTPS sadece sertifika + Caddyfile hazirsa devreye girer (yoksa hata uretmesin).
$certReady = (Test-Path (Join-Path $ProjectRoot "certs\active\fullchain.pem")) -and `
             (Test-Path (Join-Path $ProjectRoot "caddy\runtime\Caddyfile"))

$composeArgs = @("compose")
if ($hasTunnelToken) { $composeArgs += @("--profile", "tunnel") }
if ($certReady)      { $composeArgs += @("--profile", "local-https") }
$composeArgs += @("up", "-d", "--remove-orphans")

$profilMsg = @()
if ($hasTunnelToken) { $profilMsg += "Cloudflare Tunnel (dis erisim)" }
if ($certReady)      { $profilMsg += "Yerel HTTPS/Caddy + DNS (offline)" }
if ($profilMsg.Count -eq 0) { $profilMsg += "sadece web + db" }
Write-Host "[VRLCRM] Baslatiliyor: $($profilMsg -join ' + ')"
if ($hasTunnelToken -and -not $certReady) {
    Write-Host "[VRLCRM] NOT: Sertifika yok; offline HTTPS pasif. Once obtain-letsencrypt.ps1 calistirin."
}

docker @composeArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "[VRLCRM] docker compose basarisiz oldu."
    exit 1
}

Write-Host "[VRLCRM] Konteyner durumu:"
docker compose ps

Write-Host ""
Write-Host "Yerel test : http://localhost:8080"
Write-Host "Ofis ici   : https://$($env:CRM_HOSTNAME) (yerel DNS + Caddy; offline de calisir)"
Write-Host "Dis erisim : Cloudflare tunnel hostname (internet gerekir)"
Write-Host "[VRLCRM] Tamamlandi."
