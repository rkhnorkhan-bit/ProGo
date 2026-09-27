param(
    [int]$WaitPid = 0,
    [string]$ReleasePackageUrl = "https://github.com/rkhnorkhan-bit/ProGo/releases/latest/download/ProGo-release.zip",
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [string]$RemoteVersionUrl = "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/VERSION",
    [string]$CoreScriptUrl = "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/scripts/Update-ProGo.Core.ps1",
    [switch]$NoLaunch,
    [switch]$Force,
    [switch]$NoReleasePackage
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$ScriptsDir = Join-Path $InstallDir "scripts"
$CoreScriptPath = Join-Path $ScriptsDir "Update-ProGo.Core.ps1"
$BootstrapLog = Join-Path $InstallDir "update.log"

function Write-BootstrapLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $BootstrapLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Refresh-CoreScript {
    New-Item -ItemType Directory -Path $ScriptsDir -Force | Out-Null
    $downloadPath = $CoreScriptPath + ".download"
    Remove-Item -LiteralPath $downloadPath -Force -ErrorAction SilentlyContinue

    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {
        Write-BootstrapLog "TLS setup warning: $($_.Exception.Message)"
    }

    Write-BootstrapLog "Downloading updater core from GitHub."
    Invoke-WebRequest -Uri $CoreScriptUrl -OutFile $downloadPath -UseBasicParsing -ErrorAction Stop

    if (-not (Test-Path $downloadPath)) {
        throw "Updater core download did not create a file."
    }

    if ((Get-Item $downloadPath).Length -lt 1024) {
        throw "Updater core download is unexpectedly small."
    }

    Move-Item -LiteralPath $downloadPath -Destination $CoreScriptPath -Force
    try { Unblock-File -LiteralPath $CoreScriptPath -ErrorAction SilentlyContinue } catch {}
    Write-BootstrapLog "Updater core refreshed."
}

try {
    Write-BootstrapLog "Updater bootstrap started."

    try {
        Refresh-CoreScript
    } catch {
        Write-BootstrapLog "Updater core refresh failed: $($_.Exception.Message)"
        if (-not (Test-Path $CoreScriptPath)) {
            throw
        }
        Write-BootstrapLog "Using existing updater core."
    }

    $coreArgs = @{
        WaitPid = $WaitPid
        ReleasePackageUrl = $ReleasePackageUrl
        SourceZipUrl = $SourceZipUrl
        RemoteVersionUrl = $RemoteVersionUrl
    }

    if ($NoLaunch) { $coreArgs.NoLaunch = $true }
    if ($Force) { $coreArgs.Force = $true }
    if ($NoReleasePackage) { $coreArgs.NoReleasePackage = $true }

    Write-BootstrapLog "Starting transactional updater core."
    & $CoreScriptPath @coreArgs
} catch {
    Write-BootstrapLog "Updater bootstrap failed: $($_.Exception.Message)"
    throw
}
