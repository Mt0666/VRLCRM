# Haftalik sertifika yenileme gorevi kurar (renew-cert.ps1'i her Pazar 03:30'da calistirir).
# Yonetici olarak calistirin:
#   powershell -ExecutionPolicy Bypass -File scripts\windows\install-renew-task.ps1

$ErrorActionPreference = "Stop"

$RenewScript = Join-Path $PSScriptRoot "renew-cert.ps1"
if (-not (Test-Path $RenewScript)) {
    Write-Error "renew-cert.ps1 bulunamadi: $RenewScript"
    exit 1
}

$TaskName = "VRLCRM Cert Renew"
$Argument = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$RenewScript`""

$Existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($Existing) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $Argument
$Trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 3:30AM
$Settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $Trigger `
    -Settings $Settings `
    -Description "VRLCRM Let's Encrypt sertifikasini haftalik yeniler (certbot renew + Caddy reload)." `
    -RunLevel Limited | Out-Null

Write-Host "Gorev olusturuldu: $TaskName (her Pazar 03:30)"
Write-Host ""
Write-Host "Not: Gorev, Docker Desktop calisirken (kullanici oturumu acikken) calisir."
Write-Host "Elle test: powershell -ExecutionPolicy Bypass -File scripts\windows\renew-cert.ps1"
