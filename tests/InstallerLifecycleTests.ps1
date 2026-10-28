param([string]$Scripts, [string]$StorageRoot)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: install lifecycle requires isolated Windows CI'; return }
if (-not [string]::IsNullOrWhiteSpace($StorageRoot)) {
    $StorageRoot = [IO.Path]::GetFullPath($StorageRoot).TrimEnd([char]'\')
    $leaf = [IO.Path]::GetFileName($StorageRoot)
    if ([IO.Path]::GetDirectoryName($StorageRoot) -ine [IO.Path]::GetPathRoot($StorageRoot) -or
        $leaf -cnotmatch '^ProGo-storage-fixture-[a-f0-9]{32}$' -or
        ([IO.File]::GetAttributes($StorageRoot) -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or
        [IO.File]::ReadAllText((Join-Path $StorageRoot 'owner.txt')) -cne $leaf) { throw 'Storage fixture root is not owned.' }
}
$repo = Split-Path -Parent $Scripts
$payload = Join-Path $repo 'release'
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-lifecycle-' + [guid]::NewGuid().ToString('N'))
function Check-Lifecycle($condition, [string]$name) {
    if (-not $condition) { throw ('Installer lifecycle: ' + $name) }
}
function New-LifecycleDriver([string]$source, [string]$boundary, [string]$adapters, [string]$destination) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($source, [ref]$tokens, [ref]$errors)
    if ($errors.Count) { throw 'Lifecycle consumer does not parse.' }
    $statement = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -ceq $boundary
    })
    if ($statement.Count -ne 1) { throw 'Lifecycle boundary is ambiguous.' }
    $text = [IO.File]::ReadAllText($source)
    # Execute the entire actual consumer. Insert only isolated layout/OS adapters;
    # do not reproduce install, rollback, journal, shortcut or uninstall logic.
    $text = $text.Insert($statement[0].Extent.StartOffset, $adapters + "`r`n")
    [IO.File]::WriteAllText($destination, $text, (New-Object Text.UTF8Encoding($true)))
}
$script:storageAclSettled = $true
$storageSuitePassed = $false
function Invoke-LifecycleInstall {
    if ($Fixture.Mode -cne 'access-denied') { & $installDriver -PayloadDirectory $payload -NoStartup; return }
    $path = Join-Path $Fixture.Runtime 'VERSION'
    $sections = [Security.AccessControl.AccessControlSections]'Access, Owner, Group'
    $original = [IO.File]::GetAccessControl($path, $sections).GetSecurityDescriptorSddlForm($sections)
    $versionHash = (Get-FileHash -LiteralPath $path).Hash
    $probe = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose()
    $script:storageAclSettled = $false
    try {
        $acl = [IO.File]::GetAccessControl($path)
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        try { $sid = $identity.User } finally { $identity.Dispose() }
        $rule = New-Object Security.AccessControl.FileSystemAccessRule($sid, [Security.AccessControl.FileSystemRights]::WriteData, [Security.AccessControl.AccessControlType]::Deny)
        [void]$acl.AddAccessRule($rule); [IO.File]::SetAccessControl($path, $acl)
        $denied = $false; $probe = $null
        try { $probe = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
        catch { $cause = $_.Exception.GetBaseException(); $denied = $cause -is [UnauthorizedAccessException] -and ($cause.HResult -band 65535) -eq 5 }
        finally { if ($null -ne $probe) { $probe.Dispose() } }
        Check-Lifecycle ($denied -and (Get-FileHash -LiteralPath $path).Hash -ceq $versionHash) 'real WriteData access-denied 5 with readable original bytes'
        & $installDriver -PayloadDirectory $payload -NoStartup
    } finally {
        # Restore only this created file's DACL; owner/group must also remain exact.
        $restore = New-Object Security.AccessControl.FileSecurity
        $restore.SetSecurityDescriptorSddlForm($original, [Security.AccessControl.AccessControlSections]::Access)
        [IO.File]::SetAccessControl($path, $restore)
        Check-Lifecycle ([IO.File]::GetAccessControl($path, $sections).GetSecurityDescriptorSddlForm($sections) -ceq $original) 'exact fixture ACL restored'
        $probe = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose()
        $script:storageAclSettled = $true
    }
}
$installAdapters = @'
function Get-ProGoInstallerLayout {
    $Fixture.LayoutReads++
    return Resolve-ProGoRecoveryLayoutCore -DataBase $Fixture.DataBase -ProgramBase $Fixture.ProgramBase -RuntimeRoot $Fixture.Runtime -SeparateIdentityConfirmed $true
}
function Enter-ProGoInstallerMaintenance {
    $Fixture.Enters++
    $lease = [pscustomobject]@{ State = $Fixture }
    $lease | Add-Member ScriptMethod Dispose { $this.State.Disposes++ }
    return $lease
}
function Stop-ProGoInstallerApplication($InstallDirectory, $DataDirectory) {
    Check-Lifecycle ($InstallDirectory -ceq $Fixture.Runtime -and $DataDirectory -ceq $Fixture.Data) 'install stop receives both roots'
}
function Invoke-ProGoInstallerSelfCheck($Info) {
    Check-Lifecycle ($Info.FileName -ceq (Join-Path $Fixture.Runtime 'ProGo.exe')) 'self-check executable in runtime'
    if ($Fixture.Mode -ceq 'self-check-fault') { return 17 }
    [void][IO.Directory]::CreateDirectory($Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT'])
    return 0
}
function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
    $Fixture.Writes++
    $Target.SetLength(0)
    if ($Fixture.Mode -ceq 'write-fault' -and $Fixture.Writes -eq 2) {
        $Target.WriteByte(42); $Target.Flush($true)
        throw 'Synthetic lifecycle partial write fault'
    }
    $Source.CopyTo($Target); $Target.Flush($true)
    if ($Fixture.Mode -ceq 'access-denied') { $Fixture.PublishedHash = Get-ProGoInstallerStreamHash $Target }
}
function Initialize-ProGoInstallerShortcuts($RuntimeRoot, $WasInstalled, $NoStartup, $NoStartMenu) {
    Check-Lifecycle $NoStartup 'personal candidate requests NoStartup'
    return Initialize-ProGoShortcuts -InstallDirectory $RuntimeRoot -WasInstalled $WasInstalled -NoStartup $NoStartup -NoStartMenu $NoStartMenu -ProgramsDirectory $Fixture.Programs -StartupDirectory $Fixture.Startup -CaptureStartupEvidence
}
'@
$uninstallAdapters = @'
function Get-ProGoUninstallLayout {
    if ($Fixture.ContainsKey('Legacy')) { return Resolve-ProGoRecoveryLayoutCore -DataBase $Fixture.DataBase -ProgramBase $Fixture.ProgramBase -RuntimeRoot $Fixture.Data }
    return Resolve-ProGoRecoveryLayoutCore -DataBase $Fixture.DataBase -ProgramBase $Fixture.ProgramBase -RuntimeRoot $Fixture.Runtime -SeparateIdentityConfirmed $true
}
function Enter-ProGoUninstallMaintenance {
    $Fixture.Enters++
    $lease = [pscustomobject]@{ State = $Fixture }
    $lease | Add-Member ScriptMethod Dispose { $this.State.Disposes++ }
    return $lease
}
function Stop-ProGoUninstallApplication($RuntimeRoot, $DataRoot) {
    Check-Lifecycle ($RuntimeRoot -ceq $Fixture.Runtime -and $DataRoot -ceq $Fixture.Data) 'uninstall stop receives both roots'
    if ($Fixture.Mode -ceq 'stop-fault') { throw 'Synthetic lifecycle stop refusal' }
}
function Read-Host($Prompt) {
    Check-Lifecycle ($Prompt.Contains($Fixture.Data)) 'confirmation identifies data root'
    $Fixture.Prompts++; return 'KEEP'
}
function Complete-ProGoUninstallFirewall($RuntimeRoot) {
    Check-Lifecycle ($RuntimeRoot -ceq $Fixture.Runtime) 'firewall runtime root'
}
function New-ProGoUninstallShortcuts($RuntimeRoot) {
    $manager = New-Object ProGo.ApplicationShortcuts($RuntimeRoot, $Fixture.Programs, $Fixture.Startup)
    $proxy = [pscustomobject]@{ Manager = $manager; State = $Fixture }
    $proxy | Add-Member ScriptMethod RemoveOwnedShortcuts {
        $this.Manager.RemoveOwnedShortcuts()
        if ($this.State.Mode -ceq 'legacy-conflict') { [IO.File]::WriteAllText((Join-Path $this.State.Runtime 'ProGo.exe'), 'synthetic concurrent replacement') }
    }
    $proxy | Add-Member ScriptMethod RemoveRuntimeFile {
        param($relative, $hash)
        $this.Manager.RemoveRuntimeFile($relative, $hash)
        $this.State.Deletes++
        if ($this.State.Mode -ceq 'delete-fault' -and $this.State.Deletes -eq 1) { throw 'Synthetic lifecycle post-delete fault' }
    }
    return $proxy
}
'@
try {
    [void][IO.Directory]::CreateDirectory($work)
    . (Join-Path $Scripts 'Layout-ProGo.ps1')
    . (Join-Path $Scripts 'Install-ProGo.ps1') -FunctionsOnly
    . (Join-Path $Scripts 'Shortcuts-ProGo.ps1')
    Import-ProGoShortcutType
    # Read-only actual production resolver must ignore environment redirection.
    $oldLocal = $env:LOCALAPPDATA
    try {
        $env:LOCALAPPDATA = Join-Path $work 'environment-is-not-authority'
        $layout = & { param($Payload) Get-ProGoInstallerLayout } (Get-ProGoInstallPayload -Directory $payload)
        $knownLegacy = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'ProGo'
        Check-Lifecycle ($layout.Kind -ceq 'Legacy' -and $layout.RuntimeRoot -ceq $knownLegacy -and $layout.DataRoot -ceq $knownLegacy) 'actual production known-folder layout ignores LOCALAPPDATA'
    } finally { $env:LOCALAPPDATA = $oldLocal }
    & {
        $Payload = Get-ProGoInstallPayload -Directory $payload
        $copy = Join-Path $work 'changed-layout-payload'
        [void][IO.Directory]::CreateDirectory((Join-Path $copy 'scripts'))
        $helper = Join-Path $copy 'scripts/Layout-ProGo.ps1'
        [IO.File]::Copy((Join-Path $Payload.Directory 'scripts/Layout-ProGo.ps1'), $helper)
        $Payload.Directory = $copy
        [IO.File]::AppendAllText($helper, "`r`nthrow 'Unexpected changed helper execution'`r`n")
        $refused = $false
        try { Get-ProGoInstallerLayout | Out-Null } catch { $refused = $_.Exception.Message -ceq 'Installer layout helper changed after payload verification.' }
        Check-Lifecycle $refused 'changed payload layout helper rejected before execution'
    }
    $installDriver = Join-Path $work 'InstallConsumer.ps1'
    New-LifecycleDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $installAdapters $installDriver
    $modes = @('complete', 'keep-data', 'write-fault', 'self-check-fault', 'changed-runtime', 'pending', 'stop-fault', 'delete-fault')
    if ($StorageRoot) { $modes += 'access-denied' }
    foreach ($mode in $modes) {
        $case = Join-Path $work $mode
        $Fixture = @{ Mode = $mode; DataBase = (Join-Path $case 'data'); ProgramBase = $(if ($StorageRoot) { Join-Path $StorageRoot $mode } else { Join-Path $case 'programs' }); Programs = (Join-Path $case 'menu'); Startup = (Join-Path $case 'startup'); Enters = 0; Disposes = 0; Writes = 0; Deletes = 0; Prompts = 0; LayoutReads = 0 }
        $Fixture.Data = Join-Path $Fixture.DataBase 'ProGo'
        $Fixture.Runtime = Join-Path $Fixture.ProgramBase 'ProGo'
        foreach ($dir in @($Fixture.Data, $Fixture.Runtime, $Fixture.Programs, $Fixture.Startup)) { [void][IO.Directory]::CreateDirectory($dir) }
        $personal = @{}
        foreach ($leaf in @('settings.json', 'vault.enc', 'ProGo.exe', 'foreign.txt')) {
            $path = Join-Path $Fixture.Data $leaf
            [IO.File]::WriteAllText($path, 'synthetic retained personal bytes: ' + $leaf)
            $personal[$leaf] = (Get-FileHash -LiteralPath $path).Hash
        }
        $foreign = Join-Path $Fixture.Runtime 'foreign.txt'
        [IO.File]::WriteAllText($foreign, 'foreign runtime sibling')
        $oldExe = Join-Path $Fixture.Runtime 'ProGo.exe'
        [IO.File]::Copy((Join-Path $payload 'ProGo.exe'), $oldExe)
        [IO.File]::WriteAllText((Join-Path $Fixture.Runtime 'VERSION'), '0.2.1')
        if ($mode -ceq 'access-denied') { [IO.File]::AppendAllText($oldExe, 'synthetic prior-image overlay') }
        $oldHash = (Get-FileHash -LiteralPath $oldExe).Hash
        $failed = $false
        try { Invoke-LifecycleInstall } catch {
            $failed = $true
            if ($mode -notin @('write-fault', 'self-check-fault', 'access-denied')) { throw }
            if ($mode -ceq 'access-denied') {
                $cause = $_.Exception.GetBaseException()
                Check-Lifecycle ($cause -is [UnauthorizedAccessException] -and ($cause.HResult -band 65535) -eq 5 -and $Fixture.Writes -eq 1 -and $Fixture.PublishedHash -cne $oldHash) 'actual installer denied VERSION only after changed EXE publication'
            } else {
                $expected = if ($mode -ceq 'write-fault') { 'Synthetic lifecycle partial write fault' } else { 'Installed installer self-check failed.' }
                Check-Lifecycle ($_.Exception.ToString().Contains($expected)) 'expected injected install failure'
            }
        }
        Check-Lifecycle ($failed -eq ($mode -in @('write-fault', 'self-check-fault', 'access-denied'))) 'actual install outcome'
        if ($failed) {
            Check-Lifecycle ((Get-FileHash -LiteralPath $oldExe).Hash -ceq $oldHash -and [IO.File]::ReadAllText((Join-Path $Fixture.Runtime 'VERSION')) -ceq '0.2.1') 'actual rollback restores original bytes'
            $journals = @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*')
            if ($mode -ceq 'access-denied') {
                Check-Lifecycle ($journals.Count -eq 1 -and @(Get-ChildItem -LiteralPath $journals[0].FullName -Filter 'admit-*.json').Count -eq 2 -and
                    -not [IO.File]::Exists((Join-Path $journals[0].FullName 'rolled-back.json'))) 'denied open retains two admissions without false rollback terminal'
                $admission = Read-ProGoInstallerJson (Join-Path $journals[0].FullName 'admit-000000.json')
                $inventory = $admission.Inventory | ConvertFrom-Json
                $snapshots = @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-[a-f0-9]{32}$' })
                Check-Lifecycle ($snapshots.Count -eq 1) 'denial retains recovery snapshot'
                $backup = [pscustomobject]@{ InstallDirectory = $Fixture.Runtime; Directory = $snapshots[0].FullName; ManifestHash = $inventory.SnapshotHash; Entries = @($inventory.Files) }
                Assert-ProGoInstallerSnapshotMaterial $backup; Assert-ProGoInstallerRuntime $backup -Original
                $blocked = $false
                try { Assert-ProGoInstallerNoPendingJournal $Fixture.Runtime } catch { $blocked = $_.Exception.Message -ceq 'Installer pending journal requires verified recovery before a new installation.' }
                Check-Lifecycle $blocked 'denied-open pending evidence still blocks replay'
            } else {
                Check-Lifecycle ($journals.Count -eq 1 -and (Test-ProGoInstallerRollbackOutcome $journals[0].FullName $Fixture.Runtime)) 'rollback terminal and snapshot proof'
                Assert-ProGoInstallerNoPendingJournal $Fixture.Runtime
            }
            Check-Lifecycle (-not [IO.File]::Exists((Join-Path $Fixture.Runtime 'ProGo.ico'))) 'rollback restores absent leaves'
        } else {
            $inventory = @(Get-ProGoUninstallInventory $Fixture.Runtime)
            Check-Lifecycle ($inventory.Count -eq @(Get-ProGoInstallerPayloadNames | Where-Object { $_ -notin @('README.md', 'README.ru.md', 'LICENSE.md') }).Count) 'full installed inventory admitted'
            $link = Join-Path $Fixture.Programs 'ProGo\ProGo.lnk'
            $shell = New-Object -ComObject WScript.Shell
            try {
                $shortcut = $shell.CreateShortcut($link)
                Check-Lifecycle ($shortcut.TargetPath -ceq $oldExe -and $shortcut.Arguments -ceq '--show') 'actual Start Menu target/arguments'
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut)
                Check-Lifecycle (-not [IO.File]::Exists((Join-Path $Fixture.Startup 'ProGo.lnk'))) 'NoStartup creates no startup link'
                $foreignLink = $shell.CreateShortcut((Join-Path $Fixture.Startup 'ProGo.lnk'))
                $foreignLink.TargetPath = Join-Path $payload 'ProGo.exe'; $foreignLink.Save()
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($foreignLink)
            } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
            $startupHash = (Get-FileHash -LiteralPath (Join-Path $Fixture.Startup 'ProGo.lnk')).Hash
            $menuForeign = Join-Path $Fixture.Programs 'ProGo\foreign.txt'
            [IO.File]::WriteAllText($menuForeign, 'foreign menu sibling')
            $driver = Join-Path $Fixture.Runtime 'scripts\SyntheticUninstall.ps1'
            New-LifecycleDriver (Join-Path $Scripts 'Uninstall-ProGo.ps1') '$Layout' $uninstallAdapters $driver
            if ($mode -ceq 'changed-runtime') { [IO.File]::WriteAllText((Join-Path $Fixture.Runtime 'VERSION'), 'foreign replacement') }
            if ($mode -ceq 'pending') { [void][IO.Directory]::CreateDirectory((Join-Path $Fixture.Runtime ('backups\installer-journal-' + [guid]::NewGuid().ToString('N')))) }
            $uninstallFailed = $false; $uninstallError = ''
            try { if ($mode -ceq 'keep-data') { & $driver -RemoveUserData } else { & $driver } } catch { $uninstallFailed = $true; $uninstallError = $_.Exception.ToString(); if ($mode -in @('complete', 'keep-data')) { throw } }
            Check-Lifecycle ($uninstallFailed -eq ($mode -notin @('complete', 'keep-data'))) 'actual uninstall outcome'
            if ($uninstallFailed) {
                $expected = switch ($mode) {
                    'changed-runtime' { 'Uninstall requires a verified installed runtime inventory.' }
                    'pending' { 'Installer pending journal requires verified recovery before a new installation.' }
                    'stop-fault' { 'Synthetic lifecycle stop refusal' }
                    'delete-fault' { 'Synthetic lifecycle post-delete fault' }
                }
                Check-Lifecycle ($uninstallError.Contains($expected)) 'expected uninstall refusal or injected error'
            }
            if ($mode -in @('changed-runtime', 'pending', 'stop-fault')) {
                Check-Lifecycle ([IO.File]::Exists($oldExe) -and [IO.File]::Exists($link) -and $Fixture.Deletes -eq 0) 'refusal precedes shortcut/runtime deletion'
            } else {
                if ($mode -ceq 'delete-fault') {
                    Check-Lifecycle (-not [IO.File]::Exists($oldExe) -and $Fixture.Deletes -eq 1) 'real partial uninstall retained for retry'
                    $Fixture.Mode = 'retry'; & $driver
                }
                foreach ($entry in $inventory) { Check-Lifecycle (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $entry.Relative))) 'owned runtime leaf removed' }
                Check-Lifecycle (-not [IO.File]::Exists($link)) 'owned menu link removed'
                Check-Lifecycle (@(Get-ProGoUninstallInventory $Fixture.Runtime).Count -eq $inventory.Count) 'recovery proofs survive uninstall'
                Check-Lifecycle ([IO.File]::Exists($driver)) 'foreign runtime script preserved'
            }
            Check-Lifecycle ((Get-FileHash -LiteralPath (Join-Path $Fixture.Startup 'ProGo.lnk')).Hash -ceq $startupHash -and [IO.File]::ReadAllText($menuForeign) -ceq 'foreign menu sibling') 'foreign Startup and Start Menu preserved'
        }
        foreach ($leaf in $personal.Keys) { Check-Lifecycle ((Get-FileHash -LiteralPath (Join-Path $Fixture.Data $leaf)).Hash -ceq $personal[$leaf]) 'data and legacy executable preserved' }
        Check-Lifecycle ([IO.File]::ReadAllText($foreign) -ceq 'foreign runtime sibling') 'foreign runtime preserved'
        Check-Lifecycle ($Fixture.Prompts -eq [int]($mode -ceq 'keep-data')) 'data removal uses its separate confirmation'
        Check-Lifecycle ($Fixture.LayoutReads -eq 1) 'install resolves both roots exactly once'
        Check-Lifecycle ($Fixture.Enters -eq $Fixture.Disposes) 'all maintenance leases released'
        Write-Host ('PASS: actual install/uninstall lifecycle ' + $mode)
    }
    $foreignExe = Join-Path $work 'ForeignLegacyPayload.exe'
    Add-Type -TypeDefinition 'public static class ForeignLegacyPayload { public static void Main() {} }' -OutputAssembly $foreignExe -OutputType ConsoleApplication
    foreach ($mode in @('legacy-valid', 'legacy-missing', 'legacy-corrupt', 'legacy-foreign', 'legacy-conflict')) {
        $case = Join-Path $work $mode
        $Fixture = @{ Mode = $mode; Legacy = $true; DataBase = (Join-Path $case 'data'); ProgramBase = (Join-Path $case 'programs'); Programs = (Join-Path $case 'menu'); Startup = (Join-Path $case 'startup'); Enters = 0; Disposes = 0; Deletes = 0; Prompts = 0 }
        $Fixture.Data = Join-Path $Fixture.DataBase 'ProGo'; $Fixture.Runtime = $Fixture.Data
        foreach ($dir in @($Fixture.Data, $Fixture.ProgramBase, $Fixture.Programs, $Fixture.Startup, (Join-Path $Fixture.Data 'scripts'))) { [void][IO.Directory]::CreateDirectory($dir) }
        $legacyExe = Join-Path $Fixture.Data 'ProGo.exe'
        [IO.File]::Copy((Join-Path $payload 'ProGo.exe'), $legacyExe)
        foreach ($leaf in @('Uninstall-ProGo.ps1', 'Maintenance-ProGo.ps1', 'MaintenanceOperation.cs', 'Firewall-ProGo.ps1', 'Enable-HomeVpnFirewall.ps1', 'Layout-ProGo.ps1', 'Install-ProGo.ps1', 'Shortcuts-ProGo.ps1', 'ApplicationShortcuts.cs')) {
            [IO.File]::Copy((Join-Path $payload ('scripts/' + $leaf)), (Join-Path $Fixture.Data ('scripts/' + $leaf)))
        }
        $manager = New-Object ProGo.ApplicationShortcuts($Fixture.Data, $Fixture.Programs, $Fixture.Startup)
        $manager.Install($false, $false, $false)
        $ownedMenu = Join-Path $Fixture.Programs 'ProGo/ProGo.lnk'; $ownedStartup = Join-Path $Fixture.Startup 'ProGo.lnk'
        $foreignLink = Join-Path $Fixture.Programs 'ProGo/ProGo Status.lnk'
        $shell = New-Object -ComObject WScript.Shell
        try {
            $shortcut = $shell.CreateShortcut($foreignLink)
            $shortcut.TargetPath = $foreignExe; $shortcut.Arguments = '--show'; $shortcut.Save()
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut)
        } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
        $retained = @{}
        foreach ($path in @((Join-Path $Fixture.Data 'settings.json'), (Join-Path $Fixture.Data 'vault.enc'), (Join-Path $Fixture.Data 'foreign.txt'), (Join-Path $Fixture.Programs 'ProGo/foreign.txt'))) { [IO.File]::WriteAllText($path, 'synthetic retained legacy/foreign bytes') }
        foreach ($file in Get-ChildItem -LiteralPath $Fixture.Data -Recurse -File) {
            if ($file.Name -cne 'ProGo.exe') { $retained[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        }
        $retained[$foreignLink] = (Get-FileHash -LiteralPath $foreignLink).Hash
        $menuSibling = Join-Path $Fixture.Programs 'ProGo/foreign.txt'; $retained[$menuSibling] = (Get-FileHash -LiteralPath $menuSibling).Hash
        if ($mode -ceq 'legacy-missing') { [IO.File]::Delete($legacyExe) }
        if ($mode -ceq 'legacy-corrupt') { [IO.File]::WriteAllBytes($legacyExe, [byte[]]@(0, 42, 255, 1)) }
        if ($mode -ceq 'legacy-foreign') { [IO.File]::Copy($foreignExe, $legacyExe, $true) }
        $foreignHash = if ($mode -ceq 'legacy-foreign') { (Get-FileHash -LiteralPath $legacyExe).Hash } else { $null }
        $driver = Join-Path $Fixture.Data 'scripts/SyntheticLegacyUninstall.ps1'
        New-LifecycleDriver (Join-Path $Scripts 'Uninstall-ProGo.ps1') '$Layout' $uninstallAdapters $driver
        $failed = $false; $reason = ''
        try { & $driver } catch { $failed = $true; $reason = $_.Exception.ToString() }
        Check-Lifecycle ($failed -eq ($mode -in @('legacy-foreign', 'legacy-conflict'))) 'actual legacy outcome'
        if ($mode -ceq 'legacy-foreign') {
            Check-Lifecycle ($reason.Contains('Uninstall refuses a foreign executable.') -and (Get-FileHash -LiteralPath $legacyExe).Hash -ceq $foreignHash -and [IO.File]::Exists($ownedMenu) -and [IO.File]::Exists($ownedStartup)) 'recognized foreign managed payload refused without deletion'
        } elseif ($mode -ceq 'legacy-conflict') {
            Check-Lifecycle ($reason.Contains('Uninstall file changed.') -and [IO.File]::ReadAllText($legacyExe) -ceq 'synthetic concurrent replacement' -and $Fixture.Deletes -eq 0) 'native delete-handle revalidation refuses changed bytes'
        } else {
            Check-Lifecycle (-not [IO.File]::Exists($legacyExe) -and -not [IO.File]::Exists($ownedMenu) -and -not [IO.File]::Exists($ownedStartup)) 'valid/missing/corrupt legacy EXE-only uninstall removes owned links'
        }
        foreach ($path in $retained.Keys) { Check-Lifecycle ((Get-FileHash -LiteralPath $path).Hash -ceq $retained[$path]) 'legacy data/helpers/foreign objects retained' }
        Check-Lifecycle ($Fixture.Enters -eq 1 -and $Fixture.Disposes -eq 1 -and $Fixture.Prompts -eq 0) 'legacy lease released and no implicit data removal'
        Write-Host ('PASS: actual legacy uninstall ' + $mode)
    }
    if ($StorageRoot) { Write-Host 'Installer lifecycle storage cases PASS: 9 split + 5 legacy; real ACL restored' }
    else { Write-Host 'Installer lifecycle tests PASS: 8 split + 5 legacy; different-volume/access-denied NOT_RUN' }
    $storageSuitePassed = $true
} finally {
    if ($script:storageAclSettled -and (-not $StorageRoot -or $storageSuitePassed) -and [IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
    elseif (-not $script:storageAclSettled -or ($StorageRoot -and -not $storageSuitePassed)) { Write-Warning ('Storage/ACL fixture evidence retained: ' + $work) }
}
