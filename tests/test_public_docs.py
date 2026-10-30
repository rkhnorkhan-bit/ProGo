import importlib.util
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('public_content', ROOT / 'scripts/check_public_content.py')
CHECKER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CHECKER)


class DocumentationVersionTests(unittest.TestCase):
    def test_repository_headings_match_version(self):
        self.assertEqual(CHECKER.version_issues(ROOT), [])

    def test_both_headings_follow_version_change(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            (root / 'VERSION').write_text('1.2.3')
            for name in ('README.md', 'README.ru.md'):
                (root / name).write_text('# ProGo 1.2.3\n')
            self.assertEqual(CHECKER.version_issues(root), [])
            (root / 'VERSION').write_text('1.2.4')
            self.assertEqual({item[0] for item in CHECKER.version_issues(root)}, {'README.md', 'README.ru.md'})
            for name in ('README.md', 'README.ru.md'):
                (root / name).write_text('# ProGo 1.2.4\n')
            self.assertEqual(CHECKER.version_issues(root), [])

    def test_missing_or_invalid_version_and_instruction_fail(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            self.assertEqual(CHECKER.version_issues(root)[0][0], 'VERSION')
            (root / 'VERSION').write_text('bad')
            self.assertEqual(CHECKER.version_issues(root)[0][0], 'VERSION')
            (root / 'VERSION').write_text('1.2.3')
            self.assertEqual(len(CHECKER.version_issues(root)), 2)


if __name__ == '__main__':
    unittest.main()
