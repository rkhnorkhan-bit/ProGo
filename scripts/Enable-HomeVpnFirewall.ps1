param(
    [Parameter(Mandatory=$true)][string]$ProGoExe,
    [switch]$Remove
)

# Run elevated. The rules apply only to the specified executable and two UDP ports.
# They do not expose the SOCKS/HTTP proxies or change other firewall rules.
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot 'Firewall-ProGo.ps1')
$RuleNames = @("ProGo-Home-IKE", "ProGo-Home-NAT-T")
if ($Remove) {
    Remove-ProGoHomeFirewallRules $ProGoExe
    return
}
$ExePath = (Resolve-Path -LiteralPath $ProGoExe).ProviderPath
if ([System.IO.Path]::GetFileName($ExePath) -ne 'ProGo.exe') { throw 'Select the ProGo.exe that you will run.' }
$Ports = @(15000, 14500)
for ($i = 0; $i -lt $RuleNames.Count; $i++) {
    Get-NetFirewallRule -Name $RuleNames[$i] -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -Name $RuleNames[$i] -DisplayName $RuleNames[$i] -Direction Inbound `
        -Action Allow -Protocol UDP -LocalPort $Ports[$i] -Program $ExePath -Profile Any | Out-Null
}
Write-Host 'ProGo UDP entry ports allowed: 15000, 14500. Configure UDP forwarding on the home router separately.'
