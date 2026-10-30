Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer journal helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-Journal($Condition, $Name) { if (-not $Condition) { throw ('Installer journal test failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-journal-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($scenario in @('existing', 'fresh', 'persist-fail', 'short-write', 'unknown', 'invalid', 'tampered', 'partial')) {
        $install = Join-Path $work $scenario; New-Item -ItemType Directory -Path $install | Out-Null
        $target = Join-Path $install 'ProGo.exe'
        $oldExists = $scenario -ne 'fresh'
        if ($oldExists) { [IO.File]::WriteAllText($target, 'synthetic previous runtime') }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) {
            [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes')
        }
        $backup = New-ProGoInstallerBackup $payload $install
        $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        $journalFixture = @{ Scenario = $scenario; Mutations = 0; Persisted = 0 }
        function Write-ProGoInstallerJournalBytes($Stream, [byte[]]$Bytes) {
            Check-Journal ($writes.Count -eq 0 -and $journalFixture.Mutations -eq 0) 'persist before runtime admission/mutation'
            Check-Journal ((-not $oldExists -and -not [IO.File]::Exists($target)) -or
                ($oldExists -and [IO.File]::ReadAllText($target) -ceq 'synthetic previous runtime')) 'old bytes or absence before persistence'
            if ($journalFixture.Scenario -eq 'persist-fail') { throw 'synthetic journal persistence failure' }
            $length = if ($journalFixture.Scenario -eq 'short-write') { 1 } else { $Bytes.Length }
            $Stream.Write($Bytes, 0, $length); $Stream.Flush($true)
            $journalFixture.Persisted++
        }
        function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
            Check-Journal ($journalFixture.Persisted -eq 1 -and $backup.Journal.Records.Count -eq 1) 'confirmed journal before actual runtime bytes'
            # The admission handle remains pinned against deletion/write during runtime mutation.
            $pin = $null; $blocked = $false
            try { $pin = [IO.File]::Open((Join-Path $backup.Journal.Directory 'admit-000000.json'), [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite) }
            catch [IO.IOException] { $blocked = $true }
            finally { if ($null -ne $pin) { $pin.Dispose() } }
            Check-Journal $blocked 'admission pinned during runtime write'
            $journalFixture.Mutations++; $Target.SetLength(0)
            if ($journalFixture.Scenario -eq 'partial') { $Target.WriteByte(42); throw 'synthetic journaled partial write' }
            $Source.CopyTo($Target); $Target.Flush($true)
        }
        $backup | Add-Member -MemberType NoteProperty -Name Journal -Value (New-ProGoInstallerJournal $backup)
        if ($scenario -eq 'unknown') { [IO.File]::WriteAllText((Join-Path $backup.Journal.Directory 'unknown.json'), '{}') }
        if ($scenario -eq 'invalid') { $backup.Journal.Inventory = '{"Schema":999}' }
        if ($scenario -eq 'tampered') {
            $name = 'admit-000000.json'
            $backup.Journal.Records.Add([pscustomobject]@{ Name = $name; Text = 'expected synthetic record' })
            [IO.File]::WriteAllText((Join-Path $backup.Journal.Directory $name), 'tampered synthetic record')
        }
        $entry = @($backup.Entries | Where-Object { $_.Relative -eq 'ProGo.exe' })[0]
        $caught = $false
        try { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes } catch { $caught = $true }
        $allowed = $scenario -in @('existing', 'fresh', 'partial')
        Check-Journal ($caught -eq ($scenario -notin @('existing', 'fresh')) -and
            $journalFixture.Mutations -eq [int]$allowed) 'fail closed without runtime write for unconfirmed/unknown/invalid journal'
        if (-not $allowed) {
            Check-Journal ($writes.Count -eq 0 -and [IO.File]::ReadAllText($target) -ceq 'synthetic previous runtime') 'journal refusal preserves exact old bytes'
        } else {
            Check-Journal ($writes.Count -eq 1 -and $writes[0].OwnedHash -ceq (Get-FileHash -LiteralPath $target).Hash) 'journal retains actual owned full/partial bytes'
            if ($scenario -eq 'partial') {
                $partial = [IO.File]::ReadAllBytes($target)
                Check-Journal ($partial.Length -eq 1 -and $partial[0] -eq 42) 'actual partial byte preserved'
            }
            Assert-ProGoInstallerJournal $backup.Journal $backup
            Restore-ProGoInstallerRuntime $backup $writes
            Assert-ProGoInstallerRuntime $backup -Original
            Check-Journal ([IO.File]::Exists((Join-Path $backup.Journal.Directory 'admit-000000.json'))) 'intent retained after verified rollback'
        }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) {
            Check-Journal ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign bytes preserved'
        }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer journal ' + $scenario)
    }
    Write-Host 'Installer journal tests PASS: 8'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
