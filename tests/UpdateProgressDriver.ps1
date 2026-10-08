param([string]$Mode, [string]$Scripts, [string]$Fixture, [string]$Release, [string]$Package)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Requires isolated Windows CI' }
[Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)
$env:LOCALAPPDATA = $Fixture
$WaitPid = 0; $NoLaunch = $true; $Force = $false; $NoReleasePackage = $false
$ReleasePackageUrl = ''; $SourceZipUrl = ''; $RemoteVersionUrl = ''
$script:events = New-Object Collections.ArrayList
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Updater does not parse' }
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) {
        . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')))
    }
}
$realWindow = ${function:Initialize-UpdateWindow}
function Initialize-UpdateWindow {
    if ($Mode -in @('download-cancel','boundary-cancel','cancel-alive')) { & $realWindow }
}
$realPhase = ${function:Show-UpdatePhase}
function Show-UpdatePhase($Phase) {
    [void]$script:events.Add(@{ Kind = 'phase'; Phase = $Phase })
    & $realPhase $Phase
}
function Write-Progress {
    param($Id, $Activity, $Status, $CurrentOperation, $PercentComplete, [switch]$Completed)
    [void]$script:events.Add(@{ Kind = 'progress'; Id = $Id; Activity = $Activity; Status = $Status;
        Operation = $CurrentOperation; Percent = $PercentComplete; Completed = [bool]$Completed })
    if ($Mode -eq 'render-failure' -and $Id -eq 23) { throw 'Injected helper progress rendering failure' }
}
function Show-UpdateDialog($Text, $Title, $Kind) {
    [void]$script:events.Add(@{ Kind = 'dialog'; Icon = $Kind })
}
function Show-UserMessage($Text, $Title) { Show-UpdateDialog $Text $Title 'Information' }
function Save-UpdatePackage($Url, $Path) {
    if ($Mode -in @('download-cancel','cancel-alive')) {
        [IO.File]::WriteAllText($Path, 'partial fixture')
        [void]$UpdateWindow.RequestCancellation()
        $UpdateWindow.Token.ThrowIfCancellationRequested()
    }
    if ($Mode -eq 'download-failure') { throw 'Injected download failure' }
    Copy-Item -LiteralPath $Package -Destination $Path
    if ($Mode -eq 'boundary-cancel') { [void]$UpdateWindow.RequestCancellation() }
}
function Read-UpdateMetadata($Url) {
    $version = (Get-Content -Raw (Join-Path $Release 'VERSION')).Trim()
    $digest = (Get-FileHash -LiteralPath $Package -Algorithm SHA256).Hash
    if ($Mode -eq 'hash-failure') { $digest = '0' * 64 }
    $asset = @{ name = 'ProGo-release.zip'; digest = 'sha256:' + $digest;
        browser_download_url = 'https://github.com/rkhnorkhan-bit/ProGo/releases/download/v' + $version + '/ProGo-release.zip' }
    return (@{ tag_name = 'v' + $version; draft = $false; prerelease = $false; assets = @($asset) } | ConvertTo-Json -Depth 5)
}
if ($Mode -eq 'stage-failure') { function Test-StagingCopy($TargetDir) { throw 'Injected staging check failure' } }
if ($Mode -eq 'commit-failure' -or $Mode -eq 'rollback-failure') {
    $realInstall = ${function:Install-StagingToMain}
    function Install-StagingToMain($TargetDir) { & $realInstall $TargetDir; throw 'Injected failure after installed files change' }
}
if ($Mode -eq 'restart-failure') { function Start-UpdatedProGo($ExePath) { throw 'Injected restart failure' } }
if ($Mode -eq 'rollback-failure') { function Restore-BackupToMain($SourceBackupDir) { throw 'Injected rollback failure' } }
$failed = $false
try {
    # Actual transaction and package/hash checks. Only network, progress host and
    # final modal UI are replaced; no remote request or normal app launch occurs.
    foreach ($statement in $ast.EndBlock.Statements) {
        if ($statement -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
            . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')))
        }
    }
} catch {
    $failed = $true
    $failureRecord = $_
    if ($Mode -in @('download-cancel','boundary-cancel','cancel-alive')) {
        $bootstrap = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Update-ProGo.ps1'), [ref]$tokens, [ref]$errors)
        if ($errors.Count) { throw 'Bootstrap does not parse' }
        foreach ($statement in $bootstrap.EndBlock.Statements) {
            if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) { . ([scriptblock]::Create($statement.Extent.Text)) }
        }
        $script:resumed = 0; $script:cancelNotice = $false
        function Show-UpdaterCancellation([bool]$LaunchSuppressed) { $script:cancelNotice = $true }
        function Get-Process { param($Id, $ErrorAction); if ($Mode -eq 'cancel-alive') { return @{ Id = $Id } } }
        function Start-Process {
            param($FilePath, $WorkingDirectory)
            if ($FilePath -ne (Join-Path $InstallDir 'ProGo.exe') -or $WorkingDirectory -ne $InstallDir) { throw 'Recovery changed application target' }
            $lease = [ProGo.MaintenanceOperation]::Enter(); $lease.Dispose()
            $script:resumed++
        }
        $NoLaunch = $Mode -eq 'download-cancel'; $WaitPid = 88
        $handler = @($bootstrap.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.TryStatementAst] })[-1].CatchClauses[0].Body.Extent.Text
        $handler = $handler.Substring(1, $handler.Length - 2)
        $failureRecord | ForEach-Object { . ([scriptblock]::Create($handler)) }
        $generic = [Management.Automation.ErrorRecord]::new([OperationCanceledException]::new(), 'fixture', [Management.Automation.ErrorCategory]::NotSpecified, $null)
        $facts = @{ Notice = $script:cancelNotice; Resumes = $script:resumed; GenericRejected = -not (Test-UpdaterCancellation $generic);
            MainUnchanged = -not $State.MainWasChanged; TransactionRemoved = -not (Test-Path $TransactionRoot);
            NoBackup = [string]::IsNullOrWhiteSpace($BackupDir); WindowClosed = $null -eq $UpdateWindow }
        [IO.File]::WriteAllText((Join-Path $Fixture 'cancellation.json'), ($facts | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
    }
}
finally {
    [IO.File]::WriteAllText((Join-Path $Fixture 'progress.json'), ($script:events | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
}
if ($failed) { exit 7 }
