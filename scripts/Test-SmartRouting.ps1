# CI-only reproducible sing-box configuration validation.
# Official upstream release is downloaded with a pinned GitHub asset SHA-256;
# no network routing, TUN adapter or installed app settings are changed.
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "1.14.4"
$zipName = "sing-box-$version-windows-amd64.zip"
$asset = "https://github.com/SagerNet/sing-box/releases/download/v$version/$zipName"
$sha256 = "99185d75d314a07dbba4a130c8ad986cbeb629a07927975636b341ca61f991df"
$temp = Join-Path $env:TEMP ("progo-singbox-ci-" + [guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $temp $zipName
$unpack = Join-Path $temp "unpacked"
$exeProGo = Join-Path $root "release\ProGo.exe"
New-Item -ItemType Directory -Path $temp -Force | Out-Null

try {
    Write-Host "Downloading verified sing-box $version from upstream GitHub..."
    Invoke-WebRequest -Uri $asset -OutFile $zipPath -UseBasicParsing -MaximumRedirection 5
    $actual = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $sha256) { throw "Upstream zip SHA-256 mismatch: $actual" }
    Expand-Archive -LiteralPath $zipPath -DestinationPath $unpack
    $items = @(Get-ChildItem $unpack -Filter "sing-box.exe" -File -Recurse)
    if ($items.Count -ne 1) { throw "Expected one executable in verified official archive" }
    $engine = $items[0].FullName
    $binaryHash = (Get-FileHash -Path $engine -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host "SING_BOX_EXE_SHA256=$binaryHash"
    & $engine version
    if ($LASTEXITCODE -ne 0) { throw "Cannot run signed-release sing-box executable" }

    $fixtureProcess = Start-Process -FilePath $exeProGo -ArgumentList "--dump-routing-fixture" -Wait -PassThru
    if ($fixtureProcess.ExitCode -ne 0) { throw "ProGo fixture export failed with exit code $($fixtureProcess.ExitCode)" }
    $fixture = Join-Path $env:TEMP "ProGo-Routing-Fixture\active.json"
    $rules = Join-Path $env:TEMP "ProGo-Routing-Fixture\auto-proxy.json"
    $manualDirect = Join-Path $env:TEMP "ProGo-Routing-Fixture\manual-direct.json"
    $manualProxy = Join-Path $env:TEMP "ProGo-Routing-Fixture\manual-proxy.json"
    if (-not (Test-Path $fixture) -or -not (Test-Path $rules) -or -not (Test-Path $manualDirect) -or -not (Test-Path $manualProxy)) { throw "Missing routing fixture" }
    $json = Get-Content -LiteralPath $fixture -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $json.route.auto_detect_interface) { throw "TUN loop avoidance not enabled" }
    if (@($json.route.rule_set).Count -ne 3) { throw "Expected three hot-reload rule sets" }
    $ruleJson = Get-Content -LiteralPath $rules -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($ruleJson.version -ne 1) { throw "Unexpected rule-set format" }

    Write-Host "Checking actual sing-box 1.14.4 config parser..."
    & $engine check -c $fixture
    if ($LASTEXITCODE -ne 0) { throw "sing-box check rejected ProGo generated routing configuration" }
    Write-Host "PASS: upstream verified and generated config accepted by sing-box."
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $env:TEMP "ProGo-Routing-Fixture") -Recurse -Force -ErrorAction SilentlyContinue
}
