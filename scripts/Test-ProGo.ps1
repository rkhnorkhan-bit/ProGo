Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Build = Join-Path $PSScriptRoot "Build-ProGo.ps1"

function Fail($Message) {
    throw "TEST FAIL: $Message"
}

function Get-RepoFiles {
    Get-ChildItem -Path $Root -Recurse -File |
        Where-Object { $_.FullName -notmatch "\\.git\\" -and $_.FullName -notmatch "\\release\\" }
}

Write-Host "ProGo tests started."
Write-Host "PowerShell: $($PSVersionTable.PSVersion)"

$ForbiddenNames = @("vault*.json", "vault*.enc*", "*.pem", "*.key", "*.pfx", "*.p12", ".env", ".env.*", "*.log")
foreach ($pattern in $ForbiddenNames) {
    $items = Get-ChildItem -Path $Root -Recurse -Force -File -Include $pattern -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\.git\\" -and $_.FullName -notmatch "\\release\\" }
    if ($items.Count -gt 0) { Fail "forbidden runtime/secret-like file found: $pattern" }
}

$TextFiles = Get-RepoFiles | Where-Object { $_.Extension -in @(".cs", ".ps1", ".md", ".yml", ".yaml", ".json") }
$Legacy = "DM KZ VaultDesk|DungeonMasters|Dungeon Masters|KzVaultDesk|DMKZ"
foreach ($file in $TextFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -match $Legacy) { Fail "legacy branding found in $($file.FullName)" }
}

$SourceBuilder = New-Object System.Text.StringBuilder
Get-ChildItem -Path (Join-Path $Root "src") -Filter "*.cs" -File | Sort-Object FullName | ForEach-Object {
    [void]$SourceBuilder.AppendLine((Get-Content -Path $_.FullName -Raw))
}
$Source = $SourceBuilder.ToString()

if ($Source -match "using\s+System\.Threading;[\s\S]*\bTimer\b" -and $Source -notmatch "System\.Windows\.Forms\.Timer") {
    Fail "possible CS0104 Timer ambiguity"
}

if ($Source -match "DECOY|fake vault|wrong PIN|incorrect PIN") {
    Fail "decoy-disclosing marker found in source"
}

$parseErrors = $null
$scriptText = Get-Content -Raw -Path (Join-Path $PSScriptRoot "Build-ProGo.ps1")
$tokens = [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors)
if ($parseErrors -ne $null -and $parseErrors.Count -gt 0) {
    Fail "PowerShell parser errors in Build-ProGo.ps1"
}
if ($tokens.Count -eq 0) { Fail "PowerShell parser returned no tokens for Build-ProGo.ps1" }

& $Build
$Exe = Join-Path $Root "release\ProGo.exe"
if (-not (Test-Path $Exe)) { Fail "release ProGo.exe missing" }

Write-Host "ProGo tests PASS."
