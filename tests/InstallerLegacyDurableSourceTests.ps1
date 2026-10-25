Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
function Check-DurableLegacy($Condition,$Name) { if (-not $Condition) { throw ('Durable Legacy source failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-durable-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $source = $null; $held = $null; $junction = $null; $junctionOwned = $false; $process = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $scenarios = @('complete','source-disposed','destination-exists','partial-existing','partial-after','payload-tampered',
        'metadata-tampered','foreign-storage-same-bytes','wrong-operation','wrong-snapshot','wrong-install','wrong-receipt',
        'original-replaced-before','original-changed-before','legacy-reappeared','replacement-changed','missing-proof','busy-read','cancel-before','redirected-backups')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $backups = Join-Path $install 'backups'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup,$backups | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic executable; never run')
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $name),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
        [byte[]]$original = [IO.File]::ReadAllBytes($legacy); $originalHash = (Get-FileHash -LiteralPath $legacy).Hash
        $source = $manager.CaptureLegacyRollbackSource([Threading.CancellationToken]::None)
        Check-DurableLegacy ($null -ne $source) 'actual owned source captured before mutation'
        [guid]$operation = [Guid]::NewGuid(); [string]$snapshot = 'A' * 64
        $path = Join-Path $backups ('installer-legacy-source-' + $operation.ToString('N') + '.bin')
        if ($scenario -eq 'source-disposed') { $source.Dispose() }
        if ($scenario -in @('destination-exists','partial-existing')) { [IO.File]::WriteAllText($path,$(if ($scenario -eq 'partial-existing') { 'PROGO-LEGACY-SOURCE-1' } else { 'synthetic foreign source' })) }
        if ($scenario -eq 'original-replaced-before') {
            $creation = [IO.File]::GetCreationTimeUtc($legacy); [IO.File]::Move($legacy,(Join-Path $case 'retained-original.lnk'))
            [IO.File]::WriteAllBytes($legacy,$original); [IO.File]::SetCreationTimeUtc($legacy,$creation)
        }
        if ($scenario -eq 'original-changed-before') { [IO.File]::WriteAllText($legacy,'synthetic external bytes') }
        if ($scenario -eq 'redirected-backups') {
            $target = Join-Path $case 'retained-backups'; [IO.Directory]::Move($backups,$target); $junction = $backups
            Check-DurableLegacy (-not [IO.Directory]::Exists($junction)) 'junction starts at absent own location'
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $junctionOwned = $true; $process = [Diagnostics.Process]::Start($info)
            $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(5000)) { throw 'Owned durable fixture junction creation exceeded bounded wait.' }
            Check-DurableLegacy ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) 'owned junction streams settle'
            Check-DurableLegacy ($process.ExitCode -eq 0 -and ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'real own junction exists'
            $process.Dispose(); $process = $null
        }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $cancel = New-Object Threading.CancellationTokenSource
        if ($scenario -eq 'cancel-before') { $cancel.Cancel() }
        $proof = $null; $refused = $false
        try { $proof = $manager.PersistLegacyRollbackSource($source,$operation,$snapshot,$cancel.Token) } catch {
            if ($scenario -notin @('source-disposed','destination-exists','partial-existing','original-replaced-before','original-changed-before','cancel-before','redirected-backups')) { throw }
            $refused = $true
        } finally { $cancel.Dispose(); $source.Dispose(); $source = $null }
        if ($scenario -eq 'redirected-backups') { [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null }
        if ($refused) {
            Check-DurableLegacy ($null -eq $proof) 'failed/interrupted/foreign source yields no durable proof'
            if ($scenario -notin @('destination-exists','partial-existing')) { Check-DurableLegacy (-not [IO.File]::Exists($path)) 'refused admission creates no destination' }
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-DurableLegacy ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'refusal preserves all retained bytes including partial/foreign destination' }
        } else {
            Check-DurableLegacy ($null -ne $proof -and $proof.Operation -eq $operation -and $proof.SnapshotHash -ceq $snapshot -and
                -not $proof.MutationAllowed -and -not $proof.FullRollback -and -not $proof.InstallSucceeded -and
                $proof.Hash -ceq $originalHash -and $proof.DocumentHash -ceq (Get-FileHash -LiteralPath $path).Hash -and
                $proof.StorageOrigin -cmatch '^[A-F0-9]{64}$') 'durable evidence binds exact original and actual new file identity/hash'
            Check-DurableLegacy ((Get-FileHash -LiteralPath $legacy).Hash -ceq $originalHash) 'durable capture never deletes or changes original'
            foreach ($key in $before.Keys) { Check-DurableLegacy ((Get-FileHash -LiteralPath $key).Hash -ceq $before[$key]) 'source publication changes no existing byte' }
            $receipt = $manager.InstallWithStartupEvidence($true,$true,$false)
            Check-DurableLegacy ($receipt.Legacy.Disposition -ceq 'removed' -and -not [IO.File]::Exists($legacy)) 'existing migration produces actual removed receipt'
            if ($scenario -eq 'partial-after') { [IO.File]::WriteAllText($path,'PROGO-LEGACY-SOURCE-1') }
            if ($scenario -in @('payload-tampered','metadata-tampered')) {
                [byte[]]$changed = [IO.File]::ReadAllBytes($path); $at = if ($scenario -eq 'payload-tampered') { $changed.Length - 1 } else { 0 }
                $changed[$at] = $changed[$at] -bxor 255; [IO.File]::WriteAllBytes($path,$changed)
            }
            if ($scenario -eq 'foreign-storage-same-bytes') {
                [byte[]]$doc = [IO.File]::ReadAllBytes($path); $creation = [IO.File]::GetCreationTimeUtc($path)
                [IO.File]::Move($path,(Join-Path $case 'retained-source.bin')); [IO.File]::WriteAllBytes($path,$doc); [IO.File]::SetCreationTimeUtc($path,$creation)
            }
            if ($scenario -eq 'legacy-reappeared') { [IO.File]::WriteAllBytes($legacy,$original) }
            if ($scenario -eq 'replacement-changed') { [IO.File]::WriteAllText($primary,'synthetic external replacement') }
            $reader = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
            [guid]$readOperation = $operation; [string]$readSnapshot = $snapshot; $readProof = $proof
            if ($scenario -eq 'wrong-operation') { $readOperation = [Guid]::NewGuid() }
            if ($scenario -eq 'wrong-snapshot') { $readSnapshot = 'B' * 64 }
            if ($scenario -eq 'missing-proof') { $readProof = $null }
            if ($scenario -in @('wrong-install','wrong-receipt')) {
                $otherInstall = Join-Path $case 'other-app'; $otherPrograms = Join-Path $case 'other-Programs'
                New-Item -ItemType Directory -Path $otherInstall,$otherPrograms | Out-Null; [IO.File]::WriteAllText((Join-Path $otherInstall 'ProGo.exe'),'synthetic other executable')
                $other = New-Object ProGo.ApplicationShortcuts($otherInstall,$otherPrograms,$startup)
                if ($scenario -eq 'wrong-install') { $reader = $other } else { $receipt = $other.InstallWithStartupEvidence($true,$true,$false) }
            }
            $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
            if ($scenario -eq 'busy-read') { $held = [IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
            try { [byte[]]$bytes = $reader.ReadDurableLegacyRollbackBytes($readProof,$readOperation,$readSnapshot,$receipt.Legacy) }
            finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
            Check-DurableLegacy (($null -ne $bytes) -eq ($scenario -eq 'complete')) 'only complete exact durable source with trusted matching proof releases bytes'
            if ($scenario -eq 'complete') {
                Check-DurableLegacy ([Convert]::ToBase64String($bytes) -ceq [Convert]::ToBase64String($original)) 'new manager reads exact original bytes from disk after capture disposed'
                $bytes[0] = $bytes[0] -bxor 255
                [byte[]]$again = $reader.ReadDurableLegacyRollbackBytes($proof,$operation,$snapshot,$receipt.Legacy)
                Check-DurableLegacy ([Convert]::ToBase64String($again) -ceq [Convert]::ToBase64String($original)) 'caller copy mutation cannot change durable source'
            }
            Check-DurableLegacy (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'reader creates/deletes no object'
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-DurableLegacy ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $inventory[$file.FullName]) 'reader preserves every private/foreign/current/partial byte' }
        }
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-DurableLegacy ([IO.File]::ReadAllText((Join-Path $install $name)) -ceq 'synthetic private bytes') 'private content untouched' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Write-Host ('PASS: installer durable Legacy source ' + $scenario)
    }
    Write-Host ('Installer durable Legacy source tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $source) { $source.Dispose() }; if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $process) { try { if (-not $process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) } } finally { $process.Dispose() } }
    if ($junctionOwned -and $null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne (Join-Path $case 'app\backups') -or -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Durable fixture junction cleanup refuses changed destination.' }
        [IO.Directory]::Delete($junction)
    }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-durable-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Durable fixture cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Durable fixture cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Durable fixture cleanup refuses linked descendants.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
