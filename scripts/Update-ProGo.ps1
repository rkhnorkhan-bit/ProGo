param(
    [int]$WaitPid = 0,
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [string]$RemoteVersionUrl = "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/VERSION",
    [switch]$NoLaunch,
    [switch]$Force
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$UpdateLog = Join-Path $InstallDir "update.log"
$LegacyUpdateLog = Join-Path $InstallDir "progo-update.log"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$TransactionRoot = Join-Path $env:TEMP ("ProGo-update-" + $Timestamp)
$StageDir = Join-Path $TransactionRoot "stage"
$SourceDir = Join-Path $TransactionRoot "source"
$BackupsDir = Join-Path $InstallDir "backups"
$LocalVersionFile = Join-Path $InstallDir "VERSION"
$RemoteVersion = $null
$LocalVersion = $null
$BackupDir = $null
$MainWasChanged = $false

function U8($Base64) {
    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Base64))
}

function Write-UpdateLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $UpdateLog -Value $line -Encoding UTF8
    Add-Content -Path $LegacyUpdateLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Copy-LogToClipboard {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $text = ""
        if (Test-Path $UpdateLog) {
            $text = Get-Content -Raw -Path $UpdateLog
        } elseif (Test-Path $LegacyUpdateLog) {
            $text = Get-Content -Raw -Path $LegacyUpdateLog
        }

        if ([string]::IsNullOrWhiteSpace($text)) {
            $text = "update.log is empty or not found: $UpdateLog"
        }

        try {
            [System.Windows.Forms.Clipboard]::SetText($text)
        } catch {
            Set-Clipboard -Value $text
        }

        [void][System.Windows.Forms.MessageBox]::Show((U8 "0JbRg9GA0L3QsNC7INC+0LHQvdC+0LLQu9C10L3QuNGPINGB0LrQvtC/0LjRgNC+0LLQsNC9INCyINCx0YPRhNC10YAg0L7QsdC80LXQvdCwLg=="), "ProGo", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch {
        Write-Host ((U8 "0J3QtSDRg9C00LDQu9C+0YHRjCDRgdC60L7Qv9C40YDQvtCy0LDRgtGMINC20YPRgNC90LDQuzog") + $_.Exception.Message)
    }
}

function Open-UpdateLog {
    try {
        if (-not (Test-Path $UpdateLog)) {
            New-Item -ItemType File -Path $UpdateLog -Force | Out-Null
        }
        Start-Process -FilePath "notepad.exe" -ArgumentList $UpdateLog | Out-Null
    } catch {
        Write-Host ((U8 "0J3QtSDRg9C00LDQu9C+0YHRjCDQvtGC0LrRgNGL0YLRjCDRhNCw0LnQuzog") + $_.Exception.Message)
    }
}

function Open-ProGoFolder {
    try {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        Start-Process -FilePath "explorer.exe" -ArgumentList $InstallDir | Out-Null
    } catch {
        Write-Host ((U8 "0J3QtSDRg9C00LDQu9C+0YHRjCDQvtGC0LrRgNGL0YLRjCDQv9Cw0L/QutGDOiA=") + $_.Exception.Message)
    }
}

function Show-UpdateDialog($Text, $Title, $IconName) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing

        $form = New-Object System.Windows.Forms.Form
        $form.Text = $Title
        $form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
        $form.Width = 700
        $form.Height = 310
        $form.MinimizeBox = $false
        $form.MaximizeBox = $false
        $form.ShowInTaskbar = $true
        $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog

        $icon = New-Object System.Windows.Forms.PictureBox
        $icon.Left = 18
        $icon.Top = 22
        $icon.Width = 40
        $icon.Height = 40
        $icon.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::CenterImage
        if ($IconName -eq "Error") {
            $icon.Image = [System.Drawing.SystemIcons]::Error.ToBitmap()
        } elseif ($IconName -eq "Warning") {
            $icon.Image = [System.Drawing.SystemIcons]::Warning.ToBitmap()
        } else {
            $icon.Image = [System.Drawing.SystemIcons]::Information.ToBitmap()
        }
        $form.Controls.Add($icon)

        $message = New-Object System.Windows.Forms.TextBox
        $message.Left = 72
        $message.Top = 20
        $message.Width = 590
        $message.Height = 155
        $message.Multiline = $true
        $message.ReadOnly = $true
        $message.BorderStyle = [System.Windows.Forms.BorderStyle]::None
        $message.BackColor = $form.BackColor
        $message.Text = $Text
        $form.Controls.Add($message)

        $openLog = New-Object System.Windows.Forms.Button
        $openLog.Text = U8 "0J7RgtC60YDRi9GC0YwgdXBkYXRlLmxvZw=="
        $openLog.Left = 72
        $openLog.Top = 200
        $openLog.Width = 150
        $openLog.Height = 32
        $openLog.Add_Click({ Open-UpdateLog })
        $form.Controls.Add($openLog)

        $copyLog = New-Object System.Windows.Forms.Button
        $copyLog.Text = U8 "0KHQutC+0L/QuNGA0L7QstCw0YLRjCBsb2c="
        $copyLog.Left = 232
        $copyLog.Top = 200
        $copyLog.Width = 150
        $copyLog.Height = 32
        $copyLog.Add_Click({ Copy-LogToClipboard })
        $form.Controls.Add($copyLog)

        $openFolder = New-Object System.Windows.Forms.Button
        $openFolder.Text = U8 "0J7RgtC60YDRi9GC0Ywg0L/QsNC/0LrRgyBQcm9Hbw=="
        $openFolder.Left = 392
        $openFolder.Top = 200
        $openFolder.Width = 160
        $openFolder.Height = 32
        $openFolder.Add_Click({ Open-ProGoFolder })
        $form.Controls.Add($openFolder)

        $ok = New-Object System.Windows.Forms.Button
        $ok.Text = U8 "T0s="
        $ok.Left = 562
        $ok.Top = 200
        $ok.Width = 100
        $ok.Height = 32
        $ok.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $form.AcceptButton = $ok
        $form.CancelButton = $ok
        $form.Controls.Add($ok)

        [void]$form.ShowDialog()
    } catch {
        Write-Host ("{0}: {1}" -f $Title, $Text)
        Write-Host "update.log: $UpdateLog"
    }
}

function Show-UserMessage($Text, $Title) {
    Show-UpdateDialog $Text $Title "Information"
}

function Fail($Message) {
    Write-UpdateLog ("ERROR: " + $Message)
    throw $Message
}

function Get-LocalVersion {
    if (Test-Path $LocalVersionFile) {
        return ((Get-Content -Raw -Path $LocalVersionFile).Trim())
    }

    return "0.0.0"
}

function Get-RemoteVersion {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {
        Write-UpdateLog "TLS setup warning: $($_.Exception.Message)"
    }

    return ((Invoke-WebRequest -Uri $RemoteVersionUrl -UseBasicParsing).Content.Trim())
}

function Test-UpdateRequired {
    if ($Force) {
        Write-UpdateLog "Force update requested."
        return $true
    }

    $script:LocalVersion = Get-LocalVersion
    $script:RemoteVersion = Get-RemoteVersion
    Write-UpdateLog "Version check: local=$script:LocalVersion remote=$script:RemoteVersion"

    if ($script:LocalVersion -eq $script:RemoteVersion) {
        Write-UpdateLog "ProGo is already up to date."
        Show-UserMessage ((U8 "0KMg0LLQsNGBINCw0LrRgtGD0LDQu9GM0L3QsNGPINCy0LXRgNGB0LjRjyBQcm9Hbzog") + $script:LocalVersion) (U8 "0J7QsdC90L7QstC70LXQvdC40LUgUHJvR28=")
        return $false
    }

    return $true
}

function Wait-ProGoExit($TargetProcessId, $TimeoutMs) {
    if ($TargetProcessId -le 0) { return }

    Write-UpdateLog "Waiting for ProGo process to exit: PID $TargetProcessId"
    try {
        $process = Get-Process -Id $TargetProcessId -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            [void]$process.WaitForExit($TimeoutMs)
        }
    } catch {
        Write-UpdateLog "Wait process warning: $($_.Exception.Message)"
    }
}

function Stop-ExistingProGoProcesses($ExceptProcessId) {
    $processes = @(Get-Process -Name "ProGo" -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $ExceptProcessId })
    if ($processes.Count -eq 0) {
        Write-UpdateLog "No running ProGo processes found."
        return
    }

    foreach ($process in $processes) {
        Write-UpdateLog "Requesting old ProGo process close: PID $($process.Id)"
        try {
            if ($process.MainWindowHandle -ne 0) {
                [void]$process.CloseMainWindow()
                [void]$process.WaitForExit(10000)
            }
        } catch {
            Write-UpdateLog "Graceful close warning for PID $($process.Id): $($_.Exception.Message)"
        }

        $stillRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($null -ne $stillRunning) {
            Fail "ProGo is still running. Close it manually and run update again. PID $($process.Id)"
        }
    }
}

function Wait-FileUnlocked($Path, $TimeoutSeconds) {
    if (-not (Test-Path $Path)) { return }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
            return
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Fail "Timed out waiting for file unlock: $Path"
}

function Copy-FileIfExists($SourceRoot, $Name, $DestinationRoot, $Required) {
    $source = Join-Path $SourceRoot $Name
    if (Test-Path $source) {
        New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination (Join-Path $DestinationRoot $Name) -Force
        return
    }

    if ($Required) {
        Fail "Required file missing: $source"
    }
}

function Copy-DirectoryIfExists($SourceRoot, $Name, $DestinationRoot, $Required) {
    $source = Join-Path $SourceRoot $Name
    if (Test-Path $source) {
        $destination = Join-Path $DestinationRoot $Name
        if (Test-Path $destination) { Remove-Item -LiteralPath $destination -Recurse -Force -ErrorAction SilentlyContinue }
        Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
        return
    }

    if ($Required) {
        Fail "Required directory missing: $source"
    }
}

function Backup-InstalledState {
    New-Item -ItemType Directory -Path $BackupsDir -Force | Out-Null

    $from = Get-LocalVersion
    if ([string]::IsNullOrWhiteSpace($script:RemoteVersion)) {
        $script:RemoteVersion = "unknown"
    }

    $backupName = "backup-{0}-v{1}-to-v{2}" -f $Timestamp, ($from -replace '[^0-9A-Za-z._-]', '_'), ($script:RemoteVersion -replace '[^0-9A-Za-z._-]', '_')
    $backupDir = Join-Path $BackupsDir $backupName
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json", "progo.log", "update.log", "progo-update.log")) {
        try { Copy-FileIfExists $InstallDir $name $backupDir $false } catch { Write-UpdateLog "Backup warning for ${name}: $($_.Exception.Message)" }
    }

    try { Copy-DirectoryIfExists $InstallDir "scripts" $backupDir $false } catch { Write-UpdateLog "Backup warning for scripts: $($_.Exception.Message)" }

    $manifest = @(
        "product=ProGo",
        "version=$from",
        "target_version=$script:RemoteVersion",
        "created=$([DateTimeOffset]::Now.ToString('o'))",
        "reason=before-transactional-update",
        "contains=ProGo.exe,ProGo.ico,VERSION,scripts,vault.enc.json,settings.json,progo.log,update.log"
    )
    Set-Content -Path (Join-Path $backupDir "manifest.txt") -Value $manifest -Encoding UTF8

    Write-UpdateLog "Installed-state backup created: $backupDir"

    $oldBackups = @(Get-ChildItem -Path $BackupsDir -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -Skip 20)
    foreach ($old in $oldBackups) {
        Remove-Item $old.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }

    return $backupDir
}

function New-StagingCopy($TargetDir) {
    Write-UpdateLog "Creating intermediate staging copy: $TargetDir"
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json", "progo.log", "update.log", "progo-update.log")) {
        Copy-FileIfExists $InstallDir $name $TargetDir $false
    }

    Copy-DirectoryIfExists $InstallDir "scripts" $TargetDir $false
}

function Apply-ReleaseToStaging($ReleaseDir, $TargetDir) {
    Write-UpdateLog "Applying release to staging copy."
    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION")) {
        Copy-FileIfExists $ReleaseDir $name $TargetDir $true
    }

    Copy-DirectoryIfExists $ReleaseDir "scripts" $TargetDir $true
}

function Test-StagingCopy($TargetDir) {
    Write-UpdateLog "Validating staging copy."

    foreach ($name in @("ProGo.exe", "VERSION", "scripts\Update-ProGo.ps1")) {
        $path = Join-Path $TargetDir $name
        if (-not (Test-Path $path)) { Fail "Staging validation failed. Missing: $path" }
    }

    $stageVersion = ((Get-Content -Raw -Path (Join-Path $TargetDir "VERSION")).Trim())
    if ($stageVersion -ne $script:RemoteVersion) {
        Fail "Staging version mismatch: stage=$stageVersion remote=$script:RemoteVersion"
    }

    $stageExe = Join-Path $TargetDir "ProGo.exe"
    $process = Start-Process -FilePath $stageExe -ArgumentList "--self-check" -WorkingDirectory $TargetDir -PassThru -Wait
    if ($process.ExitCode -ne 0) {
        Fail "Staging self-check failed with exit code $($process.ExitCode)"
    }

    Write-UpdateLog "Staging validation PASS."
}

function Install-StagingToMain($TargetDir) {
    Write-UpdateLog "Installing validated staging copy into main application directory."
    $script:MainWasChanged = $true

    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION")) {
        Copy-FileIfExists $TargetDir $name $InstallDir $true
    }

    Copy-DirectoryIfExists $TargetDir "scripts" $InstallDir $true
}

function Restore-BackupToMain($SourceBackupDir) {
    if ([string]::IsNullOrWhiteSpace($SourceBackupDir) -or -not (Test-Path $SourceBackupDir)) {
        Write-UpdateLog "Rollback skipped: backup directory is not available."
        return
    }

    Write-UpdateLog "Rolling back main application from backup: $SourceBackupDir"

    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json", "progo.log")) {
        try { Copy-FileIfExists $SourceBackupDir $name $InstallDir $false } catch { Write-UpdateLog "Rollback warning for ${name}: $($_.Exception.Message)" }
    }

    try { Copy-DirectoryIfExists $SourceBackupDir "scripts" $InstallDir $false } catch { Write-UpdateLog "Rollback warning for scripts: $($_.Exception.Message)" }
}

function Build-DownloadedSource($DownloadedSourceRoot) {
    $buildScript = Join-Path $DownloadedSourceRoot "scripts\Build-ProGo.ps1"
    if (-not (Test-Path $buildScript)) {
        Fail "Downloaded archive does not contain scripts\Build-ProGo.ps1"
    }

    Write-UpdateLog "Building downloaded source in temporary workspace."
    & $buildScript

    if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
        Fail "Build failed with exit code $LASTEXITCODE"
    }

    $releaseDir = Join-Path $DownloadedSourceRoot "release"
    if (-not (Test-Path (Join-Path $releaseDir "ProGo.exe"))) {
        Fail "Build did not produce release\ProGo.exe"
    }

    return $releaseDir
}

function Start-UpdatedProGo($ExePath) {
    if ($NoLaunch) {
        Write-UpdateLog "Launch skipped by -NoLaunch."
        return
    }

    if (-not (Test-Path $ExePath)) {
        Fail "Cannot launch ProGo, file not found: $ExePath"
    }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-UpdateLog "Starting updated ProGo, attempt $attempt..."
        try {
            $process = Start-Process -FilePath $ExePath -WorkingDirectory $InstallDir -PassThru
            Start-Sleep -Seconds 2

            $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($null -ne $running -and -not $running.HasExited) {
                Write-UpdateLog "Updated ProGo started: PID $($process.Id)"
                return
            }

            Write-UpdateLog "Started ProGo process exited too early."
        } catch {
            Write-UpdateLog "Launch attempt $attempt failed: $($_.Exception.Message)"
        }

        Start-Sleep -Seconds 1
    }

    Fail "Updated ProGo did not stay running after restart attempts."
}

function Cleanup-TemporaryFiles {
    Write-UpdateLog "Cleaning temporary update files."
    foreach ($path in @($TransactionRoot)) {
        if (Test-Path $path) {
            Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    try {
        $oldDirs = @(Get-ChildItem -Path $InstallDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "update-*" -or $_.Name -like "update-txn-*" })
        foreach ($dir in $oldDirs) {
            Remove-Item -LiteralPath $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-UpdateLog "Cleanup warning: $($_.Exception.Message)"
    }
}

try {
    Write-UpdateLog "ProGo transactional update started."

    if (-not (Test-UpdateRequired)) {
        return
    }

    $UpdaterProcessId = $PID
    Wait-ProGoExit -TargetProcessId $WaitPid -TimeoutMs 30000
    Stop-ExistingProGoProcesses -ExceptProcessId $UpdaterProcessId

    $Exe = Join-Path $InstallDir "ProGo.exe"
    Wait-FileUnlocked -Path $Exe -TimeoutSeconds 30

    $BackupDir = Backup-InstalledState
    Write-UpdateLog "Backup before update: $BackupDir"

    New-Item -ItemType Directory -Path $TransactionRoot -Force | Out-Null
    New-StagingCopy -TargetDir $StageDir

    $ZipPath = Join-Path $TransactionRoot "ProGo-main.zip"
    New-Item -ItemType Directory -Path $SourceDir -Force | Out-Null

    Write-UpdateLog "Downloading source from GitHub into temporary workspace."
    Invoke-WebRequest -Uri $SourceZipUrl -OutFile $ZipPath -UseBasicParsing

    Write-UpdateLog "Extracting source into temporary workspace."
    Expand-Archive -Path $ZipPath -DestinationPath $SourceDir -Force

    $SourceRoot = Get-ChildItem -Path $SourceDir -Directory | Where-Object { Test-Path (Join-Path $_.FullName "scripts\Build-ProGo.ps1") } | Select-Object -First 1
    if ($null -eq $SourceRoot) {
        Fail "Downloaded archive does not contain expected ProGo source tree."
    }

    $ReleaseDir = Build-DownloadedSource -DownloadedSourceRoot $SourceRoot.FullName
    Apply-ReleaseToStaging -ReleaseDir $ReleaseDir -TargetDir $StageDir
    Test-StagingCopy -TargetDir $StageDir

    Install-StagingToMain -TargetDir $StageDir

    $installedVersion = ((Get-Content -Raw -Path (Join-Path $InstallDir "VERSION")).Trim())
    if ($installedVersion -ne $RemoteVersion) {
        Fail "Installed version mismatch after commit: installed=$installedVersion remote=$RemoteVersion"
    }

    $mainCheck = Start-Process -FilePath (Join-Path $InstallDir "ProGo.exe") -ArgumentList "--self-check" -WorkingDirectory $InstallDir -PassThru -Wait
    if ($mainCheck.ExitCode -ne 0) {
        Fail "Main self-check failed after commit with exit code $($mainCheck.ExitCode)"
    }

    Start-UpdatedProGo -ExePath (Join-Path $InstallDir "ProGo.exe")

    Write-UpdateLog "ProGo transactional update completed."
    Show-UpdateDialog ((U8 "UHJvR28g0L7QsdC90L7QstC70ZHQvS4g0KDQtdC30LXRgNCy0L3QsNGPINC60L7Qv9C40Y8g0YHQvtGF0YDQsNC90LXQvdCwOgo=") + $BackupDir) (U8 "0J7QsdC90L7QstC70LXQvdC40LUgUHJvR28=") "Information"
} catch {
    $message = $_.Exception.Message
    Write-UpdateLog "TRANSACTION FAILED: $message"

    if ($MainWasChanged) {
        Restore-BackupToMain -SourceBackupDir $BackupDir
        Write-UpdateLog "Rollback completed after failed main commit."
    } else {
        Write-UpdateLog "Main application was not changed; rollback is not required."
    }

    Show-UpdateDialog ((U8 "0J7QsdC90L7QstC70LXQvdC40LUgUHJvR28g0L3QtSDQstGL0L/QvtC70L3QtdC90L4uINCe0YHQvdC+0LLQvdC+0LUg0L/RgNC40LvQvtC20LXQvdC40LUg0YHQvtGF0YDQsNC90LXQvdC+INC40LvQuCDQstC+0YHRgdGC0LDQvdC+0LLQu9C10L3QviDQuNC3INGA0LXQt9C10YDQstC90L7QuSDQutC+0L/QuNC4LiDQn9C+0LTRgNC+0LHQvdC+0YHRgtC4INCyIHVwZGF0ZS5sb2cuCgo=") + $message) (U8 "0J7QsdC90L7QstC70LXQvdC40LUgUHJvR28=") "Error"
    throw
} finally {
    Cleanup-TemporaryFiles
}
