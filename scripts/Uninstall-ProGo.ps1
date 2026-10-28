param(
    [switch]$RemoveUserData
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-ProGoUninstallLayout {
    # Production separate-root admission stays closed until a trusted installation
    # boundary exists. Path equality and self-consistent inventory are insufficient.
    # Synthetic consumers override this resolver at their isolated AST boundary.
    if ([IO.Path]::GetFileName($PSCommandPath) -ieq 'Uninstall-ProGo.retry.ps1') {
        throw 'Production staged uninstall requires a trusted installation boundary.'
    }
    if ([IO.Path]::GetFileName($PSScriptRoot) -ieq 'scripts' -and
        [IO.Path]::GetFileName((Split-Path -Parent $PSScriptRoot)) -ieq 'uninstall-stage') {
        throw 'Invoke the retained Uninstall-ProGo.retry.ps1 entrypoint, not stage-provided code.'
    }
    return Resolve-ProGoRecoveryLayout
}
function Enter-ProGoUninstallMaintenance {
    if (-not ('ProGo.MaintenanceOperation' -as [type])) { . (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1') }
    return [ProGo.MaintenanceOperation]::Enter()
}
function Stop-ProGoUninstallApplication([string]$RuntimeRoot, [string]$DataRoot) {
    [ProGo.MaintenanceOperation]::StopApplication($RuntimeRoot, $DataRoot)
}
function Complete-ProGoUninstallFirewall([string]$RuntimeRoot) {
    if ($null -eq $UninstallStage) { . (Join-Path $PSScriptRoot 'Firewall-ProGo.ps1'); $firewallScripts = $PSScriptRoot }
    else { $firewallScripts = Join-Path $UninstallStage.Directory 'scripts' }
    Complete-ProGoHomeFirewallCleanup (Join-Path $RuntimeRoot 'ProGo.exe') (Join-Path $firewallScripts 'Enable-HomeVpnFirewall.ps1')
}
function New-ProGoUninstallShortcuts([string]$RuntimeRoot) {
    if (-not ('ProGo.ApplicationShortcuts' -as [type])) { Import-ProGoShortcutType }
    return New-Object ProGo.ApplicationShortcuts($RuntimeRoot, [Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('Startup'))
}

# BEGIN embedded admission (exact shared source; checked by Python)
function Get-ProGoInstallerPayloadNames {
    $required = @('ProGo.exe', 'VERSION', 'ProGo.ico', 'README.md', 'README.ru.md', 'LICENSE.md')
    $scriptFiles = @(
        'Install-ProGo.ps1', 'Uninstall-ProGo.ps1',
        'Update-ProGo.ps1', 'Update-ProGo.Core.ps1', 'Restore-ProGoBackup.ps1',
        'Show-ProGo.ps1', 'Start-ProGo.ps1', 'Repair-ProGo.ps1',
        'Enable-HomeVpnFirewall.ps1', 'Maintenance-ProGo.ps1',
        'BackupRetention-ProGo.ps1', 'BackupIntegrity-ProGo.ps1', 'Log-ProGo.ps1',
        'Diagnostics-ProGo.ps1', 'Measure-RescueProGo.ps1', 'Shortcuts-ProGo.ps1',
        'Layout-ProGo.ps1', 'Firewall-ProGo.ps1', 'ApplicationShortcuts.cs', 'MaintenanceOperation.cs',
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
    return $required
}

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

function Assert-ProGoInstallerBackupScope($Backup) {
    $parent = Assert-ProGoInstallerStagePath $Backup.InstallDirectory 'backups'
    if ([IO.Path]::GetDirectoryName($Backup.Directory) -ne $parent -or
        [IO.Path]::GetFileName($Backup.Directory) -notmatch '^installer-[0-9a-f]{32}$') {
        throw 'Installer backup scope is invalid.'
    }
    [void](Assert-ProGoInstallerStagePath $parent ([IO.Path]::GetFileName($Backup.Directory)))
}

function Read-ProGoInstallerJson([string]$Path) {
    if (-not [IO.File]::Exists($Path) -or (Get-Item -LiteralPath $Path).Length -gt 1048576) { throw 'Installer outcome file is missing or oversized.' }
    return ([IO.File]::ReadAllText($Path) | ConvertFrom-Json -ErrorAction Stop)
}

function Get-ProGoInstallerIdentity([string]$InstallDirectory) {
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes([IO.Path]::GetFullPath($InstallDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar).ToUpperInvariant())
    $hash = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hash.ComputeHash($bytes)).Replace('-', '') } finally { $hash.Dispose() }
}

function Assert-ProGoInstallerSnapshotMaterial($Backup) {
    if ($null -ne $Backup.Directory) {
        Assert-ProGoInstallerBackupScope $Backup
        $manifest = Assert-ProGoInstallerStagePath $Backup.Directory 'installer-snapshot.json'
        if ((Get-FileHash -LiteralPath $manifest).Hash -cne $Backup.ManifestHash) { throw 'Installer terminal snapshot identity is invalid.' }
        $saved = Read-ProGoInstallerJson $manifest
        if ($saved.Schema -ne 1 -or ($saved.Files | ConvertTo-Json -Depth 6 -Compress) -cne ($Backup.Entries | ConvertTo-Json -Depth 6 -Compress)) { throw 'Installer terminal snapshot inventory is invalid.' }
    }
    foreach ($entry in $Backup.Entries) {
        if (-not $entry.Exists) { continue }
        if ($null -eq $Backup.Directory) { throw 'Installer terminal snapshot is missing.' }
        $path = Assert-ProGoInstallerStagePath $Backup.Directory $entry.Relative
        if ((Get-FileHash -LiteralPath $path).Hash -cne $entry.Hash) { throw 'Installer terminal snapshot bytes are invalid.' }
    }
}

function Test-ProGoInstallerTerminalOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf, [ValidateSet('installed', 'rolled-back')][string]$Phase, $LiveContext = $null) {
    try {
        if ([IO.Path]::GetFileName($Directory) -cnotmatch '^installer-journal-[a-f0-9]{32}$' -or -not [IO.Directory]::Exists($Directory)) { return $false }
        $validLeaves = if ($Phase -ceq 'installed') { @('installed.json', 'outcome.pending.json') } else { @('rolled-back.json', 'rollback.pending.json') }
        if ([IO.Path]::GetDirectoryName($Directory) -cne (Assert-ProGoInstallerStagePath $InstallDirectory 'backups') -or
            $OutcomeLeaf -notin $validLeaves) { return $false }
        $outcomePath = Assert-ProGoInstallerStagePath $Directory $OutcomeLeaf
        $outcome = Read-ProGoInstallerJson $outcomePath
        $isLive = $null -ne $outcome.PSObject.Properties['Completion'] -and $null -ne $outcome.Completion -and
            $null -ne $outcome.Completion.PSObject.Properties['LiveLegacy']
        if ($isLive) {
            Import-ProGoShortcutType
            if ($Phase -cne 'installed' -or $LiveContext -isnot [ProGo.ApplicationShortcuts+LegacyStartupContext] -or
                -not $LiveContext.MatchesInstalledJournal($Directory,$InstallDirectory) -or -not $LiveContext.VerifyInstalledMetadata($OutcomeLeaf)) { return $false }
        } elseif ($null -ne $LiveContext) { return $false }
        if ($outcome.Schema -ne 1 -or $outcome.Phase -cne $Phase -or
            $outcome.Identity -cne (Get-ProGoInstallerIdentity $InstallDirectory) -or
            $outcome.Journal -cne [IO.Path]::GetFileName($Directory)) { return $false }
        $inventory = $outcome.Inventory | ConvertFrom-Json -ErrorAction Stop
        if ($inventory.Schema -ne 1) { return $false }
        $allowed = @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md', 'README.ru.md', 'LICENSE.md') })
        $entries = @($inventory.Files); $records = @($outcome.Records)
        if ($entries.Count -ne $allowed.Count -or
            ($Phase -ceq 'installed' -and $records.Count -ne $entries.Count) -or
            ($Phase -ceq 'rolled-back' -and ($records.Count -lt 1 -or $records.Count -gt $entries.Count)) -or
            @(Get-ChildItem -LiteralPath $Directory -Force).Count -ne ($records.Count + 1)) { return $false }
        $seen = @{}; $admitted = @{}
        foreach ($entry in $entries) {
            if ($entry.Relative -cnotin $allowed -or $seen.ContainsKey($entry.Relative) -or
                $entry.Exists -isnot [bool] -or $entry.NewHash -cnotmatch '^[A-F0-9]{64}$') { return $false }
            if (($entry.Exists -and $entry.Hash -cnotmatch '^[A-F0-9]{64}$') -or (-not $entry.Exists -and $null -ne $entry.Hash)) { return $false }
            $seen[$entry.Relative] = $entry
        }
        $snapshot = $null
        if ($null -ne $outcome.Snapshot) {
            if ($outcome.Snapshot -cnotmatch '^installer-[a-f0-9]{32}$') { return $false }
            $snapshot = Assert-ProGoInstallerStagePath (Join-Path $InstallDirectory 'backups') $outcome.Snapshot
        }
        if ($null -eq $snapshot -and $null -ne $inventory.SnapshotHash) { return $false }
        $backup = [pscustomobject]@{ InstallDirectory = $InstallDirectory; Directory = $snapshot; ManifestHash = $inventory.SnapshotHash; Entries = $entries }
        Assert-ProGoInstallerSnapshotMaterial $backup
        for ($index = 0; $index -lt $records.Count; $index++) {
            $proof = $records[$index]; $name = ('admit-{0:D6}.json' -f $index)
            if ($proof.Name -cne $name -or $proof.Hash -cnotmatch '^[A-F0-9]{64}$') { return $false }
            $path = Assert-ProGoInstallerStagePath $Directory $name
            if ((Get-FileHash -LiteralPath $path).Hash -cne $proof.Hash) { return $false }
            $record = Read-ProGoInstallerJson $path
            if ($record.Schema -ne 1 -or $record.Phase -cne 'admitted' -or $record.Sequence -ne $index -or
                $record.Inventory -cne $outcome.Inventory -or -not $seen.ContainsKey($record.Entry.Relative) -or
                $admitted.ContainsKey($record.Entry.Relative) -or
                ($record.Entry | ConvertTo-Json -Compress) -cne ($seen[$record.Entry.Relative] | ConvertTo-Json -Compress)) { return $false }
            $admitted[$record.Entry.Relative] = $true
        }
        if ($Phase -ceq 'rolled-back') {
            if ($outcome.RuntimeState -cne 'original-verified' -or @($outcome.Ownership).Count -ne $records.Count) { return $false }
            $owners = @{}
            foreach ($owned in $outcome.Ownership) {
                if (-not $admitted.ContainsKey($owned.Relative) -or $owners.ContainsKey($owned.Relative) -or
                    $owned.OwnedHash -cnotmatch '^[A-F0-9]{64}$') { return $false }
                $owners[$owned.Relative] = $true
            }
        }
        return $true
    } catch { return $false }
}

function Test-ProGoInstallerInstalledOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf = 'installed.json', $LiveContext = $null) {
    return (Test-ProGoInstallerTerminalOutcome $Directory $InstallDirectory $OutcomeLeaf 'installed' $LiveContext)
}

function Test-ProGoInstallerRollbackOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf = 'rolled-back.json') {
    return (Test-ProGoInstallerTerminalOutcome $Directory $InstallDirectory $OutcomeLeaf 'rolled-back')
}

function Assert-ProGoInstallerNoAuxiliaryResult([string]$InstallDirectory, [switch]$AllowStartupAdmission) {
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    if (-not (Test-Path -LiteralPath $parent)) { return }
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        if ($item.Name.StartsWith('installer-startup-outcome', [StringComparison]::OrdinalIgnoreCase) -or
            $item.Name.StartsWith('installer-menu-outcome', [StringComparison]::OrdinalIgnoreCase) -or
            (-not $AllowStartupAdmission -and ($item.Name.StartsWith('installer-startup-admission', [StringComparison]::OrdinalIgnoreCase) -or
                $item.Name.StartsWith('installer-menu-admission', [StringComparison]::OrdinalIgnoreCase)))) {
            throw 'Installer auxiliary outcome requires verified completion before a new installation.'
        }
    }
}

function Assert-ProGoInstallerNoPendingJournal([string]$InstallDirectory) {
    Assert-ProGoInstallerNoAuxiliaryResult $InstallDirectory
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    if (-not (Test-Path -LiteralPath $parent)) { return }
    if (-not [IO.Directory]::Exists($parent)) { throw 'Installer journal parent is invalid.' }
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        if (-not $item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase)) { continue }
        # Includes malformed names, files and empty/unknown records: none authorize a write.
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if (Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) { continue }
        if (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory) { continue }
        throw 'Installer pending journal requires verified recovery before a new installation.'
    }
}

function Get-ProGoUninstallInventory([string]$RuntimeRoot) {
    Assert-ProGoInstallerNoPendingJournal $RuntimeRoot
    $parent = Assert-ProGoInstallerStagePath $RuntimeRoot 'backups'
    $candidates = @()
    if ([IO.Directory]::Exists($parent)) {
        foreach ($directory in Get-ChildItem -LiteralPath $parent -Directory -Force) {
            if ($directory.Name -cnotmatch '^installer-journal-[a-f0-9]{32}$') { continue }
            if (-not (Test-ProGoInstallerInstalledOutcome $directory.FullName $RuntimeRoot)) { continue }
            $outcome = Read-ProGoInstallerJson (Join-Path $directory.FullName 'installed.json')
            $inventory = $outcome.Inventory | ConvertFrom-Json -ErrorAction Stop
            $completion = $outcome.Completion
            if ($completion.Schema -ne 1 -or @($completion.Phases).Count -ne 2 -or
                $completion.Phases[0] -cne 'self-check' -or $completion.Phases[1] -cne 'shortcuts' -or
                @($completion.Ownership).Count -ne @($inventory.Files).Count) { continue }
            $seen = @{}; $valid = $true
            foreach ($owner in $completion.Ownership) {
                $entry = @($inventory.Files | Where-Object { $_.Relative -ceq $owner.Relative })
                if ($seen.ContainsKey($owner.Relative) -or $entry.Count -ne 1 -or $owner.OwnedHash -cne $entry[0].NewHash) { $valid = $false; break }
                $seen[$owner.Relative] = $true
            }
            if (-not $valid) { continue }
            $valid = $true
            foreach ($entry in $inventory.Files) {
                $path = Assert-ProGoInstallerStagePath $RuntimeRoot $entry.Relative
                try { $attributes = [IO.File]::GetAttributes($path) }
                catch [IO.FileNotFoundException] { continue }
                catch [IO.DirectoryNotFoundException] { continue }
                if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
                    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $entry.NewHash) { $valid = $false; break }
            }
            if ($valid) { $candidates += ,@($inventory.Files) }
        }
    }
    if ($candidates.Count -eq 0) { throw 'Uninstall requires a verified installed runtime inventory.' }
    # Multiple historical outcomes are acceptable only for identical new bytes.
    $signature = @($candidates[0] | ForEach-Object { $_.Relative + ':' + $_.NewHash }) -join '|'
    foreach ($candidate in $candidates) {
        if ((@($candidate | ForEach-Object { $_.Relative + ':' + $_.NewHash }) -join '|') -cne $signature) {
            throw 'Uninstall runtime inventory is ambiguous.'
        }
    }
    return $candidates[0]
}
# Read-only layout selection. This is not an authority for privileged mutation.
function Assert-ProGoLayoutPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    for ($probe = $full; $probe; $probe = [IO.Path]::GetDirectoryName($probe)) {
        if ([IO.File]::Exists($probe) -or [IO.Directory]::Exists($probe)) {
            if (([IO.File]::GetAttributes($probe) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Layout refuses a reparse point.'
            }
        } else { throw 'Layout path is missing or inaccessible.' }
    }
    return $full
}

# Internal calculation shared with isolated tests. Confirmation is deliberately not
# exposed by the production entrypoint: a boolean is not a trusted installation identity.
function Resolve-ProGoLayoutCore {
    param([string]$DataBase, [string]$ProgramBase, [string]$RuntimeRoot,
          [bool]$SeparateIdentityConfirmed = $false, [switch]$AllowMissingExecutable)
    if ([string]::IsNullOrWhiteSpace($DataBase) -or [string]::IsNullOrWhiteSpace($ProgramBase) -or
        [string]::IsNullOrWhiteSpace($RuntimeRoot)) { throw 'Layout roots are required.' }
    $data = [IO.Path]::GetFullPath((Join-Path $DataBase 'ProGo')).TrimEnd('\')
    $program = [IO.Path]::GetFullPath((Join-Path $ProgramBase 'ProGo')).TrimEnd('\')
    $runtime = [IO.Path]::GetFullPath($RuntimeRoot).TrimEnd('\')
    if ($data.Equals($program, [StringComparison]::OrdinalIgnoreCase) -or
        $program.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $data.StartsWith($program + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Layout roots overlap.'
    }
    if ($runtime.Equals($data, [StringComparison]::OrdinalIgnoreCase)) { $kind = 'Legacy' }
    elseif ($runtime.Equals($program, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not $SeparateIdentityConfirmed) { throw 'Separate runtime identity is not confirmed.' }
        $kind = 'Separate'
    } else { throw 'Foreign runtime root.' }
    [void](Assert-ProGoLayoutPath $data)
    [void](Assert-ProGoLayoutPath $runtime)
    $exe = Join-Path $runtime 'ProGo.exe'
    try { $attributes = [IO.File]::GetAttributes($exe) }
    catch [IO.FileNotFoundException] {
        if (-not $AllowMissingExecutable) { throw 'Layout path is missing or inaccessible.' }
        # This observation is read-only, not authority to create or overwrite.
        return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = $exe; ExecutablePresent = $false }
    }
    if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Layout refuses a reparse point.' }
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { throw 'Runtime executable is not a regular file.' }
    [void](Assert-ProGoLayoutPath $exe)
    try { $identity = [Reflection.AssemblyName]::GetAssemblyName($exe) }
    catch { throw 'Runtime executable is not a valid managed payload.' }
    if ($identity.Name -cne 'ProGo') { throw 'Foreign runtime executable.' }
    return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = $exe; ExecutablePresent = $true }
}

# Recovery is structural preflight only. Repair's existing lease, admission,
# journal, hash and rollback checks remain the authority for its writes.
function Assert-ProGoRecoveryDirectory([string]$Path, [bool]$AllowMissingLeaf) {
    $full = [IO.Path]::GetFullPath($Path)
    $first = $true
    for ($probe = $full; $probe; $probe = [IO.Path]::GetDirectoryName($probe)) {
        try { $attributes = [IO.File]::GetAttributes($probe) }
        catch [IO.FileNotFoundException] { if ($first -and $AllowMissingLeaf) { $first = $false; continue }; throw }
        catch [IO.DirectoryNotFoundException] { if ($first -and $AllowMissingLeaf) { $first = $false; continue }; throw }
        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Recovery layout refuses a reparse point.' }
        if (($attributes -band [IO.FileAttributes]::Directory) -eq 0) { throw 'Recovery layout requires directories.' }
        $first = $false
    }
    return $full
}

function Resolve-ProGoRecoveryLayoutCore {
    param([string]$DataBase, [string]$ProgramBase, [string]$RuntimeRoot,
          [bool]$SeparateIdentityConfirmed = $false)
    if ([string]::IsNullOrWhiteSpace($DataBase) -or [string]::IsNullOrWhiteSpace($ProgramBase) -or [string]::IsNullOrWhiteSpace($RuntimeRoot)) { throw 'Layout roots are required.' }
    $data = [IO.Path]::GetFullPath((Join-Path $DataBase 'ProGo')).TrimEnd('\')
    $program = [IO.Path]::GetFullPath((Join-Path $ProgramBase 'ProGo')).TrimEnd('\')
    $runtime = [IO.Path]::GetFullPath($RuntimeRoot).TrimEnd('\')
    if ($data.Equals($program, [StringComparison]::OrdinalIgnoreCase) -or $program.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase) -or $data.StartsWith($program + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Layout roots overlap.' }
    if ($runtime.Equals($data, [StringComparison]::OrdinalIgnoreCase)) { $kind = 'Legacy' }
    elseif ($runtime.Equals($program, [StringComparison]::OrdinalIgnoreCase) -and $SeparateIdentityConfirmed) { $kind = 'Separate' }
    else { throw 'Recovery runtime root is foreign or unconfirmed.' }
    [void](Assert-ProGoRecoveryDirectory $data $true)
    [void](Assert-ProGoRecoveryDirectory $runtime $true)
    # Inspect an existing EXE without interpreting its contents. No suppressed
    # Exists result may authorize an inaccessible, redirected or directory leaf.
    try { $attributes = [IO.File]::GetAttributes((Join-Path $runtime 'ProGo.exe')) }
    catch [IO.FileNotFoundException] { $attributes = $null }
    catch [IO.DirectoryNotFoundException] { $attributes = $null }
    if ($null -ne $attributes -and ($attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::Directory))) { throw 'Recovery executable is redirected or a directory.' }
    return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = (Join-Path $runtime 'ProGo.exe') }
}

function Resolve-ProGoRecoveryLayout {
    $dataBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $programBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if ([string]::IsNullOrWhiteSpace($dataBase) -or [string]::IsNullOrWhiteSpace($programBase)) { throw 'Windows known folders are unavailable.' }
    return Resolve-ProGoRecoveryLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot (Join-Path $dataBase 'ProGo')
}

function Resolve-ProGoLayout {
    param([switch]$AllowMissingExecutable)
    # Known folders come from Windows, never from environment overrides or metadata.
    $dataBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $programBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if ([string]::IsNullOrWhiteSpace($dataBase) -or [string]::IsNullOrWhiteSpace($programBase)) {
        throw 'Windows known folders are unavailable.'
    }
    $legacy = Join-Path $dataBase 'ProGo'
    return Resolve-ProGoLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot $legacy -AllowMissingExecutable:$AllowMissingExecutable
}
# END embedded admission

# Stage has no manifest, path override, command or root supplied by metadata.
# Retained under recovery evidence; never included in runtime deletion inventory.
function Get-ProGoUninstallStageNames {
    return @('Uninstall-ProGo.ps1', 'Install-ProGo.ps1', 'Layout-ProGo.ps1',
        'Shortcuts-ProGo.ps1', 'ApplicationShortcuts.cs', 'Maintenance-ProGo.ps1',
        'MaintenanceOperation.cs', 'Firewall-ProGo.ps1', 'Enable-HomeVpnFirewall.ps1')
}
function Assert-ProGoUninstallStageTree([string]$Directory, [string[]]$Names) {
    [void](Assert-ProGoRecoveryDirectory $Directory $false)
    $scripts = Assert-ProGoInstallerStagePath $Directory 'scripts'
    [void](Assert-ProGoRecoveryDirectory $scripts $false)
    foreach ($item in Get-ChildItem -LiteralPath $Directory -Force -ErrorAction Stop) {
        if ($item.Name -cne 'scripts' -or -not $item.PSIsContainer) { throw 'Uninstall stage contains a foreign leaf.' }
    }
    foreach ($item in Get-ChildItem -LiteralPath $scripts -Force -ErrorAction Stop) {
        [void](Assert-ProGoInstallerStagePath $Directory ('scripts/' + $item.Name))
        if ($item.Name -cnotin $Names -or $item.PSIsContainer) { throw 'Uninstall stage contains a foreign leaf.' }
    }
}
function Initialize-ProGoUninstallStagePins {
    if ('ProGo.UninstallStageDirectoryPins' -as [type]) { return }
    # Trusted entrypoint code, independent of all stage-provided bytes. Same native
    # directory-open contract as ApplicationShortcuts.PinLegacySourceDirectory.
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace ProGo {
    public sealed class UninstallStageDirectoryPins : IDisposable {
        private readonly List<SafeFileHandle> handles = new List<SafeFileHandle>();
        [StructLayout(LayoutKind.Sequential)]
        private struct Info {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME Created, Accessed, Written;
            public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
        [DllImport("kernel32.dll", EntryPoint="CreateFileW", CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern SafeFileHandle Open(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out Info info);
        public void Pin(string path) {
            var handle = Open(Path.GetFullPath(path), 0, 1, IntPtr.Zero, 3, 0x02200000u, IntPtr.Zero);
            Info info;
            if (handle.IsInvalid || !GetFileInformationByHandle(handle, out info) || (info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0) {
                handle.Dispose(); throw new IOException("Uninstall stage directory is unavailable or redirected.");
            }
            handles.Add(handle);
        }
        public void PinAncestors(string path) {
            var paths = new Stack<string>();
            for (string p = Path.GetFullPath(path); p != null; p = Path.GetDirectoryName(p)) paths.Push(p);
            while (paths.Count != 0) Pin(paths.Pop());
        }
        public void Dispose() {
            for (int n = handles.Count - 1; n >= 0; n--) handles[n].Dispose();
            handles.Clear();
        }
    }
}
'@
}

function Open-ProGoUninstallStage([string]$RuntimeRoot, $Inventory) {
    [void](Assert-ProGoRecoveryDirectory $RuntimeRoot $false)
    $directory = Assert-ProGoInstallerStagePath $RuntimeRoot 'backups/uninstall-stage'
    $names = @(Get-ProGoUninstallStageNames)
    Initialize-ProGoUninstallStagePins
    $directoryPins = New-Object ProGo.UninstallStageDirectoryPins
    $stage = [pscustomobject]@{ Directory = $directory; RuntimeRoot = $RuntimeRoot; Pins = @{}; Hashes = @{}; Text = @{}; DirectoryPins = $directoryPins; RetryPath = (Assert-ProGoInstallerStagePath $RuntimeRoot 'backups/Uninstall-ProGo.retry.ps1'); RetryPin = $null; RetryHash = $null }
    try {
        # No repair-by-overwrite: foreign/corrupt/replaced stage bytes are refusal.
        # A crash during initial copying may resume only missing leaves, from the
        # still-admitted runtime source, never from arbitrary stage metadata.
        $directoryPins.PinAncestors($RuntimeRoot)
        $directoryPins.Pin((Join-Path $RuntimeRoot 'backups'))
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        $directoryPins.Pin($directory)
        [void](Assert-ProGoRecoveryDirectory $directory $false)
        $scripts = Assert-ProGoInstallerStagePath $directory 'scripts'
        [IO.Directory]::CreateDirectory($scripts) | Out-Null
        $directoryPins.Pin($scripts)
        Assert-ProGoUninstallStageTree $directory $names
        foreach ($name in $names) {
            $relative = 'scripts/' + $name
            $entries = @($Inventory | Where-Object { $_.Relative -ceq $relative })
            if ($entries.Count -ne 1) { throw 'Uninstall stage dependency lacks installed ownership.' }
            $expected = $entries[0].NewHash
            $destination = Assert-ProGoInstallerStagePath $directory $relative
            if (-not [IO.File]::Exists($destination)) {
                $source = Assert-ProGoInstallerStagePath $RuntimeRoot $relative
                $inputStream = [IO.File]::Open($source, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                try {
                    if ((Get-ProGoUninstallStreamHash $inputStream) -cne $expected) { throw 'Uninstall stage source changed.' }
                    $outputStream = [IO.File]::Open($destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                    try { $inputStream.CopyTo($outputStream); $outputStream.Flush($true) } finally { $outputStream.Dispose() }
                } finally { $inputStream.Dispose() }
            }
            $pin = [IO.File]::Open($destination, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
            $stage.Pins[$relative] = $pin
            if ((Get-ProGoUninstallStreamHash $pin) -cne $expected) { throw 'Uninstall stage bytes differ from installed ownership.' }
            $stage.Hashes[$relative] = $expected
            $reader = New-Object IO.StreamReader($pin, [Text.Encoding]::UTF8, $true, 4096, $true)
            try { $stage.Text[$relative] = $reader.ReadToEnd() } finally { $reader.Dispose(); $pin.Position = 0 }
        }
        # Retain installed entrypoint code OUTSIDE the dependency stage. This is
        # the caller's trusted code boundary, not an executable stage manifest.
        # Its bytes are the same installed Uninstall source admitted above.
        $stage.RetryHash = $stage.Hashes['scripts/Uninstall-ProGo.ps1']
        if (-not [IO.File]::Exists($stage.RetryPath)) {
            $stream = [IO.File]::Open($stage.RetryPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try {
                $inputStream = $stage.Pins['scripts/Uninstall-ProGo.ps1']; $inputStream.Position = 0
                $inputStream.CopyTo($stream); $stream.Flush($true); $inputStream.Position = 0
            } finally { $stream.Dispose() }
        }
        $stage.RetryPin = [IO.File]::Open($stage.RetryPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        Assert-ProGoUninstallStage $stage
        return $stage
    } catch {
        foreach ($pin in $stage.Pins.Values) { $pin.Dispose() }
        if ($null -ne $stage.RetryPin) { $stage.RetryPin.Dispose() }
        $directoryPins.Dispose()
        throw
    }
}
function Get-ProGoUninstallStreamHash($Stream) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $Stream.Position = 0
        $hash = [BitConverter]::ToString($algorithm.ComputeHash($Stream)).Replace('-', '')
        $Stream.Position = 0
        return $hash
    } finally { $algorithm.Dispose() }
}
function Assert-ProGoUninstallStage($Stage) {
    [void](Assert-ProGoInstallerStagePath $Stage.RuntimeRoot 'backups/Uninstall-ProGo.retry.ps1')
    if ((Get-ProGoUninstallStreamHash $Stage.RetryPin) -cne $Stage.RetryHash -or
        (Get-FileHash -LiteralPath $Stage.RetryPath -Algorithm SHA256).Hash -cne $Stage.RetryHash) {
        throw 'Uninstall retry entry differs from installed ownership.'
    }
    Assert-ProGoUninstallStageTree $Stage.Directory @(Get-ProGoUninstallStageNames)
    foreach ($relative in $Stage.Pins.Keys) {
        $path = Assert-ProGoInstallerStagePath $Stage.Directory $relative
        # Both the held bytes and current namespace must match installed evidence.
        if ((Get-ProGoUninstallStreamHash $Stage.Pins[$relative]) -cne $Stage.Hashes[$relative] -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $Stage.Hashes[$relative]) {
            throw 'Uninstall stage changed before mutation.'
        }
    }
}
function Import-ProGoUninstallStage($Stage) {
    Assert-ProGoUninstallStage $Stage
    # Execute only admitted, pinned bytes read into memory, never a second path read.
    foreach ($name in @('ApplicationShortcuts.cs', 'MaintenanceOperation.cs')) {
        $typeName = if ($name -ceq 'ApplicationShortcuts.cs') { 'ProGo.ApplicationShortcuts' } else { 'ProGo.MaintenanceOperation' }
        if (-not ($typeName -as [type])) { Add-Type -TypeDefinition $Stage.Text['scripts/' + $name] }
    }

}

$Layout = Get-ProGoUninstallLayout
$InstallDir = $Layout.RuntimeRoot
$DataRoot = $Layout.DataRoot
$Separate = -not $InstallDir.Equals($DataRoot, [StringComparison]::OrdinalIgnoreCase)
$UninstallStage = $null
$Maintenance = $null
try {
    if ($Separate) {
        $Inventory = @(Get-ProGoUninstallInventory $InstallDir)
        $UninstallStage = Open-ProGoUninstallStage $InstallDir $Inventory
        Import-ProGoUninstallStage $UninstallStage
        # Load the admitted firewall definitions in this script scope, from pinned bytes.
        . ([scriptblock]::Create($UninstallStage.Text['scripts/Firewall-ProGo.ps1']))
        Write-Host ('Retry uninstall with: powershell.exe -NoProfile -File "' + $UninstallStage.RetryPath + '"')
    } else {
        . (Join-Path $PSScriptRoot 'Install-ProGo.ps1') -FunctionsOnly
        . (Join-Path $PSScriptRoot 'Shortcuts-ProGo.ps1')
    }
    $Maintenance = Enter-ProGoUninstallMaintenance
    # A refused stop/cleanup aborts before shortcut, runtime or data mutation.
    if ($Separate) { Assert-ProGoUninstallStage $UninstallStage; $Inventory = @(Get-ProGoUninstallInventory $InstallDir) }
    Stop-ProGoUninstallApplication $InstallDir $DataRoot
    if ($Separate) { Assert-ProGoUninstallStage $UninstallStage }
    Complete-ProGoUninstallFirewall $InstallDir
    $Shortcuts = New-ProGoUninstallShortcuts $InstallDir
    if ($Separate) {
        Assert-ProGoUninstallStage $UninstallStage
        $Inventory = @(Get-ProGoUninstallInventory $InstallDir)
        $Shortcuts.RemoveOwnedShortcuts()
        foreach ($entry in $Inventory) {
            Assert-ProGoUninstallStage $UninstallStage
            $Shortcuts.RemoveRuntimeFile($entry.Relative, $entry.NewHash)
        }
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
} finally {
    try { if ($null -ne $Maintenance) { $Maintenance.Dispose() } }
    finally {
        if ($null -ne $UninstallStage) {
            foreach ($pin in $UninstallStage.Pins.Values) { $pin.Dispose() }
            if ($null -ne $UninstallStage.RetryPin) { $UninstallStage.RetryPin.Dispose() }
            $UninstallStage.DirectoryPins.Dispose()
        }
    }
}
