#!/usr/bin/env python3
"""Root-only installation/synchronization for the optional HTTPS QR service."""
import base64
import hashlib
import json
import os
import pathlib
import pwd
import re
import shutil
import socket
import subprocess
import sys
import time

ROOT = pathlib.Path('/var/lib/progo-home')
CONFIG = pathlib.Path('/etc/progo-profile-share')
CODE = pathlib.Path('/opt/progo-profile-share')
MARKER = '# Managed by ProGo profile sharing\n'
USER = 'progo-share'


def run(*args, capture=False):
    result = subprocess.run(args, check=True, text=True, stdout=subprocess.PIPE if capture else None)
    return result.stdout if capture else None


def write(path, text, mode=0o644, group=None):
    path = pathlib.Path(path)
    temp = path.with_name(path.name + '.new')
    with open(temp, 'w', encoding='utf-8') as stream:
        stream.write(text)
    os.chmod(temp, mode)
    if group is not None:
        os.chown(temp, 0, group)
    os.replace(temp, path)


def sync(data=None):
    if not CONFIG.exists():
        return
    data = data or json.loads((ROOT / 'state.json').read_text())
    if not data.get('share_origin'):
        return
    users = {'pgv' + i['id']: hashlib.sha256(i['password'].encode()).hexdigest()
             for i in data['invites'] if not i['revoked']}
    settings = dict(origin=data['share_origin'], server_id=data['server_id'], identity=data['identity'],
                    ca_name=data['ca_name'], ca=base64.b64encode((ROOT / 'pki/ca.der').read_bytes()).decode(), users=users)
    write(CONFIG / 'publishers.json', json.dumps(settings), 0o640, pwd.getpwnam(USER).pw_gid)


def domain_name(value):
    if (not isinstance(value, str) or len(value) > 253 or '.' not in value
            or not all(re.fullmatch(r'[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?', p) for p in value.split('.'))
            or re.fullmatch(r'[0-9.]+', value)):
        raise ValueError('Specify an ASCII DNS name, for example vpn.example.org')
    return value


def install(data, domain):
    domain = domain_name(domain)
    caddyfile = pathlib.Path('/etc/caddy/Caddyfile')
    site = pathlib.Path('/etc/caddy/progo-profile-share.caddy')
    unit = pathlib.Path('/etc/systemd/system/progo-profile-share.service')
    owned = (CONFIG / 'managed').is_file() and (CONFIG / 'managed').read_text() == MARKER
    first = not owned
    if unit.exists() and not unit.read_text().startswith(MARKER):
        raise RuntimeError('Existing unmanaged progo-profile-share.service')
    if first and (CONFIG.exists() or CODE.exists() or site.exists()
                  or subprocess.run(['getent', 'passwd', USER], stdout=subprocess.DEVNULL).returncode == 0):
        raise RuntimeError('Existing QR installation requires administrator review')
    if first:
        with socket.socket() as check:
            check.bind(('127.0.0.1', 17879))
    addresses = {row[4][0] for row in socket.getaddrinfo(domain, 443, type=socket.SOCK_STREAM)}
    local = {a['local'] for i in json.loads(run('ip', '-j', 'addr', capture=True)) for a in i.get('addr_info', [])}
    if not addresses or not addresses.issubset(local):
        raise RuntimeError('DNS A/AAAA must point directly to this VPS before QR setup; check both records')
    if not shutil.which('caddy'):
        listeners = run('ss', '-H', '-lnt', 'sport = :80 or sport = :443', capture=True)
        if listeners.strip():
            raise RuntimeError('Ports 80/443 already serve another web server. Ask the administrator to configure HTTPS reverse_proxy to 127.0.0.1:17879; existing sites were not changed.')
        os.environ['DEBIAN_FRONTEND'] = 'noninteractive'
        run('apt-get', 'update')
        run('apt-get', 'install', '-y', 'caddy')
    if not caddyfile.is_file():
        raise RuntimeError('Automatic QR setup requires /etc/caddy/Caddyfile')
    service = run('systemctl', 'show', 'caddy.service', '--property=ExecStart', '--value', capture=True)
    if '/etc/caddy/Caddyfile' not in service or '--resume' in service:
        raise RuntimeError('Caddy uses a custom configuration; ask its administrator to add the QR site')
    original = caddyfile.read_text()
    original_stat = caddyfile.stat()
    if first and domain in original:
        raise RuntimeError('Use a dedicated subdomain not already present in Caddyfile')
    old_site = site.read_text() if site.exists() else None
    if old_site is not None and not old_site.startswith(MARKER):
        raise RuntimeError('Existing unmanaged QR Caddy site')
    # Preserve existing sites and validate the combined configuration before reload.
    backup = ROOT / ('Caddyfile.before-progo-qr-' + str(time.time_ns()))
    shutil.copy2(caddyfile, backup)
    os.chmod(backup, 0o600)

    def restore():
        shutil.copyfile(backup, caddyfile)
        os.chmod(caddyfile, original_stat.st_mode & 0o777)
        os.chown(caddyfile, original_stat.st_uid, original_stat.st_gid)
    write(site, MARKER + domain + ' {\n log {\n  output discard\n }\n reverse_proxy 127.0.0.1:17879\n}\n')
    include = 'import /etc/caddy/progo-profile-share.caddy'
    try:
        if not re.search(r'^\s*' + re.escape(include) + r'\s*$', original, re.M):
            write(caddyfile, original.rstrip() + '\n\n' + include + '\n', original_stat.st_mode & 0o777)
            os.chown(caddyfile, original_stat.st_uid, original_stat.st_gid)
        run('caddy', 'validate', '--config', str(caddyfile), '--adapter', 'caddyfile')
    except Exception:
        restore()
        if old_site is None:
            site.unlink(missing_ok=True)
        else:
            write(site, old_site)
        raise
    CONFIG.mkdir(exist_ok=True)
    os.chmod(CONFIG, 0o750)
    write(CONFIG / 'managed', MARKER, 0o600)
    if subprocess.run(['getent', 'passwd', USER], stdout=subprocess.DEVNULL).returncode != 0:
        run('useradd', '--system', '--user-group', '--no-create-home', '--shell', '/usr/sbin/nologin', USER)
    CODE.mkdir(exist_ok=True)
    os.chmod(CODE, 0o755)
    for name in ('profile_share.py', 'profile_share_setup.py', 'qrcodegen.py', 'QR_LICENSE.txt'):
        write(CODE / name, pathlib.Path(__file__).with_name(name).read_text())
    CONFIG.mkdir(exist_ok=True)
    os.chmod(CONFIG, 0o750)
    os.chown(CONFIG, 0, pwd.getpwnam(USER).pw_gid)
    data['share_origin'] = 'https://' + domain
    sync(data)
    write(unit, MARKER + '''[Unit]
Description=ProGo temporary VPN profile delivery
After=network.target
[Service]
User=progo-share
Group=progo-share
ExecStart=/usr/bin/python3 -I /opt/progo-profile-share/profile_share.py
Restart=on-failure
RestartSec=3
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
PrivateDevices=yes
RestrictAddressFamilies=AF_INET
IPAddressDeny=any
IPAddressAllow=localhost
MemoryMax=160M
TasksMax=32
UMask=0077
[Install]
WantedBy=multi-user.target
''')
    # Older owner clients also write state.json. Keep the public credential set
    # synchronized without granting the web process access to private root state.
    write('/etc/systemd/system/progo-profile-sync.service', MARKER + '''[Unit]
Description=Refresh ProGo profile sharing permissions
[Service]
Type=oneshot
ExecStart=/usr/bin/python3 -I /opt/progo-profile-share/profile_share_setup.py sync
''')
    write('/etc/systemd/system/progo-profile-sync.path', MARKER + '''[Unit]
Description=Watch ProGo invitation changes
[Path]
PathChanged=/var/lib/progo-home/state.json
Unit=progo-profile-sync.service
[Install]
WantedBy=multi-user.target
''')
    run('systemctl', 'daemon-reload')
    run('systemctl', 'enable', '--now', 'progo-profile-share.service', 'progo-profile-sync.path')
    run('systemctl', 'restart', 'progo-profile-share.service')
    if shutil.which('ufw') and 'Status: active' in run('ufw', 'status', capture=True):
        run('ufw', 'allow', '80/tcp')
        run('ufw', 'allow', '443/tcp')
    try:
        run('systemctl', 'reload-or-restart', 'caddy.service')
    except Exception:
        restore()
        if old_site is None:
            site.unlink(missing_ok=True)
        else:
            write(site, old_site)
        raise
    return data['share_origin']


if __name__ == '__main__':
    if os.geteuid() != 0 or sys.argv[1:] != ['sync']:
        sys.exit('Use the ProGo owner setup wizard')
    sync()
