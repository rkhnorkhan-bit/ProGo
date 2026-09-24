Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Build = Join-Path $PSScriptRoot "Build-ProGo.ps1"

function Fail($Message) {
    throw "TEST FAIL: $Message"
}

function Get-RepoFiles {
    Get-ChildItem -Path $Root -Recurse -File |
        Where-Object { $_.FullName -notmatch "\\.git\\" -and $_.FullName -notmatch "\\release\\" -and $_.FullName -notmatch "\\build\\" }
}

Write-Host "ProGo tests started."
Write-Host "PowerShell: $($PSVersionTable.PSVersion)"

$ForbiddenNames = @("vault*.json", "vault*.enc*", "*.pem", "*.key", "*.pfx", "*.p12", ".env", ".env.*", "*.log")
foreach ($pattern in $ForbiddenNames) {
    $items = @(Get-ChildItem -Path $Root -Recurse -Force -File -Include $pattern -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\.git\\" -and $_.FullName -notmatch "\\release\\" -and $_.FullName -notmatch "\\build\\" })
    if ($items.Count -gt 0) { Fail "forbidden runtime/secret-like file found: $pattern" }
}

$TextFiles = @(Get-RepoFiles | Where-Object { $_.Extension -in @(".cs", ".ps1", ".md", ".yml", ".yaml", ".json") })
$LegacyParts = @(
    ("DM KZ Vault" + "Desk"),
    ("Dungeon" + "Masters"),
    ("Dungeon " + "Masters"),
    ("KzVault" + "Desk"),
    ("DM" + "KZ")
)
foreach ($file in $TextFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    foreach ($legacy in $LegacyParts) {
        if ($content -match [regex]::Escape($legacy)) { Fail "legacy branding found in $($file.FullName)" }
    }
}

$VersionFile = Join-Path $Root "VERSION"
if (-not (Test-Path $VersionFile)) { Fail "VERSION file missing" }
$VersionText = (Get-Content -Raw -Path $VersionFile).Trim()
if ($VersionText -notmatch "^\d+\.\d+\.\d+$") { Fail "VERSION is not semver-like: $VersionText" }

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

foreach ($requiredSource in @("Обновить ProGo", "Создать резервную копию", "Откатить из резервной копии", "BackupService.EnsureVersionBackupExists", "BackupPickerForm", "Restore-ProGoBackup.ps1")) {
    if ($Source -notmatch [regex]::Escape($requiredSource)) {
        Fail "source marker missing: $requiredSource"
    }
}

if ($Source -notmatch "BrandIcon\.Create") {
    Fail "brand icon factory is not used by tray context"
}

if ($Source -notmatch "raw\.githubusercontent\.com/rkhnorkhan-bit/ProGo/main/scripts/Update-ProGo\.ps1") {
    Fail "updater bootstrap fallback URL missing"
}

if ($Source -notmatch "TryDownloadUpdateScript") {
    Fail "updater download fallback missing"
}

if ($Source -notmatch "Always try to refresh the updater first") {
    Fail "updater script is not refreshed before local fallback"
}

if ($Source -notmatch "WindowStyle\s*=\s*ProcessWindowStyle\.Minimized") {
    Fail "updater launcher is not minimized"
}

$buildScriptText = Get-Content -Raw -Path $Build
if ($buildScriptText -notmatch "/win32icon") {
    Fail "build script does not embed executable icon"
}
if ($buildScriptText -notmatch "VERSION") {
    Fail "build script does not include VERSION"
}
if ($buildScriptText -notmatch "Restore-ProGoBackup.ps1") {
    Fail "build script does not include restore script"
}

$installScriptText = Get-Content -Raw -Path (Join-Path $PSScriptRoot "Install-ProGo.ps1")
if (-not $installScriptText.Contains('Copy-Item $VersionFile')) {
    Fail "installer does not copy VERSION marker"
}
if ($installScriptText -notmatch "Restore-ProGoBackup.ps1") {
    Fail "installer does not deploy restore script"
}

$updateScriptText = Get-Content -Raw -Path (Join-Path $PSScriptRoot "Update-ProGo.ps1")
foreach ($required in @("Test-UpdateRequired", "Get-RemoteVersion", "U8", "Backup-InstalledState", "ProGo.exe", "scripts", "vault.enc.json", "settings.json", "manifest.txt", "backups", "Start-UpdatedProGo", "-PassThru", "Updated ProGo started")) {
    if ($updateScriptText -notmatch [regex]::Escape($required)) {
        Fail "updater safety marker missing: $required"
    }
}
if ($updateScriptText -match "Stop-Process\s+-Id") {
    Fail "updater still force-kills ProGo process"
}
if ($updateScriptText.Contains("У вас актуальная версия ProGo")) {
    Fail "updater has raw Cyrillic text that breaks Windows PowerShell 5.1 encoding"
}

$restoreScriptText = Get-Content -Raw -Path (Join-Path $PSScriptRoot "Restore-ProGoBackup.ps1")
foreach ($required in @("BackupDir", "manifest.txt", "ProGo.exe", "vault.enc.json", "settings.json", "Copy-DirectoryIfExists", "Start-ProGo", "progo-restore.log", "U8")) {
    if ($restoreScriptText -notmatch [regex]::Escape($required)) {
        Fail "restore script marker missing: $required"
    }
}
if ($restoreScriptText.Contains("ProGo восстановлен")) {
    Fail "restore script has raw Cyrillic text that breaks Windows PowerShell 5.1 encoding"
}

$parseErrors = $null
foreach ($scriptName in @("Build-ProGo.ps1", "Install-ProGo.ps1", "Update-ProGo.ps1", "Restore-ProGoBackup.ps1", "Install-FromGitHub.ps1", "Uninstall-ProGo.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (-not (Test-Path $scriptPath)) { Fail "script missing: $scriptName" }
    $scriptText = Get-Content -Raw -Path $scriptPath
    $parseErrors = $null
    $tokens = @([System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors))
    if ($parseErrors -ne $null -and @($parseErrors).Count -gt 0) {
        Fail "PowerShell parser errors in $scriptName"
    }
    if ($tokens.Count -eq 0) { Fail "PowerShell parser returned no tokens for $scriptName" }
}

& $Build
$Exe = Join-Path $Root "release\ProGo.exe"
if (-not (Test-Path $Exe)) { Fail "release ProGo.exe missing" }
$ReleaseIcon = Join-Path $Root "release\ProGo.ico"
if (-not (Test-Path $ReleaseIcon)) { Fail "release ProGo.ico missing" }
if ((Get-Item $ReleaseIcon).Length -le 0) { Fail "release ProGo.ico is empty" }
$ReleaseVersion = Join-Path $Root "release\VERSION"
if (-not (Test-Path $ReleaseVersion)) { Fail "release VERSION missing" }
foreach ($scriptName in @("Update-ProGo.ps1", "Restore-ProGoBackup.ps1", "Install-FromGitHub.ps1")) {
    $releaseScript = Join-Path $Root ("release\scripts\" + $scriptName)
    if (-not (Test-Path $releaseScript)) { Fail "release script missing: $scriptName" }
}

Write-Host "ProGo tests PASS."
