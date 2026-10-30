param(
    [switch]$Show
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Layout-ProGo.ps1")
$layout = Resolve-ProGoLayout
$InstallDir = $layout.RuntimeRoot
$Exe = $layout.Executable

if (-not (Test-Path $Exe)) {
    throw "ProGo.exe not found: $Exe. Run Repair-ProGo.ps1 or reinstall ProGo."
}

$launch = @{ FilePath = $Exe; WorkingDirectory = $InstallDir }
if ($Show) { $launch.ArgumentList = "--show" }
Start-Process @launch | Out-Null
Write-Host "ProGo launch requested: $Exe. An existing instance opens its window."
