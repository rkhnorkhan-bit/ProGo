#!/usr/bin/env python3
"""Short-lived phone profile delivery. Bind to loopback behind HTTPS only.

No administrator credentials, SSH keys, private CA keys, mail, analytics, or
third-party QR services. Unclaimed links and claimed sessions live only in RAM.
"""
import base64
import hashlib
import hmac
import html
import http.cookies
import http.server
import importlib.util
import ipaddress
import json
import pathlib
import plistlib
import re
import secrets
import threading
import time
import urllib.parse
import uuid

TTL = 900
MAX_SHARES = 256
TOKEN = re.compile(r'^[A-Za-z0-9_-]{43}$')
COOKIE = '__Host-progo-profile'


def valid_home(value):
    if not isinstance(value, str) or len(value) > 253:
        return False
    try:
        return ipaddress.IPv4Address(value).is_global
    except ValueError:
        return ('.' in value and all(re.fullmatch(r'[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?', part)
                                     for part in value.split('.')))


def profiles(settings, user, password, home):
    """Match the desktop IKEv2 export; never include the invitation SSH key."""
    if not valid_home(home):
        raise ValueError('Use a public home IPv4 or DDNS name')
    ca = base64.b64decode(settings['ca'], validate=True)
    identity, ca_name = settings['identity'], settings['ca_name']

    def payload(kind, name):
        return dict(PayloadType=kind, PayloadVersion=1, PayloadUUID=str(uuid.uuid4()),
                    PayloadIdentifier='app.progo.' + uuid.uuid4().hex, PayloadDisplayName=name)
    certificate = payload('com.apple.security.root', 'ProGo VPN CA')
    certificate['PayloadContent'] = ca
    vpn = payload('com.apple.vpn.managed', 'ProGo — домашний VPN')
    sa = dict(EncryptionAlgorithm='AES-256', IntegrityAlgorithm='SHA2-256', DiffieHellmanGroup=14)
    vpn.update(UserDefinedName='ProGo — домашний VPN', VPNType='IKEv2', IPv4=dict(OverridePrimary=1),
               IKEv2=dict(RemoteAddress=home, RemoteIdentifier=identity, LocalIdentifier=user,
                          AuthenticationMethod='Certificate', ExtendedAuthEnabled=1,
                          AuthName=user, AuthPassword=password, ServerCertificateCommonName=identity,
                          ServerCertificateIssuerCommonName=ca_name, DisableMOBIKE=1,
                          EnablePFS=0, IncludeAllNetworks=1, DeadPeerDetectionRate='Medium',
                          IKESecurityAssociationParameters=sa, ChildSecurityAssociationParameters=sa.copy()))
    apple = payload('Configuration', 'ProGo — домашний VPN')
    apple['PayloadContent'] = [certificate, vpn]
    android = dict(uuid=str(uuid.uuid4()), name='ProGo — домашний VPN', type='ikev2-eap',
                   remote=dict(addr=home, id=identity, cert=settings['ca'], certreq=False),
                   local=dict(eap_id=user, id=user, shared_secret=password))
    android.update({'ike-proposal': 'aes256-sha256-modp2048', 'esp-proposal': 'aes256-sha256',
                    'split-tunneling': {'block-ipv4': True, 'block-ipv6': True}})
    return {'iphone': plistlib.dumps(apple), 'android': json.dumps(android, ensure_ascii=False).encode()}


def qr_rows(url):
    # -I omits the script directory from sys.path. Load our pinned, local MIT
    # dependency explicitly, without accepting paths from requests or cwd.
    spec = importlib.util.spec_from_file_location('progo_qrcodegen', pathlib.Path(__file__).with_name('qrcodegen.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    code = module.QrCode.encode_text(url, module.QrCode.Ecc.MEDIUM)
    return [''.join('1' if code.get_module(x, y) else '0' for x in range(code.get_size()))
            for y in range(code.get_size())]


class Shares:
    def __init__(self, settings_path, clock=time.time):
        self.settings_path = pathlib.Path(settings_path)
        self.clock = clock
        self.lock = threading.Lock()
        self.links, self.sessions = {}, {}

    def settings(self):
        return json.loads(self.settings_path.read_text())

    def clean(self):
        settings = self.settings()
        now = self.clock()
        # Also invalidate claims if the owner revokes or changes this password.
        for table in (self.links, self.sessions):
            for key, value in list(table.items()):
                if value['expires'] <= now or settings['users'].get(value['user']) != value['credential']:
                    del table[key]

    def authenticate(self, header):
        try:
            if not header.startswith('Basic '):
                return None
            raw = base64.b64decode(header[6:], validate=True).decode('ascii')
            user, password = raw.split(':', 1)
            if not re.fullmatch(r'pgv[0-9a-f]{24}', user) or not re.fullmatch(r'[0-9a-f]{48}', password):
                return None
            expected = self.settings()['users'].get(user, '')
            if hmac.compare_digest(expected, hashlib.sha256(password.encode()).hexdigest()):
                return user, password
        except (ValueError, UnicodeError):
            pass
        return None

    def revoke(self, user):
        for table in (self.links, self.sessions):
            for key, value in list(table.items()):
                if value['user'] == user:
                    del table[key]

    def create(self, user, password, home):
        settings = self.settings()
        files = profiles(settings, user, password, home)
        token = secrets.token_urlsafe(32)
        url = settings['origin'] + '/#' + token
        matrix = qr_rows(url)
        with self.lock:
            self.clean()
            if len(self.links) + len(self.sessions) >= MAX_SHARES:
                raise ValueError('Service busy; try again after 15 minutes')
            self.revoke(user)
            expires = self.clock() + TTL
            self.links[token] = dict(user=user, credential=hashlib.sha256(password.encode()).hexdigest(),
                                     expires=expires, files=files)
        return dict(Url=url, Expires=int(expires), Matrix=matrix)

    def claim(self, token):
        with self.lock:
            self.clean()
            value = self.links.pop(token, None)
            if value is None:
                return None
            ticket = secrets.token_urlsafe(32)
            self.sessions[ticket] = value
            return ticket

    def session(self, ticket):
        with self.lock:
            self.clean()
            return self.sessions.get(ticket)


STYLE = '''body{font:17px system-ui,sans-serif;background:#101723;color:#edf3ff;margin:0;padding:24px}
main{max-width:540px;margin:30px auto}h1{font-size:30px}p{line-height:1.6;color:#bbc9de}
button,a.button{display:block;box-sizing:border-box;width:100%;padding:18px;border:0;border-radius:14px;
background:#6de0c1;color:#10251f;text-align:center;font:700 18px system-ui;cursor:pointer;text-decoration:none;margin:16px 0}
a{color:#84c7ff}small{color:#bbc9de}button:disabled{opacity:.5}'''
SCRIPT = '''"use strict";
const token=location.hash.slice(1); history.replaceState(null,"",location.pathname);
const button=document.querySelector("button"), status=document.querySelector("#status");
button.disabled=!/^[A-Za-z0-9_-]{43}$/.test(token);
button.onclick=async()=>{button.disabled=true;status.textContent="Готовим профиль…";
try{const r=await fetch("/claim",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({token})});
if(!r.ok)throw Error();location.replace("/install");}
catch(e){status.textContent="Ссылка использована, отозвана или истекла. Создайте новый QR в ProGo.";}};'''


def page(content, script=False):
    return ('<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            '<title>ProGo — установка VPN</title><link rel="stylesheet" href="/style.css"><main>'
            + content + '</main>' + ('<script src="/claim.js"></script>' if script else '') + '</html>').encode()


class Handler(http.server.BaseHTTPRequestHandler):
    server_version = 'ProGo'
    protocol_version = 'HTTP/1.0'

    def setup(self):
        super().setup()
        self.connection.settimeout(10)

    def log_message(self, *_):
        # URL, cookie, Basic credentials, profile body and tokens are never logged.
        pass

    def reply(self, code, data=b'', mime='application/json', headers=None):
        if isinstance(data, dict):
            data = json.dumps(data).encode()
        self.send_response(code)
        self.send_header('Content-Type', mime)
        self.send_header('Content-Length', str(len(data)))
        self.send_header('Cache-Control', 'no-store')
        self.send_header('Referrer-Policy', 'no-referrer')
        self.send_header('X-Content-Type-Options', 'nosniff')
        self.send_header('Content-Security-Policy', "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'")
        for key, value in (headers or {}).items():
            self.send_header(key, value)
        self.end_headers()
        if self.command != 'HEAD':
            self.wfile.write(data)

    def dispatch(self):
        shares = self.server.shares
        settings = shares.settings()
        if self.headers.get('X-Forwarded-Proto') != 'https' or self.headers.get('Host') != urllib.parse.urlsplit(settings['origin']).netloc:
            self.reply(403)
            return
        if self.command in ('POST', 'DELETE'):
            origin = self.headers.get('Origin')
            if origin and origin != settings['origin']:
                self.reply(403)
                return
        if self.path == '/health' and self.command == 'GET':
            self.reply(200, dict(ServerId=settings['server_id']))
            return
        if self.path == '/api/share' and self.command in ('POST', 'DELETE'):
            auth = shares.authenticate(self.headers.get('Authorization', ''))
            if auth is None:
                self.reply(401)
                return
            if self.command == 'DELETE':
                with shares.lock:
                    shares.revoke(auth[0])
                self.reply(204)
            else:
                self.reply(200, shares.create(*auth, self.read_json().get('home')))
            return
        if self.path == '/claim' and self.command == 'POST':
            token = self.read_json().get('token', '')
            ticket = shares.claim(token) if isinstance(token, str) and TOKEN.fullmatch(token) else None
            if ticket is None:
                self.reply(410)
            else:
                self.reply(200, dict(ok=True), headers={'Set-Cookie': COOKIE + '=' + ticket + '; Secure; HttpOnly; SameSite=Strict; Path=/; Max-Age=' + str(TTL)})
            return
        if self.path == '/' and self.command == 'GET':
            self.reply(200, page('<h1>VPN на телефоне</h1><p>Установите личный профиль ProGo для iPhone или Android. '
                                 'Для работы домашний ПК должен быть включён.</p><button>Получить профиль</button>'
                                 '<p id="status">Нажмите кнопку на том телефоне, где нужен VPN. Ссылка одноразовая и действует 15 минут.</p>', True), 'text/html; charset=utf-8')
            return
        if self.command == 'GET' and self.path in ('/style.css', '/claim.js'):
            self.reply(200, (STYLE if self.path == '/style.css' else SCRIPT).encode(),
                       'text/css; charset=utf-8' if self.path == '/style.css' else 'text/javascript; charset=utf-8')
            return
        cookie = http.cookies.SimpleCookie()
        try:
            cookie.load(self.headers.get('Cookie', ''))
        except http.cookies.CookieError:
            pass
        ticket = cookie[COOKIE].value if COOKIE in cookie else ''
        session = shares.session(ticket)
        if self.path in ('/install', '/profile.mobileconfig', '/download/android'):
            if session is None:
                self.reply(410, page('<h1>Ссылка недоступна</h1><p>Время истекло или доступ отозван. Создайте новый QR в ProGo.</p>'), 'text/html; charset=utf-8')
                return
            if self.path == '/install' and self.command == 'GET':
                self.reply(200, page('<h1>Выберите телефон</h1><a class="button" href="/profile.mobileconfig">Установить на iPhone</a>'
                                     '<p>Откройте эту страницу в Safari. Разрешите загрузку. Затем: Настройки → Основные → VPN и управление устройством → ProGo → Установить.</p>'
                                     '<form method="post" action="/download/android"><button>Установить на Android</button></form>'
                                     '<p>Нужен <a href="https://play.google.com/store/apps/details?id=org.strongswan.android">strongSwan VPN Client</a>. '
                                     'Откройте скачанный файл ProGo.sswan в strongSwan и подтвердите импорт. Если файл не открылся автоматически: меню strongSwan → Импорт VPN-профиля → Загрузки.</p>'
                                     '<p>Включите профиль «ProGo — домашний VPN». Установка требует вашего подтверждения. Профиль содержит личный VPN-доступ; не пересылайте его.</p>'), 'text/html; charset=utf-8')
                return
            kind = 'iphone' if self.path == '/profile.mobileconfig' and self.command == 'GET' else 'android' if self.path == '/download/android' and self.command == 'POST' else None
            if kind:
                filename, mime = ('ProGo.mobileconfig', 'application/x-apple-aspen-config') if kind == 'iphone' else ('ProGo.sswan', 'application/vnd.strongswan.profile')
                self.reply(200, session['files'][kind], mime, {'Content-Disposition': 'attachment; filename="' + filename + '"'})
                return
        self.reply(404)

    def read_json(self):
        length = int(self.headers.get('Content-Length', '0'))
        if not 0 < length <= 2048 or self.headers.get('Content-Type', '').split(';')[0] != 'application/json' or self.headers.get('Transfer-Encoding'):
            raise ValueError('Invalid request')
        data = json.loads(self.rfile.read(length))
        if not isinstance(data, dict):
            raise ValueError('Invalid request')
        return data

    def handle_request(self):
        try:
            self.dispatch()
        except (ValueError, KeyError, TypeError):
            self.reply(400, dict(error='Invalid request or capacity limit'))
        except (ConnectionError, TimeoutError):
            pass
        except OSError:
            self.reply(503)

    do_GET = do_POST = do_DELETE = handle_request


class Server(http.server.ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self, address, shares):
        self.shares = shares
        self.slots = threading.BoundedSemaphore(24)
        super().__init__(address, Handler)

    def process_request(self, request, address):
        if not self.slots.acquire(blocking=False):
            self.shutdown_request(request)
            return
        try:
            super().process_request(request, address)
        except Exception:
            self.slots.release()
            raise

    def process_request_thread(self, request, address):
        try:
            super().process_request_thread(request, address)
        finally:
            self.slots.release()

    def service_actions(self):
        with self.shares.lock:
            self.shares.clean()


if __name__ == '__main__':
    Server(('127.0.0.1', 17879), Shares('/etc/progo-profile-share/publishers.json')).serve_forever()
