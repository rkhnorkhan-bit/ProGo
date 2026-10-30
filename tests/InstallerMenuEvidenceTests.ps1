Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer menu evidence helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-MenuEvidence($Condition, $Name) { if (-not $Condition) { throw ('Installer menu evidence failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-evidence-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    foreach ($scenario in @('created', 'existing-owned', 'skipped-absent', 'skipped-custom', 'foreign', 'foreign-target', 'changed', 'appeared', 'removed', 'other-identity', 'other-location', 'directory-conflict', 'oversized')) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $menu | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic executable; never run')
        $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
        if ($scenario -eq 'existing-owned') { $manager.Install($true, $true, $false) }
        if ($scenario -in @('foreign', 'skipped-custom')) { [IO.File]::WriteAllText($primary, 'synthetic foreign customization') }
        if ($scenario -eq 'foreign-target') {
            $otherRoot = Join-Path $case 'other-app'; New-Item -ItemType Directory -Path $otherRoot | Out-Null
            [IO.File]::WriteAllText((Join-Path $otherRoot 'ProGo.exe'), 'synthetic other executable')
            $other = New-Object ProGo.ApplicationShortcuts($otherRoot, $programs, $startup)
            $other.Install($true, $true, $false)
        }
        if ($scenario -eq 'directory-conflict') { New-Item -ItemType Directory -Path $primary | Out-Null }
        if ($scenario -eq 'oversized') { [IO.File]::WriteAllBytes($primary, (New-Object byte[] 65537)) }
        $before = if ([IO.File]::Exists($primary)) { (Get-FileHash -LiteralPath $primary).Hash } else { $null }
        $skip = $scenario -in @('skipped-absent', 'skipped-custom', 'appeared', 'oversized')
        $refused = $scenario -in @('foreign', 'foreign-target', 'directory-conflict', 'oversized')
        $caught = $false; $receipt = $null
        try { $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $true -NoStartup $true -NoStartMenu $skip -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence }
        catch { if (-not $refused) { throw }; $caught = $true }
        if ($refused) {
            Check-MenuEvidence ($caught -and $null -eq $receipt) 'foreign/invalid state cannot produce accepted evidence'
            if ($scenario -eq 'directory-conflict') { Check-MenuEvidence ([IO.Directory]::Exists($primary)) 'foreign directory remains untouched' }
            else { Check-MenuEvidence ((Get-FileHash -LiteralPath $primary).Hash -ceq $before) 'foreign/custom/oversized bytes preserved exactly' }
        } else {
            $evidence = $receipt.Menu
            Check-MenuEvidence ($evidence.Schema -eq 1 -and $manager.VerifyMenuInstallEvidence($evidence) -and
                $evidence.InstallIdentity -ceq (Get-ProGoInstallerIdentity $install) -and
                $evidence.LocationIdentity -ceq (Get-ProGoInstallerIdentity $primary) -and $evidence.Requested -eq (-not $skip)) 'real adapter binds primary result to expected install/location and actual request'
            $created = -not $skip -and $scenario -ne 'existing-owned'
            Check-MenuEvidence (($evidence.Disposition -ceq 'created') -eq $created) 'only this EnsureLink result claims creation ownership'
            if (-not $created) { Check-MenuEvidence ($evidence.Disposition -ceq 'preserved' -and $evidence.BeforeHash -ceq $before -and $evidence.Hash -ceq $before) 'existing or skipped state is observed, never claimed as newly owned' }
            if ($scenario -in @('changed', 'appeared')) {
                [IO.File]::WriteAllText($primary, 'synthetic external change')
                Check-MenuEvidence (-not $manager.VerifyMenuInstallEvidence($evidence)) 'changed or newly appeared foreign bytes invalidate evidence'
                Check-MenuEvidence ([IO.File]::ReadAllText($primary) -ceq 'synthetic external change') 'verification does not repair or overwrite foreign bytes'
            }
            if ($scenario -eq 'removed') { [IO.File]::Delete($primary); Check-MenuEvidence (-not $manager.VerifyMenuInstallEvidence($evidence) -and -not [IO.File]::Exists($primary)) 'missing result invalidates evidence without recreation' }
            if ($scenario -eq 'other-identity') {
                $other = New-Object ProGo.ApplicationShortcuts((Join-Path $case 'other-app'), $programs, $startup)
                Check-MenuEvidence (-not $other.VerifyMenuInstallEvidence($evidence)) 'receipt cannot authorize a different install identity'
            }
            if ($scenario -eq 'other-location') {
                $other = New-Object ProGo.ApplicationShortcuts($install, (Join-Path $case 'other-programs'), $startup)
                Check-MenuEvidence (-not $other.VerifyMenuInstallEvidence($evidence)) 'receipt cannot authorize another Programs location'
            }
            $json = $evidence | ConvertTo-Json -Compress
            Check-MenuEvidence ($json -notmatch 'synthetic|\.lnk|\.exe|--show') 'receipt stores no raw path, shortcut bytes or arguments'
        }
        Write-Host ('PASS: installer menu evidence ' + $scenario)
    }
    $install = Join-Path $work 'durable'; New-Item -ItemType Directory -Path $install | Out-Null
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
    $writes = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
    $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $true -NoStartup $true -NoStartMenu $false -ProgramsDirectory (Join-Path $work 'durable-programs') -StartupDirectory (Join-Path $work 'durable-startup') -CaptureStartupEvidence
    $missingMenu = [pscustomobject]@{ Schema = 1; InstallIdentity = $receipt.InstallIdentity; Menu = $null }
    $caught = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $missingMenu -RequireStartupEvidence }
    catch { $caught = $_.Exception.Message -ceq 'Installer menu evidence identity is invalid.' }
    Check-MenuEvidence ($caught -and -not [IO.File]::Exists((Join-Path $backup.Journal.Directory 'outcome.pending.json'))) 'production requirement rejects missing Menu proof before metadata publication'
    function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
        $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
        throw 'synthetic interruption after durable menu proof'
    }
    $caught = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
    catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable menu proof') }
    Check-MenuEvidence $caught 'actual owned writer durably retains receipt without installed publication'
    $saved = Read-ProGoInstallerJson (Join-Path $backup.Journal.Directory 'outcome.pending.json')
    Check-MenuEvidence ($saved.Identity -ceq $saved.Completion.Startup.Menu.InstallIdentity -and
        ($saved.Completion.Startup.Menu | ConvertTo-Json -Compress) -ceq ($receipt.Menu | ConvertTo-Json -Compress) -and
        $saved.Completion.Ownership.Count -eq $backup.Entries.Count) 'saved actual primary hashes are tied to full runtime/admission identity and ownership'
    $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
    Check-MenuEvidence $blocked 'primary plus Startup proof alone never unblocks pending-installed publication'
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-MenuEvidence ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private/foreign bytes unchanged' }
    Write-Host 'PASS: installer menu evidence durable'
    Write-Host 'Installer menu evidence tests PASS: 14'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
