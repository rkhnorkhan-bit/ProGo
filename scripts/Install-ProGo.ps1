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
function Complete-ProGoInstallerInstalledOutcome($Backup, $Writes, [string[]]$CompletedPhases = @()) {
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
    $text = ([pscustomobject]@{ Schema = 1; Phase = 'installed'; Identity = (Get-ProGoInstallerIdentity $Backup.InstallDirectory);
        Journal = [IO.Path]::GetFileName($Backup.Journal.Directory); Snapshot = $(if ($null -eq $Backup.Directory) { $null } else { [IO.Path]::GetFileName($Backup.Directory) });
        Inventory = $Backup.Journal.Inventory; Records = $proofs;
        Completion = [pscustomobject]@{ Schema = 1; Phases = @($CompletedPhases); Ownership = @($Writes | ForEach-Object {
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
function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
    $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
}
# An intent is not a terminal outcome or post-crash ownership proof.
# Until durable terminal outcomes exist, every previous installer journal is pending.
# Only finish metadata publication of an already-proven rollback. Never restore bytes.
function Resolve-ProGoInstallerPendingRollback([string]$InstallDirectory) {
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
try {
    $InstallMaintenance = Enter-ProGoInstallerMaintenance
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
    Initialize-ProGoShortcuts -InstallDirectory $InstallDir -WasInstalled $WasInstalled -NoStartup ([bool]$NoStartup) -NoStartMenu ([bool]$NoStartMenuShortcut)
    Complete-ProGoInstallerInstalledOutcome $InstallBackup $InstallWrites -CompletedPhases @('self-check', 'shortcuts')

} catch {
    $installFailure = $_
    $priorVerified = $false; $rollbackAttempted = $false; $rollbackErrorType = 'none'
    if ($null -ne $InstallBackup) {
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
