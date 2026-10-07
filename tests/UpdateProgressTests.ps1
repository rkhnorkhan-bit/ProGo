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
try {
    New-Item -ItemType Directory $work | Out-Null
    $package = Join-Path $work 'release.zip'
    Compress-Archive -Path (Join-Path $release '*') -DestinationPath $package
    foreach ($mode in @('success','current','download-failure','hash-failure','stage-failure','commit-failure','restart-failure','rollback-failure','render-failure')) {
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
            $events = @(Get-Content -LiteralPath (Join-Path $fixture 'progress.json') -Raw -Encoding UTF8 | ConvertFrom-Json)
            $phases = (@($events | Where-Object { $_.Kind -eq 'phase' } | ForEach-Object { $_.Phase })) -join ','
            $wanted = switch ($mode) {
                current { 'metadata' }
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
            $records = @($events | Where-Object { $_.Kind -eq 'progress' -and -not $_.Completed })
            Check (@($records | Where-Object { $_.Id -ne 23 -or $_.Activity -ne 'Обновление ProGo' -or $_.Percent -ne -1 -or [string]::IsNullOrWhiteSpace($_.Operation) }).Count -eq 0) "progress carries explanations without made-up percentages: $mode"
            Check ($events[-1].Kind -eq 'progress' -and $events[-1].Completed) "finally clears progress even after rollback or host failure: $mode"
            foreach ($i in 0..($events.Count - 1)) {
                if ($events[$i].Kind -eq 'dialog') { Check ($i -gt 0 -and $events[$i - 1].Kind -eq 'progress' -and $events[$i - 1].Completed) "progress clears before the result dialog: $mode" }
            }
            Check ((Get-FileHash (Join-Path $install 'settings.json')).Hash -eq $settings -and (Get-FileHash (Join-Path $install 'vault.enc.json')).Hash -eq $vault) "progress preserves settings and opaque vault: $mode"
            if ($mode -in @('success','render-failure')) {
                Check ($text.Contains('Этап 10 из 10: Завершаю обновление') -and -not $text.Contains('Этап 10 из 10: Запускаю ProGo')) "NoLaunch never claims the app restarted: $mode"
                Check ($text.Contains('Этап 3 из 10: Скачиваю обновление') -and $text.Contains('не закрывайте окно')) "plain text survives disabled progress rendering: $mode"
            } else { Check (-not $text.Contains('Обновление завершено. Резервная копия сохранена.')) "failed or current update never prints success: $mode" }
            if ($mode -notin @('success','render-failure','rollback-failure')) {
                Check ((Get-Content -LiteralPath (Join-Path $install 'VERSION') -Raw).Trim() -eq $local) "failure/current keeps or rolls back the installed version: $mode"
            }
            $lease = [ProGo.MaintenanceOperation]::Enter(); $lease.Dispose()
            Check ($true) "progress failure paths release maintenance ownership: $mode"
        } finally { $process.Dispose() }
    }
    Write-Host "Updater progress tests PASS: $passed"
} finally { if (Test-Path $work) { Remove-Item -LiteralPath $work -Recurse -Force } }
