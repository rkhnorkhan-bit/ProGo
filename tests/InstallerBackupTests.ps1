Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer backup helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-backup-tests-' + [Guid]::NewGuid().ToString('N'))
function Check-InstallerBackup($Condition, $Name) {
    if (-not $Condition) { throw ('Installer backup test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($mode in @('complete', 'fresh', 'corrupt-copy', 'copy-error', 'old-changed', 'tampered-manifest', 'new-leaf', 'foreign-cleanup')) {
        $install = Join-Path $work $mode
        New-Item -ItemType Directory -Path $install | Out-Null
        $personal = Join-Path $install 'settings.json'
        [IO.File]::WriteAllText($personal, 'synthetic personal bytes')
        $old = Join-Path $install 'ProGo.exe'
        if ($mode -ne 'fresh') { [IO.File]::WriteAllText($old, 'synthetic prior runtime') }
        $script:CreatedBackup = $null
        function Copy-ProGoInstallerBackupFile([string]$Source, [string]$Destination) {
            if ($null -eq $script:CreatedBackup) { $script:CreatedBackup = [IO.Path]::GetDirectoryName($Destination) }
            if ($mode -eq 'copy-error') { throw 'synthetic backup copy failure' }
            [IO.File]::Copy($Source, $Destination, $false)
            if ($mode -eq 'corrupt-copy') { [IO.File]::WriteAllText($Destination, 'corrupt backup') }
            if ($mode -eq 'old-changed') { [IO.File]::WriteAllText($Source, 'externally changed synthetic runtime') }
        }
        if ($mode -in @('corrupt-copy', 'copy-error', 'old-changed')) {
            $rejected = $false
            try { New-ProGoInstallerBackup $payload $install | Out-Null } catch { $rejected = $true }
            Check-InstallerBackup ($rejected -and $null -ne $script:CreatedBackup -and
                -not [IO.Directory]::Exists($script:CreatedBackup)) ($mode + ' fails closed and removes only unverified copy')
            $expectedOld = if ($mode -eq 'old-changed') { 'externally changed synthetic runtime' } else { 'synthetic prior runtime' }
            Check-InstallerBackup ([IO.File]::ReadAllText($old) -ceq $expectedOld) 'no runtime overwrite or unrequested rollback'
        } else {
            $backup = New-ProGoInstallerBackup $payload $install
            Assert-ProGoInstallerBackup $backup
            Check-InstallerBackup (@($backup.Entries | Where-Object { $_.Relative -eq 'settings.json' }).Count -eq 0) 'personal data excluded'
            if ($mode -eq 'fresh') {
                Check-InstallerBackup ($null -eq $backup.Directory -and -not [IO.Directory]::Exists((Join-Path $install 'backups'))) 'fresh install proves no overwritten leaves without fake backup'
            } elseif ($mode -eq 'complete') {
                Check-InstallerBackup ([IO.File]::ReadAllText((Join-Path $backup.Directory 'ProGo.exe')) -ceq 'synthetic prior runtime') 'exact prior bytes retained'
                $receipt = Get-Content -LiteralPath (Join-Path $backup.Directory 'installer-snapshot.json') -Raw | ConvertFrom-Json
                Check-InstallerBackup ($receipt.NewVersion -eq $payload.Version -and $receipt.Files.Count -eq $backup.Entries.Count -and
                    @($receipt.Files | Where-Object { $_.Exists }).Count -eq 1 -and
                    $receipt.Files[0].NewHash -ceq $payload.Hashes['ProGo.exe']) 'persisted inventory and new identity'
                Check-InstallerBackup ([IO.File]::ReadAllText($old) -ceq 'synthetic prior runtime') 'original unchanged'
            } elseif ($mode -eq 'foreign-cleanup') {
                $foreign = Join-Path $install 'unrelated'; New-Item -ItemType Directory -Path $foreign | Out-Null
                $marker = Join-Path $foreign 'preserved.txt'; [IO.File]::WriteAllText($marker, 'preserved')
                $rejected = $false
                try { Remove-ProGoInstallerUnverifiedBackup ([pscustomobject]@{ InstallDirectory = $install; Directory = $foreign }) } catch { $rejected = $true }
                Check-InstallerBackup ($rejected -and [IO.File]::ReadAllText($marker) -ceq 'preserved' -and [IO.Directory]::Exists($backup.Directory)) 'foreign cleanup refused and verified backup retained'
            } else {
                if ($mode -eq 'tampered-manifest') { [IO.File]::AppendAllText((Join-Path $backup.Directory 'installer-snapshot.json'), 'changed') }
                if ($mode -eq 'new-leaf') { [IO.File]::WriteAllText((Join-Path $install 'VERSION'), 'new external leaf') }
                $rejected = $false
                try { Assert-ProGoInstallerBackup $backup } catch { $rejected = $true }
                Check-InstallerBackup ($rejected -and [IO.File]::ReadAllText($old) -ceq 'synthetic prior runtime') ($mode + ' rejected before overwrite')
            }
        }
        Check-InstallerBackup ([IO.File]::ReadAllText($personal) -ceq 'synthetic personal bytes') 'personal bytes unchanged'
        Write-Host ('PASS: installer backup ' + $mode)
    }
    Write-Host 'Installer backup tests PASS: 8'
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
