Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer rollback helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-rollback-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
function Check-InstallerRollback($Condition, $Name) {
    if (-not $Condition) { throw ('Installer rollback test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($mode in @('complete', 'partial', 'restore-fail', 'restore-corrupt', 'snapshot-corrupt', 'foreign-change', 'unadmitted', 'fresh')) {
        $install = Join-Path $work $mode; New-Item -ItemType Directory -Path $install | Out-Null
        foreach ($name in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) {
            [IO.File]::WriteAllText((Join-Path $install $name), 'synthetic private or foreign bytes')
        }
        $target = Join-Path $install 'ProGo.exe'
        if ($mode -ne 'fresh') { [IO.File]::WriteAllText($target, 'synthetic prior runtime') }
        $backup = New-ProGoInstallerBackup $payload $install
        $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
            $probe = $null; $exclusive = $false
            try { $probe = [IO.File]::Open($Target.Name, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite) }
            catch [IO.IOException] { $exclusive = $true }
            finally { if ($null -ne $probe) { $probe.Dispose() } }
            Check-InstallerRollback $exclusive 'target is exclusively owned while recording partial bytes'
            $Target.SetLength(0)
            if ($mode -eq 'partial') {
                $Target.WriteByte(42)
                throw 'synthetic partial write failure'
            }
            $Source.CopyTo($Target); $Target.Flush($true)
        }
        function Copy-ProGoInstallerRestoreFile([string]$Source, [string]$Destination) {
            if ($mode -eq 'restore-fail') { throw 'synthetic restore copy failure' }
            if ($mode -eq 'restore-corrupt') { [IO.File]::WriteAllText($Destination, 'incorrect restored bytes'); return }
            [IO.File]::Copy($Source, $Destination, $true)
        }
        $entry = @($backup.Entries | Where-Object { $_.Relative -eq 'ProGo.exe' })[0]
        $writeFailed = $false
        try { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes } catch {
            if ($mode -ne 'partial' -or -not $_.Exception.Message.Contains('synthetic partial write failure')) { throw }
            $writeFailed = $true
        }
        Check-InstallerRollback ($writeFailed -eq ($mode -eq 'partial') -and $writes.Count -eq 1 -and
            $writes[0].OwnedHash -ceq (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) 'real written/partial bytes retained in admission'
        if ($mode -eq 'snapshot-corrupt') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'damaged snapshot') }
        if ($mode -eq 'foreign-change') { [IO.File]::WriteAllText($target, 'externally changed runtime') }
        if ($mode -eq 'unadmitted') { [IO.File]::WriteAllText((Join-Path $install 'VERSION'), 'foreign new leaf') }
        $beforeRollback = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $restored = $false
        try { Restore-ProGoInstallerRuntime $backup $writes; $restored = $true } catch { }
        $expected = $mode -in @('complete', 'partial', 'fresh')
        Check-InstallerRollback ($restored -eq $expected) 'rollback success requires full old-byte and absence proof'
        if ($expected) {
            Assert-ProGoInstallerRuntime $backup -Original
            if ($mode -eq 'fresh') { Check-InstallerRollback (-not [IO.File]::Exists($target)) 'remove only own new leaf' }
            else { Check-InstallerRollback ([IO.File]::ReadAllText($target) -ceq 'synthetic prior runtime') 'exact restored bytes' }
        }
        if ($mode -in @('snapshot-corrupt', 'foreign-change')) {
            Check-InstallerRollback ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ceq $beforeRollback) 'invalid material/changed ownership never overwritten'
        }
        if ($mode -eq 'unadmitted') { Check-InstallerRollback ([IO.File]::ReadAllText((Join-Path $install 'VERSION')) -ceq 'foreign new leaf') 'unadmitted runtime not removed'
        }
        foreach ($name in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) {
            Check-InstallerRollback ([IO.File]::ReadAllText((Join-Path $install $name)) -ceq 'synthetic private or foreign bytes') 'private and unrelated bytes preserved'
        }
        if ($null -ne $backup.Directory) { Check-InstallerRollback ([IO.Directory]::Exists($backup.Directory)) 'verified snapshot retained' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer rollback ' + $mode)
    }
    Write-Host 'Installer rollback tests PASS: 8'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
