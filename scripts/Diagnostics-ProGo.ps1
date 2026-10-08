function Show-ProGoDiagnostics([string]$Root) {
    # Local installed source shared with the app; never evaluate downloaded text.
    try {
        if (-not ('ProGo.DiagnosticPreview' -as [type])) {
            $sourceRoot = $PSScriptRoot
            if (-not (Test-Path (Join-Path $sourceRoot 'DiagnosticReport.cs'))) { $sourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src' }
            $sources = @('DiagnosticReport.cs','DiagnosticPreview.cs','UpdateInstallSession.cs','UiTheme.cs','BrandIcon.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
            Add-Type -Path $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll -ErrorAction Stop
        }
        $version = ''
        $versionPath = Join-Path $Root 'VERSION'
        try { if (Test-Path $versionPath) {
            # VERSION is untrusted input too; avoid an unlimited file read.
            $reader = [IO.File]::OpenText($versionPath)
            try { $buffer = New-Object char[] 32; $count = $reader.Read($buffer, 0, $buffer.Length); $version = (-join $buffer[0..([Math]::Max(0, $count - 1))]).Trim() } finally { $reader.Dispose() }
        } } catch { $version = '' }
        [ProGo.DiagnosticPreview]::Show($Root, $version)
    } catch {
        # No raw-file or raw-error fallback may escape through the export action.
        Write-Host 'Не удалось открыть предпросмотр диагностики. Личный журнал доступен на этом компьютере.'
    }
}
