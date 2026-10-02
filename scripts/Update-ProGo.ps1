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
$LocalCoreScriptPath = Join-Path $PSScriptRoot "Update-ProGo.Core.ps1"

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
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    Add-Content -LiteralPath (Join-Path $InstallDir "update.log") -Value ("Installed updater failed: " + $_.Exception.Message) -Encoding UTF8
    # Existing process stays alive after an early launch failure. Relaunch only if it exited.
    if (-not $NoLaunch -and $WaitPid -gt 0) {
        $old = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue
        if ($null -eq $old -and (Test-Path -LiteralPath (Join-Path $InstallDir "ProGo.exe"))) {
            Start-Process -FilePath (Join-Path $InstallDir "ProGo.exe") -WorkingDirectory $InstallDir
        }
    }
    throw
}
