"""HTTP-boundary tests: one-time claims, private profiles, revocation, expiry."""
import base64
import concurrent.futures
import hashlib
import http.client
import importlib.util
import json
import pathlib
import plistlib
import tempfile
import threading
import unittest

REPO = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('profile_share', REPO / 'server/profile_share.py')
share = importlib.util.module_from_spec(spec)
spec.loader.exec_module(share)


class ProfileShareTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.path = pathlib.Path(self.temp.name) / 'publishers.json'
        self.user, self.password = 'pgv' + 'a' * 24, 'b' * 48
        self.settings = dict(server_id='c' * 32, identity='c' * 32 + '.vpn.progo.invalid',
                             ca_name='ProGo Home ' + 'c' * 32, ca=base64.b64encode(b'test DER bytes').decode(),
                             origin='https://vpn.example.org', users={self.user: hashlib.sha256(self.password.encode()).hexdigest()})
        self.path.write_text(json.dumps(self.settings))
        self.now = [2000000000]
        self.shares = share.Shares(self.path, lambda: self.now[0])
        self.server = share.Server(('127.0.0.1', 0), self.shares)
        self.thread = threading.Thread(target=self.server.serve_forever, kwargs={'poll_interval': .02}, daemon=True)
        self.thread.start()

    def tearDown(self):
        self.server.shutdown(); self.server.server_close(); self.thread.join()
        self.temp.cleanup()

    def request(self, path, method='GET', data=None, auth=False, cookie=None, headers=None):
        connection = http.client.HTTPConnection('127.0.0.1', self.server.server_port, timeout=5)
        fields = {'Host': 'vpn.example.org', 'X-Forwarded-Proto': 'https'}
        if auth:
            fields['Authorization'] = 'Basic ' + base64.b64encode((self.user + ':' + self.password).encode()).decode()
        if cookie:
            fields['Cookie'] = cookie.split(';')[0]
        if data is not None:
            fields['Content-Type'] = 'application/json'
            data = json.dumps(data)
        fields.update(headers or {})
        connection.request(method, path, data, fields)
        response = connection.getresponse()
        result = response.status, dict(response.getheaders()), response.read()
        connection.close()
        return result

    def create(self):
        status, headers, body = self.request('/api/share', 'POST', {'home': 'home.example.org'}, auth=True)
        self.assertEqual(status, 200)
        self.assertEqual(headers['Cache-Control'], 'no-store')
        value = json.loads(body)
        self.assertEqual(value['Expires'], self.now[0] + 900)
        self.assertRegex(value['Url'], r'^https://vpn\.example\.org/#[A-Za-z0-9_-]{43}$')
        self.assertNotIn(self.password, body.decode())
        return value

    def claim(self, value):
        status, headers, body = self.request('/claim', 'POST', {'token': value['Url'].split('#')[1]})
        self.assertEqual(status, 200)
        self.assertIn('Secure; HttpOnly; SameSite=Strict; Path=/', headers['Set-Cookie'])
        return headers['Set-Cookie']

    def test_complete_iphone_and_android_import_data(self):
        value = self.create()
        cookie = self.claim(value)
        status, headers, data = self.request('/profile.mobileconfig', cookie=cookie)
        self.assertEqual(status, 200)
        self.assertEqual(headers['Content-Type'], 'application/x-apple-aspen-config')
        profile = plistlib.loads(data)
        self.assertEqual(profile['PayloadType'], 'Configuration')
        vpn = profile['PayloadContent'][1]['IKEv2']
        self.assertEqual(vpn['RemoteAddress'], 'home.example.org')
        self.assertEqual(vpn['RemoteIdentifier'], self.settings['identity'])
        self.assertEqual(vpn['AuthName'], self.user)
        self.assertEqual(vpn['AuthPassword'], self.password)
        self.assertEqual(vpn['IKESecurityAssociationParameters']['DiffieHellmanGroup'], 14)
        self.assertEqual(vpn['ChildSecurityAssociationParameters']['DiffieHellmanGroup'], 14)
        self.assertEqual(vpn['EnablePFS'], 0)
        self.assertEqual(vpn['IncludeAllNetworks'], 1)
        status, headers, data = self.request('/download/android', 'POST', cookie=cookie)
        self.assertEqual(status, 200)
        self.assertEqual(headers['Content-Type'], 'application/vnd.strongswan.profile')
        profile = json.loads(data)
        self.assertEqual(profile['type'], 'ikev2-eap')
        self.assertEqual(profile['remote']['id'], self.settings['identity'])
        self.assertEqual(profile['remote']['addr'], 'home.example.org')
        self.assertEqual(profile['remote']['cert'], self.settings['ca'])
        self.assertEqual(profile['local']['shared_secret'], self.password)
        self.assertEqual(profile['local']['eap_id'], self.user)
        self.assertTrue(profile['split-tunneling']['block-ipv6'])
        self.assertNotIn(b'PRIVATE KEY', data)

    def test_prefetch_never_claims_and_concurrent_claim_only_once(self):
        value = self.create()
        self.assertEqual(self.request('/')[0], 200)
        self.assertEqual(self.request('/install')[0], 410)
        token = value['Url'].split('#')[1]
        with concurrent.futures.ThreadPoolExecutor(4) as executor:
            codes = list(executor.map(lambda _: self.request('/claim', 'POST', {'token': token})[0], range(4)))
        self.assertEqual(sorted(codes), [200, 410, 410, 410])

    def test_expiry_invalidates_link_and_downloads(self):
        link = self.create(); cookie = self.claim(link)
        self.now[0] += 901
        self.assertEqual(self.request('/profile.mobileconfig', cookie=cookie)[0], 410)
        self.assertEqual(self.request('/claim', 'POST', {'token': link['Url'].split('#')[1]})[0], 410)

    def test_new_qr_revokes_previous_claim_but_not_other_account(self):
        cookie = self.claim(self.create())
        self.create()
        self.assertEqual(self.request('/install', cookie=cookie)[0], 410)
        other_user, other_password = 'pgv' + 'd' * 24, 'e' * 48
        self.settings['users'][other_user] = hashlib.sha256(other_password.encode()).hexdigest()
        self.path.write_text(json.dumps(self.settings))
        other = self.shares.create(other_user, other_password, 'other.example.org')
        cookie2 = self.claim(other)
        self.assertEqual(self.request('/api/share', 'DELETE', auth=True)[0], 204)
        self.assertEqual(self.request('/install', cookie=cookie2)[0], 200)

    def test_invitation_revocation_cuts_existing_download(self):
        cookie = self.claim(self.create())
        self.settings['users'].clear()
        self.path.write_text(json.dumps(self.settings))
        self.assertEqual(self.request('/profile.mobileconfig', cookie=cookie)[0], 410)
        self.assertEqual(self.request('/api/share', 'POST', {'home': 'home.example.org'}, auth=True)[0], 401)

    def test_auth_host_https_origin_and_download_isolation(self):
        self.assertEqual(self.request('/api/share', 'POST', {'home': 'home.example.org'})[0], 401)
        self.assertEqual(self.request('/health', headers={'X-Forwarded-Proto': 'http'})[0], 403)
        self.assertEqual(self.request('/health', headers={'Host': 'other.example.org'})[0], 403)
        self.assertEqual(self.request('/api/share', 'POST', {'home': 'home.example.org'}, auth=True,
                                      headers={'Origin': 'https://other.example.org'})[0], 403)
        self.assertEqual(self.request('/download/android', 'POST')[0], 410)
        self.assertEqual(self.request('/api/share', 'POST', {'home': 'https://invalid/'}, auth=True)[0], 400)
        self.assertEqual(self.request('/api/share', 'POST', {'home': '127.0.0.1'}, auth=True)[0], 400)

    def test_qr_is_a_valid_decodable_url(self):
        value = self.create()
        matrix = value['Matrix']
        self.assertTrue(21 <= len(matrix) <= 177 and (len(matrix) - 21) % 4 == 0)
        self.assertTrue(all(len(row) == len(matrix) and set(row) <= {'0', '1'} for row in matrix))
        # Independent decoder in the dedicated CI job. Basic tests use stdlib only.
        try:
            import zxingcpp
            import numpy as np
        except ImportError:
            return
        pixels = np.pad(np.array([[0 if c == '1' else 255 for c in row] for row in matrix], dtype=np.uint8), 4, constant_values=255)
        result = zxingcpp.read_barcode(np.repeat(np.repeat(pixels, 4, axis=0), 4, axis=1))
        self.assertIsNotNone(result)
        self.assertEqual(result.text, value['Url'])


if __name__ == '__main__':
    unittest.main()
