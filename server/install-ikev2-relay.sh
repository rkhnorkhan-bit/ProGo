#!/usr/bin/env bash
set -euo pipefail

# Installs only the loopback transport helper. It does not change SSH, strongSwan,
# packet forwarding, NAT, certificates, passwords, or the existing firewall.
if [[ $EUID -ne 0 || $# -ne 1 ]]; then
    echo 'Usage (as root): bash install-ikev2-relay.sh LOCAL_IKEV2_IPV4' >&2
    exit 1
fi
PROGO_RELAY_SOURCE=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROGO_RELAY_TARGET=$(python3 - "$1" <<'PY'
import ipaddress, socket, sys
target = str(ipaddress.IPv4Address(sys.argv[1]))
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
    sock.bind((target, 0))  # Must be assigned on this VPS.
print(target)
PY
)
python3 - "$PROGO_RELAY_SOURCE/ikev2_relay.py" <<'PY'
import ast, pathlib, sys
ast.parse(pathlib.Path(sys.argv[1]).read_text())
PY
systemctl is-active --quiet strongswan.service || {
    echo 'strongswan.service must already be running.' >&2; exit 1;
}
PROGO_RELAY_UNIT=/etc/systemd/system/progo-ikev2-relay.service
if [[ -e $PROGO_RELAY_UNIT ]] && ! grep -qx '# Managed by ProGo IKEv2 relay installer' "$PROGO_RELAY_UNIT"; then
    echo 'A different service already uses this name; no files changed.' >&2; exit 1
fi
if [[ ! -e $PROGO_RELAY_UNIT && -n $(ss -H -lnt 'sport = :17878') ]]; then
    echo 'TCP port 17878 is occupied; no files changed.' >&2; exit 1
fi
install -d -m 755 /opt/progo-ikev2-relay
if [[ -e /opt/progo-ikev2-relay/ikev2_relay.py ]]; then
    cp -a /opt/progo-ikev2-relay/ikev2_relay.py /opt/progo-ikev2-relay/ikev2_relay.py.previous
fi
if [[ -e $PROGO_RELAY_UNIT ]]; then
    cp -a "$PROGO_RELAY_UNIT" "$PROGO_RELAY_UNIT.previous"
fi
install -m 644 "$PROGO_RELAY_SOURCE/ikev2_relay.py" /opt/progo-ikev2-relay/ikev2_relay.py
cat > "$PROGO_RELAY_UNIT" <<UNIT
# Managed by ProGo IKEv2 relay installer
[Unit]
Description=ProGo loopback transport for IKEv2
After=network-online.target strongswan.service
Wants=network-online.target

[Service]
Type=simple
ExecStart=/usr/bin/python3 -I /opt/progo-ikev2-relay/ikev2_relay.py --target $PROGO_RELAY_TARGET
Restart=on-failure
RestartSec=3
DynamicUser=yes
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
PrivateDevices=yes
RestrictAddressFamilies=AF_INET AF_UNIX
CapabilityBoundingSet=
MemoryMax=96M
TasksMax=16
LimitNOFILE=256

[Install]
WantedBy=multi-user.target
UNIT
chmod 644 "$PROGO_RELAY_UNIT"
systemctl daemon-reload
systemctl enable progo-ikev2-relay.service
systemctl restart progo-ikev2-relay.service
python3 - <<'PY'
import socket, time
for attempt in range(20):
    try:
        with socket.create_connection(('127.0.0.1', 17878), timeout=1) as sock:
            hello = b'PGIK\x01\x00'
            sock.sendall(hello)
            data = b''
            while len(data) < len(hello):
                part = sock.recv(len(hello) - len(data))
                if not part:
                    raise OSError('Relay closed the probe connection')
                data += part
            if data != hello:
                raise RuntimeError('Unexpected relay response')
        print('Relay transport ready on 127.0.0.1:17878. VPN authentication is not tested by this probe.')
        break
    except OSError:
        if attempt == 19:
            raise
        time.sleep(0.1)
PY
