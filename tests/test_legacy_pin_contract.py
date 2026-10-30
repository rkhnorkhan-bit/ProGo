"""Static pin-only gate contracts; these tests do not execute Windows native sharing."""
import pathlib
import re
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class LegacyPinContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cs = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        cls.body = cls.cs.split('// Pin-only prerequisite.',1)[1].split('private string LegacyPreDeletePath',1)[0]
        cls.ps = (ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        cls.fixture = (ROOT/'tests/InstallerLegacyDeletionPinsTests.ps1').read_text()
    def test_complete_graph_pinned_before_original_admission(self):
        for path in ('LegacySourcePath','LegacyReferencePath','LegacyRestartSealPath','LegacyPreDeletePath'):
            self.assertLess(self.body.index('OpenLegacyRestartRead('+path),self.body.index('GetLegacyPreDeleteAdmissionState'))
        self.assertLess(self.body.index('GetLegacyPreDeleteAdmissionState'),self.body.index('0x80010000u'))
        self.assertIn('0x80010000u, 1, IntPtr.Zero, 3, 0x00200000u',self.body)
    def test_exact_origin_and_bytes_not_caller_flags(self):
        for token in ('reference.Source.InstallIdentity != Identity(root)','reference.Source.LocationIdentity != Identity(path)',
                      'reference.Source.Operation != operation','reference.Source.SnapshotHash != verifiedSnapshot',
                      'reference.Source.OriginIdentity != origin','reference.Source.Hash != Digest(bytes)',
                      'origin != LegacyOrigin(handle)','(byte[])document.Clone()'):
            self.assertIn(token,self.body)
    def test_no_mutation_or_outcome_api(self):
        self.assertNotRegex(self.body,r'File\.(Delete|Move|Replace)|SetFileInformation|DllImport|AdjustTokenPrivileges')
        for prop in ('MutationAllowed','FullRollback','InstallSucceeded'):
            self.assertIn('bool '+prop+' { get { return false; } }',self.body)
        self.assertIn('finally { if (!transferred) ReleaseLegacyDeletionPins(held); }',self.body)
        self.assertIn('catch (OperationCanceledException) { throw; }',self.body)
    def test_paths_only_binding_and_no_live_wiring(self):
        binding=self.ps.split('function Open-ProGoInstallerLegacyDeletionPins',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
        self.assertIn('Restore-ProGoInstallerLegacyReferenceEvidence',binding)
        self.assertIn('if ($null -ne $pins) { $pins.Dispose() }',binding)
        self.assertNotIn('Open-ProGoInstallerLegacyDeletionPins',self.ps.split('function Open-ProGoInstallerJournalAdmission',1)[1])
        self.assertNotIn('Get-ProGoInstallerLegacySourceContextFromPins',self.ps)
    def test_real_sharing_negative_cases_and_disposal(self):
        for token in ('[IO.FileAccess]::ReadWrite','[IO.File]::Move','catch [IO.IOException]',
                      '$pins.Dispose(); $pins.Dispose()','busy-original-refuses-and-unwinds','[OperationCanceledException]',
                      'all-owned-resources-released'):
            self.assertIn(token,self.fixture)
        parent=(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text()
        self.assertIn('$script:LegacyPinChecks -eq 39',parent)
        self.assertIn('LEGACY_PINS_CHILD verified=True; mutationPerformed=False',parent)
        self.assertEqual(parent.count('Test-ProGoLegacyDeletionPinsFixture $scenario'),1)
    def test_no_diagnostic_privileges_or_retry(self):
        for forbidden in ('GetThreadWaitChain','SeDebug','AdjustTokenPrivileges','Start-Sleep','ReadProcessMemory'):
            self.assertNotIn(forbidden,self.body+self.fixture)
        self.assertEqual(len(re.findall(r"'([^']+)'",(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text().split('$scenarios = @(',1)[1].split(')',1)[0])),23)
if __name__ == '__main__': unittest.main()
