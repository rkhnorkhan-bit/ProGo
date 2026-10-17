param(
    [int]$WaitPid = 0,
    [string]$ReleasePackageUrl = "https://github.com/rkhnorkhan-bit/ProGo/releases/latest/download/ProGo-release.zip",
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [string]$RemoteVersionUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest",
    [switch]$NoLaunch,
    [switch]$Force,
    [switch]$NoReleasePackage
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
try { . (Join-Path $PSScriptRoot 'Log-ProGo.ps1') } catch { }
$LocalCoreScriptPath = Join-Path $PSScriptRoot "Update-ProGo.Core.ps1"

. (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1')
# The core takes the operation lease. Do not handle a competing operation as an
# update failure or relaunch another process after its owner has exited.
$StartupProbe = $null
if (-not [ProGo.MaintenanceOperation]::TryEnterStartup([ref]$StartupProbe)) {
    throw 'ProGo: update or restore is already in progress.'
}
if ($null -ne $StartupProbe) { $StartupProbe.Dispose() }
function Test-UpdaterCancellation($ErrorObject) {
    $exception = $ErrorObject.Exception
    while ($null -ne $exception) {
        if ($exception.GetType().FullName -eq 'ProGo.UpdateDownloadCancelledException') { return $true }
        $exception = $exception.InnerException
    }
    return $false
}
function Show-UpdaterCancellation([bool]$LaunchSuppressed) {
    $text = 'Скачивание отменено. Установленные файлы ProGo не изменены.'
    if ($LaunchSuppressed) { $text += ' Автоматический запуск отключён.' }
    Write-Host $text
    if (-not $LaunchSuppressed) {
        Add-Type -AssemblyName System.Windows.Forms
        [void][Windows.Forms.MessageBox]::Show($text, 'Обновление ProGo', 'OK', 'Information')
    }
}

# Run the installed, reviewable file. Never fetch or evaluate remote script text.
try {
    if (-not (Test-Path -LiteralPath $LocalCoreScriptPath -PathType Leaf)) {
        throw "Installed updater is missing. Download the official release package and repair the installation."
    }
    $coreArgs = @{
        WaitPid = $WaitPid
        ReleasePackageUrl = $ReleasePackageUrl
        SourceZipUrl = $SourceZipUrl
        RemoteVersionUrl = $RemoteVersionUrl
        NoLaunch = $NoLaunch
        Force = $Force
        NoReleasePackage = $NoReleasePackage
    }
    & $LocalCoreScriptPath @coreArgs
} catch {
    if ($_.Exception.ToString().Contains("already in progress")) { throw }
    $cancelled = Test-UpdaterCancellation $_
    $outcome = if ($cancelled) { 'Installed updater download cancelled.' } else { 'Installed updater failed: ' + $_.Exception.Message }
    try { Write-ProGoLog -Path (Join-Path $InstallDir "update.log") -Message $outcome } catch { }
    # Existing process stays alive after an early launch failure. Relaunch only if it exited.
    if (-not $NoLaunch -and $WaitPid -gt 0) {
        $old = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue
        if ($null -eq $old -and (Test-Path -LiteralPath (Join-Path $InstallDir "ProGo.exe"))) {
            Start-Process -FilePath (Join-Path $InstallDir "ProGo.exe") -WorkingDirectory $InstallDir
        }
    }
    if ($cancelled) { Show-UpdaterCancellation ([bool]$NoLaunch); return }
    throw
}
