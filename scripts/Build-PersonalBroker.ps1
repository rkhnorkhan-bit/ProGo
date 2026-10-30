param([ValidateSet('Prepare','Seal')][string]$Phase,
      [Parameter(Mandatory=$true)][string]$BuildDirectory,
      [Parameter(Mandatory=$true)][string]$ReleaseDirectory,
      $Prepared)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$imageTool = Join-Path $PSScriptRoot 'personal_broker_image.py'
$native = Join-Path $repo 'native/personal_broker.c'
$setup = Join-Path $BuildDirectory 'ProGo.Setup.exe'
$origin = Join-Path $BuildDirectory 'PersonalBroker.origin'

if ($Phase -ceq 'Prepare') {
    # Existing compiler/SDK only. No package manager, download or installer fallback.
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'Personal broker needs the existing MSVC x64 toolchain.' }
    $vs = @(& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath)
    if ($LASTEXITCODE -ne 0 -or $vs.Count -ne 1) { throw 'Existing MSVC x64 toolchain is unavailable or ambiguous.' }
    $vcvars = Join-Path $vs[0] 'VC/Auxiliary/Build/vcvars64.bat'
    foreach ($path in @($vcvars,$native,$setup,$BuildDirectory)) {
        if ($path.IndexOfAny([char[]]@('"',"`r","`n",'%','!')) -ge 0) { throw 'Native build path cannot be represented safely.' }
    }
    . (Join-Path $PSScriptRoot 'Install-ProGo.ps1') -FunctionsOnly
    $names = @(Get-ProGoInstallerPayloadNames) + @('PersonalBroker.ps1')
    $provider = New-Object Security.Cryptography.CspParameters 24
    $provider.Flags = [Security.Cryptography.CspProviderFlags]::CreateEphemeralKey
    $seal = [Security.Cryptography.RSACryptoServiceProvider]::new(3072,$provider)
    $seal.PersistKeyInCsp = $false
    try {
        $public = $seal.ExportParameters($false)
        $blob = New-Object IO.MemoryStream
        $writer = New-Object IO.BinaryWriter($blob)
        foreach ($number in @(0x31415352,3072,$public.Exponent.Length,$public.Modulus.Length,0,0)) { $writer.Write([uint32]$number) }
        $writer.Write([byte[]]$public.Exponent); $writer.Write([byte[]]$public.Modulus); $writer.Flush()
        $header = '#define pg_payload_count ' + $names.Count + "u`r`nstatic const unsigned char pg_public_key[] = {" +
            (($blob.ToArray() | ForEach-Object { [string]$_ }) -join ',') + "};`r`nstatic const wchar_t *pg_payload_names[] = {`r`n" +
            (($names | ForEach-Object { 'L"' + $_ + '"' }) -join ",`r`n") + "`r`n};`r`n"
        [IO.File]::WriteAllText((Join-Path $BuildDirectory 'broker_key.h'),$header,[Text.Encoding]::ASCII)
        $blob.Dispose()
        $command = '@echo off' + "`r`nchcp 65001 >nul`r`ncall `"$vcvars`" >nul`r`nif errorlevel 1 exit /b 1`r`n" +
            "cl.exe /nologo /O2 /MT /W4 /D_CRT_SECURE_NO_WARNINGS /I`"$BuildDirectory`" /Fo`"$BuildDirectory\personal_broker.obj`" /Fe`"$setup`" `"$native`" /link /SUBSYSTEM:WINDOWS /DYNAMICBASE /NXCOMPAT /MANIFEST:EMBED /MANIFESTUAC:`"level='asInvoker' uiAccess='false'`"`r`nif errorlevel 1 exit /b 1`r`n" +
            "cl.exe /nologo /O2 /MT /W4 /D_CRT_SECURE_NO_WARNINGS /DPROGO_BROKER_NATIVE_FIXTURE /I`"$BuildDirectory`" /Fo`"$BuildDirectory\personal_broker_fixture.obj`" /Fe`"$BuildDirectory\ProGo.BrokerFixture.exe`" `"$native`" /link /SUBSYSTEM:WINDOWS /DYNAMICBASE /NXCOMPAT /MANIFEST:EMBED /MANIFESTUAC:`"level='asInvoker' uiAccess='false'`"`r`nexit /b %errorlevel%`r`n"
        $commandPath = Join-Path $BuildDirectory 'build-personal-broker.cmd'
        [IO.File]::WriteAllText($commandPath,$command,(New-Object Text.UTF8Encoding($false)))
        & $env:ComSpec /d /c $commandPath | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) { throw 'Native personal broker compilation failed.' }
        $digest = @(& python $imageTool $setup)
        if ($LASTEXITCODE -ne 0 -or $digest.Count -ne 1 -or $digest[0] -cnotmatch '^[A-F0-9]{64}$') { throw 'Native bootstrap identity failed.' }
        [IO.File]::WriteAllText($origin,($digest[0] + "`n" + $seal.ToXmlString($false)),[Text.Encoding]::UTF8)
        # Only public material is embedded. The short-lived build seal is neither
        # a publisher certificate nor a user credential; private bytes never leave
        # this builder process or enter logs, files, payloads or artifacts.
        return [pscustomobject]@{ Seal = $seal; Names = $names; Digest = $digest[0]; Origin = $origin; Setup = $setup }
    } catch { $seal.Dispose(); throw }
}

if ($null -eq $Prepared) { throw 'Personal broker seal requires this build process.' }
try {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PersonalBroker.ps1') -Destination (Join-Path $ReleaseDirectory 'PersonalBroker.ps1') -Force
    $memory = New-Object IO.MemoryStream
    $writer = New-Object IO.BinaryWriter($memory)
    $writer.Write([Text.Encoding]::ASCII.GetBytes('PGPK0001')); $writer.Write([uint32]$Prepared.Names.Count)
    foreach ($name in $Prepared.Names) {
        $encoded = [Text.Encoding]::ASCII.GetBytes($name)
        $bytes = [IO.File]::ReadAllBytes((Join-Path $ReleaseDirectory $name))
        $writer.Write([uint16]$encoded.Length); $writer.Write([uint32]$bytes.Length); $writer.Write($encoded); $writer.Write($bytes)
    }
    $writer.Flush(); $archive = $memory.ToArray(); $memory.Dispose()
    if ($archive.Length -gt 64MB) { throw 'Personal broker archive exceeds its fixed bound.' }
    $signature = $Prepared.Seal.SignData($archive,'SHA256')
    if (-not ('ProGoBuildResource' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class ProGoBuildResource {
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    public static extern IntPtr BeginUpdateResourceW(string path, bool delete);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool UpdateResourceW(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] bytes, uint length);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool EndUpdateResourceW(IntPtr update, bool discard);
}
'@
    }
    $update = [ProGoBuildResource]::BeginUpdateResourceW($Prepared.Setup,$false)
    if ($update -eq [IntPtr]::Zero) { throw 'Cannot open bootstrap resources.' }
    try {
        if (-not [ProGoBuildResource]::UpdateResourceW($update,[IntPtr]10,[IntPtr]101,1033,$archive,$archive.Length) -or
            -not [ProGoBuildResource]::UpdateResourceW($update,[IntPtr]10,[IntPtr]102,1033,$signature,$signature.Length)) { throw 'Cannot seal bootstrap resources.' }
        if (-not [ProGoBuildResource]::EndUpdateResourceW($update,$false)) { throw 'Cannot commit bootstrap resources.' }
        $update = [IntPtr]::Zero
    } finally { if ($update -ne [IntPtr]::Zero) { [void][ProGoBuildResource]::EndUpdateResourceW($update,$true) } }
    $digest = @(& python $imageTool $Prepared.Setup)
    if ($LASTEXITCODE -ne 0 -or $digest.Count -ne 1 -or $digest[0] -cne $Prepared.Digest) { throw 'Sealing changed the native bootstrap code identity.' }
    if ((Get-Item -LiteralPath $Prepared.Setup).Length -gt 64MB) { throw 'Sealed native bootstrap exceeds its runtime bound.' }
    Copy-Item -LiteralPath $Prepared.Setup -Destination (Join-Path $ReleaseDirectory 'ProGo.Setup.exe') -Force
} finally { $Prepared.Seal.Dispose() }
