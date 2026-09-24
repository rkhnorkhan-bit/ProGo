param(
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [switch]$NoStartup
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$TempRoot = Join-Path $env:TEMP ("ProGo-install-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$ZipPath = Join-Path $TempRoot "ProGo-main.zip"

New-Item -ItemType Directory -Path $TempRoot -Force | Out-Null

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
} catch {
    Write-Warning "TLS setup warning: $($_.Exception.Message)"
}

Write-Host "Downloading ProGo from GitHub..."
Invoke-WebRequest -Uri $SourceZipUrl -OutFile $ZipPath -UseBasicParsing

Write-Host "Extracting ProGo source..."
Expand-Archive -Path $ZipPath -DestinationPath $TempRoot -Force

$SourceRoot = Get-ChildItem -Path $TempRoot -Directory | Where-Object { Test-Path (Join-Path $_.FullName "scripts\Install-ProGo.ps1") } | Select-Object -First 1
if ($null -eq $SourceRoot) {
    throw "Downloaded archive does not contain scripts\Install-ProGo.ps1"
}

$InstallScript = Join-Path $SourceRoot.FullName "scripts\Install-ProGo.ps1"
if ($NoStartup) {
    & $InstallScript -NoStartup
} else {
    & $InstallScript
}

Write-Host "ProGo GitHub install completed."
