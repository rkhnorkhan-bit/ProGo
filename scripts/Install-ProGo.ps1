param(
    [switch]$NoStartup,
    [switch]$NoStartMenuShortcut,
    [switch]$Launch
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
# Install-FromGitHub.ps1 is only for first-time bootstrap. It is intentionally not deployed into the installed runtime app.
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $InstalledScripts $scriptName) -Force
    }
}

function New-ProGoShortcut($ShortcutPath, $Arguments) {
    $Shell = New-Object -ComObject WScript.Shell
    $Shortcut = $Shell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = Join-Path $InstallDir "ProGo.exe"
    $Shortcut.Arguments = $Arguments
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.Description = "ProGo tray proxy and local vault"
    $IconPath = Join-Path $InstallDir "ProGo.ico"
    if (Test-Path $IconPath) { $Shortcut.IconLocation = $IconPath }
    $Shortcut.Save()
}

if (-not $NoStartup) {
    $Startup = [Environment]::GetFolderPath("Startup")
    New-ProGoShortcut -ShortcutPath (Join-Path $Startup "ProGo.lnk") -Arguments ""
}

if (-not $NoStartMenuShortcut) {
    $Programs = [Environment]::GetFolderPath("Programs")
    $MenuDir = Join-Path $Programs "ProGo"
    New-Item -ItemType Directory -Path $MenuDir -Force | Out-Null
    New-ProGoShortcut -ShortcutPath (Join-Path $MenuDir "ProGo.lnk") -Arguments "--show"
    New-ProGoShortcut -ShortcutPath (Join-Path $MenuDir "ProGo Status.lnk") -Arguments "--show"
}

Write-Host "Install OK: $InstallDir"
Write-Host "Installed version: $((Get-Content -Raw -Path $VersionFile).Trim())"
Write-Host "Updater scripts: $InstalledScripts"
Write-Host "Visible launcher: $(Join-Path $InstalledScripts 'Show-ProGo.ps1')"
Write-Host "Start helper: $(Join-Path $InstalledScripts 'Start-ProGo.ps1')"
Write-Host "Repair helper: $(Join-Path $InstalledScripts 'Repair-ProGo.ps1')"
Write-Host "Backups folder: $(Join-Path $InstallDir 'backups')"
Write-Host "Vault/settings/logs are preserved in %LOCALAPPDATA%\ProGo."

if ($Launch) {
    Start-Process -FilePath (Join-Path $InstallDir "ProGo.exe") -WorkingDirectory $InstallDir -ArgumentList "--show" | Out-Null
    Write-Host "ProGo launched."
}
