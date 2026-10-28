param(
    [switch]$RemoveUserData
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-ProGoUninstallLayout {
    . (Join-Path $PSScriptRoot 'Layout-ProGo.ps1')
    return Resolve-ProGoRecoveryLayout
}
function Enter-ProGoUninstallMaintenance {
    . (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1')
    return [ProGo.MaintenanceOperation]::Enter()
}
function Stop-ProGoUninstallApplication([string]$RuntimeRoot, [string]$DataRoot) {
    [ProGo.MaintenanceOperation]::StopApplication($RuntimeRoot, $DataRoot)
}
function Complete-ProGoUninstallFirewall([string]$RuntimeRoot) {
    . (Join-Path $PSScriptRoot 'Firewall-ProGo.ps1')
    Complete-ProGoHomeFirewallCleanup (Join-Path $RuntimeRoot 'ProGo.exe') (Join-Path $PSScriptRoot 'Enable-HomeVpnFirewall.ps1')
}
function New-ProGoUninstallShortcuts([string]$RuntimeRoot) {
    Import-ProGoShortcutType
    return New-Object ProGo.ApplicationShortcuts($RuntimeRoot, [Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('Startup'))
}

$Layout = Get-ProGoUninstallLayout
$InstallDir = $Layout.RuntimeRoot
$DataRoot = $Layout.DataRoot
. (Join-Path $PSScriptRoot 'Install-ProGo.ps1') -FunctionsOnly
. (Join-Path $PSScriptRoot 'Shortcuts-ProGo.ps1')
. (Join-Path $PSScriptRoot 'Layout-ProGo.ps1')
$Maintenance = Enter-ProGoUninstallMaintenance
try {
    # A refused stop/cleanup aborts before shortcut, runtime or data mutation.
    Stop-ProGoUninstallApplication $InstallDir $DataRoot
    Complete-ProGoUninstallFirewall $InstallDir
    $Shortcuts = New-ProGoUninstallShortcuts $InstallDir
    $Separate = -not $InstallDir.Equals($DataRoot, [StringComparison]::OrdinalIgnoreCase)
    if ($Separate) {
        $Inventory = @(Get-ProGoUninstallInventory $InstallDir)
        $Shortcuts.RemoveOwnedShortcuts()
        foreach ($entry in $Inventory) { $Shortcuts.RemoveRuntimeFile($entry.Relative, $entry.NewHash) }
    } else {
        # Older legacy installs lack installer terminal evidence. Preserve the
        # historical EXE-only uninstall, with pinned hash/delete and foreign links kept.
        Assert-ProGoInstallerNoPendingJournal $InstallDir
        $Exe = Assert-ProGoInstallerStagePath $InstallDir 'ProGo.exe'
        try { $attributes = [IO.File]::GetAttributes($Exe) }
        catch [IO.FileNotFoundException] { $attributes = $null }
        catch [IO.DirectoryNotFoundException] { $attributes = $null }
        if ($null -ne $attributes) {
            if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { throw 'Uninstall executable is a directory.' }
            # Fixed legacy location permits corrupt/non-managed bytes as before.
            # Hash is change detection, not installation identity or ownership.
            $Hash = (Get-FileHash -LiteralPath $Exe -Algorithm SHA256).Hash
            $identity = $null
            try { $identity = [Reflection.AssemblyName]::GetAssemblyName($Exe) }
            catch [BadImageFormatException] { } # Historical corrupt EXE compatibility.
            if ($null -ne $identity -and $identity.Name -cne 'ProGo') { throw 'Uninstall refuses a foreign executable.' }
            if ((Get-FileHash -LiteralPath $Exe -Algorithm SHA256).Hash -cne $Hash) { throw 'Uninstall executable changed during inspection.' }
            $Shortcuts.RemoveOwnedShortcuts()
            $Shortcuts.RemoveRuntimeFile('ProGo.exe', $Hash)
        } else { $Shortcuts.RemoveOwnedShortcuts() }
    }
    # Recovery evidence and unknown runtime leaves are deliberately retained.
    if ($RemoveUserData) {
        $answer = Read-Host "This removes vault/settings/logs from $DataRoot. Type DELETE to confirm"
        if ($answer -ceq 'DELETE') {
            # Existing separate confirmation remains mandatory. Validate the root
            # and every descendant before the non-reparse directory walk deletes.
            [void](Assert-ProGoRecoveryDirectory $DataRoot $false)
            Remove-ProGoInstallerOwnedDirectory $DataRoot
            Write-Host 'User data removed.'
        } else { Write-Host 'User data kept.' }
    } else { Write-Host "Uninstall OK. User vault/settings/logs kept in $DataRoot." }
} finally { $Maintenance.Dispose() }
