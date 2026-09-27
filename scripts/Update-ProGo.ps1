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
$LocalCoreScriptPath = Join-Path $ScriptsDir "Update-ProGo.Core.ps1"
$BootstrapLog = Join-Path $InstallDir "update.log"
$LegacyBootstrapLog = Join-Path $InstallDir "progo-update.log"

function Write-BootstrapLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $BootstrapLog -Value $line -Encoding UTF8
    Add-Content -Path $LegacyBootstrapLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Wait-OldProGoExit {
    if ($WaitPid -le 0) { return }

    try {
        $process = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            Write-BootstrapLog "Waiting for old ProGo process before recovery: PID $WaitPid"
            [void]$process.WaitForExit(30000)
        }
    } catch {
        Write-BootstrapLog "Recovery wait warning: $($_.Exception.Message)"
    }
}

function Restart-InstalledProGo {
    if ($NoLaunch) { return }

    Wait-OldProGoExit

    $exe = Join-Path $InstallDir "ProGo.exe"
    if (-not (Test-Path $exe)) {
        Write-BootstrapLog "Recovery launch skipped; ProGo.exe not found: $exe"
        return
    }

    try {
        $process = Start-Process -FilePath $exe -WorkingDirectory $InstallDir -PassThru
        Write-BootstrapLog "Recovery launch started ProGo. PID=$($process.Id)"
    } catch {
        Write-BootstrapLog "Recovery launch failed: $($_.Exception.Message)"
    }
}

function Get-UpdaterCoreText {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {
        Write-BootstrapLog "TLS setup warning: $($_.Exception.Message)"
    }

    try {
        Write-BootstrapLog "Downloading updater core from GitHub into memory."
        $response = Invoke-WebRequest -Uri $CoreScriptUrl -UseBasicParsing -ErrorAction Stop
        $text = [string]$response.Content

        if ([string]::IsNullOrWhiteSpace($text) -or $text.Length -lt 1024) {
            throw "Downloaded updater core is empty or unexpectedly small."
        }

        Write-BootstrapLog "Updater core downloaded into memory."
        return $text
    } catch {
        Write-BootstrapLog "In-memory updater core download failed: $($_.Exception.Message)"

        if (Test-Path $LocalCoreScriptPath) {
            Write-BootstrapLog "Falling back to installed updater core."
            return [System.IO.File]::ReadAllText($LocalCoreScriptPath)
        }

        throw
    }
}

try {
    Write-BootstrapLog "Updater bootstrap started."

    $coreText = Get-UpdaterCoreText
    $coreBlock = [ScriptBlock]::Create($coreText)

    $coreArgs = @{
        WaitPid = $WaitPid
        ReleasePackageUrl = $ReleasePackageUrl
        SourceZipUrl = $SourceZipUrl
        RemoteVersionUrl = $RemoteVersionUrl
    }

    if ($NoLaunch) { $coreArgs.NoLaunch = $true }
    if ($Force) { $coreArgs.Force = $true }
    if ($NoReleasePackage) { $coreArgs.NoReleasePackage = $true }

    Write-BootstrapLog "Starting transactional updater core in memory."
    & $coreBlock @coreArgs
} catch {
    $message = $_.Exception.Message
    Write-BootstrapLog "Updater bootstrap failed: $message"
    Restart-InstalledProGo
    throw
}
