# Focused synthetic tests only. Never dot-source the build/test driver or create a process.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Test-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Fixture driver does not parse.' }
$definitions = @($ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-IsolatedSshFixtureThreadFacts'
},$false))
if ($definitions.Count -ne 1) { throw 'Expected exactly one existing thread classifier.' }
. ([scriptblock]::Create($definitions[0].Extent.Text))
if (-not ('ProGo.Tests.SshThreadSynthetic.Sample' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Diagnostics;
namespace ProGo.Tests.SshThreadSynthetic {
    public sealed class Sample {
        public System.Diagnostics.ThreadState State = System.Diagnostics.ThreadState.Wait;
        public ThreadWaitReason Reason = ThreadWaitReason.Executive;
        public bool ThrowState, ThrowReason, ChangeBeforeReason;
        public int StateReads, ReasonReads;
        public string PrivateData = "PRIVATE_THREAD_UNIT_SENTINEL";
        public System.Diagnostics.ThreadState ThreadState {
            get { StateReads++; if (ThrowState) throw new InvalidOperationException(PrivateData); return State; }
        }
        public ThreadWaitReason WaitReason {
            get {
                ReasonReads++;
                if (ChangeBeforeReason) State = System.Diagnostics.ThreadState.Running;
                if (ThrowReason || State != System.Diagnostics.ThreadState.Wait) throw new InvalidOperationException(PrivateData);
                return Reason;
            }
        }
    }
    public sealed class CollectionFailure {
        public Sample[] Threads { get { throw new InvalidOperationException("PRIVATE_THREAD_UNIT_SENTINEL"); } }
    }
}
'@
}
$checks = 0
function Assert-Facts($Condition,[string]$Case) {
    if (-not $Condition) { throw ('Synthetic thread facts failed: ' + $Case) }
}
function Read-Facts($Owned) {
    $text = Get-IsolatedSshFixtureThreadFacts $Owned
    Assert-Facts ($text -is [string] -and -not $text.Contains('PRIVATE_THREAD_UNIT_SENTINEL')) 'fixed-string redaction'
    $map = @{}
    foreach ($part in $text.Split(';')) {
        $pair = $part.Trim().Split('=')
        Assert-Facts ($pair.Length -eq 2 -and -not $map.ContainsKey($pair[0]) -and $pair[1] -cmatch '^(True|False|[0-9]+)$') 'fixed typed output values'
        $map[$pair[0]] = $pair[1]
    }
    $expected = @('threadsRead','threadCount','threadsCapped','collectionProbeErrors','threadProbeErrors','running','ready','waiting','otherState',
        'waitExecutive','waitUserRequest','waitSuspended','waitDelay','waitPage','waitOther','waitProbeErrors')
    Assert-Facts ($map.Count -eq $expected.Count) 'exact schema count'
    foreach ($key in $expected) { Assert-Facts ($map.ContainsKey($key)) 'exact schema key' }
    return $map
}
function New-Sample { return (New-Object ProGo.Tests.SshThreadSynthetic.Sample) }
$enumCases = @('Executive=waitExecutive','FreePage=waitPage','PageIn=waitPage','SystemAllocation=waitOther',
    'ExecutionDelay=waitDelay','Suspended=waitSuspended','UserRequest=waitUserRequest','EventPairHigh=waitOther',
    'EventPairLow=waitOther','LpcReceive=waitOther','LpcReply=waitOther','VirtualMemory=waitPage','PageOut=waitOther','Unknown=waitOther')
Assert-Facts ([Enum]::GetNames([Diagnostics.ThreadWaitReason]).Count -eq $enumCases.Count) 'complete framework enum set'
foreach ($case in $enumCases) {
    $pair = $case.Split('='); $sample = New-Sample
    $sample.Reason = [Enum]::Parse([Diagnostics.ThreadWaitReason],$pair[0])
    $facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
    Assert-Facts ($facts['threadsRead'] -ceq 'True' -and $facts['threadCount'] -ceq '1' -and $facts['waiting'] -ceq '1' -and
        $facts[$pair[1]] -ceq '1' -and $facts['waitProbeErrors'] -ceq '0' -and $sample.StateReads -eq 1 -and $sample.ReasonReads -eq 1) ('enum ' + $pair[0])
    $checks++
}
$sample = New-Sample; $sample.Reason = [Enum]::ToObject([Diagnostics.ThreadWaitReason],123456)
$facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
Assert-Facts ($facts['waitOther'] -ceq '1' -and -not ($facts.Values -contains '123456')) 'unknown-enum'
$checks++
foreach ($state in @([Diagnostics.ThreadState]::Running,[Diagnostics.ThreadState]::Ready)) {
    $sample = New-Sample; $sample.State = $state; $sample.ThrowReason = $true
    $facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
    $key = if ($state -eq [Diagnostics.ThreadState]::Running) { 'running' } else { 'ready' }
    Assert-Facts ($facts[$key] -ceq '1' -and $facts['waiting'] -ceq '0' -and $facts['waitProbeErrors'] -ceq '0' -and $sample.ReasonReads -eq 0) 'non-wait-reason-not-read'
    $checks++
}
$sample = New-Sample; $sample.State = [Enum]::ToObject([Diagnostics.ThreadState],123456); $sample.ThrowReason = $true
$facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
Assert-Facts ($facts['otherState'] -ceq '1' -and $sample.ReasonReads -eq 0) 'unknown-state'
$checks++
$sample = New-Sample; $sample.ThrowState = $true
$facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
Assert-Facts ($facts['threadProbeErrors'] -ceq '1' -and $facts['waiting'] -ceq '0' -and $sample.ReasonReads -eq 0) 'state-getter-error'
$checks++
$sample = New-Sample; $sample.ThrowReason = $true
$facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
Assert-Facts ($facts['waiting'] -ceq '1' -and $facts['waitProbeErrors'] -ceq '1' -and $facts['waitOther'] -ceq '0') 'reason-getter-error'
$checks++
$sample = New-Sample; $sample.ChangeBeforeReason = $true
$facts = Read-Facts ([pscustomobject]@{ Threads = @($sample) })
Assert-Facts ($sample.State -eq [Diagnostics.ThreadState]::Running -and $sample.StateReads -eq 1 -and $sample.ReasonReads -eq 1 -and
    $facts['waiting'] -ceq '1' -and $facts['waitProbeErrors'] -ceq '1' -and $facts['waitOther'] -ceq '0') 'state-change'
$checks++
$facts = Read-Facts (New-Object ProGo.Tests.SshThreadSynthetic.CollectionFailure)
Assert-Facts ($facts['threadsRead'] -ceq 'False' -and $facts['collectionProbeErrors'] -ceq '1') 'collection-getter-error'
$checks++
foreach ($size in @(32,40)) {
    $samples = @(1..$size | ForEach-Object { $s = New-Sample; $s.State = [Diagnostics.ThreadState]::Running; $s })
    $facts = Read-Facts ([pscustomobject]@{ Threads = $samples })
    Assert-Facts ($facts['threadCount'] -ceq '32' -and $facts['running'] -ceq '32' -and
        ($facts['threadsCapped'] -ceq 'True') -eq ($size -gt 32)) 'cap32'
    for ($i = 0; $i -lt $size; $i++) { Assert-Facts ($samples[$i].StateReads -eq $(if ($i -lt 32) { 1 } else { 0 }) -and $samples[$i].ReasonReads -eq 0) 'cap-no-extra-getter' }
    $checks++
}
# Synthetic reason/state exceptions above contain private sentinel; Read-Facts enforces fixed-schema redaction each time.
$facts = Read-Facts ([pscustomobject]@{ Threads = @() })
Assert-Facts ($facts['threadCount'] -ceq '0' -and $facts['threadsRead'] -ceq 'True') 'empty-redaction'
$checks++
$originalFailure = 'EXPECTED_OWNED_SSH_DEADLINE_FAIL'; $observed = $false
try {
    [void](Read-Facts (New-Object ProGo.Tests.SshThreadSynthetic.CollectionFailure))
    throw $originalFailure
} catch { $observed = $_.Exception.Message -ceq $originalFailure }
Assert-Facts $observed 'preserve-original-FAIL'
$checks++
Assert-Facts ($checks -eq 26) 'complete26'
Write-Host ('Synthetic existing SSH thread classifier PASS: ' + $checks + '; no process/native probe/build invoked')
