"""Ephemeral credentials for Windows parser/profile tests, never published."""
import base64
import json
import os
import pathlib
import shutil
import subprocess
import sys

directory = pathlib.Path(sys.argv[1])
directory.mkdir(parents=True, exist_ok=True)
openssl = shutil.which('openssl')
if os.name == 'nt':
    candidate = pathlib.Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/usr/bin/openssl.exe'
    if candidate.exists():
        openssl = str(candidate)
identifier = 'a' * 32
for command in [
    [openssl, 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-sha256', '-days', '2',
     '-subj', '/CN=ProGo Home ' + identifier, '-keyout', str(directory / 'ca-key'), '-out', str(directory / 'ca-cert'),
     '-addext', 'basicConstraints=critical,CA:TRUE'],
    [openssl, 'x509', '-in', str(directory / 'ca-cert'), '-outform', 'DER', '-out', str(directory / 'ca-der')],
    ['ssh-keygen', '-q', '-t', 'ed25519', '-N', '', '-f', str(directory / 'access')],
]:
    subprocess.run(command, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
public = (directory / 'access.pub').read_text().split()
token = dict(Version=1, ServerId=identifier, InviteId='b' * 24, Host='vpn.example.org', Port=22,
             User='pgv' + 'b' * 24, Password='c' * 48, PrivateKey=(directory / 'access').read_text(),
             HostKey=' '.join(public[:2]), Ca=base64.b64encode((directory / 'ca-der').read_bytes()).decode())
(directory / 'token').write_text('PROGO1.' + base64.urlsafe_b64encode(json.dumps(token).encode()).decode().rstrip('='))
