"""Static regression contracts, not native/Flush/crash or Windows execution proof."""
from pathlib import Path
import re, unittest
ROOT = Path(__file__).resolve().parents[1]
class AcceptedOutcomeContract(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.cs = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
  cls.block = cls.cs.split('// A durable observation of one live accepted disposition',1)[1].split('private static System.Collections.Generic.IDictionary',1)[0]
  cls.fixture = (ROOT/'tests/InstallerLegacyOwnedOperationTests.ps1').read_text()
 def test_four_distinct_truths(self):
  for name in ('PhysicalUnlinkVerified','WholeRollbackVerified','InstallSucceeded'):
   self.assertIn('bool '+name+' { get { return false; } }',self.block)
  for term in ('AcceptedResult','DurableOutcomeRecorded','accepted-disposition','unlink-not-proven','rollback-not-proven'):
   self.assertIn(term,self.block)
 def test_durable_no_overwrite_exact_readbacks(self):
  for term in ('FileMode.CreateNew','FileOptions.WriteThrough','stream.Flush(true)','ReadLegacyReferenceBytes(stream)',
               'OpenLegacyAcceptedPublicationRead(path)','origin != LegacyOrigin(current.SafeFileHandle)','Equal(expected, LegacyAcceptedDocument'):
   self.assertIn(term,self.block)
  self.assertNotRegex(self.block,r'File\.(Delete|Move|Replace)|SetFileInformationByHandle')
  # Readback must share our live writer's WRITE access without giving others write access.
  self.assertIn('OpenLegacyForDisposition(path, 0x80000000u, 3, IntPtr.Zero, 3, 0x00200000u',self.block)
  self.assertIn('FileAccess.ReadWrite, FileShare.Read, 4096',self.block)
 def test_full_live_evidence_and_context_binding(self):
  for term in ('lock (lease.gate)','physical.WithPins<LegacyAcceptedOutcome>','lease.resources == null',
               'lease.lastResult.OperationAccepted','lease.lastResult.AcceptanceKnown','lease.lastResult.TargetHandleClosed',
               'lease.lastResult.ContextObservedAfterOperation','VerifyLegacyOperationEvidence(lease, original)',
               'VerifyLegacyOperationPlan(lease, physical)','physical.MatchesOwner(this)','physical.VerifyPinned',
               'proof.SnapshotHash','proof.DocumentHash','lease.reference.Origin','lease.reference.Hash',
               'LegacyOrigin(stream.SafeFileHandle)','PhysicalPinHash(stream)'):
   self.assertIn(term,self.block)
 def test_cancellation_and_failed_persistence_keep_acceptance(self):
  self.assertLess(self.block.index('if (lease.acceptedOutcome != null)'),self.block.index('token.IsCancellationRequested'))
  self.assertIn('new LegacyAcceptedOutcome(lease.lastResult, false, null, null)',self.block)
  self.assertIn('lease.acceptedOutcome = failure',self.block)
  self.assertNotIn('ThrowIfCancellationRequested',self.block)
  self.assertIn('try { File.GetAttributes(originalPath); return false; }',self.block)
 def test_original_cases_and_fourteen_receipt_cases(self):
  original=re.findall(r"'([^']+)'",self.fixture.split('$cases = @(',1)[1].split(')',1)[0])
  added=re.findall(r"'([^']+)'",self.fixture.split('$outcomeCases = @(',1)[1].split(')',1)[0])
  self.assertEqual(len(original),15); self.assertEqual(len(added),14); self.assertEqual(len(set(added)),14)
  for term in ('outcome-foreign','outcome-partial','outcome-cancel','outcome-context','outcome-disposed','outcome-tamper',
               'outcome-trailing','outcome-empty','outcome-replaced','outcome-metadata','outcome-foreign-target','outcome-evidence'):
   self.assertIn(term,added)
  self.assertIn("$scenario -in @('complete','outcome-complete')",self.fixture)
 def test_no_live_integration_or_deadline_change(self):
  ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
  self.assertNotIn('RecordAcceptedOutcome',ps)
  for term in ('AdjustTokenPrivileges','GetThreadWaitChain','ReadProcessMemory','Start-Sleep'):
   self.assertNotIn(term,self.block+self.fixture)
  self.assertIn('WaitForExit(20000)',self.fixture)
  self.assertIn('fresh-process refusal=2',self.fixture)
if __name__=='__main__': unittest.main()
