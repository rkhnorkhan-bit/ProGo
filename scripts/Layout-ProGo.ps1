# Read-only layout selection. This is not an authority for privileged mutation.
function Assert-ProGoLayoutPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    for ($probe = $full; $probe; $probe = [IO.Path]::GetDirectoryName($probe)) {
        if ([IO.File]::Exists($probe) -or [IO.Directory]::Exists($probe)) {
            if (([IO.File]::GetAttributes($probe) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Layout refuses a reparse point.'
            }
        } else { throw 'Layout path is missing or inaccessible.' }
    }
    return $full
}

# Internal calculation shared with isolated tests. Confirmation is deliberately not
# exposed by the production entrypoint: a boolean is not a trusted installation identity.
function Resolve-ProGoLayoutCore {
    param([string]$DataBase, [string]$ProgramBase, [string]$RuntimeRoot,
          [bool]$SeparateIdentityConfirmed = $false, [switch]$AllowMissingExecutable)
    if ([string]::IsNullOrWhiteSpace($DataBase) -or [string]::IsNullOrWhiteSpace($ProgramBase) -or
        [string]::IsNullOrWhiteSpace($RuntimeRoot)) { throw 'Layout roots are required.' }
    $data = [IO.Path]::GetFullPath((Join-Path $DataBase 'ProGo')).TrimEnd('\')
    $program = [IO.Path]::GetFullPath((Join-Path $ProgramBase 'ProGo')).TrimEnd('\')
    $runtime = [IO.Path]::GetFullPath($RuntimeRoot).TrimEnd('\')
    if ($data.Equals($program, [StringComparison]::OrdinalIgnoreCase) -or
        $program.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $data.StartsWith($program + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Layout roots overlap.'
    }
    if ($runtime.Equals($data, [StringComparison]::OrdinalIgnoreCase)) { $kind = 'Legacy' }
    elseif ($runtime.Equals($program, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not $SeparateIdentityConfirmed) { throw 'Separate runtime identity is not confirmed.' }
        $kind = 'Separate'
    } else { throw 'Foreign runtime root.' }
    [void](Assert-ProGoLayoutPath $data)
    [void](Assert-ProGoLayoutPath $runtime)
    $exe = Join-Path $runtime 'ProGo.exe'
    try { $attributes = [IO.File]::GetAttributes($exe) }
    catch [IO.FileNotFoundException] {
        if (-not $AllowMissingExecutable) { throw 'Layout path is missing or inaccessible.' }
        # This observation is read-only, not authority to create or overwrite.
        return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = $exe; ExecutablePresent = $false }
    }
    if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Layout refuses a reparse point.' }
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { throw 'Runtime executable is not a regular file.' }
    [void](Assert-ProGoLayoutPath $exe)
    try { $identity = [Reflection.AssemblyName]::GetAssemblyName($exe) }
    catch { throw 'Runtime executable is not a valid managed payload.' }
    if ($identity.Name -cne 'ProGo') { throw 'Foreign runtime executable.' }
    return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = $exe; ExecutablePresent = $true }
}

# Recovery is structural preflight only. Repair's existing lease, admission,
# journal, hash and rollback checks remain the authority for its writes.
function Assert-ProGoRecoveryDirectory([string]$Path, [bool]$AllowMissingLeaf) {
    $full = [IO.Path]::GetFullPath($Path)
    $first = $true
    for ($probe = $full; $probe; $probe = [IO.Path]::GetDirectoryName($probe)) {
        try { $attributes = [IO.File]::GetAttributes($probe) }
        catch [IO.FileNotFoundException] { if ($first -and $AllowMissingLeaf) { $first = $false; continue }; throw }
        catch [IO.DirectoryNotFoundException] { if ($first -and $AllowMissingLeaf) { $first = $false; continue }; throw }
        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Recovery layout refuses a reparse point.' }
        if (($attributes -band [IO.FileAttributes]::Directory) -eq 0) { throw 'Recovery layout requires directories.' }
        $first = $false
    }
    return $full
}

function Resolve-ProGoRecoveryLayoutCore {
    param([string]$DataBase, [string]$ProgramBase, [string]$RuntimeRoot,
          [bool]$SeparateIdentityConfirmed = $false)
    if ([string]::IsNullOrWhiteSpace($DataBase) -or [string]::IsNullOrWhiteSpace($ProgramBase) -or [string]::IsNullOrWhiteSpace($RuntimeRoot)) { throw 'Layout roots are required.' }
    $data = [IO.Path]::GetFullPath((Join-Path $DataBase 'ProGo')).TrimEnd('\')
    $program = [IO.Path]::GetFullPath((Join-Path $ProgramBase 'ProGo')).TrimEnd('\')
    $runtime = [IO.Path]::GetFullPath($RuntimeRoot).TrimEnd('\')
    if ($data.Equals($program, [StringComparison]::OrdinalIgnoreCase) -or $program.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase) -or $data.StartsWith($program + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Layout roots overlap.' }
    if ($runtime.Equals($data, [StringComparison]::OrdinalIgnoreCase)) { $kind = 'Legacy' }
    elseif ($runtime.Equals($program, [StringComparison]::OrdinalIgnoreCase) -and $SeparateIdentityConfirmed) { $kind = 'Separate' }
    else { throw 'Recovery runtime root is foreign or unconfirmed.' }
    [void](Assert-ProGoRecoveryDirectory $data $true)
    [void](Assert-ProGoRecoveryDirectory $runtime $true)
    # Inspect an existing EXE without interpreting its contents. No suppressed
    # Exists result may authorize an inaccessible, redirected or directory leaf.
    try { $attributes = [IO.File]::GetAttributes((Join-Path $runtime 'ProGo.exe')) }
    catch [IO.FileNotFoundException] { $attributes = $null }
    catch [IO.DirectoryNotFoundException] { $attributes = $null }
    if ($null -ne $attributes -and ($attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::Directory))) { throw 'Recovery executable is redirected or a directory.' }
    return [pscustomobject]@{ Kind = $kind; DataRoot = $data; RuntimeRoot = $runtime; Executable = (Join-Path $runtime 'ProGo.exe') }
}

function Resolve-ProGoRecoveryLayout {
    $dataBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $programBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if ([string]::IsNullOrWhiteSpace($dataBase) -or [string]::IsNullOrWhiteSpace($programBase)) { throw 'Windows known folders are unavailable.' }
    return Resolve-ProGoRecoveryLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot (Join-Path $dataBase 'ProGo')
}

function Resolve-ProGoLayout {
    param([switch]$AllowMissingExecutable)
    # Known folders come from Windows, never from environment overrides or metadata.
    $dataBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $programBase = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if ([string]::IsNullOrWhiteSpace($dataBase) -or [string]::IsNullOrWhiteSpace($programBase)) {
        throw 'Windows known folders are unavailable.'
    }
    $legacy = Join-Path $dataBase 'ProGo'
    return Resolve-ProGoLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot $legacy -AllowMissingExecutable:$AllowMissingExecutable
}
