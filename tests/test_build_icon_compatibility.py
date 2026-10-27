"""Source contracts; actual host compilation/rendering requires Windows."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class BuildIconCompatibility(unittest.TestCase):
    def test_icon_is_rendered_before_removing_outputs(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text()
        call = source.index('New-ProGoIconFile -Path $preflightIcon')
        self.assertLess(source.index('foreach ($inputPath in $requiredInputs)'), call)
        self.assertLess(call, source.index('Remove-Item $Release -Recurse -Force'))
        self.assertLess(call, source.index('Remove-Item $BuildDir -Recurse -Force'))
        self.assertIn('Copy-Item -LiteralPath $preflightIcon -Destination $IconPath', source)
        self.assertIn('Remove-Item -LiteralPath $preflightIcon -Force', source)
    def test_host_specific_drawing_references(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text()
        for token in ("$PSVersionTable.PSEdition -eq 'Core'", 'System.Drawing.Common',
                      '[System.Drawing.Bitmap].Assembly.Location', '[System.Drawing.Color].Assembly.Location',
                      "'ref\\System.Runtime.dll'", '-ReferencedAssemblies System.Drawing -ErrorAction Stop'):
            self.assertIn(token, source)
    def test_no_checkout_skips_git_and_checkout_errors_fail_closed(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text()
        body = source.split("$BuildRevision = 'local'", 1)[1].split('$BuildIdentity =', 1)[0]
        self.assertIn("Test-Path -LiteralPath (Join-Path $gitProbe '.git')", body)
        self.assertIn('if ($hasGitMetadata)', body)
        self.assertGreater(body.index('rev-parse HEAD'), body.index('if ($hasGitMetadata)'))
        self.assertIn("throw 'Git checkout revision could not be verified.'", body)
        self.assertIn("throw 'Git checkout status could not be verified.'", body)
        self.assertIn("$BuildRevision += '.modified'", body)
        self.assertNotIn("$ErrorActionPreference =", body)

    def test_ci_requires_both_fresh_hosts_and_exact_existing_build(self):
        workflow = (ROOT / '.github/workflows/ci.yml').read_text()
        job = workflow.split('  windows-build:', 1)[1].split('  installer-tests:', 1)[0]
        for token in ("Get-Command pwsh -ErrorAction Stop",
                      "& $desktop -NoProfile -File .\\tests\\BuildIconCompatibilityTests.ps1",
                      "& $core.Source -NoProfile -File .\\tests\\BuildIconCompatibilityTests.ps1",
                      "$identity -cne ($version + '+' + $checkout)",
                      "if ($LASTEXITCODE -ne 0)"):
            self.assertIn(token, job)
        self.assertEqual(job.count('BuildIconCompatibilityTests.ps1'), 2)
        self.assertNotIn('continue-on-error:', job)
    def test_fixture_demands_injected_compile_failure(self):
        fixture = (ROOT / 'tests/BuildIconCompatibilityTests.ps1').read_text()
        for token in ('#error PROGO_ICON_COMPILE_SENTINEL',
                      "-cnotmatch 'PROGO_ICON_COMPILE_SENTINEL'",
                      'Compile failure did not preserve existing outputs.',
                      "$fixtureVersion + '+local'", '[System.Drawing.Image]::FromStream',
                      '$previousEnd -ne $iconBytes.Length'):
            self.assertIn(token, fixture)

    def test_windows_fixture_checks_failure_and_seven_frames(self):
        fixture = (ROOT / 'tests/BuildIconCompatibilityTests.ps1').read_text()
        for token in ("'build survives'", "'release survives'", '@(16,24,32,48,64,128,256)',
                      "'ProGo.exe'", '-Configuration Release'):
            self.assertIn(token, fixture)
        self.assertNotIn('ExecutionPolicy', fixture)

if __name__ == '__main__':
    unittest.main()
