"""Static restart ownership/layout contracts, not Windows or crash execution."""
import pathlib
import re
import unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class LegacyRestartStaticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cs=(ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        cls.native=cls.cs.split('        private string LegacyRestartSealPath',1)[1].split('        internal sealed class LegacyObservation',1)[0]
        cls.ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        cls.restore=cls.ps.split('function Restore-ProGoInstallerLegacyReferenceEvidence',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
        cls.fixture=(ROOT/'tests/InstallerLegacyRestartTests.ps1').read_text()
    def test_no_previous_process_objects_or_caller_hash(self):
        signature=self.restore.split(') {',1)[0]
        for forbidden in ('$Backup','$Reference','$Proof','$Hash'):
            self.assertNotIn(forbidden,signature)
        for required in ('Get-ProGoInstallerLegacySourceContext $backup','RehydrateLegacySource($operation,$snapshotHash)','RehydrateLegacyJournalReference($proof,$operation,$snapshotHash', '$document -cne $savedText'):
            self.assertIn(required,self.restore)
        self.assertLess(self.restore.index('$entry.Relative -cnotin $allowed'),self.restore.index('Get-ProGoInstallerLegacySourceContext $backup'))
    def test_seal_binds_actual_native_objects_and_never_overwrites(self):
        for required in ('FileMode.CreateNew','stream.Flush(true)','LegacyRestartSealDocument(operation, verifiedSnapshot, reference, LegacyOrigin(seal.SafeFileHandle))','reference.Origin','reference.Hash','source.StorageOrigin','source.DocumentHash','source.OriginIdentity','source.LocationIdentity','source.InstallIdentity'):
            self.assertIn(required,self.native)
        self.assertIn('!Equal(expectedSeal, ReadLegacyReferenceBytes(seal))',self.native)
        self.assertIn('!Equal(expected, ReadLegacySourceDocument(stream))',self.native)
        self.assertIn('0x00200000u',self.native)
        self.assertIn('LegacyOrigin(handle);',self.native)
        self.assertIn('OpenLegacyRestartRead(sealPath)',self.native)
        self.assertNotRegex(self.native,r'File\.(Delete|Move|Replace)|DllImport|AdjustTokenPrivileges')
    def test_original_required_no_mutation_in_production_flow(self):
        self.assertIn('CaptureLegacyRollbackSource(CancellationToken.None)',self.native)
        self.assertIn('if (source == null) return null;',self.native)
        main=self.ps.split("$Root = Split-Path -Parent $PSScriptRoot",1)[1]
        # Exact call appears in definition only; no normal installer/failure route invokes it.
        self.assertEqual(main.count('function Restore-ProGoInstallerLegacyReferenceEvidence'),1)
        tail=main.split('function Open-ProGoInstallerJournalAdmission',1)[1]
        self.assertNotIn('Restore-ProGoInstallerLegacyReferenceEvidence',tail)
        self.assertNotIn('Complete-ProGoInstallerLegacyRestartSeal',tail)
    def test_twenty_six_cases_and_actual_child(self):
        cases=re.findall(r"'([^']+)'",self.fixture.split('$scenarios = @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),26);self.assertEqual(len(set(cases)),26)
        for name in ('no-seal','seal-replaced-same-bytes','reference-replaced-same-bytes','source-replaced-same-bytes','missing-original','reparse-seal','private-entry-claim'):
            self.assertIn(name,cases)
        for token in ('LEGACY_RESTART_CHILD verified=True','powershell.exe','-Child -CaseDirectory','WaitForExit(20000)','$backup = $null; $reference = $null; $manager = $null','power-loss NOT_RUN'):
            self.assertIn(token,self.fixture)
    def test_nested_once_original_partition_contract_unchanged(self):
        parent=(ROOT/'tests/InstallerLegacyDurableInputTests.ps1').read_text(encoding='utf-8-sig')
        call="& (Join-Path $Root 'tests\\InstallerLegacyRestartTests.ps1')"
        self.assertEqual(parent.count(call),1)
        self.assertLess(parent.index(call),parent.index("Write-Host ('Durable input tests PASS:"))
        self.assertNotIn('InstallerLegacyRestartTests',(ROOT/'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig'))
        for forbidden in ('GetThreadWaitChain','AdjustTokenPrivileges','ReadProcessMemory','Start-Sleep'):
            self.assertNotIn(forbidden,self.fixture)
    def test_native_delimiters_only_not_compiler(self):
        text=re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',self.native,flags=re.S)
        stack=[];pairs={')':'(',']':'[','}':'{'}
        for char in text:
            if char in '([{':stack.append(char)
            elif char in pairs:
                self.assertTrue(stack);self.assertEqual(stack.pop(),pairs[char])
        self.assertEqual(stack,[])
if __name__=='__main__':unittest.main()
