# Micro11 — portable HTTP settings fixture

[Run 38168506823](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38168506823)
passed the complete portable archive harness, then Windows Python discovery
failed while importing `test_profile_share.py`. That HTTP test imported the
Linux provisioning module `profile_share_setup.py` only to call its atomic
writer. The provisioning module imports `fcntl`, which is unavailable on
Windows; the seven HTTP scenarios were therefore not executed there.

## Bounded correction

The HTTP fixture now publishes its synthetic settings with a test-local
writer: `tempfile.mkstemp` creates a unique staging file in the destination
directory; `os.fdopen` writes UTF8 JSON, flushes and closes it before
`os.replace` publishes the complete file. A `finally` block removes only the
owned staging path if it still exists. The fixture never truncates the active
settings file, so its live cleaner can continue reading complete JSON.

The Linux provisioning import is removed from this HTTP test. All seven HTTP
scenarios and their existing assertions remain unchanged, including concurrent
single-use claims, revocation, expiry, profile contents, authentication and
origin isolation. There is no module skip, retry or timeout increase.

Production server code and `test_profile_share_sync.py` are unchanged. The
latter still directly exercises the production writer's concurrent publication
and Linux file modes, plus flock ordering that prevents a delayed watcher
from restoring revoked credentials. The portable fixture does not claim to
verify Linux ownership or locking.

## Verification and limits

On the isolated local Linux workspace, the existing HTTP suite passed all
**7 tests**, and the unchanged Linux synchronization suite passed both
**2 tests**. `git diff --check` and independent read-only source review passed.
The HTTP QR test retains its existing
optional independent decoder branch; this result does not separately establish
that decoder's availability on this local interpreter.

Before the final native checkpoint below, Windows verification was pending.
`Shares.settings` uses ordinary
`Path.read_text`; a concurrently open reader could prevent `os.replace` under
Windows sharing rules. No such runtime failure has been observed for this
correction, and the fixture does not suppress or retry one. The actual Windows
HTTP scenarios must pass before this microtask receives native PASS.

Only synthetic loopback HTTP servers and temporary settings are used. No
owner PC, VPS, installed application or live service is accessed. This test
correction does not close a full plan point; historical **22/31** is unchanged.

## Final native checkpoint — 11 October 2026

Functional head `42c7ab60c9a3e035a712196765fa74a565f41f1a` contains micro11
`0e2a8c033b665c5e5cba5cb803a4a5d3fa1f833e` and micro12. Completed
[CI 38171560167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38171560167)
tested merge `cd12b11e921477c85f068eeb8580f259a8eed742` with main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

Windows job `114565055972` is **PASS**. All seven HTTP cases actually ran
and passed at 20:32:50.905–20:32:51.364 UTC. Complete Python discovery ran
24 items: 22 passed and two existing Linux ownership/flock module skips;
the HTTP module was not skipped. No Windows publication failure was observed.
This result does not prove absence of every possible reader-sharing race.

Ubuntu job `114565056126` is **PASS**: sharing 9/9 (seven HTTP plus two unchanged
Linux sync tests), operations 16, and isolated provisioning 46 checks. The QR
case passed after successful installation of numpy 2.2.6 and zxing-cpp 2.3.0.
Independent local dependency isolation also passed all seven HTTP tests with
both `fcntl` and `pwd` blocked, without skips.

Full Windows/server CI, public release-content scan and CI ZIP packaging passed.
Those CI artifacts are not a published release, a complete accepted installer
for all 31 points, or owner installation/visual acceptance. Source and completed
CI evidence received independent read-only review. Micro11 has native **PASS**;
historical **22/31** remains unchanged.
