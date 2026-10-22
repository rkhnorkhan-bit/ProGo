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
    $content = Get-Content -Encoding UTF8 -Path $file.FullName -Raw
    foreach ($legacy in $LegacyParts) {
        if ($content -match [regex]::Escape($legacy)) { Fail "legacy branding found in $($file.FullName)" }
    }
}

$VersionFile = Join-Path $Root "VERSION"
if (-not (Test-Path $VersionFile)) { Fail "VERSION file missing" }
$VersionText = (Get-Content -Encoding UTF8 -Raw -Path $VersionFile).Trim()
if ($VersionText -notmatch "^\d+\.\d+\.\d+$") { Fail "VERSION is not semver-like: $VersionText" }

$SourceBuilder = New-Object System.Text.StringBuilder
Get-ChildItem -Path (Join-Path $Root "src") -Filter "*.cs" -File | Sort-Object FullName | ForEach-Object {
    [void]$SourceBuilder.AppendLine((Get-Content -Encoding UTF8 -Path $_.FullName -Raw))
}
$Source = $SourceBuilder.ToString()

if ($Source -match "using\s+System\.Threading;[\s\S]*\bTimer\b" -and $Source -notmatch "System\.Windows\.Forms\.Timer") {
    Fail "possible CS0104 Timer ambiguity"
}

if ($Source -match "DECOY|fake vault|wrong PIN|incorrect PIN") {
    Fail "decoy-disclosing marker found in source"
}

if ($Source -match [regex]::Escape("socks5h://")) {
    Fail "C# source must not publish SOCKS URI through HTTP_PROXY/HTTPS_PROXY/ALL_PROXY; use the local HTTP CONNECT bridge"
}

foreach ($requiredSource in @(
    "BackupService.EnsureVersionBackupExists",
    "BackupPickerForm",
    "Restore-ProGoBackup.ps1",
    "CleanupOldBackups",
    "update_result",
    "backup_kind",
    "target_version",
    "created_by",
    "MaxAutomaticBackups",
    "CheckSelectedProfile",
    "SshProfileDiagnostics.Check",
    "ssh.exe -G",
    "SshResolved",
    "CheckConfiguration",
    "IdentityPaths",
    "IdentityListLimited",
    "ResolvedIdentityFile",
    "ConnectionMetrics",
    "MeasureSocksLatencyMs",
    "MeasureDownloadMbps",
    "speed.cloudflare.com",
    "Задержка соединения",
    "Измерить скорость",
    "Командная строка",
    "Interval = 2000",
    "UpdateAvailability",
    "CheckForUpdate",
    "releases/latest",
    "MessageBoxIcon.Information",
    "MessageBoxButtons.YesNo",
    "check.RemoteVersion",
    "check.LocalVersion",
    "CliProxyEnvironmentService.ApplyUserEnvironment",
    "CliProxyEnvironmentService.IsAppliedToUserEnvironment",
    "ClearUserEnvironmentIfOwned",
    "SocketTimeoutMs = 0"
)) {
    if ($Source -notmatch [regex]::Escape($requiredSource)) {
        Fail "source marker missing: $requiredSource"
    }
}

if ($Source -notmatch "BrandIcon\.Create") { Fail "brand icon factory is not used by tray context" }

$proxySetupText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $Root "docs\PROXY_SETUP.md")
foreach ($required in @(
    "HTTP_PROXY=http://127.0.0.1:1881",
    "HTTPS_PROXY=http://127.0.0.1:1881",
    "ALL_PROXY=http://127.0.0.1:1881",
    "Codex CLI"
)) {
    if ($proxySetupText -notmatch [regex]::Escape($required)) { Fail "proxy setup marker missing: $required" }
}
if ($proxySetupText -match [regex]::Escape("HTTP_PROXY=socks5h://")) { Fail "proxy setup still documents SOCKS URI for HTTP_PROXY" }

$updateLauncherSource = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $Root "src\UpdateLauncher.cs")
if ($updateLauncherSource -match 'TryDownloadUpdateScript|ExecutionPolicy Bypass|RawUpdateScriptUrl') { Fail "updater must use installed files and respect execution policy" }
if ($updateLauncherSource -notmatch 'CreateNoWindow = true') { Fail "updater must use its native install window instead of a second console" }
foreach ($required in @('MaintenanceOperation.StartOwnedHandoff', 'LaunchUpdater')) {
    if ($updateLauncherSource -notmatch [regex]::Escape($required)) { Fail "updater owned handoff guard missing: $required" }
}

$buildScriptText = Get-Content -Encoding UTF8 -Raw -Path $Build
foreach ($required in @("/win32icon", "VERSION", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Repair-ProGo.ps1", "Start-ProGo.ps1", "bootstrap-only", "MaintenanceOperation.cs", "Maintenance-ProGo.ps1")) {
    if ($buildScriptText -notmatch [regex]::Escape($required)) { Fail "build script marker missing: $required" }
}

$installScriptText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Install-ProGo.ps1")
foreach ($required in @('Copy-Item $VersionFile', "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Repair-ProGo.ps1", "Start-ProGo.ps1", "NoStartMenuShortcut", "Initialize-ProGoShortcuts", "bootstrap", "MaintenanceOperation.cs", "Maintenance-ProGo.ps1")) {
    if (-not $installScriptText.Contains($required) -and $installScriptText -notmatch [regex]::Escape($required)) {
        Fail "installer marker missing: $required"
    }
}

$updateBootstrapText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Update-ProGo.ps1")
if ($updateBootstrapText -match 'ScriptBlock|FromBase64String|Invoke-WebRequest|Download') {
    # The static error message can mention downloading a release; no network code belongs here.
    if ($updateBootstrapText -match 'ScriptBlock|FromBase64String|Invoke-WebRequest|DownloadString|DownloadFile') { Fail "updater bootstrap executes or fetches remote code" }
}
if (-not $updateBootstrapText.Contains('& $LocalCoreScriptPath @coreArgs')) { Fail "updater must execute installed core as a file" }

$updateScriptText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Update-ProGo.Core.ps1")
foreach ($required in @(
    "Test-UpdateRequired",
    "Get-RemoteVersion",
    "Backup-InstalledState",
    "Set-BackupUpdateResult",
    "ReleasePackageUrl",
    "Try-GetReleasePackage",
    "Get-FileHash",
    "Package SHA-256 verified",
    "Test-ReleaseArchive",
    "Get-ReleaseDirForUpdate",
    "update_mode=release-package",
    "ProGo-release.zip",
    "ProGo.exe",
    "scripts",
    "vault.enc.json",
    "settings.json",
    "manifest.txt",
    "backups",
    "Start-UpdatedProGo",
    "-PassThru",
    "Updated ProGo started"
)) {
    if ($updateScriptText -notmatch [regex]::Escape($required)) {
        Fail "updater safety marker missing: $required"
    }
}
if ($updateScriptText -match "Stop-Process\s+-Id") { Fail "updater still force-kills ProGo process" }
if ($updateScriptText -match 'Build-DownloadedSource|ScriptBlock') { Fail "updater must not execute downloaded source" }
foreach ($requiredStateMarker in @(
    '$State.RemoteVersion',
    '$State.MainWasChanged',
    '$State.UpdateMode',
    'Version check: local=$($State.LocalVersion) remote=$($State.RemoteVersion)',
    'target_version=$($State.RemoteVersion)',
    'update_mode=$($State.UpdateMode)',
    'installed=$installedVersion remote=$($State.RemoteVersion)'
)) {
    if ($updateScriptText -notmatch [regex]::Escape($requiredStateMarker)) {
        Fail "updater shared-state marker missing: $requiredStateMarker"
    }
}
foreach ($badInterpolation in @(
    'local=$State.LocalVersion',
    'remote=$State.RemoteVersion',
    'target_version=$State.RemoteVersion',
    'update_mode=$State.UpdateMode'
)) {
    if ($updateScriptText -match [regex]::Escape($badInterpolation)) {
        Fail "updater state property is interpolated incorrectly: $badInterpolation"
    }
}
if ($updateScriptText -notmatch [regex]::Escape("api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest")) { Fail "updater latest release API URL missing" }
if ($updateScriptText -notmatch "ConvertFrom-Json") { Fail "updater latest release API decoding missing" }
if ($updateScriptText -notmatch "tag_name") { Fail "updater latest release tag parsing missing" }
if ($updateScriptText -match [regex]::Escape("raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/VERSION")) { Fail "updater core still depends on raw GitHub VERSION URL" }
if ($updateScriptText -match [regex]::Escape("api.github.com/repos/rkhnorkhan-bit/ProGo/contents/VERSION?ref=main")) { Fail "updater core still uses main VERSION instead of published release" }

$restoreScriptText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Restore-ProGoBackup.ps1")
foreach ($required in @("BackupDir", "manifest.txt", "ProGo.exe", "Scope", "ConfirmData", "RestoreNames", "Prepare", "Backup-BeforeRestore", "Commit-Restore", "Undo-Restore", "Test-RestoredApplication", "Start-ProGo", "progo-restore.log", "U8")) {
    if ($restoreScriptText -notmatch [regex]::Escape($required)) {
        Fail "restore script marker missing: $required"
    }
}

$repairScriptText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Repair-ProGo.ps1")
foreach ($required in @("ProGo.exe", "VERSION", "backups", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Repair-ProGo.ps1", "Start-ProGo.ps1", "NoLaunch", "MaintenanceOperation.cs", "Maintenance-ProGo.ps1")) {
    if ($repairScriptText -notmatch [regex]::Escape($required)) { Fail "repair script marker missing: $required" }
}

$startScriptText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $PSScriptRoot "Start-ProGo.ps1")
foreach ($required in @("ProGo.exe", "--show", "Start-Process")) {
    if ($startScriptText -notmatch [regex]::Escape($required)) { Fail "start script marker missing: $required" }
}

$workflowText = Get-Content -Encoding UTF8 -Raw -Path (Join-Path $Root ".github\workflows\ci.yml")
foreach ($required in @("Pack release zip", "Compress-Archive", "ProGo-release.zip", "ProGo-release-zip")) {
    if ($workflowText -notmatch [regex]::Escape($required)) { Fail "CI release package marker missing: $required" }
}

$releaseWorkflowPath = Join-Path $Root ".github\workflows\release.yml"
if (-not (Test-Path $releaseWorkflowPath)) { Fail "release workflow missing" }
$releaseWorkflowText = Get-Content -Encoding UTF8 -Raw -Path $releaseWorkflowPath
foreach ($required in @(
    "permissions:",
    "contents: write",
    "VERSION",
    "gh release create",
    "gh release upload",
    "ProGo-release.zip",
    "Verify release asset"
)) {
    if ($releaseWorkflowText -notmatch [regex]::Escape($required)) { Fail "release workflow marker missing: $required" }
}

foreach ($scriptName in @("Build-ProGo.ps1", "Install-ProGo.ps1", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Install-FromGitHub.ps1", "Uninstall-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Measure-RescueProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
    $scriptPath = Join-Path $PSScriptRoot $scriptName
    if (-not (Test-Path $scriptPath)) { Fail "script missing: $scriptName" }
    $scriptText = Get-Content -Encoding UTF8 -Raw -Path $scriptPath
    $parseErrors = $null
    $tokens = @([System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors))
    if ($parseErrors -ne $null -and @($parseErrors).Count -gt 0) {
        Fail "PowerShell parser errors in $scriptName"
    }
    if ($tokens.Count -eq 0) { Fail "PowerShell parser returned no tokens for $scriptName" }
    # Match Windows PowerShell's direct -File decoding, not only explicit UTF-8 text.
    $fileTokens = $null; $fileErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$fileTokens, [ref]$fileErrors)
    if (@($fileErrors).Count -gt 0) { Fail ("PowerShell file parser errors in " + $scriptName + ": " + (($fileErrors | ForEach-Object { $_.Message }) -join '; ')) }

}

# Parse every PowerShell test using the same file decoding as Windows PowerShell.
# This must run before compilation and the long native suites.
foreach ($testScript in @(Get-ChildItem -LiteralPath (Join-Path $Root 'tests') -Filter '*.ps1' -File)) {
    $fileTokens = $null; $fileErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($testScript.FullName, [ref]$fileTokens, [ref]$fileErrors)
    if (@($fileErrors).Count -gt 0) {
        Fail ("PowerShell file parser errors in tests/" + $testScript.Name + ": " + (($fileErrors | ForEach-Object { $_.Message }) -join '; '))
    }
}

& $Build
$Exe = Join-Path $Root "release\ProGo.exe"
if (-not (Test-Path $Exe)) { Fail "release ProGo.exe missing" }
$ReleaseIcon = Join-Path $Root "release\ProGo.ico"
if (-not (Test-Path $ReleaseIcon)) { Fail "release ProGo.ico missing" }
if ((Get-Item $ReleaseIcon).Length -le 0) { Fail "release ProGo.ico is empty" }
$ReleaseVersion = Join-Path $Root "release\VERSION"
if (-not (Test-Path $ReleaseVersion)) { Fail "release VERSION missing" }
foreach ($scriptName in @("Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Uninstall-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Measure-RescueProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
    $releaseScript = Join-Path $Root ("release\scripts\" + $scriptName)
    if (-not (Test-Path $releaseScript)) { Fail "release script missing: $scriptName" }
}
$maintenanceSource = Join-Path $Root 'release\scripts\MaintenanceOperation.cs'
if (-not (Test-Path $maintenanceSource)) { Fail 'maintenance runtime source missing' }
if ((Get-FileHash $maintenanceSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\MaintenanceOperation.cs')).Hash) { Fail 'app and helper ownership protocol differ' }
$runtimeBootstrap = Join-Path $Root "release\scripts\Install-FromGitHub.ps1"
if (Test-Path $runtimeBootstrap) { Fail "bootstrap installer must not be included in runtime release scripts" }

foreach ($relativePath in @(
    "scripts\Enable-HomeVpnFirewall.ps1",
    "scripts\home-vpn\HOME_IKEV2.md",
    "scripts\home-vpn\server\ikev2_relay.py",
    "scripts\home-vpn\server\install-ikev2-relay.sh",
    "scripts\home-vpn\server\make_home_profile.py",
    "scripts\home-vpn\server\home_vpn_setup.py"
)) {
    if (-not (Test-Path (Join-Path $Root ("release\" + $relativePath)))) {
        Fail "home VPN release resource missing: $relativePath"
    }
}

$RecoveryHarness = Join-Path $Root "build\SocksRecoveryTests.exe"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
# Capability only: no server/service is started or configured by this probe.
$SshFixtureServer = Join-Path $env:WINDIR 'System32\OpenSSH\sshd.exe'
Write-Host ('Native SSH fixture server available: ' + (Test-Path -LiteralPath $SshFixtureServer -PathType Leaf))
$VaultReadHarness = Join-Path $Root 'build\VaultReadTests.exe'
# Test-only paths/log guards keep this reader harness out of actual user data.
& $Csc /nologo /target:exe /main:ProGo.VaultReadTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultReadHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'tests\VaultReadTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault read harness build failed' }
$VaultReadInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultReadInfo.FileName = $VaultReadHarness
$VaultReadInfo.UseShellExecute = $false
$VaultReadProcess = [System.Diagnostics.Process]::Start($VaultReadInfo)
try {
    if (-not $VaultReadProcess.WaitForExit(90000)) {
        $VaultReadProcess.Kill()
        [void]$VaultReadProcess.WaitForExit(5000)
        Fail 'Vault read harness exceeded its process deadline'
    }
    if ($VaultReadProcess.ExitCode -ne 0) { Fail 'Vault read tests failed' }
} finally { $VaultReadProcess.Dispose() }
$VaultDailyHarness = Join-Path $Root 'build\VaultDailyPolicyTests.exe'
# Compile only the policy/reader and independent guards: no real paths, logging or UI.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyPolicyTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'tests\VaultDailyPolicyTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily policy harness build failed' }
$VaultDailyInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyInfo.FileName = $VaultDailyHarness
$VaultDailyInfo.UseShellExecute = $false
$VaultDailyProcess = [System.Diagnostics.Process]::Start($VaultDailyInfo)
try {
    if (-not $VaultDailyProcess.WaitForExit(90000)) {
        $VaultDailyProcess.Kill()
        [void]$VaultDailyProcess.WaitForExit(5000)
        Fail 'Vault daily policy harness exceeded its process deadline'
    }
    if ($VaultDailyProcess.ExitCode -ne 0) { Fail 'Vault daily policy tests failed' }
} finally { $VaultDailyProcess.Dispose() }
$VaultDailyRecordHarness = Join-Path $Root 'build\VaultDailyRecordTests.exe'
# This guarded record harness uses only memory and native CurrentUser DPAPI.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyRecordTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyRecordHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'tests\VaultDailyRecordTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily record harness build failed' }
$VaultDailyRecordInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyRecordInfo.FileName = $VaultDailyRecordHarness
$VaultDailyRecordInfo.UseShellExecute = $false
$VaultDailyRecordProcess = [System.Diagnostics.Process]::Start($VaultDailyRecordInfo)
try {
    if (-not $VaultDailyRecordProcess.WaitForExit(90000)) {
        $VaultDailyRecordProcess.Kill()
        [void]$VaultDailyRecordProcess.WaitForExit(5000)
        Fail 'Vault daily record harness exceeded its process deadline'
    }
    if ($VaultDailyRecordProcess.ExitCode -ne 0) { Fail 'Vault daily record tests failed' }
} finally { $VaultDailyRecordProcess.Dispose() }
$VaultDailyFileHarness = Join-Path $Root 'build\VaultDailyFileTests.exe'
# Daily-file fixtures use only owned temporary roots and independent path/log guards.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyFileTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyFileHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'src\VaultDailyFile.cs') (Join-Path $Root 'tests\VaultDailyFileTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily file harness build failed' }
$VaultDailyFileInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyFileInfo.FileName = $VaultDailyFileHarness
$VaultDailyFileInfo.UseShellExecute = $false
$VaultDailyFileProcess = [System.Diagnostics.Process]::Start($VaultDailyFileInfo)
try {
    if (-not $VaultDailyFileProcess.WaitForExit(90000)) {
        $VaultDailyFileProcess.Kill()
        [void]$VaultDailyFileProcess.WaitForExit(5000)
        Fail 'Vault daily file harness exceeded its process deadline'
    }
    if ($VaultDailyFileProcess.ExitCode -ne 0) { Fail 'Vault daily file tests failed' }
} finally { $VaultDailyFileProcess.Dispose() }
$VaultDailyJournalHarness = Join-Path $Root 'build\VaultDailyJournalTests.exe'
# Isolated daily journal fixtures use synthetic data and owned temporary roots.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyJournalTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyJournalHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'src\VaultDailyFile.cs') (Join-Path $Root 'src\VaultDailyJournal.cs') (Join-Path $Root 'tests\VaultDailyJournalTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily journal harness build failed' }
$VaultDailyJournalInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyJournalInfo.FileName = $VaultDailyJournalHarness
$VaultDailyJournalInfo.UseShellExecute = $false
$VaultDailyJournalProcess = [System.Diagnostics.Process]::Start($VaultDailyJournalInfo)
try {
    if (-not $VaultDailyJournalProcess.WaitForExit(90000)) {
        $VaultDailyJournalProcess.Kill()
        [void]$VaultDailyJournalProcess.WaitForExit(5000)
        Fail 'Vault daily journal harness exceeded its process deadline'
    }
    if ($VaultDailyJournalProcess.ExitCode -ne 0) { Fail 'Vault daily journal tests failed' }
} finally { $VaultDailyJournalProcess.Dispose() }
$VaultDailyStoreHarness = Join-Path $Root 'build\VaultDailyStoreTests.exe'
# The synchronous store is exercised only on synthetic roots, including native contention.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyStoreTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyStoreHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'src\VaultDailyFile.cs') (Join-Path $Root 'src\VaultDailyStore.cs') (Join-Path $Root 'src\VaultDailyJournal.cs') (Join-Path $Root 'tests\VaultDailyStoreTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily store harness build failed' }
$VaultDailyStoreInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyStoreInfo.FileName = $VaultDailyStoreHarness
$VaultDailyStoreInfo.UseShellExecute = $false
$VaultDailyStoreProcess = [System.Diagnostics.Process]::Start($VaultDailyStoreInfo)
try {
    if (-not $VaultDailyStoreProcess.WaitForExit(90000)) {
        $VaultDailyStoreProcess.Kill()
        [void]$VaultDailyStoreProcess.WaitForExit(5000)
        Fail 'Vault daily store harness exceeded its process deadline'
    }
    if ($VaultDailyStoreProcess.ExitCode -ne 0) { Fail 'Vault daily store tests failed' }
} finally { $VaultDailyStoreProcess.Dispose() }
$VaultDailyRecoveryHarness = Join-Path $Root 'build\VaultDailyRecoveryTests.exe'
# Isolated daily recovery fixtures use synthetic data and owned temporary roots.
& $Csc /nologo /target:exe /main:ProGo.VaultDailyRecoveryTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultDailyRecoveryHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'src\VaultDailyFile.cs') (Join-Path $Root 'src\VaultDailyJournal.cs') (Join-Path $Root 'src\VaultDailyStore.cs') (Join-Path $Root 'src\VaultDailyRecovery.cs') (Join-Path $Root 'tests\VaultDailyRecoveryTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault daily recovery harness build failed' }
$VaultDailyRecoveryInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultDailyRecoveryInfo.FileName = $VaultDailyRecoveryHarness
$VaultDailyRecoveryInfo.UseShellExecute = $false
$VaultDailyRecoveryProcess = [System.Diagnostics.Process]::Start($VaultDailyRecoveryInfo)
try {
    if (-not $VaultDailyRecoveryProcess.WaitForExit(90000)) {
        $VaultDailyRecoveryProcess.Kill()
        [void]$VaultDailyRecoveryProcess.WaitForExit(5000)
        Fail 'Vault daily recovery harness exceeded its process deadline'
    }
    if ($VaultDailyRecoveryProcess.ExitCode -ne 0) { Fail 'Vault daily recovery tests failed' }
} finally { $VaultDailyRecoveryProcess.Dispose() }
$VaultOpenHarness = Join-Path $Root 'build\VaultOpenServiceTests.exe'
# Synthetic records and owned temporary roots only; guarded AppPaths and SafeLog.
& $Csc /nologo /target:exe /main:ProGo.VaultOpenServiceTests /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$VaultOpenHarness" (Join-Path $Root 'src\Vault.cs') (Join-Path $Root 'src\VaultDailyPolicy.cs') (Join-Path $Root 'src\VaultDailyRecord.cs') (Join-Path $Root 'src\VaultDailyFile.cs') (Join-Path $Root 'src\VaultDailyJournal.cs') (Join-Path $Root 'src\VaultDailyStore.cs') (Join-Path $Root 'src\VaultDailyRecovery.cs') (Join-Path $Root 'src\VaultOpenService.cs') (Join-Path $Root 'src\VaultDailyExpiry.cs') (Join-Path $Root 'src\VaultFakeDataFactory.cs') (Join-Path $Root 'tests\VaultOpenServiceTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Vault open service harness build failed' }
$VaultOpenInfo = New-Object System.Diagnostics.ProcessStartInfo
$VaultOpenInfo.FileName = $VaultOpenHarness
$VaultOpenInfo.UseShellExecute = $false
$VaultOpenProcess = [System.Diagnostics.Process]::Start($VaultOpenInfo)
try {
    if (-not $VaultOpenProcess.WaitForExit(90000)) {
        $VaultOpenProcess.Kill()
        [void]$VaultOpenProcess.WaitForExit(5000)
        Fail 'Vault open service harness exceeded its process deadline'
    }
    if ($VaultOpenProcess.ExitCode -ne 0) { Fail 'Vault open service tests failed' }
} finally { $VaultOpenProcess.Dispose() }

$VaultLifetimeSources = @('Vault.cs', 'VaultDailyPolicy.cs', 'VaultDailyRecord.cs', 'VaultDailyFile.cs', 'VaultDailyJournal.cs', 'VaultDailyStore.cs', 'VaultDailyRecovery.cs', 'VaultDailyExpiry.cs', 'VaultOpenService.cs', 'VaultFakeDataFactory.cs') | ForEach-Object { Join-Path $Root ('src\' + $_) }
foreach ($lifetimeTest in @('VaultDailyExpiryTests', 'VaultDailyRestartTests')) {
    $lifetimeHarness = Join-Path $Root ('build\' + $lifetimeTest + '.exe')
    & $Csc /nologo /target:exe ('/main:ProGo.' + $lifetimeTest) /langversion:5 /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll ('/out:' + $lifetimeHarness) $VaultLifetimeSources (Join-Path $Root ('tests\' + $lifetimeTest + '.cs'))
    if ($LASTEXITCODE -ne 0) { Fail ($lifetimeTest + ' harness build failed') }
    $lifetimeInfo = New-Object System.Diagnostics.ProcessStartInfo
    $lifetimeInfo.FileName = $lifetimeHarness
    $lifetimeInfo.UseShellExecute = $false
    $lifetimeProcess = [System.Diagnostics.Process]::Start($lifetimeInfo)
    try {
        if (-not $lifetimeProcess.WaitForExit(600000)) {
            $lifetimeProcess.Kill()
            [void]$lifetimeProcess.WaitForExit(5000)
            Fail ($lifetimeTest + ' harness exceeded its process deadline')
        }
        if ($lifetimeProcess.ExitCode -ne 0) { Fail ($lifetimeTest + ' failed') }
    } finally { $lifetimeProcess.Dispose() }
}
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$RecoveryHarness" (Join-Path $Root "src\Core.cs") (Join-Path $Root "src\BoundedLog.cs") (Join-Path $Root "src\ProxyService.cs") (Join-Path $Root "src\ConnectionHealth.cs") (Join-Path $Root "src\DiagnosticProcess.cs") (Join-Path $Root "tests\SocksRecoveryTests.cs")
if ($LASTEXITCODE -ne 0) { Fail "SOCKS recovery harness build failed" }
& $RecoveryHarness
if ($LASTEXITCODE -ne 0) { Fail "SOCKS recovery tests failed" }

$LogHarness = Join-Path $Root 'build\BoundedLogTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$LogHarness" (Join-Path $Root 'src\BoundedLog.cs') (Join-Path $Root 'tests\BoundedLogTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Bounded log harness build failed' }
& $LogHarness
if ($LASTEXITCODE -ne 0) { Fail 'Bounded log tests failed' }
$logSource = Join-Path $Root 'release\scripts\BoundedLog.cs'
if (-not (Test-Path $logSource) -or (Get-FileHash $logSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\BoundedLog.cs')).Hash) { Fail 'app and helper log policies differ' }
& (Join-Path $Root 'tests\BoundedLogTests.ps1') (Join-Path $Root 'release\scripts')

$RescueHarness = Join-Path $Root 'build\RescueMeasurementTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$RescueHarness" (Join-Path $Root 'src\RescueMeasurement.cs') (Join-Path $Root 'tests\RescueMeasurementTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Rescue measurement harness build failed' }
& $RescueHarness
if ($LASTEXITCODE -ne 0) { Fail 'Rescue measurement tests failed' }
$rescueSource = Join-Path $Root 'release\scripts\RescueMeasurement.cs'
if (-not (Test-Path $rescueSource) -or (Get-FileHash $rescueSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\RescueMeasurement.cs')).Hash) { Fail 'shipped Rescue measurement source differs' }
& (Join-Path $Root 'tests\RescueMeasurementTests.ps1') (Join-Path $Root 'release\scripts')

$shortcutSource = Join-Path $Root 'release\scripts\ApplicationShortcuts.cs'
if ((Get-FileHash $shortcutSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\ApplicationShortcuts.cs')).Hash) { Fail 'app and installer shortcut implementations differ' }
& (Join-Path $Root 'tests\HomeFirewallTests.ps1') (Join-Path $Root 'release\scripts')
& (Join-Path $Root 'tests\ApplicationShortcutsTests.ps1') (Join-Path $Root 'release\scripts')

$DiagnosticHarness = Join-Path $Root 'build\DiagnosticReportTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$DiagnosticHarness" (Join-Path $Root 'src\DiagnosticReport.cs') (Join-Path $Root 'tests\DiagnosticReportTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Diagnostic report harness build failed' }
& $DiagnosticHarness
if ($LASTEXITCODE -ne 0) { Fail 'Diagnostic report tests failed' }
foreach ($sourceName in @('DiagnosticReport.cs','DiagnosticPreview.cs','UiTheme.cs','BrandIcon.cs')) {
    if ((Get-FileHash (Join-Path $Root ('src\' + $sourceName))).Hash -ne (Get-FileHash (Join-Path $Root ('release\scripts\' + $sourceName))).Hash) { Fail 'app and updater diagnostic implementations differ' }
}
& (Join-Path $Root 'tests\DiagnosticPreviewTests.ps1') (Join-Path $Root 'release\scripts')

$RetentionHarness = Join-Path $Root 'build\BackupRetentionTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$RetentionHarness" (Join-Path $Root 'src\BackupRetention.cs') (Join-Path $Root 'tests\BackupRetentionTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Backup retention harness build failed' }
& $RetentionHarness
if ($LASTEXITCODE -ne 0) { Fail 'Backup retention tests failed' }
$retentionSource = Join-Path $Root 'release\scripts\BackupRetention.cs'
if (-not (Test-Path $retentionSource) -or (Get-FileHash $retentionSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\BackupRetention.cs')).Hash) { Fail 'app and updater backup policies differ' }
& (Join-Path $Root 'tests\BackupRetentionTests.ps1') (Join-Path $Root 'release\scripts')

$integritySource = Join-Path $Root 'release\scripts\BackupIntegrity.cs'
if (-not (Test-Path $integritySource) -or (Get-FileHash $integritySource).Hash -ne (Get-FileHash (Join-Path $Root 'src\BackupIntegrity.cs')).Hash) { Fail 'app and helpers backup integrity implementations differ' }
$IntegrityHarness = Join-Path $Root 'build\BackupIntegrityTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$IntegrityHarness" (Join-Path $Root 'src\BackupIntegrity.cs') (Join-Path $Root 'tests\BackupIntegrityTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Backup integrity harness build failed' }
& $IntegrityHarness
if ($LASTEXITCODE -ne 0) { Fail 'Backup integrity tests failed' }
& (Join-Path $Root 'tests\BackupIntegrityTests.ps1') (Join-Path $Root 'release\scripts')

# Exercise actual sockets and the installed Add-Type loader before the longer UI suite.
$TransportSource = Join-Path $Root 'release\scripts\InstalledUpdateTransport.cs'
if (-not (Test-Path $TransportSource) -or (Get-FileHash $TransportSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\InstalledUpdateTransport.cs')).Hash) { Fail 'installed update transport differs from source' }
$TransportHarness = Join-Path $Root 'build\InstalledUpdateTransportTests.exe'
& $Csc /nologo /target:exe /codepage:65001 /reference:System.dll /reference:System.Core.dll "/out:$TransportHarness" $TransportSource (Join-Path $Root 'tests\InstalledUpdateTransportTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Installed update transport harness build failed' }
& $TransportHarness
if ($LASTEXITCODE -ne 0) { Fail 'Installed update transport tests failed' }
& (Join-Path $Root 'tests\InstalledUpdateTransportTests.ps1') (Join-Path $Root 'release\scripts')

# Validate the changed helper transaction before the longer native UI suite.
& (Join-Path $Root 'tests\UpdateProgressTests.ps1') $Exe (Join-Path $Root 'release\scripts')
$WindowSource = Join-Path $Root 'release\scripts\UpdateInstallSession.cs'
if (-not (Test-Path $WindowSource) -or (Get-FileHash $WindowSource).Hash -ne (Get-FileHash (Join-Path $Root 'src\UpdateInstallSession.cs')).Hash) { Fail 'installed update window differs from source' }
$WindowHarness = Join-Path $Root 'build\UpdateInstallWindowTests.exe'
& $Csc /nologo /target:exe /main:ProGo.UpdateInstallWindowTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/out:$WindowHarness" (Join-Path $Root 'src\UpdateInstallSession.cs') (Join-Path $Root 'src\UiTheme.cs') (Join-Path $Root 'src\BrandIcon.cs') $TransportSource (Join-Path $Root 'tests\InstalledUpdateTransportTests.cs') (Join-Path $Root 'tests\UpdateInstallWindowTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Install window harness build failed' }
& $WindowHarness (Join-Path $Root 'build\desktop-shots')
if ($LASTEXITCODE -ne 0) { Fail 'Install window tests failed' }
$DesktopHarness = Join-Path $Root "build\DesktopTests.exe"
$DesktopSources = @(Get-ChildItem (Join-Path $Root 'src') -Filter '*.cs' | ForEach-Object FullName)
$UpdateCheckHarness = Join-Path $Root 'build\UpdateCheckTests.exe'
& $Csc /nologo /target:exe /main:ProGo.UpdateCheckTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$UpdateCheckHarness" $DesktopSources (Join-Path $Root 'tests\UpdateCheckTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Update check harness build failed' }
& $UpdateCheckHarness
if ($LASTEXITCODE -ne 0) { Fail 'Update check tests failed' }
& $Csc /nologo /target:exe /main:ProGo.DesktopTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$DesktopHarness" $DesktopSources (Join-Path $Root 'tests\DesktopTests.cs') (Join-Path $Root 'tests\DesktopUiWorkflowTests.cs') (Join-Path $Root 'tests\DesktopSettingsActionTests.cs') (Join-Path $Root 'tests\DesktopSettingsValidationTests.cs') (Join-Path $Root 'tests\DesktopDashboardLayoutTests.cs') (Join-Path $Root 'tests\DesktopSettingsLayoutTests.cs') (Join-Path $Root 'tests\DesktopSettingsPagesLayoutTests.cs') (Join-Path $Root 'tests\DesktopContrastThemeTests.cs') (Join-Path $Root 'tests\DesktopSettingsAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopBackupAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopVaultAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopVaultUnlockTests.cs') (Join-Path $Root 'tests\DesktopVaultContextTests.cs') (Join-Path $Root 'tests\DesktopVaultAsyncSaveTests.cs') (Join-Path $Root 'tests\DesktopVaultDurableSaveTests.cs') (Join-Path $Root 'tests\DesktopVaultExpiryTests.cs') (Join-Path $Root 'tests\DesktopVaultDailyRuntimeTests.cs') (Join-Path $Root 'tests\DesktopWizardAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopProfileSharingAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopFriendsAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopHelpAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopUpdateTests.cs') (Join-Path $Root 'tests\DesktopUpdaterHandoffTests.cs') (Join-Path $Root 'tests\DesktopHealthTests.cs') (Join-Path $Root 'tests\DesktopStartupTests.cs') (Join-Path $Root 'tests\DesktopSshEditorTests.cs') (Join-Path $Root 'tests\DesktopSshDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopSshAgentServiceTests.cs') (Join-Path $Root 'tests\DesktopSshAgentProbeTests.cs') (Join-Path $Root 'tests\DesktopSshAgentStartTests.cs') (Join-Path $Root 'tests\DesktopSshAgentStartupTests.cs') (Join-Path $Root 'tests\DesktopSshAgentInventoryTests.cs') (Join-Path $Root 'tests\DesktopSshKeyMatchingTests.cs') (Join-Path $Root 'tests\DesktopSshKeyReadinessTests.cs') (Join-Path $Root 'tests\SshAuthenticationPreparationTests.cs') (Join-Path $Root 'tests\DesktopSshKeyReadinessUiTests.cs') (Join-Path $Root 'tests\DesktopSshAuthPolicyTests.cs') (Join-Path $Root 'tests\DesktopSshConfigTests.cs') (Join-Path $Root 'tests\DesktopRouteDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopWindowsRestoreTests.cs') (Join-Path $Root 'tests\DesktopIntegrationMutationTests.cs') (Join-Path $Root 'tests\DesktopTypedCorrectionRecoveryTests.cs') (Join-Path $Root 'tests\DesktopBackupRetentionTests.cs') (Join-Path $Root 'tests\DesktopBackupMetadataTests.cs') (Join-Path $Root 'tests\DesktopManualBackupTests.cs') (Join-Path $Root 'tests\DesktopStartupBackupTests.cs') (Join-Path $Root 'tests\DesktopStartupMigrationTests.cs') (Join-Path $Root 'tests\DesktopStartupBackupLayoutTests.cs') (Join-Path $Root 'tests\DesktopSettingsPersistenceTests.cs') (Join-Path $Root 'tests\DesktopSettingsLoadFailureTests.cs') (Join-Path $Root 'tests\DesktopBridgeConsumerTests.cs') (Join-Path $Root 'tests\DesktopCodexShortcutOwnershipTests.cs') (Join-Path $Root 'tests\DesktopDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopAsyncDiagnosticPreviewTests.cs') (Join-Path $Root 'tests\DesktopDiagnosticContextTests.cs') (Join-Path $Root 'tests\DesktopPhoneStopTests.cs') (Join-Path $Root 'tests\DesktopStartupPreferenceTests.cs') (Join-Path $Root 'tests\DesktopStartupSnapshotAsyncTests.cs') (Join-Path $Root 'tests\DesktopSettingsFolderTests.cs') (Join-Path $Root 'tests\DesktopTrayPreferenceAsyncTests.cs') (Join-Path $Root 'tests\DesktopVisualPolishTests.cs') (Join-Path $Root 'tests\DesktopAsyncSettingsTests.cs') (Join-Path $Root 'tests\DesktopAsyncRestoreTests.cs') (Join-Path $Root 'tests\DesktopAsyncCleanupTests.cs') (Join-Path $Root 'tests\DesktopAsyncUpdaterTests.cs') (Join-Path $Root 'tests\DesktopSshInteractiveKeyAddTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Desktop harness build failed' }
# Test-only loopback server dependencies never enter release or system Python.
$FixtureDeps = Join-Path $Root 'build\ssh-fixture-deps'
$FixturePython = (Get-Command python -ErrorAction Stop).Source
$FixtureRequirements = Join-Path $Root 'tests\ssh-fixture-requirements.txt'
function Invoke-IsolatedSshFixtureTool($ToolArguments, $Label, $BudgetMs) {
    $FixtureOut = Join-Path $Root ('build\ssh-fixture-' + $Label + '-stdout.txt')
    $FixtureError = Join-Path $Root ('build\ssh-fixture-' + $Label + '-stderr.txt')
    # Own the native handle directly: Start-Process -PassThru with redirected
    # streams can expose a null ExitCode after WaitForExit on Windows PowerShell.
    $FixtureTool = New-Object System.Diagnostics.Process
    $FixtureTool.StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $FixtureTool.StartInfo.FileName = $FixturePython
    $FixtureTool.StartInfo.Arguments = ($ToolArguments -join ' ')
    $FixtureTool.StartInfo.UseShellExecute = $false
    $FixtureTool.StartInfo.CreateNoWindow = $true
    $FixtureTool.StartInfo.RedirectStandardOutput = $true
    $FixtureTool.StartInfo.RedirectStandardError = $true
    $FixtureStarted = $false
    $FixtureOutRead = $null
    $FixtureErrorRead = $null
    try {
        if (-not $FixtureTool.Start()) { Fail ('Owned SSH fixture ' + $Label + ' did not start') }
        $FixtureStarted = $true
        $FixtureOutRead = $FixtureTool.StandardOutput.ReadToEndAsync()
        $FixtureErrorRead = $FixtureTool.StandardError.ReadToEndAsync()
        if (-not $FixtureTool.WaitForExit($BudgetMs)) {
            $FixtureTool.Kill()
            if (-not $FixtureTool.WaitForExit(5000)) { Fail ('Owned SSH fixture ' + $Label + ' did not terminate after its deadline') }
            Fail ('Owned SSH fixture ' + $Label + ' exceeded ' + $BudgetMs + ' ms; native validation NOT_RUN')
        }
        $FixtureExitCode = $FixtureTool.ExitCode
        if ($null -eq $FixtureExitCode) { Fail ('Owned SSH fixture ' + $Label + ' did not expose an exit code') }
        if ($FixtureExitCode -ne 0) { Fail ('Owned SSH fixture ' + $Label + ' failed with exit code ' + $FixtureExitCode) }
    } finally {
        try {
            if ($FixtureStarted -and -not $FixtureTool.HasExited) {
                $FixtureTool.Kill()
                if (-not $FixtureTool.WaitForExit(5000)) { Fail ('Owned SSH fixture ' + $Label + ' cleanup did not settle') }
            }
            # unittest stderr is log text, never the success criterion. Read both
            # streams concurrently and retain actual nonzero/timeout failures.
            if ($null -ne $FixtureOutRead) {
                if (-not $FixtureOutRead.Wait(3000)) { Fail ('Owned SSH fixture ' + $Label + ' stdout did not settle') }
                [System.IO.File]::WriteAllText($FixtureOut, $FixtureOutRead.Result)
                Write-Host $FixtureOutRead.Result
            }
            if ($null -ne $FixtureErrorRead) {
                if (-not $FixtureErrorRead.Wait(3000)) { Fail ('Owned SSH fixture ' + $Label + ' stderr did not settle') }
                [System.IO.File]::WriteAllText($FixtureError, $FixtureErrorRead.Result)
                Write-Host $FixtureErrorRead.Result
            }
        } finally { $FixtureTool.Dispose() }
    }
}
# Regression vectors use only this test's own Python processes. Expected
# negative cases must throw the precise reason; no timeout becomes success.
Invoke-IsolatedSshFixtureTool @('-c', '"import sys; sys.exit(0)"') 'exit0-regression' 5000
$FixtureNonzeroObserved = $false
try { Invoke-IsolatedSshFixtureTool @('-c', '"import sys; sys.exit(17)"') 'exit17-regression' 5000 }
catch { if ($_.Exception.Message -notlike '*exit17-regression failed with exit code 17') { throw }; $FixtureNonzeroObserved = $true }
if (-not $FixtureNonzeroObserved) { Fail 'Fixture launcher accepted a nonzero native exit code' }
$FixtureTimeoutObserved = $false
try { Invoke-IsolatedSshFixtureTool @('-c', '"import time; time.sleep(5)"') 'timeout-regression' 200 }
catch { if ($_.Exception.Message -notlike '*timeout-regression exceeded 200 ms; native validation NOT_RUN') { throw }; $FixtureTimeoutObserved = $true }
if (-not $FixtureTimeoutObserved) { Fail 'Fixture launcher accepted a timed-out native process' }
Write-Host 'SSH fixture launcher exit0/exit17/owned-timeout regression PASS'
Invoke-IsolatedSshFixtureTool @('-m', 'pip', 'install', '--quiet', '--target', ('"' + $FixtureDeps + '"'), '--only-binary=:all:', '--disable-pip-version-check', '--retries', '0', '--timeout', '10', '-r', ('"' + $FixtureRequirements + '"')) 'install' 60000
$OldFixturePythonPath = $env:PYTHONPATH
$OldFixturePython = $env:PROGO_TEST_SSH_PYTHON
$OldFixtureScript = $env:PROGO_TEST_SSH_FIXTURE_SCRIPT
try {
    $env:PYTHONPATH = $FixtureDeps
    $env:PROGO_TEST_SSH_PYTHON = $FixturePython
    $env:PROGO_TEST_SSH_FIXTURE_SCRIPT = Join-Path $Root 'tests\ssh_loopback_fixture.py'
    Invoke-IsolatedSshFixtureTool @('-m', 'unittest', 'discover', '-s', ('"' + (Join-Path $Root 'tests') + '"'), '-p', 'ssh_loopback_fixture_tests.py', '-v') 'contract' 90000
    & $DesktopHarness (Join-Path $Root 'build\desktop-shots')
    if ($LASTEXITCODE -ne 0) { Fail 'Desktop tests failed' }
} finally {
    $env:PYTHONPATH = $OldFixturePythonPath
    $env:PROGO_TEST_SSH_PYTHON = $OldFixturePython
    $env:PROGO_TEST_SSH_FIXTURE_SCRIPT = $OldFixtureScript
}
$InstanceHarness = Join-Path $Root 'build\InstanceTests.exe'
& $Csc /nologo /target:exe /main:ProGo.InstanceTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$InstanceHarness" $DesktopSources (Join-Path $Root 'tests\InstanceTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Instance harness build failed' }
& $InstanceHarness $Exe $PSScriptRoot
if ($LASTEXITCODE -ne 0) { Fail 'Instance tests failed' }
& (Join-Path $Root 'tests\MaintenanceTests.ps1') $Exe (Join-Path $Root 'release\scripts')
$ShutdownHarness = Join-Path $Root 'build\ShutdownTests.exe'
& $Csc /nologo /target:exe /main:ProGo.ShutdownTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$ShutdownHarness" $DesktopSources (Join-Path $Root 'tests\ShutdownTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Shutdown harness build failed' }
& $ShutdownHarness $Exe (Join-Path $Root 'release\scripts')
if ($LASTEXITCODE -ne 0) { Fail 'Shutdown tests failed' }
& (Join-Path $Root 'tests\UpdatePackageTests.ps1')
Write-Host "ProGo tests PASS."
