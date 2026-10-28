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
        $installed = Invoke-StageHost $driver ('-Personal -PayloadDirectory "' + $Fixture.Payload + '"')
        if ($mode -eq 'parent-missing') {
            Check-Stage ($installed.Code -ne 0 -and $installed.Error.Contains('Personal native registration parent failed (2).') -and -not $installed.Output.Contains('Install OK')) 'bounded native missing-parent error without fallback'
            Check-Stage ((Read-NativeSnapshot $parentPath) -ceq 'ABSENT') 'native error never creates registry ancestors'
            foreach ($leaf in $original.Files) { Check-Stage (-not [IO.File]::Exists((Join-Path $Fixture.Runtime $leaf))) 'native admission failure precedes runtime writes' }
        } elseif ($mode -eq 'foreign-existing') {
            Check-Stage ($installed.Code -ne 0 -and $installed.Error.Contains('Personal registration is foreign or changed.') -and -not $installed.Output.Contains('Install OK')) 'copied owner SID cannot authorize foreign native registration'
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
                    ExpectedErrorMatched = $installed.Error.Contains('Synthetic native registration fault: ' + $mode + '.')
                    InstallOkAbsent = (-not $installed.Output.Contains('Install OK'))
                    Stderr = [ordered]@{ Length = $installed.Error.Length; Truncated = ($installed.Error.Length -gt 1024);
                        Text = $installed.Error.Substring(0, [Math]::Min(1024, $installed.Error.Length)) }
                    Stdout = [ordered]@{ Length = $installed.Output.Length; Truncated = ($installed.Output.Length -gt 1024);
                        Text = $installed.Output.Substring(0, [Math]::Min(1024, $installed.Output.Length)) }
                } | ConvertTo-Json -Depth 4 -Compress))
                Check-Stage ($installed.Code -ne 0 -and $installed.Error.Contains('Synthetic native registration fault: ' + $mode + '.') -and -not $installed.Output.Contains('Install OK')) 'native publication fault never reports success'
                if ($mode -eq 'registration-after') { Assert-NativeExact $Fixture $target }
                else { Check-Stage ((Read-NativeSnapshot $target) -ceq 'ABSENT') 'native partial writes rolled back without published key' }
                Check-Stage (@(Get-ProGoUninstallInventory $Fixture.Runtime).Count -gt 0 -and [IO.File]::Exists((Join-Path $Fixture.Runtime 'backups/Uninstall-ProGo.retry.ps1'))) 'native failure retains committed runtime and retry'
                $journals = @(Get-ChildItem -LiteralPath (Join-Path $Fixture.Runtime 'backups') -Directory -Filter 'installer-journal-*').Count
                $Fixture.Mode = 'retry'
                Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1') '$InstallDir' $adapters $driver (Native-Prefix $Fixture)
                $retried = Invoke-StageHost $driver ('-Personal -PayloadDirectory "' + $Fixture.Payload + '"')
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
                $changed = Invoke-StageHost $changeDriver
                Check-Stage ($changed.Code -ne 0 -and $changed.Error.Contains('Personal registration is foreign or changed.') -and -not $changed.Error.Contains('CHANGED_REGISTRATION_ACCEPTED')) 'live admission snapshot cannot authorize changed typed registration'
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
            $removed = Invoke-StageHost $uninstall '-Personal'
            if ($foreign) {
                Check-Stage ($removed.Code -ne 0 -and ($removed.Error.Contains('Personal registration is foreign or changed.') -or $removed.Error.Contains('Personal native registration read failed (234).')) -and -not $removed.Output.Contains('Uninstall OK')) 'foreign native registration refuses uninstall'
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
                    Check-Stage ($removed.Code -ne 0 -and $removed.Error.Contains('Synthetic native registration fault: ' + $mode + '.') -and -not $removed.Output.Contains('Uninstall OK')) 'native deletion fault never reports success'
                    if ($mode -eq 'registry-delete') { Check-Stage ((Read-NativeSnapshot $target) -ceq $registered) 'partial native value deletion rolls back full typed registration' }
                    else { Check-Stage ((Read-NativeSnapshot $target) -ceq 'ABSENT') 'postcommit delete fault leaves no guessed compensation' }
                    Check-Stage ([IO.File]::Exists($retained)) 'native uninstall failure retains durable retry'
                    $Fixture.Mode = 'retry'
                    Write-StageDriver $retained '$Layout' $adapters $uninstall (Native-Prefix $Fixture)
                    $removed = Invoke-StageHost $uninstall '-Personal'
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
