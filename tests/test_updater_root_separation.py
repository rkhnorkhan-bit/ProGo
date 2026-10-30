"""Static guards for updater root selection and fixture registration."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class UpdaterRoots(unittest.TestCase):
    def test_legacy_activation_and_backup_roles(self):
        core = (ROOT / 'scripts/Update-ProGo.Core.ps1').read_text(encoding='utf-8-sig')
        wrapper = (ROOT / 'scripts/Update-ProGo.ps1').read_text(encoding='utf-8-sig')
        for text in (core, wrapper):
            self.assertIn('$layout = Resolve-ProGoLayout', text)
            self.assertIn('$DataRoot = $layout.DataRoot', text)
            self.assertIn('$InstallDir = $layout.RuntimeRoot', text)
            self.assertNotIn('$env:LOCALAPPDATA', text)
        self.assertIn('$BackupsDir = Join-Path $DataRoot "backups"', core)
        self.assertIn('CopyPersonalArchives($DataRoot, $backupDir)', core)
        self.assertIn('Copy-FileIfExists $DataRoot $name $backupDir $false', core)
        self.assertIn('Copy-FileIfExists $InstallDir $name $backupDir $false', core)
        self.assertIn('Join-Path $DataRoot "update.log"', wrapper)
        self.assertIn('RequireProxyCleanupCompleted($DataRoot)', core)
        self.assertIn('synthetic separate-root updater transaction', (ROOT / 'tests/UpdaterRootSeparationTests.ps1').read_text(encoding='utf-8-sig'))

    def test_restore_and_repair_remain_legacy_only(self):
        repair = (ROOT / 'scripts/Repair-ProGo.ps1').read_text(encoding='utf-8-sig')
        restore = (ROOT / 'scripts/Restore-ProGoBackup.ps1').read_text(encoding='utf-8-sig')
        self.assertIn('$layout = Resolve-ProGoRecoveryLayout', repair)
        self.assertIn('$DataRoot = $layout.DataRoot', repair)
        self.assertIn('$InstallDir = $layout.RuntimeRoot', repair)
        self.assertIn('$layout = Resolve-ProGoRecoveryLayout', restore)
        self.assertIn('$DataRoot = $layout.DataRoot', restore)
        self.assertIn('$InstallDir = $layout.RuntimeRoot', restore)
        self.assertIn('$BackupsDir = Join-Path $DataRoot "backups"', repair)
        self.assertIn("$root = Join-Path $DataRoot 'backups'", restore)
        self.assertIn('CopyPersonalArchives($DataRoot, $path)', restore)

    def test_protective_snapshot_completeness_matches_integrity(self):
        integrity = (ROOT / 'src/BackupIntegrity.cs').read_text(encoding='utf-8-sig')
        restore = (ROOT / 'scripts/Restore-ProGoBackup.ps1').read_text(encoding='utf-8-sig')
        required = re.search(r'private static readonly string\[\] Required = \{(.*?)\};', integrity, re.S).group(1)
        preflight = re.search(r'foreach \(\$required in @\((.*?)\)\)', restore, re.S).group(1)
        self.assertEqual(re.findall(r'"([^"\n]+)"', required),
                         [name.replace('\\', '/') for name in re.findall(r"'([^'\n]+)'", preflight)])

    def test_fixture_is_required_in_a_fresh_windows_host(self):
        suite = (ROOT / 'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig')
        self.assertIn("tests\\UpdatePackageTests.ps1", suite)
        self.assertIn("UpdaterRootSeparationTests.ps1", suite)
        self.assertIn("-NoProfile -NonInteractive -File", suite)
        self.assertIn("$LASTEXITCODE -ne 0", suite)
        self.assertNotIn('continue-on-error', suite.lower())


if __name__ == '__main__':
    unittest.main()
