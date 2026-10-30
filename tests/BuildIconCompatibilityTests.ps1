# Run separately under each permitted Windows PowerShell host. Never changes execution policy.
param([Parameter(Mandatory=$true)][ValidateSet('Desktop51','Core7')][string]$ExpectedHost)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if (($ExpectedHost -ceq 'Desktop51' -and ($PSVersionTable.PSEdition -cne 'Desktop' -or $PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1)) -or
    ($ExpectedHost -ceq 'Core7' -and ($PSVersionTable.PSEdition -cne 'Core' -or $PSVersionTable.PSVersion.Major -ne 7))) { throw 'Unexpected PowerShell host.' }
$root = Split-Path -Parent $PSScriptRoot
$temp = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-icon-test-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $temp | Out-Null
    foreach ($name in @('src','scripts','docs','server','native')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $temp $name) -Recurse
    }
    foreach ($name in @('VERSION','README.md','README.ru.md','LICENSE.md')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $temp $name)
    }
    $build = Join-Path $temp 'build'; $release = Join-Path $temp 'release'
    New-Item -ItemType Directory -Path $build,$release | Out-Null
    [IO.File]::WriteAllText((Join-Path $build 'sentinel'),'build survives')
    [IO.File]::WriteAllText((Join-Path $release 'sentinel'),'release survives')
    $iconSource = Join-Path $temp 'src\BrandIcon.cs'
    $savedIconSource = Join-Path $temp 'src\BrandIcon.test-saved'
    Move-Item -LiteralPath $iconSource -Destination $savedIconSource
    $refused = $false
    try { & (Join-Path $temp 'scripts\Build-ProGo.ps1') } catch { $refused = $true }
    if (-not $refused -or [IO.File]::ReadAllText((Join-Path $build 'sentinel')) -cne 'build survives' -or
        [IO.File]::ReadAllText((Join-Path $release 'sentinel')) -cne 'release survives') {
        throw 'Missing icon dependency did not preserve existing outputs.'
    }
    Move-Item -LiteralPath $savedIconSource -Destination $iconSource
    if ($ExpectedHost -ceq 'Core7') {
        # Exercise the real resolver over the host's actual dependency graph.
        $buildScript = Join-Path $temp 'scripts\Build-ProGo.ps1'
        $tokens = $null; $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($buildScript, [ref]$tokens, [ref]$parseErrors)
        if ($parseErrors.Count -ne 0) { throw 'SETUP ERROR: build script does not parse.' }
        $resolver = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-ProGoDrawingReferences' }, $false)
        if ($null -eq $resolver) { throw 'SETUP ERROR: drawing resolver not found.' }
        . ([scriptblock]::Create($resolver.Extent.Text))
        Add-Type -AssemblyName System.Drawing.Common -ErrorAction Stop
        $runtime = Join-Path $PSHOME 'ref\System.Runtime.dll'
        if (-not (Test-Path -LiteralPath $runtime -PathType Leaf)) { $runtime = Join-Path $PSHOME 'System.Runtime.dll' }
        $references = @(Get-ProGoDrawingReferences -Roots @([System.Drawing.Bitmap].Assembly, [System.Drawing.Color].Assembly) -RuntimeReference $runtime)
        $names = @{}; $actual = @{}
        foreach ($path in $references) {
            $name = [Reflection.AssemblyName]::GetAssemblyName($path)
            if ($names.ContainsKey($name.Name)) { throw 'Drawing reference was duplicated.' }
            $names[$name.Name] = $name.FullName
            if ($name.Name.StartsWith('System.Private.Windows.', [StringComparison]::Ordinal)) { $actual[$name.Name] = $name.FullName }
        }
        foreach ($path in $references) {
            $name = [Reflection.AssemblyName]::GetAssemblyName($path)
            if ($name.Name -ceq 'System.Runtime') { continue }
            $assembly = [Reflection.Assembly]::LoadFrom($path)
            foreach ($dependency in $assembly.GetReferencedAssemblies()) {
                if ($dependency.Name.StartsWith('System.Private.Windows.', [StringComparison]::Ordinal) -and
                    (-not $actual.ContainsKey($dependency.Name) -or $actual[$dependency.Name] -cne $dependency.FullName)) {
                    throw 'Drawing dependency closure is incomplete.'
                }
            }
        }
        Write-Host ('PASS drawing dependency closure: host=' + $PSVersionTable.PSEdition + '/' + $PSVersionTable.PSVersion + '; private-count=' + $actual.Count)
        # Change only the copied fixture script to inject a missing private assembly request.
        $originalBuild = [IO.File]::ReadAllBytes($buildScript)
        try {
            $text = [IO.File]::ReadAllText($buildScript)
            $needle = 'foreach ($reference in $current.GetReferencedAssemblies()) {'
            $injected = 'foreach ($reference in @($current.GetReferencedAssemblies()) + @([Reflection.AssemblyName]::new("System.Private.Windows.ProGoMissing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"))) {'
            if (-not $text.Contains($needle)) { throw 'SETUP ERROR: missing dependency injection anchor.' }
            [IO.File]::WriteAllText($buildScript, $text.Replace($needle, $injected))
            $missingError = $null
            try { & $buildScript } catch { $missingError = $_ }
            if ($null -eq $missingError -or ($missingError | Out-String) -cnotmatch 'Missing drawing dependency: System.Private.Windows.ProGoMissing') {
                throw 'SETUP ERROR: missing dependency injection did not reach resolver refusal.'
            }
            if ([IO.File]::ReadAllText((Join-Path $build 'sentinel')) -cne 'build survives' -or
                [IO.File]::ReadAllText((Join-Path $release 'sentinel')) -cne 'release survives') { throw 'Missing dependency did not preserve outputs.' }
        } finally { [IO.File]::WriteAllBytes($buildScript, $originalBuild) }
    }
    # Existing source, valid preflight inputs and a compiler-specific diagnostic prove
    # that the failure reached Add-Type rather than an earlier setup check.
    $originalSource = [IO.File]::ReadAllBytes($iconSource)
    try {
        [IO.File]::AppendAllText($iconSource, "`r`n#error PROGO_ICON_COMPILE_SENTINEL`r`n")
        $compileError = $null
        try { & (Join-Path $temp 'scripts\Build-ProGo.ps1') } catch { $compileError = $_ }
        if ($null -eq $compileError -or ($compileError | Out-String) -cnotmatch 'PROGO_ICON_COMPILE_SENTINEL') {
            throw 'SETUP ERROR: expected injected Add-Type compiler diagnostic was not observed.'
        }
        if ([IO.File]::ReadAllText((Join-Path $build 'sentinel')) -cne 'build survives' -or
            [IO.File]::ReadAllText((Join-Path $release 'sentinel')) -cne 'release survives') {
            throw 'Compile failure did not preserve existing outputs.'
        }
    } finally { [IO.File]::WriteAllBytes($iconSource, $originalSource) }
    # This fixture intentionally has no Git checkout; reject accidental parent repository discovery.
    $probe = $temp
    while (-not [string]::IsNullOrEmpty($probe)) {
        if (Test-Path -LiteralPath (Join-Path $probe '.git')) { throw 'Fixture unexpectedly resolved a Git checkout.' }
        $probe = [IO.Path]::GetDirectoryName($probe)
    }
    & (Join-Path $temp 'scripts\Build-ProGo.ps1') -Configuration Release
    $fixtureExe = Join-Path $release 'ProGo.exe'
    $fixtureAssembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($fixtureExe))
    $fixtureIdentity = @($fixtureAssembly.GetCustomAttributes([Reflection.AssemblyInformationalVersionAttribute], $false))[0].InformationalVersion
    $fixtureVersion = (Get-Content -LiteralPath (Join-Path $temp 'VERSION') -Raw).Trim()
    if ($fixtureIdentity -cne ($fixtureVersion + '+local')) { throw 'Fixture local build identity mismatch.' }
    $iconBytes = [IO.File]::ReadAllBytes((Join-Path $release 'ProGo.ico'))
    $iconStream = New-Object IO.MemoryStream -ArgumentList (,$iconBytes)
    $reader = New-Object IO.BinaryReader($iconStream)
    try {
        if ($reader.ReadUInt16() -ne 0 -or $reader.ReadUInt16() -ne 1 -or $reader.ReadUInt16() -ne 7) { throw 'Invalid ICO header.' }
        $frames = New-Object 'System.Collections.Generic.List[object]'
        $minimumOffset = 6 + 16 * 7; $previousEnd = $minimumOffset
        foreach ($size in @(16,24,32,48,64,128,256)) {
            $width = $reader.ReadByte(); $height = $reader.ReadByte()
            $expected = if ($size -eq 256) { 0 } else { $size }
            if ($width -ne $expected -or $height -ne $expected) { throw 'ICO frame dimensions mismatch.' }
            $colors = $reader.ReadByte(); $reserved = $reader.ReadByte()
            $planes = $reader.ReadUInt16(); $depth = $reader.ReadUInt16()
            $length = $reader.ReadUInt32(); $offset = $reader.ReadUInt32()
            if ($colors -ne 0 -or $reserved -ne 0 -or $planes -ne 1 -or $depth -ne 32 -or
                $length -eq 0 -or $offset -ne $previousEnd -or ([long]$offset + [long]$length) -gt $iconBytes.Length) {
                throw 'ICO frame bounds or metadata invalid.'
            }
            $previousEnd = [long]$offset + [long]$length
            $frames.Add([pscustomobject]@{ Size=$size; Offset=$offset; Length=$length })
        }
        if ($previousEnd -ne $iconBytes.Length) { throw 'ICO trailing or missing bytes.' }
        foreach ($frame in $frames) {
            $payload = New-Object byte[] ([int]$frame.Length)
            [Array]::Copy($iconBytes, [long]$frame.Offset, $payload, 0, [long]$frame.Length)
            $png = New-Object IO.MemoryStream -ArgumentList (,$payload)
            try {
                $image = [System.Drawing.Image]::FromStream($png, $true, $true)
                try { if ($image.Width -ne $frame.Size -or $image.Height -ne $frame.Size -or $image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) { throw 'ICO PNG decoding mismatch.' } }
                finally { $image.Dispose() }
            } finally { $png.Dispose() }
        }
    } finally { $reader.Dispose(); $iconStream.Dispose() }
    if (-not (Test-Path -LiteralPath (Join-Path $release 'ProGo.exe') -PathType Leaf)) { throw 'Executable missing.' }
    Write-Host ('PASS Build icon compatibility: ' + $PSVersionTable.PSEdition + ' ' + $PSVersionTable.PSVersion)
} finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
