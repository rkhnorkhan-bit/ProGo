param([string]$ScriptsDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Firewall tests require disposable Windows CI.' }
. (Join-Path $ScriptsDirectory 'Firewall-ProGo.ps1')
$passed = 0
function Check([bool]$ok, [string]$name) {
    if (-not $ok) { throw "Firewall test failed: $name" }
    $script:passed++; Write-Host "PASS: $name"
}
function Refused([scriptblock]$action) {
    try { [void](& $action); return $false } catch { return $true }
}
$names = @('ProGo-Home-IKE', 'ProGo-Home-NAT-T')
$sentinel = 'ProGo-fixture-' + [guid]::NewGuid().ToString('N')
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-firewall-' + [guid]::NewGuid().ToString('N'))
$exe = Join-Path $fixture 'ProGo.exe'
$other = Join-Path $fixture 'other\ProGo.exe'
# Never adopt or remove a pre-existing rule even on CI.
if (@(Get-NetFirewallRule -PolicyStore PersistentStore -ErrorAction Stop | Where-Object { $_.Name -in $names }).Count) {
    throw 'Firewall fixture is not isolated.'
}
function Seed([string]$name, [string]$program, [string]$protocol='UDP', [int]$port=15000, [string]$direction='Inbound') {
    New-NetFirewallRule -PolicyStore PersistentStore -Name $name -DisplayName $name -Direction $direction -Action Allow `
        -Protocol $protocol -LocalPort $port -Program $program -Enabled False -ErrorAction Stop | Out-Null
}
function Rules { @(Get-NetFirewallRule -PolicyStore PersistentStore -ErrorAction Stop | Where-Object { $_.Name -in ($names + $sentinel) }) }
function Reset {
    Rules | Remove-NetFirewallRule -ErrorAction Stop
}
try {
    Check (@(Get-ProGoHomeFirewallRules $exe).Count -eq 0) 'missing executable and absent rules need no cleanup'
    Complete-ProGoHomeFirewallCleanup $exe 'missing-helper.ps1'
    Check $true 'empty policy does not request elevation or require a removal script'
    Check (Refused { Get-ProGoHomeFirewallRules 'relative\ProGo.exe' }) 'relative target refused'
    Check (Refused { Get-ProGoHomeFirewallRules (Join-Path $fixture 'other.exe') }) 'wrong executable refused'
    Seed $names[0] $exe
    Seed $names[1] $exe 'UDP' 14500
    Seed $sentinel $exe
    Check (@(Get-ProGoHomeFirewallRules $exe).Count -eq 2) 'real disabled legacy rules recognized by program and UDP ports'
    & (Join-Path $ScriptsDirectory 'Enable-HomeVpnFirewall.ps1') -Remove -ProGoExe $exe
    Check (@(Rules).Count -eq 1 -and (Rules)[0].Name -eq $sentinel) 'shipped remove entry deletes only both recognized rules'
    Remove-ProGoHomeFirewallRules $exe
    Check (@(Rules).Count -eq 1) 'repeat cleanup is harmless and preserves unrelated same-program rule'
    Reset

    Seed $names[0] $other
    Seed $names[1] $exe 'UDP' 14500
    Remove-ProGoHomeFirewallRules $exe
    Check (@(Rules).Count -eq 1 -and (Rules)[0].Name -eq $names[0]) 'another installation survives while this installation is cleaned'
    Reset
    foreach ($case in @(@('TCP',15000,'Inbound'), @('UDP',15001,'Inbound'), @('UDP',15000,'Outbound'))) {
        Seed $names[0] $exe $case[0] $case[1] $case[2]
        Remove-ProGoHomeFirewallRules $exe
        Check (@(Rules).Count -eq 1) ('customized rule preserved: ' + ($case -join '/'))
        Reset
    }
    Seed $names[0] $exe
    Seed $names[1] $exe 'UDP' 14500
    # Execute the real removal algorithm; only the cmdlet boundary fails.
    & {
        function Remove-NetFirewallRule { [CmdletBinding()] param([Parameter(ValueFromPipeline=$true)]$InputObject) process { throw 'synthetic access denied' } }
        Check (Refused { Remove-ProGoHomeFirewallRules $exe }) 'removal denial is an error rather than false success'
    }
    Check (@(Get-ProGoHomeFirewallRules $exe).Count -eq 2) 'denied removal retains rules for retry'
    & {
        function Test-ProGoFirewallAdministrator { return $false }
        function Start-Process { [CmdletBinding()] param($FilePath,$ArgumentList,$Verb,[switch]$Wait,[switch]$PassThru) throw 'synthetic UAC cancellation' }
        Check (Refused { Complete-ProGoHomeFirewallCleanup $exe (Join-Path $ScriptsDirectory 'Enable-HomeVpnFirewall.ps1') }) 'cancelled elevation reports failure'
    }
    Check (@(Get-ProGoHomeFirewallRules $exe).Count -eq 2) 'cancelled elevation does not delete rules'
    & {
        function Test-ProGoFirewallAdministrator { return $false }
        function Start-Process {
            [CmdletBinding()] param($FilePath,$ArgumentList,$Verb,[switch]$Wait,[switch]$PassThru)
            Check ($Verb -eq 'RunAs' -and $Wait -and $PassThru -and $ArgumentList.Contains('-Remove -ProGoExe "' + $exe + '"')) 'elevated child receives original explicit executable and removal-only arguments'
            $p = [pscustomobject]@{ ExitCode=0 }
            $p | Add-Member -MemberType ScriptMethod -Name Dispose -Value { }
            return $p
        }
        Check (Refused { Complete-ProGoHomeFirewallCleanup $exe (Join-Path $ScriptsDirectory 'Enable-HomeVpnFirewall.ps1') }) 'successful child exit alone cannot confirm removal'
    }
    Complete-ProGoHomeFirewallCleanup $exe 'unused-on-admin.ps1'
    Check (@(Rules).Count -eq 0) 'admin cleanup succeeds on retry'
    & {
        function Get-NetFirewallRule { [CmdletBinding()] param([string]$PolicyStore) throw 'synthetic provider failure' }
        Check (Refused { Complete-ProGoHomeFirewallCleanup $exe 'unused.ps1' }) 'query failure cannot masquerade as an empty policy'
    }
    foreach ($file in @('Firewall-ProGo.ps1', 'Enable-HomeVpnFirewall.ps1')) {
        Check ((Get-FileHash (Join-Path $ScriptsDirectory $file)).Hash -eq (Get-FileHash (Join-Path $PSScriptRoot ('..\scripts\' + $file))).Hash) ('packaged helper matches reviewed source: ' + $file)
    }
    Write-Host "Home firewall tests PASS: $passed"
} finally { Reset }
