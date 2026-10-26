"""Static source/layout contracts only; no Windows/C#/PowerShell execution."""
import pathlib
import re
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class LegacyJournalReferenceStaticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ps = (ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        cls.binding = cls.ps.split('# Trusted live-session binding only.',1)[1].split('function Open-ProGoInstallerJournalAdmission',1)[0]
        cls.cs = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        cls.native = cls.cs.split('// Installer-only prerequisite: an immutable reference witness',1)[1].split('internal sealed class LegacyObservation',1)[0]
        cls.fixture = (ROOT/'tests/InstallerLegacyJournalReferenceTests.ps1').read_text()
    def test_actual_snapshot_journal_no_caller_hash_authority(self):
        for token in ('Assert-ProGoInstallerSnapshotMaterial $Backup','Assert-ProGoInstallerRuntime $Backup','Assert-ProGoInstallerJournal $Backup.Journal $Backup',"'admit-{0:D6}.json'",'$record.Text -cne $expected','[IO.File]::ReadAllText($path) -cne $expected','$Backup.Journal.Records.Count -ne $allowed.Count'):
            self.assertIn(token,self.binding)
        signature=self.binding.split('function New-ProGoInstallerLegacySourceReference(',1)[1].split(') {',1)[0]
        self.assertNotIn('SnapshotHash',signature); self.assertNotIn('$Proof',signature)
        self.assertIn("$proof = $manager.PersistLegacyRollbackSource($source,$operation,$context.SnapshotHash",self.binding)
    def test_native_reference_is_bounded_immutable_no_mutation_authority(self):
        for flag in ('MutationAllowed','FullRollback','InstallSucceeded'):
            self.assertIn('public bool '+flag+' { get { return false; } }',self.native)
        self.assertIn('internal LegacyJournalReferenceEvidence(',self.native)
        for token in ('document.Length > 1048576','FileMode.CreateNew','FileOptions.WriteThrough','stream.Flush(true)','(byte[])document.Clone()','reference.Origin != LegacyOrigin(stream.SafeFileHandle)','proof.StorageOrigin != LegacyOrigin(stream.SafeFileHandle)','source.Owner != this'):
            self.assertIn(token,self.native)
        self.assertNotRegex(self.native,r'File\.(Delete|Move|Replace)|DllImport|SetFileInformation')
    def test_old_suites_and_nested_wiring(self):
        parent=(ROOT/'tests/InstallerLegacyDurableInputTests.ps1').read_text(encoding='utf-8-sig')
        call="& (Join-Path $Root 'tests\\InstallerLegacyJournalReferenceTests.ps1')"
        self.assertEqual(parent.count(call),1)
        self.assertLess(parent.index(call),parent.index("Write-Host ('Durable input tests PASS:"))
        suite=(ROOT/'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig')
        self.assertIn("{ 27 } elseif ($Partition -eq 'Recovery') { 6 } else { 33 }",suite)
        self.assertNotIn('InstallerLegacyJournalReferenceTests',suite)
    def test_twenty_real_cases_and_native_replacement_witness(self):
        cases=re.findall(r"'([^']+)'",self.fixture.split('$scenarios = @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),20);self.assertEqual(len(set(cases)),20)
        for case in ('caller-snapshot','foreign-journal','partial-reference','reference-replaced-same-bytes','source-replaced-same-bytes','original-replaced-same-bytes','runtime-after','reference-reparse','cancel-before'):
            self.assertIn(case,cases)
        self.assertIn('SetCreationTimeUtc($replace,$creation)',self.fixture)
        self.assertIn("'retained-native-object'",self.fixture)
        self.assertIn('mklink /J',self.fixture)
        self.assertIn('cleanup refuses linked descendants',self.fixture)
    def test_production_not_integrated_or_given_restore_authority(self):
        remainder=self.ps.replace('# Trusted live-session binding only.'+self.binding,'')
        for name in ('New-ProGoInstallerLegacySourceReference','Test-ProGoInstallerLegacySourceReference'):
            self.assertNotIn(name,remainder)
        self.assertNotIn('InstallWithStartupEvidence',self.binding)
        self.assertNotIn('TryRollback',self.binding)
        self.assertGreaterEqual(self.binding.count('Get-ProGoInstallerLegacySourceContext $Backup'),5)
    def test_added_csharp_delimiter_sanity_not_compilation(self):
        # Lexical delimiter balance catches editing errors; deliberately NOT a C# compiler test.
        text=re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',self.native,flags=re.S)
        stack=[];close={')':'(',']':'[','}':'{'}
        for char in text:
            if char in '([{':stack.append(char)
            elif char in close:
                self.assertTrue(stack);self.assertEqual(stack.pop(),close[char])
        self.assertEqual(stack,[])
if __name__=='__main__':unittest.main()
