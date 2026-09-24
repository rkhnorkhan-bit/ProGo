param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Src = Join-Path $Root "src"
$Release = Join-Path $Root "release"
$Out = Join-Path $Release "ProGo.exe"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $Csc)) {
    throw "csc.exe не найден: $Csc"
}

if (Test-Path $Release) {
    Remove-Item $Release -Recurse -Force
}
New-Item -ItemType Directory -Path $Release | Out-Null

$Sources = Get-ChildItem -Path $Src -Filter "*.cs" -File | Sort-Object FullName | ForEach-Object { $_.FullName }
if ($Sources.Count -eq 0) {
    throw "Исходники C# не найдены в $Src"
}

$Args = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/out:$Out",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Web.Extensions.dll"
) + $Sources

Write-Host "Building ProGo..."
& $Csc @Args
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$Readme = Join-Path $Root "README.md"
$License = Join-Path $Root "LICENSE.md"
if (Test-Path $Readme) { Copy-Item $Readme -Destination (Join-Path $Release "README.md") -Force }
if (Test-Path $License) { Copy-Item $License -Destination (Join-Path $Release "LICENSE.md") -Force }

$ReleaseScripts = Join-Path $Release "scripts"
New-Item -ItemType Directory -Path $ReleaseScripts -Force | Out-Null
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Install-FromGitHub.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $ReleaseScripts $scriptName) -Force
    }
}

Write-Host "Build OK: $Out"
Write-Host "Release folder: $Release"
