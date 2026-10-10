param(
    [Parameter(Mandatory=$true)][string]$BackupDir,
    [ValidateSet("Program", "Data", "All")][string]$Scope = "Program",
    [switch]$ConfirmData,
    [int]$WaitPid = 0,
    [switch]$NoLaunch
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
try { . (Join-Path $PSScriptRoot 'Log-ProGo.ps1') } catch { }
$RestoreLog = Join-Path $InstallDir "progo-restore.log"

function U8($Base64) {
    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Base64))
}

function Write-RestoreLog($Message) {
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    try { Write-ProGoLog -Path $RestoreLog -Message $line } catch { }
    Write-Host $Message
}

function Show-UserMessage($Text, $Title) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        [void][System.Windows.Forms.MessageBox]::Show($Text, $Title, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch {
        Write-Host ("{0}: {1}" -f $Title, $Text)
    }
}

function Fail($Message) {
    Write-RestoreLog ("ERROR: " + $Message)
    throw $Message
}

function Wait-ProGoExit($TargetProcessId, $TimeoutMs) {
    if ($TargetProcessId -le 0) { return }

    Write-RestoreLog "Waiting for ProGo process to exit: PID $TargetProcessId"
    try {
        $process = Get-Process -Id $TargetProcessId -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            [void]$process.WaitForExit($TimeoutMs)
        }
    } catch {
        Write-RestoreLog "Wait process warning: $($_.Exception.Message)"
    }
}

function Assert-RestoreTree($Path, [switch]$Shallow) {
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Restore refuses links: $Path" }
    if ($item.PSIsContainer -and -not $Shallow) {
        foreach ($child in @(Get-ChildItem -LiteralPath $Path -Force)) { Assert-RestoreTree $child.FullName }
    }
}

function Get-RestoreChildren($Path, $Prefix) {
    foreach ($child in @(Get-ChildItem -LiteralPath $Path -Force | Sort-Object Name)) {
        if (($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Restore refuses links: $($child.FullName)" }
        $relative = $Prefix + '\' + $child.Name
        if ($child.PSIsContainer) {
            $relative + ':directory'
            Get-RestoreChildren $child.FullName $relative
        } else { $relative + ':' + (Get-FileHash -LiteralPath $child.FullName -Algorithm SHA256).Hash }
    }
}

function Get-RestoreState($Root, $Names) {
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($name in $Names) {
        $path = Join-Path $Root $name
        if (-not (Test-Path -LiteralPath $path)) { $lines.Add($name + ':absent'); continue }
        Assert-RestoreTree $path
        $item = Get-Item -LiteralPath $path -Force
        if ($item.PSIsContainer) {
            $lines.Add($name + ':directory')
            # Relative names must not depend on Windows expanding an 8.3 root alias.
            foreach ($line in @(Get-RestoreChildren $path $name)) { $lines.Add($line) }
        } else { $lines.Add($name + ':' + (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash) }
    }
    return ($lines -join "`n")
}

function Copy-RestoreRoots($Source, $Target, $Names) {
    New-Item -ItemType Directory -Path $Target -ErrorAction Stop | Out-Null
    foreach ($name in $Names) {
        $path = Join-Path $Source $name
        if (Test-Path -LiteralPath $path) {
            Assert-RestoreTree $path
            Copy-Item -LiteralPath $path -Destination (Join-Path $Target $name) -Recurse -Force -ErrorAction Stop
        }
    }
    $sourceState = Get-RestoreState $Source $Names
    $copiedState = Get-RestoreState $Target $Names
    if ($sourceState -cne $copiedState) {
        $changed = @(Compare-Object ($sourceState -split "`n") ($copiedState -split "`n") | ForEach-Object { $_.InputObject.Split(':')[0] } | Select-Object -Unique -First 4)
        throw ("Restore copy verification failed at $Target; roots: " + ($changed -join ', '))
    }
}

function Backup-BeforeRestore {
    $names = @('ProGo.exe','VERSION','scripts','ProGo.ico','settings.json','vault.enc.json')
    $before = Get-RestoreState $InstallDir $names
    $root = Join-Path $InstallDir 'backups'
    if (Test-Path -LiteralPath $root) { Assert-RestoreTree $root -Shallow } else { New-Item -ItemType Directory -Path $root | Out-Null }
    $path = Join-Path $root ('backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-pre-restore-' + [guid]::NewGuid().ToString('N'))
    Copy-RestoreRoots $InstallDir $path $names
    [ProGo.BackupIntegrity]::CopyPersonalArchives($InstallDir, $path)
    if ($before -cne (Get-RestoreState $path $names)) { throw 'Current state changed during protective backup.' }
    $version = ([IO.File]::ReadAllText((Join-Path $path 'VERSION'))).Trim()
    $manifest = @(
        'product=ProGo', "version=$version", "created=$([DateTimeOffset]::Now.ToString('o'))",
        'created_by=restore', 'backup_kind=pre-restore', 'reason=before-transactional-restore',
        "contains=$([ProGo.BackupIntegrity]::Contents($path))")
    $manifest += [ProGo.BackupIntegrity]::CompositionLines($path)
    Set-Content -LiteralPath (Join-Path $path 'manifest.txt') -Encoding UTF8 -Value $manifest
    [ProGo.BackupIntegrity]::Write($path)
    [ProGo.BackupIntegrity]::Validate($path)
    Write-RestoreLog "Protective backup verified: $path"
    return $path
}

function Assert-RestoreUnlocked($Names) {
    foreach ($name in $Names) {
        $path = Join-Path $InstallDir $name
        if (-not (Test-Path -LiteralPath $path)) { continue }
        Assert-RestoreTree $path
        $item = Get-Item -LiteralPath $path -Force
        $files = if ($item.PSIsContainer) { @(Get-ChildItem -LiteralPath $path -Force -Recurse -File) } else { @($item) }
        foreach ($file in $files) {
            $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $stream.Dispose()
        }
    }
}

function Move-RestoreRoot($From, $To) {
    if (Test-Path -LiteralPath $To) { throw "Restore target already exists: $To" }
    $item = Get-Item -LiteralPath $From -Force
    if ($item.PSIsContainer) { [IO.Directory]::Move($From, $To) }
    else { [IO.File]::Move($From, $To) }
}

function Commit-Restore {
    foreach ($name in $RestoreNames) {
        $entry = [pscustomobject]@{ Name=$name; OldMoved=$false; NewMoved=$false }
        $Transaction.Journal.Add($entry)
        $target = Join-Path $InstallDir $name
        if (Test-Path -LiteralPath $target) {
            Move-RestoreRoot $target (Join-Path $Transaction.Previous $name)
            $entry.OldMoved = $true
        }
        Move-RestoreRoot (Join-Path $Transaction.Stage $name) $target
        $entry.NewMoved = $true
    }
}

function Undo-Restore {
    $failures = New-Object System.Collections.Generic.List[string]
    for ($i = $Transaction.Journal.Count - 1; $i -ge 0; $i--) {
        $entry = $Transaction.Journal[$i]
        try {
            $target = Join-Path $InstallDir $entry.Name
            if ($entry.NewMoved -and (Test-Path -LiteralPath $target)) {
                Assert-RestoreTree $target
                Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction Stop
            }
            if ($entry.OldMoved) { Move-RestoreRoot (Join-Path $Transaction.Previous $entry.Name) $target }
        } catch { $failures.Add($entry.Name + ': ' + $_.Exception.Message) }
    }
    if ($failures.Count -gt 0) { throw ('Rollback failed: ' + ($failures -join '; ')) }
    if ((Get-RestoreState $InstallDir $RestoreNames) -cne $Transaction.Before) { throw 'Rollback verification failed.' }
    $Transaction.SafeToRemove = $true
}

function Test-RestoredApplication {
    $exe = Join-Path $InstallDir 'ProGo.exe'
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $exe; $info.Arguments = '--self-check'; $info.WorkingDirectory = $InstallDir
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $process = [Diagnostics.Process]::Start($info)
    try {
        if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw 'Restored application self-check timed out.' }
        if ($process.ExitCode -ne 0) { throw "Restored application self-check failed: exit=$($process.ExitCode)" }
    } finally { $process.Dispose() }
}

function Start-ProGo {
    if ($NoLaunch) {
        Write-RestoreLog "Launch skipped by -NoLaunch."
        return
    }

    $exe = Join-Path $InstallDir "ProGo.exe"
    if (-not (Test-Path $exe)) {
        Fail "Cannot launch ProGo, file not found: $exe"
    }

    $process = Start-Process -FilePath $exe -WorkingDirectory $InstallDir -PassThru
    Start-Sleep -Seconds 2
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($null -eq $running -or $running.HasExited) {
        Fail "Restored ProGo did not stay running."
    }

    Write-RestoreLog "Restored ProGo started: PID $($process.Id)"
}

. (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1')
$Maintenance = [ProGo.MaintenanceOperation]::Enter()
$Prepared = $null
$Transaction = $null
$Protection = $null
try {
    . (Join-Path $PSScriptRoot 'BackupIntegrity-ProGo.ps1')
    # Reject a data-replacement request without consent before touching installed state.
    [void][ProGo.BackupIntegrity]::RestoreNames($BackupDir, $Scope, [bool]$ConfirmData)
    # The helper takes its own copy before acknowledgement. The UI may then dispose
    # its prepared copy while this operation waits for the old process to exit.
    $Prepared = [ProGo.BackupIntegrity]::Prepare($BackupDir)
    $BackupDir = $Prepared.Path
    $RestoreNames = [ProGo.BackupIntegrity]::RestoreNames($BackupDir, $Scope, [bool]$ConfirmData)
    Write-RestoreLog "ProGo restore started."
    Write-RestoreLog "Restore scope=$Scope; files=$($RestoreNames -join ',')"

    if (-not (Test-Path $BackupDir)) {
        Fail "Backup folder does not exist: $BackupDir"
    }

    $manifest = Join-Path $BackupDir "manifest.txt"
    if (-not (Test-Path $manifest)) {
        Fail "Backup manifest not found: $manifest"
    }

    [ProGo.MaintenanceOperation]::ConfirmHandoff()
    Wait-ProGoExit -TargetProcessId $WaitPid -TimeoutMs 30000
    [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
    [ProGo.MaintenanceOperation]::RequireProxyCleanupCompleted($InstallDir)
    [ProGo.BackupIntegrity]::Validate($BackupDir)
    Assert-RestoreTree $InstallDir -Shallow
    Assert-RestoreUnlocked $RestoreNames
    $Protection = Backup-BeforeRestore
    $txnRoot = Join-Path $InstallDir ('restore-txn-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $txnRoot | Out-Null
    $Transaction = [pscustomobject]@{
        Root=$txnRoot; Stage=(Join-Path $txnRoot 'stage'); Previous=(Join-Path $txnRoot 'previous');
        Before=(Get-RestoreState $InstallDir $RestoreNames); Journal=(New-Object System.Collections.Generic.List[object]);
        SafeToRemove=$false; Complete=$false
    }
    New-Item -ItemType Directory -Path $Transaction.Previous | Out-Null
    Copy-RestoreRoots $BackupDir $Transaction.Stage $RestoreNames
    $expected = Get-RestoreState $BackupDir $RestoreNames
    if ($Transaction.Before -cne (Get-RestoreState $Protection $RestoreNames)) { throw 'Current state changed after protective backup.' }
    Assert-RestoreUnlocked $RestoreNames
    # Recheck recorded evidence after staging I/O, immediately before replacing roots.
    [ProGo.BackupIntegrity]::Validate($BackupDir)
    [ProGo.BackupIntegrity]::Validate($Protection)
    if ((Get-RestoreState $Transaction.Stage $RestoreNames) -cne $expected) { throw 'Staged payload changed before commit.' }
    Commit-Restore
    if ((Get-RestoreState $InstallDir $RestoreNames) -cne $expected) { throw 'Installed payload does not match the prepared copy.' }
    Test-RestoredApplication
    if ((Get-RestoreState $InstallDir $RestoreNames) -cne $expected) { throw 'Self-check changed the selected payload.' }
    Start-ProGo
    $Transaction.Complete = $true; $Transaction.SafeToRemove = $true

    Write-RestoreLog "ProGo restore completed."
    Show-UserMessage ( (U8 "0JLRi9Cx0YDQsNC90L3Ri9C1INGE0LDQudC70Ysg0LLQvtGB0YHRgtCw0L3QvtCy0LvQtdC90YsuINCh0L7RgdGC0LDQsjog") + ($RestoreNames -join ", ") + "`n" + (U8 "0JfQsNGJ0LjRgtC90LDRjyDQutC+0L/QuNGPOiA=") + $Protection) (U8 "0J7RgtC60LDRgiBQcm9Hbw==")
} catch {
    $failure = $_.Exception.Message
    $outcome = 'Installed payload was not changed.'
    $rollbackFailed = $false
    if ($null -ne $Transaction -and -not $Transaction.Complete -and $Transaction.Journal.Count -gt 0) {
        try {
            [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
            Undo-Restore
            $outcome = 'Previous selected payload restored and verified.'
            try { Start-ProGo } catch { $outcome += ' Previous application could not restart: ' + $_.Exception.Message }
        } catch {
            $rollbackFailed = $true
            $outcome = 'Rollback incomplete. Recovery files retained: ' + $Transaction.Root + '; ' + $Protection + '. ' + $_.Exception.Message
        }
    } elseif ($null -ne $Transaction -and $Transaction.Complete) { $outcome = 'Restore completed; only completion reporting failed.' }
    if ($null -ne $Prepared) { try { Write-RestoreLog ("RESTORE FAILED: $failure $outcome") } catch { Write-Host ("RESTORE FAILED: $failure $outcome") } }
    # Keep pre-handoff refusal free of installed-state side effects.
    if ($null -ne $Prepared) {
        $notice = if ($rollbackFailed) { U8 "0J3QtSDRg9C00LDQu9C+0YHRjCDQv9C+0LvQvdC+0YHRgtGM0Y4g0LLQtdGA0L3Rg9GC0Ywg0L/RgNC10LbQvdC40LUg0YTQsNC50LvRiy4g0J3QtSDQt9Cw0L/Rg9GB0LrQsNC50YLQtSBQcm9HbyDQtNC+INCy0L7RgdGB0YLQsNC90L7QstC70LXQvdC40Y8uINCh0L7RhdGA0LDQvdC10L3RiyDQt9Cw0YnQuNGC0L3QsNGPINC60L7Qv9C40Y8g0Lgg0YDQsNCx0L7Rh9Cw0Y8g0L/QsNC/0LrQsCDQstC+0YHRgdGC0LDQvdC+0LLQu9C10L3QuNGPOyDQuNGFINC/0YPRgtC4INGD0LrQsNC30LDQvdGLINCyIHByb2dvLXJlc3RvcmUubG9nLiDQl9Cw0YnQuNGC0L3QsNGPINC60L7Qv9C40Y86IA==" } else { U8 "0JLQvtGB0YHRgtCw0L3QvtCy0LvQtdC90LjQtSDQvdC1INCy0YvQv9C+0LvQvdC10L3Qvi4g0J/RgNC10LbQvdC40LUg0YTQsNC50LvRiyDRgdC+0YXRgNCw0L3QtdC90Ysg0LjQu9C4INCy0L7Qt9Cy0YDQsNGJ0LXQvdGLLiDQn9C+0LTRgNC+0LHQvdC+0YHRgtC4IOKAlCDQsiBwcm9nby1yZXN0b3JlLmxvZy4g0JfQsNGJ0LjRgtC90LDRjyDQutC+0L/QuNGPOiA=" }
        if ($null -ne $Transaction -and $Transaction.Complete) { $notice = U8 "0JLQvtGB0YHRgtCw0L3QvtCy0LvQtdC90LjQtSDQt9Cw0LLQtdGA0YjQtdC90L4sINC90L4g0L3QtSDRg9C00LDQu9C+0YHRjCDQt9Cw0L/QuNGB0LDRgtGMINC40LvQuCDQv9C+0LrQsNC30LDRgtGMINC40YLQvtCz0L7QstC+0LUg0YHQvtC+0LHRidC10L3QuNC1LiDQn9C+0LTRgNC+0LHQvdC+0YHRgtC4IOKAlCDQsiBwcm9nby1yZXN0b3JlLmxvZy4g0JfQsNGJ0LjRgtC90LDRjyDQutC+0L/QuNGPOiA=" }
        Show-UserMessage ($notice + $Protection) 'ProGo'
    }
    throw
} finally {
    try {
        if ($null -ne $Transaction -and ($Transaction.SafeToRemove -or $Transaction.Journal.Count -eq 0)) {
            try { Assert-RestoreTree $Transaction.Root; Remove-Item -LiteralPath $Transaction.Root -Recurse -Force -ErrorAction Stop }
            catch { Write-Host ('Restore temporary cleanup warning: ' + $_.Exception.Message) }
        }
        if ($null -ne $Prepared) { try { $Prepared.Dispose() } catch { Write-Host ('Prepared-copy cleanup warning: ' + $_.Exception.Message) } }
    } finally { $Maintenance.Dispose() }
}
