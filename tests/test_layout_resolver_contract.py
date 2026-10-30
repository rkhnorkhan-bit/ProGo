import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class LayoutContract(unittest.TestCase):
    def test_production_known_folders_and_no_override(self):
        text = (ROOT / 'scripts/Layout-ProGo.ps1').read_text(encoding='utf-8-sig')
        production = text.split('function Resolve-ProGoLayout {', 1)[1]
        self.assertIn('SpecialFolder]::LocalApplicationData', production)
        self.assertIn('SpecialFolder]::ProgramFiles', production)
        self.assertNotIn('$env:', production)
        self.assertNotIn('SeparateIdentityConfirmed', production)
        self.assertIn('Foreign runtime root', text)
        self.assertIn('ReparsePoint', text)
        self.assertIn('GetAssemblyName', text)

    def test_wiring_and_inventory(self):
        for name in ('Start-ProGo.ps1', 'Show-ProGo.ps1'):
            script = (ROOT / 'scripts' / name).read_text(encoding='utf-8-sig')
            self.assertIn('Resolve-ProGoLayout', script)
            self.assertNotIn('$env:LOCALAPPDATA', script)
        for name in ('Build-ProGo.ps1', 'Install-ProGo.ps1', 'Repair-ProGo.ps1', 'Test-ProGo.ps1'):
            self.assertIn('Layout-ProGo.ps1', (ROOT / 'scripts' / name).read_text(encoding='utf-8-sig'))
        self.assertIn('scripts/Layout-ProGo.ps1', (ROOT / 'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig'))
        self.assertIn('LayoutResolverTests.ps1', (ROOT / 'scripts/Test-ProGoInstallerSuite.ps1').read_text(encoding='utf-8-sig'))
        fixture = (ROOT / 'tests/LayoutResolverTests.ps1').read_text(encoding='utf-8-sig')
        self.assertNotIn('::GetFolderPath', fixture)
        self.assertNotIn('$env:LOCALAPPDATA', fixture)
        self.assertIn('Resolve-ProGoLayoutCore', fixture)
        self.assertIn('Layout refuses a reparse point.', fixture)
        self.assertIn('Foreign runtime executable.', fixture)
        self.assertIn('$actual -cne $expected', fixture)

if __name__ == '__main__': unittest.main()
