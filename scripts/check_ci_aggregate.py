"""Fail closed unless every required partition succeeded on the exact tested commit."""
import json
import os
import re

REQUIRED = ('vpn-server-test', 'windows-build', 'installer-tests', 'installer-live-tests', 'installer-recovery-tests', 'installer-recovery-input-tests', 'installer-runtime-tests', 'installer-success-tests')


def validate(needs, expected_sha):
    if not isinstance(expected_sha, str) or not re.fullmatch(r'[0-9a-f]{40}', expected_sha):
        raise ValueError('Expected tested commit is missing or invalid')
    if not isinstance(needs, dict) or set(needs) != set(REQUIRED):
        raise ValueError('Required partition set is incomplete or unexpected')
    for name in REQUIRED:
        job = needs[name]
        if not isinstance(job, dict) or job.get('result') != 'success':
            raise ValueError('Required partition did not succeed: ' + name)
        outputs = job.get('outputs')
        if not isinstance(outputs, dict) or outputs.get('tested_sha') != expected_sha:
            raise ValueError('Required partition tested commit differs or is missing: ' + name)


def main():
    try:
        validate(json.loads(os.environ.get('PROGO_NEEDS_JSON', 'null')), os.environ.get('PROGO_EXPECTED_SHA'))
    except (ValueError, TypeError):
        # No untrusted input/context dumps in the log.
        raise SystemExit('CI aggregate FAIL: all required partitions must succeed on the exact tested SHA')
    print('CI aggregate PASS: Core, Installer Foundation, Installer Live, Installer Recovery, Installer RecoveryInput, Runtime, Success and Ubuntu succeeded on the exact tested SHA')


if __name__ == '__main__':
    main()
