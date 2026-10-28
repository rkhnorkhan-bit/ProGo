param([string]$Scripts, [string]$Fixture, [string]$Payload, [string]$Mode, [string]$StorageRoot)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Requires isolated Windows CI' }
if (-not [string]::IsNullOrWhiteSpace($StorageRoot)) {
    $StorageRoot = [IO.Path]::GetFullPath($StorageRoot).TrimEnd([char]'\')
    $leaf = [IO.Path]::GetFileName($StorageRoot)
    if ([IO.Path]::GetDirectoryName($StorageRoot) -ine [IO.Path]::GetPathRoot($StorageRoot) -or
        $leaf -cnotmatch '^ProGo-storage-fixture-[a-f0-9]{32}$' -or
        ([IO.File]::GetAttributes($StorageRoot) -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or
        [IO.File]::ReadAllText((Join-Path $StorageRoot 'owner.txt')) -cne $leaf) { throw 'Storage fixture root is not owned.' }
}
$fixture = [IO.Path]::GetFullPath($Fixture)
$temp = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([char]'\')
if ([IO.Path]::GetDirectoryName($fixture) -ine $temp -or [IO.Path]::GetFileName($fixture) -cnotmatch '^ProGo-split-repair-[a-f0-9]{32}$') { throw 'Fixture path outside owned temporary root.' }
$dataBase = Join-Path $fixture 'data'; $programBase = if ($StorageRoot) { Join-Path $StorageRoot 'repair' } else { Join-Path $fixture 'programs' }
$runtime = Join-Path $programBase 'ProGo'
if ($Mode -eq 'foreign') { $runtime = Join-Path $fixture 'foreign' }
$NoLaunch = $true; $Recover = $false; $PayloadDirectory = $Payload
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Repair-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Repair source does not parse.' }
foreach ($statement in $ast.EndBlock.Statements) {
    $source = $statement.Extent.Text.Replace('$PSScriptRoot', '$Scripts')
    if ($source -ceq '$layout = Resolve-ProGoRecoveryLayout') {
        $source = '$layout = Resolve-ProGoRecoveryLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot $runtime -SeparateIdentityConfirmed $true'
    }
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst] -and $statement.Name -eq 'Set-RepairFile') {
        . ([scriptblock]::Create($source))
        $realRepairFile = ${function:Set-RepairFile}
        function Set-RepairFile([string]$Source, [string]$Destination, [string]$Hash) {
            & $realRepairFile $Source $Destination $Hash
            if ($Mode -eq 'fault' -and -not [IO.File]::Exists((Join-Path $fixture 'fault-injected')) -and $Destination -ceq (Join-Path $runtime 'ProGo.exe')) {
                [IO.File]::WriteAllText((Join-Path $fixture 'fault-injected'), 'after-exe-write')
                throw 'Synthetic post-write fault'
            }
        }
    } else { . ([scriptblock]::Create($source)) }
}
