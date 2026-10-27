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

function Get-ProGoDrawingReferences {
    param([Parameter(Mandatory=$true)][System.Reflection.Assembly[]]$Roots,
          [Parameter(Mandatory=$true)][string]$RuntimeReference)
    # Restrict resolution to the current host and the actual loaded drawing assemblies' directories.
    $rootsAllowed = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    [void]$rootsAllowed.Add([IO.Path]::GetFullPath($PSHOME).TrimEnd([IO.Path]::DirectorySeparatorChar))
    $hostRuntime = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath([object].Assembly.Location))
    [void]$rootsAllowed.Add($hostRuntime)
    foreach ($assembly in $Roots) {
        if ([string]::IsNullOrEmpty($assembly.Location)) { throw 'Drawing assembly has no physical location.' }
        $directory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($assembly.Location))
        if (-not $rootsAllowed.Contains($directory)) { throw "Drawing assembly is outside the current host: $($assembly.GetName().Name)" }
    }
    $pending = New-Object 'System.Collections.Generic.Queue[System.Reflection.Assembly]'
    $seen = @{}; $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($assembly in $Roots) { $pending.Enqueue($assembly) }
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue(); $identity = $current.GetName()
        if ($seen.ContainsKey($identity.Name)) {
            if ($seen[$identity.Name] -cne $identity.FullName) { throw "Conflicting drawing dependency: $($identity.Name)" }
            continue
        }
        $seen[$identity.Name] = $identity.FullName
        if ([string]::IsNullOrEmpty($current.Location) -or
            -not $rootsAllowed.Contains([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($current.Location)))) {
            throw "Drawing dependency is outside the current host: $($identity.Name)"
        }
        $paths.Add($current.Location)
        Write-Host ("Icon dependency host=" + $PSVersionTable.PSEdition + "/" + $PSVersionTable.PSVersion + "; assembly=" + $identity.FullName)
        foreach ($reference in $current.GetReferencedAssemblies()) {
            if (-not $reference.Name.StartsWith('System.Private.Windows.', [StringComparison]::Ordinal)) { continue }
            if ($seen.ContainsKey($reference.Name)) {
                if ($seen[$reference.Name] -cne $reference.FullName) { throw "Mismatched drawing dependency: $($reference.Name)" }
                continue
            }
            $candidate = $null
            foreach ($directory in $rootsAllowed) {
                $path = Join-Path $directory ($reference.Name + '.dll')
                if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
                $fileIdentity = [Reflection.AssemblyName]::GetAssemblyName($path)
                if ($fileIdentity.FullName -cne $reference.FullName) { throw "Mismatched drawing dependency: $($reference.Name)" }
                if ($null -ne $candidate -and -not [String]::Equals($candidate, $path, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Ambiguous drawing dependency: $($reference.Name)"
                }
                $candidate = $path
            }
            if ($null -eq $candidate) { throw "Missing drawing dependency: $($reference.Name)" }
            $loaded = [Reflection.Assembly]::LoadFrom($candidate)
            if ($loaded.GetName().FullName -cne $reference.FullName -or
                -not [String]::Equals([IO.Path]::GetFullPath($loaded.Location), [IO.Path]::GetFullPath($candidate), [StringComparison]::OrdinalIgnoreCase)) {
                throw "Loaded drawing dependency mismatch: $($reference.Name)"
            }
            $pending.Enqueue($loaded)
        }
    }
    $paths.Add($RuntimeReference)
    return $paths.ToArray()
}

function New-ProGoIconFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if ($PSVersionTable.PSEdition -eq 'Core') {
        # Resolve the actual runtime implementations and the host's .NET reference assembly.
        Add-Type -AssemblyName System.Drawing.Common -ErrorAction Stop
        $bitmapAssembly = [System.Drawing.Bitmap].Assembly.Location
        $colorAssembly = [System.Drawing.Color].Assembly.Location
        $runtimeReference = Join-Path $PSHOME 'ref\System.Runtime.dll'
        if (-not (Test-Path -LiteralPath $runtimeReference -PathType Leaf)) {
            $runtimeReference = Join-Path $PSHOME 'System.Runtime.dll'
        }
        foreach ($dependency in @($bitmapAssembly, $colorAssembly, $runtimeReference)) {
            if ([string]::IsNullOrEmpty($dependency) -or -not (Test-Path -LiteralPath $dependency -PathType Leaf)) {
                throw 'PowerShell 7 icon generation dependency is unavailable.'
            }
        }
        $drawingReferences = @(Get-ProGoDrawingReferences -Roots @([System.Drawing.Bitmap].Assembly, [System.Drawing.Color].Assembly) -RuntimeReference $runtimeReference)
        Add-Type -Path (Join-Path $Src "BrandIcon.cs") -ReferencedAssemblies $drawingReferences -ErrorAction Stop
    } else {
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop
        Add-Type -Path (Join-Path $Src "BrandIcon.cs") -ReferencedAssemblies System.Drawing -ErrorAction Stop
    }
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

# Validate build and mandatory packaging inputs before replacing prior outputs.
$Sources = @(Get-ChildItem -Path $Src -Filter "*.cs" -File | Sort-Object FullName | ForEach-Object { $_.FullName })
if ($Sources.Count -eq 0) { throw "C# sources not found in $Src" }
$requiredInputs = @(
    'src\BrandIcon.cs', 'src\ApplicationShortcuts.cs', 'src\MaintenanceOperation.cs',
    'src\InstalledUpdateTransport.cs', 'src\UpdateInstallSession.cs', 'src\BackupRetention.cs',
    'src\BackupIntegrity.cs', 'src\BoundedLog.cs', 'src\RescueMeasurement.cs',
    'src\DiagnosticReport.cs', 'src\DiagnosticPreview.cs', 'src\UiTheme.cs',
    'docs\HOME_IKEV2.md', 'server\ikev2_relay.py', 'server\install-ikev2-relay.sh',
    'server\make_home_profile.py', 'server\home_vpn_setup.py', 'server\profile_share_setup.py',
    'server\profile_share.py', 'server\qrcodegen.py', 'server\QR_LICENSE.txt'
)
foreach ($inputPath in $requiredInputs) {
    if (-not (Test-Path -LiteralPath (Join-Path $Root $inputPath) -PathType Leaf)) {
        throw "Build dependency is missing: $inputPath"
    }
}

# Generate the complete icon first: missing dependencies or rendering failures must
# not remove the existing build and release directories.
$preflightIcon = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-icon-' + [Guid]::NewGuid().ToString('N') + '.ico')
try {
    New-ProGoIconFile -Path $preflightIcon
    if (-not (Test-Path -LiteralPath $preflightIcon -PathType Leaf) -or (Get-Item -LiteralPath $preflightIcon).Length -le 0) {
        throw 'Icon preflight did not produce an icon.'
    }
    if (Test-Path $Release) {
        Remove-Item $Release -Recurse -Force
    }
    if (Test-Path $BuildDir) {
        Remove-Item $BuildDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Release | Out-Null
    New-Item -ItemType Directory -Path $BuildDir | Out-Null
    Copy-Item -LiteralPath $preflightIcon -Destination $IconPath
} finally {
    if (Test-Path -LiteralPath $preflightIcon -PathType Leaf) { Remove-Item -LiteralPath $preflightIcon -Force }
}

$Version = (Get-Content -Raw $VersionFile).Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version' }
# VERSION identifies the product release; the reviewed commit identifies a candidate build.
# Local uncommitted changes must not masquerade as the exact reviewed commit.
$BuildRevision = 'local'
# A materialized source tree may have no Git metadata. Avoid invoking git there:
# Windows PowerShell 5.1 can promote native stderr to a terminating error.
$gitProbe = [IO.Path]::GetFullPath($Root)
$hasGitMetadata = $false
while (-not [string]::IsNullOrEmpty($gitProbe)) {
    if (Test-Path -LiteralPath (Join-Path $gitProbe '.git') -ErrorAction Stop) { $hasGitMetadata = $true; break }
    $gitProbe = [IO.Path]::GetDirectoryName($gitProbe)
}
# Explicit Git environment may designate a repository outside the directory tree.
if (-not [string]::IsNullOrEmpty($env:GIT_DIR) -or -not [string]::IsNullOrEmpty($env:GIT_WORK_TREE)) { $hasGitMetadata = $true }
if ($hasGitMetadata) {
    $gitCommand = Get-Command git -ErrorAction Stop
    $revisionOutput = & $gitCommand.Source -C $Root rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or "$revisionOutput" -cnotmatch '^[0-9a-f]{40}$') { throw 'Git checkout revision could not be verified.' }
    $BuildRevision = "$revisionOutput"
    $dirtyOutput = & $gitCommand.Source -C $Root status --porcelain --untracked-files=no
    if ($LASTEXITCODE -ne 0) { throw 'Git checkout status could not be verified.' }
    if ($dirtyOutput) { $BuildRevision += '.modified' }
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
