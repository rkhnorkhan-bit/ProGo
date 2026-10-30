Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer auxiliary helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-Auxiliary($Condition, $Name) { if (-not $Condition) { throw ('Installer auxiliary revalidation failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-aux-revalidation-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('created', 'custom-preserved', 'legacy-removed', 'missing-receipt', 'schema', 'unknown-field',
        'boolean-type', 'install-identity', 'startup-location', 'menu-location', 'legacy-location', 'ownership',
        'startup-changed', 'menu-changed', 'legacy-appeared', 'runtime-changed', 'snapshot-changed',
        'admission-changed', 'conflict', 'other-pending', 'trusted-location', 'invalid-json', 'oversized', 'receipt-busy')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        New-Item -ItemType Directory -Path $install, $programs, $startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'custom-preserved') { [IO.File]::WriteAllText((Join-Path $startup 'ProGo.lnk'), 'synthetic custom shortcut') }
        if ($scenario -eq 'legacy-removed') {
            $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
            $manager.Install($true, $true, $false)
            [IO.File]::Move((Join-Path $programs 'ProGo\ProGo.lnk'), (Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup ($scenario -eq 'custom-preserved') -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable auxiliary proof'
        }
        $caught = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable auxiliary proof') }
        Check-Auxiliary $caught 'actual writer stopped after durable receipt before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
        if ($scenario -eq 'missing-receipt') { $saved.Completion.Startup = $null }
        if ($scenario -eq 'schema') { $saved.Completion.Startup.Schema = 2 }
        if ($scenario -eq 'unknown-field') { $saved.Completion.Startup | Add-Member NoteProperty Path 'synthetic untrusted location' }
        if ($scenario -eq 'boolean-type') { $saved.Completion.Startup.Exists = 'true' }
        if ($scenario -eq 'install-identity') { $saved.Completion.Startup.InstallIdentity = ('0' * 64) }
        if ($scenario -eq 'startup-location') { $saved.Completion.Startup.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'menu-location') { $saved.Completion.Startup.Menu.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'legacy-location') { $saved.Completion.Startup.Legacy.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'ownership') { $saved.Completion.Startup.Menu.Disposition = 'foreign' }
        [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress))
        if ($scenario -eq 'startup-changed') { [IO.File]::WriteAllText((Join-Path $startup 'ProGo.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'menu-changed') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'legacy-appeared') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo Status.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'runtime-changed') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic external bytes') }
        if ($scenario -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic external bytes') }
        if ($scenario -eq 'admission-changed') { [IO.File]::WriteAllText((Join-Path $journal 'admit-000000.json'), '{}') }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText((Join-Path $journal 'installed.json'), '{}') }
        if ($scenario -eq 'other-pending') { New-Item -ItemType Directory -Path (Join-Path (Split-Path -Parent $journal) ('installer-journal-' + [Guid]::NewGuid().ToString('N'))) | Out-Null }
        if ($scenario -eq 'invalid-json') { [IO.File]::WriteAllText($pending, '{') }
        if ($scenario -eq 'oversized') { [IO.File]::WriteAllText($pending, (' ' * 1048577)) }
        $expectedPrograms = if ($scenario -eq 'trusted-location') { Join-Path $case 'other-Programs' } else { $programs }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $expected = $scenario -in @('created', 'custom-preserved', 'legacy-removed')
        if ($scenario -eq 'receipt-busy') { $held = [IO.File]::Open($pending, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite) }
        try { $actual = Test-ProGoInstallerPendingAuxiliaryProof $journal $install $expectedPrograms $startup }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
        Check-Auxiliary ($actual -eq $expected) 'only complete independently located and current auxiliary/runtime proof is accepted'
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-Auxiliary $blocked 'positive revalidation never publishes metadata or authorizes retry'
        $after = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-Auxiliary ($after.Count -eq $before.Count) 'read-only revalidation preserves file count'
        foreach ($file in $after) { Check-Auxiliary ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all runtime/auxiliary/private/foreign/evidence bytes preserved' }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-Auxiliary ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign files unchanged' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer auxiliary revalidation ' + $scenario)
    }
    Write-Host ('Installer auxiliary revalidation tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-aux-revalidation-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
