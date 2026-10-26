"""Static contracts only: these tests do NOT execute PowerShell or Windows getters."""
import hashlib
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class SshThreadFactsStaticTests(unittest.TestCase):
    def setUp(self):
        self.driver = (ROOT / 'scripts' / 'Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        self.harness = (ROOT / 'tests' / 'TestSshFixtureThreadFacts.ps1').read_text(encoding='utf-8-sig')
        self.classifier = self.driver.split('function Get-IsolatedSshFixtureThreadFacts', 1)[1].split(
            '# Synthetic role/redaction, empty, cap', 1)[0]
        self.launcher = self.driver.split('function Invoke-IsolatedSshFixtureTool', 1)[1].split(
            '# Regression vectors use only', 1)[0]

    def test_existing_classifier_is_unchanged(self):
        actual = hashlib.sha256(('function Get-IsolatedSshFixtureThreadFacts' + self.classifier).encode()).hexdigest()
        self.assertEqual(actual, '70984ce4c0ab16cd98982c5241b995ea0b790f0ad3eef5a80733989351f64681')

    def test_existing_launch_stdio_ownership_is_unchanged(self):
        actual = hashlib.sha256(('function Invoke-IsolatedSshFixtureTool' + self.launcher).encode()).hexdigest()
        self.assertEqual(actual, '98f47f442c276941b1f23be843e96c890bc71632e9ba94c42030827c3f5d5fc9')

    def test_all_framework_enum_vectors_are_prepared(self):
        block = self.harness.split('$enumCases = @(', 1)[1].split(')\nAssert-Facts', 1)[0]
        actual = re.findall(r"'([A-Za-z]+)=([A-Za-z]+)'", block)
        expected = {'Executive': 'waitExecutive', 'FreePage': 'waitPage', 'PageIn': 'waitPage',
                    'SystemAllocation': 'waitOther', 'ExecutionDelay': 'waitDelay', 'Suspended': 'waitSuspended',
                    'UserRequest': 'waitUserRequest', 'EventPairHigh': 'waitOther', 'EventPairLow': 'waitOther',
                    'LpcReceive': 'waitOther', 'LpcReply': 'waitOther', 'VirtualMemory': 'waitPage',
                    'PageOut': 'waitOther', 'Unknown': 'waitOther'}
        self.assertEqual(len(actual), 14)
        self.assertEqual(dict(actual), expected)
        self.assertIn('[Enum]::ToObject([Diagnostics.ThreadWaitReason],123456)', self.harness)
        self.assertIn('[Enum]::ToObject([Diagnostics.ThreadState],123456)', self.harness)

    def test_getter_change_cap_redaction_failure_vectors_are_prepared(self):
        for case in ('unknown-enum', 'unknown-state', 'state-getter-error', 'reason-getter-error', 'state-change',
                     'collection-getter-error', 'cap32', 'cap-no-extra-getter', 'empty-redaction',
                     'preserve-original-FAIL', 'non-wait-reason-not-read', 'complete26'):
            with self.subTest(case=case):
                self.assertIn("'" + case + "'", self.harness)
        self.assertIn('StateReads -eq 1', self.harness)
        self.assertIn('ReasonReads -eq 0', self.harness)
        self.assertIn('foreach ($size in @(32,40))', self.harness)
        self.assertIn('PRIVATE_THREAD_UNIT_SENTINEL', self.harness)
        self.assertIn('$checks -eq 26', self.harness)

    def test_harness_extracts_only_classifier_and_has_no_native_actions(self):
        self.assertIn('$definitions.Count -ne 1', self.harness)
        self.assertIn("$node.Name -ceq 'Get-IsolatedSshFixtureThreadFacts'", self.harness)
        self.assertIn('. ([scriptblock]::Create($definitions[0].Extent.Text))', self.harness)
        for forbidden in ('DllImport', 'Start-Process', 'Process.Start', 'Process]::Start', 'AdjustTokenPrivileges',
                          'GetThreadWaitChain', 'ReadProcessMemory', 'SuspendThread', 'Thread.Id', 'ThreadID'):
            with self.subTest(forbidden=forbidden):
                self.assertNotIn(forbidden, self.harness)

    def test_deadline_and_original_failure_are_still_mandatory(self):
        self.assertIn("'exit0-regression' 5000", self.driver)
        wait = self.launcher.index('if (-not $FixtureTool.WaitForExit($BudgetMs))')
        diagnostic = self.launcher.index('Get-IsolatedSshFixtureThreadFacts $FixtureTool')
        kill = self.launcher.index('$FixtureTool.Kill()', diagnostic)
        failure = self.launcher.index("exceeded ' + $BudgetMs + ' ms; native validation NOT_RUN", kill)
        self.assertLess(wait, diagnostic)
        self.assertLess(diagnostic, kill)
        self.assertLess(kill, failure)
        self.assertIn('$observed = $_.Exception.Message -ceq $originalFailure', self.harness)


if __name__ == '__main__':
    unittest.main()
