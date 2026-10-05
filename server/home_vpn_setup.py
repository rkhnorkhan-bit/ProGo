#!/usr/bin/env python3
"""ProGo home VPN provisioning and individually revocable invitations (Ubuntu).

Invoked over an administrator's normal SSH connection. No administrator credential
is put in an invitation. All generated state is private and outside the checkout.
"""
import argparse
import base64
import datetime as dt
import fcntl
import ipaddress
import importlib.util
import json
import os
import pathlib
import re
import secrets
import shutil
import socket
import subprocess
import sys
import tempfile
import uuid

ROOT = pathlib.Path('/var/lib/progo-home')
CONFIG = pathlib.Path('/etc/swanctl/conf.d/progo-home.conf')
SSH_CONFIG = pathlib.Path('/etc/ssh/sshd_config.d/70-progo-home.conf')
MARKER = '# Managed by ProGo home VPN'
GROUP = 'progo-home-relay'


def run(*args, capture=False, check=True):
    result = subprocess.run(args, check=check, text=True,
                            stdout=subprocess.PIPE if capture else None)
    return result.stdout.strip() if capture else result


def write(path, text, mode=0o600):
    path = pathlib.Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + '.new')
    with open(temp, 'w', encoding='utf-8', opener=lambda p, f: os.open(p, f, mode)) as stream:
        stream.write(text)
    os.chmod(temp, mode)
    os.replace(temp, path)


def managed(path, text, mode=0o600):
    path = pathlib.Path(path)
    if path.exists() and not path.read_text().startswith(MARKER + '\n'):
        raise RuntimeError('An unmanaged file occupies ' + str(path))
    write(path, MARKER + '\n' + text, mode)


def host(value):
    if not re.fullmatch(r'[A-Za-z0-9](?:[A-Za-z0-9.-]{0,251}[A-Za-z0-9])?', value):
        raise ValueError('Use an IPv4 address or DNS hostname without a scheme or port')
    return value


def invite_id(value):
    if not re.fullmatch('[0-9a-f]{24}', value):
        raise ValueError('Invalid invitation ID')
    return value


def state():
    return json.loads((ROOT / 'state.json').read_text())


def save_state(value):
    write(ROOT / 'state.json', json.dumps(value))
    if pathlib.Path('/opt/progo-profile-share/profile_share_setup.py').exists():
        share_module(pathlib.Path('/opt/progo-profile-share/profile_share_setup.py')).sync()


def share_module(path=None):
    path = path or pathlib.Path(__file__).with_name('profile_share_setup.py')
    spec = importlib.util.spec_from_file_location('progo_share_setup', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def user_name(identifier):
    return 'pgv' + invite_id(identifier)


def render_config(data):
    active = [item for item in data['invites'] if not item['revoked']]
    secret_text = ''.join('    eap-' + item['id'] + ' {\n'
                          '        id = ' + user_name(item['id']) + '\n'
                          '        secret = "' + item['password'] + '"\n    }\n'
                          for item in active)
    return '''connections {
    progo-home {
        version = 2
        local_addrs = %%any
        remote_addrs = 127.0.0.1
        pools = progo-home4, progo-home6
        proposals = aes256-sha256-modp2048,aes128-sha256-modp2048
        encap = yes
        fragmentation = yes
        mobike = no
        dpd_delay = 30s
        reauth_time = 0s
        local {
            auth = pubkey
            id = %(identity)s
            certs = progo-home-server.pem
        }
        remote {
            auth = eap-mschapv2
            eap_id = %%any
        }
        children {
            internet {
                local_ts = 0.0.0.0/0, ::/0
                remote_ts = dynamic
                esp_proposals = aes256-sha256,aes128-sha256
                dpd_action = clear
            }
        }
    }
}
pools {
    progo-home4 {
        addrs = %(pool4)s
        dns = %(dns)s
    }
    progo-home6 {
        addrs = %(pool6)s
    }
}
secrets {
%(secrets)s}
''' % dict(identity=data['identity'], pool4=data['pool4'], pool6=data['pool6'],
           dns=data['dns'], secrets=secret_text)


def load_config(data):
    managed(CONFIG, render_config(data))
    # load-conns reconciles the full connection set, so never pass just our file.
    run('swanctl', '--load-all', '--noprompt')


def network_rules(data):
    # No flush ruleset: other services' rules stay in place. IPv6 is captured in
    # the VPN and rejected, since the VPS might not have routed public IPv6.
    return '''table ip progo_home4 {
 chain input { type filter hook input priority -5; policy accept;
  ip saddr %(pool4)s drop
 }
 chain forward { type filter hook forward priority -5; policy accept;
  ip saddr %(pool4)s ip daddr { 0.0.0.0/8, 10.0.0.0/8, 100.64.0.0/10, 127.0.0.0/8, 169.254.0.0/16, 172.16.0.0/12, 192.168.0.0/16, 224.0.0.0/3 } reject
  ip saddr %(pool4)s oifname != "%(wan)s" reject
 }
 chain nat { type nat hook postrouting priority srcnat; policy accept;
  ip saddr %(pool4)s oifname "%(wan)s" masquerade
 }
}
table ip6 progo_home6 {
 chain input { type filter hook input priority -5; policy accept;
  ip6 saddr %(pool6)s drop
 }
 chain forward { type filter hook forward priority -5; policy accept;
  ip6 saddr %(pool6)s reject
 }
}
''' % data


def legacy_forward_rules(data, listing):
    """Permit only authenticated home VPN traffic through our older filter.

    An accept in progo_home4 cannot override a drop in another base chain.
    Never remove the old policy or bypass unrelated administrator firewalls.
    """
    rules = [entry['rule'] for entry in listing.get('nftables', []) if 'rule' in entry]
    if not any(any('drop' in expr for expr in rule.get('expr', []))
               and any(expr.get('match', {}).get('left') == {'meta': {'key': 'nfproto'}}
                       and expr['match'].get('right') == 'ipv4' for expr in rule.get('expr', [])) for rule in rules):
        return ''
    labels = ('ProGo home VPN outbound', 'ProGo home VPN return')
    remove = ''.join('delete rule inet progo_ikev2 forward handle %d\n' % rule['handle']
                     for rule in rules if rule.get('comment') in labels and isinstance(rule.get('handle'), int))
    return remove + '''insert rule inet progo_ikev2 forward ip saddr %(pool4)s oifname "%(wan)s" ipsec in reqid != 0 counter accept comment "ProGo home VPN outbound"
insert rule inet progo_ikev2 forward ip daddr %(pool4)s ipsec out reqid != 0 ct state established,related counter accept comment "ProGo home VPN return"
''' % data


def apply_network(data):
    rules = network_rules(data)
    prefix = ''
    for family, name in [('ip', 'progo_home4'), ('ip6', 'progo_home6')]:
        result = subprocess.run(['nft', 'list', 'table', family, name],
                                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if result.returncode == 0:
            prefix += 'delete table ' + family + ' ' + name + '\n'
    legacy = subprocess.run(['nft', '-j', 'list', 'chain', 'inet', 'progo_ikev2', 'forward'],
                            text=True, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    compatibility = legacy_forward_rules(data, json.loads(legacy.stdout)) if legacy.returncode == 0 else ''
    rules_path = ROOT / 'network.nft'
    write(rules_path, prefix + rules + compatibility)
    run('nft', '--check', '-f', str(rules_path))
    run('nft', '-f', str(rules_path))
    if compatibility:
        # Reapply narrow exceptions when the older installer rebuilds its table.
        managed('/etc/systemd/system/progo-ikev2-network.service.d/80-progo-home.conf',
                '[Service]\nExecStartPost=/usr/bin/python3 -I /opt/progo-home/home_vpn_setup.py network\n', 0o644)
    managed('/etc/sysctl.d/80-progo-home.conf', 'net.ipv4.ip_forward=1\n', 0o644)
    run('sysctl', '-w', 'net.ipv4.ip_forward=1')


def prepare():
    if (ROOT / 'state.json').exists():
        data = state()
        finish_setup(data)
        return data
    if not pathlib.Path('/etc/os-release').exists() or '\nID=ubuntu\n' not in '\n' + pathlib.Path('/etc/os-release').read_text():
        raise RuntimeError('Automatic setup currently supports Ubuntu only')
    # Preflight before installing or replacing anything.
    for path in [CONFIG, SSH_CONFIG, pathlib.Path('/etc/swanctl/x509/progo-home-server.pem'),
                 pathlib.Path('/etc/swanctl/x509ca/progo-home-ca.pem'),
                 pathlib.Path('/etc/swanctl/private/progo-home-server.key'),
                 pathlib.Path('/etc/systemd/system/progo-home-network.service'),
                 pathlib.Path('/etc/sysctl.d/80-progo-home.conf')]:
        if path.exists():
            raise RuntimeError('Existing configuration requires review: ' + str(path))
    if run('getent', 'group', GROUP, check=False, capture=True):
        raise RuntimeError('The invitation group already exists without managed state')
    if shutil.which('nft'):
        for family, name in [('ip', 'progo_home4'), ('ip6', 'progo_home6')]:
            found = subprocess.run(['nft', 'list', 'table', family, name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            if found.returncode == 0:
                raise RuntimeError('A firewall table already uses the ProGo name')
    if shutil.which('ipsec') and run('systemctl', 'is-active', '--quiet', 'strongswan-starter', check=False).returncode == 0:
        raise RuntimeError('Existing strongSwan starter installation requires manual migration')
    routes = json.loads(run('ip', '-j', '-4', 'route', capture=True))
    wan = next(row['dev'] for row in routes if row.get('dst') == 'default')
    if not re.fullmatch(r'[A-Za-z0-9_.-]{1,15}', wan):
        raise RuntimeError('Unsupported network interface name')
    nets = [ipaddress.ip_network(row['dst'], strict=False) for row in routes
            if row.get('dst', 'default') != 'default']
    existing = '\n'.join(path.read_text(errors='ignore') for path in pathlib.Path('/etc/swanctl').rglob('*.conf')) if pathlib.Path('/etc/swanctl').exists() else ''
    pool4 = next((str(ipaddress.ip_network('10.%d.240.0/24' % i)) for i in range(240, 199, -1)
                  if not any(ipaddress.ip_network('10.%d.240.0/24' % i).overlaps(net) for net in nets)
                  and ('10.%d.' % i) not in existing), None)
    if pool4 is None:
        raise RuntimeError('No unused VPN address pool found')
    dns = next(address[4][0] for address in socket.getaddrinfo('dns.quad9.net', 53, socket.AF_INET)
               if ipaddress.ip_address(address[4][0]).is_global)
    os.environ['DEBIAN_FRONTEND'] = 'noninteractive'
    run('apt-get', 'update')
    run('apt-get', 'install', '-y', 'charon-systemd', 'strongswan-swanctl',
        'libcharon-extra-plugins', 'libstrongswan-extra-plugins', 'openssl', 'nftables', 'openssh-server')
    if not re.search(r'^\s*include\s+conf\.d/\*\.conf\s*$', pathlib.Path('/etc/swanctl/swanctl.conf').read_text(), re.M):
        raise RuntimeError('swanctl.conf must include conf.d/*.conf before automatic setup')
    ROOT.mkdir(mode=0o700, parents=True, exist_ok=True)
    os.chmod(ROOT, 0o700)
    identifier = uuid.uuid4().hex
    data = dict(version=1, server_id=identifier, identity=identifier + '.vpn.progo.invalid',
                ca_name='ProGo Home ' + identifier, pool4=pool4,
                pool6='fd' + secrets.token_hex(1) + ':' + secrets.token_hex(2) + ':' + secrets.token_hex(2) + '::/120',
                dns=dns, wan=wan, invites=[])
    # Certificates and keys are generated only on this VPS.
    pki = ROOT / 'pki'
    pki.mkdir(mode=0o700)
    run('openssl', 'req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-sha256', '-days', '3650',
        '-subj', '/CN=' + data['ca_name'], '-keyout', str(pki / 'ca.key'), '-out', str(pki / 'ca.pem'),
        '-addext', 'basicConstraints=critical,CA:TRUE,pathlen:0', '-addext', 'keyUsage=critical,keyCertSign,cRLSign')
    run('openssl', 'req', '-new', '-newkey', 'rsa:3072', '-nodes', '-sha256',
        '-subj', '/CN=' + data['identity'], '-keyout', str(pki / 'server.key'), '-out', str(pki / 'server.csr'))
    write(pki / 'server.ext', 'basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\nsubjectAltName=DNS:' + data['identity'] + '\n')
    run('openssl', 'x509', '-req', '-in', str(pki / 'server.csr'), '-CA', str(pki / 'ca.pem'),
        '-CAkey', str(pki / 'ca.key'), '-CAcreateserial', '-out', str(pki / 'server.pem'),
        '-days', '825', '-sha256', '-extfile', str(pki / 'server.ext'))
    for source, target in [('ca.pem', 'x509ca/progo-home-ca.pem'),
                           ('server.pem', 'x509/progo-home-server.pem'), ('server.key', 'private/progo-home-server.key')]:
        write(pathlib.Path('/etc/swanctl') / target, (pki / source).read_text())
    run('openssl', 'x509', '-in', str(pki / 'ca.pem'), '-outform', 'DER', '-out', str(pki / 'ca.der'))
    if run('getent', 'group', GROUP, check=False).returncode != 0:
        run('groupadd', '--system', GROUP)
    ssh_text = '''Match Group progo-home-relay
    AuthenticationMethods publickey
    PubkeyAuthentication yes
    PasswordAuthentication no
    KbdInteractiveAuthentication no
    AllowTcpForwarding local
    PermitOpen 127.0.0.1:17878
    AllowStreamLocalForwarding no
    GatewayPorts no
    X11Forwarding no
    AllowAgentForwarding no
    PermitTTY no
    PermitTunnel no
    ForceCommand /usr/sbin/nologin
Match all
'''
    managed(SSH_CONFIG, ssh_text, 0o644)
    try:
        run('sshd', '-t')
    except Exception:
        SSH_CONFIG.unlink()
        raise
    run('systemctl', 'reload', 'ssh.service')
    save_state(data)
    finish_setup(data)
    return data


def finish_setup(data):
    write('/opt/progo-home/home_vpn_setup.py', pathlib.Path(__file__).read_text(), 0o700)
    managed('/etc/systemd/system/progo-home-network.service', '''[Unit]
Description=ProGo home VPN network rules
After=network-online.target nftables.service progo-ikev2-network.service
Before=strongswan.service
[Service]
Type=oneshot
ExecStart=/usr/bin/python3 -I /opt/progo-home/home_vpn_setup.py network
RemainAfterExit=yes
[Install]
WantedBy=multi-user.target
''', 0o644)
    apply_network(data)
    run('systemctl', 'daemon-reload')
    run('systemctl', 'enable', '--now', 'progo-home-network.service', 'strongswan.service')
    ufw = shutil.which('ufw')
    if ufw and 'Status: active' in run(ufw, 'status', capture=True):
        run(ufw, 'route', 'allow', 'from', data['pool4'], 'to', 'any')
    relay_unit = pathlib.Path('/etc/systemd/system/progo-ikev2-relay.service')
    if not relay_unit.exists():
        run('bash', str(pathlib.Path(__file__).with_name('install-ikev2-relay.sh')), '127.0.0.1')
    run('systemctl', 'is-active', '--quiet', 'progo-ikev2-relay.service')
    load_config(data)


def issue(data, name, server, port):
    if not 1 <= port <= 65535 or len(name) > 80 or any(ord(c) < 32 for c in name):
        raise ValueError('Invalid invitation name or SSH port')
    host(server)
    identifier = secrets.token_hex(12)
    user = user_name(identifier)
    entry = dict(id=identifier, name=name, password=secrets.token_hex(24), revoked=False,
                 created=dt.datetime.now(dt.timezone.utc).isoformat())
    pathlib.Path('/var/lib/progo-home-users').mkdir(mode=0o755, exist_ok=True)
    os.chmod('/var/lib/progo-home-users', 0o755)
    user_home = pathlib.Path('/var/lib/progo-home-users') / user
    run('useradd', '--system', '--create-home', '--home-dir', str(user_home), '--gid', GROUP,
        '--shell', '/usr/sbin/nologin', user)
    # Unlock the account for pubkey/PAM, but password authentication is prohibited
    # by Match Group. The random password is not retained or included in tokens.
    subprocess.run(['chpasswd'], input=user + ':' + secrets.token_hex(48) + '\n', text=True, check=True)
    ssh_dir = user_home / '.ssh'
    ssh_dir.mkdir(mode=0o700)
    with tempfile.TemporaryDirectory(dir=ROOT) as temporary:
        key = pathlib.Path(temporary) / 'access'
        run('ssh-keygen', '-q', '-t', 'ed25519', '-N', '', '-C', 'progo-invitation', '-f', str(key))
        write(ssh_dir / 'authorized_keys', 'restrict,port-forwarding,permitopen="127.0.0.1:17878",command="/usr/sbin/nologin" ' + key.with_suffix('.pub').read_text())
        run('chown', '-R', user + ':' + GROUP, str(user_home))
        effective = run('sshd', '-T', '-C', 'user=' + user + ',addr=127.0.0.1,host=localhost', capture=True)
        for expected in ['allowtcpforwarding local', 'passwordauthentication no',
                         'permitopen 127.0.0.1:17878', 'forcecommand /usr/sbin/nologin']:
            if expected not in effective.splitlines():
                write(ssh_dir / 'authorized_keys', '')
                raise RuntimeError('Existing SSH settings override invitation restrictions')
        data['invites'].append(entry)
        save_state(data)
        load_config(data)
        public = pathlib.Path('/etc/ssh/ssh_host_ed25519_key.pub').read_text().split()
        token = dict(Version=1, ServerId=data['server_id'], InviteId=identifier,
                     Host=server, Port=port, User=user, PrivateKey=key.read_text(),
                     HostKey=' '.join(public[:2]), Ca=base64.b64encode((ROOT / 'pki/ca.der').read_bytes()).decode(),
                     Password=entry['password'], ShareUrl=data.get('share_origin'))
    return 'PROGO1.' + base64.urlsafe_b64encode(json.dumps(token, separators=(',', ':')).encode()).decode().rstrip('=')


def revoke(data, identifier):
    item = next((i for i in data['invites'] if i['id'] == invite_id(identifier)), None)
    if item is None:
        raise ValueError('Invitation not found')
    user = user_name(identifier)
    key = pathlib.Path('/var/lib/progo-home-users') / user / '.ssh/authorized_keys'
    if key.exists():
        write(key, '')
    run('usermod', '--lock', user)
    # Each invitation has its own Unix account. Cut only that account's tunnels.
    run('pkill', '-KILL', '-u', user, check=False)
    item['revoked'] = True
    save_state(data)
    load_config(data)
    # swanctl reconciles credentials and unloads shared secrets absent from the
    # full configuration. Closing this account's SSH sessions cuts active flows;
    # old IKE SAs expire through DPD without disrupting other clients.


def invitation_summaries(data):
    # Public metadata only; credentials and private state never enter the list.
    return [dict(Id=i['id'], Name=i['name'], Revoked=i['revoked'], Created=i.get('created'))
            for i in data['invites']]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['setup', 'invite', 'revoke', 'list', 'network', 'share', 'repair'])
    parser.add_argument('--host')
    parser.add_argument('--port', type=int, default=22)
    parser.add_argument('--name', default='My iPhone')
    parser.add_argument('--id')
    parser.add_argument('--domain')
    parser.add_argument('--output')
    args = parser.parse_args()
    if os.geteuid() != 0:
        parser.error('Run through sudo or a root SSH account')
    os.umask(0o077)
    if args.action == 'network':
        apply_network(state())
        return
    # Serialize owner operations across PCs. The boot-time network action is
    # deliberately separate because setup waits for its systemd unit.
    operation_lock = open('/run/progo-home-setup.lock', 'a')
    fcntl.flock(operation_lock, fcntl.LOCK_EX)
    if not args.output:
        parser.error('--output is required; credentials are never printed to the terminal')
    if os.path.lexists(args.output):
        parser.error('The output file already exists')
    data = prepare() if args.action == 'setup' else state()
    if args.action in ('setup', 'invite'):
        result = issue(data, args.name, host(args.host or ''), args.port)
    elif args.action == 'share':
        result = share_module().install(data, args.domain or '')
        save_state(data)
    elif args.action == 'repair':
        finish_setup(data)
        result = 'VPN network rules refreshed. Reconnect the phone and test internet access.'
    elif args.action == 'revoke':
        revoke(data, args.id or '')
        result = 'Access revoked.'
    else:
        result = json.dumps(invitation_summaries(data))
    # Export exclusively to the caller's new path. An existing/symlink file is
    # never overwritten by a privileged installer.
    with open(args.output, 'x', encoding='utf-8') as stream:
        stream.write(result)
    if 'SUDO_UID' in os.environ:
        os.chown(args.output, int(os.environ['SUDO_UID']), int(os.environ['SUDO_GID']))
    print('ProGo operation completed. Result saved privately.')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, RuntimeError, subprocess.CalledProcessError) as error:
        print('ProGo setup failed: ' + str(error), file=sys.stderr)
        sys.exit(1)
