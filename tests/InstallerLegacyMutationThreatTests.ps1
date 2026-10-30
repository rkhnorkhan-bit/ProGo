# Counterexample only: ordinary metadata access in the parent's new synthetic fixture.
# No mutation protocol, deletion, privilege, ACL change or live installer action.
$script:LegacyMutationThreatChecks = 0
function Check-LegacyMutationThreat($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Legacy mutation threat failed: check=' + $Name) }
    $script:LegacyMutationThreatChecks++
    Write-Host ('PASS: installer Legacy mutation threat ' + $Name)
}
function Test-ProGoLegacyMutationThreatFixture([string]$Journal,[string]$Install,[string]$Programs,[string]$Startup) {
    $case = [IO.Path]::GetDirectoryName($Install); $work = [IO.Path]::GetDirectoryName($case)
    if ([IO.Path]::GetFileName($Install) -cne 'app' -or [IO.Path]::GetFileName($case) -cne 'complete' -or
        [IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-predelete-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Mutation threat fixture refuses unowned scope.' }
    $runtime = Join-Path $Install 'ProGo.exe'
    foreach ($path in @($work,$case,$Install,$runtime)) {
        if (([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Mutation threat fixture refuses reparse scope.' }
    }
    # .NET Framework File.SetCreationTimeUtc asks for FileAccess.Write and would test the
    # wrong sharing contract. Use a dedicated second FILE_WRITE_ATTRIBUTES handle instead.
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
    $physical = $null; $original = $null; $touched = $false
    $creation = [IO.File]::GetCreationTimeUtc($runtime); $token = [Threading.CancellationToken]::None
    $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    try {
        $physical = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
        $original = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        Check-LegacyMutationThreat ($null -ne $physical -and $null -ne $original -and $original.IsPinned -and $physical.VerifyPinned($token)) 'verified-pins-before-adversarial-metadata-write'
        # FILE_SHARE_READ does not constrain FILE_WRITE_ATTRIBUTES. This changes ONLY our own
        # fixture metadata; restoration is performed while the exact original read pins remain held.
        $touched = $true; [LegacyMetadataThreatFixture]::SetCreation($runtime,$creation.AddMinutes(1))
        Check-LegacyMutationThreat ([IO.File]::GetCreationTimeUtc($runtime) -ne $creation -and
            (Get-FileHash -LiteralPath $runtime).Hash -ceq $inventory[$runtime] -and $original.IsPinned) 'metadata-can-change-while-object-and-bytes-stay-pinned'
        Check-LegacyMutationThreat (-not $physical.VerifyPinned($token)) 'actual-origin-change-refused-at-verification'
        Check-LegacyMutationThreat (-not $physical.MutationAllowed -and -not $original.MutationAllowed -and
            -not $physical.FullRollback -and -not $original.InstallSucceeded) 'no-lease-or-observation-becomes-mutation-authority'
        [LegacyMetadataThreatFixture]::SetCreation($runtime,$creation); $touched = $false
        Check-LegacyMutationThreat ($physical.VerifyPinned($token)) 'restored-current-origin-does-not-prove-no-ABA-history'
    } catch {
        if ($_.Exception.Message -cmatch '^Legacy mutation threat failed: check=[a-z-]+$') { throw }
        $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
        throw ('Mutation threat fixture failed; safeType=' + $cause.GetType().Name)
    } finally {
        try {
            # Never restore metadata after dropping the pin that protects the exact fixture object.
            if ($touched -and $null -ne $physical) { [LegacyMetadataThreatFixture]::SetCreation($runtime,$creation) }
        } finally {
            try { if ($null -ne $original) { $original.Dispose() } }
            finally { if ($null -ne $physical) { $physical.Dispose() } }
        }
    }
    $unchanged = @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count
    foreach ($path in $inventory.Keys) {
        $unchanged = $unchanged -and [IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $inventory[$path]
        $probe = [IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose()
    }
    Check-LegacyMutationThreat ($unchanged -and [IO.File]::GetCreationTimeUtc($runtime) -eq $creation) 'private-foreign-bytes-metadata-and-cleanup-preserved'
    if ($script:LegacyMutationThreatChecks -ne 6) { throw 'Mutation threat counterexample did not execute all six checks.' }
    Write-Host 'Legacy mutation threat tests PASS: 6; global-freeze NOT_CLAIMED; deletion/power-loss NOT_RUN'
}
