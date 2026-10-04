param([string]$Mode, [string]$Scripts, [string]$Fixture, [string]$Release)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Requires isolated Windows CI' }
. (Join-Path $Scripts 'Maintenance-ProGo.ps1')
if ($Mode -eq 'hold') {
    $lease = [ProGo.MaintenanceOperation]::Enter()
    try { Set-Content (Join-Path $Fixture 'held') $PID; Start-Sleep -Seconds 30 } finally { $lease.Dispose() }
    exit
}
if ($Mode -eq 'hold-app') {
    $name = 'Global\ProGo.Instance.' + [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $mutex = New-Object Threading.Mutex($true, $name)
    try { Set-Content (Join-Path $Fixture 'held-app') $PID; Start-Sleep -Seconds 30 } finally { $mutex.ReleaseMutex(); $mutex.Dispose() }
    exit
}
if ($Mode -eq 'handoff') {
    $lease = [ProGo.MaintenanceOperation]::Enter()
    try { [ProGo.MaintenanceOperation]::ConfirmHandoff(); Start-Sleep -Milliseconds 1500 } finally { $lease.Dispose() }
    exit
}
if ($Mode -eq 'startup') {
    $guard = $null
    $allowed = [ProGo.MaintenanceOperation]::TryEnterStartup([ref]$guard)
    if ($null -ne $guard) { $guard.Dispose() }
    if (-not $allowed) { exit 4 }; exit 0
}
$env:LOCALAPPDATA = $Fixture
$BackupDir = Join-Path $Fixture 'backup'
$Scope = 'All'; $ConfirmData = $true
if ($Mode -eq 'restore-program') { $Scope = 'Program'; $ConfirmData = $false }
if ($Mode -eq 'restore-data') { $Scope = 'Data' }
if ($Mode -eq 'restore-no-consent') { $ConfirmData = $false }
$WaitPid = 0; $NoLaunch = $true; $Force = $true; $NoReleasePackage = $false
$ReleasePackageUrl = ''; $SourceZipUrl = ''; $RemoteVersionUrl = ''
if ($Mode.StartsWith('restore')) { $file = Join-Path $Scripts 'Restore-ProGoBackup.ps1' }
else { $file = Join-Path $Scripts 'Update-ProGo.Core.ps1' }
# Execute the actual local transaction body. Only network and modal UI are
# replaced by fixtures; installation, checks, rollback and lease lifetime stay real.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Transaction does not parse' }
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) {
        . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')))
    }
}
function Show-UserMessage($Text, $Title) {}
function Show-UpdateDialog($Text, $Title, $Kind) {}
function Test-UpdateRequired { $State.RemoteVersion = (Get-Content (Join-Path $Release 'VERSION')).Trim(); return $true }
function Get-ReleaseDirForUpdate { return $Release }
if ($Mode -eq 'restore-source-changed') {
    function Wait-ProGoExit($TargetProcessId, $TimeoutMs) { Set-Content (Join-Path $Fixture 'backup\settings.json') 'source changed after preparation' }
}
if ($Mode -eq 'rollback') {
    function Install-StagingToMain($TargetDir) {
        $State.MainWasChanged = $true
        Copy-Item (Join-Path $TargetDir 'ProGo.exe') (Join-Path $InstallDir 'ProGo.exe') -Force
        Set-Content (Join-Path $InstallDir 'VERSION') 'broken-fixture'
    }
}
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
        . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')))
    }
}
