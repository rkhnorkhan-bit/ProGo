param(
    [switch]$NoStartup,
    [switch]$NoStartMenuShortcut,
    [switch]$Launch,
    [ValidateNotNullOrEmpty()][string]$PayloadDirectory
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
# An explicit prebuilt payload never falls back to compilation or network access.
# Read-only preflight shared by the source installer and an explicit prebuilt payload.
# No compilation, download, installation, service, elevation or settings changes here.
function Get-ProGoInstallPayload {
    param([Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$Directory)

    $payload = [IO.Path]::GetFullPath($Directory)
    $required = @('ProGo.exe', 'VERSION', 'ProGo.ico', 'README.md', 'README.ru.md', 'LICENSE.md')
    $scriptFiles = @(
        'Install-ProGo.ps1', 'Uninstall-ProGo.ps1',
        'Update-ProGo.ps1', 'Update-ProGo.Core.ps1', 'Restore-ProGoBackup.ps1',
        'Show-ProGo.ps1', 'Start-ProGo.ps1', 'Repair-ProGo.ps1',
        'Enable-HomeVpnFirewall.ps1', 'Maintenance-ProGo.ps1',
        'BackupRetention-ProGo.ps1', 'BackupIntegrity-ProGo.ps1', 'Log-ProGo.ps1',
        'Diagnostics-ProGo.ps1', 'Measure-RescueProGo.ps1', 'Shortcuts-ProGo.ps1',
        'Firewall-ProGo.ps1', 'ApplicationShortcuts.cs', 'MaintenanceOperation.cs',
        'InstalledUpdateTransport.cs', 'UpdateInstallSession.cs', 'BackupRetention.cs',
        'BackupIntegrity.cs', 'BoundedLog.cs', 'RescueMeasurement.cs',
        'DiagnosticReport.cs', 'DiagnosticPreview.cs', 'UiTheme.cs', 'BrandIcon.cs',
        'home-vpn/HOME_IKEV2.md', 'home-vpn/server/ikev2_relay.py',
        'home-vpn/server/install-ikev2-relay.sh', 'home-vpn/server/make_home_profile.py',
        'home-vpn/server/home_vpn_setup.py', 'home-vpn/server/profile_share_setup.py',
        'home-vpn/server/profile_share.py', 'home-vpn/server/qrcodegen.py',
        'home-vpn/server/QR_LICENSE.txt'
    )
    foreach ($name in $scriptFiles) { $required += ('scripts/' + $name) }
    foreach ($name in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $payload $name) -PathType Leaf)) {
            throw "Incomplete local installer payload: $name"
        }
    }
    $version = (Get-Content -LiteralPath (Join-Path $payload 'VERSION') -Raw).Trim()
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid installer payload version.' }
    $exe = Join-Path $payload 'ProGo.exe'
    # GetAssemblyName reads metadata; it does not launch the application or run its code.
    $assembly = [Reflection.AssemblyName]::GetAssemblyName($exe)
    if ($assembly.Name -ne 'ProGo' -or $assembly.Version.ToString(3) -ne $version) {
        throw 'Installer payload executable/version mismatch.'
    }
    return [pscustomobject]@{
        Directory = $payload
        ScriptsDirectory = (Join-Path $payload 'scripts')
        Executable = $exe
        VersionFile = (Join-Path $payload 'VERSION')
        Version = $version
    }
}
if ($PSBoundParameters.ContainsKey('PayloadDirectory')) {
    $Payload = Get-ProGoInstallPayload -Directory $PayloadDirectory
} else {
    $BuildScript = Join-Path $PSScriptRoot "Build-ProGo.ps1"
    & $BuildScript
    $Payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
}
$ReleaseDir = $Payload.Directory
$Exe = $Payload.Executable
$VersionFile = $Payload.VersionFile
$PayloadScripts = $Payload.ScriptsDirectory

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
. (Join-Path $PayloadScripts "Shortcuts-ProGo.ps1")
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
    $scriptPath = Join-Path $PayloadScripts $scriptName
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
