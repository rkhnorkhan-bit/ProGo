"""Static wiring regressions; native ownership and faults require the Windows fixture."""
import pathlib
import re
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class LiveRuntimeLeafContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.native=(ROOT/'src/ApplicationShortcuts.cs').read_text()
        cls.installer=(ROOT/'scripts/Install-ProGo.ps1').read_text()
        cls.fixture=(ROOT/'tests/InstallerLiveRuntimeLeafTests.ps1').read_text()
    def test_real_handler_consumes_single_leaf_result(self):
        handler=self.installer.split('function Invoke-ProGoInstallerFailureAuxiliary(',1)[1].split('function Write-ProGoInstallerOutcomeBytes',1)[0]
        for token in ('[switch]$RestoreSingleRuntimeLeaf','RuntimeFailureBranchAllowed','RuntimeLeafProgress',
                      'Invoke-ProGoInstallerLiveRuntimeLeaf $LegacyContext $CancellationToken','VerifyRuntimeLeafOutcome()',
                      'OriginalError = $OriginalError','WholeRollbackVerified = $false'):
            self.assertIn(token,handler)
        self.assertIn('if ($null -eq $runtimeLeaf)',handler)
    def test_handle_identity_and_full_auxiliary_rechecked_before_mutation(self):
        body=self.native.split('private RuntimeLeafResult RestoreLiveRuntimeLeaf(',1)[1].split('// One terminal observation',1)[0]
        steps=['VerifyAuxiliaryOutcome()', 'BeginRuntimeLeaf(context, targetIndex, oldHash)',
               'OpenLegacyForDisposition(target, 0xC0000000u, 1', 'origin != LegacyOrigin(handle)',
               'PhysicalPinHash(output) != newHash', 'PhysicalPinHash(saved) != oldHash',
               'RetainRuntimeLeaf(output)', 'if (!context.VerifyAuxiliaryOutcome()) return null;',
               'output.SetLength(0)', 'saved.CopyTo(output)', 'output.Flush(true)', 'result.ReadbackVerified = true']
        cursor=0
        for token in steps:
            cursor=body.index(token,cursor)+len(token)
        self.assertNotIn('File.Copy(',body); self.assertNotIn('File.Delete(',body)
        self.assertIn('Array.IndexOf(LegacyOperationLeaves, leaf)',body); self.assertIn('!(bool)entry["Exists"]',body)
        self.assertIn('selected.Count == 0',body); self.assertIn('result.CompletedCount == result.SelectedCount',body)
    def test_live_expected_state_is_narrow_and_old_api_strict(self):
        body=self.native.split('internal bool VerifyPinnedForRuntime(',1)[1].split('public bool MutationAllowed',1)[0]
        for token in ('Object.ReferenceEquals(runtimeContext, context)','context.disposed',
                      'context.runtimeLeafResult.HandleReopened','context.runtimeLeafResult.MutationStarted',
                      'context.runtimeLeafResult.ReadbackVerified','LegacyOrigin(files[i].SafeFileHandle)',
                      'runtimeTransitions.TryGetValue(i, out transition)',
                      'if (transition.Completed) expected = transition.OldHash',
                      'i != runtimeActiveIndex', 'runtimeTransitions.ContainsKey(index)', 'CompleteRuntimeLeaf'):
            self.assertIn(token,body)
        self.assertIn('public bool VerifyPinned(CancellationToken token)',self.native)
        self.assertIn('hashes[i] != PhysicalPinHash(files[i])',self.native)
    def test_cancellation_and_single_attempt_not_authority(self):
        for token in ('if (context.runtimeLeafResult != null)',
                      'context.ObserveRuntimeCancellation(token); return context.runtimeLeafResult',
                      'result.PinReleaseAttempted = true', 'result.CancellationObserved = token.IsCancellationRequested',
                      'if (result.CancellationObserved) { result.LeafVerified = false; result.SetVerified = false; }',
                      '!runtimeLeafResult.CancellationObserved && runtimeLeafResult.SetVerified'):
            self.assertIn(token,self.native)
    def test_child_scope_matches_both_invocations_and_rejects_unknown(self):
        guard=self.fixture.split('if($BlockedChild){',1)[1].split('foreach($p in @($parent,$CaseDirectory))',1)[0]
        self.assertIn("[IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','multi-complete')",guard)
        self.assertIn("if($scenario -in @('complete','multi-complete'))",self.fixture)
        self.assertIn("$unknown=Join-Path $work 'unknown-child'",self.fixture)
        self.assertIn("$child.ExitCode -ne 0",self.fixture)
        self.assertIn("$err.Result.Contains('Runtime child scope refused.')",self.fixture)
        self.assertIn("-not $out.Result.Contains('RUNTIME_CHILD blocked=True')",self.fixture)

    def test_fixture_callback_requires_actual_replacement_and_reports_setup_errors(self):
        f=self.fixture
        for token in ('$localScenario=[string]$scenario','$localTarget=if($scenario -in',
                      '$localGetOrigin=(Get-Command Get-RuntimeFixtureOrigin).ScriptBlock',
                      '$observed.CallbackEntered++','$observed.CallbackCompleted++',
                      '$observed.EqualBytes=','$observed.EqualCreation=','$observed.DifferentIdentity=',
                      '$observed.OriginalUnchanged=','SETUP ERROR: required synthetic callback did not complete exactly once',
                      'SETUP ERROR: equal-byte native replacement was not established',
                      'retained original native object and bytes remain intact after refusal','RUNTIME_INJECTION case='):
            self.assertIn(token,f)
        self.assertLess(f.index('SETUP ERROR: equal-byte native replacement was not established'),
                        f.index("'conflict cancellation fault or lost proof never becomes success'"))
        self.assertIn('method.GetParameters()[0].ParameterType!=typeof(SafeFileHandle)',f)
    def test_windows_fixture_real_context_and_negative_cases(self):
        f=self.fixture
        cases=re.findall(r"'([^']+)'",f.split('foreach($scenario in @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),35); self.assertEqual(len(set(cases)),35)
        for case in ('complete','equal-byte-replaced','target-changed','reparse','snapshot-changed',
                     'snapshot-changed-after-release','snapshot-write-denied','write','short-write','flush','readback','readback-mismatch',
                     'cancel-before','cancel-after-release','cancel-written','cancel-readback','disposed','late-success',
                     'multi-complete','multi-second-write','multi-second-cancel-written','multi-last-write','multi-last-cancel-written','multi-between-cancel','multi-equal-second','multi-snapshot-second','multi-target-second','multi-reparse-second','multi-disposed','multi-late-success','multi-inventory-missing','multi-inventory-duplicate','multi-inventory-unknown','multi-installed-handling'):
            self.assertIn(case,cases)
        for token in ('Invoke-ProGoInstallerFailureAuxiliary','Invoke-ProGoInstallerSuccess','InvokeChecked($method,$manager',
                      'RestoreSingleRuntimeLeaf','same process actual handle restore','fresh process cannot use stored auxiliary receipt',
                      'snapshot source journal and auxiliary bytes preserved','once blocked snapshot proof returns, repeat cannot mutate'):
            self.assertIn(token,f)
        self.assertEqual(f.count('.Invoke('),2)
        self.assertIn('args[i].GetType()!=p[i].ParameterType', f)
        self.assertIn('return method.Invoke(target,args);', f)
        self.assertIn('method.GetParameters()[0].ParameterType!=typeof(SafeFileHandle)', f)
if __name__ == '__main__': unittest.main()
