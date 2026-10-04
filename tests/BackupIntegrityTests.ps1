param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$root = Join-Path $env:TEMP ('ProGo-integrity-links-' + [guid]::NewGuid().ToString('N'))
$target = Join-Path $root 'target'
$backup = Join-Path $root 'backup'
try {
    New-Item -ItemType Directory -Path $target,$backup | Out-Null
    Set-Content (Join-Path $target 'payload.ps1') 'foreign fixture'
    $link = Join-Path $backup 'scripts'
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    $rejected = $false
    try { [ProGo.BackupIntegrity]::Write($backup) } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path (Join-Path $backup 'backup-files.sha256'))) { throw 'Writer followed junction' }
    $rejected = $false
    try { [ProGo.BackupIntegrity]::Write($link) } catch { $rejected = $true }
    if (-not $rejected -or -not (Test-Path (Join-Path $target 'payload.ps1'))) { throw 'Root junction was not preserved/refused' }
    Write-Host 'Backup integrity junction tests PASS: writer and root refuse traversal; target preserved'
} finally {
    # Remove the junction itself before normal recursive fixture cleanup.
    if (Test-Path (Join-Path $backup 'scripts')) { [IO.Directory]::Delete((Join-Path $backup 'scripts')) }
    if (Test-Path $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
