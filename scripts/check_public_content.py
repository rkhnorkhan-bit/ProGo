"""Guard public sources and release files against infrastructure-specific examples.

This check supplements manual review. It deliberately reports paths/categories,
never the matched value. Do not add real addresses or credentials as test data.
"""
import argparse
import ipaddress
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
IPV4 = re.compile(r'(?<![\d.])(?:\d{1,3}\.){3}\d{1,3}(?![\d.])')
DOCUMENTATION_NETS = tuple(map(ipaddress.ip_network, (
    '192.0.2.0/24', '198.51.100.0/24', '203.0.113.0/24',
)))
PERSONAL_PATH = re.compile(
    r'[A-Za-z]:[\\/]+Users[\\/]+(?!Public[\\/])[^\s\\/"\'<>]+[\\/]'
    r'|/(?:home|Users)/[A-Za-z0-9_.-]+/|/root(?=/)'
)
PRIVATE_KEY = re.compile(r'-----BEGIN (?:[A-Z]+ )?PRIVATE KEY-----')
TEXT_SUFFIXES = {'.cs', '.ps1', '.py', '.sh', '.md', '.json', '.yml', '.yaml', '.csproj'}
SECRET_SUFFIXES = {'.pem', '.key', '.pfx', '.p12', '.mobileconfig', '.log'}


def issues(path, release=False):
    name = path.name.lower()
    if release and (path.suffix.lower() in SECRET_SUFFIXES or name.startswith(('vault', '.env'))
                    or name in ('settings.json', 'password.txt', 'credentials.json')):
        yield 'runtime or credential-bearing file'
    data = path.read_bytes()
    encodings = ('utf-8', 'utf-16-le') if path.suffix.lower() == '.exe' else ('utf-8',)
    for encoding in encodings:
        text = data.decode(encoding, errors='ignore')
        if PERSONAL_PATH.search(text):
            yield 'personal filesystem path'
        if PRIVATE_KEY.search(text):
            yield 'private key material'
        for match in IPV4.finditer(text):
            # PE manifests and assembly-qualified names contain four-part versions.
            if re.search(r'\bversion\s*=\s*["\']?$', text[max(0, match.start() - 32):match.start()], re.I):
                continue
            try:
                address = ipaddress.IPv4Address(match.group())
            except ipaddress.AddressValueError:
                continue
            if address.is_global and not any(address in net for net in DOCUMENTATION_NETS):
                yield 'non-documentation public IPv4 literal'
                break


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--release', type=pathlib.Path)
    args = parser.parse_args()
    sources = list(ROOT.glob('*.md'))
    for folder in ('src', 'scripts', 'server', 'tests', 'docs', '.github'):
        sources.extend(p for p in (ROOT / folder).rglob('*')
                       if p.is_file() and p.suffix in TEXT_SUFFIXES
                       and not {'__pycache__', 'bin', 'obj'}.intersection(p.parts))
    failures = []
    for path in sources:
        failures.extend((path.relative_to(ROOT), issue) for issue in set(issues(path)))
    release_files = []
    if args.release:
        if not args.release.is_dir():
            parser.error('Release directory is missing')
        release_files = [p for p in args.release.rglob('*') if p.is_file()]
        for path in release_files:
            failures.extend((path.relative_to(args.release), issue)
                            for issue in set(issues(path, release=True)))
    for path, issue in failures:
        print(f'PUBLIC CONTENT FAIL: {path}: {issue}', file=sys.stderr)
    if failures:
        return 1
    print(f'Public content PASS: {len(sources)} source files, {len(release_files)} release files.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
