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
        self.assertIn('"ProGo.exe"',body); self.assertIn('!(bool)selected["Exists"]',body)
    def test_live_expected_state_is_narrow_and_old_api_strict(self):
        body=self.native.split('internal bool VerifyPinnedForRuntime(',1)[1].split('public bool MutationAllowed',1)[0]
        for token in ('Object.ReferenceEquals(runtimeContext, context)','context.disposed',
                      'context.runtimeLeafResult.HandleReopened','context.runtimeLeafResult.MutationStarted',
                      'context.runtimeLeafResult.ReadbackVerified','LegacyOrigin(files[i].SafeFileHandle)',
                      'i == runtimeIndex && !beforeMutation ? runtimeOldHash : hashes[i]'):
            self.assertIn(token,body)
        self.assertIn('public bool VerifyPinned(CancellationToken token)',self.native)
        self.assertIn('hashes[i] != PhysicalPinHash(files[i])',self.native)
    def test_cancellation_and_single_attempt_not_authority(self):
        for token in ('if (context.runtimeLeafResult != null)',
                      'context.ObserveRuntimeCancellation(token); return context.runtimeLeafResult',
                      'result.PinReleaseAttempted = true', 'result.CancellationObserved = token.IsCancellationRequested',
                      'if (result.CancellationObserved) result.LeafVerified = false',
                      '!runtimeLeafResult.CancellationObserved && runtimeLeafResult.LeafVerified'):
            self.assertIn(token,self.native)
    def test_windows_fixture_real_context_and_negative_cases(self):
        f=self.fixture
        cases=re.findall(r"'([^']+)'",f.split('foreach($scenario in @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),19); self.assertEqual(len(set(cases)),19)
        for case in ('complete','equal-byte-replaced','target-changed','reparse','snapshot-changed',
                     'snapshot-changed-after-release','snapshot-write-denied','write','short-write','flush','readback','readback-mismatch',
                     'cancel-before','cancel-after-release','cancel-written','cancel-readback','disposed','late-success'):
            self.assertIn(case,cases)
        for token in ('Invoke-ProGoInstallerFailureAuxiliary','Invoke-ProGoInstallerSuccess','InvokeChecked($method,$manager',
                      'RestoreSingleRuntimeLeaf','same process actual handle restore','fresh process cannot use stored auxiliary receipt',
                      'snapshot source journal and auxiliary bytes preserved','once blocked snapshot proof returns, repeat cannot mutate'):
            self.assertIn(token,f)
        self.assertEqual(f.count('.Invoke('),1)
        self.assertIn('args[i].GetType()!=p[i].ParameterType', f)
        self.assertIn('return method.Invoke(target,args);', f)
if __name__ == '__main__': unittest.main()
