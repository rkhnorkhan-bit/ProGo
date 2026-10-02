"""Destructive to the disposable CI runner only. Never run on a user's VPS."""
import base64
import importlib.util
import json
import os
import pathlib
import socket
import subprocess
import tempfile
import time

REPO = pathlib.Path(__file__).resolve().parents[1]


def check(condition, message):
    if not condition:
        raise AssertionError(message)
    print('PASS: ' + message, flush=True)


def main():
    if os.environ.get('GITHUB_ACTIONS') != 'true' or os.geteuid() != 0:
        raise RuntimeError('This test is restricted to a disposable GitHub Actions runner')
    os.umask(0o077)
    # The owner normally invokes provisioning through an already-running SSH
    # server. GitHub's image ships OpenSSH but leaves its service stopped.
    subprocess.run(['systemctl', 'start', 'ssh.service'], check=True)
    spec = importlib.util.spec_from_file_location('setup', REPO / 'server/home_vpn_setup.py')
    setup = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(setup)
    data = setup.prepare()
    first = setup.issue(data, 'Test owner', 'vpn.example.org', 22222)
    second = setup.issue(data, 'Test friend', 'vpn.example.org', 22222)
    decode = lambda text: json.loads(base64.urlsafe_b64decode(text[7:] + '=' * (-len(text[7:]) % 4)))
    a, b = decode(first), decode(second)
    check(a['User'] != b['User'] and a['Password'] != b['Password'], 'invitations have independent credentials')
    check(a['HostKey'].startswith('ssh-ed25519 '), 'SSH host key is pinned in token')
    conns = setup.run('swanctl', '--list-conns', capture=True)
    check('progo-home' in conns and 'EAP_MSCHAPV2' in conns, 'strongSwan loads real connection and EAP authentication')
    check(setup.run('sshd', '-t').returncode == 0, 'OpenSSH accepts managed configuration')
    with tempfile.TemporaryDirectory() as temp:
        root = pathlib.Path(temp)
        config = root / 'sshd.conf'
        config.write_text('Include /etc/ssh/sshd_config.d/70-progo-home.conf\nPort 22222\nListenAddress 127.0.0.1\nHostKey /etc/ssh/ssh_host_ed25519_key\nUsePAM no\nPidFile ' + str(root / 'sshd.pid') + '\n')
        sshd = subprocess.Popen(['/usr/sbin/sshd', '-D', '-e', '-f', str(config)], stderr=subprocess.DEVNULL)
        processes = []
        try:
            time.sleep(1)
            check(sshd.poll() is None, 'isolated SSH test listener starts')
            def start_tunnel(access, local_port, target_port):
                key = root / access['User']
                key.write_text(access['PrivateKey']); key.chmod(0o600)
                known = root / 'known_hosts'
                known.write_text('[127.0.0.1]:22222 ' + access['HostKey'] + '\n')
                process = subprocess.Popen(['ssh', '-N', '-p', '22222', '-i', str(key), '-o', 'IdentitiesOnly=yes',
                    '-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes', '-o', 'UserKnownHostsFile=' + str(known),
                    '-o', 'ExitOnForwardFailure=yes', '-L', '127.0.0.1:%d:127.0.0.1:%d' % (local_port, target_port),
                    access['User'] + '@127.0.0.1'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                processes.append(process); time.sleep(1)
                return process
            def probe(port):
                try:
                    with socket.create_connection(('127.0.0.1', port), timeout=2) as channel:
                        channel.sendall(b'PGIK\x01\x00')
                        return channel.recv(6) == b'PGIK\x01\x00'
                except OSError:
                    return False
            original = start_tunnel(a, 22223, 17878)
            friend = start_tunnel(b, 22224, 17878)
            check(probe(22223) and probe(22224), 'both restricted accounts reach VPN receiver')
            denied = start_tunnel(b, 22225, 22)
            with socket.create_connection(('127.0.0.1', 22225), timeout=2) as channel:
                try:
                    reply = channel.recv(100)
                except OSError:
                    reply = b''
            check(not reply, 'arbitrary port forwarding is denied')
            shell = subprocess.run(['ssh', '-p', '22222', '-i', str(root / b['User']), '-o', 'BatchMode=yes',
                '-o', 'IdentitiesOnly=yes', '-o', 'UserKnownHostsFile=' + str(root / 'known_hosts'),
                b['User'] + '@127.0.0.1', 'printf PROGO_SHELL_EXECUTED'], capture_output=True, timeout=5)
            check(shell.returncode != 0 and b'PROGO_SHELL_EXECUTED' not in shell.stdout, 'invitation cannot execute a shell command')
            setup.revoke(data, b['InviteId'])
            time.sleep(1)
            check(friend.poll() is not None and not probe(22224), 'revocation closes active friend tunnel')
            check(original.poll() is None and probe(22223), 'owner tunnel survives friend revocation')
            rejected = start_tunnel(b, 22226, 17878)
            check(rejected.poll() is not None, 'revoked token cannot reconnect')
            again = setup.prepare()
            check(again['server_id'] == data['server_id'], 'repeated setup preserves CA and invitation state')
        finally:
            for process in processes:
                if process.poll() is None:
                    process.terminate()
                process.wait(timeout=5)
            sshd.terminate(); sshd.wait(timeout=5)


if __name__ == '__main__':
    main()
