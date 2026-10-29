param([Parameter(Mandatory=$true)][string]$BrokerPipe)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# This file runs only from the sealed, pinned native stage. The native launcher
# starts the fixed System32 host with an allowlisted environment BEFORE CLR startup.
# No policy override, profile, command string or arbitrary assembly path is accepted.
Add-Type -Path (Join-Path $PSScriptRoot 'ProGo.exe')
$broker = $null
$result = 1
try {
    $broker = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::EnterNativeBroker($BrokerPipe)
    if ($broker.Operation -ceq 'install') {
        & (Join-Path $PSScriptRoot 'scripts/Install-ProGo.ps1') -Personal -NoStartup -PayloadDirectory $PSScriptRoot
    } elseif ($broker.Operation -ceq 'uninstall') {
        & (Join-Path $PSScriptRoot 'scripts/Uninstall-ProGo.ps1') -Personal
    } else { throw 'Personal broker operation differs.' }
    $result = 0
} finally {
    if ($null -ne $broker) {
        try { $broker.Complete($result) } finally { $broker.Dispose() }
    }
}
exit $result
