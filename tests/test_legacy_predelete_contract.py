"""Static pre-delete intent contracts only; not native execution or crash simulation."""
import pathlib
import re
import unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class LegacyPreDeleteStaticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cs=(ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        cls.native=cls.cs.split('        private string LegacyPreDeletePath',1)[1].split('        internal sealed class LegacyObservation',1)[0]
        cls.ps=(ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        cls.binding=cls.ps.split('# Pre-delete admission is intent,',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
        cls.fixture=(ROOT/'tests/InstallerLegacyPreDeleteTests.ps1').read_text()
    def test_verified_graph_before_createnew_and_readback(self):
        body=self.native.split('public bool PersistLegacyPreDeleteAdmission',1)[1].split('public string GetLegacyPreDeleteAdmissionState',1)[0]
        self.assertLess(body.index('RehydrateLegacyJournalReference'),body.index('FileMode.CreateNew'))
        for token in ('stream.Flush(true)','origin != LegacyOrigin(stream.SafeFileHandle)','ReadLegacyReferenceBytes(current)','token.ThrowIfCancellationRequested(); return true;'):
            self.assertIn(token,body)
        for field in ('LegacyRestartSealDocument(operation, snapshot, reference, sealOrigin)','Digest(sealBytes)','admissionOrigin'):
            self.assertIn(field,self.native)
    def test_no_mutation_or_unsafe_state(self):
        self.assertNotRegex(self.native,r'File\.(Delete|Move|Replace)|SetFileInformation|DllImport|AdjustTokenPrivileges')
        state=self.native.split('public string GetLegacyPreDeleteAdmissionState',1)[1]
        self.assertEqual(set(re.findall(r'"(not-applied|blocked|applied|restored|success)"',state)),{'not-applied','blocked'})
        self.assertIn('OpenLegacyRestartRead(LegacyPreDeletePath(operation))',state)
        for flag in ('MutationAllowed = $false','FullRollback = $false','InstallSucceeded = $false'):
            self.assertIn(flag,self.binding)
    def test_independent_journal_and_after_check(self):
        for token in ('Get-ProGoInstallerLegacySourceContext $Backup','Restore-ProGoInstallerLegacyReferenceEvidence','Get-ProGoInstallerLegacyReferenceDocument $context $reference.Source','$again.Origin -cne $reference.Origin','$again.Hash -cne $reference.Hash'):
            self.assertIn(token,self.binding)
        tail=self.ps.split('function Open-ProGoInstallerJournalAdmission',1)[1]
        self.assertNotIn('New-ProGoInstallerLegacyPreDeleteAdmission',tail)
        self.assertNotIn('Get-ProGoInstallerLegacyPreDeleteState',tail)
    def test_twenty_three_cases_and_restart_without_witness(self):
        cases=re.findall(r"'([^']+)'",self.fixture.split('$scenarios = @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),23);self.assertEqual(len(set(cases)),23)
        for name in ('original-absent','foreign-admission','conflict','partial-after','admission-replaced-same-bytes','reparse-admission','cancel-before'):
            self.assertIn(name,cases)
        for token in ('LEGACY_ADMISSION_CHILD state=not-applied','powershell.exe','WaitForExit(20000)','$backup = $null; $reference = $null; $admission = $null','deletion/power-loss NOT_RUN','[OperationCanceledException]'):
            self.assertIn(token,self.fixture)
    def test_nested_once_and_no_protected_calls(self):
        parent=(ROOT/'tests/InstallerLegacyDurableInputTests.ps1').read_text(encoding='utf-8-sig')
        call="& (Join-Path $Root 'tests\\InstallerLegacyPreDeleteTests.ps1')"
        self.assertEqual(parent.count(call),1);self.assertLess(parent.index(call),parent.index("Write-Host ('Durable input tests PASS:"))
        self.assertNotIn('InstallerLegacyPreDeleteTests',(ROOT/'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig'))
        for forbidden in ('GetThreadWaitChain','AdjustTokenPrivileges','ReadProcessMemory','Start-Sleep'):
            self.assertNotIn(forbidden,self.fixture)
    def test_native_delimiter_sanity_only(self):
        text=re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',self.native,flags=re.S)
        stack=[];pairs={')':'(',']':'[','}':'{'}
        for char in text:
            if char in '([{':stack.append(char)
            elif char in pairs:
                self.assertTrue(stack);self.assertEqual(stack.pop(),pairs[char])
        self.assertEqual(stack,[])
if __name__=='__main__':unittest.main()
