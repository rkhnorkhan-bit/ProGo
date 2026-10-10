"""Private operation recovery tests; no live SSH, accounts, services or networking."""
import argparse
import base64
import concurrent.futures
import importlib.util
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from unittest import mock

if os.name != 'posix':
    raise unittest.SkipTest('Server receipt ownership and flock are tested on Linux')

SOURCE = pathlib.Path(__file__).resolve().parents[1] / 'server/home_vpn_setup.py'
spec = importlib.util.spec_from_file_location('operation_setup', SOURCE)
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)


class OperationRecoveryTest(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.folder = pathlib.Path(self.temporary.name)
        self.root = self.folder / 'managed'
        self.patch = mock.patch.multiple(helper, ROOT=self.root, OWNER_LOCK=self.folder / 'owner.lock')
        self.patch.start()
        self.addCleanup(self.patch.stop)

    def args(self, action='invite', identifier='1' * 32, **values):
        fields = dict(action=action, request_id=identifier, host='vpn.example.org', port=22,
                      name='Test friend', id='a' * 24, domain='profiles.example.org', output=None)
        fields.update(values)
        return argparse.Namespace(**fields)

    def token(self):
        self.root.mkdir(mode=0o700, exist_ok=True)
        helper.write(self.root / 'state.json', json.dumps(dict(invites=[dict(id='a' * 24, revoked=False)])))
        data = dict(InviteId='a' * 24, PrivateKey='fixture private material', Password='fixture password')
        return 'PROGO1.' + base64.urlsafe_b64encode(json.dumps(data).encode()).decode().rstrip('=')

    def record(self, identifier='1' * 32):
        return self.root / 'operations' / (identifier + '.json')

    def test_all_owner_actions_replay_exact_result_without_a_second_mutation(self):
        token = self.token()
        calls = []
        for index, action in enumerate(helper.OWNER_ACTIONS):
            with self.subTest(action=action):
                args = self.args(action, '%032x' % (index + 1))
                value = token if action in ('setup', 'invite') else 'fixture result ' + action

                def perform(request):
                    calls.append(request.action)
                    self.assertEqual(helper.operation_status(request.request_id)['State'], 'running')
                    return value

                self.assertEqual(helper.tracked_operation(args, perform), value)
                # A new export path does not change the identity of the mutation.
                args.output = str(self.folder / 'new-output')
                self.assertEqual(helper.tracked_operation(args, perform), value)
                self.assertEqual(helper.operation_result(args.request_id), value)
        self.assertEqual(calls, list(helper.OWNER_ACTIONS))

    def test_status_omits_credentials_parameters_and_private_results(self):
        token = self.token()
        helper.tracked_operation(self.args(), lambda _: token)
        status = helper.operation_status('1' * 32)
        self.assertEqual(status['State'], 'succeeded')
        self.assertTrue(status['ResultAvailable'])
        text = json.dumps(status)
        for private in (token, 'fixture password', 'fixture private material', 'Test friend', 'vpn.example.org', 'Fingerprint', 'Result"'):
            self.assertNotIn(private, text)
        self.assertEqual(set(status), {'Version', 'RequestId', 'Action', 'State', 'Started', 'Finished', 'ResultAvailable'})

    def test_tracked_share_delayed_before_admission_requires_original_terminal_receipt(self):
        output = self.folder / 'share-result'
        entered, admit, provisioned, finish = (threading.Event() for _ in range(4))
        calls = []
        original_lexists = os.path.lexists

        def preflight(path):
            if os.fspath(path) == str(output):
                entered.set()
                if not admit.wait(5):
                    raise TimeoutError('Share preflight was not released')
            return original_lexists(path)

        def perform(args):
            calls.append(args.domain)
            provisioned.set()
            if not finish.wait(5):
                raise TimeoutError('Share effects were not released')
            return 'https://' + args.domain

        argv = ['home_vpn_setup.py', 'share', '--host', 'vpn.example.org', '--port', '22',
                '--name', 'My iPhone', '--domain', 'old.example.org', '--request-id', '1' * 32,
                '--output', str(output)]
        with mock.patch.object(sys, 'argv', argv), mock.patch.object(helper.os.path, 'lexists', preflight), \
                mock.patch.object(helper, 'perform_owner', perform):
            with concurrent.futures.ThreadPoolExecutor(1) as pool:
                task = pool.submit(helper.main)
                try:
                    self.assertTrue(entered.wait(5))
                    status = helper.operation_status('1' * 32)
                    self.assertEqual(status['State'], 'not-found')
                    self.assertIsNone(status['Action'])
                    self.assertFalse(status['ResultAvailable'])
                    with self.assertRaises(RuntimeError):
                        helper.operation_result('1' * 32)
                    self.assertEqual(calls, [])
                    admit.set()
                    self.assertTrue(provisioned.wait(5))
                    status = helper.operation_status('1' * 32)
                    self.assertEqual((status['Action'], status['State'], status['ResultAvailable']), ('share', 'running', False))
                    with self.assertRaises(RuntimeError):
                        helper.operation_result('1' * 32)
                finally:
                    admit.set()
                    finish.set()
                task.result(timeout=5)
        status = helper.operation_status('1' * 32)
        self.assertEqual((status['Action'], status['State'], status['ResultAvailable']), ('share', 'succeeded', True))
        self.assertIsNotNone(status['Started'])
        self.assertIsNotNone(status['Finished'])
        self.assertEqual(helper.operation_result('1' * 32), 'https://old.example.org')
        self.assertEqual(output.read_text(), 'https://old.example.org')
        with self.assertRaisesRegex(RuntimeError, 'different parameters'):
            helper.tracked_operation(self.args('share', domain='new.example.org', name='My iPhone', id=None), perform)
        self.assertEqual(calls, ['old.example.org'])

    def test_same_id_cannot_be_rebound_to_changed_parameters(self):
        calls = []
        helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'done')
        for name, value in dict(action='list', host='other.example.org', port=2222, name='Other friend',
                                id='b' * 24, domain='other.example.org').items():
            with self.subTest(field=name):
                changed = self.args('repair')
                setattr(changed, name, value)
                with self.assertRaisesRegex(RuntimeError, 'different parameters'):
                    helper.tracked_operation(changed, lambda _: calls.append(2) or 'bad')
        self.assertEqual(calls, [1])

    def test_concurrent_same_request_cannot_duplicate_a_pending_mutation(self):
        entered, release = threading.Event(), threading.Event()
        calls = []

        def perform(_):
            calls.append(1)
            entered.set()
            if not release.wait(5):
                raise TimeoutError('Fixture was not released')
            return 'done'

        with concurrent.futures.ThreadPoolExecutor(1) as pool:
            task = pool.submit(helper.tracked_operation, self.args('repair'), perform)
            try:
                self.assertTrue(entered.wait(5))
                self.assertEqual(helper.operation_status('1' * 32)['State'], 'running')
                with self.assertRaisesRegex(RuntimeError, 'running'):
                    helper.tracked_operation(self.args('repair'), perform)
            finally:
                release.set()
            self.assertEqual(task.result(timeout=5), 'done')
        self.assertEqual(calls, [1])

    def test_distinct_requests_still_serialize_with_legacy_owner_work(self):
        entered, release = threading.Event(), threading.Event()
        calls = []

        def first(_):
            calls.append(1)
            entered.set()
            if not release.wait(5):
                raise TimeoutError('Fixture was not released')
            return 'first'

        with concurrent.futures.ThreadPoolExecutor(2) as pool:
            one = pool.submit(helper.tracked_operation, self.args('repair'), first)
            try:
                self.assertTrue(entered.wait(5))
                two = pool.submit(helper.tracked_operation, self.args('repair', '2' * 32), lambda _: calls.append(2) or 'second')
                limit = time.monotonic() + 5
                while not self.record('2' * 32).exists():
                    if time.monotonic() > limit:
                        self.fail('Second request was not recorded')
                    time.sleep(.01)
                self.assertEqual(helper.operation_status('2' * 32)['State'], 'running')
                self.assertEqual(calls, [1])
            finally:
                release.set()
            self.assertEqual(one.result(timeout=5), 'first')
            self.assertEqual(two.result(timeout=5), 'second')
        self.assertEqual(calls, [1, 2])
        with concurrent.futures.ThreadPoolExecutor(1) as pool:
            with helper.owner_operation_lock():
                pending = pool.submit(helper.tracked_operation, self.args('repair', '3' * 32), lambda _: 'third')
                with self.assertRaises(concurrent.futures.TimeoutError):
                    pending.result(timeout=.1)
            self.assertEqual(pending.result(timeout=5), 'third')

    def test_callback_failure_is_unconfirmed_and_never_reexecuted(self):
        calls = []

        def failed(_):
            calls.append(1)
            raise RuntimeError('fixture private failure detail')

        with self.assertRaises(RuntimeError):
            helper.tracked_operation(self.args('repair'), failed)
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'unconfirmed')
        self.assertNotIn('fixture private failure detail', self.record().read_text())
        with self.assertRaises(RuntimeError):
            helper.tracked_operation(self.args('repair'), failed)
        with self.assertRaises(RuntimeError):
            helper.operation_result('1' * 32)
        self.assertEqual(calls, [1])

    def test_abrupt_process_exit_leaves_an_unconfirmed_record_before_side_effects(self):
        script = '''import argparse, importlib.util, pathlib, os
spec=importlib.util.spec_from_file_location('fixture', SOURCE)
h=importlib.util.module_from_spec(spec); spec.loader.exec_module(h)
h.ROOT=pathlib.Path(ROOT); h.OWNER_LOCK=pathlib.Path(LOCK)
a=argparse.Namespace(action='repair', request_id='1'*32, host='vpn.example.org', port=22,
 name='Test friend', id='a'*24, domain='profiles.example.org', output=None)
def perform(_):
 assert h.operation_status(a.request_id)['State']=='running'
 pathlib.Path(EFFECT).write_text('one mutation')
 os._exit(19)
h.tracked_operation(a, perform)
'''
        prefix = '\n'.join(name + '=' + repr(str(value)) for name, value in dict(
            SOURCE=SOURCE, ROOT=self.root, LOCK=self.folder / 'owner.lock', EFFECT=self.folder / 'effect').items()) + '\n'
        exited = subprocess.run([sys.executable, '-c', prefix + script], capture_output=True, timeout=5)
        self.assertEqual(exited.returncode, 19)
        self.assertEqual((self.folder / 'effect').read_text(), 'one mutation')
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'unconfirmed')
        calls = []
        with self.assertRaises(RuntimeError):
            helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'duplicated')
        self.assertEqual(calls, [])

    def test_initial_record_failure_starts_no_mutation(self):
        calls = []
        with mock.patch.object(helper, 'receipt_write', side_effect=OSError('fixture disk failure')):
            with self.assertRaises(OSError):
                helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'done')
        self.assertEqual(calls, [])
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'not-found')

    def test_directory_sync_failure_starts_no_mutation(self):
        calls = []
        with mock.patch.object(helper, 'sync_receipt_directory', side_effect=OSError('fixture directory sync failure')):
            with self.assertRaises(OSError):
                helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'done')
        self.assertEqual(calls, [])
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'not-found')

    def test_result_commit_failure_cannot_authorize_blind_retry(self):
        calls = []
        real_write = helper.receipt_write

        def disk_failure(directory, identifier, record):
            if record['State'] != 'running':
                raise OSError('fixture disk failure')
            real_write(directory, identifier, record)

        with mock.patch.object(helper, 'receipt_write', side_effect=disk_failure):
            with self.assertRaises(OSError):
                helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'done')
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'unconfirmed')
        with self.assertRaises(RuntimeError):
            helper.tracked_operation(self.args('repair'), lambda _: calls.append(2) or 'bad')
        self.assertEqual(calls, [1])

    def test_revoked_invitation_cannot_be_recovered_or_reactivated(self):
        token = self.token()
        calls = []
        helper.tracked_operation(self.args(), lambda _: calls.append(1) or token)
        helper.write(self.root / 'state.json', json.dumps(dict(invites=[dict(id='a' * 24, revoked=True)])))
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'succeeded')
        self.assertFalse(helper.operation_status('1' * 32)['ResultAvailable'])
        with self.assertRaises(RuntimeError):
            helper.operation_result('1' * 32)
        with self.assertRaises(RuntimeError):
            helper.tracked_operation(self.args(), lambda _: calls.append(2) or token)
        self.assertEqual(calls, [1])

    def test_private_storage_and_missing_status_have_no_public_export(self):
        self.assertEqual(helper.operation_status('1' * 32)['State'], 'not-found')
        self.assertFalse(self.root.exists())
        helper.tracked_operation(self.args('repair'), lambda _: 'private fixture result')
        for folder in (self.root, self.root / 'operations'):
            self.assertEqual(folder.stat().st_mode & 0o777, 0o700)
        for file in (self.root / 'operations').iterdir():
            self.assertEqual(file.stat().st_mode & 0o777, 0o600)
            self.assertEqual(file.stat().st_uid, os.geteuid())
        self.assertEqual(list((self.root / 'operations').glob('.receipt-*')), [])

    def test_invalid_id_and_result_limits_fail_closed(self):
        for identifier in (None, '', '../outside', 'A' * 32, '1' * 33):
            with self.subTest(identifier=identifier):
                with self.assertRaises(ValueError):
                    helper.operation_status(identifier)
        self.assertFalse(self.root.exists())
        for index, result in enumerate((None, '', 'x' * (helper.RESULT_LIMIT + 1), '\u044f' * helper.RESULT_LIMIT)):
            with self.subTest(result=index):
                args = self.args('repair', '%032x' % (index + 1))
                with self.assertRaises(RuntimeError):
                    helper.tracked_operation(args, lambda _: result)
                self.assertEqual(helper.operation_status(args.request_id)['State'], 'unconfirmed')

    def test_symlinks_hardlinks_special_files_and_public_modes_are_refused(self):
        helper.tracked_operation(self.args('repair'), lambda _: 'done')
        outside = self.folder / 'outside'
        outside.write_text('preserved')
        outside.chmod(0o600)
        lock = self.root / 'operations' / ('1' * 32 + '.lock')
        for path in (self.record(), lock):
            original = path.read_bytes()
            for variant in ('symlink', 'hardlink', 'fifo', 'public'):
                with self.subTest(path=path.name, variant=variant):
                    path.unlink()
                    if variant == 'symlink':
                        path.symlink_to(outside)
                    elif variant == 'hardlink':
                        os.link(outside, path)
                    elif variant == 'fifo':
                        os.mkfifo(path, 0o600)
                    else:
                        path.write_bytes(original)
                        path.chmod(0o644)
                    calls = []
                    with self.assertRaises((OSError, RuntimeError)):
                        helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'bad')
                    self.assertEqual(calls, [])
                    self.assertEqual(outside.read_text(), 'preserved')
                    path.unlink()
                    path.write_bytes(original)
                    path.chmod(0o600)
        (self.root / 'operations').chmod(0o755)
        with self.assertRaises(RuntimeError):
            helper.operation_status('1' * 32)

    def test_corrupt_receipt_never_falls_back_to_execution(self):
        helper.tracked_operation(self.args('repair'), lambda _: 'done')
        original = json.loads(self.record().read_text())
        samples = ['{'] + [json.dumps(dict(original, **{name: value})) for name, value in
                           (('Fingerprint', None), ('Version', True), ('State', 'bad'),
                            ('Started', 'fixture secret'), ('Result', None), ('Unexpected', 'extra'))]
        samples.append(json.dumps(dict(original, InviteId=123)))
        for text in samples:
            with self.subTest(sample=len(text)):
                self.record().write_text(text)
                calls = []
                with self.assertRaises((ValueError, RuntimeError)):
                    helper.tracked_operation(self.args('repair'), lambda _: calls.append(1) or 'bad')
                self.assertEqual(calls, [])


if __name__ == '__main__':
    unittest.main()
