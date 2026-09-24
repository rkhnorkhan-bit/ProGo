param(
    [Parameter(Mandatory=$true)][string]$BackupDir,
    [int]$WaitPid = 0,
    [switch]$NoLaunch
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "ProGo"
$RestoreLog = Join-Path $InstallDir "progo-restore.log"

function U8($Base64) {
    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Base64))
}

function Write-RestoreLog($Message) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz") + " " + $Message
    Add-Content -Path $RestoreLog -Value $line -Encoding UTF8
    Write-Host $Message
}

function Show-UserMessage($Text, $Title) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        [void][System.Windows.Forms.MessageBox]::Show($Text, $Title, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch {
        Write-Host ("{0}: {1}" -f $Title, $Text)
    }
}

function Fail($Message) {
    Write-RestoreLog ("ERROR: " + $Message)
    throw $Message
}

function Wait-ProGoExit($TargetProcessId, $TimeoutMs) {
    if ($TargetProcessId -le 0) { return }

    Write-RestoreLog "Waiting for ProGo process to exit: PID $TargetProcessId"
    try {
        $process = Get-Process -Id $TargetProcessId -ErrorAction SilentlyContinue
        if ($null -ne $process) {
            [void]$process.WaitForExit($TimeoutMs)
        }
    } catch {
        Write-RestoreLog "Wait process warning: $($_.Exception.Message)"
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

function Copy-IfExists($Name) {
    $source = Join-Path $BackupDir $Name
    if (Test-Path $source) {
        Copy-Item $source -Destination (Join-Path $InstallDir $Name) -Force
        Write-RestoreLog "Restored file: $Name"
    }
}

function Copy-DirectoryIfExists($Name) {
    $source = Join-Path $BackupDir $Name
    $destination = Join-Path $InstallDir $Name
    if (-not (Test-Path $source)) { return }

    if (Test-Path $destination) {
        Remove-Item $destination -Recurse -Force
    }

    Copy-Item $source -Destination $destination -Recurse -Force
    Write-RestoreLog "Restored directory: $Name"
}

function Start-ProGo {
    if ($NoLaunch) {
        Write-RestoreLog "Launch skipped by -NoLaunch."
        return
    }

    $exe = Join-Path $InstallDir "ProGo.exe"
    if (-not (Test-Path $exe)) {
        Fail "Cannot launch ProGo, file not found: $exe"
    }

    $process = Start-Process -FilePath $exe -WorkingDirectory $InstallDir -PassThru
    Start-Sleep -Seconds 2
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if ($null -eq $running -or $running.HasExited) {
        Fail "Restored ProGo did not stay running."
    }

    Write-RestoreLog "Restored ProGo started: PID $($process.Id)"
}

Write-RestoreLog "ProGo restore started."
Write-RestoreLog "BackupDir=$BackupDir"

if (-not (Test-Path $BackupDir)) {
    Fail "Backup folder does not exist: $BackupDir"
}

$manifest = Join-Path $BackupDir "manifest.txt"
if (-not (Test-Path $manifest)) {
    Fail "Backup manifest not found: $manifest"
}

Wait-ProGoExit -TargetProcessId $WaitPid -TimeoutMs 30000
$exePath = Join-Path $InstallDir "ProGo.exe"
Wait-FileUnlocked -Path $exePath -TimeoutSeconds 30

Copy-IfExists "ProGo.exe"
Copy-IfExists "ProGo.ico"
Copy-IfExists "VERSION"
Copy-IfExists "vault.enc.json"
Copy-IfExists "settings.json"
Copy-IfExists "progo.log"
Copy-DirectoryIfExists "scripts"

Start-ProGo

Write-RestoreLog "ProGo restore completed."
Show-UserMessage ((U8 "UHJvR28g0LLQvtGB0YHRgtCw0L3QvtCy0LvQtdC9INC40Lcg0YDQtdC30LXRgNCy0L3QvtC5INC60L7Qv9C40Lg6IA==") + $BackupDir) (U8 "0J7RgtC60LDRgiBQcm9Hbw==")
