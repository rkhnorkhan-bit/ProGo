"""Read-only source contracts; no C#/Windows execution or filesystem race simulation."""
import pathlib
import re
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class DurableSourceStaticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.code = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        cls.write = cls.code.split('public LegacyDurableSourceEvidence PersistLegacyRollbackSource',1)[1].split('public byte[] ReadDurableLegacyRollbackBytes',1)[0]
        cls.read = cls.code.split('public byte[] ReadDurableLegacyRollbackBytes',1)[1].split('internal sealed class LegacyObservation',1)[0]
        cls.fixture = (ROOT/'tests/InstallerLegacyDurableSourceTests.ps1').read_text(encoding='utf-8-sig')
    def test_create_only_no_destructive_fallback(self):
        self.assertIn('FileMode.CreateNew', self.write)
        self.assertNotRegex(self.write+self.read, r'File\.(Delete|Move|Replace|WriteAll)|FileMode\.(Create,|OpenOrCreate|Truncate)')
    def test_proof_only_after_flush_and_two_readbacks(self):
        parts=['stream.Write(document', 'stream.Flush(true)', 'ReadLegacySourceDocument(stream)', 'AssertLegacySourceOriginal(source, original, bytes); CheckLocation(path)', 'ReadLegacySourceDocument(current)', 'return new LegacyDurableSourceEvidence']
        positions=[self.write.index(x) for x in parts]
        self.assertEqual(positions, sorted(positions))
    def test_full_native_identity_not_hash_only(self):
        origin=self.code.split('private static string LegacyOrigin(',1)[1].split('private LegacyObservation',1)[0]
        for field in ('info.Volume','info.IndexHigh','info.IndexLow','info.Creation.dwHighDateTime','info.Creation.dwLowDateTime','info.Attributes & 0x410'):
            self.assertIn(field,origin)
        self.assertIn('proof.StorageOrigin != LegacyOrigin(stream.SafeFileHandle)',self.read)
    def test_external_evidence_and_receipt_binding(self):
        for check in ('proof.Operation != operation','proof.SnapshotHash != snapshotHash','proof.InstallIdentity != Identity(root)','proof.LocationIdentity != Identity','receipt.OriginIdentity != proof.OriginIdentity','receipt.BeforeHash != proof.Hash','receipt.OwnedHash != proof.Hash','Digest(document) != proof.DocumentHash','!Equal(document, expected)'):
            self.assertIn(check,self.read)
        self.assertEqual(self.read.count('VerifyLegacyInstallEvidence(receipt)'),2)
        self.assertIn('catch { return null; }',self.read)
    def test_fixed_location_pins_and_bounded_document(self):
        for body in (self.write,self.read):
            for pin in ('PinLegacySourceDirectory(root)','PinLegacySourceDirectory(Path.Combine(root, "backups"))','PinLegacySourceDirectory(menu)'):
                self.assertIn(pin,body)
        self.assertIn('stream.Length > 70000',self.code)
        self.assertIn('bytes.Length > 65536',self.code)
        self.assertIn('offset != bytes.Length',self.code)
        self.assertIn('operation.ToString("N") + ".bin"',self.code)
    def test_twenty_distinct_existing_cases_and_limits(self):
        cases=re.findall(r"'([^']+)'",self.fixture.split('$scenarios = @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),20); self.assertEqual(len(set(cases)),20)
        for case in ('partial-existing','partial-after','foreign-storage-same-bytes','original-replaced-before','redirected-backups','busy-read','cancel-before'):
            self.assertIn(case,cases)
        # Explicitly record absence instead of claiming unimplemented native coverage.
        self.assertNotIn('oversize',cases)
    def test_no_restore_or_terminal_authority(self):
        proof=self.code.split('public sealed class LegacyDurableSourceEvidence',1)[1].split('private static bool LegacySourceHash',1)[0]
        for flag in ('MutationAllowed','FullRollback','InstallSucceeded'):
            self.assertIn('public bool '+flag+' { get { return false; } }',proof)
        self.assertIn('internal LegacyDurableSourceEvidence(',proof)
    def test_envelope_size_arithmetic_only(self):
        header=len(b'PROGO-LEGACY-SOURCE-1\n')+33+6*65+9
        self.assertLessEqual(header+65536,70000)
        self.assertGreater(header,0)
class DurableInputPreparedStaticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.text = (ROOT/'tests/InstallerLegacyDurableInputTests.ps1').read_text()
    def test_twelve_distinct_prepared_cases(self):
        cases=re.findall(r"'([^']+)'",self.text.split('$scenarios = @(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),12); self.assertEqual(len(set(cases)),12)
        self.assertEqual(sum(x.startswith('write-') for x in cases),6)
        self.assertEqual(sum(x.startswith('read-') for x in cases),6)
    def test_positive_control_and_no_false_refusal(self):
        self.assertIn('$cause -isnot [IO.IOException]',self.text)
        self.assertIn('positive control exact original bytes',self.text)
        self.assertIn('valid request remains readable after rejected writer input',self.text)
        self.assertLess(self.text.index('positive control exact original bytes'),self.text.index("'read-oversize' {"))
    def test_real_file_mutations_and_cleanup(self):
        for token in ('New-Object byte[] 70001','[byte[]]@()','$stream.WriteByte(1)','Check-InputInventory $case $beforeRead','ReparsePoint','cleanup refuses unowned root','FileShare]::None'):
            self.assertIn(token,self.text)
        self.assertNotRegex(self.text,r'(?i)DllImport|Start-Process|Set-Acl|Set-Service|Start-Sleep|Invoke-WebRequest')
    def test_registered_once_in_recovery_only(self):
        suite = (ROOT/'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig')
        call = "& (Join-Path $Root 'tests\\InstallerLegacyDurableInputTests.ps1')"
        self.assertEqual(suite.count(call),1)
        recovery = suite.split('# PARTITION Recovery BEGIN:',1)[1].split('# PARTITION Recovery END',1)[0]
        self.assertIn(call,recovery)
        self.assertNotIn('InstallerLegacyDurableInputTests', (ROOT/'scripts/Test-ProGo.ps1').read_text(encoding='utf-8-sig'))
        self.assertNotIn('Reflection',self.text)
if __name__ == '__main__': unittest.main()
