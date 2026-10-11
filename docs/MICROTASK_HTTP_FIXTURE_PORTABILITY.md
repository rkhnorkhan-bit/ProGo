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

Native Windows verification remains pending. `Shares.settings` uses ordinary
`Path.read_text`; a concurrently open reader could prevent `os.replace` under
Windows sharing rules. No such runtime failure has been observed for this
correction, and the fixture does not suppress or retry one. The actual Windows
HTTP scenarios must pass before this microtask receives native PASS.

Only synthetic loopback HTTP servers and temporary settings are used. No
owner PC, VPS, installed application or live service is accessed. This test
correction does not close a full plan point; historical **22/31** is unchanged.
