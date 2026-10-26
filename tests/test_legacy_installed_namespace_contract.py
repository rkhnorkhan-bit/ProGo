"""Static wiring checks; actual object/sharing/mutation proof belongs to Windows native40."""
from pathlib import Path
import re
import unittest
ROOT = Path(__file__).resolve().parents[1]
class InstalledNamespaceContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.native = (ROOT / 'src/ApplicationShortcuts.cs').read_text()
        cls.fixture = (ROOT / 'tests/InstallerLiveInstalledOutcomeTests.ps1').read_text()
        cls.installer = (ROOT / 'scripts/Install-ProGo.ps1').read_text()
    def test_only_live_chain_supplies_transition(self):
        self.assertIn('VerifyLegacyAcceptedOutcome(context.lease, context.physical, context)', self.native)
        self.assertIn('return owner.VerifyLegacyAcceptedOutcome(this, physical);', self.native)
        self.assertIn('VerifyLegacyOperationPlan(lease, physical) : VerifyLegacyOperationPlan(lease, physical, installedContext)', self.native)
        body = self.native.split('private bool VerifyLegacyOperationPlan(',1)[1].split('private static byte[] ReadLiveInstalledProgressBytes',1)[0]
        self.assertLess(body.index('physical.MatchesPlan(expected)'), body.index('AddLiveInstalledNamespace('))
        self.assertIn('LegacyOperationNamespace(snapshotRoot, expected) && LegacyOperationNamespace(journalRoot, expected)',body)
    def test_exact_owned_creation_and_handle(self):
        for term in ['FileMode.CreateNew,\n                        FileAccess.ReadWrite, FileShare.None',
                     'Object.ReferenceEquals(stream, installedPendingWriter)',
                     'Object.ReferenceEquals(stream, context.installedPendingWriter)',
                     'Object.ReferenceEquals(context.lease, lease)', 'Object.ReferenceEquals(context.physical, physical)',
                     'context.installedOrigin != LegacyOrigin(handle)',
                     'Equal(context.installedObservedBytes, ReadLiveInstalledProgressBytes(stream))',
                     'context.installedPendingWriter.Position = position',
                     'context.installedDocument == null || !Equal(context.installedDocument, context.installedObservedBytes)']:
            self.assertIn(term,self.native)
        self.assertIn('if ($null -ne $LiveContext) { $stream = $LiveContext.CreateInstalledPending() } else {',self.installer)
    def test_all_original_cases_and_real_namespace_negatives(self):
        cases = re.findall(r"'([^']+)'",self.fixture.split('$cases=@(',1)[1].split(')',1)[0])
        self.assertEqual(len(cases),40); self.assertEqual(len(set(cases)),40)
        for case in ['complete','pending-replaced','installed-replaced','occupied-pending','foreign-target',
                     'preexisting-installed','extra-journal','extra-after-seal','pending-bytes-changed','installed-extra','unowned-stream']:
            self.assertIn(case,cases)
        self.assertIn('SetCreationTimeUtc($pending,$creation)',self.fixture)
        self.assertIn('SetCreationTimeUtc($installed,$creation)',self.fixture)
        self.assertIn('-not $lease.VerifyAcceptedOutcome($physical)',self.fixture)
        self.assertIn('fresh-process refusal=2',self.fixture)
        self.assertNotIn('.Invoke(', self.fixture)
        self.assertNotIn('Get-FileHash', self.fixture)
    def test_actual_faults_and_no_restart_capability(self):
        for term in ['Invoke-ProGoInstallerSuccess','Write-ProGoInstallerOutcomeBytes','& $actualConfirm','& $actualPublish',
                     'LegacyRestoreReflectionFixture]::InvokeChecked','[IO.File]::Move($pending,$retained)',
                     "'real success handler consumes current live state and existing installed transaction result'",
                     'repeat observes only, never retries publication/deletion or cleans partial artifact']:
            self.assertIn(term,self.fixture)
        self.assertIn('ActualInstallationCompleted = $false; InstallSucceeded = $false',self.installer)
    def test_custom_shortcut_fixture_is_valid_before_preparation_and_preserved(self):
        f = self.fixture
        for invalid in ["WriteAllText($legacy,'synthetic custom Legacy')",
                        "WriteAllText($startupLink,'synthetic customized Startup')",
                        "WriteAllText($legacy,'synthetic foreign Legacy')"]:
            self.assertNotIn(invalid, f)
        self.assertIn('New-Object -ComObject WScript.Shell', f)
        self.assertIn('$link.TargetPath=$Target;$link.Arguments=$Arguments', f)
        self.assertIn('Test-LiveInstalledCustomShortcut $Path $Target $Arguments', f)
        self.assertLess(f.index("New-LiveInstalledCustomShortcut $legacy $foreignTarget '--synthetic-legacy'"),
                        f.index('$prepared=Initialize-ProGoShortcuts', f.index('foreach($scenario in $cases)')))
        self.assertIn("$prepared.Legacy.Disposition -ceq 'preserved' -and -not $prepared.Legacy.BeforeMatched", f)
        self.assertIn('refusal repeat and disposal preserve valid custom shortcut exact bytes and native identity', f)
        self.assertIn("GetMethod('LegacyOrigin',[Reflection.BindingFlags]'NonPublic,Static')", f)
        self.assertIn('InvokeChecked($method,$null,[object[]]@($stream.SafeFileHandle))', f)
        self.assertNotIn('.Invoke(', f)
    def test_core_incident_observer_keeps_deadline_and_real_body(self):
        fixture=(ROOT / 'tests/RepairPayloadTests.ps1').read_text()
        probe=(ROOT / 'tests/RepairPhaseProbe.ps1').read_text()
        self.assertIn("if (-not $child.WaitForExit(20000)) { Write-FixtureRepairProbe 'committed-deadline'; throw 'Post-commit repair cleanup exceeded its bounded image-check budget' }",fixture)
        self.assertIn("Write-FixtureRepairProbe 'committed-barrier-held'",fixture)
        self.assertIn("'committed-repair.phases'",probe)
        self.assertIn('& $RepairScript -NoLaunch -PayloadDirectory $Payload',probe)
        self.assertIn('[IO.FileMode]::CreateNew',probe)
        self.assertNotIn('WriteAllText',probe)
if __name__ == '__main__': unittest.main()
