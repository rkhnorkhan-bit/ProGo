# Isolated live-lease restore; private bounded fault selector never supplies authority or a path.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw 'Restore prerequisite helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
    if (-not ('LegacyMetadataThreatFixture' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class LegacyMetadataThreatFixture
{
    [DllImport("kernel32.dll", EntryPoint="CreateFileW", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern SafeFileHandle Open(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool SetFileTime(SafeFileHandle file, ref System.Runtime.InteropServices.ComTypes.FILETIME creation, IntPtr access, IntPtr write);
    public static void SetCreation(string path, DateTime value)
    {
        using (var handle = Open(path, 0x100u, 7, IntPtr.Zero, 3, 0x00200080u, IntPtr.Zero)) {
            if (handle.IsInvalid) throw new IOException("Synthetic metadata handle unavailable.");
            long ticks = value.ToFileTimeUtc();
            var time = new System.Runtime.InteropServices.ComTypes.FILETIME {
                dwLowDateTime = unchecked((int)ticks), dwHighDateTime = unchecked((int)(ticks >> 32))
            };
            if (!SetFileTime(handle, ref time, IntPtr.Zero, IntPtr.Zero)) throw new IOException("Synthetic metadata write refused.");
        }
    }
}
'@
    }
# Reflection has no PowerShell method binder: normalize inside CLR, then check actual CLR types.
if (-not ('LegacyRestoreReflectionFixture' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Reflection;
using System.Threading;
public static class LegacyRestoreReflectionFixture
{
    private static int probeCalls;
    private static object Unwrap(object value)
    {
        for (int i = 0; value != null && value.GetType().FullName == "System.Management.Automation.PSObject"; i++) {
            if (i == 8) throw new ArgumentException("Synthetic reflection wrapper depth exceeded.");
            value = value.GetType().GetProperty("BaseObject").GetValue(value, null);
        }
        return value;
    }
    public static object InvokeChecked(MethodInfo method, object target, object[] arguments)
    {
        if (method == null || arguments == null) throw new ArgumentException("Reflection method or arguments missing.");
        target = Unwrap(target);
        if (!method.IsStatic && (target == null || target.GetType() != method.DeclaringType))
            throw new ArgumentException("Reflection target CLR type mismatch.");
        var parameters = method.GetParameters();
        if (parameters.Length != arguments.Length) throw new ArgumentException("Reflection argument count mismatch.");
        var clr = new object[arguments.Length];
        for (int i = 0; i < clr.Length; i++) {
            clr[i] = Unwrap(arguments[i]);
            // All restore fault arguments are non-null, exact declared CLR types; no Convert/ChangeType.
            if (clr[i] == null || clr[i].GetType() != parameters[i].ParameterType)
                throw new ArgumentException("Reflection argument CLR type mismatch.");
        }
        return method.Invoke(target, clr);
    }
    public static object ReadFieldChecked(FieldInfo field, object target)
    {
        target = Unwrap(target);
        if (field == null || target == null || target.GetType() != field.DeclaringType)
            throw new ArgumentException("Reflection field target CLR type mismatch.");
        return field.GetValue(target);
    }
    private static bool Probe(CancellationTokenSource source, CancellationToken token)
    { probeCalls++; return token == source.Token && token.CanBeCanceled && !source.IsCancellationRequested; }
    public static bool WrapperRegression()
    {
        Type ps = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            ps = assembly.GetType("System.Management.Automation.PSObject", false);
            if (ps != null) break;
        }
        if (ps == null) return false;
        var probe = typeof(LegacyRestoreReflectionFixture).GetMethod("Probe", BindingFlags.NonPublic | BindingFlags.Static);
        using (var source = new CancellationTokenSource()) {
            object wrappedSource = Activator.CreateInstance(ps, new object[] { source });
            object wrappedToken = Activator.CreateInstance(ps, new object[] { source.Token });
            if (wrappedSource.GetType() != ps || wrappedToken.GetType() != ps) return false;
            probeCalls = 0; bool oldRefused = false;
            try { probe.Invoke(null, new object[] { wrappedSource, source.Token }); }
            catch (ArgumentException) { oldRefused = true; }
            if (!oldRefused || probeCalls != 0) return false;
            bool wrongTypeRefused = false;
            try { InvokeChecked(probe, null, new object[] { source, "synthetic wrong token type" }); }
            catch (ArgumentException) { wrongTypeRefused = true; }
            if (!wrongTypeRefused || probeCalls != 0) return false;
            object observed = InvokeChecked(probe, null, new object[] { wrappedSource, wrappedToken });
            return observed is bool && (bool)observed && probeCalls == 1;
        }
    }
}
'@
}
if (-not [LegacyRestoreReflectionFixture]::WrapperRegression()) { throw 'CLR reflection wrapper regression failed.' }
Write-Host 'Legacy restore reflection regression PASS: PSObject refused before call; unwrapped exact CLR arguments invoked once'
function Check-OwnedRestore($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Owned Legacy restore failed: case=' + $scenario + '; check=' + $Name) }
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-restore-' + [Guid]::NewGuid().ToString('N'))
$scenario = 'initial'; $stage = $null; $physical = $null; $lease = $null; $cancel = $null; $junction = $null; $process = $null
$outTask = $null; $errTask = $null; $metadata = $null; $creation = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $restoreMethod = [ProGo.ApplicationShortcuts].GetMethod('RestoreLegacyOwnedOnce',[Reflection.BindingFlags]'NonPublic,Instance')
    Check-OwnedRestore ($null -ne $restoreMethod) 'private fault boundary exists'
    $cases = @('complete','write','short-write','flush','readback','readback-throw','cancel-before','cancel-created',
        'cancel-written','cancel-flushed','cancel-readback','foreign','equal-byte','reparse','source-changed','source-disposed',
        'lease-disposed','physical-disposed','result-write','result-flush','result-readback','result-foreign')
    foreach ($scenario in $cases) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        [byte[]]$expected = [IO.File]::ReadAllBytes($legacy)
        [IO.File]::WriteAllBytes((Join-Path $menu 'ProGo.lnk'),$expected)
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
        [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $journal = $backup.Journal.Directory; $op = $reference.Source.Operation.ToString('N')
        $sourcePath = Join-Path (Join-Path $install 'backups') ('installer-legacy-source-' + $op + '.bin')
        $resultPath = Join-Path (Join-Path $install 'backups') ('installer-legacy-restored-' + $op + '.bin')
        $physical = Open-ProGoInstallerLegacyPhysicalPins $journal $install $programs $startup
        $lease = Open-ProGoInstallerLegacyDeletionPins $journal $install $programs $startup
        $cancel = New-Object Threading.CancellationTokenSource
        $accepted = $lease.DeleteOwnedOnce($physical,[Threading.CancellationToken]::None)
        $receipt = $lease.RecordAcceptedOutcome($physical,[Threading.CancellationToken]::None)
        Check-OwnedRestore ($accepted.OperationAccepted -and $receipt.DurableOutcomeRecorded -and $lease.VerifyAcceptedOutcome($physical)) 'real same-live accepted operation and source graph'
        $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $foreignBytes = [Text.Encoding]::UTF8.GetBytes('synthetic foreign restore conflict')
        if ($scenario -ceq 'foreign') { [IO.File]::WriteAllBytes($legacy,$foreignBytes) }
        if ($scenario -ceq 'equal-byte') { [IO.File]::WriteAllBytes($legacy,$expected) }
        if ($scenario -ceq 'result-foreign') { [IO.File]::WriteAllBytes($resultPath,$foreignBytes) }
        if ($scenario -ceq 'reparse') {
            $target = Join-Path $case 'retained-junction-target'; New-Item -ItemType Directory -Path $target | Out-Null
            [IO.File]::WriteAllBytes((Join-Path $target 'foreign.bin'),$foreignBytes)
            $junction = $legacy
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = $env:ComSpec; $info.Arguments = '/d /c mklink /J "' + $junction + '" "' + $target + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info)
            $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(5000)) { throw 'Owned restore junction creation exceeded bounded wait.' }
            Check-OwnedRestore ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0) 'own junction subprocess settles'
            $process.Dispose(); $process = $null
            Check-OwnedRestore (([IO.File]::GetAttributes($junction) -band [IO.FileAttributes]::ReparsePoint) -ne 0) 'real reparse conflict'
        }
        if ($scenario -ceq 'source-changed') {
            $metadata = $sourcePath; $creation = [IO.File]::GetCreationTimeUtc($metadata)
            [LegacyMetadataThreatFixture]::SetCreation($metadata,$creation.AddMinutes(1))
        }
        if ($scenario -ceq 'source-disposed') {
            $field = $lease.GetType().GetField('evidence',[Reflection.BindingFlags]'NonPublic,Instance')
            $streams = [LegacyRestoreReflectionFixture]::ReadFieldChecked($field,$lease)
            $streams[0].Dispose()
        }
        if ($scenario -ceq 'lease-disposed') { $lease.Dispose() }
        if ($scenario -ceq 'physical-disposed') { $physical.Dispose() }
        if ($scenario -ceq 'cancel-before') { $cancel.Cancel() }
        $fault = if ($scenario -in @('write','short-write','flush','readback','readback-throw','cancel-created','cancel-written','cancel-flushed','cancel-readback','result-write','result-flush','result-readback')) { $scenario } else { $null }
        if ($null -ne $fault) { $result = [LegacyRestoreReflectionFixture]::InvokeChecked($restoreMethod,$manager,[object[]]@($lease,$physical,$cancel.Token,$fault,$cancel)) }
        else { $result = $lease.RestoreOwnedOnce($physical,$cancel.Token) }
        Check-OwnedRestore ([object]::ReferenceEquals($accepted,$result.AcceptedResult) -and -not $result.WholeRollbackVerified -and -not $result.InstallSucceeded -and -not $result.RestartAuthority) 'acceptance retained without whole success or restart authority'
        $blocked = $scenario -in @('cancel-before','foreign','equal-byte','reparse','source-changed','source-disposed','lease-disposed','physical-disposed')
        Check-OwnedRestore ($result.Created -eq (-not $blocked)) 'exact creation truth'
        if ($blocked) {
            Check-OwnedRestore (-not $result.Attempted -and $result.BytesWritten -eq 0 -and -not $result.Flushed -and -not $result.ReadbackVerified -and -not $result.DurableResultRecorded) 'blocked never claims a write'
        } else {
            $written = if ($scenario -in @('write','cancel-created')) { 0 } elseif ($scenario -ceq 'short-write') { [Math]::Max(1,[int][Math]::Floor($expected.Length / 2)) } else { $expected.Length }
            $flushed = $scenario -notin @('write','short-write','flush','cancel-created','cancel-written')
            $verified = $scenario -in @('complete','cancel-readback','result-write','result-flush','result-readback','result-foreign')
            Check-OwnedRestore ($result.Attempted -and $result.BytesWritten -eq $written -and $result.Flushed -eq $flushed -and $result.ReadbackVerified -eq $verified) 'real write flush readback progress preserved across faults'
            Check-OwnedRestore ([IO.File]::Exists($legacy) -and (Get-Item -LiteralPath $legacy).Length -eq $written) 'partial or complete created artifact retained'
        }
        Check-OwnedRestore ($result.DurableResultRecorded -eq ($scenario -ceq 'complete') -and $lease.VerifyRestoredOutcome($physical) -eq ($scenario -ceq 'complete')) 'separate durable restore result and post verifier'
        if ($scenario -ceq 'foreign') { Check-OwnedRestore ([Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($foreignBytes)) 'foreign bytes retained' }
        if ($scenario -ceq 'equal-byte') { Check-OwnedRestore ([Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($expected)) 'equal bytes never adopted' }
        if ($scenario -ceq 'result-foreign') { Check-OwnedRestore ([Convert]::ToBase64String([IO.File]::ReadAllBytes($resultPath)) -ceq [Convert]::ToBase64String($foreignBytes)) 'foreign result never overwritten' }
        if ($scenario -ceq 'reparse') {
            Check-OwnedRestore ([IO.File]::ReadAllText((Join-Path $target 'foreign.bin')) -ceq 'synthetic foreign restore conflict') 'junction target preserved'
            [IO.Directory]::Delete($junction); $junction = $null
        }
        # Retry every completed/faulted/blocked call, even with cancellation, without replay or cleanup.
        $after = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $after[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $cancel.Cancel(); $again = $lease.RestoreOwnedOnce($physical,$cancel.Token)
        Check-OwnedRestore ([object]::ReferenceEquals($result,$again)) 'one-shot cached result before cancellation'
        Check-OwnedRestore (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $after.Count) 'retry creates no extra artifact'
        foreach ($path in $after.Keys) { Check-OwnedRestore ((Get-FileHash -LiteralPath $path).Hash -ceq $after[$path]) 'retry preserves every partial and foreign byte' }
        if ($scenario -ceq 'complete') {
            Check-OwnedRestore ($result.RestoredOrigin -cne $accepted.OriginalOrigin -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($expected)) 'new identity with exact original bytes'
            # Post verifier must detect native replacement, creation metadata, byte and receipt changes.
            $saved = Join-Path $case 'retained-restored'; $savedReceipt = Join-Path $case 'retained-result'
            [IO.File]::Move($legacy,$saved); [IO.File]::WriteAllBytes($legacy,$expected)
            [IO.File]::SetCreationTimeUtc($legacy,[IO.File]::GetCreationTimeUtc($saved))
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'same-byte foreign native object refused'
            [IO.File]::Delete($legacy); [IO.File]::Move($saved,$legacy)
            Check-OwnedRestore ($lease.VerifyRestoredOutcome($physical)) 'original restored object still verifiable'
            $stamp = [IO.File]::GetCreationTimeUtc($legacy); [LegacyMetadataThreatFixture]::SetCreation($legacy,$stamp.AddMinutes(1))
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'changed creation refused'
            [LegacyMetadataThreatFixture]::SetCreation($legacy,$stamp)
            [IO.File]::WriteAllText($legacy,'synthetic changed restored bytes')
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'changed restored bytes refused'
            [IO.File]::WriteAllBytes($legacy,$expected)
            [byte[]]$resultBytes = [IO.File]::ReadAllBytes($resultPath)
            [IO.File]::Move($resultPath,$savedReceipt); [IO.File]::WriteAllBytes($resultPath,$resultBytes)
            [IO.File]::SetCreationTimeUtc($resultPath,[IO.File]::GetCreationTimeUtc($savedReceipt))
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'same-byte foreign result object refused'
            [IO.File]::Delete($resultPath); [IO.File]::Move($savedReceipt,$resultPath)
            [IO.File]::WriteAllText($resultPath,'synthetic changed receipt')
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'tampered result refused'
            [IO.File]::WriteAllBytes($resultPath,$resultBytes)
            Check-OwnedRestore ($lease.VerifyRestoredOutcome($physical)) 'verified result restored only by own fixture'
            $sourceStamp = [IO.File]::GetCreationTimeUtc($sourcePath)
            [LegacyMetadataThreatFixture]::SetCreation($sourcePath,$sourceStamp.AddMinutes(1))
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'changed retained source origin blocks post verifier'
            [LegacyMetadataThreatFixture]::SetCreation($sourcePath,$sourceStamp)
            Check-OwnedRestore ($lease.VerifyRestoredOutcome($physical)) 'current source graph verified again'
            Check-OwnedRestore ($null -eq (Restore-ProGoInstallerLegacyReferenceEvidence $journal $install $programs $startup)) 'saved restore result grants no path-only restart authority'
            $lease.Dispose()
            Check-OwnedRestore (-not $lease.VerifyRestoredOutcome($physical)) 'lost live proofs remain blocked'
        }
        if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$creation); $metadata = $null }
        $lease.Dispose(); $lease = $null; $physical.Dispose(); $physical = $null; $cancel.Dispose(); $cancel = $null
        foreach ($path in $inventory.Keys) { Check-OwnedRestore ((Get-FileHash -LiteralPath $path).Hash -ceq $inventory[$path]) 'original evidence runtime private foreign bytes unchanged' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy owned restore ' + $scenario)
    }
    Write-Host ('Legacy owned restore tests PASS: ' + $cases.Count + '; actual isolated Write/short-write/Flush/readback/cancellation/conflict faults; whole rollback/restart recovery/power-loss NOT_RUN')
} finally {
    try {
        if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$creation) }
    } finally {
        try { if ($null -ne $lease) { $lease.Dispose() } }
        finally { if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } }
    }
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Owned junction process did not settle.' } }
            if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Owned junction streams did not drain.' }
        } finally { $process.Dispose() }
    }
    if ($null -ne $junction -and [IO.Directory]::Exists($junction)) {
        if ([IO.Path]::GetFullPath($junction) -cne $legacy -or -not ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Restore cleanup refuses changed junction.' }
        [IO.Directory]::Delete($junction)
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ([IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-restore-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Restore cleanup refuses unowned scope.' }
        foreach ($path in @((Get-Item -LiteralPath $work -Force)) + @(Get-ChildItem -LiteralPath $work -Recurse -Force)) { if ($path.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Restore cleanup refuses links.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
