Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$Exe = Join-Path $InstallDir "ProGo.exe"

if (-not (Test-Path $Exe)) {
    throw "ProGo.exe not found: $Exe"
}

Start-Process -FilePath $Exe -ArgumentList "--show" -WorkingDirectory $InstallDir
Write-Host "ProGo window requested. An existing instance is reused."
