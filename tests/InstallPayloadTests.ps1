Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer does not parse.' }
$resolver = @($ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-ProGoInstallPayload'
}, $false))
if ($resolver.Count -ne 1) { throw 'Installer resolver is ambiguous.' }
. ([scriptblock]::Create($resolver[0].Extent.Text))
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-install-payload-' + [Guid]::NewGuid().ToString('N'))
function Check-Payload($Condition, $Name) {
    if (-not $Condition) { throw ('Installer payload test failed: ' + $Name) }
    Write-Host ('PASS: installer payload ' + $Name)
}
function Reject-Payload($Action, $Expected) {
    try { & $Action | Out-Null } catch { return $_.Exception.Message.Contains($Expected) }
    return $false
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $fixture = Join-Path $work 'payload'
    Copy-Item -LiteralPath (Join-Path $Root 'release') -Destination $fixture -Recurse
    $plan = Get-ProGoInstallPayload -Directory $fixture
    Check-Payload ($plan.Version -eq (Get-Content -LiteralPath (Join-Path $fixture 'VERSION') -Raw).Trim() -and
        $plan.Executable -eq (Join-Path $fixture 'ProGo.exe') -and
        $plan.ScriptsDirectory -eq (Join-Path $fixture 'scripts')) 'complete local payload'

    # Execute the real installer's param/preflight statements only. Stop before its
    # first user-directory assignment; never run install, shortcuts, launch or services.
    $tokens = $null; $errors = $null
    $installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
    $ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
    if ($errors.Count) { throw 'Installer does not parse.' }
    $boundary = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir'
    })
    if ($boundary.Count -ne 1) { throw 'Installer preflight boundary is ambiguous.' }
    $text = Get-Content -LiteralPath $installer -Raw
    $prefix = $text.Substring(0, $boundary[0].Extent.StartOffset)
    $bootstrap = Join-Path $work 'bootstrap'
    New-Item -ItemType Directory -Path $bootstrap | Out-Null
    $driver = Join-Path $bootstrap 'PreflightOnly.ps1'
    Set-Content -LiteralPath $driver -Value ($prefix + "`nWrite-Output `$Payload.Version") -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $bootstrap 'Build-ProGo.ps1') -Value "throw 'Unexpected source build fallback.'" -Encoding UTF8
    $before = @(Get-ChildItem -LiteralPath $fixture -Recurse -File | Sort-Object FullName | Get-FileHash -Algorithm SHA256)
    $version = & $driver -PayloadDirectory $fixture
    Check-Payload ($version -eq $plan.Version) 'production preflight skips build'

    $missing = Join-Path $fixture 'scripts\Start-ProGo.ps1'
    $saved = [IO.File]::ReadAllBytes($missing)
    Remove-Item -LiteralPath $missing
    Check-Payload (Reject-Payload { & $driver -PayloadDirectory $fixture } 'Incomplete local installer payload: scripts/Start-ProGo.ps1') 'incomplete explicit payload has no build fallback'
    [IO.File]::WriteAllBytes($missing, $saved)

    $versionPath = Join-Path $fixture 'VERSION'
    $saved = [IO.File]::ReadAllBytes($versionPath)
    Set-Content -LiteralPath $versionPath -Value 'not-a-version'
    Check-Payload (Reject-Payload { Get-ProGoInstallPayload -Directory $fixture } 'Invalid installer payload version.') 'invalid version'
    Set-Content -LiteralPath $versionPath -Value '999.998.997'
    Check-Payload (Reject-Payload { Get-ProGoInstallPayload -Directory $fixture } 'Installer payload executable/version mismatch.') 'executable version mismatch'
    [IO.File]::WriteAllBytes($versionPath, $saved)

    $exePath = Join-Path $fixture 'ProGo.exe'
    $saved = [IO.File]::ReadAllBytes($exePath)
    Set-Content -LiteralPath $exePath -Value 'synthetic invalid executable; never launched'
    $rejected = $false
    try { Get-ProGoInstallPayload -Directory $fixture | Out-Null } catch { $rejected = $true }
    Check-Payload $rejected 'invalid executable metadata'
    [IO.File]::WriteAllBytes($exePath, $saved)

    $server = Join-Path $fixture 'scripts\home-vpn\server\profile_share.py'
    $saved = [IO.File]::ReadAllBytes($server)
    Remove-Item -LiteralPath $server
    Check-Payload (Reject-Payload { Get-ProGoInstallPayload -Directory $fixture } 'Incomplete local installer payload: scripts/home-vpn/server/profile_share.py') 'missing bundled server resource'
    [IO.File]::WriteAllBytes($server, $saved)
    Check-Payload (Reject-Payload { & $driver -PayloadDirectory (Join-Path $work 'absent') } 'Incomplete local installer payload: ProGo.exe') 'absent explicit payload has no build fallback'
    $after = @(Get-ChildItem -LiteralPath $fixture -Recurse -File | Sort-Object FullName | Get-FileHash -Algorithm SHA256)
    Check-Payload (($before.Count -eq $after.Count) -and
        -not (Compare-Object @($before | ForEach-Object { $_.Path + ':' + $_.Hash }) @($after | ForEach-Object { $_.Path + ':' + $_.Hash }))) 'read-only preflight preserves all payload bytes'
    Write-Host 'Installer payload tests PASS: 9'
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
