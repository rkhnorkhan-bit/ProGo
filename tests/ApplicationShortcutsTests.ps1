param([string]$ScriptsDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. (Join-Path $ScriptsDirectory 'Shortcuts-ProGo.ps1')
$script:passed = 0
function Check([bool]$ok, [string]$name) {
    if (-not $ok) { throw "Shortcut test failed: $name" }
    $script:passed++; Write-Host "PASS: $name"
}
function Refused([scriptblock]$action) {
    try { [void](& $action); return $false } catch { return $true }
}
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-shortcuts-' + [guid]::NewGuid().ToString('N'))
$shell = New-Object -ComObject WScript.Shell
function Seed([string]$path, [string]$target, [string]$arguments) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    $link = $shell.CreateShortcut($path)
    try { $link.TargetPath = $target; $link.Arguments = $arguments; $link.Save() }
    finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link) }
}
function Read-Link([string]$path) {
    $link = $shell.CreateShortcut($path)
    try { return @{ Target=$link.TargetPath; Arguments=$link.Arguments; Directory=$link.WorkingDirectory } }
    finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link) }
}
try {
    $install = Join-Path $fixture 'Программа с пробелами'
    $programs = Join-Path $fixture 'Programs'; $startup = Join-Path $fixture 'Startup'
    $exe = Join-Path $install 'ProGo.exe'; $menu = Join-Path $programs 'ProGo'
    $main = Join-Path $menu 'ProGo.lnk'; $old = Join-Path $menu 'ProGo Status.lnk'
    $auto = Join-Path $startup 'ProGo.lnk'
    [void][IO.Directory]::CreateDirectory($install)
    $prior = Test-ProGoExistingInstallation $install
    Check (-not $prior) 'empty directory is a first installation'
    [IO.File]::WriteAllText($exe, 'fixture executable; never launched')
    Initialize-ProGoShortcuts $install $prior $false $false $programs $startup
    Check ((Test-Path $main) -and (Test-Path $auto) -and -not (Test-Path $old)) 'first install creates one main launcher and startup'
    $link = Read-Link $main
    Check ($link.Target -eq $exe -and $link.Arguments -eq '--show' -and $link.Directory -eq $install) ("Unicode main launcher opens the installed app visibly: expected target=[$exe], directory=[$install]; actual target=[$($link.Target)], args=[$($link.Arguments)], directory=[$($link.Directory)]")
    $link = Read-Link $auto
    Check ($link.Target -eq $exe -and $link.Arguments -eq '') 'startup runs quietly without changing SSH preferences'
    $prior = Test-ProGoExistingInstallation $install
    Check $prior 'existing executable is detected before replacement'
    Remove-Item $auto
    Initialize-ProGoShortcuts $install $prior $false $false $programs $startup
    Check (-not (Test-Path $auto)) 'reinstall preserves disabled startup even with default switches'
    Seed $auto $exe '--custom-user-choice'
    $autoHash = (Get-FileHash $auto).Hash
    Initialize-ProGoShortcuts $install $true $true $false $programs $startup
    Check ((Get-FileHash $auto).Hash -eq $autoHash) 'existing custom startup is not rewritten by NoStartup'
    Initialize-ProGoShortcuts $install $true $false $false $programs $startup
    Check ((Get-FileHash $auto).Hash -eq $autoHash) 'default reinstall preserves startup bytes'
    $settings = Join-Path $install 'settings.json'
    [IO.File]::WriteAllText($settings, '{"AutoStartSocks":false}')
    $settingsHash = (Get-FileHash $settings).Hash
    $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
    Seed $old $exe '--show'
    $mainHash = (Get-FileHash $main).Hash
    Check ($manager.MigrateMenu()) 'duplicate legacy main launcher is migrated'
    Check (-not (Test-Path $old) -and (Get-FileHash $main).Hash -eq $mainHash) 'migration preserves existing primary bytes'
    Check (-not $manager.MigrateMenu()) 'migration is idempotent'
    Check ((Get-FileHash $settings).Hash -eq $settingsHash -and (Get-FileHash $auto).Hash -eq $autoHash) 'menu migration leaves connection preferences and startup untouched'
    Remove-Item $main
    Seed $old $exe '--show'
    Check ($manager.MigrateMenu() -and (Test-Path $main) -and -not (Test-Path $old)) 'legacy-only install gets a replacement before deletion'
    Check ((Read-Link $main).Arguments -eq '--show') 'migrated primary keeps normal visible launcher'
    Seed $old $exe '--special'
    $oldHash = (Get-FileHash $old).Hash
    Check (-not $manager.MigrateMenu() -and (Get-FileHash $old).Hash -eq $oldHash) 'custom legacy arguments are preserved'
    Seed $old (Join-Path $fixture 'Other.exe') '--show'
    $oldHash = (Get-FileHash $old).Hash
    Check (-not $manager.MigrateMenu() -and (Get-FileHash $old).Hash -eq $oldHash) 'same filename for another app is preserved'
    Seed $old $exe '--show'
    Seed $main (Join-Path $fixture 'Other.exe') '--show'
    $mainHash = (Get-FileHash $main).Hash; $oldHash = (Get-FileHash $old).Hash
    Check (Refused { $manager.MigrateMenu() }) 'foreign primary refuses replacement'
    Check ((Get-FileHash $main).Hash -eq $mainHash -and (Get-FileHash $old).Hash -eq $oldHash) 'failed migration retains both launchers'
    Remove-Item $main
    [IO.File]::WriteAllText($main, 'not a Shell link')
    Check (Refused { $manager.MigrateMenu() }) 'unreadable primary is not overwritten'
    Check ((Test-Path $old) -and [IO.File]::ReadAllText($main) -eq 'not a Shell link') 'malformed shortcut and legacy survive refusal'
    Remove-Item $main
    $locked = [IO.File]::Open($old, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try { Check (Refused { $manager.MigrateMenu() }) 'locked old shortcut refuses deletion safely' }
    finally { $locked.Dispose() }
    Check ((Test-Path $old) -and (Test-Path $main)) 'a failed legacy deletion still leaves a working primary'
    Check ($manager.MigrateMenu()) 'retry completes migration after unlock'
    Remove-Item $main
    Seed $old $exe '--show'
    Check (-not [ProGo.ApplicationShortcuts]::MigrateForExecutable((Join-Path $fixture 'portable\ProGo.exe'), $install, $programs, $startup)) 'portable execution cannot edit installed menu'
    Check ((Test-Path $old) -and -not (Test-Path $main)) 'portable run does not migrate old launcher'
    Check ([ProGo.ApplicationShortcuts]::MigrateForExecutable($exe, $install, $programs, $startup)) 'installed execution migrates old menu'
    Remove-Item $main
    Check (-not $manager.MigrateMenu() -and -not (Test-Path $main)) 'normal launch respects an intentionally absent menu'
    Remove-Item $auto
    Initialize-ProGoShortcuts $install $false $true $true $programs $startup
    Check (-not (Test-Path $auto) -and -not (Test-Path $main)) 'first-install opt-out switches are respected independently'
    Initialize-ProGoShortcuts $install $false $true $false $programs $startup
    Check ((Test-Path $main) -and -not (Test-Path $auto)) 'main menu can be installed without startup'
    Remove-Item $main
    Initialize-ProGoShortcuts $install $false $false $true $programs $startup
    Check ((Test-Path $auto) -and -not (Test-Path $main)) 'startup can be installed without menu'
    Remove-Item $exe
    Check (Test-ProGoExistingInstallation $install) 'retained settings prevent re-enabling startup after uninstall'
    Remove-Item $settings
    [IO.File]::WriteAllText((Join-Path $install 'VERSION'), '0.0.0')
    Check (Test-ProGoExistingInstallation $install) 'version marker preserves previous installation identity during repair'
    Check (Refused { $manager.Install($true, $false, $false) }) 'missing executable cannot create new launchers'
    Write-Host "Application shortcut tests passed: $script:passed"
} finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
    if (Test-Path $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
