"""Regression tests for concurrent owner/path-watcher permission updates."""
import concurrent.futures
import importlib.util
import json
import pathlib
import tempfile
import threading
import types
import unittest
from unittest import mock

REPO = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('profile_setup', REPO / 'server/profile_share_setup.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)


class ProfileSyncTest(unittest.TestCase):
    def test_concurrent_atomic_writes_never_share_a_temporary_file(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'publishers.json'
            helper.write(path, json.dumps({'value': -1}), 0o640)
            errors, done = [], threading.Event()

            def read():
                while not done.is_set():
                    try:
                        json.loads(path.read_text())
                        if path.stat().st_mode & 0o777 != 0o640:
                            raise AssertionError('Published file has transient wrong permissions')
                    except Exception as error:
                        errors.append(error)

            reader = threading.Thread(target=read)
            reader.start()
            try:
                def write(index):
                    for revision in range(30):
                        helper.write(path, json.dumps({'value': index, 'revision': revision, 'padding': 'x' * 8192}), 0o640)
                with concurrent.futures.ThreadPoolExecutor(6) as pool:
                    list(pool.map(write, range(6)))
            finally:
                done.set()
                reader.join()
            self.assertEqual(errors, [])
            self.assertEqual(list(path.parent.iterdir()), [path])

    def test_delayed_watcher_cannot_restore_revoked_credentials(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            config = root / 'config'
            config.mkdir()
            (root / 'pki').mkdir()
            (root / 'pki/ca.der').write_bytes(b'test CA')
            state = dict(share_origin='https://profiles.example.org', server_id='a' * 32,
                         identity='vpn.example.org', ca_name='Test CA',
                         invites=[dict(id='b' * 24, password='c' * 48, revoked=False)])
            (root / 'state.json').write_text(json.dumps(state))
            entered, release = threading.Event(), threading.Event()
            first = [True]
            real_write = helper.write

            def delayed_write(path, text, mode, group):
                if first[0]:
                    first[0] = False
                    entered.set()
                    if not release.wait(5):
                        raise TimeoutError('Test did not release the first publisher')
                # Ownership is tested by real provisioning; this test also runs unprivileged.
                real_write(path, text, mode)

            with mock.patch.object(helper, 'ROOT', root), mock.patch.object(helper, 'CONFIG', config), \
                    mock.patch.object(helper.pwd, 'getpwnam', return_value=types.SimpleNamespace(pw_gid=0)), \
                    mock.patch.object(helper, 'write', side_effect=delayed_write):
                with concurrent.futures.ThreadPoolExecutor(2) as pool:
                    watcher = pool.submit(helper.sync)
                    self.assertTrue(entered.wait(5))
                    state['invites'][0]['revoked'] = True
                    (root / 'state.json').write_text(json.dumps(state))
                    owner = pool.submit(helper.sync)
                    try:
                        with self.assertRaises(concurrent.futures.TimeoutError):
                            owner.result(timeout=.1)
                    finally:
                        release.set()
                    watcher.result(timeout=5)
                    owner.result(timeout=5)
            self.assertEqual(json.loads((config / 'publishers.json').read_text())['users'], {})


if __name__ == '__main__':
    unittest.main()
