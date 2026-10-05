import importlib.util
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('home_setup', ROOT / 'server/home_vpn_setup.py')
SETUP = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SETUP)


class InvitationMetadataTests(unittest.TestCase):
    def test_list_exports_only_public_metadata_without_mutating_state(self):
        entry = dict(id='a' * 24, name='Friend', revoked=False,
                     created='2026-01-02T03:04:05+00:00', password='fixture-private', private_key='fixture-key')
        result = SETUP.invitation_summaries({'invites': [entry]})
        self.assertEqual(result, [dict(Id='a' * 24, Name='Friend', Revoked=False, Created=entry['created'])])
        self.assertEqual(entry['password'], 'fixture-private')
        self.assertFalse(entry['revoked'])

    def test_legacy_and_revoked_records_keep_unknown_date_and_status(self):
        result = SETUP.invitation_summaries({'invites': [dict(id='b' * 24, name='Old', revoked=True)]})
        self.assertIsNone(result[0]['Created'])
        self.assertTrue(result[0]['Revoked'])
        self.assertEqual(SETUP.invitation_summaries({'invites': []}), [])


if __name__ == '__main__':
    unittest.main()
