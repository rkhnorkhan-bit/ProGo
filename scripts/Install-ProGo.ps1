param(
    [switch]$NoStartup
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $PSScriptRoot "Build-ProGo.ps1"
& $BuildScript

$ReleaseDir = Join-Path $Root "release"
$Exe = Join-Path $ReleaseDir "ProGo.exe"
$VersionFile = Join-Path $ReleaseDir "VERSION"
if (-not (Test-Path $Exe)) {
    throw "ProGo.exe not found after build."
}
if (-not (Test-Path $VersionFile)) {
    throw "VERSION not found after build."
}

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $InstallDir "backups") -Force | Out-Null
Copy-Item $Exe -Destination (Join-Path $InstallDir "ProGo.exe") -Force
Copy-Item $VersionFile -Destination (Join-Path $InstallDir "VERSION") -Force

$Icon = Join-Path $ReleaseDir "ProGo.ico"
if (Test-Path $Icon) {
    Copy-Item $Icon -Destination (Join-Path $InstallDir "ProGo.ico") -Force
}

$InstalledScripts = Join-Path $InstallDir "scripts"
New-Item -ItemType Directory -Path $InstalledScripts -Force | Out-Null
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Restore-ProGoBackup.ps1", "Install-FromGitHub.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $InstalledScripts $scriptName) -Force
    }
}

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
Write-Host "Installed version: $((Get-Content -Raw -Path $VersionFile).Trim())"
Write-Host "Updater scripts: $InstalledScripts"
Write-Host "Backups folder: $(Join-Path $InstallDir 'backups')"
Write-Host "Vault/settings/logs are preserved in %LOCALAPPDATA%\ProGo."
