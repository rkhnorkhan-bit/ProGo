Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# Isolated backup-function fixture. It does not activate a Program Files installation.
$root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $root 'scripts\Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Updater does not parse' }
foreach ($name in @('Copy-FileIfExists','Copy-DirectoryIfExists','Backup-InstalledState')) {
    $matches = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $false))
    if ($matches.Count -ne 1) { throw "Expected exactly one function: $name" }
    . ([scriptblock]::Create($matches[0].Extent.Text))
}
# The fixture observes the archive source; production integrity and retention have their own suites.
Add-Type -TypeDefinition @'
namespace ProGo {
 public static class BackupIntegrity {
  public static string ArchiveSource;
  public static PreparedBackup Prepare(string source) {
   string target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "progo-test-prepared-" + System.Guid.NewGuid().ToString("N"));
   try { CopyTree(source, target); return new PreparedBackup(target); }
   catch { if (System.IO.Directory.Exists(target)) System.IO.Directory.Delete(target, true); throw; }
  }
  private static void CopyTree(string source, string target) {
   System.IO.Directory.CreateDirectory(target);
   foreach (string file in System.IO.Directory.GetFiles(source)) System.IO.File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)), false);
   foreach (string dir in System.IO.Directory.GetDirectories(source)) CopyTree(dir, System.IO.Path.Combine(target, System.IO.Path.GetFileName(dir)));
  }
  public static void CopyPersonalArchives(string source, string destination) { ArchiveSource = source; }
  public static string Contents(string path) { return "fixture"; }
  public static string[] CompositionLines(string path) { return new string[0]; }
  public static void Write(string path) { }
  public static void Validate(string path) { }
 }
 public sealed class PreparedBackup : System.IDisposable {
  public string Path { get; private set; }
  public PreparedBackup(string path) { Path = path; }
  public void Dispose() { if (System.IO.Directory.Exists(Path)) System.IO.Directory.Delete(Path, true); }
 }
 public static class BackupRetention {
  public static object Plan(string root, string current) { return null; }
  public static RetentionResult Apply(object plan) { return new RetentionResult(); }
 }
 public class RetentionResult { public int Deleted, Kept, Failed, Skipped; }
}
'@
function Get-LocalVersion { '0.2.2' }
function Write-UpdateLog($Message) { }
$work = Join-Path ([IO.Path]::GetTempPath()) ('progo-roots-test-' + [guid]::NewGuid().ToString('N'))
try {
    foreach ($mode in @('separate','legacy')) {
        $InstallDir = Join-Path $work ($mode + '-runtime')
        $DataRoot = if ($mode -eq 'legacy') { $InstallDir } else { Join-Path $work 'separate-data' }
        $BackupsDir = Join-Path $DataRoot 'backups'
        New-Item -ItemType Directory -Force -Path $InstallDir, $DataRoot, (Join-Path $InstallDir 'scripts') | Out-Null
        [IO.File]::WriteAllText((Join-Path $InstallDir 'ProGo.exe'), 'runtime')
        [IO.File]::WriteAllText((Join-Path $InstallDir 'scripts\fixture.txt'), 'script')
        [IO.File]::WriteAllText((Join-Path $DataRoot 'settings.json'), 'private-settings')
        if ($mode -eq 'separate') { [IO.File]::WriteAllText((Join-Path $InstallDir 'settings.json'), 'wrong-root') }
        $Timestamp = 'fixture'; $State = @{ RemoteVersion = '0.2.3'; UpdateMode = 'fixture' }
        $backup = Backup-InstalledState
        if ([IO.File]::ReadAllText((Join-Path $backup 'ProGo.exe')) -cne 'runtime' -or
            [IO.File]::ReadAllText((Join-Path $backup 'scripts\fixture.txt')) -cne 'script' -or
            [IO.File]::ReadAllText((Join-Path $backup 'settings.json')) -cne 'private-settings' -or
            [ProGo.BackupIntegrity]::ArchiveSource -cne $DataRoot) { throw "Wrong backup source: $mode" }
        if ([IO.File]::ReadAllText((Join-Path $DataRoot 'settings.json')) -cne 'private-settings') { throw 'Source data changed' }
        Write-Host "PASS: updater backup roots $mode"
    }
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}

# Execute the shipped core transaction, replacing only the KnownFolder selection
# in this in-memory fixture and network/UI/self-check boundaries. No production
# parameter or environment variable can select this synthetic layout.
$work = Join-Path ([IO.Path]::GetTempPath()) ('progo-update-flow-' + [guid]::NewGuid().ToString('N'))
$Scripts = Join-Path $root 'scripts'
try {
    $dataBase = Join-Path $work 'data'; $programBase = Join-Path $work 'programs'
    $data = Join-Path $dataBase 'ProGo'; $runtime = Join-Path $programBase 'ProGo'
    $release = Join-Path $work 'release'
    foreach ($path in @($data, $runtime, (Join-Path $runtime 'scripts'), $release, (Join-Path $release 'scripts'))) {
        [void][IO.Directory]::CreateDirectory($path)
    }
    $installedExe = Join-Path $runtime 'ProGo.exe'
    Add-Type -TypeDefinition 'public static class FixtureUpdateProgram { public static void Main() {} }' -OutputAssembly $installedExe -OutputType ConsoleApplication
    [IO.File]::Copy($installedExe, (Join-Path $release 'ProGo.exe'))
    # Different bytes with the same valid assembly identity.
    $stream = [IO.File]::Open((Join-Path $release 'ProGo.exe'), [IO.FileMode]::Append)
    try { $stream.WriteByte(42) } finally { $stream.Dispose() }
    [IO.File]::WriteAllText((Join-Path $runtime 'VERSION'), '0.1.0')
    [IO.File]::WriteAllText((Join-Path $release 'VERSION'), '0.2.0')
    [IO.File]::WriteAllText((Join-Path $runtime 'ProGo.ico'), 'old icon')
    [IO.File]::WriteAllText((Join-Path $release 'ProGo.ico'), 'new icon')
    [IO.File]::WriteAllText((Join-Path $runtime 'scripts\old.txt'), 'old script')
    [IO.File]::WriteAllText((Join-Path $runtime 'foreign.txt'), 'foreign-runtime-sentinel')
    foreach ($name in @('Update-ProGo.ps1','Update-ProGo.Core.ps1')) { [IO.File]::WriteAllText((Join-Path $release "scripts\$name"), 'new script') }
    foreach ($name in @('settings.json','vault.enc.json','progo.log','progo-update.log')) {
        [IO.File]::WriteAllText((Join-Path $data $name), "data-$name")
        [IO.File]::WriteAllText((Join-Path $runtime $name), "wrong-$name")
    }
    $WaitPid = 0; $NoLaunch = $true; $Force = $true; $NoReleasePackage = $false
    $ReleasePackageUrl = ''; $SourceZipUrl = ''; $RemoteVersionUrl = ''
    foreach ($statement in $ast.EndBlock.Statements) {
        if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) {
            . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot', '$Scripts')))
        }
    }
    function Initialize-UpdateWindow { }
    function Close-UpdateWindow { }
    function Show-UpdatePhase($Phase) { }
    function Clear-UpdateProgress { }
    function Show-UpdateDialog($Text, $Title, $Kind) { }
    function Test-UpdateRequired { $State.RemoteVersion = '0.2.0'; return $true }
    function Get-ReleaseDirForUpdate { return $release }
    function Start-UpdatedProGo($ExePath) { if ($ExePath -cne (Join-Path $runtime 'ProGo.exe')) { throw 'Incorrect restart destination' } }
    function Start-Process { param($FilePath, $ArgumentList, $WorkingDirectory, [switch]$PassThru, [switch]$Wait); return [pscustomobject]@{ ExitCode = 0 } }
    $realInstall = ${function:Install-StagingToMain}
    $realRollback = ${function:Restore-BackupToMain}
    $script:injectUpdateFailure = $false
    function Install-StagingToMain($TargetDir) {
        & $realInstall $TargetDir
        if ($script:injectUpdateFailure) {
            if (-not $State.MainWasChanged -or [IO.File]::ReadAllText((Join-Path $InstallDir 'VERSION')) -cne '0.2.0') { throw 'Update fault preconditions failed' }
            [IO.File]::WriteAllText((Join-Path $work 'update-fault-injected'), 'after-main-changed')
            throw 'Synthetic failure after main changed'
        }
    }
    function Restore-BackupToMain($SourceBackupDir) {
        if (-not $SourceBackupDir.StartsWith((Join-Path $data 'backups') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Rollback used wrong backup root' }
        [IO.File]::WriteAllText((Join-Path $work 'update-rollback-entered'), 'entered')
        & $realRollback $SourceBackupDir
        [IO.File]::WriteAllText((Join-Path $work 'update-rollback-completed'), 'completed')
    }
    function Test-RollbackApplication {
        if ([IO.File]::ReadAllText((Join-Path $InstallDir 'VERSION')) -cne '0.1.0') { throw 'Rollback self-check invoked before original version returned' }
        [IO.File]::WriteAllText((Join-Path $work 'update-rollback-selfcheck'), 'isolated-stub')
    }
    $actualStageCheck = ${function:Test-StagingCopy}
    function Test-StagingCopy($TargetDir) {
        foreach ($name in @('settings.json','vault.enc.json','progo.log','progo-update.log')) {
            if ([IO.File]::ReadAllText((Join-Path $TargetDir $name)) -cne "data-$name") { throw "Wrong staged private source: $name" }
        }
        & $actualStageCheck $TargetDir
    }
    function Invoke-SyntheticUpdater {
        foreach ($statement in $ast.EndBlock.Statements) {
            if ($statement -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
                $source = $statement.Extent.Text.Replace('$PSScriptRoot', '$Scripts')
                if ($source -ceq '$layout = Resolve-ProGoLayout') {
                    $source = '$layout = Resolve-ProGoLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot $runtime -SeparateIdentityConfirmed $true'
                }
                . ([scriptblock]::Create($source))
            }
        }
    }
    # The proxy marker exists only in DataRoot. It must refuse before backup or mutation.
    $proxyMarker = Join-Path $data 'system-proxy-backup.json'
    [IO.File]::WriteAllText($proxyMarker, 'fixture marker')
    $refused = $false
    $proxyRefusal = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('0J7Rh9C40YHRgtC60LAg0L/RgNC+0LrRgdC4IFByb0dvINC90LUg0LfQsNCy0LXRgNGI0LXQvdCw'))
    try { Invoke-SyntheticUpdater } catch { $refused = $_.Exception.ToString().Contains($proxyRefusal) }
    if (-not $refused -or [IO.File]::ReadAllText((Join-Path $runtime 'VERSION')) -cne '0.1.0' -or
        [IO.Directory]::Exists((Join-Path $data 'backups')) -or -not [IO.File]::Exists($proxyMarker)) {
        throw 'Data-root proxy marker did not refuse update before mutation'
    }
    [IO.File]::Delete($proxyMarker)
    $oldProgram = Get-RollbackProgramState $runtime
    $script:injectUpdateFailure = $true
    $rollbackError = $null
    try { Invoke-SyntheticUpdater } catch { $rollbackError = $_.Exception.Message }
    if ($rollbackError -cne 'Synthetic failure after main changed' -or
        -not [IO.File]::Exists((Join-Path $work 'update-fault-injected')) -or
        -not [IO.File]::Exists((Join-Path $work 'update-rollback-entered')) -or
        -not [IO.File]::Exists((Join-Path $work 'update-rollback-completed')) -or
        -not [IO.File]::Exists((Join-Path $work 'update-rollback-selfcheck')) -or
        (Get-RollbackProgramState $runtime) -cne $oldProgram -or
        [IO.File]::ReadAllText((Join-Path $runtime 'foreign.txt')) -cne 'foreign-runtime-sentinel') { throw 'Real updater rollback was not proven' }
    foreach ($name in @('settings.json','vault.enc.json','progo.log','progo-update.log')) {
        if ([IO.File]::ReadAllText((Join-Path $data $name)) -cne "data-$name" -or
            [IO.File]::ReadAllText((Join-Path $runtime $name)) -cne "wrong-$name") { throw "Rollback changed private sentinel: $name" }
    }
    # The generated failure backup is fixture-owned; keep it outside retention's next run.
    $failureBackups = @(Get-ChildItem -LiteralPath (Join-Path $data 'backups') -Directory)
    if ($failureBackups.Count -ne 1 -or (Get-RollbackProgramState $failureBackups[0].FullName) -cne $oldProgram) { throw 'Failure backup differs from original runtime' }
    Remove-Item -LiteralPath $failureBackups[0].FullName -Recurse -Force
    $script:injectUpdateFailure = $false
    Invoke-SyntheticUpdater
    if ([IO.File]::ReadAllText((Join-Path $runtime 'VERSION')) -cne '0.2.0' -or
        (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne (Get-FileHash (Join-Path $release 'ProGo.exe')).Hash -or
        [IO.File]::ReadAllText((Join-Path $runtime 'scripts\Update-ProGo.ps1')) -cne 'new script' -or
        [IO.File]::Exists((Join-Path $runtime 'scripts\old.txt'))) { throw 'Updater did not install the release into the runtime root' }
    foreach ($name in @('settings.json','vault.enc.json','progo.log','progo-update.log')) {
        if ([IO.File]::ReadAllText((Join-Path $data $name)) -cne "data-$name" -or
            [IO.File]::ReadAllText((Join-Path $runtime $name)) -cne "wrong-$name") { throw "Private sentinel changed: $name" }
    }
    if ([IO.File]::ReadAllText((Join-Path $runtime 'foreign.txt')) -cne 'foreign-runtime-sentinel') { throw 'Foreign runtime sentinel changed' }
    if (-not [IO.File]::Exists((Join-Path $data 'update.log')) -or [IO.File]::Exists((Join-Path $runtime 'update.log'))) { throw 'Update log routed to the wrong root' }
    $backups = @(Get-ChildItem -LiteralPath (Join-Path $data 'backups') -Directory)
    if ($backups.Count -ne 1 -or [IO.File]::ReadAllText((Join-Path $backups[0].FullName 'settings.json')) -cne 'data-settings.json' -or
        [IO.File]::ReadAllText((Join-Path $backups[0].FullName 'vault.enc.json')) -cne 'data-vault.enc.json' -or
        [IO.File]::ReadAllText((Join-Path $backups[0].FullName 'VERSION')) -cne '0.1.0' -or
        [ProGo.BackupIntegrity]::ArchiveSource -cne $data -or [IO.Directory]::Exists((Join-Path $runtime 'backups'))) { throw 'Backup routed to wrong root' }
    Write-Host 'PASS: synthetic separate-root updater transaction and private sentinels'
} finally {
    if ([IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
}
