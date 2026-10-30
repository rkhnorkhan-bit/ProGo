"""Static test-only safety contracts; not native confirmation or a mutation protocol."""
import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class MutationThreatContractTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.fixture=(ROOT/'tests/InstallerLegacyMutationThreatTests.ps1').read_text()
  cls.parent=(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text()
 def test_only_owned_synthetic_complete_scope(self):
  for t in ("GetFileName($case) -cne 'complete'",'ProGo-legacy-predelete-','GetTempPath()','ReparsePoint',"$runtime = Join-Path $Install 'ProGo.exe'"):
   self.assertIn(t,self.fixture)
 def test_actual_metadata_operation_not_string_simulation(self):
  self.assertIn('[LegacyMetadataThreatFixture]::SetCreation($runtime,$creation.AddMinutes(1))',self.fixture)
  for t in ('metadata-can-change-while-object-and-bytes-stay-pinned','-not $physical.VerifyPinned($token)','Get-FileHash -LiteralPath $runtime'):
   self.assertIn(t,self.fixture)
 def test_aba_and_no_authority_assertions(self):
  for t in ('restored-current-origin-does-not-prove-no-ABA-history','-not $physical.MutationAllowed','-not $original.MutationAllowed','global-freeze NOT_CLAIMED'):
   self.assertIn(t,self.fixture)
 def test_restore_before_dispose_and_cleanup(self):
  self.assertLess(self.fixture.index('if ($touched -and $null -ne $physical)'),self.fixture.index('$physical.Dispose()'))
  self.assertIn('$original.Dispose()',self.fixture)
  self.assertIn('[IO.FileShare]::None',self.fixture)
  self.assertIn('private-foreign-bytes-metadata-and-cleanup-preserved',self.fixture)
 def test_once_nested_without_losing_existing_checks(self):
  self.assertEqual(self.parent.count('Test-ProGoLegacyMutationThreatFixture $journal'),1)
  self.assertIn("if ($scenario -ceq 'complete') { Test-ProGoLegacyMutationThreatFixture",self.parent)
  for n in ('LegacyMutationThreatChecks -eq 6','LegacyNamespaceChecks -eq 41','LegacyPhysicalChecks -eq 50','LegacyPinChecks -eq 39'):
   self.assertIn(n,self.parent)
 def test_no_deletion_hooks_privileges_or_retry(self):
  for t in ('GetThreadWaitChain','AdjustTokenPrivileges','ReadProcessMemory','Start-Sleep','[IO.File]::Delete','[IO.File]::Move','[IO.File]::WriteAll'):
   self.assertNotIn(t,self.fixture)
  self.assertEqual(self.fixture.count('[DllImport('),2)
  self.assertIn('Open(path, 0x100u, 7, IntPtr.Zero, 3, 0x00200080u',self.fixture)
  self.assertIn('using (var handle = Open',self.fixture)
if __name__=='__main__':unittest.main()
