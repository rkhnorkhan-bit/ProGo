import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


def read(name):
    return (ROOT / name).read_text(encoding='utf-8-sig')


def function(text, name):
    declarations = list(re.finditer(
        r'^function[ \t]+' + re.escape(name) + r'(?=[ \t]*(?:\(|\{))', text, re.M))
    if len(declarations) != 1:
        raise ValueError(f'Expected one exact declaration for {name}, got {len(declarations)}')
    start = declarations[0].start()
    return text[start:text.index('\n}', declarations[0].end()) + 2]


class UninstallStageContract(unittest.TestCase):
    def test_exact_declarations_reject_prefix_missing_and_duplicate(self):
        text = 'function Resolve-ProGoLayoutCore {\n}\nfunction Resolve-ProGoLayout {\n    return 7\n}\n'
        self.assertEqual(function(text, 'Resolve-ProGoLayout'), 'function Resolve-ProGoLayout {\n    return 7\n}')
        for malformed in (text.split('function Resolve-ProGoLayout {')[0], text + text):
            with self.assertRaises(ValueError):
                function(malformed, 'Resolve-ProGoLayout')
        uninstall = read('scripts/Uninstall-ProGo.ps1')
        for name in ('Resolve-ProGoLayout', 'Resolve-ProGoRecoveryLayout'):
            self.assertEqual(function(uninstall, name), function(read('scripts/Layout-ProGo.ps1'), name))
        resolver = function(uninstall, 'Get-ProGoUninstallLayout')
        self.assertNotIn('SeparateIdentityConfirmed', resolver)
        self.assertNotIn('Get-ProGoUninstallInventory', resolver)
        self.assertNotIn('SpecialFolder]::ProgramFiles', resolver)
        self.assertIn('Production staged uninstall requires a trusted installation boundary.', resolver)

    def test_admission_uses_exact_existing_authorities(self):
        installer = read('scripts/Install-ProGo.ps1')
        uninstall = read('scripts/Uninstall-ProGo.ps1')
        region = uninstall.split('# BEGIN embedded admission', 1)[1].split('# END embedded admission', 1)[0]
        names = re.findall(r'^function ([\w-]+)', region, re.M)
        layout = read('scripts/Layout-ProGo.ps1')
        self.assertEqual(len(names), 19)
        for name in names:
            source = layout if 'function ' + name in layout else installer
            self.assertEqual(function(uninstall, name), function(source, name), name)
        self.assertIn('Assert-ProGoInstallerNoPendingJournal $RuntimeRoot', function(uninstall, 'Get-ProGoUninstallInventory'))

    def test_before_execution_and_every_runtime_deletion(self):
        text = read('scripts/Uninstall-ProGo.ps1')
        body = text.split('$Layout = Get-ProGoUninstallLayout', 1)[1]
        self.assertLess(body.index('Get-ProGoUninstallInventory'), body.index('Open-ProGoUninstallStage'))
        self.assertLess(body.index('Open-ProGoUninstallStage'), body.index('Import-ProGoUninstallStage'))
        self.assertIn('Assert-ProGoUninstallStage $UninstallStage\n            $Shortcuts.RemoveRuntimeFile', body)
        stage = function(text, 'Open-ProGoUninstallStage')
        self.assertIn('$expected = $entries[0].NewHash', stage)
        self.assertIn('$entries.Count -ne 1', stage)
        self.assertIn('[IO.FileMode]::CreateNew', stage)
        self.assertIn('[IO.FileShare]::Read', stage)
        self.assertNotIn('manifest', re.sub(r'#.*', '', stage).lower())
        self.assertNotIn('Invoke-Expression', text)
        self.assertNotIn('Start-Process', text)
        self.assertIn("[scriptblock]::Create($UninstallStage.Text['scripts/Firewall-ProGo.ps1'])", text)

    def test_directory_and_file_pins_are_released_and_no_cleanup_of_evidence(self):
        text = read('scripts/Uninstall-ProGo.ps1')
        self.assertIn('0, 1, IntPtr.Zero, 3, 0x02200000u', text)
        self.assertIn('(info.Attributes & 0x400) != 0', text)
        self.assertIn('$directoryPins.PinAncestors($RuntimeRoot)', text)
        self.assertIn('$UninstallStage.DirectoryPins.Dispose()', text)
        self.assertIn('Uninstall stage contains a foreign leaf.', text)
        self.assertNotIn('Remove-Item', text)
        self.assertNotIn('Remove-ProGoInstallerOwnedDirectory $UninstallStage', text)
        self.assertIn("if ($answer -ceq 'DELETE')", text)
        self.assertIn("$Shortcuts.RemoveRuntimeFile('ProGo.exe', $Hash)", text)
        self.assertIn("$identity.Name -cne 'ProGo'", text)

    def test_native_suite_is_mandatory_and_keeps_old_suites(self):
        fixture = read('tests/UninstallStageTests.ps1')
        self.assertIn("'tests\\UninstallStageTests.ps1'", read('scripts/Test-ProGo.ps1'))
        self.assertIn("'tests\\InstallerLifecycleTests.ps1'", read('scripts/Test-ProGo.ps1'))
        for scenario in ('retry', 'corrupt', 'replaced-entry', 'foreign', 'reparse', 'pending', 'runtime-conflict', 'pins', 'retry-replaced', 'dependency-code'):
            self.assertIn("'" + scenario + "'", fixture)
        self.assertIn('$process.WaitForExit(60000)', fixture)
        self.assertIn('$process.WaitForExit(5000)', fixture)
        self.assertIn('[Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($out, $err), 5000)', fixture)
        self.assertNotIn('$out.Wait()', fixture)
        self.assertNotIn('$err.Wait()', fixture)
        self.assertIn('if ($script:StageChildrenSettled)', fixture)
        self.assertIn('fixture retained:', fixture)
        self.assertIn('$result.Error.Contains($expectedRefusal)', fixture)
        self.assertIn('foreign stage code never executed', fixture)
        self.assertIn('modified and foreign stage/retry bytes preserved', fixture)
        self.assertIn('junction and its original target preserved', fixture)
        self.assertIn('Write-StageDriver $retryEntry', fixture)
        self.assertIn("$relative -ceq 'scripts/Layout-ProGo.ps1'", fixture)
        self.assertIn("@('Install-ProGo.ps1', 'Layout-ProGo.ps1', 'Shortcuts-ProGo.ps1')", fixture)
        self.assertIn('original recovery evidence unchanged', fixture)
        self.assertIn('refusal preserves every surviving runtime byte', fixture)
        self.assertIn('private bytes and foreign links/siblings retained', fixture)


if __name__ == '__main__':
    unittest.main()
