param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$checks = 0
function Check($Condition, $Name) {
    if (-not $Condition) { throw $Name }
    $script:checks++; Write-Host ('PASS: ' + $Name)
}
$work = Join-Path $env:TEMP ('ProGo-rescue-helper-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory $work | Out-Null
    $sentinel = Join-Path $work 'settings.json'
    [IO.File]::WriteAllText($sentinel, 'private-settings-fixture')
    [IO.File]::WriteAllText((Join-Path $work 'VERSION'), '99.99.99')
    $helper = Join-Path $Scripts 'Measure-RescueProGo.ps1'
    $tokens = $null; $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($helper, [ref]$tokens, [ref]$errors)
    Check (@($errors).Count -eq 0) 'shipped Rescue helper parses directly with Windows PowerShell'
    $report = & $helper -ProcessId $PID -DurationSeconds 2 -IntervalMilliseconds 250
    Check ($report -is [string] -and $report.Contains('ProGo: локальное измерение Windows Rescue')) 'real helper emits one sanitized text report'
    Check ($report.Contains('PASS — образцы процесса') -and $report.Contains('фактический порт не указан')) 'real current-process samples do not invent local port settings'
    Check (@(Get-ChildItem -LiteralPath $work -Force).Count -eq 2 -and [IO.File]::ReadAllText($sentinel) -eq 'private-settings-fixture') 'default invocation creates no report and preserves unrelated state'
    Check (-not $report.Contains('private-settings-fixture') -and -not $report.Contains('99.99.99')) 'settings contents and unrelated VERSION are absent'
    $executable = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $reportFile = Join-Path $work 'owner-selected-report.txt'
    $fileReport = & $helper -ExecutablePath $executable -DurationSeconds 2 -ReportPath $reportFile
    Check ($fileReport.Contains('PID не выбран') -and -not $fileReport.Contains('PASS — образцы процесса')) 'file-only selection identifies the file without claiming a running application'
    Check ([IO.File]::ReadAllText($reportFile) -eq $fileReport) 'only explicit ReportPath writes exactly the displayed report'
    $wrongFile = Join-Path $work 'different.exe'; [IO.File]::WriteAllText($wrongFile, 'not a running binary')
    $mismatch = & $helper -ProcessId $PID -ExecutablePath $wrongFile -DurationSeconds 2 -IntervalMilliseconds 250
    Check ($mismatch.Contains('FAIL — выбранный PID запускает другой EXE') -and -not $mismatch.Contains('not a running binary')) 'wrong selected file cannot replace running process identity'
    Write-Host ('Rescue helper tests PASS: ' + $checks)
} finally { if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force } }
