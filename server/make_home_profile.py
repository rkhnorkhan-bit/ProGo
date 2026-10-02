#!/usr/bin/env python3
"""Derive a separate home-entry profile from an existing unsigned IKEv2 profile."""
import argparse
import copy
import ipaddress
import os
from pathlib import Path
import plistlib
import re
import uuid


def build_profile(original, server):
    profile = copy.deepcopy(original)
    try:
        address = ipaddress.ip_address(server)
        if address.version != 4 or not address.is_global:
            raise ValueError('Use a public home IPv4 address or DNS name')
    except ValueError:
        if not re.fullmatch(r'(?=.{1,253}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}', server):
            raise ValueError('Use a public home IPv4 address or DNS name') from None
    payloads = profile.get('PayloadContent', [])
    vpns = [p for p in payloads if p.get('PayloadType') == 'com.apple.vpn.managed' and p.get('VPNType') == 'IKEv2']
    if len(vpns) != 1 or not vpns[0].get('IKEv2', {}).get('RemoteIdentifier'):
        raise ValueError('Expected one IKEv2 payload with RemoteIdentifier')
    vpn = vpns[0]
    vpn['IKEv2']['RemoteAddress'] = server
    # Keep the authenticated identity and certificate of the VPS. Do not advertise
    # alternate addresses that might make the phone bypass the home relay.
    vpn['IKEv2']['DisableMOBIKE'] = 1
    # Older exported profiles encoded disabled CHILD PFS as DH=0. iOS rejects 0;
    # use a valid group and the dedicated switch, without changing server policy.
    child = vpn['IKEv2'].get('ChildSecurityAssociationParameters', {})
    if child.get('DiffieHellmanGroup') == 0:
        child['DiffieHellmanGroup'] = 14
        vpn['IKEv2']['EnablePFS'] = 0
    vpn['UserDefinedName'] = 'ProGo — через домашний ПК'
    profile['PayloadDisplayName'] = vpn['UserDefinedName']
    replacements = {}
    for payload in [profile] + payloads:
        old = payload.get('PayloadUUID')
        new = str(uuid.uuid4()).upper()
        if old:
            replacements[old] = new
        payload['PayloadUUID'] = new
        payload['PayloadIdentifier'] = 'dev.progo.home.' + new.lower()

    def replace_refs(value, key=''):
        if isinstance(value, dict):
            return {k: replace_refs(v, k) for k, v in value.items()}
        if isinstance(value, list):
            return [replace_refs(v, key) for v in value]
        if isinstance(value, str) and key.endswith(('UUID', 'UUIDs')):
            return replacements.get(value, value)
        return value

    return replace_refs(profile)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('--server', required=True, help='Home public IPv4 address or DNS name')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    with args.source.open('rb') as file:
        profile = build_profile(plistlib.load(file), args.server)
    # Existing profiles are never overwritten, and any embedded credentials remain private.
    descriptor = os.open(args.output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, 'wb') as file:
        plistlib.dump(profile, file)
    print('Created home-entry profile:', args.output)


if __name__ == '__main__':
    main()
