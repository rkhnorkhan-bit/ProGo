Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Legacy reference helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-LegacyReference($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Legacy journal reference failed: case=' + $scenario + '; check=' + $Name) }
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-reference-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $stage = $null; $cancel = $null; $junction = $null; $junctionOwned = $false; $process = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('complete','caller-snapshot','snapshot-before','journal-before','foreign-journal','source-exists','reference-exists','cancel-before',
        'missing-reference','partial-reference','reference-tampered','reference-replaced-same-bytes','source-tampered','source-replaced-same-bytes',
        'original-replaced-same-bytes','journal-after','snapshot-after','wrong-location','runtime-after','reference-reparse')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        if ($junctionOwned) {
            Check-LegacyReference ([IO.File]::ReadAllText((Join-Path $target 'foreign.txt')) -ceq 'synthetic retained target') 'reparse target bytes untouched'
            [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null
        }
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $name),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        $originalHash = (Get-FileHash -LiteralPath $legacy).Hash
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $suffix = [IO.Path]::GetFileName($backup.Journal.Directory).Substring('installer-journal-'.Length)
        $parent = Join-Path $install 'backups'; $sourcePath = Join-Path $parent ('installer-legacy-source-' + $suffix + '.bin')
        $referencePath = Join-Path $parent ('installer-legacy-reference-' + $suffix + '.json')
        $recordPath = Join-Path $backup.Journal.Directory 'admit-000000.json'
        if ($scenario -eq 'caller-snapshot') { $backup.ManifestHash = 'B' * 64 }
        if ($scenario -eq 'snapshot-before') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic corrupt snapshot') }
        if ($scenario -eq 'journal-before') { [IO.File]::WriteAllText($recordPath,'{}') }
        if ($scenario -eq 'foreign-journal') { $backup.Journal.Directory = Join-Path $case ('installer-journal-' + $suffix) }
        if ($scenario -eq 'source-exists') { [IO.File]::WriteAllText($sourcePath,'synthetic foreign source') }
        if ($scenario -eq 'reference-exists') { [IO.File]::WriteAllText($referencePath,'synthetic foreign reference') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $cancel = New-Object Threading.CancellationTokenSource
        if ($scenario -eq 'cancel-before') { $cancel.Cancel() }
        $reference = $null; $failed = $false
        try { $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup $cancel.Token } catch { $failed = $true }
        finally { $cancel.Dispose(); $cancel = $null }
        $expected = $scenario -notin @('caller-snapshot','snapshot-before','journal-before','foreign-journal','source-exists','reference-exists','cancel-before')
        Check-LegacyReference (($null -ne $reference) -eq $expected -and $failed -ne $expected) 'only verified snapshot and actual canonical journal can bind source'
        foreach ($key in $before.Keys) { Check-LegacyReference ([IO.File]::Exists($key) -and (Get-FileHash -LiteralPath $key).Hash -ceq $before[$key]) 'publication never changes retained or foreign bytes' }
        Check-LegacyReference ((Get-FileHash -LiteralPath $legacy).Hash -ceq $originalHash) 'reference never deletes or changes Legacy'
        if ($expected) {
            Check-LegacyReference (Test-ProGoInstallerLegacySourceReference $backup $reference $programs $startup) 'fresh independent reader verifies native source and trusted journal'
            Check-LegacyReference (-not $reference.MutationAllowed -and -not $reference.FullRollback -and -not $reference.InstallSucceeded) 'reference grants no restore or success authority'
            $saved = [IO.File]::ReadAllText($referencePath) | ConvertFrom-Json
            Check-LegacyReference ($saved.Context.SnapshotHash -ceq $backup.ManifestHash -and $saved.Context.Journal -ceq ('installer-journal-' + $suffix) -and
                $saved.Source.StorageOrigin -ceq $reference.Source.StorageOrigin -and $saved.Source.DocumentHash -ceq $reference.Source.DocumentHash -and
                $saved.Source.OriginIdentity -ceq $reference.Source.OriginIdentity -and $saved.Source.Hash -ceq $originalHash -and
                @($saved.Context.Records).Count -eq $backup.Entries.Count) 'durable canonical document binds real full journal snapshot original and source native origin/hash'
            if ($scenario -eq 'complete') {
                $retryFailed = $false; try { New-ProGoInstallerLegacySourceReference $backup $programs $startup | Out-Null } catch { $retryFailed = $true }
                Check-LegacyReference $retryFailed 'same operation never reuses or overwrites reference/source'
            }
            switch ($scenario) {
                'missing-reference' { [IO.File]::Move($referencePath,(Join-Path $case 'retained-reference.json')) }
                'partial-reference' { [IO.File]::WriteAllText($referencePath,'{') }
                'reference-tampered' { [IO.File]::AppendAllText($referencePath,' ') }
                'source-tampered' { [IO.File]::WriteAllText($sourcePath,'synthetic corrupt source') }
                'journal-after' { [IO.File]::WriteAllText($recordPath,'{}') }
                'runtime-after' { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic changed runtime') }
                'snapshot-after' { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic corrupt snapshot') }
            }
            $replace = if ($scenario -eq 'reference-replaced-same-bytes') { $referencePath }
                elseif ($scenario -eq 'source-replaced-same-bytes') { $sourcePath }
                elseif ($scenario -eq 'original-replaced-same-bytes') { $legacy } else { $null }
            if ($null -ne $replace) {
                [byte[]]$bytes = [IO.File]::ReadAllBytes($replace); $creation = [IO.File]::GetCreationTimeUtc($replace)
                [IO.File]::Move($replace,(Join-Path $case 'retained-native-object'))
                [IO.File]::WriteAllBytes($replace,$bytes); [IO.File]::SetCreationTimeUtc($replace,$creation)
            }
            if ($scenario -eq 'reference-reparse') {
                [IO.File]::Move($referencePath,(Join-Path $case 'retained-reference.json'))
                $target = Join-Path $case 'retained-target'; New-Item -ItemType Directory -Path $target | Out-Null
                [IO.File]::WriteAllText((Join-Path $target 'foreign.txt'),'synthetic retained target')
                $junction = $referencePath; $junctionOwned = $true
                $info = New-Object Diagnostics.ProcessStartInfo
                $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
                $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
                $process = [Diagnostics.Process]::Start($info)
                $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
                if (-not $process.WaitForExit(5000)) { throw 'Owned reference junction creation exceeded bounded wait.' }
                Check-LegacyReference ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) 'owned junction streams settle'
                Check-LegacyReference ($process.ExitCode -eq 0 -and ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'actual own reference junction exists'
                $process.Dispose(); $process = $null
            }
            $readPrograms = $programs
            if ($scenario -eq 'wrong-location') { $readPrograms = Join-Path $case 'other-Programs'; New-Item -ItemType Directory -Path $readPrograms | Out-Null }
            $readBefore = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $readBefore[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
            Check-LegacyReference ((Test-ProGoInstallerLegacySourceReference $backup $reference $readPrograms $startup) -eq ($scenario -eq 'complete')) 'mutated partial missing foreign or replaced proof fails closed'
            Check-LegacyReference (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $readBefore.Count) 'read-only verifier creates/deletes nothing'
            foreach ($key in $readBefore.Keys) { Check-LegacyReference ([IO.File]::Exists($key) -and (Get-FileHash -LiteralPath $key).Hash -ceq $readBefore[$key]) 'read-only verifier preserves external mutations' }
        }
        if ($junctionOwned) {
            Check-LegacyReference ([IO.File]::ReadAllText((Join-Path $target 'foreign.txt')) -ceq 'synthetic retained target') 'reparse target bytes untouched'
            [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null
        }
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-LegacyReference ([IO.File]::ReadAllText((Join-Path $install $name)) -ceq 'synthetic private bytes') 'private resources untouched' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy journal reference ' + $scenario)
    }
    Write-Host ('Legacy journal reference tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $process) { try { if (-not $process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) } } finally { $process.Dispose() } }
    if ($junctionOwned -and $null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne $referencePath -or -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Reference junction cleanup refuses changed destination.' }
        [IO.Directory]::Delete($junction)
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-reference-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Reference fixture cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reference fixture cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reference fixture cleanup refuses linked descendants.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
