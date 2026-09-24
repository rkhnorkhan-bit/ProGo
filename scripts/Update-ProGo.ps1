param(
    [int]$WaitPid = 0,
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$UpdateLog = Join-Path $InstallDir "progo-update.log"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$WorkDir = Join-Path $InstallDir ("update-" + $Timestamp)

function Write-UpdateLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $UpdateLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Fail($Message) {
    Write-UpdateLog ("ERROR: " + $Message)
    throw $Message
}

Write-UpdateLog "ProGo update started."

if ($WaitPid -gt 0) {
    Write-UpdateLog "Waiting for ProGo process to exit: PID $WaitPid"
    try {
        $process = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            $process.WaitForExit(30000)
        }
    } catch {
        Write-UpdateLog "Wait process warning: $($_.Exception.Message)"
    }
}

New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
$ZipPath = Join-Path $WorkDir "ProGo-main.zip"

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
} catch {
    Write-UpdateLog "TLS setup warning: $($_.Exception.Message)"
}

Write-UpdateLog "Downloading source from GitHub..."
Invoke-WebRequest -Uri $SourceZipUrl -OutFile $ZipPath -UseBasicParsing

Write-UpdateLog "Extracting source..."
Expand-Archive -Path $ZipPath -DestinationPath $WorkDir -Force

$SourceRoot = Get-ChildItem -Path $WorkDir -Directory | Where-Object { Test-Path (Join-Path $_.FullName "scripts\Install-ProGo.ps1") } | Select-Object -First 1
if ($null -eq $SourceRoot) {
    Fail "Downloaded archive does not contain scripts\Install-ProGo.ps1"
}

$InstallScript = Join-Path $SourceRoot.FullName "scripts\Install-ProGo.ps1"
Write-UpdateLog "Installing updated ProGo from $($SourceRoot.FullName)"
& $InstallScript -NoStartup

if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
    Fail "Installer failed with exit code $LASTEXITCODE"
}

$Exe = Join-Path $InstallDir "ProGo.exe"
if (-not (Test-Path $Exe)) {
    Fail "Installed ProGo.exe not found: $Exe"
}

Write-UpdateLog "Starting updated ProGo..."
Start-Process -FilePath $Exe -WorkingDirectory $InstallDir | Out-Null

try {
    Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
} catch {
    Write-UpdateLog "Cleanup warning: $($_.Exception.Message)"
}

Write-UpdateLog "ProGo update completed."
