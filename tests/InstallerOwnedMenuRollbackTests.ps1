Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Owned Menu rollback helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-OwnedMenu($Condition, $Name) { if (-not $Condition) { throw ('Owned Menu rollback failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-owned-menu-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $cancel = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('created', 'preserved-custom', 'preserved-absence', 'changed', 'replaced-identical',
        'missing-origin', 'delete-locked', 'locked-retry', 'cancelled', 'native-changed', 'native-replaced',
        'native-cancel', 'foreign-runtime', 'wrong-location', 'legacy-removed', 'post-startup', 'post-startup-replaced', 'unproved-startup-absence')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $programs 'ProGo\ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup, (Join-Path $programs 'ProGo') | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'preserved-custom') { [IO.File]::WriteAllText($link, 'synthetic user customization') }
        if ($scenario -eq 'legacy-removed') {
            $legacyManager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
            $legacyManager.Install($true, $true, $false)
            [IO.File]::Move($link, (Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu ($scenario.StartsWith('preserved-')) -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        Check-OwnedMenu ($scenario.StartsWith('preserved-') -or $receipt.Menu.OriginIdentity -cmatch '^[A-F0-9]{64}$') 'new creation receipt includes native object origin witness'
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable receipt'
        }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic interruption after durable receipt') }
        Check-OwnedMenu $interrupted 'actual writer leaves pending receipt before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
        $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
        foreach ($property in $saved.Completion.Startup.Menu.PSObject.Properties) { $map.Add($property.Name, $property.Value) }
        if ($scenario -in @('post-startup', 'post-startup-replaced')) {
            Check-OwnedMenu (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'real Startup rollback precedes Menu mutation'
            Check-OwnedMenu (Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup) 'Startup durable result independently verifies'
        }
        if ($scenario -eq 'unproved-startup-absence') { [IO.File]::Delete((Join-Path $startup 'ProGo.lnk')) }
        $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
        if ($scenario.StartsWith('native-')) { Check-OwnedMenu ($null -ne (Get-ProGoInstallerMenuRollbackAdmission $journal $install $programs $startup)) 'native conflict fixture first has full verified admission' }
        if ($scenario -in @('changed', 'native-changed')) { [IO.File]::WriteAllText($link, 'synthetic external edit') }
        if ($scenario -in @('replaced-identical', 'native-replaced', 'post-startup-replaced')) {
            $bytes = [IO.File]::ReadAllBytes($link)
            $creation = [IO.File]::GetCreationTimeUtc($link)
            [IO.File]::Move($link, (Join-Path $case 'retained-original.lnk'))
            [IO.File]::WriteAllBytes($link, $bytes)
            [IO.File]::SetCreationTimeUtc($link, $creation)
        }
        if ($scenario -eq 'missing-origin') {
            $saved.Completion.Startup.Menu.PSObject.Properties.Remove('OriginIdentity')
            [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress))
        }
        if ($scenario -eq 'foreign-runtime') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic foreign runtime') }
        $trustedPrograms = if ($scenario -eq 'wrong-location') { Join-Path $case 'other-Programs' } else { $programs }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $cancel = New-Object Threading.CancellationTokenSource
        if ($scenario -in @('cancelled', 'native-cancel')) { $cancel.Cancel() }
        if ($scenario -in @('delete-locked', 'locked-retry')) { $held = [IO.File]::Open($link, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read) }
        $result = $false; $cancelled = $false
        try {
            if ($scenario.StartsWith('native-')) { $result = $manager.TryRollbackDurableMenu($map, $cancel.Token) }
            else { $result = Invoke-ProGoInstallerMenuRollback $journal $install $trustedPrograms $startup $cancel.Token }
        } catch {
            $failure = $_.Exception; while ($null -ne $failure.InnerException) { $failure = $failure.InnerException }
            if ($scenario -notin @('cancelled', 'native-cancel') -or $failure -isnot [OperationCanceledException]) { throw }
            $cancelled = $true
        } finally {
            if ($null -ne $held) { $held.Dispose(); $held = $null }
            $cancel.Dispose(); $cancel = $null
        }
        Check-OwnedMenu ($cancelled -eq ($scenario -in @('cancelled', 'native-cancel'))) 'Stop cancellation propagates distinctly with owned handles settled'
        if ($scenario -eq 'locked-retry') {
            Check-OwnedMenu (-not $result -and (Get-FileHash -LiteralPath $link).Hash -ceq $before[$link]) 'delete denial retains exact bytes before explicit retry'
            $result = Invoke-ProGoInstallerMenuRollback $journal $install $programs $startup
        }
        $expected = $scenario -in @('created', 'preserved-custom', 'preserved-absence', 'locked-retry', 'legacy-removed', 'post-startup')
        Check-OwnedMenu ($result -eq $expected) 'only proven exact owned creation or unchanged preserved state succeeds'
        if ($scenario -in @('created', 'locked-retry', 'legacy-removed', 'post-startup')) {
            Check-OwnedMenu (-not [IO.File]::Exists($link) -and -not [IO.Directory]::Exists($link)) 'successful owned rollback confirms real original absence'
            $before.Remove($link)
        }
        if ($scenario -in @('created', 'locked-retry', 'legacy-removed', 'post-startup')) {
            Check-OwnedMenu (-not (Invoke-ProGoInstallerMenuRollback $journal $install $programs $startup)) 'explicit repeat refuses missing target without replay or false success'
        }
        $mutationPath = Join-Path (Split-Path -Parent $journal) ('installer-menu-admission-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        if ([IO.File]::Exists($mutationPath)) {
            $state = Get-ProGoInstallerMenuMutationState $journal $install $programs $startup
            $expectedState = if ($scenario -eq 'delete-locked') { 'not-applied' } else { 'original-observed' }
            Check-OwnedMenu ($state -ceq $expectedState) 'only exact new durable Menu admission is allowed and independently verifies'
            $before[$mutationPath] = (Get-FileHash -LiteralPath $mutationPath).Hash
        }
        # Opening receipt for exclusive write proves no reader/stream was abandoned.
        if ([IO.File]::Exists($link)) { $linkProbe = [IO.File]::Open($link, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None); $linkProbe.Dispose() }
        $probe = [IO.File]::Open($pending, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose()
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-OwnedMenu $blocked 'Menu-only result does not publish full rollback or permit retry'
        $after = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-OwnedMenu ($after.Count -eq $before.Count) 'only exactly owned Menu leaf may disappear'
        foreach ($file in $after) { Check-OwnedMenu ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'Startup Legacy runtime snapshot receipt private and foreign bytes preserved'
        }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-OwnedMenu ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign data unchanged' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer owned Menu rollback ' + $scenario)
    }
    Write-Host ('Installer owned Menu rollback tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-owned-menu-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
