# Local definitions only. Loading this helper never reads or changes firewall policy.
function Get-ProGoFirewallPath([string]$ProGoExe) {
    if (-not [IO.Path]::IsPathRooted($ProGoExe) -or $ProGoExe.Contains('"')) {
        throw 'Specify an absolute ProGo.exe path.'
    }
    $path = [IO.Path]::GetFullPath($ProGoExe)
    if ([IO.Path]::GetFileName($path) -ine 'ProGo.exe') { throw 'Select ProGo.exe.' }
    # No Resolve-Path: cleanup must also work after a missing/deleted executable.
    return $path
}

function Get-ProGoHomeFirewallRules([string]$ProGoExe) {
    $path = Get-ProGoFirewallPath $ProGoExe
    $ports = @{ 'ProGo-Home-IKE' = '15000'; 'ProGo-Home-NAT-T' = '14500' }
    # Enumerating the local store distinguishes an empty result from a failed query.
    # Never use SilentlyContinue: unavailable policy is not successful cleanup.
    $rules = @(Get-NetFirewallRule -PolicyStore PersistentStore -ErrorAction Stop)
    foreach ($rule in $rules) {
        if (-not $ports.ContainsKey([string]$rule.Name)) { continue }
        $apps = @($rule | Get-NetFirewallApplicationFilter -ErrorAction Stop)
        $filters = @($rule | Get-NetFirewallPortFilter -ErrorAction Stop)
        if ($apps.Count -eq 1 -and $filters.Count -eq 1 -and
            [string]::Equals([string]$apps[0].Program, $path, [StringComparison]::OrdinalIgnoreCase) -and
            [string]$rule.Direction -eq 'Inbound' -and [string]$rule.Action -eq 'Allow' -and
            [string]$filters[0].Protocol -in @('UDP', '17') -and
            @($filters[0].LocalPort).Count -eq 1 -and
            [string]$filters[0].LocalPort -eq $ports[[string]$rule.Name]) {
            $rule
        } else {
            Write-Warning 'A same-named phone rule belongs to another program or has been customized; it was preserved.'
        }
    }
}

function Remove-ProGoHomeFirewallRules([string]$ProGoExe) {
    # Refresh each candidate before deletion, preserving changes made since discovery.
    $names = @(Get-ProGoHomeFirewallRules $ProGoExe | ForEach-Object { $_.Name })
    foreach ($name in $names) {
        $current = @(Get-ProGoHomeFirewallRules $ProGoExe | Where-Object { $_.Name -eq $name })
        foreach ($rule in $current) { $rule | Remove-NetFirewallRule -ErrorAction Stop }
    }
    if (@(Get-ProGoHomeFirewallRules $ProGoExe).Count -ne 0) {
        throw 'Phone firewall cleanup is incomplete. Retry uninstall; program files have been kept.'
    }
}

function Test-ProGoFirewallAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } finally { $identity.Dispose() }
}

function Complete-ProGoHomeFirewallCleanup([string]$ProGoExe, [string]$RemovalScript) {
    $path = Get-ProGoFirewallPath $ProGoExe
    if (@(Get-ProGoHomeFirewallRules $path).Count -eq 0) { return }
    if (Test-ProGoFirewallAdministrator) {
        Remove-ProGoHomeFirewallRules $path
    } else {
        # Elevate only the firewall operation, never the per-user uninstaller.
        # The original user's explicit path survives alternate administrator credentials.
        if (-not [IO.Path]::IsPathRooted($RemovalScript) -or $RemovalScript.Contains('"') -or
            -not (Test-Path -LiteralPath $RemovalScript -PathType Leaf)) {
            throw 'The installed firewall helper is missing. Repair ProGo and retry uninstall.'
        }
        Write-Host 'Windows needs administrator approval to remove the phone connection rules.'
        $shell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $arguments = '-NoProfile -File "' + $RemovalScript + '" -Remove -ProGoExe "' + $path + '"'
        try {
            $process = Start-Process -FilePath $shell -ArgumentList $arguments -Verb RunAs -Wait -PassThru -ErrorAction Stop
            try { $code = $process.ExitCode } finally { $process.Dispose() }
        } catch {
            throw 'Administrator approval was cancelled or unavailable. Uninstall stopped; program files have been kept. Retry uninstall to finish.'
        }
        if ($code -ne 0) { throw 'Phone firewall cleanup failed. Uninstall stopped; program files have been kept. Retry uninstall to finish.' }
        if (@(Get-ProGoHomeFirewallRules $path).Count -ne 0) {
            throw 'Phone firewall rules remain. Uninstall stopped; program files have been kept.'
        }
    }
}
