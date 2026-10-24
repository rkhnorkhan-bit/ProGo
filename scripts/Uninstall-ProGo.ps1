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

. (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1')
. (Join-Path $PSScriptRoot 'Firewall-ProGo.ps1')
$Maintenance = [ProGo.MaintenanceOperation]::Enter()
try {
    # Same-user IPC restores owned preferences before the process exits.
    # Missing/refused confirmation aborts before any shortcut or executable removal.
    [ProGo.MaintenanceOperation]::StopApplication($InstallDir)

    # Firewall cleanup is confirmed before any executable/shortcut/data removal.
    # A cancelled elevation or query/removal failure leaves the installation retryable.
    Complete-ProGoHomeFirewallCleanup (Join-Path $InstallDir 'ProGo.exe') (Join-Path $PSScriptRoot 'Enable-HomeVpnFirewall.ps1')

    foreach ($path in @($StartupShortcut, $MenuDir)) {
        if (Test-Path $path) {
            Remove-Item $path -Recurse -Force
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

} finally { $Maintenance.Dispose() }
