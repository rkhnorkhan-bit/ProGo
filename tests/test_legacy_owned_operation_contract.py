"""Static contract review for exact-owned operation; not compiler/native or durable recovery proof."""
import pathlib,re,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class OwnedOperationContractTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.cs=(ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
  cls.operation=cls.cs.split('private LegacyOwnedDeleteResult ExecuteLegacyOwnedDeletion',1)[1].split('public static bool IsExistingInstallation',1)[0]
  cls.ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
  cls.wrapper=cls.ps.split('function Invoke-ProGoInstallerLegacyOwnedDeletion',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
  cls.fixture=(ROOT/'tests/InstallerLegacyOwnedOperationTests.ps1').read_text()
 def test_pinned_physical_projection_matches_approved_inventory(self):
  spec=self.ps.split('function Get-ProGoInstallerPayloadNames {',1)[1].split('function Get-ProGoInstallPayload',1)[0]
  required=re.findall(r"'([^']+)'",spec.split('$required = @(',1)[1].split(')',1)[0])
  scripts=re.findall(r"'([^']+)'",spec.split('$scriptFiles = @(',1)[1].split(')',1)[0])
  expected=[x for x in required if x not in ('README.md','README.ru.md','LICENSE.md')]+['scripts/'+x for x in scripts]
  actual=re.findall(r'"([^"]+)"',self.cs.split('LegacyOperationLeaves = new[] {',1)[1].split('};',1)[0])
  self.assertEqual(actual,expected)
  for t in ('physical.MatchesPlan(expected)','LegacySameEntry(saved[i], entries[i])','record["Sequence"]',' as System.Collections.IList'):
   self.assertIn(t,self.cs)
 def test_same_handle_only_and_final_positive_proof(self):
  op=self.operation
  self.assertEqual(op.count('SetFileInformationByHandle('),1)
  self.assertIn('SetFileInformationByHandle(lease.target.SafeFileHandle',op)
  self.assertLess(op.index('VerifyLegacyOperationEvidence'),op.index('SetFileInformationByHandle'))
  self.assertLess(op.index('VerifyLegacyOperationPlan'),op.index('SetFileInformationByHandle'))
  self.assertNotRegex(op,r'File\.(Delete|Move|Replace)|OpenLegacyForDisposition')
  for t in ('proof.StorageOrigin != LegacyOrigin','LegacyRestartSealDocument','LegacyPreDeleteDocument','proof.DocumentHash'):
   self.assertIn(t,self.cs)
 def test_cached_acceptance_precedes_cancellation_and_no_false_success(self):
  self.assertLess(self.operation.index('if (lease.lastResult != null)'),self.operation.index('token.ThrowIfCancellationRequested()'))
  for t in ('attempted = true; known = false;','known = true;','accepted-context-conflict-unrecorded','indeterminate','physical.WithPins<LegacyOwnedDeleteResult>'):
   self.assertIn(t,self.operation)
  result=self.cs.split('public sealed class LegacyOwnedDeleteResult',1)[1].split('private static System.Collections.Generic.IDictionary',1)[0]
  for p in ('DurableOutcomeRecorded','WholeRollbackVerified','InstallSucceeded'):
   self.assertIn('bool '+p+' { get { return false; } }',result)
 def test_live_context_only_and_no_installer_integration(self):
  for t in ('Restore-ProGoInstallerLegacyReferenceEvidence','Open-ProGoInstallerLegacyPhysicalPins','Open-ProGoInstallerLegacyDeletionPins','DeleteOwnedOnce($physical,$CancellationToken)','$physical.Dispose()','$original.Dispose()'):
   self.assertIn(t,self.wrapper)
  self.assertNotIn('Get-ProGoInstallerLegacyBoundaryObservation',self.wrapper)
  self.assertNotIn('GetNewClosure',self.wrapper)
  self.assertIn('LegacyOperationNamespace(snapshotRoot, expected)',self.cs)
  self.assertIn('LegacyOperationNamespace(journalRoot, expected)',self.cs)
  self.assertNotIn('Invoke-ProGoInstallerLegacyOwnedDeletion',self.ps.split('function Open-ProGoInstallerJournalAdmission',1)[1])
 def test_fifteen_adversarial_cases_and_actual_child(self):
  cases=re.findall(r"'([^']+)'",self.fixture.split('$cases = @(',1)[1].split(')',1)[0])
  self.assertEqual(len(cases),15);self.assertEqual(len(set(cases)),15)
  for t in ('equal-byte-replacement','foreign-appeared-name','target-metadata-change','accepted-context-change','accepted-persistence-fail','cancel-during-check','late-cancel-no-replay','incomplete-physical-plan'):
   self.assertIn(t,cases)
  for t in ('[IO.FileMode]::CreateNew','[object]::ReferenceEquals($result,$again)','LEGACY_OWNED_CHILD blocked=True; replayPerformed=False','WaitForExit(20000)'):
   self.assertIn(t,self.fixture)
 def test_once_wired_without_privilege_or_timeout_changes(self):
  parent=(ROOT/'tests/InstallerLegacyDurableInputTests.ps1').read_text()
  self.assertEqual(parent.count("& (Join-Path $Root 'tests\\InstallerLegacyOwnedOperationTests.ps1')"),1)
  for t in ('AdjustTokenPrivileges','GetThreadWaitChain','ReadProcessMemory','Start-Sleep'):
   self.assertNotIn(t,self.fixture+self.operation)
if __name__=='__main__':unittest.main()
