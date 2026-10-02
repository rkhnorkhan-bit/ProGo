Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$Harness = Join-Path $Root 'build\RelayHarness.exe'
New-Item -ItemType Directory -Path (Split-Path -Parent $Harness) -Force | Out-Null

Push-Location $Root
try {
    & $Csc /nologo /target:exe /reference:System.dll /reference:System.Core.dll "/out:$Harness" .\src\Ikev2RelayService.cs .\tests\RelayHarness.cs
    if ($LASTEXITCODE -ne 0) { throw 'Relay harness build failed' }
    $env:PROGO_RELAY_TEST_COMMAND = ConvertTo-Json -InputObject @($Harness) -Compress
    python -m unittest discover -s tests -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) { throw 'Relay integration tests failed' }
}
finally {
    Remove-Item Env:\PROGO_RELAY_TEST_COMMAND -ErrorAction SilentlyContinue
    Pop-Location
}
