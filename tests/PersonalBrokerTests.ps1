param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: personal broker consumers require isolated Windows CI'; return }
$repo = Split-Path -Parent $Scripts
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'tests/UninstallStageTests.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Broker host helpers do not parse.' }
foreach ($name in @('Check-Stage','Quote-Stage','Write-StageDriver','Invoke-StageHost')) {
    $functions = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.FunctionDefinitionAst] -and $_.Name -ceq $name })
    if ($functions.Count -ne 1) { throw 'Broker host helper is ambiguous.' }
    . ([scriptblock]::Create($functions[0].Extent.Text))
}
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'tests/PersonalInstallTransactionTests.ps1'),[ref]$tokens,[ref]$errors)
$assignment = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -ceq '$adapters' })
if ($errors.Count -or $assignment.Count -ne 1) { throw 'Broker adapters are ambiguous.' }
. ([scriptblock]::Create($assignment[0].Extent.Text))
$adapterAst = [Management.Automation.Language.Parser]::ParseInput($adapters,[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Broker adapters do not parse.' }
# Use existing isolated OS adapters, but NEVER override either consumer factory.
$adapters = (@($adapterAst.EndBlock.Statements | Where-Object {
    $_ -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $_.Name -cnotin @('New-PersonalFixtureContext','Open-ProGoPersonalInstallerTransaction','Open-ProGoPersonalUninstallerTransaction')
}) | ForEach-Object { $_.Extent.Text }) -join "`r`n"
if ($adapters.Contains('function Open-ProGoPersonal')) { throw 'Broker fixture replaced a consumed factory.' }
$script:StageChildrenSettled = $true
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-personal-transaction-' + [guid]::NewGuid().ToString('N'))
. (Join-Path $Scripts 'Install-ProGo.ps1') -FunctionsOnly
$payload = Get-ProGoInstallPayload (Join-Path $repo 'release')
function Invoke-BrokerConsumer($Fixture,[string]$operation,[string]$proof='valid',[string]$expected='') {
    $Fixture.Operation = $operation; $Fixture.Proof = $proof; $Fixture.Expected = $expected
    $serialized = '$Fixture = ' + (Quote-Stage ($Fixture | ConvertTo-Json -Depth 6 -Compress)) + ' | ConvertFrom-Json'
    $prefix = $serialized + @'

if ('ProGo.ApplicationShortcuts' -as [type]) { throw 'Broker fixture needs a fresh host.' }
$using = New-Object 'System.Collections.Generic.HashSet[string]'
$texts = @()
foreach ($source in @('ApplicationShortcuts.cs','PersonalBroker.cs','PersonalBootstrapImage.cs')) {
    $text = [IO.File]::ReadAllText((Join-Path $Fixture.Sources $source))
    foreach ($match in [regex]::Matches($text,'(?m)^using [^;]+;')) { [void]$using.Add($match.Value) }
    $texts += [regex]::Replace($text,'(?m)^using [^;]+;','')
}
Add-Type -TypeDefinition ("#define PROGO_PERSONAL_INSTALL_FIXTURE`r`n" + (($using | Sort-Object) -join "`r`n") + "`r`n" + ($texts -join "`r`n"))
if ($Fixture.Mode -ceq 'success' -and $Fixture.Operation -ceq 'install') {
    $repo = Split-Path -Parent $Fixture.Sources
    $image = [IO.File]::ReadAllBytes((Join-Path $repo 'release/ProGo.Setup.exe'))
    $origin = [IO.File]::ReadAllText((Join-Path $repo 'build/PersonalBroker.origin'))
    if (-not [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CheckFixtureSeal($image,$origin)) { throw 'Native seal positive failed.' }
    [byte[]]$changed = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CorruptFixtureArchive($image)
    $refused = $false
    try { [void][ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CheckFixtureSeal($changed,$origin) }
    catch { $refused = $_.Exception.InnerException -is [Security.SecurityException] -and $_.Exception.InnerException.Message -ceq 'Personal broker payload seal differs.' }
    if (-not $refused) { throw 'Native sealed payload substitution accepted.' }
    [byte[]]$changed = $image.Clone()
    $pe = [BitConverter]::ToInt32($changed,60); $section = $pe + 24 + [BitConverter]::ToUInt16($changed,$pe+20)
    $textRaw = [BitConverter]::ToInt32($changed,$section+20); $changed[$textRaw] = $changed[$textRaw] -bxor 1
    $refused = $false
    try { [void][ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CheckFixtureSeal($changed,$origin) }
    catch { $refused = $_.Exception.InnerException -is [Security.SecurityException] -and $_.Exception.InnerException.Message -ceq 'Personal broker native origin differs.' }
    if (-not $refused) { throw 'Native code substitution accepted.' }
    # Same consumed C# admission must reject relocation redirection into .rsrc,
    # even with unchanged sealed archive, signature and manifest.
    [byte[]]$changed = $image.Clone()
    $opt = $pe + 24
    [Array]::Copy([BitConverter]::GetBytes([BitConverter]::ToUInt32($image,$opt+128)),0,$changed,$opt+152,4)
    [Array]::Copy([BitConverter]::GetBytes([uint32]12),0,$changed,$opt+156,4)
    $refused = $false
    try { [void][ProGo.ApplicationShortcuts+PersonalInstallTransaction]::CheckFixtureSeal($changed,$origin) }
    catch { $refused = $_.Exception.InnerException -is [IO.IOException] -and $_.Exception.InnerException.Message -ceq 'Personal relocation directory must belong to bounded .reloc.' }
    if (-not $refused) { throw 'Native relocation resource substitution accepted.' }
    # Verify attributes on the actual compiled declarations, without KTM/HKLM
    # calls or claiming that a Windows DLL planting exploit was reproduced.
    $store = [ProGo.ApplicationShortcuts+PersonalInstallTransaction].GetNestedType('NativeRegistrationStore',[Reflection.BindingFlags]::NonPublic)
    foreach ($method in @('CreateTransaction','CommitTransaction','RollbackTransaction')) {
        $member = $store.GetMethod($method,([Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static))
        $attributes = @($member.GetCustomAttributes([Runtime.InteropServices.DefaultDllImportSearchPathsAttribute],$false))
        if ($attributes.Count -ne 1 -or $attributes[0].Paths -ne [Runtime.InteropServices.DllImportSearchPath]::System32) { throw 'KTM import allows staging DLL lookup.' }
    }
}
$global:BrokerFixtureLease = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::EnterFixtureBroker(
    $Fixture.Root,$Fixture.Case,$Fixture.Version,[string[]]$Fixture.Names,[string[]]$Fixture.Hashes,$Fixture.Operation,$Fixture.Proof,$Fixture.Mode)
if ($Fixture.Mode -ceq 'replay') {
    $early = [ProGo.ApplicationShortcuts+PersonalInstallTransaction]::OpenPersonalInstallerTransaction()
    $early.Dispose()
}
if ($Fixture.Mode -ceq 'expired') { $global:BrokerFixtureLease.RevokeFixture() }
'@
    $uninstall = $operation -ceq 'uninstall'
    if ($Fixture.Mode -ceq 'wrong-operation') { $uninstall = $false }
    $source = if ($uninstall) { 'Uninstall-ProGo.ps1' } else { 'Install-ProGo.ps1' }
    $boundary = if ($uninstall) { '$Layout' } else { '$InstallDir' }
    $driver = Join-Path (Join-Path $Fixture.Root $Fixture.Case) ('broker-' + $operation + '.ps1')
    Write-StageDriver (Join-Path $Scripts $source) $boundary $adapters $driver $prefix
    $outer = $driver + '.outer.ps1'
    $parameters = if ($uninstall) { '@{ Personal = $true }' } else { '@{ Personal = $true; PayloadDirectory = ' + (Quote-Stage $Fixture.Payload) + ' }' }
    $wrapper = "Set-StrictMode -Version 2.0`r`n`$ErrorActionPreference = 'Stop'`r`n" + $serialized + "`r`n`$global:BrokerFixtureLease = `$null`r`ntry {`r`n" +
        '$parameters = ' + $parameters + "`r`n& " + (Quote-Stage $driver) + " @parameters`r`n" +
        '$global:BrokerFixtureLease.Complete(0)' + "`r`nWrite-Host 'BROKER_CONSUMER_SETTLED'`r`n} catch {`r`n" + @'
$failure = $_.Exception; $depth = 0
while ($depth -lt 4 -and $null -ne $failure.InnerException -and
    ($failure -is [Management.Automation.MethodInvocationException] -or $failure -is [Reflection.TargetInvocationException])) {
    $failure = $failure.InnerException; $depth++
}
if ($Fixture.Expected.Length -gt 0 -and $failure.Message -ceq $Fixture.Expected) { Write-Host 'BROKER_EXPECTED_REFUSAL'; exit 1 }
Write-Host ('BROKER_UNEXPECTED_TYPE:' + $failure.GetType().FullName); exit 2
} finally { if ($null -ne $global:BrokerFixtureLease) { $global:BrokerFixtureLease.Dispose() } }
'@
    [IO.File]::WriteAllText($outer,$wrapper,(New-Object Text.UTF8Encoding($true)))
    return Invoke-StageHost $outer
}
try {
    [void][IO.Directory]::CreateDirectory($work)
    foreach ($mode in @('success','foreign-origin','preclr-manager','preclr-profiler','alternate-admin','wrong-session','dead-original','wrong-operation','replay','expired','runtime-fault','registration-before','uninstall-fault')) {
        $case = Join-Path $work $mode
        $Fixture = @{ Root=$work; Case=$mode; Mode=$mode; Sources=(Join-Path $repo 'src'); Runtime=(Join-Path $case 'program/ProGo'); Data=(Join-Path $case 'data/ProGo');
            Payload=(Join-Path $case 'payload'); Menu=(Join-Path $case 'menu'); Startup=(Join-Path $case 'startup'); Version=$payload.Version; Names=@($payload.Files);
            Hashes=@($payload.Files | ForEach-Object { $payload.Hashes[$_] }); Writes=0; Operation=''; Proof=''; Expected='' }
        foreach ($directory in @($Fixture.Runtime,$Fixture.Data,$Fixture.Payload,$Fixture.Menu,$Fixture.Startup)) { [void][IO.Directory]::CreateDirectory($directory) }
        foreach ($name in $payload.Files) {
            $destination = Join-Path $Fixture.Payload $name
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination)); [IO.File]::Copy((Join-Path $payload.Directory $name),$destination)
        }
        $private = @{}
        foreach ($name in @('vault.enc','settings.json','private.log')) {
            $path = Join-Path $Fixture.Data $name; [IO.File]::WriteAllText($path,'original fixture bytes'); $private[$path]=(Get-FileHash -LiteralPath $path).Hash
        }
        foreach ($path in @((Join-Path $Fixture.Runtime 'foreign.txt'),(Join-Path $Fixture.Startup 'foreign.lnk'))) {
            [IO.File]::WriteAllText($path,'foreign bytes'); $private[$path]=(Get-FileHash -LiteralPath $path).Hash
        }
        $arp = Join-Path $case 'arp.txt'; [IO.File]::WriteAllText($arp,'')
        $proof = if ($mode -in @('foreign-origin','preclr-manager','preclr-profiler','alternate-admin','wrong-session','dead-original')) { $mode } else { 'valid' }
        $expected = if ($proof -cne 'valid') { 'Synthetic native broker evidence refused.' } else {
            switch ($mode) {
                'wrong-operation' { 'Personal broker operation/replay refused.' }
                'replay' { 'Personal broker operation/replay refused.' }
                'expired' { 'Personal broker authority expired.' }
                'runtime-fault' { 'Synthetic personal runtime fault.' }
                'registration-before' { 'Synthetic registration pre-write fault.' }
                default { '' }
            }
        }
        $operation = if ($mode -ceq 'wrong-operation') { 'uninstall' } else { 'install' }
        $result = Invoke-BrokerConsumer $Fixture $operation $proof $expected
        if ($expected.Length -gt 0) {
            Check-Stage ($result.Code -eq 1 -and $result.Output.Contains('BROKER_EXPECTED_REFUSAL') -and -not $result.Output.Contains('BROKER_CONSUMER_SETTLED')) 'actual broker/factory refusal, no false settlement'
            Check-Stage ([IO.File]::ReadAllText($arp).Length -eq 0) 'refusal preserves registration'
        } else { Check-Stage ($result.Code -eq 0 -and $result.Output.Contains('BROKER_CONSUMER_SETTLED')) 'actual install broker/factory registration-last settlement' }
        if ($mode -in @('success','registration-before','uninstall-fault')) {
            $Fixture.Mode = 'success'
            if ($mode -ceq 'registration-before') {
                $retry = Invoke-BrokerConsumer $Fixture 'install'
                Check-Stage ($retry.Code -eq 0 -and $retry.Output.Contains('BROKER_CONSUMER_SETTLED') -and -not $retry.Output.Contains('PERSONAL_RUNTIME_WRITE')) 'registration retry consumes committed outcome without replay'
            }
            if ($mode -ceq 'uninstall-fault') {
                $Fixture.Mode = 'uninstall-fault'
                $failure = Invoke-BrokerConsumer $Fixture 'uninstall' 'valid' 'Synthetic personal late uninstall fault.'
                Check-Stage ($failure.Code -eq 1 -and $failure.Output.Contains('BROKER_EXPECTED_REFUSAL')) 'late removal refuses settlement'
                Check-Stage ([IO.File]::ReadAllText($arp).Length -gt 0) 'partial removal retains ARP'
                $Fixture.Mode = 'success'
            }
            $removed = Invoke-BrokerConsumer $Fixture 'uninstall'
            Check-Stage ($removed.Code -eq 0 -and $removed.Output.Contains('BROKER_CONSUMER_SETTLED') -and [IO.File]::ReadAllText($arp).Length -eq 0) 'retained self-contained uninstall retry settles registration last'
        }
        foreach ($path in $private.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'broker preserves private data and foreign leaves' }
        Write-Host ('PASS personal broker consumer: ' + $mode)
    }
} finally { if ($script:StageChildrenSettled -and [IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force } }
