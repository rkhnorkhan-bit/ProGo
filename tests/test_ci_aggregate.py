import copy
import hashlib
import importlib.util
import pathlib
import re
import unittest

SPEC = importlib.util.spec_from_file_location(
    'progo_ci_aggregate', pathlib.Path(__file__).resolve().parents[1] / 'scripts' / 'check_ci_aggregate.py')
GATE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GATE)


PARTITIONS = ('vpn-server-test', 'windows-build', 'installer-tests', 'installer-live-tests', 'installer-recovery-tests')


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
    for name, expected in (('Foundation', EXPECTED_INSTALLER_SUITES[:26]),
                           ('Live', EXPECTED_INSTALLER_SUITES[26:27]),
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
    if "$SuiteCount = if ($Partition -eq 'Foundation') { 26 } elseif ($Partition -eq 'Live') { 1 } elseif ($Partition -eq 'Recovery') { 6 } else { 33 }" not in text:
        raise ValueError('Partition count differs')
    if "[ValidateSet('All', 'Foundation', 'Live', 'Recovery')]" not in text:
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
        self.assertEqual(workflow.count('-Suite Installer -InstallerPartition Live'), 1)
        self.assertIn('needs: [vpn-server-test, windows-build, installer-tests, installer-live-tests, installer-recovery-tests]', workflow)
        self.assertEqual(workflow.count('  installer-recovery-tests:\n'), 1)
        self.assertIn("tests\\InstallerLegacySourceCaptureTests.ps1'", self.text)

    def test_classifier_wired_once_in_core_after_installer_return(self):
        driver = (self.root / 'scripts' / 'Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        call = "& (Join-Path $Root 'tests\\TestSshFixtureThreadFacts.ps1')"
        self.assertEqual(driver.count(call), 1)
        self.assertLess(driver.index("ProGo Installer "), driver.index(call))
        self.assertLess(driver.index(call), driver.index('function Invoke-IsolatedSshFixtureTool'))
        self.assertNotIn('TestSshFixtureThreadFacts.ps1', self.text)
        self.assertIn("{ 26 } elseif ($Partition -eq 'Live') { 1 } elseif ($Partition -eq 'Recovery') { 6 } else { 33 }", self.text)

    def test_missing_suite_refused(self):
        for name in EXPECTED_INSTALLER_SUITES:
            with self.subTest(suite=name), self.assertRaises(ValueError):
                require_partition_layout(self.text.replace("& (Join-Path $Root 'tests\\" + name + "')", '# removed', 1))

    def test_duplicate_suite_refused(self):
        with self.assertRaises(ValueError):
            require_partition_layout(self.text + "\n& (Join-Path $Root 'tests\\InstallerLegacySourceCaptureTests.ps1')\n")

    def test_wrong_partition_selector_refused(self):
        for name in ('Foundation', 'Live', 'Recovery'):
            with self.subTest(partition=name), self.assertRaises(ValueError):
                require_partition_layout(self.text.replace("@('All', '" + name + "')", "@('All', 'unknown')", 1))


# Frozen CI728 fixture bodies: normalized text hashes avoid platform CRLF/BOM differences.
# Partitioning changes scheduling only, not any scenario, assertion, injection, or child deadline.
CI728_FIXTURE_SHA256 = {'UpdatePackageTests.ps1': '2a95e34d382c69e1c0d9ca8bc4045899688273552a7d18410e5677d8cc8789a0',
 'InstallPayloadTests.ps1': 'd70e0d8ea41780de767af483c6ebd3c37a6d3f5c79c7ac9eb3f136f60a6fcd44',
 'InstallerMaintenanceTests.ps1': '8b32c6e359ea785d62dca869f4ea58a8b6188eea9957dd8876d8d3de3d1acb95',
 'InstallerStageTests.ps1': 'a8629c99a6cba5af6606517e0b47b0ef05c6885aec64033b134902b884d78dfc',
 'InstallerBackupTests.ps1': '644dab9dc8aeff5e07dc1b5c742529971682c5811edf915d74b819a86c47b10f',
 'InstallerSelfCheckTests.ps1': '8c48c7da6bc10fcf52061721038e189928af0388cd7e7a4c4c765bbfc7148c2e',
 'InstallerRollbackTests.ps1': '0e30a60dbf6277c93562ac5b07ceaf16ef2d237fb5c395d08bb6b2a381495632',
 'InstallerJournalTests.ps1': '3e2019f5b08582b54c0b3bad9ec9fec81e5ececb3ced25b7c98f416d05a57ed5',
 'InstallerRestartGateTests.ps1': '2e970637f560c8b33f478aa8e664f422c4346201d94047ee35556f172ba1ff14',
 'InstallerOutcomeTests.ps1': '0197bb6b031b80dd8504646b8628be0636eda98f938bbb19405d7e5459d1b9dd',
 'InstallerRollbackOutcomeTests.ps1': 'b013611962408d539b1cc51d65c884d66ea617256b265968fd4cd25c48d4e1bd',
 'InstallerOutcomeReconcileTests.ps1': '33b1eb5f9fbc78c8df303e05ae634a829317932344f9c6b094538528ab8a2feb',
 'InstallerPendingInstalledProofTests.ps1': '763a6ef7be5fa7a767e161d89d8077c71e32db1d3fc36e88917659c15dd2ccb0',
 'InstallerStartupEvidenceTests.ps1': '45146e13e7083b97791bb3caa72077a2bd0aff48ec138fa1caa1b1afa25d5f65',
 'InstallerMenuEvidenceTests.ps1': '61b89e92cae95fce47560f5e69d400d40ceee8c34808237cee0fe381f8a17408',
 'InstallerLegacyEvidenceTests.ps1': 'f781a17f0e537ea5ab546477b1e21f249233fd23f95803688813a9cf0ebb29c7',
 'InstallerAuxiliaryRevalidationTests.ps1': '40c04a79fa0d73bc89e29f2e3cc1487ad603888e3e8196cec8453804e7662627',
 'InstallerPendingPublicationTests.ps1': '59ad506c8f83ac95fd19b88656b141792571e9bfdda79531447ca73d2a9369a7',
 'InstallerStartupRollbackAdmissionTests.ps1': 'a62ae4f7198335fa9ff1272810f1d6fe9edd13bf5f7e46d48a7c6abc77c2f2fb',
 'InstallerOwnedStartupRollbackTests.ps1': '503d5ec823cb6c9b53a00e9b5d6ff235823f52515211d47d62cebd3ac1ebe484',
 'InstallerStartupResultTests.ps1': 'b1253993f9f3041081f8bf9c8748b68899e817748ce866358136c7cc97bdc38d',
 'InstallerStartupMutationAdmissionTests.ps1': 'a51ecaa294382232d2a7b41fad57ed3716dc92084f694a5f4920d0b13860d935',
 'InstallerStartupFailureTests.ps1': '2d7b80922772cc6eb33612475f1055d1e740db3669e5fa74d57a3ed722c3ca82',
 'InstallerMenuPublicationTests.ps1': '777b73a5cda8dd5303fa3114296ddd6b5d2c0a677bcd4f8a48ec7345c881b64f',
 'InstallerMenuRollbackAdmissionTests.ps1': 'ce4254652fef663ed8775fc6b51c85c6c97f1027af76c92b65963eb3d89212e6',
 'InstallerOwnedMenuRollbackTests.ps1': '3f1836c73d90191ef26c8b88988588433a4de70d11bcbeef8265a8754dcfeb89',
 'InstallerMenuMutationAdmissionTests.ps1': '1ed58f4d907df78b855e021eeff14ea199eec925a20f0cef88e3a63c3b731bd1',
 'InstallerMenuResultTests.ps1': '600401187aecf345215ba7f3d5e062d1ca9983b90924611201adecda0c8e21a7',
 'InstallerMenuFailureTests.ps1': 'a53cd5d2c43289712ed8c362917ffc8c7c488abfe68ef2b067966a9f1d761bd1',
 'InstallerLegacyRollbackAdmissionTests.ps1': 'ab871a06152f26e55f3d9ece1171634e8e9a65ec4eac90e31ee60b8f49594ea7',
 'InstallerLegacySourceCaptureTests.ps1': '7e94360e60ea065448cbf2f9941be89924812be63eca33c191f6b0e43780e0f0',
 'InstallerLegacyDurableSourceTests.ps1': 'a5295d391590c559b28a396d01b754061481f6f141098a1d65a14a35c86dc087',
 'InstallerLegacyDurableInputTests.ps1': 'ebfe3b9c63fd22a0ba2e2bc5d18e907cde492115434c3262b0c19a9ffb710e8e',
 'InstallerLegacyOwnedRestoreTests.ps1': '701806174e9d75684bb98a781614e64bdad2f6241e98caa14d77d422c1ea7c42',
 'InstallerLegacyPreparationTests.ps1': 'ea99a4f82ecdafc72dc3e9ba8f1545c6ea823faa73883c24d6a69e963fb61dac',
 'InstallerLegacyStartupLiveTests.ps1': '40ba99e4d4b5e9d3d0d6153bc67e0f7e1c188f51c747e2c85dbdba873d113002'}


def require_fixture_parity(bodies):
    if set(bodies) != set(CI728_FIXTURE_SHA256):
        raise ValueError('Required fixture body set differs')
    for name, expected in CI728_FIXTURE_SHA256.items():
        body = bodies[name]
        if name == 'InstallerMenuMutationAdmissionTests.ps1':
            body = body.removesuffix("& (Join-Path $Root 'tests\\InstallerAuxiliaryScopedOutcomeTests.ps1') -PreservedFlow\n").removesuffix("& (Join-Path $Root 'tests\\InstallerAuxiliaryScopedOutcomeTests.ps1')\n")
        if hashlib.sha256(body.encode()).hexdigest() != expected:
            raise ValueError('Required fixture body differs: ' + name)


def require_workflow_contract(text):
    text = text.split('jobs:\n', 1)[1]
    jobs = re.findall(r'^  ([a-z][a-z0-9-]*):$', text, re.M)
    if len(jobs) != len(set(jobs)) or set(jobs) != set(PARTITIONS) | {'ci-complete'}:
        raise ValueError('Workflow job set differs')
    expected_needs = 'needs: [' + ', '.join(PARTITIONS) + ']'
    if text.count(expected_needs) != 1 or 'if: ${{ always() }}' not in text:
        raise ValueError('Aggregate dependencies or unconditional gate differ')
    for job in PARTITIONS:
        block = re.search(
            r'^  ' + re.escape(job) + r':\n(.*?)(?=^  [a-z][a-z0-9-]*:|\Z)', text, re.M | re.S).group(1)
        deadline = 12 if job == 'vpn-server-test' else 40
        for token in ('timeout-minutes: ' + str(deadline) + '\n', 'tested_sha: ${{ steps.identity.outputs.sha }}',
                      'id: identity', 'git rev-parse HEAD', 'GITHUB_SHA', 'actions/checkout@v4'):
            if token not in block:
                raise ValueError('Required exact SHA/deadline contract differs: ' + job)
    for job, selector in (('installer-tests', 'Foundation'), ('installer-live-tests', 'Live'),
                          ('installer-recovery-tests', 'Recovery')):
        block = re.search(r'^  ' + job + r':\n(.*?)(?=^  [a-z][a-z0-9-]*:|\Z)', text, re.M | re.S).group(1)
        call = '-Suite Installer -InstallerPartition ' + selector
        if text.count(call) != 1 or call not in block:
            raise ValueError('Installer selector coverage differs')
    gate = text.split('  ci-complete:', 1)[1]
    for token in ('timeout-minutes: 5\n', 'PROGO_EXPECTED_SHA: ${{ github.sha }}',
                  'PROGO_NEEDS_JSON: ${{ toJSON(needs) }}', 'python3 scripts/check_ci_aggregate.py'):
        if token not in gate:
            raise ValueError('Strict aggregate invocation differs')


class ExpandedPartitionRegressionTests(unittest.TestCase):
    def setUp(self):
        self.root = pathlib.Path(__file__).resolve().parents[1]
        self.runner = (self.root / 'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig')
        self.workflow = (self.root / '.github/workflows/ci.yml').read_text()

    def test_exact_workflow_and_driver_selectors(self):
        require_workflow_contract(self.workflow)
        driver = (self.root / 'scripts/Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        self.assertIn("[ValidateSet('All', 'Foundation', 'Live', 'Recovery')]", driver)
        self.assertIn("if ($Suite -ne 'Installer' -and $InstallerPartition -ne 'All') { throw", driver)

    def test_fixture_bodies_and_nested_live_order_are_frozen(self):
        bodies = {name: (self.root / 'tests' / name).read_text(encoding='utf-8-sig') for name in CI728_FIXTURE_SHA256}
        require_fixture_parity(bodies)
        host = bodies['InstallerMenuMutationAdmissionTests.ps1']
        nested = re.findall(r"^& \(Join-Path \$Root 'tests\\([^']+)'\)([^\n]*)$", host, re.M)
        self.assertEqual(nested, [('InstallerLegacyOwnedRestoreTests.ps1',''), ('InstallerLegacyPreparationTests.ps1',''),
                                 ('InstallerLegacyStartupLiveTests.ps1',''), ('InstallerLegacyStartupLiveTests.ps1',' -MenuFlow'), ('InstallerAuxiliaryScopedOutcomeTests.ps1',''), ('InstallerAuxiliaryScopedOutcomeTests.ps1',' -PreservedFlow')])

    def test_changed_or_missing_fixture_refused(self):
        bodies = {name: (self.root / 'tests' / name).read_text(encoding='utf-8-sig') for name in CI728_FIXTURE_SHA256}
        for name in bodies:
            with self.subTest(fixture=name), self.assertRaises(ValueError):
                require_fixture_parity({**bodies, name: bodies[name] + '# altered'})
        del bodies['InstallerLegacyStartupLiveTests.ps1']
        with self.assertRaises(ValueError):
            require_fixture_parity(bodies)

    def test_missing_live_job_or_need_refused(self):
        for text in (self.workflow.replace('installer-live-tests, ', '', 1),
                     self.workflow.replace('  installer-live-tests:', '  unapproved-live-tests:', 1)):
            with self.assertRaises(ValueError):
                require_workflow_contract(text)

    def test_live_selector_sha_or_deadline_changes_refused(self):
        for before, after in (('-InstallerPartition Live', '-InstallerPartition Foundation'),
                              ('timeout-minutes: 40', 'timeout-minutes: 41'),
                              ('tested_sha: ${{ steps.identity.outputs.sha }}', 'tested_sha: wrong')):
            a = self.workflow.index('  installer-live-tests:')
            modified = self.workflow[:a] + self.workflow[a:].replace(before, after, 1)
            with self.subTest(change=before), self.assertRaises(ValueError):
                require_workflow_contract(modified)

    def test_missing_duplicate_misassigned_boundary_refused(self):
        for text in (self.runner.replace('# PARTITION Live END', '# PARTITION Live WRONG', 1),
                     self.runner + '\n# PARTITION Live BEGIN: duplicated\n',
                     self.runner.replace("@('All', 'Live')", "@('All', 'Foundation')", 1),
                     self.runner.replace("{ 26 } elseif ($Partition -eq 'Live') { 1 }", "{ 27 } elseif ($Partition -eq 'Live') { 0 }", 1)):
            with self.assertRaises(ValueError):
                require_partition_layout(text)

    def test_new_live_non_success_and_wrong_sha_even_if_old_four_pass(self):
        sha = 'a' * 40
        old_four = {name: {'result':'success','outputs':{'tested_sha':sha}} for name in PARTITIONS if name != 'installer-live-tests'}
        for state in ('failure','cancelled','skipped','in_progress',None):
            with self.subTest(state=state), self.assertRaises(ValueError):
                GATE.validate({**old_four, 'installer-live-tests':{'result':state,'outputs':{'tested_sha':sha}}}, sha)
        with self.assertRaises(ValueError):
            GATE.validate(old_four, sha)
        with self.assertRaises(ValueError):
            GATE.validate({**old_four, 'installer-live-tests':{'result':'success','outputs':{'tested_sha':'b'*40}}}, sha)


if __name__ == '__main__':
    unittest.main()
