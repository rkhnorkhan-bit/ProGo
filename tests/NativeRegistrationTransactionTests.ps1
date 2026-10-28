param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: native registration requires isolated Windows CI'; return }
$repo = Split-Path -Parent $Scripts
# Import the existing bounded fresh-host helpers and actual consumer adapters.
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'tests/UninstallStageTests.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Native registration host helpers do not parse.' }
foreach ($name in @('Check-Stage', 'Quote-Stage', 'Write-StageDriver', 'Invoke-StageHost')) {
    $functions = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.FunctionDefinitionAst] -and $_.Name -ceq $name })
    if ($functions.Count -ne 1) { throw 'Native registration host helper is ambiguous.' }
    . ([scriptblock]::Create($functions[0].Extent.Text))
}
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'tests/PersonalInstallTransactionTests.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Native registration consumer adapters do not parse.' }
$assignment = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -ceq '$adapters' })
if ($assignment.Count -ne 1) { throw 'Native registration consumer adapters are ambiguous.' }
. ([scriptblock]::Create($assignment[0].Extent.Text))
if ([regex]::Matches($adapters, '\]::CreateFixture\(').Count -ne 1) { throw 'Native registration factory boundary differs.' }
$adapters = $adapters.Replace(']::CreateFixture(', ']::CreateRegistryFixture(')
$script:StageChildrenSettled = $true
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-personal-transaction-' + [guid]::NewGuid().ToString('N'))
$registryPath = 'SOFTWARE\ProGo.RegistryFixtures\' + (Split-Path -Leaf $work).Substring('ProGo-personal-transaction-'.Length)
$hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryView]::Registry64)
. (Join-Path $Scripts 'Install-ProGo.ps1') -FunctionsOnly
$original = Get-ProGoInstallPayload (Join-Path $repo 'release')
function Native-Prefix($Fixture) {
    return '$Fixture = ' + (Quote-Stage ($Fixture | ConvertTo-Json -Depth 6 -Compress)) + ' | ConvertFrom-Json' + @'

if ('ProGo.ApplicationShortcuts' -as [type]) { throw 'Native registration did not start in a fresh host.' }
Add-Type -TypeDefinition ("#define PROGO_PERSONAL_INSTALL_FIXTURE`r`n" + [IO.File]::ReadAllText($Fixture.Source))
'@
}
function ConvertTo-NativeErrorRecord {
    param([Exception]$Exception, [string]$Mode)
    $depth = 0
    while ($depth -lt 4 -and $null -ne $Exception.InnerException -and
        ($Exception.GetType() -eq [Management.Automation.MethodInvocationException] -or
         $Exception.GetType() -eq [Reflection.TargetInvocationException])) {
        $Exception = $Exception.InnerException; $depth++
    }
    $type = $Exception.GetType().FullName; $message = $Exception.Message
    return ([ordered]@{ Schema = 1; Mode = $Mode.Substring(0, [Math]::Min(64, $Mode.Length));
        Type = $type.Substring(0, [Math]::Min(128, $type.Length));
        Message = $message.Substring(0, [Math]::Min(1024, $message.Length)); MessageLength = $message.Length;
        Truncated = ($Mode.Length -gt 64 -or $type.Length -gt 128 -or $message.Length -gt 1024);
        WrapperDepth = $depth } | ConvertTo-Json -Depth 4 -Compress)
}
function Invoke-NativeHost([string]$Driver, [hashtable]$ConsumerArguments, [string]$Mode) {
    # The generated consumer is unchanged and runs with & in this same fresh child.
    # Its catch/finally unwind before this external catch observes the exception.
    if ($null -eq $ConsumerArguments) { throw 'Native consumer arguments must be a hashtable.' }
    foreach ($key in $ConsumerArguments.Keys) {
        if ($key -isnot [string] -or $key -cnotin @('Personal', 'PayloadDirectory')) { throw 'Unknown native consumer argument.' }
    }
    $entries = @()
    if ($ConsumerArguments.ContainsKey('Personal')) {
        if ($ConsumerArguments['Personal'] -isnot [bool]) { throw 'Native Personal argument must be Boolean.' }
        $literal = if ($ConsumerArguments['Personal']) { '$true' } else { '$false' }
        $entries += 'Personal = ' + $literal
    }
    if ($ConsumerArguments.ContainsKey('PayloadDirectory')) {
        if ($ConsumerArguments['PayloadDirectory'] -isnot [string]) { throw 'Native PayloadDirectory argument must be String.' }
        $entries += 'PayloadDirectory = ' + (Quote-Stage $ConsumerArguments['PayloadDirectory'])
    }
    $wrapper = $Driver + '.outer.ps1'
    $text = "Set-StrictMode -Version 2.0`r`n`$ErrorActionPreference = 'Stop'`r`n"
    $text += 'function ConvertTo-NativeErrorRecord {' + (Get-Command -CommandType Function -Name ConvertTo-NativeErrorRecord).Definition + "}`r`ntry {`r`n"
    $text += '    $consumerArguments = @{' + ($entries -join '; ') + "}`r`n"
    $text += '    & ' + (Quote-Stage $Driver) + " @consumerArguments`r`n} catch {`r`n"
    $text += '    $record = ConvertTo-NativeErrorRecord $_.Exception ' + (Quote-Stage $Mode) + "`r`n"
    $text += '    [Console]::Error.WriteLine(''PROGO_NATIVE_ERROR_V1 '' + $record)' + "`r`n    throw`r`n}`r`n"
    [IO.File]::WriteAllText($wrapper, $text, (New-Object Text.UTF8Encoding($true)))
    return Invoke-StageHost $wrapper
}
function Test-NativeFailureRecord($Result, [string]$Mode, [string]$Message) {
    if ($Result.Code -isnot [int] -or $Result.Code -eq 0 -or $Result.Error -isnot [string] -or $Result.Output -isnot [string] -or
        $Result.Error.Length -gt 65536 -or $Result.Output.Contains('Install OK') -or $Result.Output.Contains('Uninstall OK')) { return $false }
    $marker = 'PROGO_NATIVE_ERROR_V1'; $prefix = $marker + ' '
    if ([regex]::Matches($Result.Error, [regex]::Escape($marker)).Count -ne 1) { return $false }
    $lines = @($Result.Error -split '\r?\n' | Where-Object { $_.StartsWith($marker, [StringComparison]::Ordinal) })
    if ($lines.Count -ne 1 -or -not $lines[0].StartsWith($prefix, [StringComparison]::Ordinal)) { return $false }
    $json = $lines[0].Substring($prefix.Length)
    if ($json.Length -gt 8192) { return $false }
    try {
        $record = $json | ConvertFrom-Json -ErrorAction Stop
        if (($record.PSObject.Properties.Name -join ',') -cne 'Schema,Mode,Type,Message,MessageLength,Truncated,WrapperDepth') { return $false }
        # Canonical round-trip rejects duplicate keys and noncanonical/extra framing.
        if (-not [string]::Equals($json, ($record | ConvertTo-Json -Depth 4 -Compress), [StringComparison]::Ordinal)) { return $false }
        if (($record.Schema -isnot [int] -and $record.Schema -isnot [long]) -or $record.Schema -ne 1 -or $record.Mode -isnot [string] -or
            $record.Type -isnot [string] -or $record.Message -isnot [string] -or ($record.MessageLength -isnot [int] -and $record.MessageLength -isnot [long]) -or
            $record.Truncated -isnot [bool] -or $record.Truncated -or ($record.WrapperDepth -isnot [int] -and $record.WrapperDepth -isnot [long]) -or
            $record.WrapperDepth -lt 0 -or $record.WrapperDepth -gt 4 -or $record.Message.Length -gt 1024 -or
            $record.MessageLength -ne $record.Message.Length) { return $false }
        return ([string]::Equals($record.Mode, $Mode, [StringComparison]::Ordinal) -and
            [string]::Equals($record.Type, 'System.IO.IOException', [StringComparison]::Ordinal) -and
            [string]::Equals($record.Message, $Message, [StringComparison]::Ordinal))
    } catch { return $false }
}
function Test-NativeArgumentBinding([string]$Work) {
    $payload = Join-Path $Work "payload with spaces and O'Brien"
    foreach ($kind in @('install', 'uninstall', 'empty')) {
        $parameters = @{}; $keys = ''; $present = '$false'; $expectedPayload = ''
        if ($kind -ne 'empty') { $parameters.Personal = $true; $keys = 'Personal'; $present = '$true' }
        if ($kind -eq 'install') { $parameters.PayloadDirectory = $payload; $keys = 'PayloadDirectory,Personal'; $expectedPayload = $payload }
        $driver = Join-Path $Work ('NativeBinding-' + $kind + '.ps1')
        $text = @'
param([switch]$Personal, [string]$PayloadDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
'@
        $text += "`r`n" + 'if ($Personal.IsPresent -ne ' + $present + ") { throw 'Native binding switch differs.' }`r`n"
        $text += 'if (-not [string]::Equals($PayloadDirectory, ' + (Quote-Stage $expectedPayload) + ", [StringComparison]::Ordinal)) { throw 'Native binding payload differs.' }`r`n"
        $text += 'if ((@($PSBoundParameters.Keys | Sort-Object) -join '','') -cne ' + (Quote-Stage $keys) + ") { throw 'Native bound keys differ.' }`r`n"
        $text += 'if ($args.Count -ne 0) { throw ''Native binding has positional leftovers.'' }' + "`r`n"
        $text += '[Console]::Out.WriteLine(''NATIVE_BINDING_OK'')' + "`r`n"
        [IO.File]::WriteAllText($driver, $text, (New-Object Text.UTF8Encoding($true)))
        $result = Invoke-NativeHost -Driver $driver -ConsumerArguments $parameters -Mode 'binding'
        Check-Stage ($result.Code -eq 0 -and $result.Error.Length -eq 0 -and $result.Output.TrimEnd([char[]]"`r`n") -ceq 'NATIVE_BINDING_OK') ('fresh child named argument binding: ' + $kind)
    }
    Write-Host 'PASS: native fresh-child named argument binding'
}
function Test-NativeFailureProtocol([string]$Work) {
    $mode = 'protocol'; $message = 'Synthetic native registration fault: protocol.'
    $io = [IO.IOException]::new($message)
    $json = ConvertTo-NativeErrorRecord $io $mode
    $line = 'PROGO_NATIVE_ERROR_V1 ' + $json
    $valid = [pscustomobject]@{ Code = 1; Error = $line + "`r`n"; Output = '' }
    Check-Stage (Test-NativeFailureRecord $valid $mode $message) 'structured native direct IOException matches exactly'
    $known = [Reflection.TargetInvocationException]::new($io)
    $knownLine = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord $known $mode)
    Check-Stage (Test-NativeFailureRecord ([pscustomobject]@{ Code = 1; Error = $knownLine; Output = '' }) $mode $message) 'known invocation wrapper unwraps'
    $deep = $io
    for ($n = 0; $n -lt 5; $n++) { $deep = [Reflection.TargetInvocationException]::new($deep) }
    $foreign = [InvalidOperationException]::new($message, $io)
    $vectors = [ordered]@{
        'wrong-mode' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord $io 'other')
        'wrong-type' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord ([InvalidOperationException]::new($message)) $mode)
        'wrong-message' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord ([IO.IOException]::new($message + ' extra')) $mode)
        'wrong-case' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord ([IO.IOException]::new($message.ToUpperInvariant())) $mode)
        'formatted-message' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord ([IO.IOException]::new("Synthetic native registration fault: `r`nprotocol.")) $mode)
        'foreign-outer-with-expected-inner' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord $foreign $mode)
        'wrapper-depth' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord $deep $mode)
        'truncated' = 'PROGO_NATIVE_ERROR_V1 ' + (ConvertTo-NativeErrorRecord ([IO.IOException]::new('x' * 1025)) $mode)
        'missing' = $message
        'duplicate' = $line + "`r`n" + $line
        'malformed' = 'PROGO_NATIVE_ERROR_V1 {'
        'duplicate-key' = 'PROGO_NATIVE_ERROR_V1 ' + $json.Replace('"Schema":1', '"Schema":1,"Schema":1')
    }
    foreach ($name in $vectors.Keys) {
        $result = [pscustomobject]@{ Code = 1; Error = $vectors[$name]; Output = '' }
        Check-Stage (-not (Test-NativeFailureRecord $result $mode $message)) ('structured native matcher refuses ' + $name)
    }
    foreach ($name in @('exit-zero', 'install-success', 'uninstall-success')) {
        $result = [pscustomobject]@{ Code = 1; Error = $line; Output = '' }
        if ($name -eq 'exit-zero') { $result.Code = 0 }
        if ($name -eq 'install-success') { $result.Output = 'Install OK' }
        if ($name -eq 'uninstall-success') { $result.Output = 'Uninstall OK' }
        Check-Stage (-not (Test-NativeFailureRecord $result $mode $message)) ('valid native record still refuses ' + $name)
    }
    # A distinct finally exception must reach the external catch, not the earlier
    # foreign exception seen by the consumer's own catch. No consumer trap injection.
    $driver = Join-Path $Work 'NativeProtocolConsumer.ps1'
    $text = @'
try {
    try { throw [InvalidOperationException]::new('foreign consumer fault') }
    catch { [Console]::Out.WriteLine('CONSUMER_CATCH'); throw }
} finally {
    [Console]::Out.WriteLine('CONSUMER_FINALLY')
    throw [IO.IOException]::new('Synthetic native registration fault: protocol.')
}
'@
    [IO.File]::WriteAllText($driver, $text, (New-Object Text.UTF8Encoding($true)))
    $result = Invoke-NativeHost -Driver $driver -ConsumerArguments @{} -Mode $mode
    Check-Stage ((Test-NativeFailureRecord $result $mode $message) -and $result.Output.Contains('CONSUMER_CATCH') -and $result.Output.Contains('CONSUMER_FINALLY')) 'external wrapper records only after consumer catch/finally'
    Write-Host 'PASS: native structured error protocol and negative matcher vectors'
}
function Read-NativeSnapshot([string]$Path) {
    $key = $hive.OpenSubKey($Path)
    if ($null -eq $key) { return 'ABSENT' }
    try {
        $values = @()
        foreach ($name in @($key.GetValueNames() | Sort-Object)) {
            $value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            $values += [ordered]@{ Name = $name; Kind = $key.GetValueKind($name).ToString(); Value = $value }
        }
        return ([ordered]@{ Values = $values; Children = @($key.GetSubKeyNames() | Sort-Object) } | ConvertTo-Json -Depth 6 -Compress)
    } finally { $key.Dispose() }
}
function Assert-NativeExact($Fixture, [string]$Path) {
    $key = $hive.OpenSubKey($Path)
    Check-Stage ($null -ne $key) 'native ARP key exists after verified commit'
    try {
        $shell = Join-Path ([Environment]::GetFolderPath('System')) 'WindowsPowerShell/v1.0/powershell.exe'
        $expected = @{ DisplayName = 'ProGo'; DisplayVersion = $Fixture.Version; InstallLocation = $Fixture.Runtime;
            UninstallString = ('"' + $shell + '" -NoProfile -File "' + (Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1') + '" -Personal');
            NoModify = 1; NoRepair = 1; ProGoOwnerSid = $Fixture.OwnerSid }
        Check-Stage ($key.SubKeyCount -eq 0 -and $key.ValueCount -eq 7) 'native exact fixed key/value inventory'
        foreach ($name in $expected.Keys) {
            $kind = if ($name -in @('NoModify', 'NoRepair')) { 'DWord' } else { 'String' }
            Check-Stage ($key.GetValueKind($name).ToString() -ceq $kind -and $key.GetValue($name) -ceq $expected[$name]) ('native typed value ' + $name)
        }
    } finally { $key.Dispose() }
}
try {
    [void][IO.Directory]::CreateDirectory($work)
    Test-NativeArgumentBinding $work
    Test-NativeFailureProtocol $work
    foreach ($mode in @('success', 'registration-before', 'registry-write', 'registry-commit', 'registration-after', 'registry-delete', 'registry-delete-after', 'parent-missing', 'foreign-existing', 'foreign-type', 'foreign-extra', 'foreign-child', 'foreign-partial', 'foreign-oversize', 'changed-after-admit')) {
        $case = Join-Path $work $mode
        $Fixture = @{ Root = $work; Case = $mode; Mode = $mode; Runtime = (Join-Path $case 'program/ProGo'); Data = (Join-Path $case 'data/ProGo'); Payload = (Join-Path $case 'payload'); Menu = (Join-Path $case 'menu'); Startup = (Join-Path $case 'startup'); Source = (Join-Path $repo 'src/ApplicationShortcuts.cs'); Writes = 0; Version = $original.Version; OwnerSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value; Names = @($original.Files); Hashes = @($original.Files | ForEach-Object { $original.Hashes[$_] }); Canary = (Join-Path $case 'canary.txt') }
        foreach ($dir in @($Fixture.Runtime, $Fixture.Data, $Fixture.Payload, $Fixture.Menu, $Fixture.Startup)) { [void][IO.Directory]::CreateDirectory($dir) }
        foreach ($leaf in $original.Files) {
            $destination = Join-Path $Fixture.Payload $leaf
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
            [IO.File]::Copy((Join-Path $original.Directory $leaf), $destination)
        }
        $private = @{}
        foreach ($leaf in @('vault.enc', 'settings.json', 'private.log', 'backups/foreign.txt')) {
            $path = Join-Path $Fixture.Data $leaf; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
            [IO.File]::WriteAllText($path, 'private native fixture data'); $private[$path] = (Get-FileHash -LiteralPath $path).Hash
        }
        $parentPath = $registryPath + '\' + $mode; $target = $parentPath + '\ProGo'
        if ($mode -ne 'parent-missing') {
            $parentKey = $hive.CreateSubKey($parentPath)
            try { $parentKey.SetValue('ForeignSibling', 'preserve', [Microsoft.Win32.RegistryValueKind]::String) } finally { $parentKey.Dispose() }
        }
        if ($mode -eq 'foreign-existing') {
            $key = $hive.CreateSubKey($target)
            try { $key.SetValue('ProGoOwnerSid', $Fixture.OwnerSid); $key.SetValue('Foreign', 'keep') } finally { $key.Dispose() }
        }
        $initial = Read-NativeSnapshot $target
        $driver = Join-Path $case 'NativeInstallConsumer.ps1'
        Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $adapters $driver (Native-Prefix $Fixture)
        $installed = Invoke-NativeHost -Driver $driver -ConsumerArguments @{ Personal = $true; PayloadDirectory = $Fixture.Payload } -Mode $mode
        if ($mode -eq 'parent-missing') {
            Check-Stage ($installed.Code -ne 0 -and (Test-NativeFailureRecord $installed $mode 'Personal native registration parent failed (2).') -and -not $installed.Output.Contains('Install OK')) 'bounded native missing-parent error without fallback'
            Check-Stage ((Read-NativeSnapshot $parentPath) -ceq 'ABSENT') 'native error never creates registry ancestors'
            foreach ($leaf in $original.Files) { Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $leaf))) 'native admission failure precedes runtime writes' }
        } elseif ($mode -eq 'foreign-existing') {
            Check-Stage ($installed.Code -ne 0 -and (Test-NativeFailureRecord $installed $mode 'Personal registration is foreign or changed.') -and -not $installed.Output.Contains('Install OK')) 'copied owner SID cannot authorize foreign native registration'
            Check-Stage ((Read-NativeSnapshot $target) -ceq $initial) 'native install admission preserves foreign exact snapshot'
            foreach ($leaf in $original.Files) { Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $leaf))) 'foreign native admission precedes runtime writes' }
            $hive.DeleteSubKey($target, $true)
            $hive.DeleteSubKey($parentPath, $true)
        } else {
            $writeFault = $mode -in @('registration-before', 'registry-write', 'registry-commit', 'registration-after')
            if ($writeFault) {
                # Synthetic fixture streams only. Lengths are UTF-16 code units;
                # cap each prefix before JSON escaping, without changing the checks.
                Write-Host ('NATIVE_REGISTRATION_DIAGNOSTIC ' + ([ordered]@{
                    Mode = $mode; ChildCode = $installed.Code
                    CodeNonZero = ($installed.Code -ne 0)
                    ExpectedErrorMatched = (Test-NativeFailureRecord $installed $mode ('Synthetic native registration fault: ' + $mode + '.'))
                    InstallOkAbsent = (-not $installed.Output.Contains('Install OK'))
                    Stderr = [ordered]@{ Length = $installed.Error.Length; Truncated = ($installed.Error.Length -gt 1024);
                        Text = $installed.Error.Substring(0, [Math]::Min(1024, $installed.Error.Length)) }
                    Stdout = [ordered]@{ Length = $installed.Output.Length; Truncated = ($installed.Output.Length -gt 1024);
                        Text = $installed.Output.Substring(0, [Math]::Min(1024, $installed.Output.Length)) }
                } | ConvertTo-Json -Depth 4 -Compress))
                Check-Stage ($installed.Code -ne 0 -and (Test-NativeFailureRecord $installed $mode ('Synthetic native registration fault: ' + $mode + '.')) -and -not $installed.Output.Contains('Install OK')) 'native publication fault never reports success'
                if ($mode -eq 'registration-after') { Assert-NativeExact $Fixture $target }
                else { Check-Stage ((Read-NativeSnapshot $target) -ceq 'ABSENT') 'native partial writes rolled back without published key' }
                Check-Stage (@(Get-ProGoUninstallInventory $Fixture.Runtime).Count -gt 0 -and [IO.File]::Exists((Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'))) 'native failure retains committed runtime and retry'
                $journals = @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*').Count
                $Fixture.Mode = 'retry'
                Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $adapters $driver (Native-Prefix $Fixture)
                $retried = Invoke-NativeHost -Driver $driver -ConsumerArguments @{ Personal = $true; PayloadDirectory = $Fixture.Payload } -Mode $mode
                Check-Stage ($retried.Code -eq 0 -and $retried.Output.Contains('without reinstall') -and -not $retried.Output.Contains('PERSONAL_RUNTIME_WRITE') -and @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*').Count -eq $journals) 'native fresh retry registers without replaying runtime transaction'
            } else { Check-Stage ($installed.Code -eq 0) ('native install consumer succeeds: ' + $installed.Error) }
            Assert-NativeExact $Fixture $target
            $registered = Read-NativeSnapshot $target
            $runtimeBefore = @{}
            foreach ($entry in @(Get-ProGoUninstallInventory $Fixture.Runtime)) { $runtimeBefore[$entry.Relative] = (Get-FileHash -LiteralPath (Join-Path $Fixture.Runtime $entry.Relative)).Hash }
            if ($mode -eq 'changed-after-admit') {
                $change = Native-Prefix $Fixture
                $change += "`r`n" + $adapters + "`r`n" + @'
$context = New-PersonalFixtureContext
try {
    $context.AdmitRegistration()
    $hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryView]::Registry64)
    try {
        $path = 'SOFTWARE\ProGo.RegistryFixtures\' + (Split-Path -Leaf $Fixture.Root).Substring('ProGo-personal-transaction-'.Length) + '\' + $Fixture.Case + '\ProGo'
        $key = $hive.OpenSubKey($path, $true)
        try { $key.SetValue('NoModify', '1', [Microsoft.Win32.RegistryValueKind]::String) } finally { $key.Dispose() }
    } finally { $hive.Dispose() }
    $context.PublishRegistration()
    throw 'CHANGED_REGISTRATION_ACCEPTED'
} finally { $context.Dispose() }
'@
                $changeDriver = Join-Path $case 'ChangedRegistration.ps1'
                [IO.File]::WriteAllText($changeDriver, $change, (New-Object Text.UTF8Encoding($true)))
                $changed = Invoke-NativeHost -Driver $changeDriver -ConsumerArguments @{} -Mode $mode
                Check-Stage ($changed.Code -ne 0 -and (Test-NativeFailureRecord $changed $mode 'Personal registration is foreign or changed.') -and -not $changed.Error.Contains('CHANGED_REGISTRATION_ACCEPTED')) 'live admission snapshot cannot authorize changed typed registration'
            } elseif ($mode.StartsWith('foreign-')) {
                $key = $hive.OpenSubKey($target, $true)
                try {
                    switch ($mode) {
                        'foreign-type' { $key.SetValue('NoModify', '1', [Microsoft.Win32.RegistryValueKind]::String) }
                        'foreign-extra' { $key.SetValue('Foreign', 'keep', [Microsoft.Win32.RegistryValueKind]::String) }
                        'foreign-child' { $child = $key.CreateSubKey('ForeignChild'); try { $child.SetValue('Keep', 'foreign child') } finally { $child.Dispose() } }
                        'foreign-partial' { $key.DeleteValue('NoRepair') }
                        'foreign-oversize' { $key.SetValue('DisplayName', ('x' * 20000), [Microsoft.Win32.RegistryValueKind]::String) }
                    }
                } finally { $key.Dispose() }
            }
            $foreign = $mode.StartsWith('foreign-') -or $mode -eq 'changed-after-admit'
            $beforeRemoval = Read-NativeSnapshot $target
            $childBefore = if ($mode -eq 'foreign-child') { Read-NativeSnapshot ($target + '\ForeignChild') } else { $null }
            $retained = Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'
            $uninstall = Join-Path $case 'NativeUninstallConsumer.ps1'
            Write-StageDriver $retained '$Layout' $adapters $uninstall (Native-Prefix $Fixture)
            $removed = Invoke-NativeHost -Driver $uninstall -ConsumerArguments @{ Personal = $true } -Mode $mode
            if ($foreign) {
                Check-Stage ($removed.Code -ne 0 -and ((Test-NativeFailureRecord $removed $mode 'Personal registration is foreign or changed.') -or (Test-NativeFailureRecord $removed $mode 'Personal native registration read failed (234).')) -and -not $removed.Output.Contains('Uninstall OK')) 'foreign native registration refuses uninstall'
                Check-Stage ((Read-NativeSnapshot $target) -ceq $beforeRemoval) 'foreign typed registry snapshot preserved exactly'
                if ($mode -eq 'foreign-child') { Check-Stage ((Read-NativeSnapshot ($target + '\ForeignChild')) -ceq $childBefore) 'foreign native child content preserved exactly' }
                foreach ($leaf in $runtimeBefore.Keys) { Check-Stage ((Get-FileHash -LiteralPath (Join-Path $Fixture.Runtime $leaf)).Hash -ceq $runtimeBefore[$leaf]) 'foreign native registration preserves runtime before deletion' }
                # Explicit fixture owner cleanup, never production recursive deletion.
                $key = $hive.OpenSubKey($target, $true)
                try {
                    if ($mode -eq 'foreign-child') { $key.DeleteSubKey('ForeignChild', $true) }
                    foreach ($name in $key.GetValueNames()) { $key.DeleteValue($name) }
                } finally { $key.Dispose() }
                $hive.DeleteSubKey($target, $true)
            } else {
                if ($mode -in @('registry-delete', 'registry-delete-after')) {
                    Check-Stage ($removed.Code -ne 0 -and (Test-NativeFailureRecord $removed $mode ('Synthetic native registration fault: ' + $mode + '.')) -and -not $removed.Output.Contains('Uninstall OK')) 'native deletion fault never reports success'
                    if ($mode -eq 'registry-delete') { Check-Stage ((Read-NativeSnapshot $target) -ceq $registered) 'partial native value deletion rolls back full typed registration' }
                    else { Check-Stage ((Read-NativeSnapshot $target) -ceq 'ABSENT') 'postcommit delete fault leaves no guessed compensation' }
                    Check-Stage ([IO.File]::Exists($retained)) 'native uninstall failure retains durable retry'
                    $Fixture.Mode = 'retry'
                    Write-StageDriver $retained '$Layout' $adapters $uninstall (Native-Prefix $Fixture)
                    $removed = Invoke-NativeHost -Driver $uninstall -ConsumerArguments @{ Personal = $true } -Mode $mode
                }
                Check-Stage ($removed.Code -eq 0 -and (Read-NativeSnapshot $target) -ceq 'ABSENT' -and [IO.File]::Exists($retained)) ('native registration removed last and retry retained: ' + $removed.Error)
            }
            $parentKey = $hive.OpenSubKey($parentPath, $true)
            try {
                Check-Stage ($parentKey.SubKeyCount -eq 0 -and $parentKey.ValueCount -eq 1 -and $parentKey.GetValue('ForeignSibling') -ceq 'preserve') 'foreign parent registration values preserved'
                $parentKey.DeleteValue('ForeignSibling')
            } finally { $parentKey.Dispose() }
            $hive.DeleteSubKey($parentPath, $true)
        }
        foreach ($path in $private.Keys) { Check-Stage ((Get-FileHash -LiteralPath $path).Hash -ceq $private[$path]) 'native backend preserves profile vault settings logs and backups' }
        Write-Host ('PASS: consumed native registration ' + $mode)
    }
    $hive.DeleteSubKey($registryPath, $true)
} finally {
    $hive.Dispose()
    if ($script:StageChildrenSettled -and [IO.Directory]::Exists($work)) { Remove-ProGoInstallerOwnedDirectory $work }
    elseif (-not $script:StageChildrenSettled) { Write-Warning 'Native registration child settlement unknown; isolated fixtures retained.' }
    # Failed registry cases are deliberately retained; no recursive cleanup fallback.
}
