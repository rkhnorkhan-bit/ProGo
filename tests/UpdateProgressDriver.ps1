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
$realPhase = ${function:Show-UpdatePhase}
function Show-UpdatePhase($Phase) {
    [void]$script:events.Add(@{ Kind = 'phase'; Phase = $Phase })
    & $realPhase $Phase
}
function Write-Progress {
    param($Id, $Activity, $Status, $CurrentOperation, $PercentComplete, [switch]$Completed)
    [void]$script:events.Add(@{ Kind = 'progress'; Id = $Id; Activity = $Activity; Status = $Status;
        Operation = $CurrentOperation; Percent = $PercentComplete; Completed = [bool]$Completed })
    if ($Mode -eq 'render-failure') { throw 'Injected progress host rendering failure' }
}
function Show-UpdateDialog($Text, $Title, $Kind) {
    [void]$script:events.Add(@{ Kind = 'dialog'; Icon = $Kind })
}
function Show-UserMessage($Text, $Title) { Show-UpdateDialog $Text $Title 'Information' }
function Invoke-WebRequest {
    param($Uri, $Headers, $OutFile, [switch]$UseBasicParsing, $ErrorAction)
    if ($OutFile) {
        if ($Mode -eq 'download-failure') { throw 'Injected download failure' }
        Copy-Item -LiteralPath $Package -Destination $OutFile
        return
    }
    $version = (Get-Content -Raw (Join-Path $Release 'VERSION')).Trim()
    $digest = (Get-FileHash -LiteralPath $Package -Algorithm SHA256).Hash
    if ($Mode -eq 'hash-failure') { $digest = '0' * 64 }
    $asset = @{ name = 'ProGo-release.zip'; digest = 'sha256:' + $digest;
        browser_download_url = 'https://github.com/rkhnorkhan-bit/ProGo/releases/download/v' + $version + '/ProGo-release.zip' }
    return @{ Content = (@{ tag_name = 'v' + $version; draft = $false; prerelease = $false; assets = @($asset) } | ConvertTo-Json -Depth 5) }
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
} catch { $failed = $true }
finally {
    [IO.File]::WriteAllText((Join-Path $Fixture 'progress.json'), ($script:events | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
}
if ($failed) { exit 7 }
