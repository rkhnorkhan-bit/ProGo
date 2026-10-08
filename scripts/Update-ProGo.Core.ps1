param(
    [int]$WaitPid = 0,
    [string]$ReleasePackageUrl = "https://github.com/rkhnorkhan-bit/ProGo/releases/latest/download/ProGo-release.zip",
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [string]$RemoteVersionUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest",
    [switch]$NoLaunch,
    [switch]$Force,
    [switch]$NoReleasePackage
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
try { . (Join-Path $PSScriptRoot 'Log-ProGo.ps1') } catch { }
$UpdateLog = Join-Path $InstallDir "update.log"
$LegacyUpdateLog = Join-Path $InstallDir "progo-update.log"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$TransactionRoot = Join-Path $env:TEMP ("ProGo-update-" + $Timestamp)
$StageDir = Join-Path $TransactionRoot "stage"
$SourceDir = Join-Path $TransactionRoot "source"
$PackageDir = Join-Path $TransactionRoot "package"
$BackupsDir = Join-Path $InstallDir "backups"
$LocalVersionFile = Join-Path $InstallDir "VERSION"
$State = @{
    RemoteVersion = $null
    LocalVersion = $null
    MainWasChanged = $false
    UpdateMode = "unknown"
}
$BackupDir = $null
$UpdateWindow = $null

function Write-UpdateLog($Message) {
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    try { Write-ProGoLog -Path $UpdateLog -Message $line } catch { }
    Write-Host $Message
}

function Show-UpdatePhase {
    param([ValidateSet('metadata','waiting','download','package','backup','staging','stage-check','install','installed-check','restart','rollback')][string]$Phase)
    # Phase numbers describe work, never invented byte percentages or time estimates.
    $phases = @{
        metadata = @(1, 'Проверяю выпуск обновления', 'Получаю версию и сведения о пакете с GitHub.')
        waiting = @(2, 'Ожидаю завершения ProGo', 'Приложение сохраняет состояние и освобождает файлы. Ожидание может занять до 30 секунд; затем проверяется доступ к файлам.')
        download = @(3, 'Скачиваю обновление', 'Скорость зависит от подключения. Установленные файлы ещё не заменяются.')
        package = @(4, 'Проверяю скачанный пакет', 'Проверяю контрольную сумму, содержимое архива и версию. Установка начнётся только после проверки.')
        backup = @(5, 'Сохраняю резервную копию', 'Сохраняю текущую программу и настройки перед заменой файлов.')
        staging = @(6, 'Подготавливаю новую версию', 'Собираю отдельную копию для проверки. Установленные файлы ещё не заменяются.')
        'stage-check' = @(7, 'Проверяю новую версию', 'Запускаю встроенную проверку подготовленной копии.')
        install = @(8, 'Устанавливаю обновление', 'Идёт замена файлов. Дождитесь результата; не закрывайте окно.')
        'installed-check' = @(9, 'Проверяю установленную версию', 'Проверяю версию и запускаю встроенную проверку после установки.')
        restart = @(10, 'Запускаю ProGo', 'Проверяю, что приложение запустилось. Дождитесь результата.')
        rollback = @(0, 'Восстанавливаю предыдущую версию', 'Обновление не завершилось. Пытаюсь вернуть файлы из резервной копии; дождитесь результата.')
    }
    $item = $phases[$Phase]
    $title = if ($Phase -eq 'rollback') { $item[1] } else { 'Этап {0} из 10: {1}' -f $item[0], $item[1] }
    if ($Phase -eq 'restart' -and $NoLaunch) {
        $title = 'Этап 10 из 10: Завершаю обновление'
        $item[2] = 'Автоматический запуск отключён. После завершения можно открыть ProGo вручную.'
    }
    if ($null -ne $UpdateWindow) { $UpdateWindow.ShowPhase($title, $item[2]) }
    # Progress is presentation only. Hosts may suppress it or refuse rendering;
    # that must never abort installation, rollback or ownership cleanup.
    try { Write-Host $title; Write-Host $item[2] } catch { }
    try { Write-Progress -Id 23 -Activity 'Обновление ProGo' -Status $title -CurrentOperation $item[2] -PercentComplete -1 } catch { }
}

function Clear-UpdateProgress {
    try { Write-Progress -Id 23 -Activity 'Обновление ProGo' -Completed } catch { }
}

function Initialize-UpdateWindow {
    if (-not ('ProGo.UpdateInstallSession' -as [type])) {
        $sources = @('UpdateInstallSession.cs','DiagnosticReport.cs','DiagnosticPreview.cs','UiTheme.cs','BrandIcon.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
        Add-Type -Path $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll -ErrorAction Stop
    }
    $script:UpdateWindow = [ProGo.UpdateInstallSession]::Open()
}

function Close-UpdateWindow {
    $window = $UpdateWindow
    $script:UpdateWindow = $null
    if ($null -ne $window) { $window.Dispose() }
}

function Test-UpdateCancelled($ErrorObject) {
    $exception = $ErrorObject.Exception
    while ($null -ne $exception) {
        if ($exception -is [OperationCanceledException]) { return $true }
        $exception = $exception.InnerException
    }
    return $false
}

function Copy-LogToClipboard {
    try {
        . (Join-Path $PSScriptRoot 'Diagnostics-ProGo.ps1')
        Show-ProGoDiagnostics -Root $InstallDir
    } catch {
        Write-Host 'Не удалось открыть предпросмотр диагностики. Личный журнал доступен на этом компьютере.'
    }
}

function Open-UpdateLog {
    try {
        $path = $UpdateLog
        if (-not (Test-Path $path) -and (Test-Path $LegacyUpdateLog)) { $path = $LegacyUpdateLog }
        if (-not (Test-Path $path)) { New-Item -ItemType File -Path $path -Force | Out-Null }
        Start-Process -FilePath "notepad.exe" -ArgumentList $path | Out-Null
    } catch {
        Write-Host (('Не удалось открыть файл: ') + $_.Exception.Message)
    }
}

function Open-ProGoFolder {
    try {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        Start-Process -FilePath "explorer.exe" -ArgumentList $InstallDir | Out-Null
    } catch {
        Write-Host (('Не удалось открыть папку: ') + $_.Exception.Message)
    }
}

function Show-UpdateDialog($Text, $Title, $IconName) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing

        $form = New-Object System.Windows.Forms.Form
        $form.Text = $Title
        $form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
        $form.Width = 760
        $form.BackColor = [Drawing.Color]::FromArgb(12,17,27)
        $form.ForeColor = [Drawing.Color]::FromArgb(235,241,250)
        $form.Font = New-Object Drawing.Font "Segoe UI", 10
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
        $message.Width = 650
        $message.Height = 155
        $message.Multiline = $true
        $message.ReadOnly = $true
        $message.BorderStyle = [System.Windows.Forms.BorderStyle]::None
        $message.BackColor = $form.BackColor
        $message.ForeColor = $form.ForeColor
        $message.Text = $Text
        $form.Controls.Add($message)

        $openLog = New-Object System.Windows.Forms.Button
        $openLog.Text = 'Журнал обновления'
        $openLog.Left = 72
        $openLog.Top = 200
        $openLog.Width = 150
        $openLog.Height = 32
        $openLog.Add_Click({ Open-UpdateLog })
        $form.Controls.Add($openLog)

        $copyLog = New-Object System.Windows.Forms.Button
        $copyLog.Text = 'Диагностика…'
        $copyLog.Left = 232
        $copyLog.Top = 200
        $copyLog.Width = 150
        $copyLog.Height = 32
        $copyLog.Add_Click({ Copy-LogToClipboard })
        $form.Controls.Add($copyLog)

        $openFolder = New-Object System.Windows.Forms.Button
        $openFolder.Text = 'Открыть папку ProGo'
        $openFolder.Left = 392
        $openFolder.Top = 200
        $openFolder.Width = 160
        $openFolder.Height = 32
        $openFolder.Add_Click({ Open-ProGoFolder })
        $form.Controls.Add($openFolder)

        $ok = New-Object System.Windows.Forms.Button
        $ok.Text = 'OK'
        $ok.Left = 562
        $ok.Top = 200
        $ok.Width = 100
        $ok.Height = 32
        $ok.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $form.AcceptButton = $ok
        $form.CancelButton = $ok
        $form.Controls.Add($ok)

        foreach ($control in $form.Controls) {
            if ($control -is [Windows.Forms.Button]) {
                $control.FlatStyle = 'Flat'
                $control.BackColor = [Drawing.Color]::FromArgb(30,40,57)
                $control.ForeColor = $form.ForeColor
            }
        }
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

function Initialize-UpdateTransport {
    if (-not ('ProGo.InstalledUpdateTransport' -as [type])) {
        # Local installed source only; no network-loaded code or policy bypass.
        Add-Type -Path (Join-Path $PSScriptRoot 'InstalledUpdateTransport.cs')
    }
}

function Read-UpdateMetadata($Url) {
    Initialize-UpdateTransport
    return [ProGo.InstalledUpdateTransport]::ReadMetadata($Url, [Threading.CancellationToken]::None)
}

function Save-UpdatePackage($Url, $Path) {
    Initialize-UpdateTransport
    $token = if ($null -ne $UpdateWindow) { $UpdateWindow.Token } else { [Threading.CancellationToken]::None }
    [ProGo.InstalledUpdateTransport]::DownloadPackage($Url, $Path, $token)
}

function Get-RemoteVersion {
    $payload = (Read-UpdateMetadata $RemoteVersionUrl) | ConvertFrom-Json
    if ($payload.draft -or $payload.prerelease) { throw "Only stable published releases are accepted." }
    $version = ([string]$payload.tag_name).TrimStart('v')
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid release version." }
    $assets = @($payload.assets | Where-Object { $_.name -eq 'ProGo-release.zip' })
    if ($assets.Count -ne 1) { throw "The release package is missing or ambiguous." }
    $asset = $assets[0]
    $expectedUrl = 'https://github.com/rkhnorkhan-bit/ProGo/releases/download/v' + $version + '/ProGo-release.zip'
    if ([string]$asset.browser_download_url -cne $expectedUrl) { throw "Unexpected release download location." }
    if ([string]$asset.digest -notmatch '^sha256:[a-fA-F0-9]{64}$') { throw "The release has no SHA-256 digest. Update cancelled." }
    $State.ReleaseUrl = $expectedUrl
    $State.ReleaseSha256 = ([string]$asset.digest).Substring(7)
    return $version
}

function Test-UpdateRequired {
    $State.LocalVersion = Get-LocalVersion
    $State.RemoteVersion = Get-RemoteVersion
    Write-UpdateLog "Version check: local=$($State.LocalVersion) remote=$($State.RemoteVersion)"
    if ($Force) { Write-UpdateLog "Force update requested."; return $true }

    if ([version]$State.LocalVersion -ge [version]$State.RemoteVersion) {
        Write-UpdateLog "ProGo is already up to date."
        Clear-UpdateProgress
        Close-UpdateWindow
        Show-UserMessage (('У вас актуальная версия ProGo: ') + $State.LocalVersion) ('Обновление ProGo')
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
    if ([string]::IsNullOrWhiteSpace($State.RemoteVersion)) {
        $State.RemoteVersion = "unknown"
    }

    $backupName = "backup-{0}-v{1}-to-v{2}" -f $Timestamp, ($from -replace '[^0-9A-Za-z._-]', '_'), ($State.RemoteVersion -replace '[^0-9A-Za-z._-]', '_')
    $backupDir = Join-Path $BackupsDir $backupName
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

    foreach ($name in @("ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json", "progo.log", "update.log", "progo-update.log")) {
        Copy-FileIfExists $InstallDir $name $backupDir $false
    }

    Copy-DirectoryIfExists $InstallDir "scripts" $backupDir $false

    $manifest = @(
        "product=ProGo",
        "version=$from",
        "target_version=$($State.RemoteVersion)",
        "created=$([DateTimeOffset]::Now.ToString('o'))",
        "created_by=updater",
        "backup_kind=pre-update",
        "update_result=pending",
        "reason=before-transactional-update",
        "contains=$([ProGo.BackupIntegrity]::Contents($backupDir))",
        "update_mode=$($State.UpdateMode)"
    )
    Set-Content -Path (Join-Path $backupDir "manifest.txt") -Value $manifest -Encoding UTF8

    [ProGo.BackupIntegrity]::Write($backupDir)
    [ProGo.BackupIntegrity]::Validate($backupDir)
    Write-UpdateLog "Installed-state backup created: $backupDir"

    # Manifest-aware policy is shared with BackupService. Unknown/manual folders
    # and the latest baseline/pre-update are never pruned by a directory count.
    $cleanup = [ProGo.BackupRetention]::Apply([ProGo.BackupRetention]::Plan($BackupsDir, $backupDir))
    Write-UpdateLog "Backup retention: deleted=$($cleanup.Deleted); kept=$($cleanup.Kept); failed=$($cleanup.Failed); skipped=$($cleanup.Skipped)"

    return $backupDir
}

function Set-BackupUpdateResult($BackupPath, $Result) {
    try {
        if ([string]::IsNullOrWhiteSpace($BackupPath)) { return }
        $manifestPath = Join-Path $BackupPath "manifest.txt"
        if (-not (Test-Path $manifestPath)) { return }

        $lines = New-Object System.Collections.Generic.List[string]
        $hasResult = $false
        foreach ($line in (Get-Content -Path $manifestPath)) {
            if ($line -like "update_result=*") {
                $lines.Add("update_result=$Result")
                $hasResult = $true
            } else {
                $lines.Add($line)
            }
        }
        if (-not $hasResult) { $lines.Add("update_result=$Result") }
        Set-Content -Path $manifestPath -Value $lines -Encoding UTF8
    } catch {
        Write-UpdateLog "Backup result marker warning: $($_.Exception.Message)"
    }
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

    foreach ($name in @("ProGo.exe", "VERSION", "scripts\Update-ProGo.ps1", "scripts\Update-ProGo.Core.ps1")) {
        $path = Join-Path $TargetDir $name
        if (-not (Test-Path $path)) { Fail "Staging validation failed. Missing: $path" }
    }

    $stageVersion = ((Get-Content -Raw -Path (Join-Path $TargetDir "VERSION")).Trim())
    if ($stageVersion -ne $State.RemoteVersion) {
        Fail "Staging version mismatch: stage=$stageVersion remote=$($State.RemoteVersion)"
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
    $State.MainWasChanged = $true

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

function Find-ReleaseDirInExtractedPackage($Root) {
    $directExe = Join-Path $Root "ProGo.exe"
    if (Test-Path $directExe) { return $Root }

    $releaseDir = Join-Path $Root "release"
    if (Test-Path (Join-Path $releaseDir "ProGo.exe")) { return $releaseDir }

    $candidate = Get-ChildItem -Path $Root -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName "ProGo.exe") } |
        Select-Object -First 1

    if ($null -ne $candidate) { return $candidate.FullName }
    return $null
}

function Test-ReleaseArchive($ZipPath, $Root) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
        $total = 0L
        if ($archive.Entries.Count -gt 1000) { throw "Too many files in release archive." }
        foreach ($entry in $archive.Entries) {
            $relative = $entry.FullName.Replace('/', '\')
            if ([IO.Path]::IsPathRooted($relative) -or $relative.Contains(':')) { throw "Unsafe archive path." }
            $target = [IO.Path]::GetFullPath((Join-Path $Root $relative))
            if (-not $target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Archive path escapes staging." }
            $total += $entry.Length
            if ($total -gt 64MB) { throw "Release archive exceeds size limit." }
        }
    } finally { $archive.Dispose() }
}

function Try-GetReleasePackage($DestinationRoot) {
    if ($NoReleasePackage) { throw "Source execution during update is no longer supported. Use a published release." }
    if ([string]::IsNullOrWhiteSpace($State.ReleaseUrl) -or [string]::IsNullOrWhiteSpace($State.ReleaseSha256)) { throw "Release metadata is unavailable." }
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    $packageZip = Join-Path $DestinationRoot "ProGo-release.zip"
    Write-UpdateLog "update_mode=release-package"
    Write-UpdateLog "Downloading published package: $($State.ReleaseUrl)"
    Show-UpdatePhase download
    if ($null -ne $UpdateWindow) { $UpdateWindow.BeginDownload() }
    Save-UpdatePackage $State.ReleaseUrl $packageZip
    if ($null -ne $UpdateWindow) { $UpdateWindow.EndDownload() }
    Show-UpdatePhase package
    $actual = (Get-FileHash -LiteralPath $packageZip -Algorithm SHA256).Hash
    if ($actual -ine $State.ReleaseSha256) { throw "Package SHA-256 mismatch. No installed files were changed." }
    Write-UpdateLog "Package SHA-256 verified: $actual"
    Test-ReleaseArchive -ZipPath $packageZip -Root $DestinationRoot
    Expand-Archive -LiteralPath $packageZip -DestinationPath $DestinationRoot -Force
    $releaseDir = Find-ReleaseDirInExtractedPackage $DestinationRoot
    if ([string]::IsNullOrWhiteSpace($releaseDir)) { throw "Release package does not contain ProGo.exe." }
    foreach ($required in @("ProGo.exe", "VERSION", "scripts\Update-ProGo.ps1", "scripts\Update-ProGo.Core.ps1")) {
        if (-not (Test-Path -LiteralPath (Join-Path $releaseDir $required))) { throw "Release package missing: $required" }
    }
    $packageVersion = (Get-Content -Raw -LiteralPath (Join-Path $releaseDir "VERSION")).Trim()
    if ($packageVersion -ne $State.RemoteVersion) { throw "Release package version mismatch." }
    return $releaseDir
}

function Get-ReleaseDirForUpdate {
    $State.UpdateMode = "release-package"
    return (Try-GetReleasePackage -DestinationRoot $PackageDir)
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

# Acquire before logs, network, backups or waits. A rejected contender has no
# installed-state side effects and cannot enter rollback/cleanup for the owner.
. (Join-Path $PSScriptRoot 'Maintenance-ProGo.ps1')
$Maintenance = [ProGo.MaintenanceOperation]::Enter()
try {
    . (Join-Path $PSScriptRoot 'BackupRetention-ProGo.ps1')
    . (Join-Path $PSScriptRoot 'BackupIntegrity-ProGo.ps1')
    Write-UpdateLog "ProGo transactional update started."

    Initialize-UpdateWindow
    Show-UpdatePhase metadata
    if (-not (Test-UpdateRequired)) {
        Clear-UpdateProgress
        return
    }

    [ProGo.MaintenanceOperation]::ConfirmHandoff()
    Show-UpdatePhase waiting
    Wait-ProGoExit -TargetProcessId $WaitPid -TimeoutMs 30000
    [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
    [ProGo.MaintenanceOperation]::RequireProxyCleanupCompleted($InstallDir)

    $Exe = Join-Path $InstallDir "ProGo.exe"
    Wait-FileUnlocked -Path $Exe -TimeoutSeconds 30

    New-Item -ItemType Directory -Path $TransactionRoot -Force | Out-Null

    $ReleaseDir = Get-ReleaseDirForUpdate

    Show-UpdatePhase backup
    $BackupDir = Backup-InstalledState
    Write-UpdateLog "Backup before update: $BackupDir"

    Show-UpdatePhase staging
    New-StagingCopy -TargetDir $StageDir
    Apply-ReleaseToStaging -ReleaseDir $ReleaseDir -TargetDir $StageDir
    Show-UpdatePhase stage-check
    Test-StagingCopy -TargetDir $StageDir

    Show-UpdatePhase install
    Install-StagingToMain -TargetDir $StageDir

    Show-UpdatePhase installed-check
    $installedVersion = ((Get-Content -Raw -Path (Join-Path $InstallDir "VERSION")).Trim())
    if ($installedVersion -ne $State.RemoteVersion) {
        Fail "Installed version mismatch after commit: installed=$installedVersion remote=$($State.RemoteVersion)"
    }

    $mainCheck = Start-Process -FilePath (Join-Path $InstallDir "ProGo.exe") -ArgumentList "--self-check" -WorkingDirectory $InstallDir -PassThru -Wait
    if ($mainCheck.ExitCode -ne 0) {
        Fail "Main self-check failed after commit with exit code $($mainCheck.ExitCode)"
    }

    Show-UpdatePhase restart
    Start-UpdatedProGo -ExePath (Join-Path $InstallDir "ProGo.exe")

    Set-BackupUpdateResult -BackupPath $BackupDir -Result "success"
    Write-UpdateLog "ProGo transactional update completed."
    Clear-UpdateProgress
    Close-UpdateWindow
    try { Write-Host 'Обновление завершено. Резервная копия сохранена.' } catch { }
    Show-UpdateDialog (('ProGo обновлён. Резервная копия сохранена:
') + $BackupDir) ('Обновление ProGo') "Information"
} catch {
    if (-not $State.MainWasChanged -and $null -ne $UpdateWindow -and $UpdateWindow.CancellationRequested -and (Test-UpdateCancelled $_)) {
        Write-UpdateLog 'Download cancelled before validation or installation. Installed files were not changed.'
        Clear-UpdateProgress
        Close-UpdateWindow
        throw (New-Object ProGo.UpdateDownloadCancelledException)
    }
    $message = $_.Exception.Message
    Write-UpdateLog "TRANSACTION FAILED: $message"
    Set-BackupUpdateResult -BackupPath $BackupDir -Result "failed"

    if ($State.MainWasChanged) {
        [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
        Show-UpdatePhase rollback
        Restore-BackupToMain -SourceBackupDir $BackupDir
        Write-UpdateLog "Rollback completed after failed main commit."
    } else {
        Write-UpdateLog "Main application was not changed; rollback is not required."
    }

    Clear-UpdateProgress
    Close-UpdateWindow
    try { Write-Host 'Обновление не выполнено. Подробности доступны в журнале обновления.' } catch { }
    Show-UpdateDialog (('Обновление ProGo не выполнено. Основное приложение сохранено или восстановлено из резервной копии. Подробности в update.log.

') + $message) ('Обновление ProGo') "Error"
    throw
} finally {
    try { Cleanup-TemporaryFiles } finally { try { Clear-UpdateProgress } finally { try { Close-UpdateWindow } finally { $Maintenance.Dispose() } } }
}
