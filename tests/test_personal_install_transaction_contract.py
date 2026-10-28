import importlib.util
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('stage_contract', ROOT / 'tests/test_uninstall_stage_contract.py')
STAGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(STAGE)


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


class PersonalTransactionContract(unittest.TestCase):
    def test_production_factory_and_fixture_constructor_boundary(self):
        install = read('scripts/Install-ProGo.ps1')
        uninstall = read('scripts/Uninstall-ProGo.ps1')
        for text, name in ((install, 'Open-ProGoPersonalInstallerTransaction'), (uninstall, 'Open-ProGoPersonalUninstallerTransaction')):
            factory = STAGE.function(text, name)
            self.assertIn("throw 'Personal", factory)
            self.assertNotIn('RunAs', factory)
            self.assertNotIn('ProgramFiles', factory)
        source = read('src/ApplicationShortcuts.cs')
        production = re.sub(r'#if PROGO_PERSONAL_INSTALL_FIXTURE\n.*?\n#endif', '', source, flags=re.S)
        self.assertNotIn('CreateFixture(', production)
        self.assertNotIn('FixtureRegistrationStore', production)
        self.assertIn('private PersonalInstallTransaction(', production)
        self.assertNotIn('public PersonalInstallTransaction(', production)
        self.assertIn('sid != CurrentSid()', production)
        self.assertIn('LegacyOrigin(current.SafeFileHandle) != LegacyOrigin(source.Value.SafeFileHandle)', production)
        self.assertNotIn('Microsoft.Win32.Registry', source)

    def test_registration_is_consumed_after_terminal_and_not_rolled_back_as_precommit(self):
        text = read('scripts/Install-ProGo.ps1')
        body = text.split('$InstallDir = ', 1)[1]
        terminal = body.index('Complete-ProGoInstallerInstalledOutcome $InstallBackup')
        self.assertIn('$PersonalRuntimeCommitted = $true', body[terminal:])
        self.assertLess(terminal, body.rindex('Complete-ProGoPersonalInstallerRegistration $PersonalTransaction'))
        self.assertIn('if ($PersonalRuntimeCommitted)', body)
        self.assertIn('if ($Personal) { $NoStartup = $true }', text)
        registrar = STAGE.function(text, 'Complete-ProGoPersonalInstallerRegistration')
        self.assertLess(registrar.index('Get-ProGoUninstallInventory'), registrar.index('Open-ProGoUninstallStage'))
        self.assertLess(registrar.index('Assert-ProGoUninstallStage'), registrar.index('$Context.PublishRegistration()'))
        self.assertIn('$Context.PayloadRoot', registrar)
        self.assertIn('. $UninstallSource -FunctionsOnly', registrar)
        retry = STAGE.function(text, 'Test-ProGoPersonalCommittedRuntime')
        self.assertIn('Get-ProGoUninstallInventory', retry)
        self.assertIn('$entry.NewHash -cne $Payload.Hashes[$entry.Relative]', retry)

    def test_uninstall_removes_registration_last_and_preserves_existing_guards(self):
        text = read('scripts/Uninstall-ProGo.ps1')
        body = text.split('$Layout = Get-ProGoUninstallLayout', 1)[1]
        self.assertLess(body.index('$PersonalTransaction.AdmitRegistration()'), body.index('Stop-ProGoUninstallApplication'))
        self.assertLess(body.index('$Shortcuts.RemoveRuntimeFile'), body.index('$PersonalTransaction.RemoveRegistration()'))
        self.assertLess(body.index("$answer -ceq 'DELETE'"), body.index('$PersonalTransaction.RemoveRegistration()'))
        self.assertLess(body.index('$PersonalTransaction.RemoveRegistration()'), body.index('Uninstall OK'))
        self.assertIn('if ($FunctionsOnly) { return }', text)
        self.assertIn('Assert-ProGoUninstallStage $UninstallStage\n            $Shortcuts.RemoveRuntimeFile', body)
        source = read('src/ApplicationShortcuts.cs')
        self.assertIn('registration.CompareExchange(ExpectedRegistration(), null)', source)
        self.assertIn('Personal uninstall runtime deletion is incomplete.', source)

    def test_isolated_e2e_is_mandatory_and_reuses_bounded_host(self):
        fixture = read('tests/PersonalInstallTransactionTests.ps1')
        self.assertIn("'tests\\PersonalInstallTransactionTests.ps1'", read('scripts/Test-ProGo.ps1'))
        self.assertIn("'Invoke-StageHost'", fixture)
        self.assertIn('if ($script:StageChildrenSettled)', fixture)
        for mode in ('success', 'uac-cancel', 'uac-denied', 'context-sid', 'context-path', 'context-object', 'context-reparse', 'payload-tamper', 'entry-tamper', 'registry-foreign', 'registration-before', 'registration-after', 'registration-pins', 'runtime-fault', 'uninstall-fault', 'uninstall-foreign'):
            self.assertIn("'" + mode + "'", fixture)
        for assertion in ('specific personal refusal', 'without reinstall', 'registration and retry', 'foreign registration is never removed', 'untrusted payload or entry bytes never execute', 'production installer remains gated', 'production uninstall remains gated', 'original data, legacy executable and foreign startup link preserved'):
            self.assertIn(assertion, fixture)


if __name__ == '__main__':
    unittest.main()
