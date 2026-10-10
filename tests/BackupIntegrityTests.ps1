param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$root = Join-Path $env:TEMP ('ProGo-integrity-links-' + [guid]::NewGuid().ToString('N'))
$target = Join-Path $root 'target'
$backup = Join-Path $root 'backup'
$source = Join-Path $root 'source'
$privateNames = @('access.dat', 'admin-request.dat', 'share-request.dat')
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
    foreach ($name in $privateNames) {
        $privateLink = Join-Path $f22homeArchivePath $name
        New-Item -ItemType Junction -Path $privateLink -Target $target | Out-Null
        $rejected = $false
        try { [ProGo.BackupIntegrity]::CopyPersonalArchives($source, $backup) } catch { $rejected = $true }
        if (-not $rejected -or -not (Test-Path (Join-Path $target 'payload.ps1')) -or (Test-Path (Join-Path $backup ('home-vpn-private\' + $name)))) { throw 'Personal archive followed protected-file junction' }
        [IO.Directory]::Delete($privateLink)
    }
    $compositionBackup = Join-Path $root 'composition'
    New-Item -ItemType Directory (Join-Path $compositionBackup 'scripts'),(Join-Path $f22homeArchivePath 'admin-fixture') | Out-Null
    Set-Content (Join-Path $compositionBackup 'ProGo.exe') 'synthetic fixture; never execute'
    Set-Content (Join-Path $compositionBackup 'VERSION') '0.0.1'
    Set-Content (Join-Path $compositionBackup 'manifest.txt') @('product=ProGo','version=0.0.1')
    Set-Content (Join-Path $compositionBackup 'settings.json') '{}'
    foreach ($name in @('Start-ProGo.ps1','Restore-ProGoBackup.ps1','Update-ProGo.Core.ps1')) {
        Set-Content (Join-Path $compositionBackup ('scripts\' + $name)) 'synthetic script; never execute'
    }
    Add-Type -AssemblyName System.Security
    $requestBytes = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes('synthetic pending owner-bound request; never log'), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    [IO.File]::WriteAllBytes((Join-Path $f22homeArchivePath 'ADMIN-REQUEST.DAT'), $requestBytes)
    $shareRequestBytes = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes('synthetic pending HTTPS-domain request; never log'), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    [IO.File]::WriteAllBytes((Join-Path $f22homeArchivePath 'SHARE-REQUEST.DAT'), $shareRequestBytes)
    Set-Content (Join-Path $f22homeArchivePath 'admin-request.dat.fixture.new') 'temporary plaintext request; must not archive'
    Set-Content (Join-Path $f22homeArchivePath 'share-request.dat.fixture.new') 'temporary HTTPS request; must not archive'
    Set-Content (Join-Path $f22homeArchivePath 'admin-fixture\result.txt') 'temporary plaintext result; must not archive'
    [ProGo.BackupIntegrity]::CopyPersonalArchives($source, $compositionBackup)
    Add-Content (Join-Path $compositionBackup 'manifest.txt') ([ProGo.BackupIntegrity]::CompositionLines($compositionBackup))
    [ProGo.BackupIntegrity]::Write($compositionBackup)
    [ProGo.BackupIntegrity]::Validate($compositionBackup)
    $archives = @([ProGo.BackupIntegrity]::ArchiveNames($compositionBackup))
    $composition = @(Get-Content (Join-Path $compositionBackup 'manifest.txt'))
    if ($archives.Count -ne 2 -or $archives -cnotcontains 'home-vpn-private/admin-request.dat' -or $archives -cnotcontains 'home-vpn-private/share-request.dat' -or
        $composition -cnotcontains ('archived_only=' + ($archives -join ',')) -or
        $composition -cnotcontains 'home_vpn_protection=DPAPI-CurrentUser;not-a-portable-export;no-automatic-import') { throw 'Installed integrity helper omitted or activated pending admin request' }
    if ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $compositionBackup 'home-vpn-private\admin-request.dat'))) -cne [Convert]::ToBase64String($requestBytes)) { throw 'Installed integrity helper changed encrypted pending request bytes' }
    if ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $compositionBackup 'home-vpn-private\share-request.dat'))) -cne [Convert]::ToBase64String($shareRequestBytes)) { throw 'Installed integrity helper changed encrypted pending HTTPS request bytes' }
    $archivedShareRequest = Join-Path $compositionBackup 'home-vpn-private\share-request.dat'
    $alteredShareRequest = [byte[]]$shareRequestBytes.Clone()
    $alteredShareRequest[$alteredShareRequest.Length - 1] = $alteredShareRequest[$alteredShareRequest.Length - 1] -bxor 1
    [IO.File]::WriteAllBytes($archivedShareRequest, $alteredShareRequest)
    $rejected = $false
    try { [ProGo.BackupIntegrity]::Validate($compositionBackup) } catch { $rejected = $true }
    if (-not $rejected) { throw 'Installed helper ignored changed HTTPS request bytes' }
    [IO.File]::WriteAllBytes($archivedShareRequest, $shareRequestBytes)
    [ProGo.BackupIntegrity]::Validate($compositionBackup)
    foreach ($scope in @('Program','Data','All')) {
        $restore = @([ProGo.BackupIntegrity]::RestoreNames($compositionBackup, $scope, $true))
        if ($restore -contains 'home-vpn-private' -or $restore -contains 'home-vpn-private/admin-request.dat' -or $restore -contains 'home-vpn-private/share-request.dat') { throw 'Pending request archive became an automatic restore input' }
    }
    $plaintextTarget = Join-Path $root 'plaintext-request'
    New-Item -ItemType Directory $plaintextTarget | Out-Null
    Set-Content (Join-Path $f22homeArchivePath 'SHARE-REQUEST.DAT') 'plaintext HTTPS request; must not archive'
    $rejected = $false
    try { [ProGo.BackupIntegrity]::CopyPersonalArchives($source, $plaintextTarget) } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path (Join-Path $plaintextTarget 'home-vpn-private\share-request.dat'))) { throw 'Installed helper archived plaintext HTTPS request' }
    Write-Host 'Backup integrity tests PASS: real protected-file junctions refused; installed helper retains exact DPAPI admin and HTTPS requests as archived-only evidence'
} finally {
    # Remove the junction itself before normal recursive fixture cleanup.
    if (Test-Path (Join-Path $backup 'scripts')) { [IO.Directory]::Delete((Join-Path $backup 'scripts')) }
    $privateRoot = Join-Path $source 'home-vpn-private'
    if ((Test-Path $privateRoot) -and ((Get-Item -LiteralPath $privateRoot).Attributes -band [IO.FileAttributes]::ReparsePoint)) { [IO.Directory]::Delete($privateRoot) }
    else {
        foreach ($name in $privateNames) {
            $privateLink = Join-Path $privateRoot $name
            if ((Test-Path $privateLink) -and ((Get-Item -LiteralPath $privateLink).Attributes -band [IO.FileAttributes]::ReparsePoint)) { [IO.Directory]::Delete($privateLink) }
        }
    }
    if (Test-Path $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
