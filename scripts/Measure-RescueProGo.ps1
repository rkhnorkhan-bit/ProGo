param(
    [ValidateRange(0, 2147483647)][int]$ProcessId = 0,
    [string]$ExecutablePath,
    [ValidateRange(2, 300)][int]$DurationSeconds = 30,
    [ValidateRange(250, 5000)][int]$IntervalMilliseconds = 1000,
    # Zero means unknown. Read the actual ports from ProGo, do not assume defaults.
    [ValidateRange(0, 65535)][int]$SocksPort = 0,
    [ValidateRange(0, 65535)][int]$HttpPort = 0,
    [string]$ReportPath
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($ProcessId -eq 0 -and [string]::IsNullOrWhiteSpace($ExecutablePath)) {
    throw 'Укажите PID запущенного ProGo (-ProcessId) или выбранный файл (-ExecutablePath). Ничего не запускалось.'
}
try {
    if (-not ('ProGo.RescueMeasurement' -as [type])) {
        $measurementSource = Join-Path $PSScriptRoot 'RescueMeasurement.cs'
        if (-not (Test-Path -LiteralPath $measurementSource)) {
            $measurementSource = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\RescueMeasurement.cs'
        }
        Add-Type -Path $measurementSource -ReferencedAssemblies System.dll,System.Core.dll -ErrorAction Stop
    }
    $probe = New-Object ProGo.WindowsRescueMeasurementProbe($ProcessId, $ExecutablePath, $SocksPort, $HttpPort)
    try {
        $measurement = [ProGo.RescueMeasurement]::Collect($probe, ($DurationSeconds * 1000), $IntervalMilliseconds,
            $SocksPort, $HttpPort, [Threading.CancellationToken]::None)
        $report = [ProGo.RescueMeasurement]::Report($measurement)
    } finally { $probe.Dispose() }
    if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
        # Only this explicit caller-selected report write; no automatic directory/log.
        [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), $report, (New-Object Text.UTF8Encoding($true)))
    }
    Write-Output $report
} catch {
    # Do not expose arbitrary exception text, filesystem paths or child output.
    throw 'Не удалось завершить локальное измерение. Проверьте выбранный PID, путь к helper и права чтения. Службы, настройки и VPS не менялись.'
}
