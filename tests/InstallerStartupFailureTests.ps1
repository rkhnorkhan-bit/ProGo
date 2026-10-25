Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$actualInitialize = (Get-Command Initialize-ProGoShortcuts).ScriptBlock
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer Startup failure path does not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$boundary = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir' })
if ($boundary.Count -ne 1) { throw 'Installer Startup failure boundary is ambiguous.' }
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
$actualFailure = (Get-Command Invoke-ProGoInstallerFailureStartup).ScriptBlock
function Invoke-ProGoInstallerFailureStartup($AuxiliaryStarted, $Backup, $InstallDirectory) {
    if (-not $fixture.LeaseHeld) { throw 'Startup failure escaped maintenance.' }
    # Admission experiments run after the failed writer's finally closes its own receipt.
    if ($AuxiliaryStarted -and $null -ne $Backup) {
        if ($fixture.Case -eq 'admission-not-applied') { $lease = Open-ProGoInstallerStartupMutationAdmission $Backup.Journal.Directory $InstallDirectory $programs $startup; $lease.Dispose() }
        if ($fixture.Case -eq 'already-observed') { Check-StartupFailure (Invoke-ProGoInstallerStartupRollback $Backup.Journal.Directory $InstallDirectory $programs $startup) 'prior actual owned mutation completed before failure handling' }
    }
    $fixture.Unchanged = @{}
    foreach ($root in @($programs, $(if ($null -ne $Backup) { $Backup.Directory } else { $null }))) {
        if ($null -ne $root -and [IO.Directory]::Exists($root)) {
            foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) { $fixture.Unchanged[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        }
    }
    return (& $actualFailure $AuxiliaryStarted $Backup $InstallDirectory $programs $startup)
}
function Check-StartupFailure($Condition, $Name) { if (-not $Condition) { throw ('Installer Startup failure test failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-startup-failure-' + [Guid]::NewGuid().ToString('N'))
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
    $receipt = & $actualInitialize -InstallDirectory $InstallDirectory -WasInstalled $false -NoStartup ([bool]$NoStartup) -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
    $fixture.CreatedHash = if ([IO.File]::Exists($link)) { (Get-FileHash -LiteralPath $link).Hash } else { $null }
    if ($fixture.Case -eq 'receipt-missing') { throw 'synthetic original installation error' }
    return $receipt
}
'@ | Set-Content -LiteralPath (Join-Path $fixturePayload 'scripts\Shortcuts-ProGo.ps1') -Encoding UTF8
    $payload = Get-ProGoInstallPayload $fixturePayload
    $scenarios = @('created', 'custom-preserved', 'absent-preserved', 'receipt-missing', 'receipt-partial',
        'startup-changed', 'startup-replaced', 'menu-changed', 'snapshot-changed', 'admission-persist-fail',
        'result-persist-fail', 'admission-not-applied', 'already-observed', 'delete-locked', 'stop-refused')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'ProGo'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup | Out-Null
        $env:LOCALAPPDATA = $case
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic original runtime')
        $private = @{}; foreach ($leaf in @('settings.json', 'vault.enc', 'keys\synthetic.key', 'foreign.txt')) {
            $path = Join-Path $install $leaf; [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
            [IO.File]::WriteAllText($path, 'synthetic private bytes'); $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        if ($scenario -eq 'custom-preserved') { [IO.File]::WriteAllText($link, 'synthetic custom shortcut') }
        $fixture = @{ Case = $scenario; LeaseHeld = $false; Stops = 0; Disposes = 0; Launches = 0; Success = $false; State = 'none'; FullRollback = $false; Writes = 0; CreatedHash = $null; MutatedHash = $null; Unchanged = @{} }
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
            if ($Object -match 'startupFailureState=([^;]+)') { $fixture.State = $Matches[1] }
            if ($Object -match 'rollbackTerminalVerified=True') { $fixture.FullRollback = $true }
        }
        function Write-ProGoInstallerStartupAdmissionBytes($Stream, [byte[]]$Bytes) {
            if ($fixture.Case -eq 'admission-persist-fail') { throw 'synthetic admission persistence error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
        }
        function Write-ProGoInstallerStartupResultBytes($Stream, [byte[]]$Bytes) {
            if ($fixture.Case -eq 'result-persist-fail') { throw 'synthetic result persistence error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
        }
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $fixture.Writes++
            if ($fixture.Case -eq 'receipt-partial') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); throw 'synthetic original installation error' }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
            if ($fixture.Case -eq 'startup-changed') { [IO.File]::WriteAllText($link, 'synthetic external bytes') }
            if ($fixture.Case -eq 'startup-replaced') { $bytes = [IO.File]::ReadAllBytes($link); [IO.File]::Move($link,(Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($link,$bytes) }
            if ($fixture.Case -eq 'menu-changed') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic external menu') }
            if ($fixture.Case -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $InstallBackup.Directory 'ProGo.exe'), 'synthetic external snapshot') }
            if ($fixture.Case -eq 'delete-locked') { $script:held = [IO.File]::Open($link,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read) }
            $fixture.MutatedHash = if ([IO.File]::Exists($link)) { (Get-FileHash -LiteralPath $link).Hash } else { $null }
            throw 'synthetic original installation error'
        }
        $Payload = $payload; $PayloadScripts = $payload.ScriptsDirectory; $ReleaseDir = $payload.Directory
        $Exe = $payload.Executable; $VersionFile = $payload.VersionFile; $Launch = $true
        $NoStartup = $scenario -in @('custom-preserved','absent-preserved'); $NoStartMenuShortcut = $false
        $caught = $false
        try { . $body } catch { $caught = $_.Exception.Message -ceq 'synthetic original installation error' }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
        Check-StartupFailure ($caught -and $fixture.Stops -eq 1 -and $fixture.Disposes -eq 1 -and -not $fixture.LeaseHeld) 'actual error path preserves original error and settles Stop/maintenance finally'
        Check-StartupFailure ($fixture.Launches -eq 0 -and -not $fixture.Success -and -not $fixture.FullRollback) 'no install success Launch or full rollback claim'
        foreach ($path in $fixture.Unchanged.Keys) { Check-StartupFailure ([IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $fixture.Unchanged[$path]) 'Menu Legacy snapshot and external replacements retained byte-for-byte' }
        foreach ($path in $private.Keys) { Check-StartupFailure ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'settings vault keys and foreign bytes untouched' }
        if ($scenario -eq 'stop-refused') {
            Check-StartupFailure ($fixture.State -ceq 'not-started' -and $fixture.Writes -eq 0 -and [IO.File]::ReadAllText((Join-Path $install 'ProGo.exe')) -ceq 'synthetic original runtime') 'Stop refusal never enters auxiliary mutation'
        } else {
            $observed = $scenario -in @('created','custom-preserved','absent-preserved','already-observed')
            Check-StartupFailure ($fixture.State -ceq $(if ($observed) { 'startup-observed' } else { 'blocked' })) 'reported state requires exact durable observed result'
            # Current new runtime is necessary to revalidate this partial observation;
            # no runtime restoration/full terminal is authorized until Menu/Legacy dependencies close.
            Assert-ProGoInstallerRuntime $InstallBackup
            Check-StartupFailure ([IO.File]::ReadAllText((Join-Path $install 'ProGo.exe')) -cne 'synthetic original runtime') 'incomplete auxiliary rollback cannot silently restore runtime'
            if ($observed) {
                Check-StartupFailure (Test-ProGoInstallerStartupResult $InstallBackup.Journal.Directory $install $programs $startup) 'real durable result verifies snapshot identity ownership receipt trusted locations and actual bytes'
                Check-StartupFailure (Complete-ProGoInstallerStartupMutationSettlement $InstallBackup.Journal.Directory $install $programs $startup) 'repeat observation settles without dangerous replay'
                if ($scenario -eq 'custom-preserved') { Check-StartupFailure ([IO.File]::ReadAllText($link) -ceq 'synthetic custom shortcut') 'custom Startup never mutated' }
                else { Check-StartupFailure (-not [IO.File]::Exists($link)) 'created Startup actual original absence restored' }
            } elseif ($scenario -ne 'result-persist-fail') {
                $expectedHash = if ($null -ne $fixture.MutatedHash) { $fixture.MutatedHash } else { $fixture.CreatedHash }
                Check-StartupFailure ([IO.File]::Exists($link) -and (Get-FileHash -LiteralPath $link).Hash -ceq $expectedHash) 'unproved conflicting incomplete or unapplied admission never deletes or overwrites Startup'
            } else { Check-StartupFailure (-not [IO.File]::Exists($link) -and -not (Test-ProGoInstallerStartupResult $InstallBackup.Journal.Directory $install $programs $startup)) 'actual deletion with result persistence failure stays blocked without false evidence' }
            Check-StartupFailure (@(Get-ChildItem -LiteralPath $InstallBackup.Journal.Directory -Filter 'rolled-back.json').Count -eq 0) 'Startup observation does not publish whole rollback'
            $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
            Check-StartupFailure $blocked 'unresolved auxiliary state still blocks next automatic attempt'
            foreach ($file in Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        }
        Check-StartupFailure ($null -eq $InstallStage -or -not [IO.Directory]::Exists($InstallStage.Directory)) 'actual stage finally cleanup completes'
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer Startup failure ' + $scenario)
    }
    Microsoft.PowerShell.Utility\Write-Host ('Installer Startup failure tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $InstallStage -and [IO.Directory]::Exists($InstallStage.Directory)) { Remove-ProGoInstallerStage $InstallStage }
    $env:LOCALAPPDATA = $oldLocal
    if ($owned -and [IO.Directory]::Exists($work)) {
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Fixture cleanup refused reparse point.' } }
        if ((Get-Item -LiteralPath $work).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refused linked root.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
