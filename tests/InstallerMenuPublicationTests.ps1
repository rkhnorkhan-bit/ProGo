Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
if (-not ('ProGo.Tests.MenuPublicationOriginTests' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'MenuPublicationOriginTests.cs') }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-publication-' + [Guid]::NewGuid().ToString('N'))
$owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $count = [ProGo.Tests.MenuPublicationOriginTests]::Run([ProGo.ApplicationShortcuts],$work)
    if ($count -ne 6) { throw 'Menu publication origin regression count is incomplete.' }
    Write-Host ('Installer Menu publication origin tests PASS: ' + $count)
} finally {
    if ($owned -and [IO.Directory]::Exists($work) -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-menu-publication-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Publication fixture cleanup refuses linked root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Publication fixture cleanup refuses linked descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
