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
        [string]$StartupDirectory = [Environment]::GetFolderPath('Startup'))
    Import-ProGoShortcutType
    $shortcuts = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
    $shortcuts.Install($WasInstalled, $NoStartup, $NoStartMenu)
}
