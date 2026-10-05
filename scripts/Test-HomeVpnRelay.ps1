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
    $WizardHarness = Join-Path $Root 'build\HomeVpnWizardTests.exe'
    $Fixture = Join-Path $Root 'build\vpn-fixture'
    python .\tests\make_vpn_test_token.py $Fixture
    if ($LASTEXITCODE -ne 0) { throw 'VPN fixture generation failed' }
    & $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$WizardHarness" .\src\Core.cs .\src\ClipboardService.cs .\src\ProxyService.cs .\src\ConnectionHealth.cs .\src\DiagnosticProcess.cs .\src\Ikev2RelayService.cs .\src\HomeVpnAccess.cs .\src\HomeVpnService.cs .\src\HomeVpnWizardForm.cs .\src\HomeProfileShare.cs .\src\UiTheme.cs .\src\BrandIcon.cs .\tests\HomeVpnWizardTests.cs
    if ($LASTEXITCODE -ne 0) { throw 'Home VPN wizard harness build failed' }
    & $WizardHarness (Join-Path $Fixture 'token')
    if ($LASTEXITCODE -ne 0) { throw 'Home VPN wizard tests failed' }
    $env:PROGO_RELAY_TEST_COMMAND = ConvertTo-Json -InputObject @($Harness) -Compress
    python -m unittest discover -s tests -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) { throw 'Relay integration tests failed' }
}
finally {
    Remove-Item Env:\PROGO_RELAY_TEST_COMMAND -ErrorAction SilentlyContinue
    Pop-Location
}
