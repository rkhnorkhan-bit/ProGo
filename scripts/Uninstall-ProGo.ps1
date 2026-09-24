param(
    [switch]$RemoveUserData
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$Startup = [Environment]::GetFolderPath("Startup")
$ShortcutPath = Join-Path $Startup "ProGo.lnk"

Get-Process -Name "ProGo" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $_.CloseMainWindow() | Out-Null
        Start-Sleep -Milliseconds 700
        if (-not $_.HasExited) { $_.Kill() }
    } catch {}
}

if (Test-Path $ShortcutPath) {
    Remove-Item $ShortcutPath -Force
}

$Exe = Join-Path $InstallDir "ProGo.exe"
if (Test-Path $Exe) {
    Remove-Item $Exe -Force
}

if ($RemoveUserData) {
    $answer = Read-Host "Это удалит vault/settings/logs из $InstallDir. Введите DELETE для подтверждения"
    if ($answer -eq "DELETE") {
        Remove-Item $InstallDir -Recurse -Force
        Write-Host "User data removed."
    } else {
        Write-Host "User data kept."
    }
} else {
    Write-Host "Uninstall OK. User vault/settings/logs kept in $InstallDir."
}
