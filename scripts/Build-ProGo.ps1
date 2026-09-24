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
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

function New-ProGoIconFile {
    param([Parameter(Mandatory=$true)][string]$Path)

    Add-Type -AssemblyName System.Drawing
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class ProGoNativeIconMethods {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

    $bitmap = New-Object System.Drawing.Bitmap 64, 64
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
    $backgroundBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(24, 64, 160))
    $accentBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(69, 214, 147))
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $font = New-Object System.Drawing.Font "Segoe UI", 36, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $fileStream = $null
    $icon = $null
    $hIcon = [IntPtr]::Zero

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.FillEllipse($shadowBrush, 6, 7, 54, 54)
        $graphics.FillEllipse($backgroundBrush, 4, 4, 56, 56)
        $graphics.FillEllipse($accentBrush, 43, 10, 10, 10)
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $graphics.DrawString("P", $font, $whiteBrush, (New-Object System.Drawing.RectangleF 0, 5, 64, 54), $format)

        $hIcon = $bitmap.GetHicon()
        $icon = [System.Drawing.Icon]::FromHandle($hIcon)
        $fileStream = [System.IO.File]::Create($Path)
        $icon.Save($fileStream)
    }
    finally {
        if ($fileStream -ne $null) { $fileStream.Dispose() }
        if ($icon -ne $null) { $icon.Dispose() }
        if ($hIcon -ne [IntPtr]::Zero) { [void][ProGoNativeIconMethods]::DestroyIcon($hIcon) }
        $format.Dispose()
        $font.Dispose()
        $whiteBrush.Dispose()
        $accentBrush.Dispose()
        $backgroundBrush.Dispose()
        $shadowBrush.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

if (-not (Test-Path $Csc)) {
    throw "csc.exe не найден: $Csc"
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
    throw "Исходники C# не найдены в $Src"
}

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
Copy-Item $IconPath -Destination (Join-Path $Release "ProGo.ico") -Force

$ReleaseScripts = Join-Path $Release "scripts"
New-Item -ItemType Directory -Path $ReleaseScripts -Force | Out-Null
foreach ($scriptName in @("Install-ProGo.ps1", "Uninstall-ProGo.ps1", "Update-ProGo.ps1", "Install-FromGitHub.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (Test-Path $scriptPath) {
        Copy-Item $scriptPath -Destination (Join-Path $ReleaseScripts $scriptName) -Force
    }
}

Write-Host "Build OK: $Out"
Write-Host "Icon OK: $IconPath"
Write-Host "Release folder: $Release"
