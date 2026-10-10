param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$root = Join-Path $env:TEMP ('ProGo-integrity-links-' + [guid]::NewGuid().ToString('N'))
$target = Join-Path $root 'target'
$backup = Join-Path $root 'backup'
$source = Join-Path $root 'source'
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
    New-Item -ItemType Directory $source | Out-Null
    $f22homeArchivePath = Join-Path $source 'home-vpn-private'
    New-Item -ItemType Junction -Path $f22homeArchivePath -Target $target | Out-Null
    $rejected = $false
    try { [ProGo.BackupIntegrity]::CopyPersonalArchives($source, $backup) } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path (Join-Path $backup 'home-vpn-private'))) { throw 'Personal archive followed source home junction' }
    [IO.Directory]::Delete($f22homeArchivePath)
    New-Item -ItemType Directory $f22homeArchivePath | Out-Null
    New-Item -ItemType Junction -Path (Join-Path $f22homeArchivePath 'access.dat') -Target $target | Out-Null
    $rejected = $false
    try { [ProGo.BackupIntegrity]::CopyPersonalArchives($source, $backup) } catch { $rejected = $true }
    if (-not $rejected -or -not (Test-Path (Join-Path $target 'payload.ps1'))) { throw 'Personal archive followed protected-file junction' }
    Write-Host 'Backup integrity junction tests PASS: writer, root, personal source folder and selected private file refuse traversal; target preserved'
} finally {
    # Remove the junction itself before normal recursive fixture cleanup.
    if (Test-Path (Join-Path $backup 'scripts')) { [IO.Directory]::Delete((Join-Path $backup 'scripts')) }
    if (Test-Path (Join-Path $source 'home-vpn-private\access.dat')) { [IO.Directory]::Delete((Join-Path $source 'home-vpn-private\access.dat')) }
    elseif ((Test-Path (Join-Path $source 'home-vpn-private')) -and ((Get-Item -LiteralPath (Join-Path $source 'home-vpn-private')).Attributes -band [IO.FileAttributes]::ReparsePoint)) { [IO.Directory]::Delete((Join-Path $source 'home-vpn-private')) }
    if (Test-Path $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
