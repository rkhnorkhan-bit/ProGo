# Supplemental Windows native tests; registered once in Installer Recovery after the original 32 suites.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
function Check-Input($Condition, [string]$Name) {
    if (-not $Condition) { throw ('Durable input failed: case=' + $scenario + '; check=' + $Name) }
}
function Get-InputInventory([string]$Directory) {
    $result = @{}
    foreach ($file in Get-ChildItem -LiteralPath $Directory -Recurse -File) { $result[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    return $result
}
function Check-InputInventory([string]$Directory, $Before) {
    $after = Get-InputInventory $Directory
    Check-Input ($after.Count -eq $Before.Count) 'inventory count unchanged'
    foreach ($key in $Before.Keys) { Check-Input ($after.ContainsKey($key) -and $after[$key] -ceq $Before[$key]) 'every retained byte unchanged' }
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-durable-input-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $source = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $scenarios = @('read-oversize','read-empty','read-trailing-data',
        'write-empty-operation','write-null-snapshot','write-short-snapshot','write-nonhex-snapshot','write-lowercase-snapshot','write-null-source',
        'read-empty-operation','read-null-snapshot','read-nonhex-snapshot')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $backups = Join-Path $install 'backups'
        New-Item -ItemType Directory -Path $install,$menu,$startup,$backups | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic executable; never run')
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $name),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false)
        $legacy = Join-Path $menu 'ProGo Status.lnk'
        [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        [byte[]]$original = [IO.File]::ReadAllBytes($legacy)
        $source = $manager.CaptureLegacyRollbackSource([Threading.CancellationToken]::None)
        Check-Input ($null -ne $source) 'real owned original captured'
        [guid]$operation = [Guid]::NewGuid(); [string]$snapshot = 'A' * 64
        $path = Join-Path $backups ('installer-legacy-source-' + $operation.ToString('N') + '.bin')
        if ($scenario.StartsWith('write-')) {
            [guid]$badOperation = $operation; $badSnapshot = $snapshot; $badSource = $source
            switch ($scenario) {
                'write-empty-operation' { $badOperation = [Guid]::Empty }
                'write-null-snapshot' { $badSnapshot = $null }
                'write-short-snapshot' { $badSnapshot = 'A' * 63 }
                'write-nonhex-snapshot' { $badSnapshot = 'G' * 64 }
                'write-lowercase-snapshot' { $badSnapshot = 'a' * 64 }
                'write-null-source' { $badSource = $null }
            }
            $before = Get-InputInventory $case; $refused = $false; $badProof = $null
            try { $badProof = $manager.PersistLegacyRollbackSource($badSource,$badOperation,$badSnapshot,[Threading.CancellationToken]::None) }
            catch {
                # Do not accept an unrelated COM/reflection/fixture failure as the expected API refusal.
                $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                if ($cause -isnot [IO.IOException]) { throw }
                $refused = $true
            }
            Check-Input ($refused -and $null -eq $badProof) 'invalid writer input refuses with no evidence'
            Check-InputInventory $case $before
        }
        # Positive control in EVERY case, after invalid writer admission where applicable.
        $proof = $manager.PersistLegacyRollbackSource($source,$operation,$snapshot,[Threading.CancellationToken]::None)
        $source.Dispose(); $source = $null
        $receipt = $manager.InstallWithStartupEvidence($true,$true,$false)
        Check-Input ($receipt.Legacy.Disposition -ceq 'removed') 'real removed receipt'
        $reader = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        [byte[]]$control = $reader.ReadDurableLegacyRollbackBytes($proof,$operation,$snapshot,$receipt.Legacy)
        Check-Input ($null -ne $control -and [Convert]::ToBase64String($control) -ceq [Convert]::ToBase64String($original)) 'positive control exact original bytes'
        switch ($scenario) {
            'read-oversize' { [IO.File]::WriteAllBytes($path,(New-Object byte[] 70001)) }
            'read-empty' { [IO.File]::WriteAllBytes($path,[byte[]]@()) }
            'read-trailing-data' {
                $stream = [IO.File]::Open($path,[IO.FileMode]::Append,[IO.FileAccess]::Write,[IO.FileShare]::None)
                try { $stream.WriteByte(1) } finally { $stream.Dispose() }
            }
        }
        [guid]$readOperation = $operation; $readSnapshot = $snapshot
        switch ($scenario) {
            'read-empty-operation' { $readOperation = [Guid]::Empty }
            'read-null-snapshot' { $readSnapshot = $null }
            'read-nonhex-snapshot' { $readSnapshot = 'G' * 64 }
        }
        $beforeRead = Get-InputInventory $case
        [byte[]]$result = $reader.ReadDurableLegacyRollbackBytes($proof,$readOperation,$readSnapshot,$receipt.Legacy)
        if ($scenario.StartsWith('read-')) { Check-Input ($null -eq $result) 'malformed document or input releases no bytes' }
        else { Check-Input ($null -ne $result -and [Convert]::ToBase64String($result) -ceq [Convert]::ToBase64String($original)) 'valid request remains readable after rejected writer input' }
        Check-InputInventory $case $beforeRead
        Check-Input (-not $proof.MutationAllowed -and -not $proof.FullRollback -and -not $proof.InstallSucceeded) 'proof grants no mutation or terminal authority'
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { Check-Input ([IO.File]::ReadAllText((Join-Path $install $name)) -ceq 'synthetic private bytes') 'private bytes preserved' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) {
            $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            $probe.Dispose()
        }
        Write-Host ('PASS: durable input ' + $scenario)
    }
    & (Join-Path $Root 'tests\InstallerLegacyJournalReferenceTests.ps1')
    & (Join-Path $Root 'tests\InstallerLegacyRestartTests.ps1')
    & (Join-Path $Root 'tests\InstallerLegacyPreDeleteTests.ps1')
    Write-Host ('Durable input tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $source) { $source.Dispose() }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-durable-input-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Input fixture cleanup refuses unowned root.' }
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Input fixture cleanup refuses linked root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Input fixture cleanup refuses linked descendant.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
