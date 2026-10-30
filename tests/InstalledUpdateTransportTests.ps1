param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$UpdateWindow = $null
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Requires isolated Windows CI' }
# Load only the transport adapter definitions, not transaction top-level actions.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Updater does not parse' }
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst] -and
        $statement.Name -in @('Initialize-UpdateTransport','Read-UpdateMetadata','Save-UpdatePackage')) {
        . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')))
    }
}
Initialize-UpdateTransport
if (-not ('ProGo.InstalledUpdateTransport' -as [type])) { throw 'Installed local transport was not loaded' }
Initialize-UpdateTransport
Write-Host 'PASS: installed transport compiles and loads idempotently in Windows PowerShell'
$refused = $false
try { Read-UpdateMetadata 'http://127.0.0.1/fixture' } catch { $refused = $_.Exception.ToString().Contains('HTTPS') }
if (-not $refused) { throw 'Metadata adapter did not call the guarded local transport' }
Write-Host 'PASS: metadata adapter reaches production URL guard without network'
$target = Join-Path $env:TEMP ('ProGo-transport-adapter-' + [guid]::NewGuid().ToString('N') + '.zip')
$refused = $false
try { Save-UpdatePackage 'http://127.0.0.1/fixture' $target } catch { $refused = $_.Exception.ToString().Contains('HTTPS') }
if (-not $refused -or (Test-Path $target)) { throw 'Package adapter bypassed transport URL guard' }
Write-Host 'PASS: package adapter refuses HTTP before creating a file'
Write-Host 'Installed transport adapter tests PASS: 3'
