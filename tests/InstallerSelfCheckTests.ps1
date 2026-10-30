Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer self-check helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-selfcheck-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
function Check-InstallerSelfCheck($Condition, $Name) {
    if (-not $Condition) { throw ('Installer self-check test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($mode in @('complete', 'exit-error', 'missing-root', 'changed-after', 'partial-copy', 'prior-intact', 'bad-restore', 'fresh-prior', 'timeout')) {
        $install = Join-Path $work $mode; New-Item -ItemType Directory -Path $install | Out-Null
        $personal = Join-Path $install 'settings.json'; [IO.File]::WriteAllText($personal, 'synthetic personal bytes')
        $old = Join-Path $install 'ProGo.exe'
        if ($mode -ne 'fresh-prior') { [IO.File]::WriteAllText($old, 'synthetic old bytes') }
        $backup = New-ProGoInstallerBackup $payload $install
        $stage = New-ProGoInstallerStage $payload
        $calls = New-Object 'System.Collections.Generic.List[string]'
        function Invoke-ProGoInstallerSelfCheck($Info) {
            $calls.Add('self-check')
            $data = $Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT']
            Check-InstallerSelfCheck ($Info.FileName -eq (Join-Path $install 'ProGo.exe') -and
                $Info.Arguments -ceq '--self-check' -and $Info.WorkingDirectory -eq $install -and
                -not $Info.UseShellExecute -and $Info.EnvironmentVariables['LOCALAPPDATA'] -eq $data -and
                [IO.Path]::GetDirectoryName($data) -eq $stage.Directory) 'installed image and isolated child context'
            if ($mode -eq 'exit-error') { return 17 }
            if ($mode -eq 'timeout') { throw (New-Object TimeoutException -ArgumentList 'synthetic owned self-check timeout') }
            if ($mode -ne 'missing-root') { [IO.Directory]::CreateDirectory($data) | Out-Null }
            if ($mode -eq 'changed-after') { [IO.File]::WriteAllText($Info.FileName, 'synthetic changed image') }
            return 0
        }
        if ($mode -in @('prior-intact', 'bad-restore', 'fresh-prior', 'timeout')) {
            if ($mode -eq 'bad-restore') { [IO.File]::WriteAllText($old, 'synthetic incorrectly restored bytes') }
            $rejected = $false
            try { Assert-ProGoInstallerRuntime $backup -Original } catch { $rejected = $true }
            Check-InstallerSelfCheck ($rejected -eq ($mode -eq 'bad-restore') -and $calls.Count -eq 0) 'old byte/absence proof, not a restore callback claim'
        } else {
            foreach ($entry in $backup.Entries) {
                $target = Join-Path $install $entry.Relative
                [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
                [IO.File]::Copy((Join-Path $stage.Directory $entry.Relative), $target, $true)
            }
            if ($mode -eq 'partial-copy') { [IO.File]::WriteAllText($old, 'synthetic partial copy') }
            $rejected = $false
            try { Confirm-ProGoInstallerSelfCheck $backup $stage } catch { $rejected = $true }
            Check-InstallerSelfCheck ($rejected -eq ($mode -ne 'complete')) 'self-check must fail unless all guarantees hold'
            Check-InstallerSelfCheck ($calls.Count -eq $(if ($mode -eq 'partial-copy') { 0 } else { 1 })) 'partial copy is refused before launch, one check otherwise'
        }
        Check-InstallerSelfCheck ([IO.File]::ReadAllText($personal) -ceq 'synthetic personal bytes') 'personal data unchanged'
        if ($null -ne $backup.Directory) { Check-InstallerSelfCheck ([IO.Directory]::Exists($backup.Directory)) 'recovery snapshot retained' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer self-check ' + $mode)
    }
    Write-Host 'Installer self-check tests PASS: 9'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
