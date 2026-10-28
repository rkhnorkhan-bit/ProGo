param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: staged uninstall requires isolated Windows CI'; return }
$repo = Split-Path -Parent $Scripts
$payload = Join-Path $repo 'release'
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-uninstall-stage-tests-' + [guid]::NewGuid().ToString('N'))
$script:StageChildrenSettled = $true
$junction = $null
function Check-Stage($condition, [string]$message) { if (-not $condition) { throw ('Uninstall stage: ' + $message) } }
function Quote-Stage([string]$value) { return "'" + $value.Replace("'", "''") + "'" }
function Write-StageDriver([string]$Source, [string]$Boundary, [string]$Adapters, [string]$Destination, [string]$Prefix) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($Source, [ref]$tokens, [ref]$errors)
    Check-Stage ($errors.Count -eq 0) 'consumer parses'
    $statement = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -ceq $Boundary
    })
    Check-Stage ($statement.Count -eq 1) 'one actual consumer boundary'
    $text = [IO.File]::ReadAllText($Source).Insert($statement[0].Extent.StartOffset, $Prefix + "`r`n" + $Adapters + "`r`n")
    [IO.File]::WriteAllText($Destination, $text, (New-Object Text.UTF8Encoding($true)))
}
function Invoke-StageHost([string]$Driver, [string]$Arguments = '') {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
    $info.Arguments = '-NoProfile -File "' + $Driver + '" ' + $Arguments
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $script:StageChildrenSettled = $false
    $process = $null; $out = $null; $err = $null; $settled = $false
    try {
        $process = [Diagnostics.Process]::Start($info)
        $out = $process.StandardOutput.ReadToEndAsync(); $err = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) { throw 'Uninstall stage child deadline.' }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($out, $err), 5000)) { throw 'Uninstall stage drain deadline.' }
        $settled = $true
        return [pscustomobject]@{ Code = $process.ExitCode; Output = $out.Result; Error = $err.Result; Pid = $process.Id }
    } finally {
        if ($null -ne $process) {
            if (-not $settled) {
                try {
                    if (-not $process.HasExited) { $process.Kill() }
                    # Kill is only a request. Settlement and pipe drain are bounded.
                    if ($process.WaitForExit(5000) -and $null -ne $out -and $null -ne $err) {
                        $settled = [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($out, $err), 5000)
                    }
                } catch { $settled = $false }
            }
            $process.Dispose()
        }
        $script:StageChildrenSettled = $settled
    }
}
$installAdapters = @'
. (Join-Path $Fixture.Scripts 'Layout-ProGo.ps1')
function Get-ProGoInstallerLayout {
    return Resolve-ProGoRecoveryLayoutCore -DataBase $Fixture.DataBase -ProgramBase $Fixture.ProgramBase -RuntimeRoot $Fixture.Runtime -SeparateIdentityConfirmed $true
}
function Enter-ProGoInstallerMaintenance {
    $lease = [pscustomobject]@{}; $lease | Add-Member ScriptMethod Dispose {}; return $lease
}
function Stop-ProGoInstallerApplication($InstallDirectory, $DataDirectory) {}
function Invoke-ProGoInstallerSelfCheck($Info) {
    [void][IO.Directory]::CreateDirectory($Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT']); return 0
}
function Initialize-ProGoInstallerShortcuts($RuntimeRoot, $WasInstalled, $NoStartup, $NoStartMenu) {
    return Initialize-ProGoShortcuts -InstallDirectory $RuntimeRoot -WasInstalled $WasInstalled -NoStartup $NoStartup -NoStartMenu $NoStartMenu -ProgramsDirectory $Fixture.Menu -StartupDirectory $Fixture.Startup -CaptureStartupEvidence
}
'@
$uninstallAdapters = @'
function Get-ProGoUninstallLayout {
    return Resolve-ProGoRecoveryLayoutCore -DataBase $Fixture.DataBase -ProgramBase $Fixture.ProgramBase -RuntimeRoot $Fixture.Runtime -SeparateIdentityConfirmed $true
}
function Enter-ProGoUninstallMaintenance {
    $lease = [pscustomobject]@{}; $lease | Add-Member ScriptMethod Dispose {}; return $lease
}
function Stop-ProGoUninstallApplication($RuntimeRoot, $DataRoot) {
    if ($Fixture.Mode -ceq 'pins') {
        $stageScripts = Join-Path $Fixture.Runtime 'backups/uninstall-stage/scripts'
        $writeRefused = $false; $renameRefused = $false
        try { [IO.File]::WriteAllText((Join-Path $stageScripts 'MaintenanceOperation.cs'), 'foreign replacement') } catch [IO.IOException] { $writeRefused = $true }
        try { [IO.Directory]::Move($stageScripts, $stageScripts + '-foreign') } catch [IO.IOException] { $renameRefused = $true }
        if (-not $writeRefused -or -not $renameRefused) { throw 'Stage physical pin failure.' }
        Write-Host 'STAGE_PHYSICAL_PINS_CONFIRMED'
    }
}
function Complete-ProGoUninstallFirewall($RuntimeRoot) {}
function Read-Host($Prompt) { throw 'Unexpected data consent prompt.' }
function New-ProGoUninstallShortcuts($RuntimeRoot) {
    $manager = New-Object ProGo.ApplicationShortcuts($RuntimeRoot, $Fixture.Menu, $Fixture.Startup)
    $proxy = [pscustomobject]@{ Manager = $manager }
    $proxy | Add-Member ScriptMethod RemoveOwnedShortcuts { $this.Manager.RemoveOwnedShortcuts() }
    $proxy | Add-Member ScriptMethod RemoveRuntimeFile {
        param($relative, $hash)
        $this.Manager.RemoveRuntimeFile($relative, $hash)
        if ($Fixture.Fault -and $relative -ceq 'scripts/Layout-ProGo.ps1') {
            foreach ($leaf in @('Install-ProGo.ps1', 'Layout-ProGo.ps1', 'Shortcuts-ProGo.ps1')) {
                if ([IO.File]::Exists((Join-Path $Fixture.Runtime ('scripts/' + $leaf)))) { throw 'Late fault boundary did not remove helpers.' }
            }
            throw 'Synthetic late uninstall fault after all three helper deletions.'
        }
    }
    return $proxy
}
'@
try {
    [void][IO.Directory]::CreateDirectory($work)
    . (Join-Path $Scripts 'Install-ProGo.ps1') -FunctionsOnly
    . (Join-Path $Scripts 'Layout-ProGo.ps1')
    foreach ($mode in @('retry', 'corrupt', 'replaced-entry', 'foreign', 'reparse', 'pending', 'runtime-conflict', 'pins', 'retry-replaced', 'dependency-code')) {
        $case = Join-Path $work $mode
        $Fixture = @{ Scripts = $Scripts; DataBase = (Join-Path $case 'data'); ProgramBase = (Join-Path $case 'program'); Menu = (Join-Path $case 'menu'); Startup = (Join-Path $case 'startup'); Fault = $true; Mode = $mode }
        $Fixture.Runtime = Join-Path $Fixture.ProgramBase 'ProGo'; $data = Join-Path $Fixture.DataBase 'ProGo'
        foreach ($dir in @($Fixture.Runtime, $data, $Fixture.Menu, $Fixture.Startup)) { [void][IO.Directory]::CreateDirectory($dir) }
        $private = @{}
        foreach ($leaf in @('vault.enc', 'settings.json', 'private.log')) {
            $path = Join-Path $data $leaf; [IO.File]::WriteAllText($path, 'synthetic private ' + $leaf)
            $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        $prefix = '$Fixture = ' + (Quote-Stage ($Fixture | ConvertTo-Json -Compress)) + ' | ConvertFrom-Json'
        $install = Join-Path $case 'InstallConsumer.ps1'
        Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $installAdapters $install $prefix
        $installed = Invoke-StageHost $install ('-NoStartup -PayloadDirectory "' + $payload + '"')
        Check-Stage ($installed.Code -eq 0) ('fresh install succeeds: ' + $installed.Error)
        $inventory = @(Get-ProGoUninstallInventory $Fixture.Runtime)
        $foreignRuntime = Join-Path $Fixture.Runtime 'foreign.txt'; [IO.File]::WriteAllText($foreignRuntime, 'foreign runtime')
        $foreignMenu = Join-Path $Fixture.Menu 'ProGo/foreign.txt'; [IO.File]::WriteAllText($foreignMenu, 'foreign menu')
        $foreignLink = Join-Path $Fixture.Startup 'ProGo.lnk'
        $shell = New-Object -ComObject WScript.Shell
        try {
            $link = $shell.CreateShortcut($foreignLink); $link.TargetPath = Join-Path $payload 'ProGo.exe'; $link.Save()
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link)
        } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
        foreach ($path in @($foreignRuntime, $foreignMenu, $foreignLink)) { $private[$path] = (Get-FileHash -LiteralPath $path).Hash }
        $proofs = @{}
        foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Recurse -File) { $proofs[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $first = Join-Path $case 'InitialUninstall.ps1'
        Write-StageDriver (Join-Path $Fixture.Runtime 'scripts/Uninstall-ProGo.ps1') '$Layout' $uninstallAdapters $first $prefix
        $fault = Invoke-StageHost $first
        Check-Stage ($fault.Code -ne 0 -and $fault.Error.Contains('Synthetic late uninstall fault') -and -not $fault.Output.Contains('Uninstall OK')) 'real late deletion is failure, never success'
        foreach ($leaf in @('Install-ProGo.ps1', 'Layout-ProGo.ps1', 'Shortcuts-ProGo.ps1')) {
            Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime ('scripts/' + $leaf)))) 'runtime helper actually absent'
        }
        $stage = Join-Path $Fixture.Runtime 'backups/uninstall-stage'
        $entry = Join-Path $stage 'scripts/Uninstall-ProGo.ps1'
        $retryEntry = Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'
        Check-Stage ([IO.File]::Exists($retryEntry) -and $fault.Output.Contains($retryEntry)) 'stable trusted retry entry reported and retained'
        $Fixture.Fault = $false
        $prefix = '$Fixture = ' + (Quote-Stage ($Fixture | ConvertTo-Json -Compress)) + ' | ConvertFrom-Json'
        $prefix += "`r`nif ('ProGo.ApplicationShortcuts' -as [type]) { throw 'Retry inherited shortcut type.' }"
        # The fresh consumer comes from retained installed entrypoint code outside
        # the untrusted dependency stage. Only OS, root and fault adapters are inserted.
        $retry = Join-Path $case 'RetryConsumer.ps1'
        Write-StageDriver $retryEntry '$Layout' $uninstallAdapters $retry $prefix
        $junction = $null
        $canary = Join-Path $case 'foreign-execution-canary.txt'
        $canaryCode = '[IO.File]::WriteAllText(' + (Quote-Stage $canary) + ", 'executed'); Write-Output 'STAGE_FOREIGN_EXECUTION_CANARY'; throw 'FOREIGN_CODE_EXECUTED'"
        # RetryConsumer was generated from retained trusted code BEFORE mutation.
        # This tests that trusted caller's refusal, not malicious-entry self-defense.
        switch ($mode) {
            'corrupt' { [IO.File]::AppendAllText((Join-Path $stage 'scripts/MaintenanceOperation.cs'), '// corrupt') }
            'retry-replaced' { [IO.File]::AppendAllText($retryEntry, '# changed retained entry bytes') }
            'replaced-entry' { [IO.File]::WriteAllText($entry, $canaryCode) }
            'dependency-code' { [IO.File]::WriteAllText((Join-Path $stage 'scripts/Firewall-ProGo.ps1'), $canaryCode) }
            'foreign' { [IO.File]::WriteAllText((Join-Path $stage 'foreign.txt'), 'foreign stage sibling') }
            'reparse' {
                $stageScripts = Join-Path $stage 'scripts'; $saved = Join-Path $case 'saved-stage-scripts'
                [IO.Directory]::Move($stageScripts, $saved)
                $junction = New-Item -ItemType Junction -Path $stageScripts -Target $saved
            }
            'pending' {
                $pendingPath = Join-Path $Fixture.Runtime ('backups/installer-journal-' + [guid]::NewGuid().ToString('N'))
                [void][IO.Directory]::CreateDirectory($pendingPath)
            }
            'runtime-conflict' { [IO.File]::WriteAllText((Join-Path $Fixture.Runtime 'scripts/Firewall-ProGo.ps1'), 'foreign replacement') }
        }
        $expectedRefusal = switch ($mode) {
            'corrupt' { 'Uninstall stage bytes differ from installed ownership.' }
            'replaced-entry' { 'Uninstall stage bytes differ from installed ownership.' }
            'dependency-code' { 'Uninstall stage bytes differ from installed ownership.' }
            'foreign' { 'Uninstall stage contains a foreign leaf.' }
            'reparse' { 'Installer stage refuses redirected paths.' }
            'pending' { 'Installer pending journal requires verified recovery before a new installation.' }
            'runtime-conflict' { 'Uninstall requires a verified installed runtime inventory.' }
            'retry-replaced' { 'Uninstall retry entry differs from installed ownership.' }
            default { $null }
        }
        $protectedStageRoot = if ($mode -ceq 'reparse') { $saved } else { Join-Path $stage 'scripts' }
        $stageFiles = @{}
        foreach ($file in Get-ChildItem -LiteralPath $protectedStageRoot -File -Force) { $stageFiles[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $stageFiles[$retryEntry] = (Get-FileHash -LiteralPath $retryEntry).Hash
        if ($mode -ceq 'foreign') { $stageFiles[(Join-Path $stage 'foreign.txt')] = (Get-FileHash -LiteralPath (Join-Path $stage 'foreign.txt')).Hash }
        if ($mode -ceq 'reparse') { $junctionTarget = (Get-Item -LiteralPath $junction.FullName -Force).Target | ConvertTo-Json -Compress }
        $remaining = @{}
        foreach ($entryRecord in $inventory) {
            $path = Join-Path $Fixture.Runtime $entryRecord.Relative
            if ([IO.File]::Exists($path)) { $remaining[$path] = (Get-FileHash -LiteralPath $path).Hash }
        }
        $result = Invoke-StageHost $retry
        if ($mode -ceq 'pins') { Check-Stage ($fault.Output.Contains('STAGE_PHYSICAL_PINS_CONFIRMED') -and $result.Output.Contains('STAGE_PHYSICAL_PINS_CONFIRMED')) 'actual native file and directory pins in both fresh consumers' }
        Check-Stage ($result.Pid -ne $fault.Pid -and $fault.Pid -ne $installed.Pid) 'independent fresh processes'
        if ($mode -in @('retry', 'pins')) {
            Check-Stage ($result.Code -eq 0 -and $result.Output.Contains('Uninstall OK')) ('late fresh retry succeeds: ' + $result.Error)
            foreach ($entryRecord in $inventory) { Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $entryRecord.Relative))) 'all admitted runtime leaves removed' }
            Check-Stage (@(Get-ProGoUninstallInventory $Fixture.Runtime).Count -eq $inventory.Count) 'retained recovery evidence admits absent leaves'
        } else {
            Check-Stage ($result.Code -ne 0 -and -not $result.Output.Contains('Uninstall OK')) 'unsafe stage or runtime refuses without false success'
            Check-Stage ($result.Error.Contains($expectedRefusal)) ('specific refusal: ' + $mode)
            foreach ($path in $stageFiles.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $stageFiles[$path]) 'modified and foreign stage/retry bytes preserved' }
            Check-Stage (@(Get-ChildItem -LiteralPath $protectedStageRoot -File -Force).Count -eq 9) 'stage or junction target inventory preserved'
            if ($mode -ceq 'reparse') {
                $currentJunction = Get-Item -LiteralPath $junction.FullName -Force
                Check-Stage (($currentJunction.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -and
                    ($currentJunction.Target | ConvertTo-Json -Compress) -ceq $junctionTarget) 'junction and its original target preserved'
            }
            if ($mode -ceq 'pending') { Check-Stage ([IO.Directory]::Exists($pendingPath) -and @(Get-ChildItem -LiteralPath $pendingPath -Force).Count -eq 0) 'pending refusal preserves empty journal' }
            foreach ($path in $remaining.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $remaining[$path]) 'refusal preserves every surviving runtime byte' }
        }
        Check-Stage (-not [IO.File]::Exists($canary) -and -not $result.Output.Contains('STAGE_FOREIGN_EXECUTION_CANARY') -and -not $result.Error.Contains('FOREIGN_CODE_EXECUTED')) 'foreign stage code never executed'
        foreach ($path in $private.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'private bytes and foreign links/siblings retained' }
        foreach ($path in $proofs.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $proofs[$path]) 'original recovery evidence unchanged' }
        if ($null -ne $junction) { [IO.Directory]::Delete($junction.FullName, $false); $junction = $null }
        Write-Host ('PASS: staged uninstall fresh-process ' + $mode)
    }
} finally {
    if ($script:StageChildrenSettled) {
        if ($null -ne $junction) { [IO.Directory]::Delete($junction.FullName, $false); $junction = $null }
        if ([IO.Directory]::Exists($work)) { Remove-ProGoInstallerOwnedDirectory $work }
    } else { Write-Warning ('Child settlement/drain is unknown; fixture retained: ' + $work) }
}
