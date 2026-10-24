Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer stage helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-stage-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
function Check-Stage($Condition, $Name) {
    if (-not $Condition) { throw ('Installer stage test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $source = Join-Path $work 'payload'
    Microsoft.PowerShell.Management\Copy-Item -LiteralPath (Join-Path $Root 'release') -Destination $source -Recurse
    $foreign = Join-Path $work 'unrelated'
    New-Item -ItemType Directory -Path $foreign | Out-Null
    $marker = Join-Path $foreign 'preserved.txt'
    [IO.File]::WriteAllText($marker, 'synthetic unrelated bytes')
    $payload = Get-ProGoInstallPayload -Directory $source
    [IO.File]::WriteAllText((Join-Path $source 'extra.txt'), 'not admitted to runtime')
    $stage = New-ProGoInstallerStage $payload
    Check-Stage ($stage.Payload.Version -eq $payload.Version -and
        $stage.Payload.Hashes['ProGo.exe'] -ceq $payload.Hashes['ProGo.exe'] -and
        $stage.Payload.Files.Count -eq $payload.Files.Count) 'identity and inventory'
    foreach ($relative in $payload.Files) {
        Check-Stage ($stage.Payload.Hashes[$relative] -ceq $payload.Hashes[$relative]) 'every staged byte hash'
    }
    Write-Host 'PASS: installer stage exact identity and all frozen hashes'
    Check-Stage (-not (Test-Path -LiteralPath (Join-Path $stage.Directory 'extra.txt'))) 'unlisted source file excluded'
    Write-Host 'PASS: installer stage admits only declared payload'

    $icon = Join-Path $source 'ProGo.ico'; $originalIcon = [IO.File]::ReadAllBytes($icon)
    [IO.File]::WriteAllText($icon, 'changed source after verified staging')
    Check-Stage ((Get-FileHash -LiteralPath (Join-Path $stage.Directory 'ProGo.ico') -Algorithm SHA256).Hash -ceq $payload.Hashes['ProGo.ico']) 'staged bytes independent of later source changes'
    [IO.File]::WriteAllBytes($icon, $originalIcon)
    $ownedPath = $stage.Directory
    Remove-ProGoInstallerStage $stage; $stage = $null
    Check-Stage (-not [IO.Directory]::Exists($ownedPath)) 'successful owned cleanup'
    Write-Host 'PASS: installer stage frozen source and owned cleanup'

    foreach ($mode in @('source-changed', 'source-missing', 'corrupt-copy', 'copy-error')) {
        $version = Join-Path $source 'VERSION'; $originalVersion = [IO.File]::ReadAllBytes($version)
        $script:StageObserved = $null
        function Copy-ProGoInstallerStageFile([string]$Source, [string]$Destination) {
            # First inventory entry is ProGo.exe directly below the new owned root.
            if ($null -eq $script:StageObserved) { $script:StageObserved = [IO.Path]::GetDirectoryName($Destination) }
            if ($mode -eq 'copy-error') { throw 'synthetic-stage-copy-error' }
            [IO.File]::Copy($Source, $Destination, $false)
            if ($mode -eq 'corrupt-copy') { [IO.File]::WriteAllText($Destination, 'synthetic corrupt stage') }
        }
        try {
            if ($mode -eq 'source-changed') { [IO.File]::WriteAllText($version, '999.998.997') }
            if ($mode -eq 'source-missing') { [IO.File]::Delete($version) }
            $rejected = $false
            try { $stage = New-ProGoInstallerStage $payload } catch { $rejected = $true }
            Check-Stage ($rejected -and $null -ne $script:StageObserved -and
                -not [IO.Directory]::Exists($script:StageObserved)) ($mode + ' refuses and cleans owned stage')
            Check-Stage ([IO.File]::ReadAllText($marker) -ceq 'synthetic unrelated bytes') ($mode + ' preserves unrelated data')
            Write-Host ('PASS: installer stage ' + $mode)
        } finally { [IO.File]::WriteAllBytes($version, $originalVersion) }
    }
    $rejected = $false
    try { Remove-ProGoInstallerStage ([pscustomobject]@{ Directory = $foreign }) } catch { $rejected = $true }
    Check-Stage ($rejected -and [IO.File]::ReadAllText($marker) -ceq 'synthetic unrelated bytes') 'foreign cleanup scope refused'
    Write-Host 'PASS: installer stage foreign cleanup refused without data changes'
    Write-Host 'Installer stage tests PASS: 8'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
