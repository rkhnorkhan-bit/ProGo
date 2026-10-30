Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Layout-ProGo.ps1")
$layout = Resolve-ProGoLayout
$InstallDir = $layout.RuntimeRoot
$Exe = $layout.Executable

if (-not (Test-Path $Exe)) {
    throw "ProGo.exe not found: $Exe"
}

Start-Process -FilePath $Exe -ArgumentList "--show" -WorkingDirectory $InstallDir
Write-Host "ProGo window requested. An existing instance is reused."
