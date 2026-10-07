Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
# Load only pure updater functions, never its top-level installer/rollback actions.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Updater does not parse' }
foreach ($function in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $false)) {
    . ([scriptblock]::Create($function.Extent.Text))
}
function Write-UpdateLog($Message) { Write-Host $Message }
$RemoteVersionUrl = 'https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest'
$NoReleasePackage = $false
$State = @{}
$work = Join-Path $Root 'build\update-fixture'
New-Item -ItemType Directory -Force $work | Out-Null
$fixture = Join-Path $work 'source'
New-Item -ItemType Directory -Force (Join-Path $fixture 'scripts') | Out-Null
Set-Content (Join-Path $fixture 'VERSION') '0.2.0'
Set-Content (Join-Path $fixture 'ProGo.exe') 'fixture only; never execute'
Set-Content (Join-Path $fixture 'scripts\Update-ProGo.ps1') '# fixture'
Set-Content (Join-Path $fixture 'scripts\Update-ProGo.Core.ps1') '# fixture'
$zip = Join-Path $work 'fixture.zip'
Compress-Archive -Path (Join-Path $fixture '*') -DestinationPath $zip -Force
$digest = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$release = @{ tag_name = 'v0.2.0'; draft = $false; prerelease = $false; assets = @(@{ name = 'ProGo-release.zip'; browser_download_url = 'https://github.com/rkhnorkhan-bit/ProGo/releases/download/v0.2.0/ProGo-release.zip'; digest = "sha256:$digest" }) }
function Save-UpdatePackage($Url, $Path) { Copy-Item -LiteralPath $zip -Destination $Path }
function Read-UpdateMetadata($Url) { return ($release | ConvertTo-Json -Depth 5) }
$State.RemoteVersion = Get-RemoteVersion
if ($State.RemoteVersion -ne '0.2.0') { throw 'Wrong release version' }
$valid = Try-GetReleasePackage (Join-Path $work 'valid')
if (-not (Test-Path (Join-Path $valid 'VERSION'))) { throw 'Valid release rejected' }
Write-Host 'PASS: published package metadata, hash and version accepted'
$State.ReleaseSha256 = '0' * 64
$rejected = $false
try { Try-GetReleasePackage (Join-Path $work 'bad-hash') } catch { $rejected = $_.Exception.Message.Contains('SHA-256 mismatch') }
if (-not $rejected -or (Test-Path (Join-Path $work 'bad-hash\ProGo.exe'))) { throw 'Corrupt package was extracted' }
Write-Host 'PASS: corrupt archive rejected before extraction'
$release.assets[0].digest = ''
$rejected = $false; try { Get-RemoteVersion } catch { $rejected = $true }
if (-not $rejected) { throw 'Missing digest accepted' }
Write-Host 'PASS: missing digest fails closed'
$release.assets[0].digest = "sha256:$digest"
$release.assets[0].browser_download_url = 'https://example.org/other.zip'
$rejected = $false; try { Get-RemoteVersion } catch { $rejected = $true }
if (-not $rejected) { throw 'Unexpected download host accepted' }
Write-Host 'PASS: unexpected release host rejected'
$evil = Join-Path $work 'traversal.zip'
$archive = [IO.Compression.ZipFile]::Open($evil, [IO.Compression.ZipArchiveMode]::Create)
try { [void]$archive.CreateEntry('../outside.txt') } finally { $archive.Dispose() }
$rejected = $false; try { Test-ReleaseArchive $evil (Join-Path $work 'unzip') } catch { $rejected = $true }
if (-not $rejected) { throw 'Archive traversal accepted' }
Write-Host 'PASS: archive path traversal rejected'
Write-Host 'Updater package tests PASS: 5'
