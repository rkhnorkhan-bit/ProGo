import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('home_profile', Path(__file__).resolve().parents[1] / 'server/make_home_profile.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ProfileTests(unittest.TestCase):
    def test_server_identity_and_certificate_are_preserved(self):
        original = {
            'PayloadUUID': 'old-profile', 'PayloadIdentifier': 'org.example.vpn',
            'PayloadContent': [
                {'PayloadType': 'com.apple.vpn.managed', 'VPNType': 'IKEv2',
                 'PayloadUUID': 'old-vpn', 'IKEv2': {
                     'RemoteAddress': 'vpn.example.org', 'RemoteIdentifier': 'vpn.example.org',
                     'AuthenticationMethod': 'Certificate', 'PayloadCertificateUUID': 'old-cert',
                     'AuthPassword': 'old-cert',
                     'ServerCertificateCommonName': 'vpn.example.org', 'ExtendedAuthEnabled': 1,
                     'IncludeAllNetworks': 1}},
                {'PayloadType': 'com.apple.security.root', 'PayloadUUID': 'old-cert', 'PayloadContent': b'example-only'}]}
        before = copy.deepcopy(original)
        result = module.build_profile(original, 'home.example.org')
        self.assertEqual(original, before)
        config = result['PayloadContent'][0]['IKEv2']
        self.assertEqual(config['RemoteAddress'], 'home.example.org')
        self.assertEqual(config['RemoteIdentifier'], 'vpn.example.org')
        self.assertEqual(config['ServerCertificateCommonName'], 'vpn.example.org')
        self.assertEqual(config['IncludeAllNetworks'], 1)
        self.assertEqual(config['AuthPassword'], 'old-cert')
        self.assertEqual(config['DisableMOBIKE'], 1)
        self.assertEqual(config['PayloadCertificateUUID'], result['PayloadContent'][1]['PayloadUUID'])
        self.assertNotEqual(result['PayloadUUID'], original['PayloadUUID'])
        self.assertEqual(result['PayloadContent'][1]['PayloadContent'], b'example-only')

    def test_invalid_entry_addresses_and_non_ikev2_profiles_are_rejected(self):
        for address in ('127.0.0.1', '192.168.1.1', '::1', 'home.example.org/secret', '-bad.example.org'):
            with self.assertRaises(ValueError):
                module.build_profile({}, address)
        with self.assertRaises(ValueError):
            module.build_profile({}, 'home.example.org')

    def test_legacy_invalid_dh_is_repaired_without_changing_valid_pfs_policy(self):
        for group, pfs, expected_group, expected_pfs in ((0, 0, 14, 0), (19, 1, 19, 1)):
            original = {'PayloadContent': [{
                'PayloadType': 'com.apple.vpn.managed', 'VPNType': 'IKEv2',
                'IKEv2': {'RemoteIdentifier': 'vpn.example.org', 'EnablePFS': pfs,
                          'ChildSecurityAssociationParameters': {'DiffieHellmanGroup': group}}}]}
            result = module.build_profile(original, 'home.example.org')['PayloadContent'][0]['IKEv2']
            self.assertEqual(result['ChildSecurityAssociationParameters']['DiffieHellmanGroup'], expected_group)
            self.assertEqual(result['EnablePFS'], expected_pfs)
            self.assertEqual(original['PayloadContent'][0]['IKEv2']['ChildSecurityAssociationParameters']['DiffieHellmanGroup'], group)


if __name__ == '__main__':
    unittest.main()
