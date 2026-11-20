<#
Optional TUN engine installer. Run only with explicit administrator consent
under the SAME Windows account that runs ProGo (not a different admin account).
The normal ProGo UI stays unelevated. No VPS, firewall or proxies are modified.
#>
param([switch]$Remove)
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$id = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$admin = New-Object System.Security.Principal.WindowsPrincipal($id)
if (-not $admin.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run PowerShell as administrator under the ProGo user's account."
}

$name = "ProGoSmartRouting"
$dir = Join-Path $env:ProgramFiles "ProGoRoutingEngine"
$exe = Join-Path $dir "sing-box.exe"
$hostPath = Join-Path $dir "ProGoSmartRoutingHost.exe"
$hostSource = Join-Path $env:LOCALAPPDATA "ProGo\tools\ProGoSmartRoutingHost.exe"
$startupMarker = Join-Path $dir "autostart-owned.txt"
$run = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
$config = Join-Path $env:LOCALAPPDATA "ProGo\smart-routing\active.json"
$argsLine = ""
$progo = Join-Path $env:LOCALAPPDATA "ProGo\ProGo.exe"
$version = "1.14.4"
$assetSha = "99185d75d314a07dbba4a130c8ad986cbeb629a07927975636b341ca61f991df"
$engineSha = "8f30682ee198855440d2a7fe070d5d3653793b2d01bb8c85eea88f93bbe7c8b9"
$uri = "https://github.com/SagerNet/sing-box/releases/download/v$version/sing-box-$version-windows-amd64.zip"

function Assert-TaskOwnership {
    $existing = Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
    if ($null -eq $existing) { return $null }
    if (@($existing.Actions).Count -ne 1 -or $existing.Actions[0].Execute -ine $hostPath -or [string]$existing.Actions[0].Arguments -cne $argsLine) {
        throw "Another task named $name exists. Nothing changed."
    }
    return $existing
}

$existing = Assert-TaskOwnership
if ($Remove) {
    if ($null -ne $existing) {
        Stop-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $name -Confirm:$false
    }
    $expectedRun = '"' + $progo + '"'
    $runEntry = (Get-ItemProperty -Path $run -Name "ProGo" -ErrorAction SilentlyContinue).ProGo
    if ((Test-Path $startupMarker) -and $runEntry -eq $expectedRun) {
        Remove-ItemProperty -Path $run -Name "ProGo"
        Remove-Item -LiteralPath $startupMarker -Force
        Write-Host "ProGo startup entry installed by the routing engine has been reverted."
    }
    Write-Host "Owned routing task removed. Engine files and ProGo settings preserved."
    return
}

if (-not (Test-Path (Join-Path $env:LOCALAPPDATA "ProGo\settings.json"))) {
    throw "ProGo settings.json missing for this account. Launch ProGo first."
}
if (-not (Test-Path $progo)) {
    throw "Install ProGo to $progo before enabling automatic TUN startup."
}
if (-not (Test-Path $config)) {
    throw "No Smart Routing config. Enable Smart Routing, save and start ProGo before installing the task."
}
if (-not (Test-Path $hostSource)) {
    throw "ProGoSmartRoutingHost.exe not found in the installed release. Update ProGo first."
}
$progoSettings = Get-Content (Join-Path $env:LOCALAPPDATA "ProGo\settings.json") -Encoding UTF8 -Raw | ConvertFrom-Json
if (-not $progoSettings.SmartRoutingEnabled -or $progoSettings.AutoSystemProxy) {
    throw "Enable SmartRoutingEnabled and disable legacy AutoSystemProxy in ProGo before installing the task."
}

$zipPath = Join-Path $env:TEMP ("progo-tun-" + [guid]::NewGuid().ToString("N") + ".zip")
$folder = $zipPath + "-extract"
try {
    Write-Host "Downloading official sing-box $version..."
    Invoke-WebRequest -Uri $uri -OutFile $zipPath -UseBasicParsing -MaximumRedirection 5
    $sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sha -ne $assetSha) { throw "Archive SHA256 mismatch: refusing installation." }
    Expand-Archive -LiteralPath $zipPath -DestinationPath $folder
    $candidates = @(Get-ChildItem $folder -Recurse -File -Filter "sing-box.exe")
    if ($candidates.Count -ne 1) { throw "Unexpected upstream archive contents." }
    $sha = (Get-FileHash $candidates[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sha -ne $engineSha) { throw "Executable SHA256 mismatch: refusing installation." }

    if ($null -ne $existing) { Stop-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue }
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Copy-Item -LiteralPath $candidates[0].FullName -Destination $exe -Force
    Copy-Item -LiteralPath $hostSource -Destination $hostPath -Force
    if ((Get-FileHash $hostSource -Algorithm SHA256).Hash -ne (Get-FileHash $hostPath -Algorithm SHA256).Hash) {
        throw "TUN guard checksum changed during protected copy."
    }
    if ((Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant() -ne $engineSha) {
        throw "Protected executable checksum mismatch."
    }

    # Restrict modifications to SYSTEM and local administrators. Ordinary
    # users receive only Read/Execute to the privileged TUN binary.
    & icacls.exe $dir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)RX' /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Unable to secure TUN engine folder ACL." }

    & $exe check -c $config
    if ($LASTEXITCODE -ne 0) { throw "sing-box check rejected live config; task was not registered." }
    & $hostPath --self-check
    if ($LASTEXITCODE -ne 0) { throw "Protected TUN host self-check failed." }

    $userId = $id.Name
    $action = New-ScheduledTaskAction -Execute $hostPath -WorkingDirectory (Split-Path $config)
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $userId
    $trigger.Delay = "PT45S"
    $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval ([TimeSpan]::FromMinutes(1)) -MultipleInstances IgnoreNew -StartWhenAvailable
    Register-ScheduledTask -TaskName $name -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "ProGo Smart Routing supervised TUN; SHA-256 verified sing-box 1.14.4" -Force | Out-Null

    # Preserve pre-existing user startup commands and only install our own
    # startup entry when none exists. An ownership marker enables safe removal.
    $expectedRun = '"' + $progo + '"'
    $runEntry = (Get-ItemProperty -Path $run -Name "ProGo" -ErrorAction SilentlyContinue).ProGo
    if ($null -eq $runEntry) {
        New-ItemProperty -Path $run -Name "ProGo" -PropertyType String -Value $expectedRun | Out-Null
        Set-Content -LiteralPath $startupMarker -Value $userId -Encoding ASCII
    }
    elseif ($runEntry -ne $expectedRun) {
        Write-Warning "Existing ProGo startup command was preserved; configure autostart manually if needed."
    }
    Write-Host "Protected TUN installed. Task: $name; owner: $userId."
    Write-Host "Starts 45 seconds after logon. Set AutoStartSocks in ProGo."
}
finally {
    Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $folder -Force -Recurse -ErrorAction SilentlyContinue
}
