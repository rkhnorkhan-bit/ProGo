param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $Scripts 'BackupRetention-ProGo.ps1')
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
# Run the real updater backup function, without its network or installation body.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Updater parse failure' }
foreach ($function in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $false)) {
    . ([scriptblock]::Create($function.Extent.Text))
}
function Write-UpdateLog($Message) { Write-Host $Message }
$InstallDir = Join-Path $env:TEMP ('ProGo-retention-updater-' + [guid]::NewGuid().ToString('N'))
$BackupsDir = Join-Path $InstallDir 'backups'
$LocalVersionFile = Join-Path $InstallDir 'VERSION'
$State = @{ RemoteVersion = '0.0.2'; UpdateMode = 'fixture' }
$Timestamp = '20260201-000000'
try {
    New-Item -ItemType Directory -Path $BackupsDir -Force | Out-Null
    Set-Content $LocalVersionFile '0.0.1'
    Set-Content (Join-Path $InstallDir 'ProGo.exe') 'fixture; never execute'
    Copy-Item $Scripts (Join-Path $InstallDir 'scripts') -Recurse
    foreach ($i in 0..29) {
        $dir = Join-Path $BackupsDir ('backup-20260101-{0:D4}' -f $i)
        New-Item -ItemType Directory $dir | Out-Null
        Set-Content (Join-Path $dir 'manifest.txt') @('product=ProGo','backup_kind=automatic','created_by=app')
    }
    $manual = Join-Path $BackupsDir 'backup-20250101-manual'
    $baseline = Join-Path $BackupsDir 'backup-20250102-baseline'
    $unknown = Join-Path $BackupsDir 'backup-20250103-unknown'
    foreach ($dir in @($manual,$baseline,$unknown)) { New-Item -ItemType Directory $dir | Out-Null }
    Set-Content (Join-Path $manual 'manifest.txt') @('product=ProGo','backup_kind=manual','created_by=manual')
    Set-Content (Join-Path $manual 'payload.txt') 'manual copy must survive'
    Set-Content (Join-Path $baseline 'manifest.txt') @('product=ProGo','backup_kind=baseline')
    Set-Content (Join-Path $unknown 'manifest.txt') 'unrecognised contents'
    $saved = (Get-FileHash (Join-Path $manual 'payload.txt')).Hash
    $backup = Backup-InstalledState
    [ProGo.BackupIntegrity]::Validate($backup)
    if ((Get-Content (Join-Path $backup 'manifest.txt') -Raw) -match 'contains=.*vault.enc.json') { throw 'Updater invents absent user data' }
    if (-not (Test-Path $backup) -or (Get-FileHash (Join-Path $manual 'payload.txt')).Hash -ne $saved) { throw 'Updater removed old manual copy' }
    foreach ($dir in @($baseline,$unknown)) { if (-not (Test-Path $dir)) { throw 'Updater removed protected copy' } }
    if (@(Get-ChildItem $BackupsDir -Directory).Count -ne 13) { throw 'Updater retention differs from ten-slot shared policy' }
    if ([ProGo.BackupRetention]::Plan($BackupsDir).Candidates.Count -ne 0) { throw 'App and updater retention disagree' }
    $Timestamp = '20260201-000001'
    Set-Content (Join-Path $InstallDir 'settings.json') 'synthetic settings'
    $locked = [IO.File]::Open((Join-Path $InstallDir 'settings.json'), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $refused = $false
        try { Backup-InstalledState | Out-Null } catch { $refused = $true }
        if (-not $refused) { throw 'Updater silently certified a failed user-data copy' }
        $incomplete = Join-Path $BackupsDir 'backup-20260201-000001-v0.0.1-to-v0.0.2'
        if (Test-Path (Join-Path $incomplete 'backup-files.sha256')) { throw 'Partial backup has a completed digest index' }
    } finally { $locked.Dispose() }
    Remove-Item (Join-Path $InstallDir 'ProGo.exe')
    $Timestamp = '20260201-000002'
    $refused = $false
    try { Backup-InstalledState | Out-Null } catch { $refused = $true }
    if (-not $refused) { throw 'Updater reported incomplete installation as a ready copy' }
    Write-Host 'Backup updater tests PASS: manual contents, unknown folder, latest baseline/pre-update ten-slot parity, shared integrity and locked-copy refusal'
} finally { if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force } }
