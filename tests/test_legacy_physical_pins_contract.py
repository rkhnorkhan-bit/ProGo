"""Static contracts only, not Windows native sharing or power-loss tests."""
import pathlib,re,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class PhysicalPinsContractTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.cs=(ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
  cls.native=cls.cs.split('// Existing physical material only.',1)[1].split('private string LegacyPreDeletePath',1)[0]
  cls.ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
  cls.wrapper=cls.ps.split('function Open-ProGoInstallerLegacyPhysicalPins',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
  cls.fixture=(ROOT/'tests/InstallerLegacyPhysicalPinsTests.ps1').read_text()
 def test_plan_anchored_before_leaves_and_revalidated_under_pins(self):
  b=self.wrapper
  self.assertLess(b.index('$documentHash -cne $reference.Hash'),b.index('$paths.Add'))
  self.assertLess(b.index('$entry.Relative -cnotin $allowed'),b.index('$paths.Add($entry.Relative)'))
  self.assertLess(b.index('PinLegacyPhysicalMaterial'),b.index('$again = Restore-ProGoInstallerLegacyReferenceEvidence'))
  for t in ('$again.Origin -cne $reference.Origin','$again.Hash -cne $reference.Hash','$pins.VerifyPinned($CancellationToken)'):
   self.assertIn(t,b)
 def test_all_existing_material_and_parents(self):
  for t in ('installer-snapshot.json',"$paths.Add($entry.Relative)",'if ($entry.Exists)','admit-{0:D6}.json'):
   self.assertIn(t,self.wrapper)
  for t in ('Path.GetDirectoryName(parent)','PinLegacySourceDirectory(parent)','OpenLegacyRestartRead(path)',"part == \"..\"",'!seen.Add(path)'):
   self.assertIn(t,self.native)
 def test_same_handles_origin_hash_and_dispose(self):
  for t in ('lock (gate)','LegacyOrigin(files[i].SafeFileHandle)','PhysicalPinHash(files[i])','if (held == null) return false;','ReleaseLegacyDeletionPins(resources)','if (!transferred) ReleaseLegacyDeletionPins(held)'):
   self.assertIn(t,self.native)
  self.assertIn('if ($null -ne $pins) { $pins.Dispose() }',self.wrapper)
 def test_no_mutation_live_wiring_or_authority(self):
  self.assertNotRegex(self.native,r'File\.(Delete|Move|Replace)|SetFileInformation|DllImport|AdjustTokenPrivileges')
  for p in ('MutationAllowed','FullRollback','InstallSucceeded'):
   self.assertIn('bool '+p+' { get { return false; } }',self.native)
  self.assertNotIn('Open-ProGoInstallerLegacyPhysicalPins',self.ps.split('function Open-ProGoInstallerJournalAdmission',1)[1])
 def test_native_fixture_coverage_and_child(self):
  for t in ('write-refused-','replacement-refused-','required-parent-rename-refused','partial-tamper-refused-before-pins-return','busy-context-fails-closed','native-invalid-plan-or-late-open-refuses','cancel-before-pinning','coexists-with-exact-original-pins'):
   self.assertIn(t,self.fixture)
  parent=(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text()
  self.assertIn('$script:LegacyPhysicalChecks -eq 50',parent)
  self.assertIn('LEGACY_PHYSICAL_CHILD verified=True; mutationPerformed=False',parent)
  self.assertEqual(parent.count('Test-ProGoLegacyPhysicalPinsFixture $scenario'),1)
 def test_bounded_no_privileged_probes(self):
  self.assertIn('relativeFiles.Length > 256',self.native)
  for t in ('GetThreadWaitChain','AdjustTokenPrivileges','ReadProcessMemory','Start-Sleep'):
   self.assertNotIn(t,self.native+self.fixture)
if __name__=='__main__':unittest.main()
