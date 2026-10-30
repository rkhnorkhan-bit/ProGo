Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $root 'scripts\Layout-ProGo.ps1')
# Only generated paths under this fresh temporary directory reach the core.
# No production resolver, KnownFolder lookup, environment override, launcher or real profile.
$work = Join-Path ([IO.Path]::GetTempPath()) ('progo-layout-fixture-' + [guid]::NewGuid().ToString('N'))
function Check-Layout($condition, $message) { if (-not $condition) { throw $message } }
function Reject-Layout([scriptblock]$action, [string]$expected) {
    $actual = $null; try { & $action | Out-Null } catch { $actual = $_.Exception.Message }
    if ($actual -cne $expected) { throw "Expected refusal '$expected'; got '$actual'" }
}
try {
    $dataBase = Join-Path $work 'data'; $programBase = Join-Path $work 'programs'
    $data = Join-Path $dataBase 'ProGo'; $program = Join-Path $programBase 'ProGo'
    [void][IO.Directory]::CreateDirectory($data); [void][IO.Directory]::CreateDirectory($program)
    $exe = Join-Path $data 'ProGo.exe'
    Add-Type -TypeDefinition 'public static class FixtureProgram { public static void Main() { } }' -OutputAssembly $exe -OutputType ConsoleApplication
    Check-Layout (([Reflection.AssemblyName]::GetAssemblyName($exe)).Name -ceq 'ProGo') 'fixture assembly identity is not ProGo'
    [IO.File]::Copy($exe, (Join-Path $program 'ProGo.exe'))
    $personal = Join-Path $data 'settings.json'; [IO.File]::WriteAllText($personal, 'private-sentinel')
    $legacy = Resolve-ProGoLayoutCore $dataBase $programBase $data
    Check-Layout ($legacy.Kind -ceq 'Legacy' -and $legacy.DataRoot -ceq $legacy.RuntimeRoot) 'legacy layout failed'
    $split = Resolve-ProGoLayoutCore $dataBase $programBase $program $true
    Check-Layout ($split.Kind -ceq 'Separate' -and $split.DataRoot -ceq $data -and $split.RuntimeRoot -ceq $program) 'separate layout failed'
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program } 'Separate runtime identity is not confirmed.'
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase (Join-Path $work 'foreign') $true } 'Foreign runtime root.'
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $dataBase $data $true } 'Layout roots overlap.'
    [IO.File]::WriteAllText((Join-Path $program 'ProGo.exe'), 'not a managed executable')
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true } 'Runtime executable is not a valid managed payload.'
    [IO.File]::Delete((Join-Path $program 'ProGo.exe'))
    $foreignExe = Join-Path $work 'Foreign.exe'
    Add-Type -TypeDefinition 'public static class ForeignFixtureProgram { public static void Main() { } }' -OutputAssembly $foreignExe -OutputType ConsoleApplication
    Check-Layout (([Reflection.AssemblyName]::GetAssemblyName($foreignExe)).Name -ceq 'Foreign') 'fixture foreign assembly identity mismatch'
    [IO.File]::Copy($foreignExe, (Join-Path $program 'ProGo.exe'))
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true } 'Foreign runtime executable.'
    [IO.File]::Delete((Join-Path $program 'ProGo.exe'))
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true } 'Layout path is missing or inaccessible.'
    $missing = Resolve-ProGoLayoutCore $dataBase $programBase $program $true -AllowMissingExecutable
    Check-Layout ($missing.Kind -ceq 'Separate' -and -not $missing.ExecutablePresent -and -not [IO.File]::Exists($missing.Executable)) 'missing executable recovery preflight failed'
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program -AllowMissingExecutable } 'Separate runtime identity is not confirmed.'
    [IO.File]::WriteAllText((Join-Path $program 'ProGo.exe'), 'bad payload')
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true -AllowMissingExecutable } 'Runtime executable is not a valid managed payload.'
    [IO.File]::Delete((Join-Path $program 'ProGo.exe'))
    [void][IO.Directory]::CreateDirectory((Join-Path $program 'ProGo.exe'))
    Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true -AllowMissingExecutable } 'Runtime executable is not a regular file.'
    [IO.Directory]::Delete((Join-Path $program 'ProGo.exe'))
    & $env:ComSpec /d /c mklink /J "$(Join-Path $program 'ProGo.exe')" "$data" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not create fixture executable junction' }
    try { Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true -AllowMissingExecutable } 'Layout refuses a reparse point.' }
    finally { [IO.Directory]::Delete((Join-Path $program 'ProGo.exe')) }
    # A directory junction requires no symbolic-link privilege on Windows.
    [IO.Directory]::Delete($program)
    & $env:ComSpec /d /c mklink /J "$program" "$data" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not create fixture junction' }
    try { Reject-Layout { Resolve-ProGoLayoutCore $dataBase $programBase $program $true } 'Layout refuses a reparse point.' }
    finally { [IO.Directory]::Delete($program) }
    Check-Layout ([IO.File]::ReadAllText($personal) -ceq 'private-sentinel') 'personal data changed'
    Write-Host 'PASS: isolated layout legacy, separate, refusal and preservation'
} finally {
    if ([IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
}
