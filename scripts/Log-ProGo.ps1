# Definitions only: no filesystem writes or compilation before maintenance gates.
function Write-ProGoLog([string]$Path, [string]$Message) {
    try {
        if (-not ('ProGo.BoundedLog' -as [type])) {
            $source = Join-Path $PSScriptRoot 'BoundedLog.cs'
            if (-not (Test-Path -LiteralPath $source)) {
                $source = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\BoundedLog.cs'
            }
            Add-Type -Path $source -ErrorAction Stop
        }
        [void][ProGo.BoundedLog]::TryWrite($Path, $Message)
    } catch {
        # Missing helper, file lock or disk failure must not hide the real result.
        # Never fall back to an unbounded append.
    }
}
