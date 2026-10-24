Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer legacy evidence helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-LegacyEvidence($Condition, $Name) { if (-not $Condition) { throw ('Installer legacy evidence failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-evidence-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $workOwned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $workOwned = $true
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    Check-LegacyEvidence ([Runtime.InteropServices.Marshal]::SizeOf([ProGo.ApplicationShortcuts].GetNestedType('LegacyDisposition', [Reflection.BindingFlags]::NonPublic)) -eq 1 -and
        [Runtime.InteropServices.Marshal]::SizeOf([ProGo.ApplicationShortcuts].GetNestedType('LegacyFileInfo', [Reflection.BindingFlags]::NonPublic)) -eq 52) 'native disposition/file identity layout matches documented ABI'
    $observe = [ProGo.ApplicationShortcuts].GetMethod('ObserveLegacy', $flags)
    $delete = [ProGo.ApplicationShortcuts].GetMethod('DeleteLegacyOwned', $flags)
    foreach ($scenario in @('absent', 'removed', 'skipped-owned', 'skipped-custom', 'foreign-target', 'malformed', 'locked-retry', 'changed-admission', 'replaced-identical', 'appeared', 'replacement-changed', 'other-identity', 'other-location')) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install, $menu | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic executable; never run')
        $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
        if ($scenario -notin @('absent', 'skipped-custom', 'malformed', 'foreign-target')) {
            $manager.Install($true, $true, $false); [IO.File]::Move($primary, $legacy)
        }
        if ($scenario -in @('skipped-custom', 'malformed')) { [IO.File]::WriteAllText($legacy, 'synthetic user customization') }
        if ($scenario -eq 'foreign-target') {
            $otherRoot = Join-Path $case 'other-app'; New-Item -ItemType Directory -Path $otherRoot | Out-Null
            [IO.File]::WriteAllText((Join-Path $otherRoot 'ProGo.exe'), 'synthetic other executable')
            $other = New-Object ProGo.ApplicationShortcuts($otherRoot, $programs, $startup)
            $other.Install($true, $true, $false); [IO.File]::Move($primary, $legacy)
        }
        $before = if ([IO.File]::Exists($legacy)) { (Get-FileHash -LiteralPath $legacy).Hash } else { $null }
        if ($scenario -in @('changed-admission', 'replaced-identical')) {
            # Distinguishing experiment on the actual private admission seam, no production hook.
            $witness = $observe.Invoke($manager, [object[]]@($false))
            [byte[]]$expected = $witness.GetType().GetField('Bytes', $flags).GetValue($witness)
            $origin = $witness.GetType().GetField('Origin', $flags).GetValue($witness)
            if ($scenario -eq 'changed-admission') { [IO.File]::WriteAllText($legacy, 'synthetic changed before admission') }
            else { [IO.File]::Move($legacy, (Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($legacy, $expected) }
            $foreignHash = (Get-FileHash -LiteralPath $legacy).Hash
            $arguments = New-Object object[] 2; $arguments[0] = $expected; $arguments[1] = $origin
            $caught = $false
            try { [void]$delete.Invoke($manager, $arguments) } catch {
                $failure = $_.Exception
                while ($null -ne $failure.InnerException) { $failure = $failure.InnerException }
                $caught = $failure -is [IO.IOException] -and $failure.Message -ceq 'Legacy shortcut changed before deletion admission. It was preserved.'
            }
            Check-LegacyEvidence ($caught -and [IO.File]::Exists($legacy) -and (Get-FileHash -LiteralPath $legacy).Hash -ceq $foreignHash) 'changed bytes or replaced identical bytes fail ownership admission and are never deleted'
            Write-Host ('PASS: installer legacy evidence ' + $scenario)
            continue
        }
        $skip = $scenario -in @('skipped-owned', 'skipped-custom')
        if ($scenario -eq 'locked-retry') { $held = [IO.File]::Open($legacy, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read) }
        $receipt = $null; $caught = $false
        try { $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $true -NoStartup $true -NoStartMenu $skip -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence }
        catch {
            if ($scenario -notin @('malformed', 'locked-retry')) { throw }
            $failure = $_.Exception
            while ($null -ne $failure.InnerException) { $failure = $failure.InnerException }
            $caught = $scenario -eq 'malformed' -or $failure.Message -ceq 'Legacy deletion ownership could not be acquired.'
        }
        if ($scenario -eq 'malformed' -or $scenario -eq 'locked-retry') {
            Check-LegacyEvidence ($caught -and $null -eq $receipt -and (Get-FileHash -LiteralPath $legacy).Hash -ceq $before) 'invalid or delete-denied source never produces removal proof and retains exact bytes'
            if ($scenario -eq 'malformed') { Write-Host ('PASS: installer legacy evidence ' + $scenario); continue }
            $held.Dispose(); $held = $null
            $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $true -NoStartup $true -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        }
        $evidence = $receipt.Legacy
        $removed = $scenario -notin @('absent', 'skipped-owned', 'skipped-custom', 'foreign-target')
        Check-LegacyEvidence ($manager.VerifyLegacyInstallEvidence($evidence) -and
            $evidence.InstallIdentity -ceq (Get-ProGoInstallerIdentity $install) -and $evidence.LocationIdentity -ceq (Get-ProGoInstallerIdentity $legacy)) 'actual result proof is bound to trusted install/location and current bytes/absence'
        Check-LegacyEvidence (($evidence.Disposition -ceq 'removed') -eq $removed) 'only admitted exact source deletion claims removed ownership'
        if ($removed) {
            Check-LegacyEvidence ($evidence.BeforeMatched -and $evidence.BeforeHash -ceq $before -and $evidence.OwnedHash -ceq $before -and
                $evidence.OriginIdentity -cmatch '^[A-F0-9]{64}$' -and -not $evidence.Exists -and -not [IO.File]::Exists($legacy) -and
                $evidence.ReplacementHash -ceq $receipt.Menu.Hash) 'removal proves original object/bytes, actual absence and verified primary replacement'
        } else {
            Check-LegacyEvidence ($null -eq $evidence.OwnedHash -and $evidence.BeforeHash -ceq $evidence.Hash -and $evidence.Hash -ceq $before) 'absent or custom/foreign/skipped state is preserved, not claimed as owned'
        }
        if ($scenario -eq 'appeared') {
            [IO.File]::WriteAllText($legacy, 'synthetic foreign appearance')
            Check-LegacyEvidence (-not $manager.VerifyLegacyInstallEvidence($evidence) -and [IO.File]::ReadAllText($legacy) -ceq 'synthetic foreign appearance') 'new foreign legacy invalidates proof without deletion'
        }
        if ($scenario -eq 'replacement-changed') {
            [IO.File]::WriteAllText($primary, 'synthetic external primary edit')
            Check-LegacyEvidence (-not $manager.VerifyLegacyInstallEvidence($evidence) -and [IO.File]::ReadAllText($primary) -ceq 'synthetic external primary edit') 'changed replacement invalidates removal proof without repair'
        }
        if ($scenario -eq 'other-identity') {
            $other = New-Object ProGo.ApplicationShortcuts((Join-Path $case 'other-app'), $programs, $startup)
            Check-LegacyEvidence (-not $other.VerifyLegacyInstallEvidence($evidence)) 'wrong install identity refused'
        }
        if ($scenario -eq 'other-location') {
            $other = New-Object ProGo.ApplicationShortcuts($install, (Join-Path $case 'other-programs'), $startup)
            Check-LegacyEvidence (-not $other.VerifyLegacyInstallEvidence($evidence)) 'wrong legacy location refused'
        }
        Check-LegacyEvidence (($evidence | ConvertTo-Json -Compress) -notmatch 'synthetic|\.lnk|\.exe|--show') 'receipt contains hashes/roles, no raw path, shortcut bytes or arguments'
        Write-Host ('PASS: installer legacy evidence ' + $scenario)
    }
    $install = Join-Path $work 'durable'; New-Item -ItemType Directory -Path $install | Out-Null
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
    $writes = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
    $programs = Join-Path $work 'durable-programs'; $startup = Join-Path $work 'durable-startup'
    $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
    $manager.Install($true, $true, $false)
    [IO.File]::Move((Join-Path $programs 'ProGo\ProGo.lnk'), (Join-Path $programs 'ProGo\ProGo Status.lnk'))
    $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $true -NoStartup $true -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
    $missing = [pscustomobject]@{ Schema = 1; InstallIdentity = $receipt.InstallIdentity; Menu = $receipt.Menu; Legacy = $null }
    $caught = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $missing -RequireStartupEvidence }
    catch { $caught = $_.Exception.Message -ceq 'Installer legacy evidence identity is invalid.' }
    Check-LegacyEvidence ($caught -and -not [IO.File]::Exists((Join-Path $backup.Journal.Directory 'outcome.pending.json'))) 'missing required legacy evidence blocks before durable publication'
    function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
        $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
        throw 'synthetic interruption after durable legacy proof'
    }
    $caught = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
    catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable legacy proof') }
    Check-LegacyEvidence $caught 'actual owned writer durably retains legacy proof before terminal rename'
    $saved = Read-ProGoInstallerJson (Join-Path $backup.Journal.Directory 'outcome.pending.json')
    Check-LegacyEvidence ($saved.Identity -ceq $saved.Completion.Startup.Legacy.InstallIdentity -and
        ($saved.Completion.Startup.Legacy | ConvertTo-Json -Compress) -ceq ($receipt.Legacy | ConvertTo-Json -Compress) -and
        $saved.Completion.Ownership.Count -eq $backup.Entries.Count) 'durable origin/owned-byte receipt shares verified runtime identity/admissions/inventory'
    $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
    Check-LegacyEvidence $blocked 'all auxiliary observations still do not authorize unfinished pending-installed publication'
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-LegacyEvidence ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private/foreign bytes unchanged' }
    Write-Host 'PASS: installer legacy evidence durable'
    Write-Host 'Installer legacy evidence tests PASS: 14'
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($workOwned -and (Test-Path -LiteralPath $work)) { Remove-Item -LiteralPath $work -Recurse -Force }
}
