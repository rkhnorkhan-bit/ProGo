Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Legacy rollback admission helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-LegacyAdmission($Condition,$Name) { if (-not $Condition) { throw ('Legacy rollback admission failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-admission-' + [Guid]::NewGuid().ToString('N'))
$owned = $false; $stage = $null; $held = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('absent-preserved','custom-preserved','removed','post-startup','missing-menu-result','tampered-menu-result',
        'missing-origin','wrong-origin','same-bytes-replaced','legacy-appeared','legacy-changed','snapshot-changed',
        'runtime-changed','wrong-location','receipt-busy','admission-tampered','unknown-receipt-field','wrong-replacement','foreign-admission')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $primary = Join-Path $programs 'ProGo\ProGo.lnk'; $legacy = Join-Path $programs 'ProGo\ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$programs,$startup,(Join-Path $programs 'ProGo') | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf),'synthetic private bytes') }
        if ($scenario -in @('removed','post-startup','wrong-replacement','missing-origin','wrong-origin')) {
            $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
            $manager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
        }
        $customHash = $null
        if ($scenario -in @('custom-preserved','same-bytes-replaced','legacy-changed')) {
            # A customized shortcut must still be a real Shell Link for the native semantic oracle.
            $foreignInstall = Join-Path $case 'foreign-app'
            New-Item -ItemType Directory -Path $foreignInstall | Out-Null
            [IO.File]::WriteAllText((Join-Path $foreignInstall 'ProGo.exe'),'synthetic foreign executable')
            $customManager = New-Object ProGo.ApplicationShortcuts($foreignInstall,$programs,$startup)
            $customManager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
            $matches = [ProGo.ApplicationShortcuts].GetMethod('Matches',[Reflection.BindingFlags]'Instance,NonPublic')
            $currentManager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
            # CLR String[] enforces unwrapped String elements for reflection's Object[] parameter.
            [string[]]$matchArguments = @([string]$legacy,[string]'--show')
            $parameters = $matches.GetParameters()
            Check-LegacyAdmission ($matchArguments.GetType() -eq [string[]] -and $matchArguments.Length -eq 2 -and
                $matchArguments.GetType().GetElementType() -eq [string] -and
                $parameters.Length -eq 2 -and $parameters[0].ParameterType -eq [string] -and
                $parameters[1].ParameterType -eq [string]) 'reflection arguments have exact CLR String element type and match both native oracle parameters'
            Check-LegacyAdmission ($matches.Invoke($customManager,$matchArguments) -and
                -not $matches.Invoke($currentManager,$matchArguments)) 'real native custom link loads and belongs only to foreign fixture target'
            $customHash = (Get-FileHash -LiteralPath $legacy).Hash
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        if ($null -ne $customHash) { Check-LegacyAdmission ((Get-FileHash -LiteralPath $legacy).Hash -ceq $customHash -and
            $receipt.Legacy.Disposition -ceq 'preserved' -and -not $receipt.Legacy.BeforeMatched -and
            $receipt.Legacy.BeforeExists -and $receipt.Legacy.Exists) 'actual migration preserves exact customized Legacy bytes and non-owned disposition' }
        function Write-ProGoInstallerOutcomeBytes($Stream,[byte[]]$Bytes) { $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true); throw 'synthetic pending receipt interruption' }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check','shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic pending receipt interruption') }
        Check-LegacyAdmission $interrupted 'actual durable pending receipt exists'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        if ($scenario -eq 'post-startup') {
            Check-LegacyAdmission (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'actual Startup rollback precedes Legacy classification'
            Check-LegacyAdmission (Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup) 'Startup durable result verifies'
        }
        Check-LegacyAdmission (Invoke-ProGoInstallerMenuRollback $journal $install $programs $startup) 'actual Menu rollback precedes Legacy classification'
        Complete-ProGoInstallerMenuResult $journal $install $programs $startup
        Check-LegacyAdmission (Test-ProGoInstallerMenuResult $journal $install $programs $startup) 'real Menu admission/result and physical context first verify'
        $suffix = (Split-Path -Leaf $journal).Substring('installer-journal-'.Length); $parent = Split-Path -Parent $journal
        $resultPath = Join-Path $parent ('installer-menu-outcome-' + $suffix + '.json')
        $admissionPath = Join-Path $parent ('installer-menu-admission-' + $suffix + '.json')
        if ($scenario -eq 'missing-menu-result') { [IO.File]::Delete($resultPath) }
        if ($scenario -eq 'tampered-menu-result') { [IO.File]::WriteAllText($resultPath,'{}') }
        if ($scenario -eq 'admission-tampered') { [IO.File]::WriteAllText($admissionPath,'{}') }
        if ($scenario -in @('missing-origin','wrong-origin','unknown-receipt-field','wrong-replacement')) {
            $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
            if ($scenario -eq 'missing-origin') { $saved.Completion.Startup.Legacy.PSObject.Properties.Remove('OriginIdentity') }
            if ($scenario -eq 'wrong-origin') { $saved.Completion.Startup.Legacy.OriginIdentity = ('0' * 64) }
            if ($scenario -eq 'unknown-receipt-field') { $saved.Completion.Startup.Legacy | Add-Member NoteProperty Unexpected $true }
            if ($scenario -eq 'wrong-replacement') { $saved.Completion.Startup.Legacy.ReplacementHash = ('0' * 64) }
            [IO.File]::WriteAllText($pending,($saved | ConvertTo-Json -Depth 8 -Compress))
        }
        if ($scenario -eq 'same-bytes-replaced') {
            $bytes = [IO.File]::ReadAllBytes($legacy); $creation = [IO.File]::GetCreationTimeUtc($legacy)
            [IO.File]::Move($legacy,(Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($legacy,$bytes); [IO.File]::SetCreationTimeUtc($legacy,$creation)
        }
        if ($scenario -in @('legacy-appeared','legacy-changed')) { [IO.File]::WriteAllText($legacy,'synthetic external Legacy bytes') }
        if ($scenario -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic damaged snapshot') }
        if ($scenario -eq 'runtime-changed') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic external runtime') }
        if ($scenario -eq 'foreign-admission') { [IO.File]::WriteAllText((Join-Path $parent 'installer-menu-admission-unknown.json'),'synthetic conflicting admission') }
        $trustedPrograms = if ($scenario -eq 'wrong-location') { Join-Path $case 'other-Programs' } else { $programs }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $expected = $scenario -in @('absent-preserved','custom-preserved','removed','post-startup')
        if ($scenario -eq 'receipt-busy') { $held = [IO.File]::Open($pending,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None) }
        try {
            $first = $null
            for ($read = 0; $read -lt 3; $read++) {
                $proof = Get-ProGoInstallerLegacyRollbackAdmission $journal $install $trustedPrograms $startup
                Check-LegacyAdmission (($null -ne $proof) -eq $expected) 'fresh classification requires complete verified evidence'
                if ($null -ne $proof) {
                    $removed = $scenario -in @('removed','post-startup')
                    Check-LegacyAdmission ($proof.Kind -ceq $(if ($removed) { 'removed-needs-source' } else { 'preserve-only' }) -and
                        $proof.RestorationRequired -eq $removed -and -not $proof.VerifiedRestoreSourceAvailable) 'removed receipt hash does not fabricate verified original source bytes'
                    Check-LegacyAdmission (-not $proof.MutationAllowed -and -not $proof.RestorationAllowed -and -not $proof.FullRollback -and -not $proof.InstallSucceeded) 'classification grants no mutation restoration success or full rollback'
                    Check-LegacyAdmission ($proof.ReceiptHash -ceq $before[$pending] -and $proof.MenuResultHash -ceq $before[$resultPath] -and
                        $proof.MenuAdmissionHash -ceq $before[$admissionPath] -and $proof.LocationIdentity -ceq (Get-ProGoInstallerIdentity $legacy)) 'proof is bound to exact receipt/admission/result and independent Legacy location'
                    if ($removed) { Check-LegacyAdmission ($proof.BeforeExists -and -not $proof.Exists -and $proof.OwnedHash -ceq $proof.BeforeHash -and $null -ne $proof.OriginIdentity) 'removed provenance remains linked while actual absence is preserved' }
                    $text = $proof | ConvertTo-Json -Depth 5 -Compress
                    if ($null -eq $first) { $first = $text } else { Check-LegacyAdmission ($text -ceq $first) 'repeated observation yields same exact evidence without publication' }
                }
            }
        } finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
        $after = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-LegacyAdmission ($after.Count -eq $before.Count) 'read-only classification creates or deletes no file'
        foreach ($file in $after) { Check-LegacyAdmission ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'every private foreign native snapshot and metadata byte is unchanged' }
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-LegacyAdmission $blocked 'Legacy observation never permits new installation or Launch'
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy rollback admission ' + $scenario)
    }
    Write-Host ('Installer Legacy rollback admission tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-admission-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Fixture cleanup refuses unowned root.' }
        foreach ($item in Get-ChildItem -LiteralPath $work -Recurse -Force) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse descendants.' } }
        if ((Get-Item -LiteralPath $work).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses linked root.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
