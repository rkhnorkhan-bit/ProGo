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
    "LooksDirectTarget",
    "FoundInConfig",
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
if ($updateLauncherSource -notmatch 'CreateNoWindow = false') { Fail "updater must show its progress console" }
if ($updateLauncherSource -notmatch 'MaintenanceOperation.StartHandoff') { Fail "updater ownership handoff guard missing" }

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

foreach ($scriptName in @("Build-ProGo.ps1", "Install-ProGo.ps1", "Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Install-FromGitHub.ps1", "Uninstall-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
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

& $Build
$Exe = Join-Path $Root "release\ProGo.exe"
if (-not (Test-Path $Exe)) { Fail "release ProGo.exe missing" }
$ReleaseIcon = Join-Path $Root "release\ProGo.ico"
if (-not (Test-Path $ReleaseIcon)) { Fail "release ProGo.ico missing" }
if ((Get-Item $ReleaseIcon).Length -le 0) { Fail "release ProGo.ico is empty" }
$ReleaseVersion = Join-Path $Root "release\VERSION"
if (-not (Test-Path $ReleaseVersion)) { Fail "release VERSION missing" }
foreach ($scriptName in @("Update-ProGo.ps1", "Update-ProGo.Core.ps1", "Restore-ProGoBackup.ps1", "Show-ProGo.ps1", "Uninstall-ProGo.ps1", "Start-ProGo.ps1", "Repair-ProGo.ps1", "Maintenance-ProGo.ps1", "BackupRetention-ProGo.ps1", "BackupIntegrity-ProGo.ps1", "Log-ProGo.ps1", "Diagnostics-ProGo.ps1", "Shortcuts-ProGo.ps1", "Firewall-ProGo.ps1")) {
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

$DesktopHarness = Join-Path $Root "build\DesktopTests.exe"
$DesktopSources = @(Get-ChildItem (Join-Path $Root 'src') -Filter '*.cs' | ForEach-Object FullName)
$UpdateCheckHarness = Join-Path $Root 'build\UpdateCheckTests.exe'
& $Csc /nologo /target:exe /main:ProGo.UpdateCheckTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$UpdateCheckHarness" $DesktopSources (Join-Path $Root 'tests\UpdateCheckTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Update check harness build failed' }
& $UpdateCheckHarness
if ($LASTEXITCODE -ne 0) { Fail 'Update check tests failed' }
& $Csc /nologo /target:exe /main:ProGo.DesktopTests /codepage:65001 /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$DesktopHarness" $DesktopSources (Join-Path $Root 'tests\DesktopTests.cs') (Join-Path $Root 'tests\DesktopUiWorkflowTests.cs') (Join-Path $Root 'tests\DesktopSettingsActionTests.cs') (Join-Path $Root 'tests\DesktopSettingsValidationTests.cs') (Join-Path $Root 'tests\DesktopDashboardLayoutTests.cs') (Join-Path $Root 'tests\DesktopSettingsLayoutTests.cs') (Join-Path $Root 'tests\DesktopSettingsPagesLayoutTests.cs') (Join-Path $Root 'tests\DesktopContrastThemeTests.cs') (Join-Path $Root 'tests\DesktopSettingsAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopBackupAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopVaultAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopWizardAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopProfileSharingAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopFriendsAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopHelpAccessibilityTests.cs') (Join-Path $Root 'tests\DesktopUpdateTests.cs') (Join-Path $Root 'tests\DesktopHealthTests.cs') (Join-Path $Root 'tests\DesktopStartupTests.cs') (Join-Path $Root 'tests\DesktopSshEditorTests.cs') (Join-Path $Root 'tests\DesktopSshDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopRouteDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopWindowsRestoreTests.cs') (Join-Path $Root 'tests\DesktopBackupRetentionTests.cs') (Join-Path $Root 'tests\DesktopBridgeConsumerTests.cs') (Join-Path $Root 'tests\DesktopDiagnosticTests.cs') (Join-Path $Root 'tests\DesktopStartupPreferenceTests.cs')
if ($LASTEXITCODE -ne 0) { Fail 'Desktop harness build failed' }
& $DesktopHarness (Join-Path $Root 'build\desktop-shots')
if ($LASTEXITCODE -ne 0) { Fail 'Desktop tests failed' }
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
