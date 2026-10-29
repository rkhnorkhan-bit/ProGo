param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: personal transaction requires isolated Windows CI'; return }
# Reuse the already bounded host/AST adapters; do not copy or relax their deadlines.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path (Split-Path -Parent $Scripts) 'tests/UninstallStageTests.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Personal fixture host helpers do not parse.' }
foreach ($name in @('Check-Stage', 'Quote-Stage', 'Write-StageDriver', 'Invoke-StageHost')) {
    $functions = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.FunctionDefinitionAst] -and $_.Name -ceq $name })
    if ($functions.Count -ne 1) { throw 'Personal fixture host helper is ambiguous.' }
    . ([scriptblock]::Create($functions[0].Extent.Text))
}
$script:StageChildrenSettled = $true
$junction = $null
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-personal-transaction-' + [guid]::NewGuid().ToString('N'))
$repo = Split-Path -Parent $Scripts
. (Join-Path $Scripts 'Install-ProGo.ps1') -FunctionsOnly
$original = Get-ProGoInstallPayload (Join-Path $repo 'release')
# Test-only oracle: independent exact record, never used as production authority.
function Test-PersonalFixtureRegistration([string]$Value, [string]$Runtime, [string]$Version, [string]$OwnerSid) {
    $nativeCommand = '"' + (Join-Path $Runtime 'backups/personal-broker/ProGo.Setup.exe') + '" --uninstall'
    $expectedRegistration = "DisplayName=ProGo`nDisplayVersion=$Version`nInstallLocation=$Runtime`nUninstallString=$nativeCommand`nNoModify:DWORD=1`nNoRepair:DWORD=1`nProGoOwnerSid=$OwnerSid`n"
    return $Value -ceq $expectedRegistration
}
$adapters = @'
function Check-Stage($condition, [string]$message) { if (-not $condition) { throw ('Personal transaction: ' + $message) } }
function New-PersonalFixtureContext {
    $sid = $Fixture.OwnerSid; $runtime = $Fixture.Runtime
    if ($Fixture.Mode -ceq 'context-sid') { $sid = $sid + '-foreign' }
    if ($Fixture.Mode -ceq 'context-path') { $runtime += '-foreign' }
    $identity = $null; $impersonation = $null; $context = $null
    try {
        if ($Fixture.Mode -ceq 'context-impersonation') {
            $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
            $impersonation = $identity.Impersonate()
        }
        $context = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CreateFixture($Fixture.Root, $Fixture.Case, $runtime, $Fixture.Data,
            $Fixture.Payload, $sid, [string[]]$Fixture.Names, [string[]]$Fixture.Hashes, $Fixture.Version, $Fixture.Mode)
        if ($Fixture.Mode -ceq 'context-revalidate') {
            $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
            $impersonation = $identity.Impersonate()
            # Exercise the actual registration admission after constructing the
            # capability without impersonation. Same SID must still be refused.
            $context.AdmitRegistration()
        }
        return $context
    } catch {
        if ($null -ne $context) { $context.Dispose() }
        throw
    } finally {
        if ($null -ne $impersonation) { $impersonation.Dispose() }
        if ($null -ne $identity) { $identity.Dispose() }
    }
}
function Open-ProGoPersonalInstallerTransaction($Payload) {
    if ($Fixture.Mode -ceq 'context-object') {
        $fake = [pscustomobject]@{ RuntimeRoot = 'foreign'; DataRoot = 'foreign' }
        $fake | Add-Member ScriptMethod Dispose { [IO.File]::WriteAllText($Fixture.Canary, 'foreign context method executed') }
        return $fake
    }
    if ($Fixture.Mode -ceq 'uac-cancel') { throw [OperationCanceledException]::new('Synthetic UAC cancellation.') }
    if ($Fixture.Mode -ceq 'uac-denied') { throw [Security.SecurityException]::new('Synthetic UAC refusal.') }
    return New-PersonalFixtureContext
}
function Open-ProGoPersonalUninstallerTransaction { return New-PersonalFixtureContext }
function Enter-ProGoInstallerMaintenance {
    $lease = [pscustomobject]@{}; $lease | Add-Member ScriptMethod Dispose {}; return $lease
}
function Enter-ProGoUninstallMaintenance { return Enter-ProGoInstallerMaintenance }
function Check-PersonalRegistrationPins {
    if ($Fixture.Mode -ne 'registration-pins') { return }
    $arp = Join-Path (Join-Path $Fixture.Root $Fixture.Case) 'arp.txt'
    $writeRefused = $false; $renameRefused = $false
    try { [IO.File]::WriteAllText($arp, 'foreign registration') } catch [IO.IOException] { $writeRefused = $true }
    try { [IO.File]::Move($arp, $arp + '-foreign') } catch [IO.IOException] { $renameRefused = $true }
    Check-Stage ($writeRefused -and $renameRefused) 'personal registration object is physically pinned'
    Write-Host 'PERSONAL_REGISTRATION_PINS_CONFIRMED'
}
function Stop-ProGoInstallerApplication($InstallDirectory, $DataDirectory) {
    Check-PersonalRegistrationPins
    Check-Stage ($InstallDirectory -ceq $Fixture.Runtime -and $DataDirectory -ceq $Fixture.Data) 'personal install preserves original data context'
}
function Stop-ProGoUninstallApplication($RuntimeRoot, $DataRoot) {
    Check-PersonalRegistrationPins
    Check-Stage ($RuntimeRoot -ceq $Fixture.Runtime -and $DataRoot -ceq $Fixture.Data) 'personal uninstall preserves original data context'
}
function Complete-ProGoUninstallFirewall($RuntimeRoot) {}
function Read-Host($Prompt) { throw 'Personal fixture must not request userdata deletion.' }
function Invoke-ProGoInstallerSelfCheck($Info) {
    [void][IO.Directory]::CreateDirectory($Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT']); return 0
}
function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
    Write-Host 'PERSONAL_RUNTIME_WRITE'
    $Fixture.Writes++; $Target.SetLength(0)
    if ($Fixture.Mode -ceq 'runtime-fault' -and $Fixture.Writes -eq 2) { $Target.WriteByte(42); $Target.Flush($true); throw 'Synthetic personal runtime fault.' }
    $Source.CopyTo($Target); $Target.Flush($true)
}
function Initialize-ProGoInstallerShortcuts($RuntimeRoot, $WasInstalled, $NoStartup, $NoStartMenu) {
    Check-Stage $NoStartup 'personal install forces NoStartup without changing legacy defaults'
    return Initialize-ProGoShortcuts -InstallDirectory $RuntimeRoot -WasInstalled $WasInstalled -NoStartup $NoStartup -NoStartMenu $NoStartMenu -ProgramsDirectory $Fixture.Menu -StartupDirectory $Fixture.Startup -CaptureStartupEvidence
}
function New-ProGoUninstallShortcuts($RuntimeRoot) {
    $manager = New-Object ProGo.ApplicationShortcuts($RuntimeRoot, $Fixture.Menu, $Fixture.Startup)
    $proxy = [pscustomobject]@{ Manager = $manager }
    $proxy | Add-Member ScriptMethod RemoveOwnedShortcuts { $this.Manager.RemoveOwnedShortcuts() }
    $proxy | Add-Member ScriptMethod RemoveRuntimeFile {
        param($relative, $hash)
        $this.Manager.RemoveRuntimeFile($relative, $hash)
        if ($Fixture.Mode -ceq 'uninstall-fault' -and $relative -ceq 'scripts/Layout-ProGo.ps1') { throw 'Synthetic personal late uninstall fault.' }
    }
    return $proxy
}
'@
function Personal-Prefix($Fixture) {
    return '$Fixture = ' + (Quote-Stage ($Fixture | ConvertTo-Json -Depth 6 -Compress)) + ' | ConvertFrom-Json' + @'

if ('ProGo.ApplicationShortcuts' -as [type]) { throw 'Personal fixture did not start in a fresh host.' }
Add-Type -TypeDefinition ("#define PROGO_PERSONAL_INSTALL_FIXTURE`r`n" + [IO.File]::ReadAllText($Fixture.Source))
'@
}
try {
    [void][IO.Directory]::CreateDirectory($work)
    foreach ($mode in @('success', 'uac-cancel', 'uac-denied', 'context-sid', 'context-impersonation', 'context-revalidate', 'context-path', 'context-object', 'context-reparse', 'payload-tamper', 'entry-tamper', 'registry-foreign', 'registration-before', 'registration-after', 'registration-pins', 'runtime-fault', 'uninstall-fault', 'uninstall-foreign')) {
        $case = Join-Path $work $mode
        $Fixture = @{ Root = $work; Case = $mode; Mode = $mode; Runtime = (Join-Path $case 'program/ProGo'); Data = (Join-Path $case 'data/ProGo'); Payload = (Join-Path $case 'payload'); Menu = (Join-Path $case 'menu'); Startup = (Join-Path $case 'startup'); Source = (Join-Path $repo 'src/ApplicationShortcuts.cs'); Writes = 0; Version = $original.Version; OwnerSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value; Names = @($original.Files); Hashes = @($original.Files | ForEach-Object { $original.Hashes[$_] }) }
        foreach ($dir in @($Fixture.Runtime, $Fixture.Data, $Fixture.Payload, $Fixture.Menu, $Fixture.Startup)) { [void][IO.Directory]::CreateDirectory($dir) }
        foreach ($leaf in $original.Files) {
            $destination = Join-Path $Fixture.Payload $leaf
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
            [IO.File]::Copy((Join-Path $original.Directory $leaf), $destination)
        }
        $junction = $null
        if ($mode -ceq 'context-reparse') {
            $foreignTarget = Join-Path $case 'foreign-target'; [void][IO.Directory]::CreateDirectory($foreignTarget)
            [IO.File]::WriteAllText((Join-Path $foreignTarget 'foreign.txt'), 'foreign target bytes')
            [IO.Directory]::Delete($Fixture.Runtime, $false)
            $junction = New-Item -ItemType Junction -Path $Fixture.Runtime -Target $foreignTarget
            $junctionTarget = $junction.Target | ConvertTo-Json -Compress
        }
        $private = @{}
        foreach ($leaf in @('vault.enc', 'settings.json', 'private.log')) {
            $path = Join-Path $Fixture.Data $leaf; [IO.File]::WriteAllText($path, 'original private fixture bytes')
            $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        $legacy = Join-Path $Fixture.Data 'ProGo.exe'; [IO.File]::Copy((Join-Path $Fixture.Payload 'ProGo.exe'), $legacy)
        $private[$legacy] = (Get-FileHash -LiteralPath $legacy).Hash
        $foreignLink = Join-Path $Fixture.Startup 'ProGo.lnk'
        $shell = New-Object -ComObject WScript.Shell
        try {
            $link = $shell.CreateShortcut($foreignLink); $link.TargetPath = Join-Path $Fixture.Payload 'ProGo.exe'; $link.Save()
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link)
        } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
        $private[$foreignLink] = (Get-FileHash -LiteralPath $foreignLink).Hash
        [void][IO.Directory]::CreateDirectory((Join-Path $Fixture.Menu 'ProGo'))
        foreach ($path in @((Join-Path $Fixture.Menu 'ProGo/foreign.txt'), (Join-Path $Fixture.Runtime 'foreign.txt'))) {
            if (-not [IO.File]::Exists($path)) { [IO.File]::WriteAllText($path, 'foreign sibling bytes') }
            $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        $arp = Join-Path $case 'arp.txt'; [IO.File]::WriteAllText($arp, '')
        if ($mode -ceq 'registry-foreign') { [IO.File]::WriteAllText($arp, 'foreign registration bytes') }
        $initialArp = [IO.File]::ReadAllText($arp)
        $canary = Join-Path $case 'execution-canary.txt'; $Fixture.Canary = $canary
        $canaryCode = '[IO.File]::WriteAllText(' + (Quote-Stage $canary) + ", 'executed'); throw 'PERSONAL_FOREIGN_CODE_EXECUTED'"
        if ($mode -ceq 'payload-tamper') { [IO.File]::WriteAllText((Join-Path $Fixture.Payload 'scripts/Layout-ProGo.ps1'), $canaryCode) }
        if ($mode -ceq 'entry-tamper') { [IO.File]::AppendAllText((Join-Path $Fixture.Payload 'scripts/Install-ProGo.ps1'), $canaryCode) }
        $driver = Join-Path $case 'InstallConsumer.ps1'
        Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $adapters $driver (Personal-Prefix $Fixture)
        # No NoStartup flag is passed: the actual Personal consumer must force it.
        $result = Invoke-StageHost $driver ('-Personal -PayloadDirectory "' + $Fixture.Payload + '"')
        $expected = switch ($mode) {
            'uac-cancel' { 'Synthetic UAC cancellation.' }
            'uac-denied' { 'Synthetic UAC refusal.' }
            'context-sid' { 'Personal installer original user context differs.' }
            'context-impersonation' { 'Personal installer refuses thread impersonation.' }
            'context-revalidate' { 'Personal installer refuses thread impersonation.' }
            'context-path' { 'Personal fixture fixed paths differ.' }
            'context-object' { 'Personal installer requires a live typed transaction.' }
            'context-reparse' { 'Legacy source directory is unavailable or redirected.' }
            'payload-tamper' { 'Personal payload differs from trusted caller input.' }
            'entry-tamper' { 'Personal payload differs from trusted caller input.' }
            'registry-foreign' { 'Personal registration is foreign or changed.' }
            'registration-before' { 'Synthetic registration pre-write fault.' }
            'registration-after' { 'Synthetic registration post-write fault.' }
            'runtime-fault' { 'Synthetic personal runtime fault.' }
            default { $null }
        }
        if ($null -ne $expected) {
            Check-Stage ($result.Code -ne 0 -and $result.Error.Contains($expected) -and -not $result.Output.Contains('Install OK')) 'specific personal refusal, never false success'
            Check-Stage ([IO.File]::ReadAllText($arp) -ceq $initialArp) 'refusal preserves original registration'
            if ($mode -in @('registration-before', 'registration-after')) {
                Check-Stage (@(Get-ProGoUninstallInventory $Fixture.Runtime).Count -gt 0 -and [IO.File]::Exists((Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'))) 'postcommit refusal retains verified runtime and durable retry'
            } else {
                foreach ($leaf in @($original.Files | Where-Object { $_ -notin @('README.md', 'README.ru.md', 'LICENSE.md') })) { Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $leaf))) 'precommit refusal or rollback leaves no installed runtime leaf' }
            }
        } else { Check-Stage ($result.Code -eq 0) ('personal install succeeds: ' + $result.Error) }
        Check-Stage (-not [IO.File]::Exists($canary) -and -not $result.Error.Contains('PERSONAL_FOREIGN_CODE_EXECUTED')) 'untrusted payload or entry bytes never execute'
        if ($mode -in @('registration-before', 'registration-after', 'runtime-fault')) {
            $journalsBefore = @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*').Count
            $Fixture.Mode = 'retry'; $Fixture.Writes = 0
            Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $adapters $driver (Personal-Prefix $Fixture)
            $retry = Invoke-StageHost $driver ('-Personal -PayloadDirectory "' + $Fixture.Payload + '"')
            Check-Stage ($retry.Code -eq 0) ('personal install retry succeeds: ' + $retry.Error)
            if ($mode -ne 'runtime-fault') { Check-Stage (@(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*').Count -eq $journalsBefore -and $retry.Output.Contains('without reinstall') -and -not $retry.Output.Contains('PERSONAL_RUNTIME_WRITE')) 'registration retry does not replay runtime install' }
        }
        if ($null -eq $expected -or $mode -in @('registration-before', 'registration-after', 'runtime-fault')) {
            $registered = [IO.File]::ReadAllText($arp); $retained = Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'
            Check-Stage (Test-PersonalFixtureRegistration $registered $Fixture.Runtime $original.Version $Fixture.OwnerSid) 'registration derives fixed version, root, command and original user only after commit'
            if ($mode -ceq 'success') {
                $nativeCommand = '"' + (Join-Path $Fixture.Runtime 'backups/personal-broker/ProGo.Setup.exe') + '" --uninstall'
                # Exercise the SAME matcher as the consumed registration assertion.
                $mutations = @(
                    $registered.Replace($nativeCommand, ('"' + $retained + '" -Personal')),
                    $registered.Replace($nativeCommand, ((Join-Path $Fixture.Runtime 'backups/personal-broker/ProGo.Setup.exe') + ' --uninstall')),
                    $registered.Replace($nativeCommand, ($nativeCommand + ' -Personal')),
                    $registered.Replace(' --uninstall', ' --install'),
                    $registered.Replace($nativeCommand, ('"' + (Join-Path ($Fixture.Runtime + '-foreign') 'backups/personal-broker/ProGo.Setup.exe') + '" --uninstall')),
                    $registered.Replace('DisplayName=ProGo', 'DisplayName=ProGoForeign'),
                    $registered.Replace('DisplayVersion=' + $original.Version, 'DisplayVersion=' + $original.Version + '-foreign'),
                    $registered.Replace('InstallLocation=' + $Fixture.Runtime, 'InstallLocation=' + $Fixture.Runtime + '-foreign'),
                    $registered.Replace('ProGoOwnerSid=' + $Fixture.OwnerSid, 'ProGoOwnerSid=' + $Fixture.OwnerSid + '-foreign'),
                    $registered.Replace('NoModify:DWORD=1', 'NoModify:DWORD=0'),
                    $registered.Replace('NoRepair:DWORD=1', 'NoRepair:DWORD=0'),
                    ($registered + 'UninstallString=' + $nativeCommand + "`n"),
                    ('foreign-prefix' + $registered)
                )
                foreach ($mutation in $mutations) {
                    Check-Stage (-not (Test-PersonalFixtureRegistration $mutation $Fixture.Runtime $original.Version $Fixture.OwnerSid)) 'exact registration matcher rejects command/identity/duplicate mutations'
                }
            }
            $dataGate = Invoke-StageHost $retained '-Personal -RemoveUserData'
            Check-Stage ($dataGate.Code -ne 0 -and $dataGate.Error.Contains('Personal privileged transaction keeps user data') -and [IO.File]::ReadAllText($arp) -ceq $registered) 'privileged worker cannot delete user data'
            $gate = Invoke-StageHost $retained '-Personal'
            Check-Stage ($gate.Code -ne 0 -and $gate.Error.Contains('Personal uninstaller requires its native bootstrap.') -and [IO.File]::ReadAllText($arp) -ceq $registered) 'production uninstall remains gated without fixture factory'
            $proofs = @{}
            foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Recurse -File) { $proofs[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
            $runtimeBefore = @{}
            foreach ($entry in @(Get-ProGoUninstallInventory $Fixture.Runtime)) { $runtimeBefore[$entry.Relative] = (Get-FileHash -LiteralPath (Join-Path $Fixture.Runtime $entry.Relative)).Hash }
            if ($mode -ceq 'uninstall-foreign') { [IO.File]::WriteAllText($arp, 'foreign replacement registration') }
            $uninstall = Join-Path $case 'UninstallConsumer.ps1'
            Write-StageDriver $retained '$Layout' $adapters $uninstall (Personal-Prefix $Fixture)
            $removed = Invoke-StageHost $uninstall '-Personal'
            if ($mode -in @('uninstall-fault', 'uninstall-foreign')) {
                $reason = if ($mode -ceq 'uninstall-fault') { 'Synthetic personal late uninstall fault.' } else { 'Personal registration is foreign or changed.' }
                Check-Stage ($removed.Code -ne 0 -and $removed.Error.Contains($reason) -and -not $removed.Output.Contains('Uninstall OK')) 'uninstall specific refusal without false success'
                if ($mode -ceq 'uninstall-fault') {
                    Check-Stage ([IO.File]::ReadAllText($arp) -ceq $registered -and -not [IO.File]::Exists((Join-Path $Fixture.Runtime 'scripts/Layout-ProGo.ps1'))) 'late uninstall failure preserves registration and retry'
                    $Fixture.Mode = 'retry'
                    Write-StageDriver $retained '$Layout' $adapters $uninstall (Personal-Prefix $Fixture)
                    $removed = Invoke-StageHost $uninstall '-Personal'
                } else {
                    Check-Stage ([IO.File]::ReadAllText($arp) -ceq 'foreign replacement registration') 'foreign registration is never removed and runtime is kept'
                    foreach ($leaf in $runtimeBefore.Keys) { Check-Stage ((Get-FileHash -LiteralPath (Join-Path $Fixture.Runtime $leaf)).Hash -ceq $runtimeBefore[$leaf]) 'foreign registration refusal preserves every runtime leaf' }
                }
            }
            if ($mode -ceq 'registration-pins') { Check-Stage ($result.Output.Contains('PERSONAL_REGISTRATION_PINS_CONFIRMED') -and $removed.Output.Contains('PERSONAL_REGISTRATION_PINS_CONFIRMED')) 'registration pins hold in both actual consumers' }
            if ($mode -ne 'uninstall-foreign') { Check-Stage ($removed.Code -eq 0 -and [IO.File]::ReadAllText($arp) -ceq '' -and [IO.File]::Exists($retained)) ('owned registration removed last; retry remains: ' + $removed.Error) }
            foreach ($path in $proofs.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $proofs[$path]) 'personal uninstall retains all original recovery evidence' }
        }
        foreach ($path in $private.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'original data, legacy executable and foreign startup link preserved' }
        if ($null -ne $junction) {
            $currentJunction = Get-Item -LiteralPath $junction.FullName -Force
            Check-Stage (($currentJunction.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -and
                ($currentJunction.Target | ConvertTo-Json -Compress) -ceq $junctionTarget -and
                [IO.File]::ReadAllText((Join-Path $foreignTarget 'foreign.txt')) -ceq 'foreign target bytes' -and
                @(Get-ChildItem -LiteralPath $foreignTarget -Force).Count -eq 1) 'foreign junction target preserved'
            [IO.Directory]::Delete($junction.FullName, $false); $junction = $null
        }
        Write-Host ('PASS: consumed personal transaction ' + $mode)
    }
    $productionCompile = Join-Path $work 'ProductionContext.ps1'
    $compileText = 'Add-Type -TypeDefinition ([IO.File]::ReadAllText(' + (Quote-Stage (Join-Path $repo 'src/ApplicationShortcuts.cs')) + '))' + "`r`n" + @'
$type = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]
if ($null -ne $type.GetMethod('CreateFixture') -or $null -ne $type.GetMethod('CreateRegistryFixture') -or $type.GetConstructors().Length -ne 0) { throw 'Production context factory is exposed.' }
Write-Host 'PRODUCTION_PERSONAL_FACTORY_CLOSED'
'@
    [IO.File]::WriteAllText($productionCompile, $compileText)
    $compile = Invoke-StageHost $productionCompile
    Check-Stage ($compile.Code -eq 0 -and $compile.Output.Contains('PRODUCTION_PERSONAL_FACTORY_CLOSED')) 'normal compilation excludes fixture factory'
    $gate = Invoke-StageHost (Join-Path $Scripts 'Install-ProGo.ps1') ('-Personal -PayloadDirectory "' + $original.Directory + '"')
    Check-Stage ($gate.Code -ne 0 -and $gate.Error.Contains('Personal installer requires its native bootstrap.')) 'production installer remains gated without fixture factory'
} finally {
    if ($script:StageChildrenSettled) {
        if ($null -ne $junction) { [IO.Directory]::Delete($junction.FullName, $false) }
        if ([IO.Directory]::Exists($work)) { Remove-ProGoInstallerOwnedDirectory $work }
    }
    elseif (-not $script:StageChildrenSettled) { Write-Warning ('Personal child settlement unknown; fixture retained: ' + $work) }
}
