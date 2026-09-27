param(
    [int]$WaitPid = 0,
    [string]$ReleasePackageUrl = "https://github.com/rkhnorkhan-bit/ProGo/releases/latest/download/ProGo-release.zip",
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [string]$RemoteVersionUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest",
    [string]$CoreApiUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/contents/scripts/Update-ProGo.Core.ps1?ref=main",
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

function Get-CoreFromGitHubApi {
    Write-BootstrapLog "Downloading updater core through GitHub API."
    $headers = @{ "User-Agent" = "ProGo-Updater" }
    $response = Invoke-WebRequest -Uri $CoreApiUrl -Headers $headers -UseBasicParsing -ErrorAction Stop
    $payload = $response.Content | ConvertFrom-Json
    if ($payload.encoding -ne "base64" -or [string]::IsNullOrWhiteSpace([string]$payload.content)) {
        throw "GitHub API did not return base64 updater core content."
    }

    $base64 = ([string]$payload.content) -replace "\\s", ""
    $bytes = [Convert]::FromBase64String($base64)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ([string]::IsNullOrWhiteSpace($text) -or $text.Length -lt 1024) {
        throw "GitHub API updater core content is empty or unexpectedly small."
    }

    Write-BootstrapLog "Updater core received through GitHub API."
    return $text
}

function Get-CoreFromSourceArchive {
    Write-BootstrapLog "GitHub API unavailable; trying source archive transport."

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $client = New-Object System.Net.WebClient
    $client.Headers.Add("User-Agent", "ProGo-Updater")
    $bytes = $client.DownloadData($SourceZipUrl)
    $memory = New-Object System.IO.MemoryStream(,$bytes)
    $archive = New-Object System.IO.Compression.ZipArchive($memory, [System.IO.Compression.ZipArchiveMode]::Read, $false)

    try {
        $entry = $archive.Entries |
            Where-Object { $_.FullName -like "*/scripts/Update-ProGo.Core.ps1" } |
            Select-Object -First 1

        if ($null -eq $entry) {
            throw "Source archive does not contain scripts/Update-ProGo.Core.ps1."
        }

        $stream = $entry.Open()
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8, $true)
        try {
            $text = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
            $stream.Dispose()
        }

        if ([string]::IsNullOrWhiteSpace($text) -or $text.Length -lt 1024) {
            throw "Source archive updater core is empty or unexpectedly small."
        }

        Write-BootstrapLog "Updater core received from source archive."
        return $text
    } finally {
        $archive.Dispose()
        $memory.Dispose()
        $client.Dispose()
    }
}

function Get-UpdaterCoreText {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {
        Write-BootstrapLog "TLS setup warning: $($_.Exception.Message)"
    }

    try {
        return (Get-CoreFromGitHubApi)
    } catch {
        Write-BootstrapLog "GitHub API updater core download failed: $($_.Exception.Message)"
    }

    try {
        return (Get-CoreFromSourceArchive)
    } catch {
        Write-BootstrapLog "Source archive updater core download failed: $($_.Exception.Message)"
    }

    if (Test-Path $LocalCoreScriptPath) {
        Write-BootstrapLog "Falling back to installed updater core."
        return [System.IO.File]::ReadAllText($LocalCoreScriptPath)
    }

    throw "All updater core transports failed."
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
