Set-StrictMode -Version 2.0

function Import-ProGoShortcutType {
    if (-not ('ProGo.ApplicationShortcuts' -as [type])) {
        $source = Join-Path $PSScriptRoot 'ApplicationShortcuts.cs'
        if (-not (Test-Path -LiteralPath $source)) {
            $source = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ApplicationShortcuts.cs'
        }
        Add-Type -Path $source
    }
}

function Test-ProGoExistingInstallation {
    param([string]$InstallDirectory)
    Import-ProGoShortcutType
    return [ProGo.ApplicationShortcuts]::IsExistingInstallation($InstallDirectory)
}

function Initialize-ProGoShortcuts {
    param([string]$InstallDirectory, [bool]$WasInstalled, [bool]$NoStartup, [bool]$NoStartMenu,
        [string]$ProgramsDirectory = [Environment]::GetFolderPath('Programs'),
        [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'),
        [switch]$CaptureStartupEvidence, [switch]$PreserveLegacyForOwnedOperation)
    if ($PreserveLegacyForOwnedOperation -and -not $CaptureStartupEvidence) { throw 'Legacy preparation requires captured shortcut evidence.' }
    Import-ProGoShortcutType
    $shortcuts = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
    if ($PreserveLegacyForOwnedOperation) { return $shortcuts.PrepareForOwnedLegacyOperation($WasInstalled, $NoStartup, $NoStartMenu) }
    if ($CaptureStartupEvidence) { return $shortcuts.InstallWithStartupEvidence($WasInstalled, $NoStartup, $NoStartMenu) }
    $shortcuts.Install($WasInstalled, $NoStartup, $NoStartMenu)
}
