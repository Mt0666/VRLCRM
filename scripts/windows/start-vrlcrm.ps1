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

if ($hasTunnelToken) {
    Write-Host "[VRLCRM] Stack + Cloudflare Tunnel baslatiliyor..."
    docker compose --profile tunnel up -d --remove-orphans
} else {
    Write-Host "[VRLCRM] CLOUDFLARE_TUNNEL_TOKEN yok; sadece web + db baslatiliyor..."
    docker compose up -d --remove-orphans
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "[VRLCRM] docker compose basarisiz oldu."
    exit 1
}

Write-Host "[VRLCRM] Konteyner durumu:"
docker compose ps

Write-Host "[VRLCRM] Tamamlandi."
