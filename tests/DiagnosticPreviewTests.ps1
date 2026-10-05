param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$work = Join-Path $env:TEMP ('ProGo-diagnostic-helper-' + [guid]::NewGuid().ToString('N'))
$timer = New-Object Windows.Forms.Timer
$timer.Interval = 100
$script:seen = $false; $script:probeError = $null; $script:copyReport = $false; $script:previewText = ''
try {
    New-Item -ItemType Directory $work | Out-Null
    [IO.File]::WriteAllText((Join-Path $work 'VERSION'), '0.2.2')
    [IO.File]::WriteAllText((Join-Path $work 'update.log'), 'TRANSACTION FAILED: password=private-helper-fixture')
    $InstallDir = $work
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Update-ProGo.Core.ps1'), [ref]$tokens, [ref]$errors)
    $fn = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Copy-LogToClipboard' }, $false)
    # Same updater action, with only the installed script location supplied.
    . ([scriptblock]::Create($fn.Extent.Text.Replace('$PSScriptRoot', '$Scripts')))
    $timer.Add_Tick({
        $form = $null
        try {
            $form = @([Windows.Forms.Application]::OpenForms | Where-Object { $_.GetType().Name -eq 'DiagnosticPreviewForm' }) | Select-Object -First 1
            if ($null -eq $form) { return }
            $script:seen = $true
            $layout = $form.Controls[0]
            $preview = @($layout.Controls | Where-Object { $_ -is [Windows.Forms.TextBox] })[0]
            $script:previewText = $preview.Text
            if (-not $preview.ReadOnly -or $preview.Text.Contains('private-helper-fixture') -or -not $preview.Text.Contains('UPDATE_FAILED')) { throw 'Unsafe or empty updater preview' }
            if ($script:copyReport) {
                $actions = @($layout.Controls | Where-Object { $_ -is [Windows.Forms.FlowLayoutPanel] })[0]
                $actions.Controls[0].PerformClick()
                if ([Windows.Forms.Clipboard]::GetText() -ne $preview.Text) { throw 'Updater clipboard differs from preview' }
            }
            $form.Close(); $timer.Stop()
        } catch {
            $script:probeError = $_.Exception.Message
            if ($null -ne $form) { $form.Close() }
            $timer.Stop()
        }
    })
    [Windows.Forms.Clipboard]::SetText('unrelated clipboard fixture')
    $timer.Start(); Copy-LogToClipboard
    if (-not $script:seen -or $null -ne $script:probeError) { throw ('Installed preview failed: ' + $script:probeError) }
    if ([Windows.Forms.Clipboard]::GetText() -ne 'unrelated clipboard fixture') { throw 'Opening/cancelling updater preview changed clipboard' }
    Write-Host 'PASS: real updater action opens shared sanitized preview without automatic clipboard writes'
    $script:copyReport = $true; $script:seen = $false
    $timer.Start(); Copy-LogToClipboard
    if (-not $script:seen -or $null -ne $script:probeError) { throw ('Installed copy failed: ' + $script:probeError) }
    if ([Windows.Forms.Clipboard]::GetText() -ne $script:previewText) { throw 'Reviewed report not copied' }
    Write-Host 'PASS: explicit updater copy uses exactly the reviewed report'
    Write-Host 'Diagnostic helper tests PASS: 2'
} finally {
    $timer.Stop(); $timer.Dispose()
    if ([Windows.Forms.Clipboard]::GetText() -eq $script:previewText -or [Windows.Forms.Clipboard]::GetText() -eq 'unrelated clipboard fixture') { [Windows.Forms.Clipboard]::Clear() }
    if (Test-Path $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
