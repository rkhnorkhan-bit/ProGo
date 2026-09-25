param(
    [switch]$RemoveUserData
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$Startup = [Environment]::GetFolderPath("Startup")
$StartupShortcut = Join-Path $Startup "ProGo.lnk"
$Programs = [Environment]::GetFolderPath("Programs")
$MenuDir = Join-Path $Programs "ProGo"

Get-Process -Name "ProGo" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $_.CloseMainWindow() | Out-Null
        Start-Sleep -Milliseconds 700
        if (-not $_.HasExited) { $_.Kill() }
    } catch {}
}

foreach ($path in @($StartupShortcut, $MenuDir)) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$Exe = Join-Path $InstallDir "ProGo.exe"
if (Test-Path $Exe) {
    Remove-Item $Exe -Force
}

if ($RemoveUserData) {
    $answer = Read-Host "This removes vault/settings/logs from $InstallDir. Type DELETE to confirm"
    if ($answer -eq "DELETE") {
        Remove-Item $InstallDir -Recurse -Force
        Write-Host "User data removed."
    } else {
        Write-Host "User data kept."
    }
} else {
    Write-Host "Uninstall OK. User vault/settings/logs kept in $InstallDir."
}
