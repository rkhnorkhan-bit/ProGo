Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
function Check-LegacySource($Condition,$Name) { if (-not $Condition) { throw ('Legacy source capture failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-source-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $source = $null; $held = $null; $junction = $null; $junctionOwned = $false; $process = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $scenarios = @('capture','absent','foreign-target','malformed','busy','cancel-before','changed-before-migration',
        'replaced-identical','foreign-owner','foreign-receipt','legacy-reappeared','replacement-changed',
        'preserved-receipt','copy-isolation','disposed','captured-bytes-corrupt','reparse','oversize')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic executable; never run')
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        if ($scenario -ne 'absent') { $manager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy) }
        if ($scenario -eq 'foreign-target') {
            [IO.File]::Move($legacy,(Join-Path $case 'retained-original.lnk'))
            $otherInstall = Join-Path $case 'other-app'; New-Item -ItemType Directory -Path $otherInstall | Out-Null
            [IO.File]::WriteAllText((Join-Path $otherInstall 'ProGo.exe'),'synthetic foreign executable')
            $other = New-Object ProGo.ApplicationShortcuts($otherInstall,$programs,$startup)
            $other.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
        }
        if ($scenario -eq 'malformed') { [IO.File]::WriteAllText($legacy,'not a Shell Link') }
        if ($scenario -eq 'oversize') { [IO.File]::WriteAllBytes($legacy,(New-Object byte[] 65537)) }
        if ($scenario -eq 'reparse') {
            $target = Join-Path $case 'retained-menu'; [IO.Directory]::Move($menu,$target); $junction = $menu
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            Check-LegacySource (-not [IO.Directory]::Exists($junction) -and -not [IO.File]::Exists($junction)) 'own junction destination starts absent'
            $junctionOwned = $true
            $process = [Diagnostics.Process]::Start($info)
            $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(5000)) { throw 'Owned junction fixture creation exceeded its bounded wait.' }
            Check-LegacySource ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) 'owned junction process streams settle within bounded wait'
            Check-LegacySource ($process.ExitCode -eq 0 -and [IO.Directory]::Exists($junction) -and
                ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'own junction fixture actually exists'
            $process.Dispose(); $process = $null
        }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        if ($scenario -eq 'busy') { $held = [IO.File]::Open($legacy,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
        $cancellation = New-Object Threading.CancellationTokenSource
        if ($scenario -eq 'cancel-before') { $cancellation.Cancel() }
        $refused = $false
        try { $source = $manager.CaptureLegacyRollbackSource($cancellation.Token) } catch {
            if ($scenario -notin @('malformed','busy','cancel-before','reparse','oversize')) { throw }
            $refused = $true
        } finally { $cancellation.Dispose(); if ($null -ne $held) { $held.Dispose(); $held = $null } }
        if ($scenario -eq 'reparse') { [IO.Directory]::Delete($junction); $junctionOwned = $false; $junction = $null }
        if ($scenario -ne 'reparse') {
            Check-LegacySource (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $before.Count) 'capture creates/deletes no file'
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-LegacySource ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'capture itself preserves exact entire case inventory' }
        }
        if ($scenario -in @('absent','foreign-target','malformed','busy','cancel-before','reparse','oversize')) {
            Check-LegacySource ($null -eq $source -and ($refused -eq ($scenario -notin @('absent','foreign-target')))) 'absence/foreign is not owned; invalid/busy/cancel/reparse/oversize refuses without source'
            # The junction was removed only by its fixture owner; its retained target bytes remain untouched.
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-LegacySource ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'refused capture preserves every retained byte' }
        } else {
            Check-LegacySource ($null -ne $source -and $source.InstallIdentity -cmatch '^[A-F0-9]{64}$' -and
                $source.LocationIdentity -cmatch '^[A-F0-9]{64}$' -and $source.OriginIdentity -cmatch '^[A-F0-9]{64}$' -and
                $source.Hash -ceq $before[$legacy]) 'capture binds exact native original object/location/install/bytes'
            [byte[]]$original = [IO.File]::ReadAllBytes($legacy)
            if ($scenario -eq 'changed-before-migration') {
                # A different valid native shortcut avoids conflating byte mismatch with malformed COM input.
                [IO.File]::Move($legacy,(Join-Path $case 'retained-original.lnk'))
                $manager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
            }
            if ($scenario -eq 'replaced-identical') {
                $creation = [IO.File]::GetCreationTimeUtc($legacy)
                [IO.File]::Move($legacy,(Join-Path $case 'retained-original.lnk'))
                [IO.File]::WriteAllBytes($legacy,$original); [IO.File]::SetCreationTimeUtc($legacy,$creation)
            }
            $receipt = $manager.InstallWithStartupEvidence($true,$true,($scenario -eq 'preserved-receipt'))
            if ($scenario -eq 'legacy-reappeared') { [IO.File]::WriteAllBytes($legacy,$original) }
            if ($scenario -eq 'replacement-changed') { [IO.File]::WriteAllText($primary,'synthetic external replacement') }
            $reader = $manager
            if ($scenario -in @('foreign-owner','foreign-receipt')) {
                $otherInstall = Join-Path $case 'other-app'; $otherPrograms = Join-Path $case 'other-Programs'
                New-Item -ItemType Directory -Path $otherInstall,$otherPrograms | Out-Null
                [IO.File]::WriteAllText((Join-Path $otherInstall 'ProGo.exe'),'synthetic foreign executable')
                $other = New-Object ProGo.ApplicationShortcuts($otherInstall,$otherPrograms,$startup)
                if ($scenario -eq 'foreign-owner') { $reader = $other } else { $receipt = $other.InstallWithStartupEvidence($true,$true,$false) }
            }
            if ($scenario -eq 'disposed') { $source.Dispose() }
            if ($scenario -eq 'captured-bytes-corrupt') {
                [byte[]]$retained = $source.GetType().GetField('bytes',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($source)
                $retained[0] = $retained[0] -bxor 255
            }
            $snapshot = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $snapshot[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
            [byte[]]$bytes = $reader.ReadVerifiedLegacyRollbackBytes($source,$receipt.Legacy)
            $accepted = $scenario -in @('capture','copy-isolation')
            Check-LegacySource (($null -ne $bytes) -eq $accepted) 'only exact captured original and real removal receipt release a source copy'
            if ($accepted) {
                Check-LegacySource ([Convert]::ToBase64String($bytes) -ceq [Convert]::ToBase64String($original) -and
                    $receipt.Legacy.OriginIdentity -ceq $source.OriginIdentity -and $receipt.Legacy.BeforeHash -ceq $source.Hash) 'actual returned bytes equal original and receipt links full native origin/hash'
                $bytes[0] = $bytes[0] -bxor 255
                [byte[]]$again = $manager.ReadVerifiedLegacyRollbackBytes($source,$receipt.Legacy)
                Check-LegacySource ([Convert]::ToBase64String($again) -ceq [Convert]::ToBase64String($original)) 'caller mutation cannot alter retained capture'
            }
            Check-LegacySource (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $snapshot.Count) 'source verification creates/deletes no file'
            foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { Check-LegacySource ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $snapshot[$file.FullName]) 'verification preserves all runtime/private/foreign/current bytes' }
            $source.Dispose(); $source = $null
        }
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-LegacySource ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign content is untouched' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Write-Host ('PASS: installer Legacy source capture ' + $scenario)
    }
    Write-Host ('Installer Legacy source capture tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $source) { $source.Dispose() }
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $process) { try { if (-not $process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) } } finally { $process.Dispose() } }
    if ($junctionOwned -and $null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne (Join-Path $case 'Programs\ProGo') -or
            -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Junction cleanup refuses changed fixture destination.' }
        [IO.Directory]::Delete($junction)
    }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-source-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Legacy source cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Legacy source cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Legacy source cleanup refuses linked descendants.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
