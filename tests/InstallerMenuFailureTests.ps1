Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$actualInitialize = (Get-Command Initialize-ProGoShortcuts).ScriptBlock
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer Menu failure path does not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
# Isolated legacy destination; production known folders must not be redirected.
function Get-ProGoInstallerLayout {
    $legacy = Join-Path $env:LOCALAPPDATA 'ProGo'
    return [pscustomobject]@{ RuntimeRoot = $legacy; DataRoot = $legacy }
}
$boundary = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir' })
if ($boundary.Count -ne 1) { throw 'Installer Menu failure boundary is ambiguous.' }
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
$actualStartupFailure = (Get-Command Invoke-ProGoInstallerFailureStartup).ScriptBlock
$actualMenuFailure = (Get-Command Invoke-ProGoInstallerFailureMenu).ScriptBlock
$actualMenuRollback = (Get-Command Invoke-ProGoInstallerMenuRollback).ScriptBlock
function Invoke-ProGoInstallerMenuRollback($Directory, $InstallDirectory, $ProgramsDirectory, $StartupDirectory) {
    if (-not $fixture.LeaseHeld) { throw 'Owned Menu call escaped maintenance.' }
    $fixture.MenuCalls++
    return (& $actualMenuRollback $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory)
}
function Invoke-ProGoInstallerFailureStartup($AuxiliaryStarted, $Backup, $InstallDirectory) {
    if (-not $fixture.LeaseHeld) { throw 'Startup failure escaped maintenance.' }
    if ($AuxiliaryStarted -and $null -ne $Backup) {
        $directory = $Backup.Journal.Directory
        if ($fixture.Case -eq 'startup-blocked') { $lease = Open-ProGoInstallerStartupMutationAdmission $directory $InstallDirectory $programs $startup; $lease.Dispose() }
        if ($fixture.Case -in @('admission-not-applied','already-removed','already-result','admission-corrupt','result-corrupt')) {
            Check-MenuFailure (Invoke-ProGoInstallerStartupRollback $directory $InstallDirectory $programs $startup) 'real Startup mutation precedes Menu crash boundary'
            Check-MenuFailure (Complete-ProGoInstallerStartupMutationSettlement $directory $InstallDirectory $programs $startup) 'actual Startup durable settlement precedes Menu crash boundary'
            if ($fixture.Case -in @('admission-not-applied','admission-corrupt')) { $lease = Open-ProGoInstallerMenuMutationAdmission $directory $InstallDirectory $programs $startup; $lease.Dispose() }
            else { Check-MenuFailure (Invoke-ProGoInstallerMenuRollback $directory $InstallDirectory $programs $startup) 'real accepted Menu deletion precedes crash boundary' }
            if ($fixture.Case -eq 'admission-corrupt') {
                [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $directory) ('installer-menu-admission-' + (Split-Path -Leaf $directory).Substring('installer-journal-'.Length) + '.json')),'{}')
            }
            if ($fixture.Case -eq 'result-corrupt') {
                [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $directory) ('installer-menu-outcome-' + (Split-Path -Leaf $directory).Substring('installer-journal-'.Length) + '.json')),'{}')
            }
            if ($fixture.Case -eq 'already-result') { Complete-ProGoInstallerMenuResult $directory $InstallDirectory $programs $startup }
        }
    }
    return (& $actualStartupFailure $AuxiliaryStarted $Backup $InstallDirectory $programs $startup)
}
function Invoke-ProGoInstallerFailureMenu($AuxiliaryStarted, $Backup, $InstallDirectory, $StartupFailureState) {
    if (-not $fixture.LeaseHeld) { throw 'Menu failure escaped maintenance.' }
    if ($AuxiliaryStarted -and $null -ne $Backup) {
        if ($fixture.Case -eq 'menu-replaced') {
            $bytes = [IO.File]::ReadAllBytes($link); $creation = [IO.File]::GetCreationTimeUtc($link)
            [IO.File]::Move($link,(Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($link,$bytes); [IO.File]::SetCreationTimeUtc($link,$creation)
        }
        if ($fixture.Case -eq 'delete-locked') { $script:held = [IO.File]::Open($link,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read) }
        $fixture.Unchanged = @{}
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) {
            $fixture.Unchanged[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash
        }
    }
    return (& $actualMenuFailure $AuxiliaryStarted $Backup $InstallDirectory $StartupFailureState $programs $startup)
}
function Check-MenuFailure($Condition, $Name) { if (-not $Condition) { throw ('Installer Menu failure test failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-failure-' + [Guid]::NewGuid().ToString('N'))
$InstallStage = $null
$owned = $false; $oldLocal = $env:LOCALAPPDATA; $held = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $fixturePayload = Join-Path $work 'payload'
    Copy-Item -LiteralPath (Join-Path $Root 'release') -Destination $fixturePayload -Recurse
    # The adapter substitutes only independent test locations and the executable self-check.
    # Actual shortcut creation, native rollback, receipt writers and error body remain real.
    @'
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
function Initialize-ProGoShortcuts {
    param($InstallDirectory, $WasInstalled, $NoStartup, $NoStartMenu, [switch]$CaptureStartupEvidence)
    if (-not $fixture.LeaseHeld -or $fixture.Stops -ne 1) { throw 'Auxiliary operation escaped maintenance.' }
    $receipt = & $actualInitialize -InstallDirectory $InstallDirectory -WasInstalled $false -NoStartup ([bool]$NoStartup) -NoStartMenu ([bool]$NoStartMenu) -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
    $fixture.CreatedHash = if ([IO.File]::Exists($link)) { (Get-FileHash -LiteralPath $link).Hash } else { $null }
    if ($fixture.Case -eq 'receipt-missing') { throw 'synthetic original installation error' }
    return $receipt
}
'@ | Set-Content -LiteralPath (Join-Path $fixturePayload 'scripts\Shortcuts-ProGo.ps1') -Encoding UTF8
    $payload = Get-ProGoInstallPayload $fixturePayload
    $scenarios = @('created','custom-preserved','absent-preserved','legacy-removed','receipt-missing','receipt-partial',
        'menu-changed','menu-replaced','snapshot-changed','admission-persist-fail','result-persist-fail',
        'admission-not-applied','already-removed','already-result','delete-locked','stop-refused','startup-blocked','admission-corrupt','result-corrupt')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'ProGo'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $programs 'ProGo\ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup, (Join-Path $programs 'ProGo') | Out-Null
        $env:LOCALAPPDATA = $case
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic original runtime')
        $private = @{}; foreach ($leaf in @('settings.json', 'vault.enc', 'keys\synthetic.key', 'foreign.txt')) {
            $path = Join-Path $install $leaf; [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
            [IO.File]::WriteAllText($path, 'synthetic private bytes'); $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        if ($scenario -eq 'legacy-removed') {
            $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
            $manager.Install($true,$true,$false); [IO.File]::Move($link,(Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        if ($scenario -eq 'custom-preserved') { [IO.File]::WriteAllText($link, 'synthetic custom shortcut') }
        $fixture = @{ Case = $scenario; LeaseHeld = $false; Stops = 0; Disposes = 0; Launches = 0; Success = $false; State = 'none'; MenuCalls = 0; FullRollback = $false; Writes = 0; CreatedHash = $null; MutatedHash = $null; Unchanged = @{} }
        function Enter-ProGoInstallerMaintenance {
            $fixture.LeaseHeld = $true
            $lease = [pscustomobject]@{ State = $fixture }
            $lease | Add-Member ScriptMethod Dispose { $this.State.Disposes++; $this.State.LeaseHeld = $false }
            return $lease
        }
        function Stop-ProGoInstallerApplication($InstallDirectory) { $fixture.Stops++; if ($fixture.Case -eq 'stop-refused') { throw 'synthetic original installation error' } }
        function Invoke-ProGoInstallerSelfCheck($Info) { [IO.Directory]::CreateDirectory($Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT']) | Out-Null; return 0 }
        function Start-Process { $fixture.Launches++; throw 'Unexpected fixture Launch.' }
        function Write-Host($Object) {
            if ($Object -like 'Install OK:*') { $fixture.Success = $true }
            if ($Object -match 'menuFailureState=([^;]+)') { $fixture.State = $Matches[1] }
            if ($Object -match 'rollbackTerminalVerified=True') { $fixture.FullRollback = $true }
        }
        function Write-ProGoInstallerMenuAdmissionBytes($Stream, [byte[]]$Bytes) {
            if ($fixture.Case -eq 'admission-persist-fail') { throw 'synthetic admission persistence error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
        }
        function Write-ProGoInstallerMenuResultBytes($Stream, [byte[]]$Bytes) {
            if ($fixture.Case -eq 'result-persist-fail') { throw 'synthetic result persistence error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
        }
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $fixture.Writes++
            if ($fixture.Case -eq 'receipt-partial') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); throw 'synthetic original installation error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
            if ($fixture.Case -eq 'menu-changed') { [IO.File]::WriteAllText($link,'synthetic external menu') }
            if ($fixture.Case -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $InstallBackup.Directory 'ProGo.exe'),'synthetic external snapshot') }
            $fixture.MutatedHash = if ([IO.File]::Exists($link)) { (Get-FileHash -LiteralPath $link).Hash } else { $null }
            throw 'synthetic original installation error'
        }
        $Payload = $payload; $PayloadScripts = $payload.ScriptsDirectory; $ReleaseDir = $payload.Directory
        $Exe = $payload.Executable; $VersionFile = $payload.VersionFile; $Launch = $true
        $NoStartup = $false; $NoStartMenuShortcut = $scenario -in @('custom-preserved','absent-preserved')
        $caught = $false
        try { . $body } catch { $caught = $_.Exception.Message -ceq 'synthetic original installation error' }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
        Check-MenuFailure ($caught -and $fixture.Stops -eq 1 -and $fixture.Disposes -eq 1 -and -not $fixture.LeaseHeld) 'actual error path preserves original error and settles Stop/maintenance finally'
        Check-MenuFailure ($fixture.Launches -eq 0 -and -not $fixture.Success -and -not $fixture.FullRollback) 'no install success Launch or full rollback claim'
        foreach ($path in $fixture.Unchanged.Keys) {
            if ($path -ceq $link -and $scenario -in @('created','legacy-removed','result-persist-fail')) { continue }
            Check-MenuFailure ([IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $fixture.Unchanged[$path]) 'all pre-Menu snapshot Startup Legacy receipt private and external bytes unchanged'
        }
        foreach ($path in $private.Keys) { Check-MenuFailure ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'settings vault keys and foreign bytes untouched' }
        if ($scenario -eq 'stop-refused') {
            Check-MenuFailure ($fixture.State -ceq 'not-started' -and $fixture.Writes -eq 0 -and [IO.File]::ReadAllText((Join-Path $install 'ProGo.exe')) -ceq 'synthetic original runtime') 'Stop refusal never enters auxiliary mutation'
        } else {
            $observed = $scenario -in @('created','custom-preserved','absent-preserved','legacy-removed','already-removed','already-result')
            Check-MenuFailure ($fixture.State -ceq $(if ($observed) { 'menu-observed' } else { 'blocked' })) 'reported Menu state requires exact durable independently verified result'
            Assert-ProGoInstallerRuntime $InstallBackup
            Check-MenuFailure ([IO.File]::ReadAllText((Join-Path $install 'ProGo.exe')) -cne 'synthetic original runtime') 'incomplete auxiliary rollback cannot restore runtime or claim terminal'
            if ($observed) {
                Check-MenuFailure (Test-ProGoInstallerMenuResult $InstallBackup.Journal.Directory $install $programs $startup) 'actual durable Menu result verifies exact admission and full physical context'
                $saved = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $InstallBackup.Journal.Directory) ('installer-menu-outcome-' + (Split-Path -Leaf $InstallBackup.Journal.Directory).Substring('installer-journal-'.Length) + '.json')) -Raw | ConvertFrom-Json
                Check-MenuFailure (-not $saved.FullRollback -and -not $saved.InstallSucceeded -and -not $saved.MutationAllowed) 'Menu result grants no full rollback success or mutation authority'
                if ($scenario -eq 'custom-preserved') { Check-MenuFailure ([IO.File]::ReadAllText($link) -ceq 'synthetic custom shortcut') 'custom Menu never mutated' }
                else { Check-MenuFailure (-not [IO.File]::Exists($link)) 'accepted Menu result is actual original absence' }
                $callsBeforeRepeat = $fixture.MenuCalls
                $after = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $after[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
                $repeatMaintenance = Enter-ProGoInstallerMaintenance
                try { Check-MenuFailure ((& $actualMenuFailure $true $InstallBackup $install 'blocked' $programs $startup) -ceq 'menu-observed') 'repeat failure handling only observes existing admission/result without deletion replay' }
                finally { $repeatMaintenance.Dispose() }
                Check-MenuFailure (-not $fixture.LeaseHeld -and $fixture.Stops -eq 1 -and $fixture.Disposes -eq 2) 'repeat observation settles its own maintenance without replaying Stop'
                Check-MenuFailure ($fixture.MenuCalls -eq $callsBeforeRepeat) 'existing admission/result never re-enters owned Menu mutation'
                Check-MenuFailure (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $after.Count) 'repeat creates no resources'
                foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-MenuFailure ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $after[$file.FullName]) 'repeat observation changes no byte' }
            } elseif ($scenario -in @('result-persist-fail','result-corrupt')) {
                Check-MenuFailure (-not [IO.File]::Exists($link) -and -not (Test-ProGoInstallerMenuResult $InstallBackup.Journal.Directory $install $programs $startup)) 'accepted deletion plus failed result persistence stays blocked'
            } else {
                if ($scenario -eq 'absent-preserved') { Check-MenuFailure (-not [IO.File]::Exists($link)) 'preserved absence remains' }
                else { Check-MenuFailure ([IO.File]::Exists($link)) 'unproved or unapplied admission never deletes Menu'
                }
            }
            Check-MenuFailure (@(Get-ChildItem -LiteralPath $InstallBackup.Journal.Directory -Filter 'rolled-back.json').Count -eq 0) 'Menu observation does not publish whole rollback'
            $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
            Check-MenuFailure $blocked 'unresolved auxiliary state still blocks next automatic attempt'
            foreach ($file in Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        }
        if ($scenario -ne 'stop-refused') {
            $parent = Split-Path -Parent $InstallBackup.Journal.Directory
            $suffix = (Split-Path -Leaf $InstallBackup.Journal.Directory).Substring('installer-journal-'.Length)
            $allowedNew = @((Join-Path $parent ('installer-menu-admission-' + $suffix + '.json')), (Join-Path $parent ('installer-menu-outcome-' + $suffix + '.json')))
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) {
                Check-MenuFailure ($fixture.Unchanged.ContainsKey($file.FullName) -or $file.FullName -cin $allowedNew) 'only exact owned Menu admission/result may be newly created'
            }
            $expectedCalls = if ($scenario -in @('created','custom-preserved','absent-preserved','legacy-removed','admission-persist-fail','result-persist-fail','already-removed','already-result','delete-locked','result-corrupt')) { 1 } else { 0 }
            Check-MenuFailure ($fixture.MenuCalls -eq $expectedCalls) ('exact owned Menu call count; expected=' + $expectedCalls + '; observed=' + $fixture.MenuCalls)
        }
        Check-MenuFailure ($null -eq $InstallStage -or -not [IO.Directory]::Exists($InstallStage.Directory)) 'actual stage finally cleanup completes'
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer Menu failure ' + $scenario)
    }
    Microsoft.PowerShell.Utility\Write-Host ('Installer Menu failure tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $InstallStage -and [IO.Directory]::Exists($InstallStage.Directory)) { Remove-ProGoInstallerStage $InstallStage }
    $env:LOCALAPPDATA = $oldLocal
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-menu-failure-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Fixture cleanup refused unowned root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Fixture cleanup refused reparse point.' } }
        if ((Get-Item -LiteralPath $work).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refused linked root.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
