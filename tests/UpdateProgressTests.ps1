param([string]$Exe, [string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: update progress transaction tests require isolated Windows CI'; return }
. (Join-Path $Scripts 'Maintenance-ProGo.ps1')
$passed = 0
function Check($Value, $Name) { if (-not $Value) { throw $Name }; $script:passed++; Write-Host "PASS: $Name" }
$work = Join-Path $env:TEMP ('ProGo-update-progress-' + [guid]::NewGuid().ToString('N'))
$release = Split-Path -Parent $Exe
$version = (Get-Content -Raw (Join-Path $release 'VERSION')).Trim()
$expected = 'metadata,waiting,download,package,backup,staging,stage-check,install,installed-check,restart'
$runtimeSettings = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ProGo\settings.json'
$originalSettings = if (Test-Path $runtimeSettings) { [IO.File]::ReadAllBytes($runtimeSettings) } else { $null }
try {
    New-Item -ItemType Directory $work | Out-Null
    $package = Join-Path $work 'release.zip'
    Compress-Archive -Path (Join-Path $release '*') -DestinationPath $package
    foreach ($mode in @('success','current','download-failure','hash-failure','stage-failure','commit-failure','restart-failure','rollback-failure','render-failure','download-cancel','boundary-cancel','cancel-alive')) {
        $fixture = Join-Path $work $mode
        $install = Join-Path $fixture 'ProGo'
        New-Item -ItemType Directory -Path $install -Force | Out-Null
        Copy-Item -LiteralPath $Exe -Destination (Join-Path $install 'ProGo.exe')
        Copy-Item -LiteralPath $Scripts -Destination (Join-Path $install 'scripts') -Recurse
        $local = if ($mode -eq 'current') { $version } else { '0.0.1' }
        Set-Content -LiteralPath (Join-Path $install 'VERSION') $local
        [IO.File]::WriteAllText((Join-Path $install 'settings.json'), '{}')
        [IO.File]::WriteAllText((Join-Path $install 'vault.enc.json'), 'opaque progress fixture')
        $settings = (Get-FileHash (Join-Path $install 'settings.json')).Hash
        $vault = (Get-FileHash (Join-Path $install 'vault.enc.json')).Hash
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $PSHOME 'powershell.exe'
        $driver = Join-Path $PSScriptRoot 'UpdateProgressDriver.ps1'
        $info.Arguments = "-NoProfile -File `"$driver`" -Mode $mode -Scripts `"$Scripts`" -Fixture `"$fixture`" -Release `"$release`" -Package `"$package`""
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.StandardOutputEncoding = New-Object Text.UTF8Encoding($false)
        $process = [Diagnostics.Process]::Start($info)
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        try {
            if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw "Progress fixture timed out: $mode" }
            $text = $stdout.Result
            # Windows PowerShell 5.1 returns a JSON array as one pipeline object.
            # Assign it directly; @() would wrap it in an extra array layer.
            $events = Get-Content -LiteralPath (Join-Path $fixture 'progress.json') -Raw -Encoding UTF8 | ConvertFrom-Json
            $phases = (@($events | Where-Object { $_.Kind -eq 'phase' } | ForEach-Object { $_.Phase })) -join ','
            $wanted = switch ($mode) {
                current { 'metadata' }
                'download-cancel' { 'metadata,waiting,download' }
                'boundary-cancel' { 'metadata,waiting,download' }
                'cancel-alive' { 'metadata,waiting,download' }
                'download-failure' { 'metadata,waiting,download' }
                'hash-failure' { 'metadata,waiting,download,package' }
                'stage-failure' { 'metadata,waiting,download,package,backup,staging,stage-check' }
                'commit-failure' { 'metadata,waiting,download,package,backup,staging,stage-check,install,rollback' }
                'rollback-failure' { 'metadata,waiting,download,package,backup,staging,stage-check,install,rollback' }
                'restart-failure' { $expected + ',rollback' }
                default { $expected }
            }
            if ($phases -ne $wanted) { Write-Host $text; Write-Host $stderr.Result }
            Check ($phases -eq $wanted) "actual transaction exposes only reached phases in order: $mode"
            Check (($process.ExitCode -eq 0) -eq ($mode -in @('success','current','render-failure'))) "progress does not mask the transaction outcome: $mode"
            # Expand-Archive emits its own legitimate byte/file progress too.
            # Validate this helper's fixed indicator, without confusing hosts.
            $records = @($events | Where-Object { $_.Kind -eq 'progress' -and $_.Id -eq 23 -and -not $_.Completed })
            Check ($records.Count -eq $wanted.Split(',').Length) "each reached helper phase produces its own progress record: $mode"
            Check (@($records | Where-Object { $_.Id -ne 23 -or $_.Activity -ne 'Обновление ProGo' -or $_.Percent -ne -1 -or [string]::IsNullOrWhiteSpace($_.Operation) }).Count -eq 0) "progress carries explanations without made-up percentages: $mode"
            Check ($events[-1].Kind -eq 'progress' -and $events[-1].Id -eq 23 -and $events[-1].Completed) "finally requests helper progress clearance even after rollback or host failure: $mode"
            foreach ($i in 0..($events.Count - 1)) {
                if ($events[$i].Kind -eq 'dialog') { Check ($i -gt 0 -and $events[$i - 1].Kind -eq 'progress' -and $events[$i - 1].Id -eq 23 -and $events[$i - 1].Completed) "helper progress clears before the result dialog: $mode" }
            }
            Check ((Get-FileHash (Join-Path $install 'settings.json')).Hash -eq $settings -and (Get-FileHash (Join-Path $install 'vault.enc.json')).Hash -eq $vault) "progress preserves settings and opaque vault: $mode"
            if ($mode -in @('success','render-failure')) {
                Check ($text.Contains('Этап 10 из 10: Завершаю обновление') -and -not $text.Contains('Этап 10 из 10: Запускаю ProGo')) "NoLaunch never claims the app restarted: $mode"
                Check ($text.Contains('Этап 3 из 10: Скачиваю обновление') -and $text.Contains('не закрывайте окно')) "plain text survives disabled progress rendering: $mode"
            } else { Check (-not $text.Contains('Обновление завершено. Резервная копия сохранена.')) "failed or current update never prints success: $mode" }
            if ($mode -notin @('success','render-failure','rollback-failure')) {
                Check ((Get-Content -LiteralPath (Join-Path $install 'VERSION') -Raw).Trim() -eq $local) "failure/current keeps or rolls back the installed version: $mode"
            }
            if ($mode -in @('download-cancel','boundary-cancel','cancel-alive')) {
                $facts = Get-Content -LiteralPath (Join-Path $fixture 'cancellation.json') -Raw | ConvertFrom-Json
                Check $facts.SharedDiagnostics "installed window and diagnostic preview share one local theme assembly: $mode"
                Check ($facts.Notice -and $facts.GenericRejected) "bootstrap distinguishes safe download cancellation from other cancelled operations: $mode"
                Check ($facts.Resumes -eq $(if ($mode -eq 'boundary-cancel') { 1 } else { 0 })) "cancel recovery obeys NoLaunch and keeps a surviving instance: $mode"
                Check ($facts.MainUnchanged -and $facts.TransactionRemoved -and $facts.NoBackup -and $facts.WindowClosed) "cancel before validation closes UI and removes only its temporary work: $mode"
                Check ((Get-FileHash (Join-Path $install 'ProGo.exe')).Hash -eq (Get-FileHash $Exe).Hash -and
                    (Get-FileHash (Join-Path $install 'scripts\Update-ProGo.Core.ps1')).Hash -eq (Get-FileHash (Join-Path $Scripts 'Update-ProGo.Core.ps1')).Hash) "cancel preserves installed executable and helper: $mode"
                Check (@($events | Where-Object { $_.Kind -eq 'dialog' -and $_.Icon -eq 'Error' }).Count -eq 0) "download cancellation does not claim failure or rollback: $mode"
            }
            $lease = [ProGo.MaintenanceOperation]::Enter(); $lease.Dispose()
            Check ($true) "progress failure paths release maintenance ownership: $mode"
        } finally { $process.Dispose() }
    }
    Write-Host "Updater progress tests PASS: $passed"
} finally {
    # Compiled self-check uses Windows' real known-folder API, not just the child's
    # LOCALAPPDATA override. Preserve that isolated runner input as well.
    if ($null -ne $originalSettings) { [IO.File]::WriteAllBytes($runtimeSettings, $originalSettings) }
    else { Remove-Item -LiteralPath $runtimeSettings -ErrorAction SilentlyContinue }
    if (Test-Path $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
