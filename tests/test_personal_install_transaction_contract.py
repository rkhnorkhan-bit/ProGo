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

    def test_process_identity_gate_refuses_impersonation_at_construction_and_revalidation(self):
        source = read('src/ApplicationShortcuts.cs')
        gate = source.split('private static void RefusePersonalImpersonation()', 1)[1].split('private static string CurrentSid()', 1)[0]
        self.assertIn('OpenPersonalThreadToken(PersonalThread(), 0x0008u, true', gate)
        self.assertIn('if (present) throw', gate)
        self.assertIn('if (error != 1008) throw', gate)
        self.assertLess(gate.index('Marshal.GetLastWin32Error()'), gate.index('threadToken.Dispose()'))
        identity = source.split('private static string CurrentSid()', 1)[1].split('private PersonalInstallTransaction(', 1)[0]
        self.assertEqual(identity.count('RefusePersonalImpersonation();'), 2)
        self.assertIn('OpenPersonalProcessToken(PersonalProcess(), 0x0008u', identity)
        self.assertIn('using (processToken)', identity)
        self.assertNotIn('WindowsIdentity.GetCurrent()', identity)
        constructor = source.split('private PersonalInstallTransaction(', 1)[1].split('private void PinAncestors', 1)[0]
        self.assertLess(constructor.index('sid != CurrentSid()'), constructor.index('PinAncestors(rootPath)'))
        validate = source.split('public void Validate(string runtimeRoot', 1)[1].split('private string ExpectedRegistration()', 1)[0]
        self.assertIn('sid != CurrentSid()', validate)
        fixture = read('tests/PersonalInstallTransactionTests.ps1')
        for mode in ('context-impersonation', 'context-revalidate'):
            self.assertIn("'" + mode + "'", fixture)
        self.assertIn('$context.AdmitRegistration()', fixture)
        self.assertIn('$impersonation.Dispose()', fixture)

    def test_native_store_requires_transactional_registry_and_fixed_typed_inventory(self):
        source = read('src/ApplicationShortcuts.cs')
        native = source.split('private sealed class NativeRegistrationStore', 1)[1].split('// This factory and file store', 1)[0]
        for api in ('RegOpenKeyTransactedW', 'RegCreateKeyTransactedW', 'RegDeleteKeyTransactedW', 'RegSetValueExW', 'RegDeleteValueW', 'RegEnumValueW', 'CommitTransaction', 'RollbackTransaction', 'NtQueryKey'):
            self.assertIn(api, native)
        for forbidden in ('RegDeleteTree', 'RegSetKeySecurity', 'RegConnectRegistry', 'RegCreateKeyExW', 'RegDeleteKeyW'):
            self.assertNotIn(forbidden, native)
        self.assertIn('SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall', native)
        names = re.search(r'ValueNames = \{ (.*?) \};', native).group(1)
        self.assertEqual(re.findall(r'"([A-Za-z]+)"', names), ['DisplayName', 'DisplayVersion', 'InstallLocation', 'UninstallString', 'NoModify', 'NoRepair', 'ProGoOwnerSid'])
        self.assertIn('type != (index == 4 || index == 5 ? 4u : 1u)', native)
        self.assertIn('bytes[b] != values[index][b]', native)
        self.assertIn('subkeys != 0 || count != ValueNames.Length', native)
        self.assertIn('new byte[16384]', native)
        self.assertIn('0, 0, 5000, "ProGo registration"', native)
        self.assertLess(native.index('if (Snapshot(key) != previous)'), native.index('RegCreateKeyTransactedW(parentKey'))
        self.assertIn('if (disposition != 1) throw', native)
        self.assertIn('if (!committed && !RollbackTransaction(transaction))', native)
        self.assertIn('public void PublicationFailed(string value) { /* No compensating delete', native)

    def test_native_backend_is_consumed_and_fixture_factory_is_compile_time_only(self):
        source = read('src/ApplicationShortcuts.cs')
        production = re.sub(r'#if PROGO_PERSONAL_INSTALL_FIXTURE\n.*?\n#endif', '', source, flags=re.S)
        self.assertNotIn('CreateRegistryFixture(', production)
        self.assertNotIn('ProGo.RegistryFixtures', production)
        self.assertIn('private sealed class NativeRegistrationStore : RegistrationStore', production)
        self.assertIn('NativeRegistrationStore.Fixture(rootPath, caseName, runtimeRoot, payloadVersion, originalSid, fault)', source)
        publish = source.split('public void PublishRegistration()', 1)[1].split('public void RemoveRegistration()', 1)[0]
        self.assertIn('registration.PublicationFailed(expected)', publish)
        self.assertNotIn('registration.CompareExchange(expected, null)', publish)
        self.assertIn("'tests\\NativeRegistrationTransactionTests.ps1'", read('scripts/Test-ProGo.ps1'))
        fixture = read('tests/NativeRegistrationTransactionTests.ps1')
        self.assertIn("Replace(']::CreateFixture(', ']::CreateRegistryFixture(')", fixture)
        self.assertNotIn('RegistryHive]::LocalMachine', fixture)
        self.assertNotIn('DeleteSubKeyTree', fixture)
        self.assertIn('RegistryHive]::CurrentUser', fixture)
        self.assertIn('RegistryView]::Registry64', fixture)

    def test_native_fixture_covers_consumers_partial_failures_and_foreign_snapshots(self):
        fixture = read('tests/NativeRegistrationTransactionTests.ps1')
        for mode in ('success', 'registration-before', 'registry-write', 'registry-commit', 'registration-after', 'registry-delete', 'registry-delete-after', 'parent-missing', 'foreign-existing', 'foreign-type', 'foreign-extra', 'foreign-child', 'foreign-partial', 'foreign-oversize', 'changed-after-admit'):
            self.assertIn("'" + mode + "'", fixture)
        for assertion in ('native partial writes rolled back', 'partial native value deletion rolls back', 'without replaying runtime transaction', 'foreign typed registry snapshot preserved exactly', 'native registration removed last', 'live admission snapshot cannot authorize changed', 'copied owner SID cannot authorize foreign', 'native exact fixed key/value inventory'):
            self.assertIn(assertion, fixture)
        self.assertIn("Write-StageDriver (Join-Path $Scripts 'Install-ProGo.ps1')", fixture)
        self.assertIn("Write-StageDriver $retained '$Layout'", fixture)
        self.assertIn('$context.PublishRegistration()', fixture)

    def test_native_fault_diagnostic_is_bounded_and_precedes_unchanged_assertion(self):
        fixture = read('tests/NativeRegistrationTransactionTests.ps1')
        assertion = "Check-Stage ($installed.Code -ne 0 -and $installed.Error.Contains('Synthetic native registration fault: ' + $mode + '.') -and -not $installed.Output.Contains('Install OK')) 'native publication fault never reports success'"
        self.assertEqual(fixture.count(assertion), 1)
        diagnostic = fixture.split("if ($writeFault) {", 1)[1].split(assertion, 1)[0]
        self.assertEqual(diagnostic.count('NATIVE_REGISTRATION_DIAGNOSTIC'), 1)
        for field in ('Mode = $mode', 'ChildCode = $installed.Code', 'CodeNonZero = ($installed.Code -ne 0)', "ExpectedErrorMatched = $installed.Error.Contains('Synthetic native registration fault: ' + $mode + '.')", "InstallOkAbsent = (-not $installed.Output.Contains('Install OK'))"):
            self.assertIn(field, diagnostic)
        for stream in ('Error', 'Output'):
            self.assertIn('Length = $installed.' + stream + '.Length', diagnostic)
            self.assertIn('Truncated = ($installed.' + stream + '.Length -gt 1024)', diagnostic)
            self.assertIn('$installed.' + stream + '.Substring(0, [Math]::Min(1024, $installed.' + stream + '.Length))', diagnostic)
        self.assertIn('ConvertTo-Json -Depth 4 -Compress', diagnostic)
        self.assertNotRegex(diagnostic, r'(?m)^\s*\$installed(?:\.|\s*=)')
        self.assertNotIn('Invoke-StageHost', diagnostic)

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
