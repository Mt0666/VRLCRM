# Windows oturum acildiginda VRLCRM stack'ini otomatik baslatir.
# Yonetici olarak calistirin:
#   powershell -ExecutionPolicy Bypass -File scripts\windows\install-autostart.ps1

$ErrorActionPreference = "Stop"

$StartScript = Join-Path $PSScriptRoot "start-vrlcrm.ps1"
if (-not (Test-Path $StartScript)) {
    Write-Error "start-vrlcrm.ps1 bulunamadi: $StartScript"
    exit 1
}

$TaskName = "VRLCRM Docker Stack"
$Argument = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$StartScript`""

$Existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($Existing) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $Argument
$Trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$Settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $Trigger `
    -Settings $Settings `
    -Description "VRLCRM web, SQL ve Cloudflare Tunnel konteynerlerini baslatir." `
    -RunLevel LeastPrivilege | Out-Null

Write-Host "Gorev olusturuldu: $TaskName"
Write-Host ""
Write-Host "Ayrica Docker Desktop icinde su secenekleri acin:"
Write-Host "  Settings > General > Start Docker Desktop when you sign in"
Write-Host "  Settings > General > Start containers when Docker starts (varsa)"
Write-Host ""
Write-Host "Cloudflare tarafinda Public Hostname > Service URL: http://web:8080"
Write-Host "Test icin: powershell -ExecutionPolicy Bypass -File scripts\windows\start-vrlcrm.ps1"
