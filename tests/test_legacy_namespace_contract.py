"""Static namespace-observation contracts, not native execution or atomic namespace proof."""
import pathlib,re,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class NamespaceObservationTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
  cls.ns=cls.ps.split('function Test-ProGoInstallerLegacyNamespace',1)[1].split('# Read-only operation-boundary rehearsal',1)[0]
  cls.boundary=cls.ps.split('function Get-ProGoInstallerLegacyBoundaryObservation',1)[1].split('# Exact owned object only;',1)[0].split('function Open-ProGoInstallerJournalAdmission',1)[0]
  cls.fixture=(ROOT/'tests/InstallerLegacyNamespaceTests.ps1').read_text()
 def test_anchor_and_whitelist_precede_namespace_enumeration(self):
  for t in ('$Reference -isnot [ProGo.ApplicationShortcuts+LegacyJournalReferenceEvidence]','$digest -cne $Reference.Hash','$saved.Context.Operation -cne $operation','$saved.Context.SnapshotHash -cne $Reference.Source.SnapshotHash'):
   self.assertIn(t,self.ns)
  self.assertLess(self.ns.index('$digest -cne $Reference.Hash'),self.ns.index('$saved = $text | ConvertFrom-Json'))
  self.assertLess(self.ns.index('$entry.Relative -cnotin $allowed'),self.ns.index('EnumerateFileSystemEntries'))
 def test_exact_file_directory_and_journal_names(self):
  for t in ('[StringComparer]::Ordinal',"$files.Add('installer-snapshot.json')",'if (-not $entry.Exists) { continue }','-not $directories.Contains($relative)','-not $files.Contains($relative)','$fileCount -ne $files.Count','admit-{0:D6}.json','$count -eq $names.Count','$actualFiles.SetEquals($files)','$actualNames.SetEquals($names)'):
   self.assertIn(t,self.ns)
  self.assertLess(self.ns.index('$attributes -band [IO.FileAttributes]::ReparsePoint'),self.ns.index('$pending.Push($path)'))
 def test_boundary_leases_and_repeated_checks(self):
  b=self.boundary
  self.assertLess(b.index('Open-ProGoInstallerLegacyPhysicalPins'),b.index('Test-ProGoInstallerLegacyNamespace'))
  self.assertLess(b.index('Test-ProGoInstallerLegacyNamespace'),b.index('Open-ProGoInstallerLegacyDeletionPins'))
  self.assertEqual(b.count('Test-ProGoInstallerLegacyNamespace $reference'),3)
  for t in ('$physical.VerifyPinned($CancellationToken)','$original.IsPinned','try { if ($null -ne $original) { $original.Dispose() } }','finally { if ($null -ne $physical) { $physical.Dispose() } }'):
   self.assertIn(t,b)
 def test_no_mutation_live_wiring_or_permission_state(self):
  for block in (self.ns,self.boundary):
   self.assertNotRegex(block,r'\[IO\.File\]::(?:Delete|Move|Write)|SetFileInformation|MutationAllowed\s*=\s*\$true')
   self.assertIn('$cause -is [OperationCanceledException]',block)
  self.assertEqual(set(re.findall(r"return '(.*?)'",self.boundary)),{'blocked','observed-only'})
  self.assertNotIn('Get-ProGoInstallerLegacyBoundaryObservation',self.ps.split('function Open-ProGoInstallerJournalAdmission',1)[1])
 def test_real_namespace_mutations_preserve_foreign_objects(self):
  for t in ('directory-pins-do-not-reserve-absent-child','appeared-absent-leaf-refused','unchanged-pinned-material-does-not-prove-namespace','current-absence-observation-is-not-ABA-history-or-deletion-proof','unknown-journal-record-refused','unknown-snapshot-directory-refused-without-descent','missing-required-object-refused','cancel-propagates-not-observed-success','read-only-helper-preserves-all-private-and-foreign-bytes'):
   self.assertIn(t,self.fixture)
  self.assertIn('[IO.File]::WriteAllText($appeared',self.fixture)
  self.assertNotIn('[IO.File]::Delete',self.fixture)
 def test_nested_once_old_cases_and_fresh_process_required(self):
  p=(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text()
  self.assertEqual(p.count('Test-ProGoLegacyNamespaceFixture $scenario'),1)
  self.assertIn('$script:LegacyNamespaceChecks -eq 41',p)
  self.assertIn('LEGACY_NAMESPACE_CHILD observedOnly=True; mutationPerformed=False',p)
  self.assertEqual(len(re.findall(r"'([^']+)'",p.split('$scenarios = @(',1)[1].split(')',1)[0])),23)
  for t in ('GetThreadWaitChain','AdjustTokenPrivileges','ReadProcessMemory','Start-Sleep'):
   self.assertNotIn(t,self.ns+self.boundary+self.fixture)
if __name__=='__main__':unittest.main()
