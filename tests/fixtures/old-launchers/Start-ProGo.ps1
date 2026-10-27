param(
    [switch]$Show
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$Exe = Join-Path $InstallDir "ProGo.exe"

if (-not (Test-Path $Exe)) {
    throw "ProGo.exe not found: $Exe. Run Repair-ProGo.ps1 or reinstall ProGo."
}

$launch = @{ FilePath = $Exe; WorkingDirectory = $InstallDir }
if ($Show) { $launch.ArgumentList = "--show" }
Start-Process @launch | Out-Null
Write-Host "ProGo launch requested: $Exe. An existing instance opens its window."
