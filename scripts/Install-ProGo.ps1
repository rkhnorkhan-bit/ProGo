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
    $hashes = @{}
    foreach ($name in $required) {
        $hashes[$name] = (Get-FileHash -LiteralPath (Join-Path $payload $name) -Algorithm SHA256).Hash
    }
    return [pscustomobject]@{
        Directory = $payload
        ScriptsDirectory = (Join-Path $payload 'scripts')
        Executable = $exe
        VersionFile = (Join-Path $payload 'VERSION')
        Version = $version
        Files = @($required)
        Hashes = $hashes
    }
}
# Staging admits only the preflight inventory, never arbitrary payload extras.
function Assert-ProGoInstallerStagePath([string]$RootDirectory, [string]$Relative) {
    $rootPath = [IO.Path]::GetFullPath($RootDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $Relative))
    if (-not $path.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Installer stage path escaped its root.'
    }
    for ($probe = $path; $probe.Length -ge $rootPath.Length; $probe = [IO.Path]::GetDirectoryName($probe)) {
        if (Test-Path -LiteralPath $probe) {
            if (([IO.File]::GetAttributes($probe) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Installer stage refuses redirected paths.'
            }
        }
        if ($probe.TrimEnd([IO.Path]::DirectorySeparatorChar) -eq $rootPath) { break }
    }
    return $path
}
function Copy-ProGoInstallerStageFile([string]$Source, [string]$Destination) {
    [IO.File]::Copy($Source, $Destination, $false)
}
function Remove-ProGoInstallerOwnedDirectory([string]$rootPath) {
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $directories = New-Object 'System.Collections.Generic.List[string]'
    $files = New-Object 'System.Collections.Generic.List[string]'
    $pending.Push($rootPath)
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop(); $directories.Add($directory)
        foreach ($entry in [IO.Directory]::GetFileSystemEntries($directory)) {
            $relative = $entry.Substring($rootPath.Length + 1)
            [void](Assert-ProGoInstallerStagePath $rootPath $relative)
            if ([IO.Directory]::Exists($entry)) { $pending.Push($entry) } else { $files.Add($entry) }
        }
    }
    # No recursive deletion that could follow a redirected directory.
    foreach ($file in $files) { [IO.File]::Delete($file) }
    for ($index = $directories.Count - 1; $index -ge 0; $index--) { [IO.Directory]::Delete($directories[$index], $false) }
}
function Remove-ProGoInstallerStage($Stage) {
    $rootPath = [IO.Path]::GetFullPath($Stage.Directory)
    $parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::GetDirectoryName($rootPath) -ne $parent -or
        [IO.Path]::GetFileName($rootPath) -notmatch '^ProGo-install-stage-[0-9a-f]{32}$') {
        throw 'Installer stage cleanup scope is invalid.'
    }
    if (-not [IO.Directory]::Exists($rootPath)) { return }
    if (([IO.File]::GetAttributes($rootPath) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Installer stage root is redirected.' }
    Remove-ProGoInstallerOwnedDirectory $rootPath
}
function New-ProGoInstallerStage($Payload) {
    $directory = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-install-stage-' + [Guid]::NewGuid().ToString('N'))
    $stage = [pscustomobject]@{ Directory = $directory; Payload = $null }
    $created = $false
    try {
        New-Item -ItemType Directory -Path $directory -ErrorAction Stop | Out-Null
        $created = $true
        foreach ($relative in $Payload.Files) {
            $source = Assert-ProGoInstallerStagePath $Payload.Directory $relative
            $destination = Assert-ProGoInstallerStagePath $directory $relative
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            Copy-ProGoInstallerStageFile $source $destination
            if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -cne $Payload.Hashes[$relative]) {
                throw 'Installer payload changed or stage copy is corrupt.'
            }
        }
        $stage.Payload = Get-ProGoInstallPayload -Directory $directory
        foreach ($relative in $Payload.Files) {
            if ($stage.Payload.Hashes[$relative] -cne $Payload.Hashes[$relative]) { throw 'Installer stage verification mismatch.' }
        }
        return $stage
    } catch {
        if ($created) { Remove-ProGoInstallerStage $stage }
        throw
    }
}
# Snapshot only runtime leaves which this installer can replace. Personal files
# are neither read nor backed up here. Verified copies remain for future rollback.
function Copy-ProGoInstallerBackupFile([string]$Source, [string]$Destination) {
    [IO.File]::Copy($Source, $Destination, $false)
}
function Assert-ProGoInstallerBackupScope($Backup) {
    $parent = Assert-ProGoInstallerStagePath $Backup.InstallDirectory 'backups'
    if ([IO.Path]::GetDirectoryName($Backup.Directory) -ne $parent -or
        [IO.Path]::GetFileName($Backup.Directory) -notmatch '^installer-[0-9a-f]{32}$') {
        throw 'Installer backup scope is invalid.'
    }
    [void](Assert-ProGoInstallerStagePath $parent ([IO.Path]::GetFileName($Backup.Directory)))
}
function Remove-ProGoInstallerUnverifiedBackup($Backup) {
    Assert-ProGoInstallerBackupScope $Backup
    if ([IO.Directory]::Exists($Backup.Directory)) { Remove-ProGoInstallerOwnedDirectory $Backup.Directory }
}
function Assert-ProGoInstallerBackup($Backup) {
    if ($null -ne $Backup.Directory) {
        Assert-ProGoInstallerBackupScope $Backup
        $manifest = Assert-ProGoInstallerStagePath $Backup.Directory 'installer-snapshot.json'
        if ((Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash -cne $Backup.ManifestHash) {
            throw 'Installer backup manifest verification failed.'
        }
    }
    foreach ($entry in $Backup.Entries) {
        $target = Assert-ProGoInstallerStagePath $Backup.InstallDirectory $entry.Relative
        if ($entry.Exists) {
            if ($null -eq $Backup.Directory) { throw 'Installer backup is required before overwrite.' }
            $saved = Assert-ProGoInstallerStagePath $Backup.Directory $entry.Relative
            if (-not [IO.File]::Exists($saved) -or (Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash -cne $entry.Hash) {
                throw 'Installer backup bytes verification failed.'
            }
            if (-not [IO.File]::Exists($target) -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $entry.Hash) {
                throw 'Installer runtime changed before overwrite admission.'
            }
        } elseif (Test-Path -LiteralPath $target) { throw 'Installer runtime appeared before overwrite admission.' }
    }
}
function New-ProGoInstallerBackup($Payload, [string]$InstallDirectory) {
    $entries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($relative in $Payload.Files) {
        # Build ships these docs but the existing installer never overwrites them.
        if ($relative -in @('README.md', 'README.ru.md', 'LICENSE.md')) { continue }
        $target = Assert-ProGoInstallerStagePath $InstallDirectory $relative
        if ((Test-Path -LiteralPath $target) -and -not [IO.File]::Exists($target)) { throw 'Installer runtime leaf is not a file.' }
        $exists = [IO.File]::Exists($target)
        $hash = if ($exists) { (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash } else { $null }
        $entries.Add([pscustomobject]@{ Relative = $relative; Exists = $exists; Hash = $hash; NewHash = $Payload.Hashes[$relative] })
    }
    $backup = [pscustomobject]@{ InstallDirectory = [IO.Path]::GetFullPath($InstallDirectory); Directory = $null; Entries = $entries.ToArray(); ManifestHash = $null }
    $created = $false
    try {
        if (@($backup.Entries | Where-Object { $_.Exists }).Count -gt 0) {
            $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
            [IO.Directory]::CreateDirectory($parent) | Out-Null
            $backup.Directory = Join-Path $parent ('installer-' + [Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $backup.Directory -ErrorAction Stop | Out-Null
            $created = $true
            foreach ($entry in $backup.Entries) {
                if (-not $entry.Exists) { continue }
                $source = Assert-ProGoInstallerStagePath $InstallDirectory $entry.Relative
                $saved = Assert-ProGoInstallerStagePath $backup.Directory $entry.Relative
                [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved)) | Out-Null
                Copy-ProGoInstallerBackupFile $source $saved
            }
            $manifest = [pscustomobject]@{ Schema = 1; NewVersion = $Payload.Version; Files = $backup.Entries }
            $manifestPath = Join-Path $backup.Directory 'installer-snapshot.json'
            [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 6), (New-Object System.Text.UTF8Encoding($false)))
            # Round trip verifies that the persisted inventory describes the snapshot.
            $read = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            if ($read.Schema -ne 1 -or $read.NewVersion -cne $Payload.Version -or @($read.Files).Count -ne $backup.Entries.Count) {
                throw 'Installer backup inventory verification failed.'
            }
            for ($index = 0; $index -lt $backup.Entries.Count; $index++) {
                $expected = $backup.Entries[$index]; $actual = $read.Files[$index]
                if ($actual.Relative -cne $expected.Relative -or $actual.Exists -ne $expected.Exists -or
                    $actual.Hash -cne $expected.Hash -or $actual.NewHash -cne $expected.NewHash) { throw 'Installer backup inventory verification failed.' }
            }
            $backup.ManifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
        }
        Assert-ProGoInstallerBackup $backup
        return $backup
    } catch {
        if ($created) { Remove-ProGoInstallerUnverifiedBackup $backup }
        throw
    }
}
# Proof checks do not perform rollback. Original bytes/absence must actually
# match before any caller may describe restoration as verified.
function Assert-ProGoInstallerRuntime($Backup, [switch]$Original) {
    foreach ($entry in $Backup.Entries) {
        $target = Assert-ProGoInstallerStagePath $Backup.InstallDirectory $entry.Relative
        if ($Original -and -not $entry.Exists) {
            if (Test-Path -LiteralPath $target) { throw 'Installer prior runtime is not verified.' }
            continue
        }
        $expected = if ($Original) { $entry.Hash } else { $entry.NewHash }
        if (-not [IO.File]::Exists($target) -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $expected) {
            throw 'Installer runtime bytes are not verified.'
        }
    }
}
function Invoke-ProGoInstallerSelfCheck($Info) {
    return [ProGo.MaintenanceOperation]::RunOwnedSelfCheck($Info, 8000)
}
function Confirm-ProGoInstallerSelfCheck($Backup, $Stage) {
    Assert-ProGoInstallerRuntime $Backup
    $dataRoot = Assert-ProGoInstallerStagePath $Stage.Directory 'self-check-data'
    if (Test-Path -LiteralPath $dataRoot) { throw 'Installer self-check root already exists.' }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $Backup.InstallDirectory 'ProGo.exe'
    $info.Arguments = '--self-check'; $info.WorkingDirectory = $Backup.InstallDirectory
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $dataRoot
    $info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT'] = $dataRoot
    $exit = Invoke-ProGoInstallerSelfCheck $info
    if ($exit -ne 0) { throw 'Installed installer self-check failed.' }
    if (-not [IO.Directory]::Exists($dataRoot)) { throw 'Installer self-check root was not confirmed.' }
    [void](Assert-ProGoInstallerStagePath $Stage.Directory 'self-check-data')
    Assert-ProGoInstallerRuntime $Backup
}
# Use the same owned maintenance protocol as update/restore/uninstall. These
# narrow adapters also let fixture tests replace OS/IPC operations completely.
function Enter-ProGoInstallerMaintenance {
    . (Join-Path $PayloadScripts 'Maintenance-ProGo.ps1')
    return [ProGo.MaintenanceOperation]::Enter()
}
function Stop-ProGoInstallerApplication([string]$InstallDirectory) {
    [ProGo.MaintenanceOperation]::StopApplication($InstallDirectory)
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
$InstallMaintenance = $null
$InstallStage = $null
$InstallBackup = $null
try {
    $InstallMaintenance = Enter-ProGoInstallerMaintenance
    Stop-ProGoInstallerApplication -InstallDirectory $InstallDir
    $InstallStage = New-ProGoInstallerStage $Payload
    $ReleaseDir = $InstallStage.Payload.Directory
    $Exe = $InstallStage.Payload.Executable
    $VersionFile = $InstallStage.Payload.VersionFile
    $PayloadScripts = $InstallStage.Payload.ScriptsDirectory
    $InstallBackup = New-ProGoInstallerBackup $InstallStage.Payload $InstallDir
    Assert-ProGoInstallerBackup $InstallBackup
    $InstalledVersion = $InstallStage.Payload.Version
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

    Confirm-ProGoInstallerSelfCheck $InstallBackup $InstallStage
    Initialize-ProGoShortcuts -InstallDirectory $InstallDir -WasInstalled $WasInstalled -NoStartup ([bool]$NoStartup) -NoStartMenu ([bool]$NoStartMenuShortcut)

} catch {
    $priorVerified = $false
    if ($null -ne $InstallBackup) {
        try { Assert-ProGoInstallerRuntime $InstallBackup -Original; $priorVerified = $true } catch { }
    }
    Write-Host ('Installer failure: priorRuntimeVerified=' + $priorVerified +
        '; rollbackAttempted=False; backupRetained=' + ($null -ne $InstallBackup -and $null -ne $InstallBackup.Directory))
    throw
} finally {
    try {
        if ($null -ne $InstallStage) { Remove-ProGoInstallerStage $InstallStage }
    } finally {
        if ($null -ne $InstallMaintenance) { $InstallMaintenance.Dispose() }
    }
}

Write-Host "Install OK: $InstallDir"
Write-Host "Installed version: $InstalledVersion"
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
