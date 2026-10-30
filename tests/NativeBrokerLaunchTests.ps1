param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: pre-CLR launch fixtures require isolated Windows CI'; return }
$repo = Split-Path -Parent $Scripts
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-native-broker-fixture-' + [guid]::NewGuid().ToString('N'))
$unknownTree = $false
function Check-NativeLaunch($condition,[string]$message) { if (-not $condition) { throw ('Native broker launch: ' + $message) } }
function Invoke-NativeLaunch([string]$executable,[string]$arguments,[hashtable]$environment,[int]$deadline=60000) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $executable; $info.Arguments = $arguments; $info.WorkingDirectory = $work
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    foreach ($key in $environment.Keys) { $info.EnvironmentVariables[$key] = $environment[$key] }
    # Fixed native telemetry is below 512 bytes, so it cannot fill this pipe.
    $info.RedirectStandardOutput = ($arguments -ceq '--fixture-host')
    $process = $null
    try {
        $process = [Diagnostics.Process]::Start($info)
        if (-not $process.WaitForExit($deadline)) { $script:unknownTree = $true; throw 'Native broker fixture child deadline.' }
        if ($info.RedirectStandardOutput) {
            $buffer = New-Object char[] 513
            $length = 0
            while ($length -lt $buffer.Length) {
                $read = $process.StandardOutput.Read($buffer,$length,($buffer.Length - $length))
                if ($read -eq 0) { break }; $length += $read
            }
            $diagnostic = -join $buffer[0..([Math]::Max(0,$length - 1))]
            # Never echo arbitrary child output, paths or environment values.
            if ($diagnostic -cmatch '^PG_NATIVE_DIAG v=2 stage=([1-9]|1[0-9]|2[0-7]) error=[0-9]{1,10} temp_match=[01] long_known=[01] long_match=[01] created=[01] assigned=[01] resumed=[01] exit_known=[01] host_exit=[0-9]{1,10} account_ok=[01] active=[0-9]{1,10} wait_result=[0-9]{1,10} identity_result=[0-3]\n$') {
                Write-Host $diagnostic.TrimEnd()
            } else { Write-Host 'PG_NATIVE_DIAG invalid_or_missing=1' }
        }
        if ($process.ExitCode -eq 3) { $script:unknownTree = $true }; return $process.ExitCode
    } catch {
        # Launch/observation failures cannot prove descendant settlement. Sticky.
        $script:unknownTree = $true; throw
    } finally {
        if ($null -ne $process) {
            if ($script:unknownTree) {
                try { if (-not $process.HasExited) { $process.Kill() }; [void]$process.WaitForExit(5000) } catch { $script:unknownTree = $true }
            }
            $process.Dispose()
        }
    }
}
try {
    [void][IO.Directory]::CreateDirectory($work)
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $managerSource = Join-Path $work 'PreClrManager.cs'
    [IO.File]::WriteAllText($managerSource,@'
using System;
using System.IO;
public sealed class FixtureDomainManager : AppDomainManager {
    public override void InitializeNewDomain(AppDomainSetup setup) {
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(),"preclr.canary"),"early");
    }
}
'@)
    & $compiler /nologo /target:library ("/out:" + (Join-Path $work 'PreClrManager.dll')) $managerSource
    Check-NativeLaunch ($LASTEXITCODE -eq 0) 'manager compiles'
    $probeSource = Join-Path $work 'ClrProbe.cs'
    [IO.File]::WriteAllText($probeSource,'using System; using System.IO; public static class ClrProbe { public static void Main(string[] args) { if(args.Length > 0 && args[0] == "--unknown") { Environment.Exit(3); } if(args.Length > 0 && args[0] == "--timeout") { System.Threading.Thread.Sleep(30000); } File.WriteAllText("managed-main.canary","main"); } }')
    $probe = Join-Path $work 'ClrProbe.exe'
    & $compiler /nologo /target:exe ("/out:" + $probe) $probeSource
    Check-NativeLaunch ($LASTEXITCODE -eq 0) 'managed control compiles'
    $managerIdentity = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $work 'PreClrManager.dll')).FullName
    $config = '<configuration><runtime><appDomainManagerAssembly value="' + $managerIdentity + '"/><appDomainManagerType value="FixtureDomainManager"/></runtime></configuration>'
    [IO.File]::WriteAllText(($probe + '.config'),$config)
    Check-NativeLaunch ((Invoke-NativeLaunch $probe '' @{}) -eq 0) 'managed positive control exits'
    $canary = Join-Path $work 'preclr.canary'
    Check-NativeLaunch ([IO.File]::Exists($canary) -and [IO.File]::Exists((Join-Path $work 'managed-main.canary'))) 'control actually executes foreign code before managed Main'
    [IO.File]::Delete($canary)
    $native = Join-Path $work 'ProGo.BrokerFixture.exe'
    [IO.File]::Copy((Join-Path $repo 'build/ProGo.BrokerFixture.exe'),$native)
    [IO.File]::WriteAllText(($native + '.config'),$config)
    [IO.File]::WriteAllText((Join-Path $work 'host.ps1'),@'
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
foreach ($name in @('APPDOMAIN_MANAGER_ASM','APPDOMAIN_MANAGER_TYPE','COMPLUS_Version','COR_ENABLE_PROFILING','COR_PROFILER','COR_PROFILER_PATH','DOTNET_STARTUP_HOOKS')) {
    if ($null -ne [Environment]::GetEnvironmentVariable($name,'Process')) { throw 'Native launch inherited a forbidden startup variable.' }
}
[IO.File]::WriteAllText((Join-Path ([Environment]::CurrentDirectory) 'host.success'),'host')
'@)
    $hostile = @{ APPDOMAIN_MANAGER_ASM=$managerIdentity; APPDOMAIN_MANAGER_TYPE='FixtureDomainManager'; COMPLUS_Version='v0.0.0';
        COR_ENABLE_PROFILING='1'; COR_PROFILER='{226A7332-A231-4A9C-ADE4-CB1E96424A02}'; COR_PROFILER_PATH=(Join-Path $work 'PreClrManager.dll'); DOTNET_STARTUP_HOOKS=(Join-Path $work 'PreClrManager.dll') }
    $nativeExit = Invoke-NativeLaunch $native '--fixture-host' $hostile
    Write-Host ('PG_NATIVE_PARENT exit={0} host_success={1} preclr_canary={2}' -f $nativeExit,[int][IO.File]::Exists((Join-Path $work 'host.success')),[int][IO.File]::Exists($canary))
    Check-NativeLaunch ($nativeExit -eq 0) 'actual native launch uses the fixed system host and respects policy'
    Check-NativeLaunch ([IO.File]::Exists((Join-Path $work 'host.success')) -and -not [IO.File]::Exists($canary)) 'foreign startup code never executes; trusted host still runs'
    $production = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes((Join-Path $repo 'release/ProGo.Setup.exe')))
    Check-NativeLaunch (-not $production.Contains('--fixture-host')) 'fixture entrypoint is absent from production native image'
    # Regression: exit3 then a successfully joined launcher must retain evidence.
    Check-NativeLaunch ((Invoke-NativeLaunch $probe '--unknown' @{}) -eq 3) 'unknown-tree launcher exits3'
    Check-NativeLaunch $script:unknownTree 'exit3 stays unknown after finally WaitForExit'
    Check-NativeLaunch ((Invoke-NativeLaunch $probe '' @{}) -eq 0) 'later launcher exits normally'
    Check-NativeLaunch $script:unknownTree 'later normal exit cannot clear unknown tree'
    $timedOut = $false
    try { [void](Invoke-NativeLaunch $probe '--timeout' @{} 100) }
    catch { if ($_.Exception.Message -cne 'Native broker fixture child deadline.') { throw }; $timedOut = $true }
    Check-NativeLaunch ($timedOut -and $script:unknownTree) 'timeout and launcher kill/join retain unknown tree'
    Write-Host 'PASS native broker pre-CLR config/environment/profile boundary (synthetic, no elevation)'
} finally {
    if (-not $script:unknownTree -and [IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
    elseif ($script:unknownTree) { Write-Host 'Native broker fixture retained: child settlement unknown.' }
}
