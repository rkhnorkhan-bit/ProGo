# CI697: bind Menu origin to owned publication, retaining full native identity

Baseline source8caf03239ce6d762aa49a044dad1a3270fd979de, tested merge
5c270df2b6a130a439db2fab5491c3bc235c13dd. Independently read full CI697 Windows121167474515
log10478 lines and Ubuntu121167474319 log578 lines: Windows FAILURE, Ubuntu SUCCESS.
Probe completed=True, witnessCalls1, beforeRead/afterRead=True, volumeSame/indexSame/bytesSame=True,
creationSame/originSame=False, elapsed11ms. Actual legacy-removed: bytes and all other proof gates
True, originSame/durableMenu=False. Menu32:2PASS/1FAIL/29NOT_RUN. Earlier installer/Startup15/14/13/
14/25/Desktop6394/Instance30/Maintenance126/Shutdown47 PASS; later suites/pack NOT_RUN.

## Proven cause / relevance / acceptance

Menu capture uses the same native EnsureLink rename as the isolated probe. Its callback ran BEFORE
publication, so the receipt retained pre-rename creation time while the actual same file object
acquired different creation time at publication. Full origin correctly rejected that stale receipt.
No proof of replacement in this valid fixture; volume/index/bytes stayed equal. No need to assert
an OS-internal explanation beyond the observed native transition. Bytes-only admission or removing
creation from the final origin would weaken protection and is explicitly NOT the fix.

Only capture-enabled Menu opts into a new private five-argument EnsureLink overload. Existing
four-argument witness (Startup/diagnostic), non-capture install, migration/native deletion and all
other APIs preserve previous behavior. Preserve the original temporary file's native read handle
through no-overwrite rename. Read before native object info, reject reparse/directory attributes,
then prove stable volume/index on the SAME retained handle after publication. A handle cannot
silently become the replacement object. Creation may legitimately differ across this owned rename;
this continuity check is not a new general identity or ownership rule.

WitnessPublishedLink then computes the FULL post-publication native origin (still including creation)
on that owned handle, checks exact expected bytes, opens the independently supplied published path
with a read pin, and requires the same full origin and bytes. Only then invoke the receipt witness,
with both handles held. Recheck full origin/bytes on both handles and current location before return.
External replacement before pin, changed bytes/origin/location, ambiguous witness/error all throw,
never return an accepted receipt. No blind update of saved receipts. Callback failure retains its
original exception; no retry/overwrite/delete of the foreign object. All own streams close.

## Regressions / unchanged guarantees

Original MenuAdmission32 and all prior fixtures/assertions/timeouts remain byte-identical. That
suite's actual production legacy-removed receipt/classifier is the end-to-end regression, including
identical Menu replacement and post-Startup replacement refusals. Diagnostic CLR probe remains
unchanged and still illustrates pre-publication origin change if an unexpected failure returns.

New MenuPublication6 invokes the EXACT production publication verifier on isolated native handles:
- Actual owned rename into a vacated target name changes creation while preserving volume/index;
  witness accepts only exact full post-publication origin and bytes. The new own temporary file's
  creation time is set to a fixed distinguishable synthetic value BEFORE opening/renaming it;
  no system settings/ACL or live shortcut are touched.
- Foreign replacement with identical bytes AND copied creation still fails native identity before
  callback. Retain original object alive, preventing file-index reuse; preserve foreign bytes.
- Different foreign bytes and real incomplete bytes also refuse without callback/overwrite.
- Exact original witness exception propagates, no replay.
- Replacement attempted during witness is refused by the actual published read pin; same original
  object and bytes remain. Exclusive reopen proves cleanup after success/error/refusal.
All six required, guarded own GUID-root cleanup, no waits/processes/timeouts relaxed.

No Menu rollback mutation, Legacy/runtime extension, full auxiliary outcome/success/Launch or new
features. Existing installer failure/original error/Stop/finally/private settings/vault/keys unchanged.
Local static/review/signatures/parity/secrets/public/JSON checks are not Windows compilation/native
proof: Publication6/Menu32/new exact regressions remain NOT_RUN until exact CI. Old unrelated
Python/rebind/sharing/repair/setup incidents UNKNOWN; old PowerShell probe inner cause still unknown,
new CLR diagnostic was proven executable at697. BLOCKED_TRANSFER/no retry, scores unchanged.
One guarded-source automatic CI, no parallel rerun. No live install/services/network/security/ACL/
credentials/VPS/release/uploads. Next exact final logs; no second functional stage this pass.
