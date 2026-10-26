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
function Get-ProGoInstallerPayloadNames {
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
    return $required
}
function Get-ProGoInstallPayload {
    param([Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$Directory)

    $payload = [IO.Path]::GetFullPath($Directory)
    $required = @(Get-ProGoInstallerPayloadNames)
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
# Each write keeps an exclusive target handle through partial-byte hashing.
function Get-ProGoInstallerStreamHash($Stream) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $Stream.Position = 0
        $hash = [BitConverter]::ToString($algorithm.ComputeHash($Stream)).Replace('-', '')
        $Stream.Position = 0
        return $hash
    } finally { $algorithm.Dispose() }
}
function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
    $Target.SetLength(0); $Source.CopyTo($Target); $Target.Flush($true)
}
# Append-only session intent, persisted before even creating a new runtime leaf.
# A persisted intent is NOT proof of post-crash ownership of partial runtime bytes.
function Write-ProGoInstallerJournalBytes($Stream, [byte[]]$Bytes) {
    $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
}
# Terminal state is evidence of a completed install, never authorization to restore.
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
function Test-ProGoInstallerTerminalOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf, [ValidateSet('installed', 'rolled-back')][string]$Phase) {
    try {
        if ([IO.Path]::GetFileName($Directory) -cnotmatch '^installer-journal-[a-f0-9]{32}$' -or -not [IO.Directory]::Exists($Directory)) { return $false }
        $validLeaves = if ($Phase -ceq 'installed') { @('installed.json', 'outcome.pending.json') } else { @('rolled-back.json', 'rollback.pending.json') }
        if ([IO.Path]::GetDirectoryName($Directory) -cne (Assert-ProGoInstallerStagePath $InstallDirectory 'backups') -or
            $OutcomeLeaf -notin $validLeaves) { return $false }
        $outcomePath = Assert-ProGoInstallerStagePath $Directory $OutcomeLeaf
        $outcome = Read-ProGoInstallerJson $outcomePath
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
function Test-ProGoInstallerInstalledOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf = 'installed.json') {
    return (Test-ProGoInstallerTerminalOutcome $Directory $InstallDirectory $OutcomeLeaf 'installed')
}
function Test-ProGoInstallerRollbackOutcome([string]$Directory, [string]$InstallDirectory, [string]$OutcomeLeaf = 'rolled-back.json') {
    return (Test-ProGoInstallerTerminalOutcome $Directory $InstallDirectory $OutcomeLeaf 'rolled-back')
}
function Complete-ProGoInstallerRollbackOutcome($Backup, $Writes) {
    Assert-ProGoInstallerJournal $Backup.Journal $Backup
    Assert-ProGoInstallerSnapshotMaterial $Backup
    Assert-ProGoInstallerRuntime $Backup -Original
    if ($Writes.Count -lt 1 -or $Writes.Count -ne $Backup.Journal.Records.Count -or $Writes.Count -gt $Backup.Entries.Count) {
        throw 'Installer rollback terminal ownership is incomplete.'
    }
    $owners = @{}
    foreach ($owned in $Writes) {
        if ($owners.ContainsKey($owned.Relative) -or $owned.OwnedHash -cnotmatch '^[A-F0-9]{64}$' -or
            @($Backup.Entries | Where-Object { $_.Relative -ceq $owned.Relative }).Count -ne 1) { throw 'Installer rollback terminal ownership is invalid.' }
        $owners[$owned.Relative] = $true
    }
    $proofs = @($Backup.Journal.Records | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Hash = (Get-FileHash -LiteralPath (Assert-ProGoInstallerStagePath $Backup.Journal.Directory $_.Name)).Hash }
    })
    $text = ([pscustomobject]@{ Schema = 1; Phase = 'rolled-back'; RuntimeState = 'original-verified';
        Identity = (Get-ProGoInstallerIdentity $Backup.InstallDirectory); Journal = [IO.Path]::GetFileName($Backup.Journal.Directory);
        Snapshot = $(if ($null -eq $Backup.Directory) { $null } else { [IO.Path]::GetFileName($Backup.Directory) });
        Inventory = $Backup.Journal.Inventory; Records = $proofs; Ownership = @($Writes | ForEach-Object {
            [pscustomobject]@{ Relative = $_.Relative; OwnedHash = $_.OwnedHash }
        }) } | ConvertTo-Json -Depth 8 -Compress)
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($text)
    $path = Assert-ProGoInstallerStagePath $Backup.Journal.Directory 'rollback.pending.json'
    $stream = New-Object IO.FileStream -ArgumentList @($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 4096, [IO.FileOptions]::WriteThrough)
    try {
        Write-ProGoInstallerRollbackOutcomeBytes $stream $bytes
        $stream.Flush($true); $stream.Position = 0
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false)), $false, 4096, $true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Installer rollback outcome readback is invalid.' } } finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
    if (-not (Test-ProGoInstallerRollbackOutcome $Backup.Journal.Directory $Backup.InstallDirectory 'rollback.pending.json')) { throw 'Installer rollback terminal is not verified.' }
    [IO.File]::Move($path, (Assert-ProGoInstallerStagePath $Backup.Journal.Directory 'rolled-back.json'))
}
function Write-ProGoInstallerRollbackOutcomeBytes($Stream, [byte[]]$Bytes) {
    $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
}
function Complete-ProGoInstallerInstalledOutcome($Backup, $Writes, [string[]]$CompletedPhases = @(), $StartupEvidence = $null, [switch]$RequireStartupEvidence) {
    Assert-ProGoInstallerJournal $Backup.Journal $Backup
    Assert-ProGoInstallerSnapshotMaterial $Backup
    Assert-ProGoInstallerRuntime $Backup
    if ($Writes.Count -ne $Backup.Entries.Count -or $Backup.Journal.Records.Count -ne $Writes.Count) { throw 'Installer terminal ownership is incomplete.' }
    foreach ($entry in $Backup.Entries) {
        $owned = @($Writes | Where-Object { $_.Relative -ceq $entry.Relative })
        if ($owned.Count -ne 1 -or $owned[0].OwnedHash -cne $entry.NewHash) { throw 'Installer terminal owned bytes are invalid.' }
    }
    $proofs = @($Backup.Journal.Records | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Hash = (Get-FileHash -LiteralPath (Assert-ProGoInstallerStagePath $Backup.Journal.Directory $_.Name)).Hash }
    })
    if ($RequireStartupEvidence -and $null -eq $StartupEvidence) { throw 'Installer startup completion evidence is missing.' }
    if ($null -ne $StartupEvidence -and ($StartupEvidence.Schema -ne 1 -or
        $StartupEvidence.InstallIdentity -cne (Get-ProGoInstallerIdentity $Backup.InstallDirectory))) {
        throw 'Installer startup evidence identity is invalid.'
    }
    if ($RequireStartupEvidence -and ($null -eq $StartupEvidence.Menu -or $StartupEvidence.Menu.Schema -ne 1 -or
        $StartupEvidence.Menu.InstallIdentity -cne (Get-ProGoInstallerIdentity $Backup.InstallDirectory))) {
        throw 'Installer menu evidence identity is invalid.'
    }
    if ($RequireStartupEvidence -and ($null -eq $StartupEvidence.Legacy -or $StartupEvidence.Legacy.Schema -ne 1 -or
        $StartupEvidence.Legacy.InstallIdentity -cne (Get-ProGoInstallerIdentity $Backup.InstallDirectory))) {
        throw 'Installer legacy evidence identity is invalid.'
    }
    $text = ([pscustomobject]@{ Schema = 1; Phase = 'installed'; Identity = (Get-ProGoInstallerIdentity $Backup.InstallDirectory);
        Journal = [IO.Path]::GetFileName($Backup.Journal.Directory); Snapshot = $(if ($null -eq $Backup.Directory) { $null } else { [IO.Path]::GetFileName($Backup.Directory) });
        Inventory = $Backup.Journal.Inventory; Records = $proofs;
        Completion = [pscustomobject]@{ Schema = 1; Phases = @($CompletedPhases); Startup = $StartupEvidence; Ownership = @($Writes | ForEach-Object {
            [pscustomobject]@{ Relative = $_.Relative; OwnedHash = $_.OwnedHash }
        }) } } | ConvertTo-Json -Depth 8 -Compress)
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($text)
    $path = Assert-ProGoInstallerStagePath $Backup.Journal.Directory 'outcome.pending.json'
    $stream = New-Object IO.FileStream -ArgumentList @($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 4096, [IO.FileOptions]::WriteThrough)
    try {
        Write-ProGoInstallerOutcomeBytes $stream $bytes
        $stream.Flush($true); $stream.Position = 0
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false)), $false, 4096, $true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Installer terminal outcome readback is invalid.' } } finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
    if (-not (Test-ProGoInstallerInstalledOutcome $Backup.Journal.Directory $Backup.InstallDirectory 'outcome.pending.json')) { throw 'Installer terminal outcome is not verified.' }
    if ($CompletedPhases.Count -gt 0 -and -not (Test-ProGoInstallerPendingInstalledProof $Backup.Journal.Directory $Backup.InstallDirectory)) { throw 'Installer completion phase proof is not verified.' }
    # Atomic publication is the terminal boundary; no fallible post-publish validation.
    [IO.File]::Move($path, (Assert-ProGoInstallerStagePath $Backup.Journal.Directory 'installed.json'))
}
# Read-only prerequisite, NOT authorization to rename pending metadata or restore runtime.
function Test-ProGoInstallerPendingInstalledProof([string]$Directory, [string]$InstallDirectory) {
    try {
        if (-not (Test-ProGoInstallerInstalledOutcome $Directory $InstallDirectory 'outcome.pending.json')) { return $false }
        $saved = Read-ProGoInstallerJson (Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json')
        $completion = $saved.Completion
        if ($completion.Schema -ne 1 -or @($completion.Phases).Count -ne 2 -or
            $completion.Phases[0] -cne 'self-check' -or $completion.Phases[1] -cne 'shortcuts') { return $false }
        $inventory = $saved.Inventory | ConvertFrom-Json -ErrorAction Stop
        $entries = @($inventory.Files); $owners = @($completion.Ownership)
        if ($owners.Count -ne $entries.Count) { return $false }
        $seen = @{}
        foreach ($owner in $owners) {
            $entry = @($entries | Where-Object { $_.Relative -ceq $owner.Relative })
            if ($seen.ContainsKey($owner.Relative) -or $entry.Count -ne 1 -or $owner.OwnedHash -cne $entry[0].NewHash) { return $false }
            $seen[$owner.Relative] = $true
        }
        $snapshot = if ($null -eq $saved.Snapshot) { $null } else {
            Assert-ProGoInstallerStagePath (Assert-ProGoInstallerStagePath $InstallDirectory 'backups') $saved.Snapshot
        }
        $backup = [pscustomobject]@{ InstallDirectory = $InstallDirectory; Directory = $snapshot;
            ManifestHash = $inventory.SnapshotHash; Entries = $entries }
        Assert-ProGoInstallerSnapshotMaterial $backup
        Assert-ProGoInstallerRuntime $backup
        return $true
    } catch { return $false }
}
function Test-ProGoInstallerAuxiliaryJournalScope([string]$Directory, [string]$InstallDirectory) {
    Assert-ProGoInstallerNoAuxiliaryResult $InstallDirectory -AllowStartupAdmission
    if ([IO.Path]::GetFileName($Directory) -cnotmatch '^installer-journal-[a-f0-9]{32}$') { return $false }
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    if ((Assert-ProGoInstallerStagePath $parent ([IO.Path]::GetFileName($Directory))) -cne $Directory) { return $false }
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        if (-not $item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase)) { continue }
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if ($item.FullName -ceq $Directory) { continue }
        if (-not (Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -and
            -not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { return $false }
    }
    return $true
}
# Read-only auxiliary revalidation prerequisite. Does not publish or recover pending state.
# Expected locations come from the caller/platform, never from durable receipt data.
function Test-ProGoInstallerPendingAuxiliaryProof([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        if (-not (Test-ProGoInstallerAuxiliaryJournalScope $Directory $InstallDirectory)) { return $false }
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        # Pin this exact bounded receipt against writes/deletion through all validation.
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        if (-not (Test-ProGoInstallerAuxiliaryJournalScope $Directory $InstallDirectory) -or
            -not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { return $false }
        $receipt = $saved.Completion.Startup
        $parts = @($receipt, $receipt.Menu, $receipt.Legacy)
        $maps = @()
        for ($index = 0; $index -lt 3; $index++) {
            $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
            foreach ($property in $parts[$index].PSObject.Properties) {
                if ($index -eq 0 -and $property.Name -cin @('Menu', 'Legacy')) { continue }
                $map.Add($property.Name, $property.Value)
            }
            $maps += ,$map
        }
        # Use the shipped helper, not any executable or script identified by journal fields.
        Import-ProGoShortcutType
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        if (-not $manager.VerifyDurableInstallEvidence($maps[0], $maps[1], $maps[2])) { return $false }
        return ((Test-ProGoInstallerAuxiliaryJournalScope $Directory $InstallDirectory) -and
            (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory))
    } catch { return $false } finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $stream) { $stream.Dispose() }
    }
}
# Read-only Startup rollback prerequisite, not authority to mutate or publish rollback.
# Caller must supply trusted locations independently and revalidate again at any future write.
function Get-ProGoInstallerStartupRollbackAdmission([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        if (-not (Test-ProGoInstallerAuxiliaryJournalScope $Directory $InstallDirectory)) { return $null }
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $null }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        if (-not (Test-ProGoInstallerPendingAuxiliaryProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        $receipt = $saved.Completion.Startup
        # Existing verifier proves actual before/after presence and exact current bytes/target.
        # Preserved/custom state never becomes owned merely because it was observed.
        $kind = if ($receipt.Disposition -ceq 'created' -and -not $receipt.BeforeExists -and
            $null -eq $receipt.BeforeHash -and $receipt.Exists) { 'created-from-absence' }
            elseif ($receipt.Disposition -ceq 'preserved' -and $receipt.BeforeExists -eq $receipt.Exists -and
                $receipt.BeforeHash -ceq $receipt.Hash) { 'preserve-only' } else { return $null }
        $proof = [pscustomobject]@{ Schema = 1; Kind = $kind;
            InstallIdentity = (Get-ProGoInstallerIdentity $InstallDirectory);
            LocationIdentity = (Get-ProGoInstallerIdentity (Join-Path $StartupDirectory 'ProGo.lnk'));
            ReceiptHash = (Get-FileHash -LiteralPath $pending).Hash;
            BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash;
            Exists = $receipt.Exists; Hash = $receipt.Hash }
        if (-not (Test-ProGoInstallerPendingAuxiliaryProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        return $proof
    } catch { return $null } finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $stream) { $stream.Dispose() }
    }
}
# Observational Menu prerequisite only: never a deletion target, mutation lease or terminal outcome.
function Get-ProGoInstallerMenuRollbackAdmission([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        $context = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        # Permit either fully verified original auxiliary completion or already-proven Startup
        # settlement. Missing Startup is not enough: its exact admission AND result must verify.
        $startupSettled = (Get-ProGoInstallerStartupMutationState $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory) -ceq 'original-observed' -and
            (Test-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)
        if (-not $startupSettled -and -not (Test-ProGoInstallerPendingAuxiliaryProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $null }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $receipt = $saved.Completion.Startup.Menu
        $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
        foreach ($property in $receipt.PSObject.Properties) { $map.Add($property.Name, $property.Value) }
        Import-ProGoShortcutType
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        if (-not $manager.VerifyDurableMenuRollbackEvidence($map)) { return $null }
        $kind = if ($receipt.Disposition -ceq 'created' -and -not $receipt.BeforeExists -and $null -eq $receipt.BeforeHash -and $receipt.Exists) { 'created-from-absence' }
            elseif ($receipt.Disposition -ceq 'preserved' -and $receipt.BeforeExists -eq $receipt.Exists -and $receipt.BeforeHash -ceq $receipt.Hash) { 'preserve-only' } else { return $null }
        $proof = [pscustomobject]@{ Schema = 1; Kind = $kind; MutationAllowed = $false;
            InstallIdentity = (Get-ProGoInstallerIdentity $InstallDirectory);
            LocationIdentity = (Get-ProGoInstallerIdentity (Join-Path $ProgramsDirectory 'ProGo\ProGo.lnk'));
            ReceiptHash = (Get-FileHash -LiteralPath $pending).Hash; OriginIdentity = $receipt.OriginIdentity;
            BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash; Exists = $receipt.Exists; Hash = $receipt.Hash }
        $again = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne ($context.Record | ConvertTo-Json -Depth 5 -Compress) -or $again.State -cne $context.State -or
            -not $manager.VerifyDurableMenuRollbackEvidence($map) -or (Get-FileHash -LiteralPath $pending).Hash -cne $proof.ReceiptHash) { return $null }
        if ($startupSettled) {
            if ((Get-ProGoInstallerStartupMutationState $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory) -cne 'original-observed' -or
                -not (Test-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        } elseif (-not (Test-ProGoInstallerPendingAuxiliaryProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        return $proof
    } catch { return $null } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Explicit Menu-only operation. Caller owns maintenance; no automatic error-path binding yet.
# Admission is revalidated while the bounded receipt is pinned, then native ownership is acquired.
# This partial operation never publishes full rollback, success, Launch or retry authorization.
function Invoke-ProGoInstallerMenuRollback([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $stream = $null; $reader = $null; $mutation = $null
    try {
        $CancellationToken.ThrowIfCancellationRequested()
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $admission = Get-ProGoInstallerMenuRollbackAdmission $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $admission -or $admission.ReceiptHash -cne (Get-FileHash -LiteralPath $pending).Hash) { return $false }
        $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
        foreach ($property in $saved.Completion.Startup.Menu.PSObject.Properties) { $map.Add($property.Name, $property.Value) }
        $CancellationToken.ThrowIfCancellationRequested()
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        $mutation = Open-ProGoInstallerMenuMutationAdmission $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $CancellationToken.ThrowIfCancellationRequested()
        return $manager.TryRollbackDurableMenu($map, $CancellationToken)
    } finally {
        if ($null -ne $mutation) { $mutation.Dispose() }
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $stream) { $stream.Dispose() }
    }
}
# Startup-only owned rollback operation. Caller holds maintenance and preserves original install error.
# Does not publish a full auxiliary/runtime rollback outcome or authorize a retry.
function Invoke-ProGoInstallerStartupRollback([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None, $LegacyContext = $null) {
    if ($null -ne $LegacyContext) {
        Import-ProGoShortcutType
        if ($LegacyContext -isnot [ProGo.ApplicationShortcuts+LegacyStartupContext] -or
            -not $LegacyContext.MatchesInstallerLocations($Directory,$InstallDirectory,$ProgramsDirectory,$StartupDirectory)) { return $false }
        return $LegacyContext.RollbackStartupOnce($CancellationToken)
    }
    $stream = $null; $reader = $null; $mutation = $null
    try {
        $CancellationToken.ThrowIfCancellationRequested()
        if (-not (Test-ProGoInstallerAuxiliaryJournalScope $Directory $InstallDirectory)) { return $false }
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $admission = Get-ProGoInstallerStartupRollbackAdmission $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $admission -or $admission.ReceiptHash -cne (Get-FileHash -LiteralPath $pending).Hash) { return $false }
        $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
        foreach ($property in $saved.Completion.Startup.PSObject.Properties) {
            if ($property.Name -cin @('Menu', 'Legacy')) { continue }
            $map.Add($property.Name, $property.Value)
        }
        $CancellationToken.ThrowIfCancellationRequested()
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        $mutation = Open-ProGoInstallerStartupMutationAdmission $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $CancellationToken.ThrowIfCancellationRequested()
        return $manager.TryRollbackDurableStartup($map, $CancellationToken)
    } finally {
        if ($null -ne $mutation) { $mutation.Dispose() }
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $stream) { $stream.Dispose() }
    }
}
# Partial auxiliary evidence blocks automatic terminal reconciliation/new installation.
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
# Read-only proof of observed original Startup state, not full rollback or mutation authority.
function Get-ProGoInstallerStartupResultProof([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    if (-not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Installer Startup result requires verified runtime and snapshot.' }
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    $name = 'installer-startup-outcome-' + [IO.Path]::GetFileName($Directory).Substring('installer-journal-'.Length) + '.json'
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if ($item.Name.StartsWith('installer-startup-outcome', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $name) { throw 'Installer auxiliary results conflict.' }
        if ($item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase) -and $item.FullName -cne $Directory -and
            -not (Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -and
            -not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { throw 'Installer pending journals conflict.' }
    }
    $stream = $null; $reader = $null
    try {
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Installer Startup receipt size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $receipt = $saved.Completion.Startup; $maps = @(); $parts = @($receipt, $receipt.Menu, $receipt.Legacy)
        for ($index = 0; $index -lt 3; $index++) {
            $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
            foreach ($property in $parts[$index].PSObject.Properties) {
                if ($index -eq 0 -and $property.Name -cin @('Menu', 'Legacy')) { continue }
                $map.Add($property.Name, $property.Value)
            }
            $maps += ,$map
        }
        Import-ProGoShortcutType
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        if (-not $manager.VerifyDurableStartupOriginalResult($maps[0], $maps[1], $maps[2]) -or
            -not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Installer Startup original state is not verified.' }
        return [pscustomobject]@{ Schema = 1; Phase = 'startup-original-observed'; FullRollback = $false;
            InstallSucceeded = $false; MutationAllowed = $false; RuntimeState = 'new-verified';
            Identity = (Get-ProGoInstallerIdentity $InstallDirectory); Journal = [IO.Path]::GetFileName($Directory);
            ReceiptHash = (Get-FileHash -LiteralPath $pending).Hash; StartupLocation = (Get-ProGoInstallerIdentity (Join-Path $StartupDirectory 'ProGo.lnk'));
            OriginIdentity = $receipt.OriginIdentity; BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash }
    } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
function Write-ProGoInstallerStartupResultBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true) }
function Complete-ProGoInstallerStartupResult([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $proof = Get-ProGoInstallerStartupResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    $name = 'installer-startup-outcome-' + $proof.Journal.Substring('installer-journal-'.Length) + '.json'
    $path = Assert-ProGoInstallerStagePath $parent $name
    $text = $proof | ConvertTo-Json -Depth 5 -Compress
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($text)
    $stream = New-Object IO.FileStream -ArgumentList @($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None, 4096, [IO.FileOptions]::WriteThrough)
    try {
        Write-ProGoInstallerStartupResultBytes $stream $bytes
        $stream.Flush($true); $stream.Position = 0
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $false, 4096, $true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Installer Startup result readback failed.' } } finally { $reader.Dispose() }
        $again = Get-ProGoInstallerStartupResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (($again | ConvertTo-Json -Depth 5 -Compress) -cne $text) { throw 'Installer Startup result evidence changed.' }
    } finally { $stream.Dispose() }
}
function Test-ProGoInstallerStartupResult([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        $expected = Get-ProGoInstallerStartupResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $path = Assert-ProGoInstallerStagePath $parent ('installer-startup-outcome-' + $expected.Journal.Substring('installer-journal-'.Length) + '.json')
        $stream = New-Object IO.FileStream -ArgumentList @($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        return $reader.ReadToEnd() -ceq ($expected | ConvertTo-Json -Depth 5 -Compress)
    } catch { return $false } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Intent is descriptive durable evidence, never permission for automatic mutation.
function Get-ProGoInstallerStartupMutationContext([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    if (-not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Installer mutation requires verified snapshot and runtime.' }
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    $suffix = [IO.Path]::GetFileName($Directory).Substring('installer-journal-'.Length)
    $name = 'installer-startup-admission-' + $suffix + '.json'; $outcome = 'installer-startup-outcome-' + $suffix + '.json'
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if (($item.Name.StartsWith('installer-startup-admission', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $name) -or
            ($item.Name.StartsWith('installer-startup-outcome', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $outcome) -or
            ($item.Name.StartsWith('installer-menu-admission', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne ('installer-menu-admission-' + $suffix + '.json'))) { throw 'Installer auxiliary admissions conflict.' }
        if ($item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase) -and $item.FullName -cne $Directory -and
            -not (Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -and
            -not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { throw 'Installer pending journals conflict.' }
    }
    $stream = $null; $reader = $null
    try {
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Installer mutation receipt size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $receipt = $saved.Completion.Startup; $maps = @(); $parts = @($receipt, $receipt.Menu, $receipt.Legacy)
        for ($index = 0; $index -lt 3; $index++) {
            $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
            foreach ($property in $parts[$index].PSObject.Properties) {
                if ($index -eq 0 -and $property.Name -cin @('Menu', 'Legacy')) { continue }
                $map.Add($property.Name, $property.Value)
            }
            $maps += ,$map
        }
        Import-ProGoShortcutType
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        $before = $manager.VerifyDurableInstallEvidence($maps[0], $maps[1], $maps[2])
        $after = $manager.VerifyDurableStartupOriginalResult($maps[0], $maps[1], $maps[2])
        if ((-not $before -and -not $after) -or -not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Installer Startup mutation state conflicts.' }
        $state = if ($after) { 'original-observed' } else { 'not-applied' }
        $outcomePath = Assert-ProGoInstallerStagePath $parent $outcome
        if ([IO.File]::Exists($outcomePath) -and ($state -cne 'original-observed' -or
            -not (Test-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory))) { throw 'Installer Startup result conflicts.' }
        $record = [pscustomobject]@{ Schema = 1; Phase = 'startup-mutation-admitted'; AutomaticMutationAllowed = $false;
            Identity = (Get-ProGoInstallerIdentity $InstallDirectory); Journal = [IO.Path]::GetFileName($Directory);
            ReceiptHash = (Get-FileHash -LiteralPath $pending).Hash; LocationIdentity = (Get-ProGoInstallerIdentity (Join-Path $StartupDirectory 'ProGo.lnk'));
            Disposition = $receipt.Disposition; OriginIdentity = $receipt.OriginIdentity; Hash = $receipt.Hash;
            BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash }
        return [pscustomobject]@{ State = $state; Record = $record; Path = (Assert-ProGoInstallerStagePath $parent $name) }
    } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
function Write-ProGoInstallerStartupAdmissionBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true) }
# Returns a pinned stream owned by the caller until physical native settlement.
function Open-ProGoInstallerStartupMutationAdmission([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    $context = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    if ($context.State -cne 'not-applied' -and $context.Record.Disposition -cne 'preserved') { throw 'Installer Startup already changed; observe settlement without replay.' }
    $text = $context.Record | ConvertTo-Json -Depth 5 -Compress
    $stream = $null
    try {
        if ([IO.File]::Exists($context.Path)) {
            $stream = New-Object IO.FileStream -ArgumentList @($context.Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        } else {
            $stream = New-Object IO.FileStream -ArgumentList @($context.Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read, 4096, [IO.FileOptions]::WriteThrough)
            Write-ProGoInstallerStartupAdmissionBytes $stream ((New-Object Text.UTF8Encoding($false)).GetBytes($text))
            $stream.Flush($true); $stream.Position = 0
        }
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Installer Startup admission size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Installer Startup admission readback failed.' } } finally { $reader.Dispose() }
        $again = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne $text -or $again.State -cne $context.State) { throw 'Installer Startup admission changed.' }
        $result = $stream; $stream = $null; return $result
    } finally { if ($null -ne $stream) { $stream.Dispose() } }
}
# Read-only crash classification; never starts native rollback or restores bytes.
function Get-ProGoInstallerStartupMutationState([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        $context = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $stream = New-Object IO.FileStream -ArgumentList @($context.Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return 'blocked' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        if ($reader.ReadToEnd() -cne ($context.Record | ConvertTo-Json -Depth 5 -Compress)) { return 'blocked' }
        $again = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($again.State -cne $context.State -or ($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne ($context.Record | ConvertTo-Json -Depth 5 -Compress)) { return 'blocked' }
        return $context.State
    } catch { return 'blocked' } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Settlement may only persist a verified observation; intent grants no mutation authority.
function Complete-ProGoInstallerStartupMutationSettlement([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    if ((Get-ProGoInstallerStartupMutationState $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory) -cne 'original-observed') { return $false }
    if (-not (Test-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) {
        Complete-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    }
    return (Test-ProGoInstallerStartupResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)
}
# Exact fixed-path metadata reader. No paths or mutation authority come from admission data.
function Test-ProGoInstallerMenuRecord([string]$Path, $Expected) {
    $stream = $null; $reader = $null
    try {
        $stream = New-Object IO.FileStream -ArgumentList @($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        return $reader.ReadToEnd() -ceq ($Expected | ConvertTo-Json -Depth 5 -Compress)
    } catch { return $false } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Reconstruct all expected fields from independently checked receipt/runtime/locations, not intent.
function Get-ProGoInstallerMenuMutationContext([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    if (-not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Menu mutation requires verified runtime and snapshot.' }
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    $journal = [IO.Path]::GetFileName($Directory)
    if ($journal -cnotmatch '^installer-journal-[a-f0-9]{32}$' -or (Assert-ProGoInstallerStagePath $parent $journal) -cne $Directory) { throw 'Menu journal ownership is invalid.' }
    $suffix = $journal.Substring('installer-journal-'.Length)
    $name = 'installer-menu-admission-' + $suffix + '.json'
    $startupAdmission = 'installer-startup-admission-' + $suffix + '.json'
    $startupOutcome = 'installer-startup-outcome-' + $suffix + '.json'
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if (($item.Name.StartsWith('installer-menu-admission', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $name) -or
            ($item.Name.StartsWith('installer-startup-admission', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $startupAdmission) -or
            ($item.Name.StartsWith('installer-startup-outcome', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne $startupOutcome) -or
            ($item.Name.StartsWith('installer-menu-outcome', [StringComparison]::OrdinalIgnoreCase) -and $item.Name -cne ('installer-menu-outcome-' + $suffix + '.json'))) { throw 'Menu auxiliary metadata conflicts.' }
        if ($item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase) -and $item.FullName -cne $Directory -and
            -not (Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -and
            -not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { throw 'Menu pending journal conflicts.' }
    }
    $stream = $null; $reader = $null
    try {
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Menu receipt size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream, (New-Object Text.UTF8Encoding($false, $true)), $true, 4096, $true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        $receipt = $saved.Completion.Startup; $maps = @(); $parts = @($receipt, $receipt.Menu, $receipt.Legacy)
        for ($index = 0; $index -lt 3; $index++) {
            $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
            foreach ($property in $parts[$index].PSObject.Properties) {
                if ($index -eq 0 -and $property.Name -cin @('Menu','Legacy')) { continue }
                $map.Add($property.Name, $property.Value)
            }
            $maps += ,$map
        }
        Import-ProGoShortcutType
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        $before = $manager.VerifyDurableInstallEvidence($maps[0], $maps[1], $maps[2])
        $startupOriginal = $false
        $after = $manager.VerifyDurableMenuOriginalResult($maps[0], $maps[1], $maps[2], $false)
        if (-not $before -and -not $after) {
            $before = $manager.VerifyDurableStartupOriginalResult($maps[0], $maps[1], $maps[2])
            $after = $manager.VerifyDurableMenuOriginalResult($maps[0], $maps[1], $maps[2], $true)
            $startupOriginal = $true
        }
        if (-not $before -and -not $after) { throw 'Menu physical evidence conflicts.' }
        # After Menu removal the old Startup verifier correctly refuses a missing primary. Validate
        # its EXACT existing durable records against independently reconstructed fields instead.
        $identity = Get-ProGoInstallerIdentity $InstallDirectory
        $receiptHash = (Get-FileHash -LiteralPath $pending).Hash
        $startupLocation = Get-ProGoInstallerIdentity (Join-Path $StartupDirectory 'ProGo.lnk')
        $expectedAdmission = [pscustomobject]@{ Schema = 1; Phase = 'startup-mutation-admitted'; AutomaticMutationAllowed = $false;
            Identity = $identity; Journal = $journal; ReceiptHash = $receiptHash; LocationIdentity = $startupLocation;
            Disposition = $receipt.Disposition; OriginIdentity = $receipt.OriginIdentity; Hash = $receipt.Hash;
            BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash }
        $admissionPath = Assert-ProGoInstallerStagePath $parent $startupAdmission
        $outcomePath = Assert-ProGoInstallerStagePath $parent $startupOutcome
        if (($startupOriginal -or [IO.File]::Exists($admissionPath)) -and -not (Test-ProGoInstallerMenuRecord $admissionPath $expectedAdmission)) { throw 'Startup admission is not proven.' }
        if ($startupOriginal) {
            $expectedOutcome = [pscustomobject]@{ Schema = 1; Phase = 'startup-original-observed'; FullRollback = $false;
                InstallSucceeded = $false; MutationAllowed = $false; RuntimeState = 'new-verified'; Identity = $identity; Journal = $journal;
                ReceiptHash = $receiptHash; StartupLocation = $startupLocation; OriginIdentity = $receipt.OriginIdentity;
                BeforeExists = $receipt.BeforeExists; BeforeHash = $receipt.BeforeHash }
            if (-not (Test-ProGoInstallerMenuRecord $outcomePath $expectedOutcome)) { throw 'Startup settlement is not proven.' }
        } elseif ([IO.File]::Exists($outcomePath)) { throw 'Startup outcome conflicts.' }
        if ($before -and -not $manager.VerifyDurableMenuRollbackEvidence($maps[1])) { throw 'Menu origin is not proven.' }
        if (-not (Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory)) { throw 'Menu runtime evidence changed.' }
        $record = [pscustomobject]@{ Schema = 1; Phase = 'menu-mutation-admitted'; AutomaticMutationAllowed = $false;
            Identity = $identity; Journal = $journal; ReceiptHash = $receiptHash;
            LocationIdentity = (Get-ProGoInstallerIdentity (Join-Path $ProgramsDirectory 'ProGo\ProGo.lnk'));
            StartupLocation = $startupLocation; StartupOriginal = $startupOriginal;
            Disposition = $receipt.Menu.Disposition; OriginIdentity = $receipt.Menu.OriginIdentity; Hash = $receipt.Menu.Hash;
            BeforeExists = $receipt.Menu.BeforeExists; BeforeHash = $receipt.Menu.BeforeHash }
        return [pscustomobject]@{ State = $(if ($after) { 'original-observed' } else { 'not-applied' }); Record = $record; Path = (Assert-ProGoInstallerStagePath $parent $name) }
    } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
function Write-ProGoInstallerMenuAdmissionBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true) }
# Explicit invocation only. Keep exact durable admission read-pinned through native settlement.
function Open-ProGoInstallerMenuMutationAdmission([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    $context = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    if ($context.State -cne 'not-applied' -and $context.Record.Disposition -cne 'preserved') { throw 'Menu already changed; observe without replay.' }
    $text = $context.Record | ConvertTo-Json -Depth 5 -Compress; $stream = $null
    try {
        if ([IO.File]::Exists($context.Path)) {
            $stream = New-Object IO.FileStream -ArgumentList @($context.Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        } else {
            $stream = New-Object IO.FileStream -ArgumentList @($context.Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read,4096,[IO.FileOptions]::WriteThrough)
            Write-ProGoInstallerMenuAdmissionBytes $stream ((New-Object Text.UTF8Encoding($false)).GetBytes($text))
            $stream.Flush($true); $stream.Position = 0
        }
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Menu admission size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Menu admission readback failed.' } } finally { $reader.Dispose() }
        $again = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($again.State -cne $context.State -or ($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne $text) { throw 'Menu admission evidence changed.' }
        $result = $stream; $stream = $null; return $result
    } finally { if ($null -ne $stream) { $stream.Dispose() } }
}
# Read-only crash settlement: observes actual results, never replays native mutation or writes outcomes.
function Get-ProGoInstallerMenuMutationState([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        $context = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $stream = New-Object IO.FileStream -ArgumentList @($context.Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return 'blocked' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        if ($reader.ReadToEnd() -cne ($context.Record | ConvertTo-Json -Depth 5 -Compress)) { return 'blocked' }
        $again = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($again.State -cne $context.State -or ($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne ($context.Record | ConvertTo-Json -Depth 5 -Compress)) { return 'blocked' }
        return $context.State
    } catch { return 'blocked' } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Menu-only observed result. This is not a terminal install/rollback or mutation authority.
function Get-ProGoInstallerMenuResultProof([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory) {
    $context = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    if ($context.State -cne 'original-observed') { throw 'Menu original result is not observed.' }
    $stream = $null; $reader = $null
    try {
        $stream = New-Object IO.FileStream -ArgumentList @($context.Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Menu result admission size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        $text = $context.Record | ConvertTo-Json -Depth 5 -Compress
        if ($reader.ReadToEnd() -cne $text) { throw 'Menu result admission is not verified.' }
        $again = Get-ProGoInstallerMenuMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($again.State -cne 'original-observed' -or ($again.Record | ConvertTo-Json -Depth 5 -Compress) -cne $text) { throw 'Menu result evidence changed.' }
        return [pscustomobject]@{ Schema = 1; Phase = 'menu-original-observed'; FullRollback = $false;
            InstallSucceeded = $false; MutationAllowed = $false; RuntimeState = 'new-verified';
            Identity = $context.Record.Identity; Journal = $context.Record.Journal;
            ReceiptHash = $context.Record.ReceiptHash; AdmissionHash = (Get-FileHash -LiteralPath $context.Path).Hash;
            LocationIdentity = $context.Record.LocationIdentity; OriginIdentity = $context.Record.OriginIdentity;
            BeforeExists = $context.Record.BeforeExists; BeforeHash = $context.Record.BeforeHash;
            StartupLocation = $context.Record.StartupLocation; StartupOriginal = $context.Record.StartupOriginal }
    } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
function Write-ProGoInstallerMenuResultBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true) }
function Complete-ProGoInstallerMenuResult([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $proof = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    $path = Assert-ProGoInstallerStagePath $parent ('installer-menu-outcome-' + $proof.Journal.Substring('installer-journal-'.Length) + '.json')
    $text = $proof | ConvertTo-Json -Depth 5 -Compress
    $stream = New-Object IO.FileStream -ArgumentList @($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None,4096,[IO.FileOptions]::WriteThrough)
    try {
        Write-ProGoInstallerMenuResultBytes $stream ((New-Object Text.UTF8Encoding($false)).GetBytes($text))
        $stream.Flush($true); $stream.Position = 0
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$false,4096,$true)
        try { if ($reader.ReadToEnd() -cne $text) { throw 'Menu result readback failed.' } } finally { $reader.Dispose() }
        $again = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (($again | ConvertTo-Json -Depth 5 -Compress) -cne $text) { throw 'Menu result evidence changed during publication.' }
    } finally { $stream.Dispose() }
}
# Independent bounded, pinned read plus physical revalidation; saved flags alone never suffice.
function Test-ProGoInstallerMenuResult([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null
    try {
        $proof = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $path = Assert-ProGoInstallerStagePath $parent ('installer-menu-outcome-' + $proof.Journal.Substring('installer-journal-'.Length) + '.json')
        $stream = New-Object IO.FileStream -ArgumentList @($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $false }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        $text = $proof | ConvertTo-Json -Depth 5 -Compress
        if ($reader.ReadToEnd() -cne $text) { return $false }
        $again = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        return ($again | ConvertTo-Json -Depth 5 -Compress) -ceq $text
    } catch { return $false } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# Failure-path adapter. Only the running maintenance owner may invoke mutation.
# A restart intent alone never enters this path. No full auxiliary/runtime outcome is claimed.
function Invoke-ProGoInstallerFailureStartup([bool]$AuxiliaryStarted, $Backup,
    [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'), $LegacyContext = $null,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    if (-not $AuxiliaryStarted) { return 'not-started' }
    if ($null -ne $LegacyContext) {
        try {
            Import-ProGoShortcutType
            if ($LegacyContext -isnot [ProGo.ApplicationShortcuts+LegacyStartupContext] -or $null -eq $Backup -or $null -eq $Backup.Journal -or
                -not $LegacyContext.MatchesInstallerLocations($Backup.Journal.Directory,$InstallDirectory,$ProgramsDirectory,$StartupDirectory)) { return 'blocked' }
            if (-not $LegacyContext.BeginFailureHandling()) {
                if ($LegacyContext.VerifyStartupSettlement()) { return 'legacy-startup-observed-menu-blocked' }
                return 'blocked'
            }
            [void]$LegacyContext.RestoreLegacy($CancellationToken)
            if (-not $LegacyContext.VerifyHandoff()) { return 'blocked' }
            if ((Invoke-ProGoInstallerStartupRollback $Backup.Journal.Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken $LegacyContext) -and
                $LegacyContext.VerifyStartupSettlement()) { return 'legacy-startup-observed-menu-blocked' }
        } catch { return 'blocked' }
        return 'blocked'
    }
    try {
        if ($null -eq $Backup -or $null -eq $Backup.Journal) { return 'blocked' }
        $directory = $Backup.Journal.Directory
        # No replay: an existing admission permits read-only settlement only, even if not-applied.
        $context = Get-ProGoInstallerStartupMutationContext $directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ([IO.File]::Exists($context.Path)) {
            if (Complete-ProGoInstallerStartupMutationSettlement $directory $InstallDirectory $ProgramsDirectory $StartupDirectory) { return 'startup-observed' }
            return 'blocked'
        }
        # Fresh in-process failure only: full snapshot/runtime/receipt and exact origin are
        # independently revalidated by the existing owned operation immediately before deletion.
        if (-not (Invoke-ProGoInstallerStartupRollback $directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return 'blocked' }
        if (Complete-ProGoInstallerStartupMutationSettlement $directory $InstallDirectory $ProgramsDirectory $StartupDirectory) { return 'startup-observed' }
    } catch { return 'blocked' }
    return 'blocked'
}
# Read-only Legacy prerequisite. A verified removed receipt is NOT a source of original bytes.
# No path/authority is accepted from metadata, and no Legacy write/delete/restore is performed.
function Get-ProGoInstallerLegacyRollbackAdmission([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $stream = $null; $reader = $null; $resultStream = $null; $resultReader = $null
    try {
        $menuProof = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (-not (Test-ProGoInstallerMenuResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        $pending = Assert-ProGoInstallerStagePath $Directory 'outcome.pending.json'
        $stream = New-Object IO.FileStream -ArgumentList @($pending,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { return $null }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        $saved = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $pending).Hash -cne $menuProof.ReceiptHash) { return $null }
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $resultPath = Assert-ProGoInstallerStagePath $parent ('installer-menu-outcome-' + $menuProof.Journal.Substring('installer-journal-'.Length) + '.json')
        $resultStream = New-Object IO.FileStream -ArgumentList @($resultPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($resultStream.Length -lt 1 -or $resultStream.Length -gt 1048576) { return $null }
        $resultReader = New-Object IO.StreamReader -ArgumentList @($resultStream,(New-Object Text.UTF8Encoding($false,$true)),$true,4096,$true)
        $text = $menuProof | ConvertTo-Json -Depth 5 -Compress
        if ($resultReader.ReadToEnd() -cne $text) { return $null }
        # Existing Menu context independently performs fixed-schema/native Legacy identity,
        # origin/bytes/absence and replacement-linkage verification at trusted locations.
        $again = Get-ProGoInstallerMenuResultProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (($again | ConvertTo-Json -Depth 5 -Compress) -cne $text -or
            -not (Test-ProGoInstallerMenuResult $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return $null }
        $legacy = $saved.Completion.Startup.Legacy
        $kind = if ($legacy.Disposition -ceq 'preserved') { 'preserve-only' }
            elseif ($legacy.Disposition -ceq 'removed') { 'removed-needs-source' } else { return $null }
        return [pscustomobject]@{ Schema = 1; Kind = $kind; MutationAllowed = $false; RestorationAllowed = $false;
            FullRollback = $false; InstallSucceeded = $false; VerifiedRestoreSourceAvailable = $false;
            RestorationRequired = ($kind -ceq 'removed-needs-source');
            Identity = $menuProof.Identity; Journal = $menuProof.Journal; ReceiptHash = $menuProof.ReceiptHash;
            MenuAdmissionHash = $menuProof.AdmissionHash; MenuResultHash = (Get-FileHash -LiteralPath $resultPath).Hash;
            LocationIdentity = (Get-ProGoInstallerIdentity (Join-Path $ProgramsDirectory 'ProGo\ProGo Status.lnk'));
            OriginIdentity = $legacy.OriginIdentity; BeforeExists = $legacy.BeforeExists; BeforeHash = $legacy.BeforeHash;
            Exists = $legacy.Exists; Hash = $legacy.Hash; OwnedHash = $legacy.OwnedHash; ReplacementHash = $legacy.ReplacementHash }
    } catch { return $null } finally {
        if ($null -ne $resultReader) { $resultReader.Dispose() }; if ($null -ne $resultStream) { $resultStream.Dispose() }
        if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() }
    }
}
# Menu failure integration: maintenance owner only; an existing intent NEVER replays deletion.
# Startup must settle first for fresh Menu mutation. Already-applied Menu uses read-only context,
# which independently revalidates the canonical Startup result even without the Menu primary.
function Invoke-ProGoInstallerFailureMenu([bool]$AuxiliaryStarted, $Backup, [string]$InstallDirectory,
    [string]$StartupFailureState,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'), $LegacyContext = $null) {
    if (-not $AuxiliaryStarted) { return 'not-started' }
    if ($null -ne $LegacyContext) { return 'blocked' } # This bounded live gate never permits Menu mutation.
    if ($StartupFailureState -cnotin @('startup-observed','blocked')) { return 'blocked' }
    try {
        if ($null -eq $Backup -or $null -eq $Backup.Journal) { return 'blocked' }
        $directory = $Backup.Journal.Directory
        $context = Get-ProGoInstallerMenuMutationContext $directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $resultPath = Assert-ProGoInstallerStagePath $parent ('installer-menu-outcome-' + $context.Record.Journal.Substring('installer-journal-'.Length) + '.json')
        if ([IO.File]::Exists($context.Path)) {
            if ((Get-ProGoInstallerMenuMutationState $directory $InstallDirectory $ProgramsDirectory $StartupDirectory) -cne 'original-observed') { return 'blocked' }
        } else {
            # Foreign/partial metadata cannot cause a destructive attempt or blind receipt repair.
            if ([IO.File]::Exists($resultPath) -or $StartupFailureState -cne 'startup-observed' -or
                -not (Test-ProGoInstallerStartupResult $directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return 'blocked' }
            if (-not (Invoke-ProGoInstallerMenuRollback $directory $InstallDirectory $ProgramsDirectory $StartupDirectory)) { return 'blocked' }
        }
        if ([IO.File]::Exists($resultPath)) {
            if (Test-ProGoInstallerMenuResult $directory $InstallDirectory $ProgramsDirectory $StartupDirectory) { return 'menu-observed' }
            return 'blocked'
        }
        Complete-ProGoInstallerMenuResult $directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if (Test-ProGoInstallerMenuResult $directory $InstallDirectory $ProgramsDirectory $StartupDirectory) { return 'menu-observed' }
    } catch { return 'blocked' }
    return 'blocked'
}
function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
    $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
}
# An intent is not a terminal outcome or post-crash ownership proof.
# Until durable terminal outcomes exist, every previous installer journal is pending.
# Only finish metadata publication of an already-proven rollback. Never restore bytes.
# Called under the installer maintenance lease, before Stop or any new runtime write.
# Only publish a fully completed prior outcome; never restore runtime from an intent.
function Resolve-ProGoInstallerPendingInstalled([string]$InstallDirectory,
    [string]$ShortcutScript,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    Assert-ProGoInstallerNoAuxiliaryResult $InstallDirectory
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    if (-not (Test-Path -LiteralPath $parent)) { return }
    if (-not [IO.Directory]::Exists($parent)) { throw 'Installer journal parent is invalid.' }
    $candidate = $null
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        if (-not $item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase)) { continue }
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if ((Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -or
            (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { continue }
        # Existing verified rollback reconciliation keeps its separate contract below.
        if (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory 'rollback.pending.json') { continue }
        if ($null -ne $candidate) { throw 'Installer multiple pending outcomes require verified recovery.' }
        $candidate = $item.FullName
    }
    if ($null -eq $candidate) { return }
    # Trusted helper is supplied by the installer itself, never selected by a journal.
    if (-not [string]::IsNullOrEmpty($ShortcutScript)) { . $ShortcutScript }
    if (-not (Test-ProGoInstallerPendingAuxiliaryProof $candidate $InstallDirectory $ProgramsDirectory $StartupDirectory)) {
        throw 'Installer pending journal requires verified recovery before a new installation.'
    }
    $pending = Assert-ProGoInstallerStagePath $candidate 'outcome.pending.json'
    $digest = (Get-FileHash -LiteralPath $pending).Hash
    if (-not (Test-ProGoInstallerPendingAuxiliaryProof $candidate $InstallDirectory $ProgramsDirectory $StartupDirectory) -or
        (Get-FileHash -LiteralPath $pending).Hash -cne $digest) { throw 'Installer pending completion evidence changed.' }
    # Atomic no-overwrite rename only. Sharing/destination failures propagate; no Launch.
    [IO.File]::Move($pending, (Assert-ProGoInstallerStagePath $candidate 'installed.json'))
}
function Resolve-ProGoInstallerPendingRollback([string]$InstallDirectory) {
    Assert-ProGoInstallerNoAuxiliaryResult $InstallDirectory
    $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
    if (-not (Test-Path -LiteralPath $parent)) { return }
    if (-not [IO.Directory]::Exists($parent)) { throw 'Installer journal parent is invalid.' }
    $ready = New-Object 'System.Collections.Generic.List[object]'
    foreach ($item in Get-ChildItem -LiteralPath $parent -Force -ErrorAction Stop) {
        if (-not $item.Name.StartsWith('installer-journal', [StringComparison]::OrdinalIgnoreCase)) { continue }
        [void](Assert-ProGoInstallerStagePath $parent $item.Name)
        if ((Test-ProGoInstallerInstalledOutcome $item.FullName $InstallDirectory) -or
            (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory)) { continue }
        if (-not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory 'rollback.pending.json')) {
            throw 'Installer pending journal requires verified recovery before a new installation.'
        }
        $pending = Assert-ProGoInstallerStagePath $item.FullName 'rollback.pending.json'
        $saved = Read-ProGoInstallerJson $pending
        $inventory = $saved.Inventory | ConvertFrom-Json -ErrorAction Stop
        $snapshot = if ($null -eq $saved.Snapshot) { $null } else { Assert-ProGoInstallerStagePath $parent $saved.Snapshot }
        $backup = [pscustomobject]@{ InstallDirectory = $InstallDirectory; Directory = $snapshot;
            ManifestHash = $inventory.SnapshotHash; Entries = @($inventory.Files) }
        # Recheck saved material and current OLD bytes/absence: intent cannot authorize recovery.
        Assert-ProGoInstallerSnapshotMaterial $backup
        Assert-ProGoInstallerRuntime $backup -Original
        if (-not (Test-ProGoInstallerRollbackOutcome $item.FullName $InstallDirectory 'rollback.pending.json')) {
            throw 'Installer pending rollback evidence changed.'
        }
        $ready.Add([pscustomobject]@{ Directory = $item.FullName; Pending = $pending; Backup = $backup })
    }
    if ($ready.Count -gt 1) { throw 'Installer multiple pending rollbacks require verified recovery.' }
    foreach ($candidate in $ready) {
        Assert-ProGoInstallerSnapshotMaterial $candidate.Backup
        Assert-ProGoInstallerRuntime $candidate.Backup -Original
        if (-not (Test-ProGoInstallerRollbackOutcome $candidate.Directory $InstallDirectory 'rollback.pending.json')) { throw 'Installer pending rollback evidence changed.' }
        [IO.File]::Move($candidate.Pending, (Assert-ProGoInstallerStagePath $candidate.Directory 'rolled-back.json'))
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
function New-ProGoInstallerJournal($Backup) {
    Assert-ProGoInstallerNoPendingJournal $Backup.InstallDirectory
    Assert-ProGoInstallerBackup $Backup
    $parent = Assert-ProGoInstallerStagePath $Backup.InstallDirectory 'backups'
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    $directory = Join-Path $parent ('installer-journal-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $directory -ErrorAction Stop | Out-Null
    $inventory = [pscustomobject]@{ Schema = 1; SnapshotHash = $Backup.ManifestHash; Files = $Backup.Entries }
    return [pscustomobject]@{ Directory = $directory; InstallDirectory = $Backup.InstallDirectory;
        Inventory = ($inventory | ConvertTo-Json -Depth 6 -Compress);
        Records = (New-Object 'System.Collections.Generic.List[object]') }
}
function Assert-ProGoInstallerJournal($Journal, $Backup) {
    $parent = Assert-ProGoInstallerStagePath $Backup.InstallDirectory 'backups'
    if ($Journal.InstallDirectory -cne $Backup.InstallDirectory -or
        [IO.Path]::GetDirectoryName($Journal.Directory) -cne $parent -or
        [IO.Path]::GetFileName($Journal.Directory) -notmatch '^installer-journal-[a-f0-9]{32}$') {
        throw 'Installer journal scope is invalid.'
    }
    [void](Assert-ProGoInstallerStagePath $parent ([IO.Path]::GetFileName($Journal.Directory)))
    if (-not [IO.Directory]::Exists($Journal.Directory)) { throw 'Installer journal is missing.' }
    $inventory = [pscustomobject]@{ Schema = 1; SnapshotHash = $Backup.ManifestHash; Files = $Backup.Entries }
    if ($Journal.Inventory -cne ($inventory | ConvertTo-Json -Depth 6 -Compress)) { throw 'Installer journal inventory is invalid.' }
    $files = @(Get-ChildItem -LiteralPath $Journal.Directory -Force)
    if ($files.Count -ne $Journal.Records.Count) { throw 'Installer journal contains unknown records.' }
    foreach ($saved in $Journal.Records) {
        $path = Assert-ProGoInstallerStagePath $Journal.Directory $saved.Name
        if (-not [IO.File]::Exists($path) -or [IO.File]::ReadAllText($path) -cne $saved.Text) {
            throw 'Installer journal record is not verified.'
        }
    }
}
# Trusted live-session binding only. No restart-import, deletion or automatic restoration.
function Get-ProGoInstallerLegacySourceContext($Backup) {
    Assert-ProGoInstallerSnapshotMaterial $Backup
    Assert-ProGoInstallerRuntime $Backup
    Assert-ProGoInstallerJournal $Backup.Journal $Backup
    if ($null -eq $Backup.Directory -or $Backup.ManifestHash -cnotmatch '^[A-F0-9]{64}$') { throw 'Legacy source requires an actual verified snapshot.' }
    $allowed = @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md','README.ru.md','LICENSE.md') })
    if ($Backup.Entries.Count -ne $allowed.Count -or $Backup.Journal.Records.Count -ne $allowed.Count) { throw 'Legacy journal coverage is incomplete.' }
    $seen = @{}; $records = @()
    for ($index = 0; $index -lt $Backup.Entries.Count; $index++) {
        $entry = $Backup.Entries[$index]; $record = $Backup.Journal.Records[$index]
        if ($entry.Relative -cnotin $allowed -or $seen.ContainsKey($entry.Relative) -or $entry.Exists -isnot [bool] -or
            $entry.NewHash -cnotmatch '^[A-F0-9]{64}$' -or ($entry.Exists -and $entry.Hash -cnotmatch '^[A-F0-9]{64}$') -or
            (-not $entry.Exists -and $null -ne $entry.Hash)) { throw 'Legacy journal inventory is invalid.' }
        $seen[$entry.Relative] = $true
        $name = 'admit-{0:D6}.json' -f $index
        if ($record.Name -cne $name) { throw 'Legacy journal sequence is invalid.' }
        $path = Assert-ProGoInstallerStagePath $Backup.Journal.Directory $name
        if ((Get-Item -LiteralPath $path).Length -gt 1048576) { throw 'Legacy journal admission is oversized.' }
        $expected = ([pscustomobject]@{ Schema = 1; Phase = 'admitted'; Sequence = $index; Inventory = $Backup.Journal.Inventory; Entry = $entry } | ConvertTo-Json -Depth 8 -Compress)
        if ($record.Text -cne $expected -or [IO.File]::ReadAllText($path) -cne $expected) { throw 'Legacy journal admission is not canonical.' }
        $records += [pscustomobject]@{ Name = $name; Hash = (Get-FileHash -LiteralPath $path).Hash }
    }
    $journal = [IO.Path]::GetFileName($Backup.Journal.Directory)
    $operation = [Guid]::ParseExact($journal.Substring('installer-journal-'.Length),'N')
    return [pscustomobject]@{ Schema = 1; Phase = 'legacy-source-reference'; MutationAllowed = $false;
        FullRollback = $false; InstallSucceeded = $false; Operation = $operation.ToString('N');
        Identity = (Get-ProGoInstallerIdentity $Backup.InstallDirectory); Journal = $journal;
        Snapshot = [IO.Path]::GetFileName($Backup.Directory); SnapshotHash = $Backup.ManifestHash;
        Inventory = $Backup.Journal.Inventory; Records = $records }
}
function Get-ProGoInstallerLegacyReferenceDocument($Context, $Proof) {
    if ($null -eq $Proof -or $Proof -isnot [ProGo.ApplicationShortcuts+LegacyDurableSourceEvidence] -or
        $Proof.Operation.ToString('N') -cne $Context.Operation -or $Proof.SnapshotHash -cne $Context.SnapshotHash -or
        $Proof.InstallIdentity -cne $Context.Identity) { throw 'Legacy source proof does not match verified journal.' }
    return ([pscustomobject]@{ Context = $Context; Source = [pscustomobject]@{
        LocationIdentity = $Proof.LocationIdentity; OriginIdentity = $Proof.OriginIdentity; Hash = $Proof.Hash;
        StorageOrigin = $Proof.StorageOrigin; DocumentHash = $Proof.DocumentHash } } | ConvertTo-Json -Depth 10 -Compress)
}
function New-ProGoInstallerLegacySourceReference($Backup,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $source = $null
    try {
        $CancellationToken.ThrowIfCancellationRequested()
        $context = Get-ProGoInstallerLegacySourceContext $Backup
        $canonical = $context | ConvertTo-Json -Depth 8 -Compress
        [guid]$operation = [Guid]::ParseExact($context.Operation,'N')
        $manager = New-Object ProGo.ApplicationShortcuts($Backup.InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $source = $manager.CaptureLegacyRollbackSource($CancellationToken)
        if ($null -eq $source) { throw 'Legacy source is absent or not owned.' }
        $proof = $manager.PersistLegacyRollbackSource($source,$operation,$context.SnapshotHash,$CancellationToken)
        $document = Get-ProGoInstallerLegacyReferenceDocument $context $proof
        if (((Get-ProGoInstallerLegacySourceContext $Backup) | ConvertTo-Json -Depth 8 -Compress) -cne $canonical) { throw 'Legacy journal changed before reference.' }
        $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($document)
        $reference = $manager.PersistLegacyJournalReference($source,$proof,$operation,$context.SnapshotHash,$bytes,$CancellationToken)
        if (((Get-ProGoInstallerLegacySourceContext $Backup) | ConvertTo-Json -Depth 8 -Compress) -cne $canonical -or
            -not $manager.VerifyLegacyJournalReference($source,$reference,$operation,$context.SnapshotHash,$bytes)) { throw 'Legacy reference is not verified.' }
        return $reference
    } finally { if ($null -ne $source) { $source.Dispose() } }
}
function Test-ProGoInstallerLegacySourceReference($Backup, $Reference,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    $source = $null
    try {
        if ($Reference -isnot [ProGo.ApplicationShortcuts+LegacyJournalReferenceEvidence]) { return $false }
        $context = Get-ProGoInstallerLegacySourceContext $Backup
        $canonical = $context | ConvertTo-Json -Depth 8 -Compress
        $document = Get-ProGoInstallerLegacyReferenceDocument $context $Reference.Source
        $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($document)
        $manager = New-Object ProGo.ApplicationShortcuts($Backup.InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $source = $manager.CaptureLegacyRollbackSource([Threading.CancellationToken]::None)
        [guid]$operation = [Guid]::ParseExact($context.Operation,'N')
        if (-not $manager.VerifyLegacyJournalReference($source,$Reference,$operation,$context.SnapshotHash,$bytes)) { return $false }
        return (((Get-ProGoInstallerLegacySourceContext $Backup) | ConvertTo-Json -Depth 8 -Compress) -ceq $canonical)
    } catch { return $false } finally { if ($null -ne $source) { $source.Dispose() } }
}
# Seal is a separate prerequisite, not an installer-flow mutation or permission to restore.
function Complete-ProGoInstallerLegacyRestartSeal($Backup, $Reference,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $source = $null
    try {
        $CancellationToken.ThrowIfCancellationRequested()
        if (-not (Test-ProGoInstallerLegacySourceReference $Backup $Reference $ProgramsDirectory $StartupDirectory)) { throw 'Legacy restart reference is not verified.' }
        $context = Get-ProGoInstallerLegacySourceContext $Backup
        $canonical = $context | ConvertTo-Json -Depth 8 -Compress
        $document = Get-ProGoInstallerLegacyReferenceDocument $context $Reference.Source
        $manager = New-Object ProGo.ApplicationShortcuts($Backup.InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $source = $manager.CaptureLegacyRollbackSource($CancellationToken)
        [guid]$operation = [Guid]::ParseExact($context.Operation,'N')
        if (-not $manager.PersistLegacyRestartSeal($source,$Reference,$operation,$context.SnapshotHash,
            (New-Object Text.UTF8Encoding($false)).GetBytes($document),$CancellationToken)) { throw 'Legacy restart seal is not durable.' }
        if (((Get-ProGoInstallerLegacySourceContext $Backup) | ConvertTo-Json -Depth 8 -Compress) -cne $canonical) { throw 'Legacy restart context changed.' }
    } finally { if ($null -ne $source) { $source.Dispose() } }
}
function Read-ProGoInstallerLegacyRestartText([string]$Path) {
    $stream = $null; $reader = $null
    try {
        $stream = New-Object IO.FileStream -ArgumentList @($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        if ($stream.Length -lt 1 -or $stream.Length -gt 1048576) { throw 'Legacy restart document size is invalid.' }
        $reader = New-Object IO.StreamReader -ArgumentList @($stream,(New-Object Text.UTF8Encoding($false,$true)),$false,4096,$true)
        return $reader.ReadToEnd()
    } finally { if ($null -ne $reader) { $reader.Dispose() }; if ($null -ne $stream) { $stream.Dispose() } }
}
# No Backup, proof, expected hash or location from a previous process is accepted.
function Restore-ProGoInstallerLegacyReferenceEvidence([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    try {
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $journal = [IO.Path]::GetFileName($Directory)
        if ($journal -cnotmatch '^installer-journal-[a-f0-9]{32}$' -or
            (Assert-ProGoInstallerStagePath $parent $journal) -cne $Directory) { return $null }
        [guid]$operation = [Guid]::ParseExact($journal.Substring('installer-journal-'.Length),'N')
        $referencePath = Assert-ProGoInstallerStagePath $parent ('installer-legacy-reference-' + $operation.ToString('N') + '.json')
        $savedText = Read-ProGoInstallerLegacyRestartText $referencePath
        $saved = $savedText | ConvertFrom-Json -ErrorAction Stop
        # These are untrusted candidates, independently checked against actual snapshot/admissions/runtime below.
        if ($saved.Context.Snapshot -cnotmatch '^installer-[a-f0-9]{32}$') { return $null }
        $snapshot = Assert-ProGoInstallerStagePath $parent $saved.Context.Snapshot
        $manifest = Assert-ProGoInstallerStagePath $snapshot 'installer-snapshot.json'
        $snapshotHash = (Get-FileHash -LiteralPath $manifest).Hash
        $first = Read-ProGoInstallerLegacyRestartText (Assert-ProGoInstallerStagePath $Directory 'admit-000000.json') | ConvertFrom-Json -ErrorAction Stop
        $inventory = $first.Inventory | ConvertFrom-Json -ErrorAction Stop
        if ($inventory.Schema -ne 1 -or $inventory.SnapshotHash -cne $snapshotHash) { return $null }
        $entries = @($inventory.Files)
        $allowed = @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md','README.ru.md','LICENSE.md') })
        if ($entries.Count -ne $allowed.Count -or @(Get-ChildItem -LiteralPath $Directory -Force).Count -ne $allowed.Count) { return $null }
        # Validate untrusted leaf names BEFORE snapshot/runtime helpers can open any candidate file.
        $seen = @{}
        foreach ($entry in $entries) {
            if ($entry.Relative -cnotin $allowed -or $seen.ContainsKey($entry.Relative) -or $entry.Exists -isnot [bool] -or
                $entry.NewHash -cnotmatch '^[A-F0-9]{64}$' -or ($entry.Exists -and $entry.Hash -cnotmatch '^[A-F0-9]{64}$') -or
                (-not $entry.Exists -and $null -ne $entry.Hash)) { return $null }
            $seen[$entry.Relative] = $true
        }
        $records = New-Object 'System.Collections.Generic.List[object]'
        for ($index = 0; $index -lt $allowed.Count; $index++) {
            $name = 'admit-{0:D6}.json' -f $index
            $text = Read-ProGoInstallerLegacyRestartText (Assert-ProGoInstallerStagePath $Directory $name)
            $records.Add([pscustomobject]@{ Name = $name; Text = $text })
        }
        $backup = [pscustomobject]@{ InstallDirectory = $InstallDirectory; Directory = $snapshot; ManifestHash = $snapshotHash; Entries = $entries;
            Journal = [pscustomobject]@{ Directory = $Directory; InstallDirectory = $InstallDirectory; Inventory = $first.Inventory; Records = $records } }
        $context = Get-ProGoInstallerLegacySourceContext $backup
        $canonical = $context | ConvertTo-Json -Depth 8 -Compress
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $proof = $manager.RehydrateLegacySource($operation,$snapshotHash)
        if ($null -eq $proof) { return $null }
        $document = Get-ProGoInstallerLegacyReferenceDocument $context $proof
        if ($document -cne $savedText) { return $null }
        $reference = $manager.RehydrateLegacyJournalReference($proof,$operation,$snapshotHash,(New-Object Text.UTF8Encoding($false)).GetBytes($document))
        if ($null -eq $reference -or ((Get-ProGoInstallerLegacySourceContext $backup) | ConvertTo-Json -Depth 8 -Compress) -cne $canonical) { return $null }
        return $reference
    } catch { return $null }
}
# Pre-delete admission is intent, never evidence that deletion happened or permission to replay it.
function New-ProGoInstallerLegacyPreDeleteAdmission($Backup,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested()
    $context = Get-ProGoInstallerLegacySourceContext $Backup
    $canonical = $context | ConvertTo-Json -Depth 8 -Compress
    $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Backup.Journal.Directory $Backup.InstallDirectory $ProgramsDirectory $StartupDirectory
    if ($null -eq $reference) { throw 'Legacy admission requires independently rehydrated sealed evidence.' }
    $document = Get-ProGoInstallerLegacyReferenceDocument $context $reference.Source
    $manager = New-Object ProGo.ApplicationShortcuts($Backup.InstallDirectory,$ProgramsDirectory,$StartupDirectory)
    [guid]$operation = [Guid]::ParseExact($context.Operation,'N')
    if (-not $manager.PersistLegacyPreDeleteAdmission($reference,$operation,$context.SnapshotHash,
        (New-Object Text.UTF8Encoding($false)).GetBytes($document),$CancellationToken)) { throw 'Legacy admission was not persisted.' }
    if (((Get-ProGoInstallerLegacySourceContext $Backup) | ConvertTo-Json -Depth 8 -Compress) -cne $canonical -or
        (Get-ProGoInstallerLegacyPreDeleteState $Backup.Journal.Directory $Backup.InstallDirectory $ProgramsDirectory $StartupDirectory) -cne 'not-applied') {
        throw 'Legacy admission context changed.'
    }
    return [pscustomobject]@{ Schema = 1; State = 'not-applied'; MutationAllowed = $false; FullRollback = $false; InstallSucceeded = $false }
}
function Get-ProGoInstallerLegacyPreDeleteState([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
    [string]$StartupDirectory = [Environment]::GetFolderPath('Startup')) {
    try {
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $reference) { return 'blocked' }
        # Reference JSON is used only after independent rehydration, and is checked AGAIN natively below.
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $operation = $reference.Source.Operation
        $path = Assert-ProGoInstallerStagePath $parent ('installer-legacy-reference-' + $operation.ToString('N') + '.json')
        $text = Read-ProGoInstallerLegacyRestartText $path
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $state = $manager.GetLegacyPreDeleteAdmissionState($reference,$operation,$reference.Source.SnapshotHash,
            (New-Object Text.UTF8Encoding($false)).GetBytes($text))
        $again = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($state -cne 'not-applied' -or $null -eq $again -or $again.Origin -cne $reference.Origin -or $again.Hash -cne $reference.Hash) { return 'blocked' }
        return 'not-applied'
    } catch { return 'blocked' }
}
# Pins evidence and the owned original only; never called by the live installer route.
function Open-ProGoInstallerLegacyDeletionPins([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested()
    $pins = $null
    try {
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $reference) { return $null }
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $path = Assert-ProGoInstallerStagePath $parent ('installer-legacy-reference-' + $reference.Source.Operation.ToString('N') + '.json')
        $text = Read-ProGoInstallerLegacyRestartText $path
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $pins = $manager.PinLegacyPreDeleteAdmission($reference,$reference.Source.Operation,$reference.Source.SnapshotHash,
            (New-Object Text.UTF8Encoding($false)).GetBytes($text),$CancellationToken)
        if ($null -eq $pins) { return $null }
        # Original is now READ|DELETE pinned: do not reopen it with incompatible share access.
        # This lease does not pin runtime/snapshot/journal; future mutation must revalidate them.
        $CancellationToken.ThrowIfCancellationRequested()
        $result = $pins; $pins = $null; return $result
    } finally { if ($null -ne $pins) { $pins.Dispose() } }
}
# Pin existing physical objects, then independently verify the actual graph under those pins.
# No deletion, missing-leaf reservation or installer-flow integration is provided.
function Open-ProGoInstallerLegacyPhysicalPins([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested(); $pins = $null
    try {
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $reference) { return $null }
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $operation = $reference.Source.Operation.ToString('N')
        $referencePath = Assert-ProGoInstallerStagePath $parent ('installer-legacy-reference-' + $operation + '.json')
        $text = Read-ProGoInstallerLegacyRestartText $referencePath
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $documentHash = [BitConverter]::ToString($hash.ComputeHash((New-Object Text.UTF8Encoding($false)).GetBytes($text))).Replace('-','') }
        finally { $hash.Dispose() }
        # Bind the pin plan itself before opening its leaves; no transient caller/serialized plan.
        if ($documentHash -cne $reference.Hash) { return $null }
        $saved = $text | ConvertFrom-Json -ErrorAction Stop
        $entries = @(($saved.Context.Inventory | ConvertFrom-Json -ErrorAction Stop).Files)
        $allowed = @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md','README.ru.md','LICENSE.md') })
        if ($saved.Context.Operation -cne $operation -or $saved.Context.Snapshot -cnotmatch '^installer-[a-f0-9]{32}$' -or $entries.Count -ne $allowed.Count) { return $null }
        $seen = @{}; $paths = New-Object 'System.Collections.Generic.List[string]'
        $snapshot = $saved.Context.Snapshot; $journal = 'installer-journal-' + $operation
        $paths.Add('backups/' + $snapshot + '/installer-snapshot.json')
        for ($index = 0; $index -lt $entries.Count; $index++) {
            $entry = $entries[$index]
            if ($entry.Relative -cnotin $allowed -or $seen.ContainsKey($entry.Relative) -or $entry.Exists -isnot [bool]) { return $null }
            $seen[$entry.Relative] = $true
            $paths.Add($entry.Relative)
            if ($entry.Exists) { $paths.Add('backups/' + $snapshot + '/' + $entry.Relative) }
            $paths.Add(('backups/' + $journal + '/admit-{0:D6}.json' -f $index))
        }
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory,$ProgramsDirectory,$StartupDirectory)
        $pins = $manager.PinLegacyPhysicalMaterial($paths.ToArray(),$CancellationToken)
        if ($null -eq $pins) { return $null }
        # The plan and serialized claims are not authority: repeat all existing physical checks.
        $again = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $again -or $again.Origin -cne $reference.Origin -or $again.Hash -cne $reference.Hash -or
            (Get-ProGoInstallerLegacyPreDeleteState $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory) -cne 'not-applied' -or
            -not $pins.VerifyPinned($CancellationToken)) { return $null }
        $result = $pins; $pins = $null; return $result
    } finally { if ($null -ne $pins) { $pins.Dispose() } }
}
# Namespace observation only. A directory handle does NOT reserve absent children.
function Test-ProGoInstallerLegacyNamespace($Reference, [string]$Directory, [string]$InstallDirectory,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested()
    try {
        if ($Reference -isnot [ProGo.ApplicationShortcuts+LegacyJournalReferenceEvidence]) { return $false }
        $parent = Assert-ProGoInstallerStagePath $InstallDirectory 'backups'
        $operation = $Reference.Source.Operation.ToString('N'); $journal = 'installer-journal-' + $operation
        if ($Directory -cne (Assert-ProGoInstallerStagePath $parent $journal) -or
            $Reference.Source.InstallIdentity -cne (Get-ProGoInstallerIdentity $InstallDirectory)) { return $false }
        $text = Read-ProGoInstallerLegacyRestartText (Assert-ProGoInstallerStagePath $parent ('installer-legacy-reference-' + $operation + '.json'))
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $digest = [BitConverter]::ToString($hash.ComputeHash((New-Object Text.UTF8Encoding($false)).GetBytes($text))).Replace('-','') }
        finally { $hash.Dispose() }
        if ($digest -cne $Reference.Hash) { return $false }
        $saved = $text | ConvertFrom-Json -ErrorAction Stop
        if ($saved.Context.Operation -cne $operation -or $saved.Context.Journal -cne $journal -or
            $saved.Context.Identity -cne $Reference.Source.InstallIdentity -or
            $saved.Context.SnapshotHash -cne $Reference.Source.SnapshotHash -or
            $saved.Context.Snapshot -cnotmatch '^installer-[a-f0-9]{32}$') { return $false }
        $snapshot = Assert-ProGoInstallerStagePath $parent $saved.Context.Snapshot
        $entries = @(($saved.Context.Inventory | ConvertFrom-Json -ErrorAction Stop).Files)
        $allowed = @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md','README.ru.md','LICENSE.md') })
        if ($entries.Count -ne $allowed.Count) { return $false }
        $files = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        $directories = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        $seen = @{}; [void]$files.Add('installer-snapshot.json')
        foreach ($entry in $entries) {
            if ($entry.Relative -cnotin $allowed -or $seen.ContainsKey($entry.Relative) -or $entry.Exists -isnot [bool]) { return $false }
            $seen[$entry.Relative] = $true
            if (-not $entry.Exists) { continue }
            $relative = $entry.Relative.Replace('\','/'); [void]$files.Add($relative)
            $parts = @($relative.Split('/')); $prefix = ''
            for ($index = 0; $index -lt ($parts.Length - 1); $index++) {
                $prefix = if ($index -eq 0) { $parts[$index] } else { $prefix + '/' + $parts[$index] }
                [void]$directories.Add($prefix)
            }
        }
        # Enumerate one directory at a time; reject unknown/reparse nodes BEFORE descending.
        $pending = New-Object 'System.Collections.Generic.Stack[string]'; $pending.Push($snapshot)
        $fileCount = 0; $directoryCount = 0
        $actualFiles = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        $actualDirectories = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        while ($pending.Count -gt 0) {
            $current = $pending.Pop()
            if (([IO.File]::GetAttributes($current) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
            foreach ($path in [IO.Directory]::EnumerateFileSystemEntries($current)) {
                $CancellationToken.ThrowIfCancellationRequested()
                $relative = $path.Substring($snapshot.Length + 1).Replace('\','/')
                $attributes = [IO.File]::GetAttributes($path)
                if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
                if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) {
                    if (-not $directories.Contains($relative) -or -not $actualDirectories.Add($relative)) { return $false }
                    $directoryCount++; if ($directoryCount -gt $directories.Count) { return $false }
                    $pending.Push($path)
                } else {
                    if (-not $files.Contains($relative) -or -not $actualFiles.Add($relative)) { return $false }
                    $fileCount++; if ($fileCount -gt $files.Count) { return $false }
                }
            }
        }
        if ($fileCount -ne $files.Count -or $directoryCount -ne $directories.Count -or
            -not $actualFiles.SetEquals($files) -or -not $actualDirectories.SetEquals($directories)) { return $false }
        if (([IO.File]::GetAttributes($Directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
        $names = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        for ($index = 0; $index -lt $entries.Count; $index++) { [void]$names.Add(('admit-{0:D6}.json' -f $index)) }
        $count = 0; $actualNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach ($path in [IO.Directory]::EnumerateFileSystemEntries($Directory)) {
            $CancellationToken.ThrowIfCancellationRequested(); $attributes = [IO.File]::GetAttributes($path)
            if (($attributes -band ([IO.FileAttributes]::Directory -bor [IO.FileAttributes]::ReparsePoint)) -ne 0 -or
                -not $names.Contains([IO.Path]::GetFileName($path)) -or -not $actualNames.Add([IO.Path]::GetFileName($path))) { return $false }
            $count++; if ($count -gt $names.Count) { return $false }
        }
        $CancellationToken.ThrowIfCancellationRequested(); return ($count -eq $names.Count -and $actualNames.SetEquals($names))
    } catch {
        $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
        if ($cause -is [OperationCanceledException]) { throw }; return $false
    }
}
# Read-only operation-boundary rehearsal, never a capability for a later mutation.
function Get-ProGoInstallerLegacyBoundaryObservation([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested(); $physical = $null; $original = $null
    try {
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $reference) { return 'blocked' }
        $physical = Open-ProGoInstallerLegacyPhysicalPins $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken
        if ($null -eq $physical -or -not (Test-ProGoInstallerLegacyNamespace $reference $Directory $InstallDirectory $CancellationToken)) { return 'blocked' }
        $original = Open-ProGoInstallerLegacyDeletionPins $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken
        if ($null -eq $original -or -not $original.IsPinned -or
            -not (Test-ProGoInstallerLegacyNamespace $reference $Directory $InstallDirectory $CancellationToken) -or
            -not $physical.VerifyPinned($CancellationToken) -or
            -not (Test-ProGoInstallerLegacyNamespace $reference $Directory $InstallDirectory $CancellationToken)) { return 'blocked' }
        $CancellationToken.ThrowIfCancellationRequested(); return 'observed-only'
    } catch {
        $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
        if ($cause -is [OperationCanceledException]) { throw }; return 'blocked'
    }
    finally {
        try { if ($null -ne $original) { $original.Dispose() } }
        finally { if ($null -ne $physical) { $physical.Dispose() } }
    }
}
# Exact owned object only; not called by the ordinary installer/failure route.
function Invoke-ProGoInstallerLegacyOwnedDeletion([string]$Directory, [string]$InstallDirectory,
    [string]$ProgramsDirectory, [string]$StartupDirectory,
    [Threading.CancellationToken]$CancellationToken = [Threading.CancellationToken]::None) {
    $CancellationToken.ThrowIfCancellationRequested(); $physical = $null; $original = $null; $result = $null; $cleanupFailed = $false
    try {
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        if ($null -eq $reference) { return $null }
        $physical = Open-ProGoInstallerLegacyPhysicalPins $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken
        if ($null -eq $physical) { return $null }
        $original = Open-ProGoInstallerLegacyDeletionPins $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken
        if ($null -eq $original) { return $null }
        $result = $original.DeleteOwnedOnce($physical,$CancellationToken)
    } finally {
        try { if ($null -ne $original) { $original.Dispose() } } catch { $cleanupFailed = $true }
        finally { try { if ($null -ne $physical) { $physical.Dispose() } } catch { $cleanupFailed = $true } }
        if ($cleanupFailed -and $null -eq $result) { throw 'Legacy operation cleanup failed before any recorded in-memory result.' }
    }
    if ($cleanupFailed) { return $result.WithCleanupFailure() }
    return $result
}
function Open-ProGoInstallerJournalAdmission($Backup, $Entry) {
    if ($null -eq $Backup.PSObject.Properties['Journal']) {
        $Backup | Add-Member -MemberType NoteProperty -Name Journal -Value (New-ProGoInstallerJournal $Backup)
    }
    $journal = $Backup.Journal
    Assert-ProGoInstallerJournal $journal $Backup
    $expected = @($Backup.Entries | Where-Object { $_.Relative -ceq $Entry.Relative })
    if ($expected.Count -ne 1 -or ($Entry | ConvertTo-Json -Compress) -cne ($expected[0] | ConvertTo-Json -Compress)) {
        throw 'Installer journal admission is outside inventory.'
    }
    $name = ('admit-{0:D6}.json' -f $journal.Records.Count)
    $path = Assert-ProGoInstallerStagePath $journal.Directory $name
    $text = ([pscustomobject]@{ Schema = 1; Phase = 'admitted'; Sequence = $journal.Records.Count;
        Inventory = $journal.Inventory; Entry = $Entry } | ConvertTo-Json -Depth 8 -Compress)
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($text)
    $stream = $null
    try {
        $stream = New-Object IO.FileStream -ArgumentList @($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite,
            [IO.FileShare]::Read, 4096, [IO.FileOptions]::WriteThrough)
        Write-ProGoInstallerJournalBytes $stream $bytes
        $stream.Flush($true)
        # Exact same handle readback, not a separate pathname or mocked success flag.
        $stream.Position = 0
        $read = New-Object byte[] $bytes.Length
        $offset = 0
        while ($offset -lt $read.Length) {
            $count = $stream.Read($read, $offset, $read.Length - $offset)
            if ($count -eq 0) { throw 'Installer journal admission readback is incomplete.' }
            $offset += $count
        }
        if ($stream.Length -ne $bytes.Length -or [Convert]::ToBase64String($read) -cne [Convert]::ToBase64String($bytes)) {
            throw 'Installer journal admission readback is invalid.'
        }
        $journal.Records.Add([pscustomobject]@{ Name = $name; Text = $text })
        return $stream
    } catch { if ($null -ne $stream) { $stream.Dispose() }; throw }
}
function Set-ProGoInstallerRuntimeFile($Stage, $Backup, $Entry, $Writes) {
    if (@($Writes | Where-Object { $_.Relative -eq $Entry.Relative }).Count -ne 0) { throw 'Installer runtime write was already admitted.' }
    $source = Assert-ProGoInstallerStagePath $Stage.Directory $Entry.Relative
    $target = Assert-ProGoInstallerStagePath $Backup.InstallDirectory $Entry.Relative
    $runtimeSourceStream = $null; $runtimeTargetStream = $null; $record = $null; $admission = $null
    try {
        $runtimeSourceStream = [IO.File]::Open($source, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ((Get-ProGoInstallerStreamHash $runtimeSourceStream) -cne $Entry.NewHash) { throw 'Installer staged runtime changed before write.' }
        $admission = Open-ProGoInstallerJournalAdmission $Backup $Entry
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        $mode = if ($Entry.Exists) { [IO.FileMode]::Open } else { [IO.FileMode]::CreateNew }
        $runtimeTargetStream = [IO.File]::Open($target, $mode, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        if ($Entry.Exists -and (Get-ProGoInstallerStreamHash $runtimeTargetStream) -cne $Entry.Hash) { throw 'Installer runtime changed before owned write.' }
        $record = [pscustomobject]@{ Relative = $Entry.Relative; OwnedHash = $null }
        $Writes.Add($record)
        Write-ProGoInstallerRuntimeBytes $runtimeSourceStream $runtimeTargetStream
    } finally {
        try {
            if ($null -ne $runtimeTargetStream -and $null -ne $record) {
                $runtimeTargetStream.Flush()
                $record.OwnedHash = Get-ProGoInstallerStreamHash $runtimeTargetStream
            }
        } finally {
            try { if ($null -ne $runtimeTargetStream) { $runtimeTargetStream.Dispose() } }
            finally {
                try { if ($null -ne $runtimeSourceStream) { $runtimeSourceStream.Dispose() } }
                finally { if ($null -ne $admission) { $admission.Dispose() } }
            }
        }
    }
    if ($record.OwnedHash -cne $Entry.NewHash) { throw 'Installer runtime bytes are not verified.' }
}
# Retain existing file/folder call sites, but admit only the frozen runtime leaves.
function Copy-ProGoInstallerRuntime($Path, $Destination, [switch]$Force, [switch]$Recurse) {
    $sourcePath = [IO.Path]::GetFullPath($Path); $destinationPath = [IO.Path]::GetFullPath($Destination)
    $matched = 0
    if ($Recurse -and ($sourcePath -ne (Assert-ProGoInstallerStagePath $InstallStage.Directory 'scripts/home-vpn') -or
        $destinationPath -ne (Assert-ProGoInstallerStagePath $InstallBackup.InstallDirectory 'scripts'))) { throw 'Installer runtime folder scope is invalid.' }
    foreach ($entry in $InstallBackup.Entries) {
        $source = Assert-ProGoInstallerStagePath $InstallStage.Directory $entry.Relative
        if ($Recurse) { if (-not $entry.Relative.StartsWith('scripts/home-vpn/', [StringComparison]::Ordinal)) { continue } }
        elseif ($source -ne $sourcePath) { continue }
        if (-not $Recurse -and (Assert-ProGoInstallerStagePath $InstallBackup.InstallDirectory $entry.Relative) -ne $destinationPath) { throw 'Installer runtime destination is invalid.' }
        Set-ProGoInstallerRuntimeFile $InstallStage $InstallBackup $entry $InstallWrites
        $matched++
    }
    if ($matched -eq 0) { throw 'Installer runtime source is outside its inventory.' }
}
function Copy-ProGoInstallerRestoreFile([string]$Source, [string]$Destination) {
    [IO.File]::Copy($Source, $Destination, $true)
}
function Restore-ProGoInstallerRuntime($Backup, $Writes) {
    # Validate all recovery material before changing even one admitted runtime leaf.
    if ($null -ne $Backup.Directory) {
        Assert-ProGoInstallerBackupScope $Backup
        $manifest = Assert-ProGoInstallerStagePath $Backup.Directory 'installer-snapshot.json'
        if ((Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash -cne $Backup.ManifestHash) { throw 'Installer rollback manifest is not verified.' }
    }
    foreach ($entry in $Backup.Entries) {
        if (-not $entry.Exists) { continue }
        if ($null -eq $Backup.Directory) { throw 'Installer rollback snapshot is missing.' }
        $saved = Assert-ProGoInstallerStagePath $Backup.Directory $entry.Relative
        if ((Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash -cne $entry.Hash) { throw 'Installer rollback snapshot bytes are not verified.' }
    }
    $seen = @{}
    foreach ($record in $Writes) {
        if ($seen.ContainsKey($record.Relative) -or @($Backup.Entries | Where-Object { $_.Relative -ceq $record.Relative }).Count -ne 1) { throw 'Installer rollback admission is invalid.' }
        $seen[$record.Relative] = $true
    }
    for ($index = $Writes.Count - 1; $index -ge 0; $index--) {
        $record = $Writes[$index]
        $entry = @($Backup.Entries | Where-Object { $_.Relative -ceq $record.Relative })[0]
        $target = Assert-ProGoInstallerStagePath $Backup.InstallDirectory $entry.Relative
        if (-not (Test-Path -LiteralPath $target)) {
            if (-not $entry.Exists) { continue }
            throw 'Installer rollback lost runtime ownership.'
        }
        $current = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($entry.Exists -and $current -ceq $entry.Hash) { continue }
        if ($null -eq $record.OwnedHash -or $current -cne $record.OwnedHash) { throw 'Installer rollback refuses changed runtime ownership.' }
        if ($entry.Exists) {
            Copy-ProGoInstallerRestoreFile (Assert-ProGoInstallerStagePath $Backup.Directory $entry.Relative) $target
            if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -cne $entry.Hash) { throw 'Installer restored bytes are not verified.' }
        } else { [IO.File]::Delete($target) }
    }
    Assert-ProGoInstallerRuntime $Backup -Original
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
$InstallWrites = New-Object 'System.Collections.Generic.List[object]'
$InstallAuxiliaryStarted = $false
try {
    $InstallMaintenance = Enter-ProGoInstallerMaintenance
    Resolve-ProGoInstallerPendingInstalled $InstallDir -ShortcutScript (Join-Path $Payload.ScriptsDirectory 'Shortcuts-ProGo.ps1')
    Resolve-ProGoInstallerPendingRollback $InstallDir
    Assert-ProGoInstallerNoPendingJournal $InstallDir
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
    Copy-ProGoInstallerRuntime $Exe -Destination (Join-Path $InstallDir "ProGo.exe") -Force
    Copy-ProGoInstallerRuntime $VersionFile -Destination (Join-Path $InstallDir "VERSION") -Force

    $Icon = Join-Path $ReleaseDir "ProGo.ico"
    if (Test-Path $Icon) {
        Copy-ProGoInstallerRuntime $Icon -Destination (Join-Path $InstallDir "ProGo.ico") -Force
    }

    $InstalledScripts = Join-Path $InstallDir "scripts"
    New-Item -ItemType Directory -Path $InstalledScripts -Force | Out-Null
    # Install-FromGitHub.ps1 is only for first-time bootstrap. It is intentionally not deployed into the installed runtime app.
    foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Enable-HomeVpnFirewall.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Measure-RescueProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
        $scriptPath = Join-Path $PayloadScripts $scriptName
        if (Test-Path $scriptPath) {
            Copy-ProGoInstallerRuntime $scriptPath -Destination (Join-Path $InstalledScripts $scriptName) -Force
        }
    }

    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\ApplicationShortcuts.cs") -Destination (Join-Path $InstalledScripts "ApplicationShortcuts.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\MaintenanceOperation.cs") -Destination (Join-Path $InstalledScripts "MaintenanceOperation.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\InstalledUpdateTransport.cs") -Destination (Join-Path $InstalledScripts "InstalledUpdateTransport.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\UpdateInstallSession.cs") -Destination (Join-Path $InstalledScripts "UpdateInstallSession.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\BackupRetention.cs") -Destination (Join-Path $InstalledScripts "BackupRetention.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\BackupIntegrity.cs") -Destination (Join-Path $InstalledScripts "BackupIntegrity.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\BoundedLog.cs") -Destination (Join-Path $InstalledScripts "BoundedLog.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\RescueMeasurement.cs") -Destination (Join-Path $InstalledScripts "RescueMeasurement.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\DiagnosticReport.cs") -Destination (Join-Path $InstalledScripts "DiagnosticReport.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\DiagnosticPreview.cs") -Destination (Join-Path $InstalledScripts "DiagnosticPreview.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\UiTheme.cs") -Destination (Join-Path $InstalledScripts "UiTheme.cs") -Force
    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\BrandIcon.cs") -Destination (Join-Path $InstalledScripts "BrandIcon.cs") -Force

    Copy-ProGoInstallerRuntime (Join-Path $ReleaseDir "scripts\home-vpn") -Destination $InstalledScripts -Recurse -Force

    Confirm-ProGoInstallerSelfCheck $InstallBackup $InstallStage
    $InstallAuxiliaryStarted = $true
    $InstallStartupEvidence = Initialize-ProGoShortcuts -InstallDirectory $InstallDir -WasInstalled $WasInstalled -NoStartup ([bool]$NoStartup) -NoStartMenu ([bool]$NoStartMenuShortcut) -CaptureStartupEvidence
    Complete-ProGoInstallerInstalledOutcome $InstallBackup $InstallWrites -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $InstallStartupEvidence -RequireStartupEvidence

} catch {
    $installFailure = $_
    $priorVerified = $false; $rollbackAttempted = $false; $rollbackErrorType = 'none'
    # Set the conservative default before calling any fallible failure-path helper.
    $startupFailureState = if ($InstallAuxiliaryStarted) { 'blocked' } else { 'not-started' }
    try { $startupFailureState = Invoke-ProGoInstallerFailureStartup $InstallAuxiliaryStarted $InstallBackup $InstallDir } catch { }
    $menuFailureState = if ($InstallAuxiliaryStarted) { 'blocked' } else { 'not-started' }
    try { $menuFailureState = Invoke-ProGoInstallerFailureMenu $InstallAuxiliaryStarted $InstallBackup $InstallDir $startupFailureState } catch { }
    if ($startupFailureState -ceq 'not-started' -and $null -ne $InstallBackup) {
        try {
            if ($InstallWrites.Count -gt 0) {
                $rollbackAttempted = $true
                Restore-ProGoInstallerRuntime $InstallBackup $InstallWrites
            }
            Assert-ProGoInstallerRuntime $InstallBackup -Original
            $priorVerified = $true
        } catch { $rollbackErrorType = $_.Exception.GetType().Name }
    }
    # Terminal evidence may fail independently; never replace the original install error.
    $rollbackTerminalVerified = $false
    if ($priorVerified -and $InstallWrites.Count -gt 0) {
        try { Complete-ProGoInstallerRollbackOutcome $InstallBackup $InstallWrites; $rollbackTerminalVerified = $true }
        catch { $rollbackTerminalVerified = $false }
    }
    Write-Host ('Installer failure: priorRuntimeVerified=' + $priorVerified +
        '; rollbackAttempted=' + $rollbackAttempted + '; rollbackVerified=' + ($rollbackAttempted -and $priorVerified) +
        '; startupFailureState=' + $startupFailureState + '; menuFailureState=' + $menuFailureState + '; auxiliaryRollbackVerified=False' +
        '; rollbackTerminalVerified=' + $rollbackTerminalVerified + '; rollbackErrorType=' + $rollbackErrorType + '; backupRetained=' + ($null -ne $InstallBackup -and $null -ne $InstallBackup.Directory))
    throw $installFailure
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
