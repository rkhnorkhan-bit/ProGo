"""Source contracts; actual host compilation/rendering requires Windows."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class BuildIconCompatibility(unittest.TestCase):
    def test_windows_test_entrypoint_preserves_utf8_bom(self):
        # CI743: WinPS 5.1 interpreted non-BOM source markers as ANSI.
        data = (ROOT / 'scripts/Test-ProGo.ps1').read_bytes()
        self.assertTrue(data.startswith(b'\xef\xbb\xbf'), 'WinPS 5.1 test entrypoint requires UTF-8 BOM')
        data.decode('utf-8-sig', errors='strict')

    def test_icon_is_rendered_before_removing_outputs(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text(encoding='utf-8-sig')
        call = source.index('New-ProGoIconFile -Path $preflightIcon')
        self.assertLess(source.index('foreach ($inputPath in $requiredInputs)'), call)
        self.assertLess(call, source.index('Remove-Item $Release -Recurse -Force'))
        self.assertLess(call, source.index('Remove-Item $BuildDir -Recurse -Force'))
        self.assertIn('Copy-Item -LiteralPath $preflightIcon -Destination $IconPath', source)
        self.assertIn('Remove-Item -LiteralPath $preflightIcon -Force', source)
    def test_host_specific_drawing_references(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text(encoding='utf-8-sig')
        for token in ("$PSVersionTable.PSEdition -eq 'Core'", 'System.Drawing.Common',
                      '[System.Drawing.Bitmap].Assembly.Location', '[System.Drawing.Color].Assembly.Location',
                      "'ref\\System.Runtime.dll'", '-ReferencedAssemblies System.Drawing -ErrorAction Stop'):
            self.assertIn(token, source)
    def test_drawing_dependency_closure_and_missing_refusal(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text(encoding='utf-8-sig')
        body = source.split('function Get-ProGoDrawingReferences', 1)[1].split('function New-ProGoIconFile', 1)[0]
        for token in ('GetReferencedAssemblies()', "StartsWith('System.Private.Windows.'", 'GetAssemblyName($path)',
                      'FullName -cne $reference.FullName', 'LoadFrom($candidate)', '$pending.Enqueue($loaded)',
                      'Missing drawing dependency:', '$rootsAllowed.Contains'):
            self.assertIn(token, body)
        self.assertNotIn('System.Private.CoreLib', body)
        fixture = (ROOT / 'tests/BuildIconCompatibilityTests.ps1').read_text(encoding='utf-8-sig')
        for token in ('Drawing dependency closure is incomplete.', 'System.Private.Windows.ProGoMissing',
                      'missing dependency injection did not reach resolver refusal', 'Missing dependency did not preserve outputs.'):
            self.assertIn(token, fixture)

    def test_no_checkout_skips_git_and_checkout_errors_fail_closed(self):
        source = (ROOT / 'scripts/Build-ProGo.ps1').read_text(encoding='utf-8-sig')
        body = source.split("$BuildRevision = 'local'", 1)[1].split('$BuildIdentity =', 1)[0]
        self.assertIn("Test-Path -LiteralPath (Join-Path $gitProbe '.git')", body)
        self.assertIn('if ($hasGitMetadata)', body)
        self.assertGreater(body.index('rev-parse HEAD'), body.index('if ($hasGitMetadata)'))
        self.assertIn("throw 'Git checkout revision could not be verified.'", body)
        self.assertIn("throw 'Git checkout status could not be verified.'", body)
        self.assertIn("$BuildRevision += '.modified'", body)
        self.assertNotIn("$ErrorActionPreference =", body)

    def test_ci_requires_both_fresh_hosts_and_exact_existing_build(self):
        workflow = (ROOT / '.github/workflows/ci.yml').read_text(encoding='utf-8-sig')
        job = workflow.split('  windows-build:', 1)[1].split('  installer-tests:', 1)[0]
        identity = job.split('      - name: Verify clean checkout build identity', 1)[1].split('      - name:', 1)[0]
        desktop = job.split('      - name: Verify icon fixture on Windows PowerShell 5.1', 1)[1].split('      - name:', 1)[0]
        core = job.split('      - name: Verify icon fixture on PowerShell 7', 1)[1].split('      - name:', 1)[0]
        self.assertIn('if: ${{ success() }}', identity)
        self.assertIn("$identity -cne ($version + '+' + $checkout)", identity)
        self.assertIn("'release\\ProGo.exe'", identity)
        for step in (desktop, core):
            self.assertIn('if: ${{ !cancelled() }}', step)
            self.assertIn('if ($LASTEXITCODE -ne 0)', step)
            self.assertNotIn('continue-on-error:', step)
            self.assertEqual(step.count('BuildIconCompatibilityTests.ps1'), 1)
        self.assertIn('& $desktop -NoProfile -File', desktop)
        self.assertIn('-ExpectedHost Desktop51', desktop)
        self.assertIn('Test-Path -LiteralPath $desktop -PathType Leaf', desktop)
        self.assertIn('Get-Command pwsh -ErrorAction Stop', core)
        self.assertIn('& $core.Source -NoProfile -File', core)
        self.assertIn('-ExpectedHost Core7', core)
        self.assertNotIn('continue-on-error:', job)
        self.assertIn('needs: [vpn-server-test, windows-build, installer-tests, installer-live-tests, installer-recovery-tests, installer-runtime-tests, installer-success-tests]', workflow)
        self.assertIn('if: success() && (github.event.repository.private', job)
    def test_fixture_demands_injected_compile_failure(self):
        fixture = (ROOT / 'tests/BuildIconCompatibilityTests.ps1').read_text(encoding='utf-8-sig')
        for token in ('#error PROGO_ICON_COMPILE_SENTINEL',
                      "-cnotmatch 'PROGO_ICON_COMPILE_SENTINEL'",
                      'Compile failure did not preserve existing outputs.',
                      "$fixtureVersion + '+local'", '[System.Drawing.Image]::FromStream',
                      '$previousEnd -ne $iconBytes.Length'):
            self.assertIn(token, fixture)

    def test_windows_fixture_checks_failure_and_seven_frames(self):
        fixture = (ROOT / 'tests/BuildIconCompatibilityTests.ps1').read_text(encoding='utf-8-sig')
        for token in ("'build survives'", "'release survives'", '@(16,24,32,48,64,128,256)',
                      "'ProGo.exe'", '-Configuration Release'):
            self.assertIn(token, fixture)
        self.assertNotIn('ExecutionPolicy', fixture)

if __name__ == '__main__':
    unittest.main()
