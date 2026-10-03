Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# This is installed source, never downloaded/evaluated code. Build copies the
# same class used by Program, so the mutex names and handoff cannot drift.
if (-not ('ProGo.MaintenanceOperation' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'MaintenanceOperation.cs')
}
