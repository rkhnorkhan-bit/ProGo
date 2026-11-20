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
$config = Join-Path $env:LOCALAPPDATA "ProGo\smart-routing\active.json"
$argsLine = 'run -c "' + $config + '"'
$progo = Join-Path $env:LOCALAPPDATA "ProGo\ProGo.exe"
$version = "1.14.4"
$assetSha = "99185d75d314a07dbba4a130c8ad986cbeb629a07927975636b341ca61f991df"
$engineSha = "8f30682ee198855440d2a7fe070d5d3653793b2d01bb8c85eea88f93bbe7c8b9"
$uri = "https://github.com/SagerNet/sing-box/releases/download/v$version/sing-box-$version-windows-amd64.zip"

function Assert-TaskOwnership {
    $existing = Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
    if ($null -eq $existing) { return $null }
    if (@($existing.Actions).Count -ne 1 -or $existing.Actions[0].Execute -ine $exe -or $existing.Actions[0].Arguments -cne $argsLine) {
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
    if ((Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant() -ne $engineSha) {
        throw "Protected executable checksum mismatch."
    }

    $userId = $id.Name
    $action = New-ScheduledTaskAction -Execute $exe -Argument $argsLine -WorkingDirectory (Split-Path $config)
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $userId
    $trigger.Delay = "PT45S"
    $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval ([TimeSpan]::FromMinutes(1)) -MultipleInstances IgnoreNew -StartWhenAvailable
    Register-ScheduledTask -TaskName $name -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "ProGo Smart Routing TUN; SHA-256 verified sing-box 1.14.4" -Force | Out-Null

    # A regular unelevated ProGo session owns the existing SSH/SOCKS process.
    $run = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
    Set-ItemProperty -Path $run -Name "ProGo" -Value ('"' + $progo + '"')
    Write-Host "Protected TUN installed. Task: $name; owner: $userId."
    Write-Host "Starts 45 seconds after logon. Set AutoStartSocks in ProGo."
}
finally {
    Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $folder -Force -Recurse -ErrorAction SilentlyContinue
}
