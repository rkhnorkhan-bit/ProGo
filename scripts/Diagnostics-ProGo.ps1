function Show-ProGoDiagnostics([string]$Root, [string]$RuntimeRoot = $Root) {
    # Local installed source shared with the app; never evaluate downloaded text.
    try {
        if (-not ('ProGo.DiagnosticPreview' -as [type])) {
            $sourceRoot = $PSScriptRoot
            if (-not (Test-Path (Join-Path $sourceRoot 'DiagnosticReport.cs'))) { $sourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src' }
            $sources = @('DiagnosticReport.cs','DiagnosticPreview.cs','UpdateInstallSession.cs','UiTheme.cs','BrandIcon.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
            Add-Type -Path $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll -ErrorAction Stop
        }
        # The owned preview worker reads the bounded installed VERSION; Click
        # never performs this file read or treats a stale cancelled value as ready.
        $diagnosticApi = 'ProGo.DiagnosticPreview' -as [type]
        if ($null -ne $diagnosticApi.GetMethod('ShowInstalledLayout', [type[]]@([string], [string]))) {
            [ProGo.DiagnosticPreview]::ShowInstalledLayout($Root, $RuntimeRoot)
        } elseif ($Root -ieq $RuntimeRoot -and $null -ne $diagnosticApi.GetMethod('ShowInstalled', [type[]]@([string]))) {
            [ProGo.DiagnosticPreview]::ShowInstalled($Root)
        } else {
            # A running updater may retain the previous API assembly after its
            # files were replaced. Do not claim its old version or read on Click.
            [ProGo.DiagnosticPreview]::Show($Root, '')
        }
    } catch {
        # No raw-file or raw-error fallback may escape through the export action.
        Write-Host 'Не удалось открыть предпросмотр диагностики. Личный журнал доступен на этом компьютере.'
    }
}
