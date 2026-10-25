Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer restart gate does not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$boundary = @($ast.EndBlock.Statements | Where-Object {
    $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir'
})
if ($boundary.Count -ne 1) { throw 'Installer restart gate boundary is ambiguous.' }
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
# This isolated body omits preflight; supply its trusted helper dependency explicitly.
# Stop remains mocked before any payload/runtime installation.
$Payload = [pscustomobject]@{ ScriptsDirectory = (Join-Path $Root 'scripts') }
function Check-RestartGate($Condition, $Name) { if (-not $Condition) { throw ('Installer restart gate failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-restart-tests-' + [Guid]::NewGuid().ToString('N'))
$oldLocal = $env:LOCALAPPDATA
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    foreach ($scenario in @('no-parent', 'ordinary', 'empty', 'intent', 'corrupt', 'schema', 'file', 'malformed')) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'ProGo'
        New-Item -ItemType Directory -Path $install | Out-Null
        $env:LOCALAPPDATA = $case
        foreach ($leaf in @('ProGo.exe', 'settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) {
            [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic unchanged bytes')
        }
        $pending = $scenario -notin @('no-parent', 'ordinary')
        if ($scenario -ne 'no-parent') {
            $parent = Join-Path $install 'backups'; New-Item -ItemType Directory -Path $parent | Out-Null
            if ($scenario -eq 'ordinary') {
                New-Item -ItemType Directory -Path (Join-Path $parent 'ordinary-backup') | Out-Null
                [IO.File]::WriteAllText((Join-Path $parent 'ordinary-backup\manifest.json'), 'synthetic ordinary backup')
            } else {
                $name = if ($scenario -eq 'malformed') { 'INSTALLER-JOURNAL-unknown' } else { 'installer-journal-' + [Guid]::NewGuid().ToString('N') }
                $journal = Join-Path $parent $name
                if ($scenario -eq 'file') { [IO.File]::WriteAllText($journal, 'synthetic journal-shaped file') }
                else {
                    New-Item -ItemType Directory -Path $journal | Out-Null
                    if ($scenario -in @('intent', 'schema', 'corrupt')) {
                        $text = if ($scenario -eq 'intent') { '{"Schema":1,"Phase":"admitted","Sequence":0}' } elseif ($scenario -eq 'schema') { '{"Schema":999,"Phase":"complete"}' } else { '{broken' }
                        [IO.File]::WriteAllText((Join-Path $journal 'admit-000000.json'), $text)
                    }
                }
            }
        }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $install -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $fixture = @{ Stops = 0; Disposes = 0; Launches = 0; Success = $false }
        function Enter-ProGoInstallerMaintenance {
            $lease = [pscustomobject]@{ State = $fixture }
            $lease | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.State.Disposes++ }
            return $lease
        }
        function Stop-ProGoInstallerApplication($InstallDirectory) { $fixture.Stops++; throw 'synthetic stop reached after gate' }
        function Start-Process { $fixture.Launches++; throw 'unexpected launch' }
        function Write-Host($Object) { if ($Object -like 'Install OK:*') { $fixture.Success = $true } }
        $reason = $null
        try { & $body } catch { $reason = $_.Exception.Message }
        $expected = if ($pending) { 'Installer pending journal requires verified recovery before a new installation.' } else { 'synthetic stop reached after gate' }
        Check-RestartGate ($reason -ceq $expected -and $fixture.Stops -eq [int](-not $pending) -and
            $fixture.Disposes -eq 1 -and $fixture.Launches -eq 0 -and -not $fixture.Success) 'actual installer gate precedes Stop and releases maintenance without success/Launch'
        if ($pending) {
            $refused = $false
            try { New-ProGoInstallerJournal ([pscustomobject]@{ InstallDirectory = $install }) | Out-Null }
            catch { $refused = $_.Exception.Message -ceq $expected }
            Check-RestartGate $refused 'direct new journal cannot bypass restart detection'
        }
        $after = @(Get-ChildItem -LiteralPath $install -Recurse -File)
        Check-RestartGate ($after.Count -eq $before.Count) 'no runtime/journal/private/foreign leaf created or deleted'
        foreach ($file in $after) { Check-RestartGate ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all existing bytes unchanged' }
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer restart gate ' + $scenario)
    }
    Microsoft.PowerShell.Utility\Write-Host 'Installer restart gate tests PASS: 8'
} finally {
    $env:LOCALAPPDATA = $oldLocal
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
