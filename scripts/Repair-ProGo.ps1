param(
    [switch]$NoLaunch
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$ScriptsDir = Join-Path $InstallDir "scripts"
$BackupsDir = Join-Path $InstallDir "backups"
$Root = Split-Path -Parent $PSScriptRoot
$ReleaseDir = Join-Path $Root "release"

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
New-Item -ItemType Directory -Path $ScriptsDir -Force | Out-Null
New-Item -ItemType Directory -Path $BackupsDir -Force | Out-Null

function Copy-IfExists($Source, $Destination) {
    if (Test-Path $Source) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Host "Restored: $Destination"
    }
}

$releaseExe = Join-Path $ReleaseDir "ProGo.exe"
$releaseVersion = Join-Path $ReleaseDir "VERSION"
$releaseIcon = Join-Path $ReleaseDir "ProGo.ico"

Copy-IfExists $releaseExe (Join-Path $InstallDir "ProGo.exe")
Copy-IfExists $releaseVersion (Join-Path $InstallDir "VERSION")
Copy-IfExists $releaseIcon (Join-Path $InstallDir "ProGo.ico")

foreach ($scriptName in @("Update-ProGo.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Uninstall-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1")) {
    $candidate = Join-Path $PSScriptRoot $scriptName
    Copy-IfExists $candidate (Join-Path $ScriptsDir $scriptName)
}

$exe = Join-Path $InstallDir "ProGo.exe"
if (-not (Test-Path $exe)) {
    throw "Repair completed folders/scripts, but ProGo.exe is still missing: $exe"
}

Write-Host "Repair OK: $InstallDir"
if (-not $NoLaunch) {
    Start-Process -FilePath $exe -WorkingDirectory $InstallDir | Out-Null
    Write-Host "ProGo launched."
}
