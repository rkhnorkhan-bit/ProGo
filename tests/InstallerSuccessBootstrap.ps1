# Compile prerequisite fixture types without executing or duplicating any prior suite body.
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$Root=Split-Path -Parent $PSScriptRoot
$source=Get-Content -LiteralPath (Join-Path $Root 'tests\InstallerLegacyOwnedRestoreTests.ps1') -Raw
$matches=[regex]::Matches($source,"(?s)Add-Type -TypeDefinition @'\r?\n(.*?)\r?\n'@")
foreach($name in @('LegacyMetadataThreatFixture','LegacyRestoreReflectionFixture')){
    $definitions=@($matches | Where-Object { $_.Groups[1].Value.Contains('public static class '+$name) })
    if($definitions.Count -ne 1 -or ($name -as [type])){throw 'Success bootstrap prerequisite type identity is invalid.'}
    Add-Type -TypeDefinition $definitions[0].Groups[1].Value
    if(-not ($name -as [type])){throw 'Success bootstrap prerequisite type compilation failed.'}
}
& (Join-Path $Root 'tests\InstallerLiveInstalledOutcomeTests.ps1')
