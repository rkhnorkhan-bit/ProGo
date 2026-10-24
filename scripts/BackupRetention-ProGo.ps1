Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# Installed local source shared with the compiled app; never downloaded code.
if (-not ('ProGo.BackupRetention' -as [type])) {
    $source = Join-Path $PSScriptRoot 'BackupRetention.cs'
    if (-not (Test-Path $source)) { $source = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\BackupRetention.cs' }
    Add-Type -Path $source
}
