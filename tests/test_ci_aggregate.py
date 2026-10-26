import copy
import importlib.util
import pathlib
import re
import unittest

SPEC = importlib.util.spec_from_file_location(
    'progo_ci_aggregate', pathlib.Path(__file__).resolve().parents[1] / 'scripts' / 'check_ci_aggregate.py')
GATE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GATE)


PARTITIONS = ('vpn-server-test', 'windows-build', 'installer-tests', 'installer-recovery-tests')


class AggregateTests(unittest.TestCase):
    def setUp(self):
        self.sha = 'a' * 40
        self.needs = {name: {'result': 'success', 'outputs': {'tested_sha': self.sha}}
                      for name in PARTITIONS}

    def test_exact_required_partitions(self):
        self.assertEqual(GATE.REQUIRED, PARTITIONS)

    def test_all_exact_success(self):
        GATE.validate(self.needs, self.sha)

    def test_any_non_success(self):
        for name in PARTITIONS:
            for state in ('failure', 'cancelled', 'skipped', 'in_progress', None):
                with self.subTest(job=name, state=state):
                    needs = copy.deepcopy(self.needs)
                    needs[name]['result'] = state
                    with self.assertRaises(ValueError):
                        GATE.validate(needs, self.sha)

    def test_missing_partition(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            del needs[name]
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_extra_partition(self):
        self.needs['unapproved'] = self.needs[PARTITIONS[0]]
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_each_wrong_commit(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            needs[name]['outputs']['tested_sha'] = 'b' * 40
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_each_missing_commit(self):
        for name in PARTITIONS:
            needs = copy.deepcopy(self.needs)
            del needs[name]['outputs']['tested_sha']
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_invalid_expected_commit(self):
        for sha in (None, '', 'a' * 39, 'a' * 41, '../invalid', 'A' * 40):
            with self.assertRaises(ValueError):
                GATE.validate(self.needs, sha)

    def test_invalid_needs(self):
        for needs in (None, [], '', 0):
            with self.assertRaises(ValueError):
                GATE.validate(needs, self.sha)

    def test_invalid_job(self):
        self.needs[PARTITIONS[0]] = None
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_missing_outputs(self):
        del self.needs[PARTITIONS[0]]['outputs']
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)

    def test_invalid_outputs(self):
        self.needs[PARTITIONS[0]]['outputs'] = []
        with self.assertRaises(ValueError):
            GATE.validate(self.needs, self.sha)


# Coverage contract: original CI708 suites plus approved durable source prerequisite, once, ordered.
EXPECTED_INSTALLER_SUITES = ('UpdatePackageTests.ps1', 'InstallPayloadTests.ps1', 'InstallerMaintenanceTests.ps1', 'InstallerStageTests.ps1', 'InstallerBackupTests.ps1', 'InstallerSelfCheckTests.ps1', 'InstallerRollbackTests.ps1', 'InstallerJournalTests.ps1', 'InstallerRestartGateTests.ps1', 'InstallerOutcomeTests.ps1', 'InstallerRollbackOutcomeTests.ps1', 'InstallerOutcomeReconcileTests.ps1', 'InstallerPendingInstalledProofTests.ps1', 'InstallerStartupEvidenceTests.ps1', 'InstallerMenuEvidenceTests.ps1', 'InstallerLegacyEvidenceTests.ps1', 'InstallerAuxiliaryRevalidationTests.ps1', 'InstallerPendingPublicationTests.ps1', 'InstallerStartupRollbackAdmissionTests.ps1', 'InstallerOwnedStartupRollbackTests.ps1', 'InstallerStartupResultTests.ps1', 'InstallerStartupMutationAdmissionTests.ps1', 'InstallerStartupFailureTests.ps1', 'InstallerMenuPublicationTests.ps1', 'InstallerMenuRollbackAdmissionTests.ps1', 'InstallerOwnedMenuRollbackTests.ps1', 'InstallerMenuMutationAdmissionTests.ps1', 'InstallerMenuResultTests.ps1', 'InstallerMenuFailureTests.ps1', 'InstallerLegacyRollbackAdmissionTests.ps1', 'InstallerLegacySourceCaptureTests.ps1', 'InstallerLegacyDurableSourceTests.ps1', 'InstallerLegacyDurableInputTests.ps1')

def require_partition_layout(text):
    found = re.findall(r"& \(Join-Path \$Root 'tests\\([^']+)'\)", text)
    if tuple(found) != EXPECTED_INSTALLER_SUITES:
        raise ValueError('Installer coverage/order differs')
    for name, expected in (('Foundation', EXPECTED_INSTALLER_SUITES[:27]),
                           ('Recovery', EXPECTED_INSTALLER_SUITES[27:])):
        start = '# PARTITION ' + name + ' BEGIN:'
        end = '# PARTITION ' + name + ' END'
        if text.count(start) != 1 or text.count(end) != 1:
            raise ValueError('Partition boundary missing or duplicated')
        group = text.split(start, 1)[1].split(end, 1)[0]
        if "if ($Partition -in @('All', '" + name + "')) {" not in group:
            raise ValueError('Partition selector differs')
        if tuple(re.findall(r"& \(Join-Path \$Root 'tests\\([^']+)'\)", group)) != expected:
            raise ValueError('Partition coverage differs')
    if "[ValidateSet('All', 'Foundation', 'Recovery')]" not in text:
        raise ValueError('Unknown partition must be refused')


class PartitionLayoutTests(unittest.TestCase):
    def setUp(self):
        self.root = pathlib.Path(__file__).resolve().parents[1]
        self.text = (self.root / 'scripts' / 'Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig')

    def test_checked_in_partition_contract(self):
        require_partition_layout(self.text)
        workflow = (self.root / '.github' / 'workflows' / 'ci.yml').read_text()
        self.assertEqual(workflow.count('-Suite Installer -InstallerPartition Foundation'), 1)
        self.assertEqual(workflow.count('-Suite Installer -InstallerPartition Recovery'), 1)
        self.assertIn('needs: [vpn-server-test, windows-build, installer-tests, installer-recovery-tests]', workflow)
        self.assertEqual(workflow.count('  installer-recovery-tests:\n'), 1)
        self.assertIn("tests\\InstallerLegacySourceCaptureTests.ps1'", self.text)

    def test_classifier_wired_once_in_core_after_installer_return(self):
        driver = (self.root / 'scripts' / 'Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        call = "& (Join-Path $Root 'tests\\TestSshFixtureThreadFacts.ps1')"
        self.assertEqual(driver.count(call), 1)
        self.assertLess(driver.index("ProGo Installer "), driver.index(call))
        self.assertLess(driver.index(call), driver.index('function Invoke-IsolatedSshFixtureTool'))
        self.assertNotIn('TestSshFixtureThreadFacts.ps1', self.text)
        self.assertIn("{ 27 } elseif ($Partition -eq 'Recovery') { 6 } else { 33 }", self.text)

    def test_missing_suite_refused(self):
        for name in EXPECTED_INSTALLER_SUITES:
            with self.subTest(suite=name), self.assertRaises(ValueError):
                require_partition_layout(self.text.replace("& (Join-Path $Root 'tests\\" + name + "')", '# removed', 1))

    def test_duplicate_suite_refused(self):
        with self.assertRaises(ValueError):
            require_partition_layout(self.text + "\n& (Join-Path $Root 'tests\\InstallerLegacySourceCaptureTests.ps1')\n")

    def test_wrong_partition_selector_refused(self):
        for name in ('Foundation', 'Recovery'):
            with self.subTest(partition=name), self.assertRaises(ValueError):
                require_partition_layout(self.text.replace("@('All', '" + name + "')", "@('All', 'unknown')", 1))


if __name__ == '__main__':
    unittest.main()
