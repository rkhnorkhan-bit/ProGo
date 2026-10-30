param([switch]$Child,[string]$CaseDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Legacy restart helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
if ($Child) {
    $parent = [IO.Path]::GetDirectoryName($CaseDirectory)
    if ([IO.Path]::GetFileName($CaseDirectory) -cne 'complete' -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-legacy-restart-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Restart child refuses unowned fixture scope.' }
    $install = Join-Path $CaseDirectory 'app'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1) { throw 'Restart child journal is ambiguous.' }
    $observed = Restore-ProGoInstallerLegacyReferenceEvidence $journals[0].FullName $install (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup')
    if ($null -eq $observed -or $observed.MutationAllowed -or $observed.FullRollback -or $observed.InstallSucceeded) { throw 'Fresh-process Legacy evidence did not verify safely.' }
    Write-Host 'LEGACY_RESTART_CHILD verified=True; mutationAllowed=False; installSucceeded=False'
    return
}
function Check-LegacyRestart($Condition,[string]$Name) { if (-not $Condition) { throw ('Legacy restart failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-restart-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $stage = $null; $process = $null; $junction = $null; $junctionOwned = $false; $cancel = $null; $outTask = $null; $errTask = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('complete','no-seal','partial-seal','seal-tampered','seal-replaced-same-bytes','foreign-seal','reference-tampered',
        'reference-replaced-same-bytes','source-tampered','source-replaced-same-bytes','original-replaced-same-bytes',
        'journal-tampered','snapshot-tampered','runtime-tampered','wrong-location','operation-claim','snapshot-claim','location-claim','hash-claim',
        'extra-record','missing-reference','missing-source','missing-original','reparse-seal','cancel-seal','private-entry-claim')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $name),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        $originalHash = (Get-FileHash -LiteralPath $legacy).Hash
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        $journal = $backup.Journal.Directory; $suffix = [IO.Path]::GetFileName($journal).Substring('installer-journal-'.Length)
        $sourcePath = Join-Path (Join-Path $install 'backups') ('installer-legacy-source-' + $suffix + '.bin')
        $referencePath = Join-Path (Join-Path $install 'backups') ('installer-legacy-reference-' + $suffix + '.json')
        $sealPath = Join-Path (Join-Path $install 'backups') ('installer-legacy-seal-' + $suffix + '.bin')
        if ($scenario -eq 'cancel-seal') {
            $cancel = New-Object Threading.CancellationTokenSource; $cancel.Cancel(); $canceled = $false
            try { Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup $cancel.Token } catch {
                $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                if ($cause -isnot [OperationCanceledException]) { throw }; $canceled = $true
            } finally { $cancel.Dispose(); $cancel = $null }
            Check-LegacyRestart ($canceled -and -not [IO.File]::Exists($sealPath)) 'cancellation before seal creates no authority'
        } elseif ($scenario -ne 'no-seal') { Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup }
        if ($scenario -eq 'complete') {
            [byte[]]$foreignSeal = [IO.File]::ReadAllBytes($sealPath)
            $repeatFailed = $false; try { Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup } catch { $repeatFailed = $true }
            Check-LegacyRestart ($repeatFailed -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($sealPath)) -ceq [Convert]::ToBase64String($foreignSeal)) 'seal is never reused or overwritten'
        }
        Check-LegacyRestart ((Get-FileHash -LiteralPath $legacy).Hash -ceq $originalHash) 'seal publication preserves exact original Legacy bytes'
        switch ($scenario) {
            'partial-seal' { [IO.File]::WriteAllText($sealPath,'PROGO-LEGACY-SEAL-1') }
            'seal-tampered' { [IO.File]::AppendAllText($sealPath,'x') }
            'foreign-seal' { [IO.File]::WriteAllBytes($sealPath,$foreignSeal) }
            'reference-tampered' { [IO.File]::AppendAllText($referencePath,' ') }
            'source-tampered' { [IO.File]::AppendAllText($sourcePath,'x') }
            'journal-tampered' { [IO.File]::WriteAllText((Join-Path $journal 'admit-000000.json'),'{}') }
            'snapshot-tampered' { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic damaged snapshot') }
            'runtime-tampered' { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic changed runtime') }
            'private-entry-claim' {
                $firstPath = Join-Path $journal 'admit-000000.json'
                $first = [IO.File]::ReadAllText($firstPath) | ConvertFrom-Json
                $inventory = $first.Inventory | ConvertFrom-Json
                $inventory.Files[0].Relative = 'settings.json'; $first.Entry.Relative = 'settings.json'
                $first.Inventory = $inventory | ConvertTo-Json -Depth 6 -Compress
                [IO.File]::WriteAllText($firstPath,($first | ConvertTo-Json -Depth 8 -Compress))
            }
            'extra-record' { [IO.File]::WriteAllText((Join-Path $journal 'foreign.json'),'{}') }
            'missing-reference' { [IO.File]::Move($referencePath,(Join-Path $case 'retained-reference')) }
            'missing-source' { [IO.File]::Move($sourcePath,(Join-Path $case 'retained-source')) }
            'missing-original' { [IO.File]::Move($legacy,(Join-Path $case 'retained-original')) }
        }
        if ($scenario -in @('operation-claim','snapshot-claim','location-claim','hash-claim')) {
            $claims = [IO.File]::ReadAllText($referencePath) | ConvertFrom-Json
            switch ($scenario) {
                'operation-claim' { $claims.Context.Operation = [Guid]::NewGuid().ToString('N') }
                'snapshot-claim' { $claims.Context.Snapshot = '..\foreign' }
                'location-claim' { $claims.Source.LocationIdentity = 'B' * 64 }
                'hash-claim' { $claims.Source.DocumentHash = 'B' * 64 }
            }
            [IO.File]::WriteAllText($referencePath,($claims | ConvertTo-Json -Depth 10 -Compress))
        }
        $replace = if ($scenario -eq 'seal-replaced-same-bytes') { $sealPath } elseif ($scenario -eq 'reference-replaced-same-bytes') { $referencePath }
            elseif ($scenario -eq 'source-replaced-same-bytes') { $sourcePath } elseif ($scenario -eq 'original-replaced-same-bytes') { $legacy } else { $null }
        if ($null -ne $replace) {
            [byte[]]$bytes = [IO.File]::ReadAllBytes($replace); $creation = [IO.File]::GetCreationTimeUtc($replace)
            [IO.File]::Move($replace,(Join-Path $case 'retained-native-object')); [IO.File]::WriteAllBytes($replace,$bytes); [IO.File]::SetCreationTimeUtc($replace,$creation)
        }
        if ($scenario -eq 'reparse-seal') {
            [IO.File]::Move($sealPath,(Join-Path $case 'retained-seal'))
            $target = Join-Path $case 'retained-target'; New-Item -ItemType Directory -Path $target | Out-Null
            [IO.File]::WriteAllText((Join-Path $target 'foreign.txt'),'synthetic retained target')
            $junction = $sealPath; $junctionOwned = $true
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(5000)) { throw 'Owned restart junction creation exceeded bounded wait.' }
            Check-LegacyRestart ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and
                ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'actual owned seal junction exists and streams settle'
            $process.Dispose(); $process = $null
        }
        $readPrograms = $programs
        if ($scenario -eq 'wrong-location') { $readPrograms = Join-Path $case 'other-Programs'; New-Item -ItemType Directory -Path $readPrograms | Out-Null }
        $backup = $null; $reference = $null; $manager = $null; $writes = $null
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        # Only paths enter recovery; no retained Backup/proof/origin/hash is supplied.
        $recovered = Restore-ProGoInstallerLegacyReferenceEvidence $journal $install $readPrograms $startup
        Check-LegacyRestart (($null -ne $recovered) -eq ($scenario -eq 'complete')) 'fresh disk reconstruction rejects incomplete tampered foreign replaced and redirected evidence'
        if ($scenario -eq 'complete') {
            Check-LegacyRestart (-not $recovered.MutationAllowed -and -not $recovered.FullRollback -and -not $recovered.InstallSucceeded -and $recovered.Source.Hash -ceq $originalHash) 'rehydrated evidence preserves exact bytes binding and denies mutation/success'
            # An actual new Windows PowerShell/CLR process has no parent proof objects or handles.
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = Join-Path $PSHOME 'powershell.exe'
            $info.Arguments = '-NoProfile -NonInteractive -File "' + $PSCommandPath + '" -Child -CaseDirectory "' + $case + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(20000)) { throw 'Owned fresh-process Legacy verification exceeded bounded twenty-second contract.' }
            Check-LegacyRestart ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and
                $outTask.Result.Contains('LEGACY_RESTART_CHILD verified=True; mutationAllowed=False; installSucceeded=False') -and $errTask.Result.Length -eq 0) 'actual independent process rehydrates only verified disk evidence'
            $process.Dispose(); $process = $null
        }
        Check-LegacyRestart (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $before.Count) 'recovery creates or deletes no file'
        foreach ($key in $before.Keys) { Check-LegacyRestart ([IO.File]::Exists($key) -and (Get-FileHash -LiteralPath $key).Hash -ceq $before[$key]) 'recovery preserves every current foreign and private byte' }
        if ($junctionOwned) { [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null }
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-LegacyRestart ([IO.File]::ReadAllText((Join-Path $install $name)) -ceq 'synthetic private bytes') 'private bytes untouched' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy restart ' + $scenario)
    }
    Write-Host ('Legacy restart tests PASS: ' + $scenarios.Count + '; actual fresh-process verification=1; power-loss NOT_RUN')
} finally {
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Owned restart child did not settle during cleanup.' } }
            if ($null -ne $outTask -and $null -ne $errTask -and
                -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Owned restart child streams did not drain during cleanup.' }
        } finally { $process.Dispose() }
    }
    if ($junctionOwned -and $null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne $sealPath -or -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Restart junction cleanup refuses changed destination.' }
        [IO.Directory]::Delete($junction)
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-restart-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Restart cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Restart cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Restart cleanup refuses linked descendants.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
