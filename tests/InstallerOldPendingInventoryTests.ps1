Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer outcome helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-PendingProof($Condition, $Name) { if (-not $Condition) { throw ('Installer outcome test failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-pending-proof-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $currentNames = ${function:Get-ProGoInstallerPayloadNames}
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($scenario in @('old-inventory')) {
        $install = Join-Path $work $scenario; New-Item -ItemType Directory -Path $install | Out-Null
        if ($scenario -ne 'fresh') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable') }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'old-inventory') {
            function Get-ProGoInstallerPayloadNames { & $currentNames | Where-Object { $_ -cne 'scripts/Layout-ProGo.ps1' } }
            $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable proof'
        }
        $caught = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') }
        catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable proof') }
        Check-PendingProof $caught 'actual writer stopped after durable proof before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
        if ($scenario -eq 'legacy') { $saved.PSObject.Properties.Remove('Completion') }
        if ($scenario -eq 'phases') { $saved.Completion.Phases = @('self-check') }
        if ($scenario -eq 'ownership') { $saved.Completion.Ownership[0].OwnedHash = ('0' * 64) }
        if ($scenario -eq 'inventory') { $saved.Records = @($saved.Records | Select-Object -Skip 1) }
        if ($scenario -eq 'identity') { $saved.Identity = ('0' * 64) }
        [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress))
        if ($scenario -eq 'foreign-runtime') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic foreign bytes') }
        if ($scenario -eq 'snapshot-corrupt') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic damaged snapshot') }
        if ($scenario -eq 'journal-corrupt') { [IO.File]::WriteAllText((Join-Path $journal 'admit-000000.json'), '{}') }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText((Join-Path $journal 'installed.json'), '{}') }
        if ($scenario -eq 'old-inventory') {
            Check-PendingProof (Test-ProGoInstallerPendingInstalledProof $journal $install) 'old inventory proof valid under old exact allowlist'
            Set-Item -Path Function:Get-ProGoInstallerPayloadNames -Value $currentNames
        }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $install -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        Check-PendingProof ((Test-ProGoInstallerPendingInstalledProof $journal $install) -eq ($scenario -in @('complete', 'fresh'))) 'only full phase/ownership/admission/snapshot/current-byte proof accepted'
        $blocked = $false
        $reason = $null
        try { Resolve-ProGoInstallerPendingInstalled $install -ProgramsDirectory (Join-Path $work 'programs') -StartupDirectory (Join-Path $work 'startup') }
        catch { $reason = $_.Exception.Message }
        Check-PendingProof ($reason -ceq 'Installer pending journal requires verified recovery before a new installation.') 'old inventory refuses reconciliation before any write'
        try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-PendingProof $blocked 'even valid proof alone does not authorize pending publication or a new installation'
        $after = @(Get-ChildItem -LiteralPath $install -Recurse -File)
        Check-PendingProof ($after.Count -eq $before.Count) 'read-only proof preserves file count'
        foreach ($file in $after) { Check-PendingProof ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all runtime/private/foreign/evidence bytes untouched' }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-PendingProof ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private/foreign bytes unchanged' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer pending proof ' + $scenario)
    }
    Write-Host 'Installer old pending inventory test PASS: 1'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
