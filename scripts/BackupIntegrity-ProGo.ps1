Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# Installed local source; the app compiles the identical integrity implementation.
if (-not ('ProGo.BackupIntegrity' -as [type])) {
    $source = Join-Path $PSScriptRoot 'BackupIntegrity.cs'
    if (-not (Test-Path $source)) { $source = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\BackupIntegrity.cs' }
    Add-Type -Path $source
}
