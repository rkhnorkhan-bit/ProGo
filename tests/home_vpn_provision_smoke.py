"""Destructive to the disposable CI runner only. Never run on a user's VPS."""
import base64
import http.client
import importlib.util
import json
import os
import pathlib
import socket
import ssl
import subprocess
import tempfile
import time
import uuid

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
            setup.revoke(data, b['InviteId'])
            replacement = decode(setup.issue(data, 'Test friend', 'vpn.example.org', 22222))
            check(replacement['InviteId'] != b['InviteId'] and replacement['User'] != b['User']
                  and replacement['Password'] != b['Password'], 'reissue after repeated revoke has independent credentials')
            renewed = start_tunnel(replacement, 22227, 17878)
            check(renewed.poll() is None and probe(22227), 'replacement restricted token reaches VPN receiver')
            check(original.poll() is None and probe(22223), 'other invitation remains connected after reissue')
            check(start_tunnel(b, 22228, 17878).poll() is not None, 'old token remains rejected after replacement is issued')
            again = setup.prepare()
            check(again['server_id'] == data['server_id'], 'repeated setup preserves CA and invitation state')
            # Reproduce the older direct-VPN installer blocking every forwarded
            # IPv4 flow not from its own pool. Accepting in another chain is not enough.
            subprocess.run(['nft', '-f', '-'], input='table inet progo_ikev2 { chain forward { type filter hook forward priority -5; policy accept; meta nfproto ipv4 drop; }\n}\n', text=True, check=True)
            setup.apply_network(data)
            setup.apply_network(data)
            legacy = setup.run('nft', 'list', 'chain', 'inet', 'progo_ikev2', 'forward', capture=True)
            check(legacy.count('ProGo home VPN outbound') == 1 and legacy.count('ProGo home VPN return') == 1,
                  'legacy firewall repair is narrow and idempotent')
            check('meta nfproto ipv4 drop' in legacy and 'ipsec in reqid != 0' in legacy and 'ipsec out reqid != 0' in legacy,
                  'legacy drop policy remains; only authenticated home VPN is excepted')
            check(pathlib.Path('/etc/systemd/system/progo-ikev2-network.service.d/80-progo-home.conf').exists(),
                  'legacy firewall reload reapplies home VPN compatibility')
            data = test_operation_recovery(setup, root)
            test_https_sharing(setup, data, a)
        finally:
            for process in processes:
                if process.poll() is None:
                    process.terminate()
                process.wait(timeout=5)
            sshd.terminate(); sshd.wait(timeout=5)


def test_operation_recovery(setup, folder):
    """Lose a real CLI export after issuing access, then recover without reissue."""
    identifier = uuid.uuid4().hex
    command = ['python3', str(REPO / 'server/home_vpn_setup.py')]
    invitation = ['invite', '--host', 'vpn.example.org', '--port', '22222', '--name', 'Recovery fixture',
                  '--request-id', identifier]

    def invoke(arguments, output):
        return subprocess.run(command + arguments + ['--output', str(output)], capture_output=True, text=True, timeout=60)

    before = len(setup.state()['invites'])
    missing = folder / 'missing-parent' / 'result'
    lost = invoke(invitation, missing)
    check(lost.returncode != 0 and not missing.exists(), 'lost CLI export does not claim success to the caller')
    check(len(setup.state()['invites']) == before + 1, 'lost export issued exactly one real invitation')
    status_file = folder / 'operation-status'
    status_call = invoke(['operation-status', '--request-id', identifier], status_file)
    check(status_call.returncode == 0, 'owner can query completed operation after export failure')
    status = json.loads(status_file.read_text())
    check(status['State'] == 'succeeded' and status['ResultAvailable'] and status['RequestId'] == identifier,
          'durable completion survives the lost CLI response')
    check(set(status) == {'Version', 'RequestId', 'Action', 'State', 'Started', 'Finished', 'ResultAvailable'},
          'status response contains no credentials or invitation parameters')
    recovered = folder / 'recovered-result'
    result_call = invoke(['operation-result', '--request-id', identifier], recovered)
    check(result_call.returncode == 0, 'owner recovers the original private token')
    token = recovered.read_text()
    access = json.loads(base64.urlsafe_b64decode(token[7:] + '=' * (-len(token[7:]) % 4)))
    replay_file = folder / 'replayed-result'
    replay = invoke(invitation, replay_file)
    check(replay.returncode == 0 and replay_file.read_text() == token
          and len(setup.state()['invites']) == before + 1,
          'same request ID returns identical access without a second invitation')
    changed = list(invitation)
    changed[changed.index('--name') + 1] = 'Changed fixture'
    rebound = invoke(changed, folder / 'changed-result')
    check(rebound.returncode != 0 and len(setup.state()['invites']) == before + 1,
          'request ID cannot be rebound to changed invitation parameters')
    empty = invoke(['invite', '--host', 'vpn.example.org', '--request-id', ''], folder / 'empty-id-result')
    check(empty.returncode != 0 and len(setup.state()['invites']) == before + 1,
          'empty explicit request ID never falls back to legacy issuance')
    private = [token, access['Password'], access['PrivateKey']]
    check(all(value not in call.stdout + call.stderr for value in private
              for call in (lost, status_call, result_call, replay, rebound, empty)),
          'operation recovery never prints token, password or private key')
    operations = setup.ROOT / 'operations'
    check(operations.stat().st_mode & 0o777 == 0o700 and all(
          path.stat().st_uid == 0 and path.stat().st_mode & 0o777 == 0o600 for path in operations.iterdir()),
          'operation results and locks stay root-owned and private')
    setup.revoke(setup.state(), access['InviteId'])
    after_revoke = folder / 'revoked-status'
    revoked_status = invoke(['operation-status', '--request-id', identifier], after_revoke)
    check(revoked_status.returncode == 0 and not json.loads(after_revoke.read_text())['ResultAvailable'],
          'revoked access is unavailable for recovery')
    revoked = invoke(['operation-result', '--request-id', identifier], folder / 'revoked-result')
    replay_revoked = invoke(invitation, folder / 'replayed-revoked-result')
    check(revoked.returncode != 0 and replay_revoked.returncode != 0
          and len(setup.state()['invites']) == before + 1,
          'recovery cannot recreate or reactivate revoked access')
    return setup.state()


def test_https_sharing(setup, data, owner):
    """Real systemd service, Caddy TLS and least-privilege credentials on CI only."""
    subprocess.run(['apt-get', 'install', '-y', 'caddy'], check=True)
    # Local CA is test-only; production installer uses publicly trusted ACME TLS.
    pathlib.Path('/etc/caddy/Caddyfile').write_text('{\n local_certs\n}\n:8085 {\n respond "existing site"\n}\n')
    with open('/etc/hosts', 'a') as hosts:
        hosts.write('\n127.0.0.1 profiles.progo.test\n')
    helper = setup.share_module()
    origin = helper.install(data, 'profiles.progo.test')
    setup.save_state(data)
    check(origin == 'https://profiles.progo.test', 'QR installer creates dedicated HTTPS origin')
    cert = pathlib.Path('/var/lib/caddy/.local/share/caddy/pki/authorities/local/root.crt')
    for _ in range(40):
        if cert.exists():
            break
        time.sleep(.25)
    context = ssl.create_default_context(cafile=str(cert))

    def request(path, method='GET', content=None, auth=None, cookie=None):
        channel = http.client.HTTPSConnection('profiles.progo.test', context=context, timeout=5)
        headers = {}
        if auth:
            headers['Authorization'] = 'Basic ' + base64.b64encode((auth['User'] + ':' + auth['Password']).encode()).decode()
        if cookie:
            headers['Cookie'] = cookie.split(';')[0]
        if content is not None:
            content = json.dumps(content)
            headers['Content-Type'] = 'application/json'
        channel.request(method, path, content, headers)
        result = channel.getresponse()
        code, fields, body = result.status, dict(result.getheaders()), result.read()
        channel.close()
        return code, fields, body

    for attempt in range(40):
        try:
            response = request('/health')
            if response[0] == 200:
                break
        except OSError:
            pass
        if attempt == 39:
            raise AssertionError('HTTPS QR service did not become healthy')
        time.sleep(.25)
    check(json.loads(response[2])['ServerId'] == data['server_id'], 'real HTTPS reaches the isolated profile service')
    settings = pathlib.Path('/etc/progo-profile-share/publishers.json').read_text()
    check(owner['Password'] not in settings and owner['PrivateKey'] not in settings, 'web service configuration contains hashes and public CA only')
    forbidden = subprocess.run(['runuser', '-u', 'progo-share', '--', 'cat', str(setup.ROOT / 'state.json')], capture_output=True)
    check(forbidden.returncode != 0, 'web process cannot read private VPN state')
    receipt = next((setup.ROOT / 'operations').glob('*.json'))
    forbidden = subprocess.run(['runuser', '-u', 'progo-share', '--', 'cat', str(receipt)], capture_output=True)
    check(forbidden.returncode != 0, 'web process cannot read cached operation credentials')
    code, _, body = request('/api/share', 'POST', {'home': 'home.example.org'}, owner)
    check(code == 200, 'owner creates QR over actual TLS')
    token = json.loads(body)['Url'].split('#')[1]
    code, headers, _ = request('/claim', 'POST', {'token': token})
    check(code == 200, 'phone claims the profile once over TLS')
    cookie = next(v for k, v in headers.items() if k.lower() == 'set-cookie')
    code, _, body = request('/profile.mobileconfig', cookie=cookie)
    check(code == 200 and b'AuthPassword' in body and b'PRIVATE KEY' not in body, 'Safari download is served with VPN-only data')
    new_token = setup.issue(data, 'Share test', 'vpn.example.org', 22222)
    child = json.loads(base64.urlsafe_b64decode(new_token[7:] + '=' * (-len(new_token[7:]) % 4)))
    check(child['ShareUrl'] == origin, 'friend invitation discovers the configured QR service')
    code, _, _ = request('/api/share', 'POST', {'home': 'home.example.org'}, child)
    check(code == 200, 'new invitation has independent publishing access')
    setup.revoke(data, child['InviteId'])
    check(request('/api/share', 'POST', {'home': 'home.example.org'}, child)[0] == 401, 'server revocation invalidates QR publishing')
    check(request('/profile.mobileconfig', cookie=cookie)[0] == 200, 'friend revocation preserves owner download')
    # Repeat installation must preserve the original Caddy site and avoid imports multiplying.
    helper.install(data, 'profiles.progo.test')
    config = pathlib.Path('/etc/caddy/Caddyfile').read_text()
    check('existing site' in config and config.count('import /etc/caddy/progo-profile-share.caddy') == 1,
          'repeated QR setup preserves existing Caddy site and single import')
    check(request('/profile.mobileconfig', cookie=cookie)[0] == 410, 'restart discards short-lived profile sessions')


if __name__ == '__main__':
    main()
