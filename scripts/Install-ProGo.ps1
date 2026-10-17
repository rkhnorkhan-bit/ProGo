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
. (Join-Path $PSScriptRoot "Shortcuts-ProGo.ps1")
$WasInstalled = Test-ProGoExistingInstallation -InstallDirectory $InstallDir
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
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Enable-HomeVpnFirewall.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Measure-RescueProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $InstalledScripts $scriptName) -Force
    }
}

Copy-Item (Join-Path $ReleaseDir "scripts\ApplicationShortcuts.cs") -Destination (Join-Path $InstalledScripts "ApplicationShortcuts.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\MaintenanceOperation.cs") -Destination (Join-Path $InstalledScripts "MaintenanceOperation.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\InstalledUpdateTransport.cs") -Destination (Join-Path $InstalledScripts "InstalledUpdateTransport.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\UpdateInstallSession.cs") -Destination (Join-Path $InstalledScripts "UpdateInstallSession.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\BackupRetention.cs") -Destination (Join-Path $InstalledScripts "BackupRetention.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\BackupIntegrity.cs") -Destination (Join-Path $InstalledScripts "BackupIntegrity.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\BoundedLog.cs") -Destination (Join-Path $InstalledScripts "BoundedLog.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\RescueMeasurement.cs") -Destination (Join-Path $InstalledScripts "RescueMeasurement.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\DiagnosticReport.cs") -Destination (Join-Path $InstalledScripts "DiagnosticReport.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\DiagnosticPreview.cs") -Destination (Join-Path $InstalledScripts "DiagnosticPreview.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\UiTheme.cs") -Destination (Join-Path $InstalledScripts "UiTheme.cs") -Force
Copy-Item (Join-Path $ReleaseDir "scripts\BrandIcon.cs") -Destination (Join-Path $InstalledScripts "BrandIcon.cs") -Force

Copy-Item (Join-Path $ReleaseDir "scripts\home-vpn") -Destination $InstalledScripts -Recurse -Force

Initialize-ProGoShortcuts -InstallDirectory $InstallDir -WasInstalled $WasInstalled -NoStartup ([bool]$NoStartup) -NoStartMenu ([bool]$NoStartMenuShortcut)

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
