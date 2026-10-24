Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer does not parse.' }
# Load pure preflight/staging helpers only; Enter/Stop remain mocked below.
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$boundary = @($ast.EndBlock.Statements | Where-Object {
    $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir'
})
if ($boundary.Count -ne 1) { throw 'Installer maintenance boundary is ambiguous.' }
# Exercise the actual production write/try/finally/launch statements, not a copy
# of their logic. The adapters below replace every OS/IPC/shortcut/process action.
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-installer-owner-' + [Guid]::NewGuid().ToString('N'))
$oldLocal = $env:LOCALAPPDATA
function Check-InstallerOwner($Condition, $Name) {
    if (-not $Condition) { throw ('Installer maintenance test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $ReleaseDir = Join-Path $work 'payload'
    Microsoft.PowerShell.Management\Copy-Item -LiteralPath (Join-Path $Root 'release') -Destination $ReleaseDir -Recurse
    $PayloadScripts = Join-Path $ReleaseDir 'scripts'
    # This shim is copied only inside the fixture. It never calls real known folders.
    @'
function Test-ProGoExistingInstallation { param($InstallDirectory) return $true }
function Initialize-ProGoShortcuts {
    param($InstallDirectory, $WasInstalled, $NoStartup, $NoStartMenu)
    if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Shortcut escaped maintenance.' }
    $FixtureState.Events.Add('shortcuts')
    if ($FixtureState.Mode -eq 'shortcut-fail') { throw 'fixture-shortcut-fail' }
}
'@ | Set-Content -LiteralPath (Join-Path $PayloadScripts 'Shortcuts-ProGo.ps1') -Encoding UTF8
    $Payload = Get-ProGoInstallPayload -Directory $ReleaseDir
    $Exe = Join-Path $ReleaseDir 'ProGo.exe'
    $VersionFile = Join-Path $ReleaseDir 'VERSION'
    $NoStartup = $false; $NoStartMenuShortcut = $false
    function Run-InstallerOwnerCase($Mode) {
        $case = Join-Path $work $Mode
        New-Item -ItemType Directory -Path (Join-Path $case 'ProGo') | Out-Null
        $env:LOCALAPPDATA = $case
        $data = Join-Path $case 'ProGo\settings.json'
        [IO.File]::WriteAllText($data, 'synthetic-user-data-unchanged')
        $before = [IO.File]::ReadAllBytes($data)
        $FixtureState = @{ Mode = $Mode; LeaseHeld = $false; Stopped = $false; Disposes = 0; Copies = 0; SuccessMessage = $false; Events = (New-Object 'System.Collections.Generic.List[string]') }
        function Enter-ProGoInstallerMaintenance {
            $FixtureState.Events.Add('enter')
            if ($Mode -eq 'busy') { throw 'fixture-busy' }
            $FixtureState.LeaseHeld = $true
            $lease = [pscustomobject]@{ State = $FixtureState }
            $lease | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
                $this.State.Disposes++; $this.State.LeaseHeld = $false
                $this.State.Events.Add('dispose')
            }
            return $lease
        }
        function Stop-ProGoInstallerApplication($InstallDirectory) {
            if (-not $FixtureState.LeaseHeld) { throw 'Stop escaped maintenance.' }
            $FixtureState.Events.Add('stop')
            if ($Mode -eq 'stop-fail' -or $Mode -eq 'retry-stop') { throw ('fixture-' + $Mode) }
            $FixtureState.Stopped = $true
        }
        function Copy-Item {
            param($Path, $Destination, [switch]$Force, [switch]$Recurse)
            if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Copy escaped maintenance.' }
            $FixtureState.Copies++; $FixtureState.Events.Add('copy')
            if ($Mode -eq 'copy-fail') { throw 'fixture-copy-fail' }
            Microsoft.PowerShell.Management\Copy-Item -LiteralPath $Path -Destination $Destination -Force:$Force -Recurse:$Recurse
        }
        function Start-Process {
            param($FilePath, $WorkingDirectory, $ArgumentList)
            if ($FixtureState.LeaseHeld -or $FixtureState.Disposes -ne 1) { throw 'Launch preceded release.' }
            $FixtureState.Events.Add('launch')
            if ($Mode -eq 'launch-fail') { throw 'fixture-launch-fail' }
        }
        function Write-Host($Object) {
            # Existing installer status text contains its path. Capture only the
            # success boolean; never emit fixture/user temp paths into CI output.
            if ($Object -like 'Install OK:*') { $FixtureState.SuccessMessage = $true }
        }
        $Launch = $Mode -eq 'launch' -or $Mode -eq 'launch-fail'
        $savedVersion = [IO.File]::ReadAllBytes($VersionFile)
        if ($Mode -eq 'stage-changed') { [IO.File]::WriteAllText($VersionFile, '999.998.997') }
        $caught = $false
        try { & $body } catch {
            if ($Mode -eq 'stage-changed') {
                if (-not $_.Exception.Message.Contains('Installer payload changed or stage copy is corrupt.')) { throw }
            } elseif (-not $_.Exception.Message.Contains('fixture-' + $Mode)) { throw }
            $caught = $true
        }
        [IO.File]::WriteAllBytes($VersionFile, $savedVersion)
        Check-InstallerOwner (-not $FixtureState.LeaseHeld) ($Mode + ' releases ownership')
        Check-InstallerOwner (([Convert]::ToBase64String($before)) -ceq ([Convert]::ToBase64String([IO.File]::ReadAllBytes($data)))) ($Mode + ' preserves user bytes')
        if ($Mode -eq 'busy') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 0 -and $FixtureState.Copies -eq 0 -and $FixtureState.Events.Count -eq 1) 'busy refuses all writes'
        } elseif ($Mode -eq 'stage-changed') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 1 -and $FixtureState.Copies -eq 0 -and
                ($FixtureState.Events -join ',') -eq 'enter,stop,dispose') 'failed staging releases without runtime writes'
        } elseif ($Mode -eq 'stop-fail' -or $Mode -eq 'retry-stop') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 1 -and $FixtureState.Copies -eq 0 -and ($FixtureState.Events -join ',') -eq 'enter,stop,dispose') 'stop refusal releases without writes'
            if ($Mode -eq 'retry-stop') {
                # A new explicit attempt must acquire and release a new owner too.
                try { & $body; throw 'Retry unexpectedly succeeded.' } catch {
                    if (-not $_.Exception.Message.Contains('fixture-retry-stop')) { throw }
                }
                Check-InstallerOwner ($FixtureState.Disposes -eq 2 -and $FixtureState.Copies -eq 0) 'retry retains refusal and releases new owner'
            }
        } else {
            Check-InstallerOwner ($FixtureState.Disposes -eq 1 -and $FixtureState.Events[0] -eq 'enter' -and $FixtureState.Events[1] -eq 'stop' -and $FixtureState.Copies -gt 0) 'copy lifecycle ordering'
            Check-InstallerOwner ($caught -eq ($Mode -in @('copy-fail', 'shortcut-fail', 'launch-fail'))) ($Mode + ' propagates failure')
            if ($Mode -eq 'launch' -or $Mode -eq 'launch-fail') {
                Check-InstallerOwner ($FixtureState.Events[$FixtureState.Events.Count - 2] -eq 'dispose' -and $FixtureState.Events[$FixtureState.Events.Count - 1] -eq 'launch') 'launch after release'
            } else { Check-InstallerOwner ($FixtureState.Events[$FixtureState.Events.Count - 1] -eq 'dispose') 'final release' }
        }
        Check-InstallerOwner ($FixtureState.SuccessMessage -eq ($Mode -in @('complete', 'launch', 'launch-fail'))) ($Mode + ' never reports install success before admitted copies/shortcuts')
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer maintenance ' + $Mode)
    }
    foreach ($mode in @('complete', 'busy', 'stop-fail', 'copy-fail', 'shortcut-fail', 'launch', 'launch-fail', 'retry-stop', 'stage-changed')) { Run-InstallerOwnerCase $mode }
    Write-Host 'Installer maintenance tests PASS: 9'
} finally {
    $env:LOCALAPPDATA = $oldLocal
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
