param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$work = Join-Path $env:TEMP ('ProGo-log-helper-' + [guid]::NewGuid().ToString('N'))
$checks = 0
function Check($Condition, $Name) {
    if (-not $Condition) { throw $Name }
    $script:checks++; Write-Host ('PASS: ' + $Name)
}
try {
    . (Join-Path $Scripts 'Log-ProGo.ps1')
    Check (-not (Test-Path $work)) 'loading log definitions has no filesystem side effects'
    $InstallDir = $work
    $UpdateLog = Join-Path $work 'update.log'
    $LegacyUpdateLog = Join-Path $work 'progo-update.log'
    $RestoreLog = Join-Path $work 'progo-restore.log'
    # Exercise the shipped writer functions without running installer actions.
    foreach ($name in @('Update-ProGo.Core.ps1', 'Restore-ProGoBackup.ps1')) {
        $tokens = $null; $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts $name), [ref]$tokens, [ref]$errors)
        if ($errors.Count) { throw 'Helper parse failure' }
        foreach ($fn in $ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in @('Write-UpdateLog','Write-RestoreLog') }, $false)) {
            . ([scriptblock]::Create($fn.Extent.Text))
        }
    }
    Write-UpdateLog 'first update'
    Check ((Get-Content -Raw $UpdateLog).Contains('first update')) 'installed updater uses shared log implementation'
    Check (-not (Test-Path $LegacyUpdateLog)) 'new updates do not create the duplicate legacy log'
    [IO.File]::WriteAllText($LegacyUpdateLog, 'legacy fixture')
    $file = [IO.File]::OpenWrite($UpdateLog)
    try { $file.SetLength(2 * [ProGo.BoundedLog]::MaxFileBytes) } finally { $file.Dispose() }
    Write-UpdateLog 'next update'
    Check ((Get-Item $UpdateLog).Length -lt 1024 -and (Get-Item ($UpdateLog + '.1')).Length -le [ProGo.BoundedLog]::MaxFileBytes) 'real updater writer rotates historical oversized log'
    Check ([IO.File]::ReadAllText($LegacyUpdateLog) -eq 'legacy fixture') 'updater leaves existing legacy log unchanged'
    $locked = [IO.File]::Open($UpdateLog, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try { Write-UpdateLog 'locked update' } finally { $locked.Dispose() }
    Check (-not ([IO.File]::ReadAllText($UpdateLog).Contains('locked update'))) 'blocked logging does not throw or append through a fallback'
    Write-RestoreLog 'restore fixture'
    Check ([IO.File]::ReadAllText($RestoreLog).Contains('restore fixture')) 'restore helper uses same bounded policy'

    # Real bootstrap failure with a locked log must preserve its original error.
    $bootstrap = Join-Path $work 'bootstrap'
    New-Item -ItemType Directory $bootstrap | Out-Null
    foreach ($name in @('Update-ProGo.ps1','Maintenance-ProGo.ps1','MaintenanceOperation.cs','Log-ProGo.ps1','BoundedLog.cs')) {
        Copy-Item (Join-Path $Scripts $name) $bootstrap
    }
    $app = Join-Path $work 'ProGo'; New-Item -ItemType Directory $app | Out-Null
    $locked = [IO.File]::Open((Join-Path $app 'update.log'), [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $process = $null
    try {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $PSHOME 'powershell.exe'
        $info.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $bootstrap 'Update-ProGo.ps1') + '" -NoLaunch'
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.EnvironmentVariables['LOCALAPPDATA'] = $work
        $process = [Diagnostics.Process]::Start($info)
        Check ($process.WaitForExit(20000)) 'bootstrap exits despite locked error log'
        $errorText = $process.StandardError.ReadToEnd()
        Check ($process.ExitCode -ne 0 -and $errorText.Contains('Installed updater is missing')) 'bootstrap preserves original failure when log cannot be written'
    } finally {
        if ($null -ne $process) {
            if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
            $process.Dispose()
        }
        $locked.Dispose()
    }
    Write-Host ('Log helper tests PASS: ' + $checks)
} finally { if (Test-Path $work) { Remove-Item -LiteralPath $work -Recurse -Force } }
