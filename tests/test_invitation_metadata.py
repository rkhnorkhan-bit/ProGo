import ast
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
# Execute the actual pure projection without importing Linux-only provisioning
# dependencies (fcntl) or any server setup entry point on the Windows runner.
source = ROOT / 'server/home_vpn_setup.py'
module = ast.parse(source.read_text(encoding='utf-8'), filename=str(source))
projection = next(node for node in module.body
                  if isinstance(node, ast.FunctionDef) and node.name == 'invitation_summaries')
namespace = {}
exec(compile(ast.Module(body=[projection], type_ignores=[]), str(source), 'exec'), namespace)
summaries = namespace['invitation_summaries']


class InvitationMetadataTests(unittest.TestCase):
    def test_list_exports_only_public_metadata_without_mutating_state(self):
        entry = dict(id='a' * 24, name='Friend', revoked=False,
                     created='2026-01-02T03:04:05+00:00', password='fixture-private', private_key='fixture-key')
        result = summaries({'invites': [entry]})
        self.assertEqual(result, [dict(Id='a' * 24, Name='Friend', Revoked=False, Created=entry['created'])])
        self.assertEqual(entry['password'], 'fixture-private')
        self.assertFalse(entry['revoked'])

    def test_legacy_and_revoked_records_keep_unknown_date_and_status(self):
        result = summaries({'invites': [dict(id='b' * 24, name='Old', revoked=True)]})
        self.assertIsNone(result[0]['Created'])
        self.assertTrue(result[0]['Revoked'])
        self.assertEqual(summaries({'invites': []}), [])


if __name__ == '__main__':
    unittest.main()
