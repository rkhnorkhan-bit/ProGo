param([switch]$Child,[string]$CaseDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Legacy admission helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
. (Join-Path $PSScriptRoot 'InstallerLegacyDeletionPinsTests.ps1')
. (Join-Path $PSScriptRoot 'InstallerLegacyPhysicalPinsTests.ps1')
. (Join-Path $PSScriptRoot 'InstallerLegacyNamespaceTests.ps1')
if ($Child) {
    $parent = [IO.Path]::GetDirectoryName($CaseDirectory)
    if ([IO.Path]::GetFileName($CaseDirectory) -cne 'complete' -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-legacy-predelete-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Admission child refuses unowned scope.' }
    $install = Join-Path $CaseDirectory 'app'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1 -or (Get-ProGoInstallerLegacyPreDeleteState $journals[0].FullName $install (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup')) -cne 'not-applied') { throw 'Fresh-process admission state is not verified.' }
    $pins = Open-ProGoInstallerLegacyDeletionPins $journals[0].FullName $install (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup')
    try { if ($null -eq $pins -or -not $pins.IsPinned -or $pins.MutationAllowed) { throw 'Fresh-process pin gate is not verified.' } }
    finally { if ($null -ne $pins) { $pins.Dispose() } }
    $physical = Open-ProGoInstallerLegacyPhysicalPins $journals[0].FullName $install (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup')
    try { if ($null -eq $physical -or -not $physical.VerifyPinned([Threading.CancellationToken]::None) -or $physical.MutationAllowed) { throw 'Fresh physical pins are not verified.' } }
    finally { if ($null -ne $physical) { $physical.Dispose() } }
    if ((Get-ProGoInstallerLegacyBoundaryObservation $journals[0].FullName $install (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup')) -cne 'observed-only') { throw 'Fresh namespace observation is blocked.' }
    Write-Host 'LEGACY_NAMESPACE_CHILD observedOnly=True; mutationPerformed=False'
    Write-Host 'LEGACY_PHYSICAL_CHILD verified=True; mutationPerformed=False'
    Write-Host 'LEGACY_PINS_CHILD verified=True; mutationPerformed=False'
    Write-Host 'LEGACY_ADMISSION_CHILD state=not-applied; mutationPerformed=False'
    return
}
function Check-PreDelete($Condition,[string]$Name) { if (-not $Condition) { throw ('Legacy pre-delete failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-predelete-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $stage = $null; $process = $null; $cancel = $null; $junction = $null; $junctionOwned = $false; $outTask = $null; $errTask = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('complete','no-seal-before','snapshot-before','journal-before','runtime-before','foreign-location-before','conflict','cancel-before',
        'partial-after','tampered-after','admission-replaced-same-bytes','seal-tampered-after','seal-replaced-same-bytes','reference-replaced-same-bytes',
        'source-replaced-same-bytes','original-replaced-same-bytes','original-absent','snapshot-after','journal-after','wrong-location-after',
        'reparse-admission','missing-admission','foreign-admission')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        $originalHash = (Get-FileHash -LiteralPath $legacy).Hash
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        if ($scenario -ne 'no-seal-before') { Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup }
        $journal = $backup.Journal.Directory; $suffix = [IO.Path]::GetFileName($journal).Substring('installer-journal-'.Length); $parent = Join-Path $install 'backups'
        $admissionPath = Join-Path $parent ('installer-legacy-admission-' + $suffix + '.bin')
        $sealPath = Join-Path $parent ('installer-legacy-seal-' + $suffix + '.bin')
        $referencePath = Join-Path $parent ('installer-legacy-reference-' + $suffix + '.json')
        $sourcePath = Join-Path $parent ('installer-legacy-source-' + $suffix + '.bin')
        $recordPath = Join-Path $journal 'admit-000000.json'
        $readPrograms = $programs
        if ($scenario -eq 'snapshot-before') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic corrupt snapshot') }
        if ($scenario -eq 'journal-before') { [IO.File]::WriteAllText($recordPath,'{}') }
        if ($scenario -eq 'runtime-before') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic changed runtime') }
        if ($scenario -eq 'foreign-location-before') { $readPrograms = Join-Path $case 'other-Programs'; New-Item -ItemType Directory -Path $readPrograms | Out-Null }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText($admissionPath,'synthetic foreign admission') }
        if ($scenario -eq 'foreign-admission') { [IO.File]::WriteAllBytes($admissionPath,$foreignAdmission) }
        Check-PreDelete ((Get-ProGoInstallerLegacyPreDeleteState $journal $install $readPrograms $startup) -ceq 'blocked') 'no verified admission means blocked even when source is sealed'
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $cancel = New-Object Threading.CancellationTokenSource; if ($scenario -eq 'cancel-before') { $cancel.Cancel() }
        $admission = $null; $failed = $false
        try { $admission = New-ProGoInstallerLegacyPreDeleteAdmission $backup $readPrograms $startup $cancel.Token } catch {
            if ($scenario -eq 'cancel-before') {
                $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                if ($cause -isnot [OperationCanceledException]) { throw }
            }
            $failed = $true
        }
        finally { $cancel.Dispose(); $cancel = $null }
        $expected = $scenario -notin @('no-seal-before','snapshot-before','journal-before','runtime-before','foreign-location-before','conflict','cancel-before','foreign-admission')
        Check-PreDelete (($null -ne $admission) -eq $expected -and $failed -ne $expected) 'only freshly verified context publishes new durable intent'
        Check-PreDelete (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq ($before.Count + [int]$expected)) 'only own new admission is created on verified publication'
        foreach ($key in $before.Keys) { Check-PreDelete ([IO.File]::Exists($key) -and (Get-FileHash -LiteralPath $key).Hash -ceq $before[$key]) 'publication preserves every existing or foreign byte' }
        Check-PreDelete ((Get-FileHash -LiteralPath $legacy).Hash -ceq $originalHash) 'admission does not delete or mutate Legacy'
        if ($expected) {
            Check-PreDelete ($admission.State -ceq 'not-applied' -and -not $admission.MutationAllowed -and -not $admission.FullRollback -and -not $admission.InstallSucceeded) 'durable intent grants no replay restore or success authority'
            if ($scenario -eq 'complete') {
                [byte[]]$foreignAdmission = [IO.File]::ReadAllBytes($admissionPath)
                $repeatFailed = $false; try { New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup | Out-Null } catch { $repeatFailed = $true }
                Check-PreDelete ($repeatFailed -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($admissionPath)) -ceq [Convert]::ToBase64String($foreignAdmission)) 'same operation never overwrites or reuses admission'
            }
            switch ($scenario) {
                'partial-after' { [IO.File]::WriteAllText($admissionPath,'PROGO-LEGACY-PREDELETE-1') }
                'tampered-after' { [IO.File]::AppendAllText($admissionPath,'x') }
                'seal-tampered-after' { [IO.File]::AppendAllText($sealPath,'x') }
                'original-absent' { [IO.File]::Move($legacy,(Join-Path $case 'retained-original')) }
                'snapshot-after' { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic changed snapshot') }
                'journal-after' { [IO.File]::WriteAllText($recordPath,'{}') }
                'missing-admission' { [IO.File]::Move($admissionPath,(Join-Path $case 'retained-admission')) }
            }
            $replace = if ($scenario -eq 'admission-replaced-same-bytes') { $admissionPath } elseif ($scenario -eq 'seal-replaced-same-bytes') { $sealPath }
                elseif ($scenario -eq 'reference-replaced-same-bytes') { $referencePath } elseif ($scenario -eq 'source-replaced-same-bytes') { $sourcePath }
                elseif ($scenario -eq 'original-replaced-same-bytes') { $legacy } else { $null }
            if ($null -ne $replace) {
                [byte[]]$bytes = [IO.File]::ReadAllBytes($replace); $creation = [IO.File]::GetCreationTimeUtc($replace)
                [IO.File]::Move($replace,(Join-Path $case 'retained-native-object')); [IO.File]::WriteAllBytes($replace,$bytes); [IO.File]::SetCreationTimeUtc($replace,$creation)
            }
            if ($scenario -eq 'wrong-location-after') { $readPrograms = Join-Path $case 'other-Programs'; New-Item -ItemType Directory -Path $readPrograms | Out-Null }
            if ($scenario -eq 'reparse-admission') {
                [IO.File]::Move($admissionPath,(Join-Path $case 'retained-admission'))
                $target = Join-Path $case 'retained-target'; New-Item -ItemType Directory -Path $target | Out-Null
                [IO.File]::WriteAllText((Join-Path $target 'foreign.txt'),'synthetic retained target')
                $junction = $admissionPath; $junctionOwned = $true
                $info = New-Object Diagnostics.ProcessStartInfo
                $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
                $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
                $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
                if (-not $process.WaitForExit(5000)) { throw 'Owned admission junction creation exceeded bounded wait.' }
                Check-PreDelete ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and
                    ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'real own admission junction and bounded streams'
                $process.Dispose(); $process = $null
            }
        }
        Test-ProGoLegacyDeletionPinsFixture $scenario $journal $install $readPrograms $startup @($legacy,$sourcePath,$referencePath,$sealPath,$admissionPath)
        Test-ProGoLegacyPhysicalPinsFixture $scenario $journal $install $readPrograms $startup $backup.Directory
        Test-ProGoLegacyNamespaceFixture $scenario $journal $install $readPrograms $startup $backup.Directory
        $backup = $null; $reference = $null; $admission = $null; $manager = $null; $writes = $null
        $readBefore = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $readBefore[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        Check-PreDelete ((Get-ProGoInstallerLegacyPreDeleteState $journal $install $readPrograms $startup) -ceq $(if ($scenario -eq 'complete') { 'not-applied' } else { 'blocked' })) 'restart classifier refuses intent plus absence or changed evidence without any replay'
        if ($scenario -eq 'complete') {
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = Join-Path $PSHOME 'powershell.exe'; $info.Arguments = '-NoProfile -NonInteractive -File "' + $PSCommandPath + '" -Child -CaseDirectory "' + $case + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(20000)) { throw 'Owned admission restart check exceeded bounded twenty-second contract.' }
            Check-PreDelete ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and
                $outTask.Result.Contains('LEGACY_ADMISSION_CHILD state=not-applied; mutationPerformed=False') -and $outTask.Result.Contains('LEGACY_PINS_CHILD verified=True; mutationPerformed=False') -and $outTask.Result.Contains('LEGACY_PHYSICAL_CHILD verified=True; mutationPerformed=False') -and $outTask.Result.Contains('LEGACY_NAMESPACE_CHILD observedOnly=True; mutationPerformed=False') -and $errTask.Result.Length -eq 0) 'actual fresh process verifies admission without memory witnesses or mutation'
            $process.Dispose(); $process = $null
        }
        Check-PreDelete (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $readBefore.Count) 'restart read creates or deletes no object'
        foreach ($key in $readBefore.Keys) { Check-PreDelete ([IO.File]::Exists($key) -and (Get-FileHash -LiteralPath $key).Hash -ceq $readBefore[$key]) 'restart read preserves all actual private and foreign bytes' }
        if ($junctionOwned) { [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null }
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-PreDelete ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private bytes untouched' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy pre-delete ' + $scenario)
    }
    Check-PreDelete ($script:LegacyNamespaceChecks -eq 41) 'all namespace observations executed'
    Write-Host ('Legacy namespace tests PASS: ' + $script:LegacyNamespaceChecks + '; fresh-process verification=1; namespace-reservation NOT_PROVIDED; deletion/power-loss NOT_RUN')
    Check-PreDelete ($script:LegacyPhysicalChecks -eq 50) 'all physical pin checks executed'
    Write-Host ('Legacy physical pins tests PASS: ' + $script:LegacyPhysicalChecks + '; fresh-process verification=1; deletion/power-loss NOT_RUN')
    Check-PreDelete ($script:LegacyPinChecks -eq 39) 'all 39 pin checks completed without dropping old cases'
    Write-Host ('Legacy pin gate tests PASS: ' + $script:LegacyPinChecks + '; fresh-process verification=1; deletion/power-loss NOT_RUN')
    Write-Host ('Legacy pre-delete tests PASS: ' + $scenarios.Count + '; fresh-process verification=1; deletion/power-loss NOT_RUN')
} finally {
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Owned admission child did not settle during cleanup.' } }
            if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Owned admission streams did not drain.' }
        } finally { $process.Dispose() }
    }
    if ($junctionOwned -and $null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne $admissionPath -or -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Admission junction cleanup refuses changed destination.' }
        [IO.Directory]::Delete($junction)
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-predelete-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Admission cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Admission cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Admission cleanup refuses linked descendant.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
