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

$args = @()
if ($Show) { $args += "--show" }

Start-Process -FilePath $Exe -WorkingDirectory $InstallDir -ArgumentList $args | Out-Null
Write-Host "ProGo started: $Exe"
