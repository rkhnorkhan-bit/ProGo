param(
    [switch]$NoStartup
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $PSScriptRoot "Build-ProGo.ps1"
& $BuildScript

$Exe = Join-Path $Root "release\ProGo.exe"
if (-not (Test-Path $Exe)) {
    throw "ProGo.exe не найден после сборки."
}

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item $Exe -Destination (Join-Path $InstallDir "ProGo.exe") -Force

if (-not $NoStartup) {
    $Startup = [Environment]::GetFolderPath("Startup")
    $ShortcutPath = Join-Path $Startup "ProGo.lnk"
    $Shell = New-Object -ComObject WScript.Shell
    $Shortcut = $Shell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = Join-Path $InstallDir "ProGo.exe"
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.Description = "ProGo tray proxy and local vault"
    $Shortcut.Save()
}

Write-Host "Install OK: $InstallDir"
Write-Host "Vault/settings/logs are preserved in %LOCALAPPDATA%\ProGo."
