Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer startup evidence helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-StartupEvidence($Condition, $Name) { if (-not $Condition) { throw ('Installer startup evidence failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-startup-evidence-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    foreach ($scenario in @('created', 'existing-absent', 'existing-custom', 'existing-owned', 'fresh-owned', 'no-startup-absent', 'no-startup-custom', 'conflict', 'changed', 'appeared', 'other-identity', 'other-location')) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        New-Item -ItemType Directory -Path $install, $startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic executable; never run')
        $link = Join-Path $startup 'ProGo.lnk'
        $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
        if ($scenario -in @('existing-owned', 'fresh-owned')) { $manager.Install($false, $false, $true) }
        if ($scenario -in @('existing-custom', 'no-startup-custom', 'conflict')) { [IO.File]::WriteAllText($link, 'synthetic user customization') }
        $before = if ([IO.File]::Exists($link)) { (Get-FileHash -LiteralPath $link).Hash } else { $null }
        $wasInstalled = $scenario.StartsWith('existing-')
        $noStartup = $scenario.StartsWith('no-startup-') -or $scenario -eq 'appeared'
        $caught = $false; $evidence = $null
        try { $evidence = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $wasInstalled -NoStartup $noStartup -NoStartMenu $true -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence }
        catch { if ($scenario -ne 'conflict') { throw }; $caught = $true }
        if ($scenario -eq 'conflict') {
            Check-StartupEvidence ($caught -and $null -eq $evidence -and (Get-FileHash -LiteralPath $link).Hash -ceq $before) 'foreign fresh collision is refused without overwriting it'
        } else {
            Check-StartupEvidence ($evidence.Schema -eq 1 -and $manager.VerifyStartupInstallEvidence($evidence) -and
                $evidence.InstallIdentity -ceq (Get-ProGoInstallerIdentity $install) -and $evidence.LocationIdentity -ceq (Get-ProGoInstallerIdentity $link)) 'actual wrapper verifies bytes and binds expected install/location identities'
            $created = $scenario -in @('created', 'changed', 'other-identity', 'other-location')
            Check-StartupEvidence (($evidence.Disposition -ceq 'created') -eq $created) 'only actual newly written bytes claim creation ownership'
            if (-not $created) {
                Check-StartupEvidence ($evidence.Disposition -ceq 'preserved' -and $evidence.BeforeHash -ceq $evidence.Hash -and $evidence.Hash -ceq $before) 'existing customized/disabled state or absence is preserved exactly'
            }
            if ($scenario -eq 'changed') { [IO.File]::WriteAllText($link, 'synthetic external edit'); Check-StartupEvidence (-not $manager.VerifyStartupInstallEvidence($evidence)) 'external edit invalidates receipt without repair'; Check-StartupEvidence ([IO.File]::ReadAllText($link) -ceq 'synthetic external edit') 'external edited bytes untouched' }
            if ($scenario -eq 'appeared') { [IO.File]::WriteAllText($link, 'synthetic external creation'); Check-StartupEvidence (-not $manager.VerifyStartupInstallEvidence($evidence)) 'foreign file replacing recorded absence invalidates receipt'; Check-StartupEvidence ([IO.File]::ReadAllText($link) -ceq 'synthetic external creation') 'external appeared bytes untouched' }
            if ($scenario -eq 'other-identity') {
                $other = New-Object ProGo.ApplicationShortcuts((Join-Path $case 'other-app'), $programs, $startup)
                Check-StartupEvidence (-not $other.VerifyStartupInstallEvidence($evidence)) 'receipt cannot authorize another installation'
            }
            if ($scenario -eq 'other-location') {
                $other = New-Object ProGo.ApplicationShortcuts($install, $programs, (Join-Path $case 'other-startup'))
                Check-StartupEvidence (-not $other.VerifyStartupInstallEvidence($evidence)) 'receipt cannot authorize another startup location'
            }
            $json = $evidence | ConvertTo-Json -Depth 4 -Compress
            Check-StartupEvidence (-not $json.Contains($case.Replace('\', '\\')) -and $json -notmatch 'synthetic|\.lnk|\.exe') 'durable receipt contains no raw path, shortcut content or executable arguments'
        }
        Write-Host ('PASS: installer startup evidence ' + $scenario)
    }
    # Actual durable writer round trip: phase record and Startup receipt share one inventory,
    # identity, snapshot/admission proof and immutable pending outcome payload.
    $install = Join-Path $work 'durable'; New-Item -ItemType Directory -Path $install | Out-Null
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
    $writes = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
    $missing = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -RequireStartupEvidence }
    catch { $missing = $_.Exception.Message -ceq 'Installer startup completion evidence is missing.' }
    Check-StartupEvidence ($missing -and -not [IO.File]::Exists((Join-Path $backup.Journal.Directory 'outcome.pending.json'))) 'required auxiliary receipt cannot be omitted before terminal publication'
    $wrongIdentity = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $evidence -RequireStartupEvidence }
    catch { $wrongIdentity = $_.Exception.Message -ceq 'Installer startup evidence identity is invalid.' }
    Check-StartupEvidence ($wrongIdentity -and -not [IO.File]::Exists((Join-Path $backup.Journal.Directory 'outcome.pending.json'))) 'foreign receipt identity is refused before durable outcome creation'
    $evidence = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu $true -ProgramsDirectory (Join-Path $work 'durable-programs') -StartupDirectory (Join-Path $work 'durable-startup') -CaptureStartupEvidence
    function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
        $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
        throw 'synthetic interruption after durable startup proof'
    }
    $caught = $false
    try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $evidence -RequireStartupEvidence }
    catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable startup proof') }
    Check-StartupEvidence $caught 'actual writer reaches durable pending proof, never installed publication'
    $saved = Read-ProGoInstallerJson (Join-Path $backup.Journal.Directory 'outcome.pending.json')
    Check-StartupEvidence ($saved.Identity -ceq $saved.Completion.Startup.InstallIdentity -and
        ($saved.Completion.Startup | ConvertTo-Json -Compress) -ceq ($evidence | ConvertTo-Json -Compress) -and
        $saved.Completion.Ownership.Count -eq $backup.Entries.Count) 'persisted receipt retains exact actual hashes and identity alongside full runtime ownership'
    $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
    Check-StartupEvidence $blocked 'Startup-only proof does not authorize unfinished pending-installed publication'
    foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-StartupEvidence ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private/foreign data unchanged' }
    Write-Host 'PASS: installer startup evidence durable'
    Write-Host 'Installer startup evidence tests PASS: 13'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
