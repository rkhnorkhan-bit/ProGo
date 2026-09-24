param(
    [int]$WaitPid = 0,
    [string]$SourceZipUrl = "https://github.com/rkhnorkhan-bit/ProGo/archive/refs/heads/main.zip",
    [switch]$NoLaunch
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$UpdateLog = Join-Path $InstallDir "progo-update.log"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$WorkDir = Join-Path $InstallDir ("update-" + $Timestamp)

function Write-UpdateLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $UpdateLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Fail($Message) {
    Write-UpdateLog ("ERROR: " + $Message)
    throw $Message
}

function Wait-ProGoExit($Pid, $TimeoutMs) {
    if ($Pid -le 0) { return }

    Write-UpdateLog "Waiting for ProGo process to exit: PID $Pid"
    try {
        $process = Get-Process -Id $Pid -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            [void]$process.WaitForExit($TimeoutMs)
        }
    } catch {
        Write-UpdateLog "Wait process warning: $($_.Exception.Message)"
    }
}

function Stop-ExistingProGoProcesses($ExceptPid) {
    $processes = @(Get-Process -Name "ProGo" -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $ExceptPid })
    if ($processes.Count -eq 0) {
        Write-UpdateLog "No running ProGo processes found."
        return
    }

    foreach ($process in $processes) {
        Write-UpdateLog "Stopping old ProGo process: PID $($process.Id)"
        try {
            if ($process.MainWindowHandle -ne 0) {
                [void]$process.CloseMainWindow()
                [void]$process.WaitForExit(5000)
            }
        } catch {
            Write-UpdateLog "Graceful stop warning for PID $($process.Id): $($_.Exception.Message)"
        }

        try {
            $stillRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($null -ne $stillRunning) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                Write-UpdateLog "Forced old ProGo process stop: PID $($process.Id)"
            }
        } catch {
            Write-UpdateLog "Forced stop warning for PID $($process.Id): $($_.Exception.Message)"
        }
    }
}

function Wait-FileUnlocked($Path, $TimeoutSeconds) {
    if (-not (Test-Path $Path)) { return }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
            return
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Fail "Timed out waiting for file unlock: $Path"
}

function Start-UpdatedProGo($ExePath) {
    if ($NoLaunch) {
        Write-UpdateLog "Launch skipped by -NoLaunch."
        return
    }

    if (-not (Test-Path $ExePath)) {
        Fail "Cannot launch ProGo, file not found: $ExePath"
    }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-UpdateLog "Starting updated ProGo, attempt $attempt..."
        try {
            $process = Start-Process -FilePath $ExePath -WorkingDirectory $InstallDir -PassThru
            Start-Sleep -Seconds 2

            $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($null -ne $running -and -not $running.HasExited) {
                Write-UpdateLog "Updated ProGo started: PID $($process.Id)"
                return
            }

            Write-UpdateLog "Started ProGo process exited too early."
        } catch {
            Write-UpdateLog "Launch attempt $attempt failed: $($_.Exception.Message)"
        }

        Start-Sleep -Seconds 1
    }

    Fail "Updated ProGo did not stay running after restart attempts."
}

Write-UpdateLog "ProGo update started."

$UpdaterPid = $PID
Wait-ProGoExit -Pid $WaitPid -TimeoutMs 30000
Stop-ExistingProGoProcesses -ExceptPid $UpdaterPid

$Exe = Join-Path $InstallDir "ProGo.exe"
Wait-FileUnlocked -Path $Exe -TimeoutSeconds 30

New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
$ZipPath = Join-Path $WorkDir "ProGo-main.zip"

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
} catch {
    Write-UpdateLog "TLS setup warning: $($_.Exception.Message)"
}

Write-UpdateLog "Downloading source from GitHub..."
Invoke-WebRequest -Uri $SourceZipUrl -OutFile $ZipPath -UseBasicParsing

Write-UpdateLog "Extracting source..."
Expand-Archive -Path $ZipPath -DestinationPath $WorkDir -Force

$SourceRoot = Get-ChildItem -Path $WorkDir -Directory | Where-Object { Test-Path (Join-Path $_.FullName "scripts\Install-ProGo.ps1") } | Select-Object -First 1
if ($null -eq $SourceRoot) {
    Fail "Downloaded archive does not contain scripts\Install-ProGo.ps1"
}

$InstallScript = Join-Path $SourceRoot.FullName "scripts\Install-ProGo.ps1"
Write-UpdateLog "Installing updated ProGo from $($SourceRoot.FullName)"
& $InstallScript -NoStartup

if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
    Fail "Installer failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $Exe)) {
    Fail "Installed ProGo.exe not found: $Exe"
}

Start-UpdatedProGo -ExePath $Exe

try {
    Remove-Item $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
} catch {
    Write-UpdateLog "Cleanup warning: $($_.Exception.Message)"
}

Write-UpdateLog "ProGo update completed."
