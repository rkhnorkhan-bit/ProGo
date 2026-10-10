param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Src = Join-Path $Root "src"
$Release = Join-Path $Root "release"
$BuildDir = Join-Path $Root "build"
$Out = Join-Path $Release "ProGo.exe"
$IconPath = Join-Path $BuildDir "ProGo.ico"
$VersionFile = Join-Path $Root "VERSION"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

function New-ProGoIconFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    Add-Type -AssemblyName System.Drawing
    Add-Type -Path (Join-Path $Src "BrandIcon.cs") -ReferencedAssemblies System.Drawing
    [ProGo.BrandIcon]::WriteIcon($Path)
}

if (-not (Test-Path $Csc)) {
    throw "csc.exe not found: $Csc"
}

if (-not (Test-Path $VersionFile)) {
    throw "VERSION file not found: $VersionFile"
}

$Version = (Get-Content -Raw $VersionFile).Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version' }
foreach ($readmeName in @('README.md', 'README.ru.md')) {
    $heading = (Get-Content -LiteralPath (Join-Path $Root $readmeName) -Encoding UTF8 -TotalCount 1)
    if ($heading -ne "# ProGo $Version") { throw "Documentation version mismatch: $readmeName; update heading to VERSION before building." }
}

if (Test-Path $Release) {
    Remove-Item $Release -Recurse -Force
}
if (Test-Path $BuildDir) {
    Remove-Item $BuildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $Release | Out-Null
New-Item -ItemType Directory -Path $BuildDir | Out-Null
New-ProGoIconFile -Path $IconPath

$Sources = @(Get-ChildItem -Path $Src -Filter "*.cs" -File | Sort-Object FullName | ForEach-Object { $_.FullName })
if ($Sources.Count -eq 0) {
    throw "C# sources not found in $Src"
}

$Version = (Get-Content -Raw $VersionFile).Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version' }
# VERSION identifies the product release; the reviewed commit identifies a candidate build.
# Local uncommitted changes must not masquerade as the exact reviewed commit.
$BuildRevision = 'local'
if (Get-Command git -ErrorAction SilentlyContinue) {
    $revisionOutput = & git -C $Root rev-parse HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and "$revisionOutput" -match '^[0-9a-f]{40}$') {
        $BuildRevision = "$revisionOutput"
        $dirtyOutput = & git -C $Root status --porcelain --untracked-files=no 2>$null
        if ($LASTEXITCODE -ne 0 -or $dirtyOutput) { $BuildRevision += '.modified' }
    }
}
$BuildIdentity = "$Version+$BuildRevision"
$VersionSource = Join-Path $BuildDir 'VersionInfo.cs'
@"
using System.Reflection;
[assembly: AssemblyTitle("ProGo")]
[assembly: AssemblyDescription("Local proxy, home VPN and encrypted vault")]
[assembly: AssemblyProduct("ProGo")]
[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$BuildIdentity")]
"@ | Set-Content -LiteralPath $VersionSource -Encoding UTF8
$Sources += $VersionSource

$Args = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/win32icon:$IconPath",
    "/out:$Out",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Security.dll",
    "/reference:System.Xml.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Web.Extensions.dll"
) + $Sources

Write-Host "Building ProGo..."
& $Csc @Args
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

if ([Reflection.AssemblyName]::GetAssemblyName($Out).Version.ToString(3) -ne $Version) {
    throw 'Compiled application version does not match VERSION'
}
$BuiltAssembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($Out))
$BuiltIdentity = @($BuiltAssembly.GetCustomAttributes([Reflection.AssemblyInformationalVersionAttribute], $false))[0].InformationalVersion
if ($BuiltIdentity -ne $BuildIdentity) { throw 'Compiled application build identity does not match source revision' }
Write-Host "Build identity: $BuiltIdentity"

$Readme = Join-Path $Root "README.md"
$RussianReadme = Join-Path $Root "README.ru.md"
$License = Join-Path $Root "LICENSE.md"
Copy-Item $RussianReadme -Destination (Join-Path $Release "README.ru.md") -Force
if (Test-Path $Readme) { Copy-Item $Readme -Destination (Join-Path $Release "README.md") -Force }
if (Test-Path $License) { Copy-Item $License -Destination (Join-Path $Release "LICENSE.md") -Force }
Copy-Item $IconPath -Destination (Join-Path $Release "ProGo.ico") -Force
Copy-Item $VersionFile -Destination (Join-Path $Release "VERSION") -Force

$ReleaseScripts = Join-Path $Release "scripts"
New-Item -ItemType Directory -Path $ReleaseScripts -Force | Out-Null
# Install-FromGitHub.ps1 is bootstrap-only. Do not include it in the runtime release:
# some endpoint protection tools block downloaded bootstrap installers during self-update builds.
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Enable-HomeVpnFirewall.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Measure-RescueProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $ReleaseScripts $scriptName) -Force
    }
}

Copy-Item (Join-Path $Src "ApplicationShortcuts.cs") -Destination (Join-Path $ReleaseScripts "ApplicationShortcuts.cs") -Force
Copy-Item (Join-Path $Src "MaintenanceOperation.cs") -Destination (Join-Path $ReleaseScripts "MaintenanceOperation.cs") -Force
Copy-Item (Join-Path $Src "InstalledUpdateTransport.cs") -Destination (Join-Path $ReleaseScripts "InstalledUpdateTransport.cs") -Force
Copy-Item (Join-Path $Src "UpdateInstallSession.cs") -Destination (Join-Path $ReleaseScripts "UpdateInstallSession.cs") -Force
Copy-Item (Join-Path $Src "BackupRetention.cs") -Destination (Join-Path $ReleaseScripts "BackupRetention.cs") -Force
Copy-Item (Join-Path $Src "BackupIntegrity.cs") -Destination (Join-Path $ReleaseScripts "BackupIntegrity.cs") -Force
Copy-Item (Join-Path $Src "BoundedLog.cs") -Destination (Join-Path $ReleaseScripts "BoundedLog.cs") -Force
Copy-Item (Join-Path $Src "RescueMeasurement.cs") -Destination (Join-Path $ReleaseScripts "RescueMeasurement.cs") -Force
Copy-Item (Join-Path $Src "DiagnosticReport.cs") -Destination (Join-Path $ReleaseScripts "DiagnosticReport.cs") -Force
Copy-Item (Join-Path $Src "DiagnosticPreview.cs") -Destination (Join-Path $ReleaseScripts "DiagnosticPreview.cs") -Force
Copy-Item (Join-Path $Src "UiTheme.cs") -Destination (Join-Path $ReleaseScripts "UiTheme.cs") -Force
Copy-Item (Join-Path $Src "BrandIcon.cs") -Destination (Join-Path $ReleaseScripts "BrandIcon.cs") -Force

# Keep optional VPN resources inside scripts so existing transactional updaters
# install and back up them together with the other runtime helpers.
$HomeVpnDir = Join-Path $ReleaseScripts "home-vpn"
$HomeVpnServer = Join-Path $HomeVpnDir "server"
New-Item -ItemType Directory -Path $HomeVpnServer -Force | Out-Null
Copy-Item (Join-Path $Root "docs\HOME_IKEV2.md") -Destination (Join-Path $HomeVpnDir "HOME_IKEV2.md") -Force
foreach ($serverFile in @("ikev2_relay.py", "install-ikev2-relay.sh", "make_home_profile.py", "home_vpn_setup.py", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt")) {
    Copy-Item (Join-Path $Root ("server\" + $serverFile)) -Destination (Join-Path $HomeVpnServer $serverFile) -Force
}

Write-Host "Build OK: $Out"
Write-Host "Icon OK: $IconPath"
Write-Host "Version OK: $((Get-Content -Raw -Path $VersionFile).Trim())"
Write-Host "Release folder: $Release"
