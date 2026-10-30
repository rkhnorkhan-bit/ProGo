param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: storage lifecycle requires isolated Windows CI'; return }
# Read existing volume identities only. Never create/mount/format a volume.
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class ProGoStorageVolumeFixture {
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle, StringBuilder result, uint size, uint flags);
    public static string Identity(string path) {
        // Follow the actual directory object, including mount/junction aliases.
        using (var handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero)) {
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            var result = new StringBuilder(32768);
            uint size = GetFinalPathNameByHandle(handle, result, 32768, 1); // VOLUME_NAME_GUID
            if (size == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (size >= 32768) throw new InvalidOperationException("Volume path exceeds fixture bound.");
            string value = result.ToString(); int end = value.IndexOf('}');
            if (!value.StartsWith(@"\\?\Volume{", StringComparison.OrdinalIgnoreCase) || end < 0)
                throw new InvalidOperationException("No actual volume GUID identity.");
            return value.Substring(0, end + 1).ToUpperInvariant();
        }
    }
}
'@
$probe = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-storage-data-' + [guid]::NewGuid().ToString('N'))
$storage = $null; $complete = $false
try {
    New-Item -ItemType Directory -Path $probe -ErrorAction Stop | Out-Null
    try { $dataIdentity = [ProGoStorageVolumeFixture]::Identity($probe) }
    catch { Write-Host 'BLOCKER: data volume identity unavailable; different-volume lifecycle NOT_RUN.'; throw }
    if ([ProGoStorageVolumeFixture]::Identity($env:TEMP) -cne $dataIdentity) { throw 'BLOCKER: fixture temp bases differ; different-volume lifecycle NOT_RUN.' }
    foreach ($drive in [IO.DriveInfo]::GetDrives()) {
        if (-not $drive.IsReady -or $drive.DriveType -ne [IO.DriveType]::Fixed) { continue }
        try { $identity = [ProGoStorageVolumeFixture]::Identity($drive.RootDirectory.FullName) }
        catch { Write-Host 'Storage candidate has no usable volume identity.'; continue }
        if ($identity -ceq $dataIdentity) { continue }
        $leaf = 'ProGo-storage-fixture-' + [guid]::NewGuid().ToString('N')
        $candidate = Join-Path $drive.RootDirectory.FullName $leaf
        try { New-Item -ItemType Directory -Path $candidate -ErrorAction Stop | Out-Null }
        catch [UnauthorizedAccessException] { continue }
        $storage = $candidate
        [IO.File]::WriteAllText((Join-Path $storage 'owner.txt'), $leaf)
        break
    }
    if ($null -eq $storage) {
        Write-Host 'BLOCKER: no second writable existing volume identity; different-volume lifecycle NOT_RUN.'
        throw 'Mandatory different-volume lifecycle NOT_RUN.'
    }
    $runtimeIdentity = [ProGoStorageVolumeFixture]::Identity($storage)
    if ($runtimeIdentity -ceq $dataIdentity) { throw 'Volume identities are equal; different-volume lifecycle NOT_RUN.' }
    Write-Host ('STORAGE_VOLUME data=' + $dataIdentity + '; runtime=' + $runtimeIdentity)
    # Reuse complete actual consumer fixtures, including their partial-write/Undo,
    # private/foreign preservation and retained recovery evidence assertions.
    $shell = Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe'
    foreach ($suite in @('InstallerLifecycleTests.ps1','RepairSplitTests.ps1','RestoreSplitTests.ps1')) {
        # Fresh hosts preserve each fixture's existing type-loading/process seams.
        & $shell -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot $suite) -Scripts $Scripts -StorageRoot $storage
        if ($LASTEXITCODE -ne 0) { throw ('Storage consumer suite failed: ' + $suite) }
    }
    if ([ProGoStorageVolumeFixture]::Identity($probe) -cne $dataIdentity -or
        [ProGoStorageVolumeFixture]::Identity($storage) -cne $runtimeIdentity) { throw 'Fixture volume identity changed.' }
    $complete = $true
} finally {
    if ($complete -and $null -ne $storage) { Remove-Item -LiteralPath $storage -Recurse -Force }
    elseif ($null -ne $storage) { Write-Warning ('Storage fixture evidence retained: ' + $storage) }
    if ([IO.Directory]::Exists($probe)) { Remove-Item -LiteralPath $probe -Recurse -Force }
}
if ($complete) { Write-Host 'PASS: actual different-volume Install/Uninstall/Repair/Restore; ACL denial and restoration verified' }
