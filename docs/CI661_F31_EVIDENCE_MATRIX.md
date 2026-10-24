# CI661: итоговая матрица F31 и приватный candidate plan

Canonical evidence for this reconciliation only: source `2b068cf909e761c27b9594bec91d78b582fce68d`, tested merge `77904dff703f65002d306bf5acc84a2f15df98ab`,
[CI661](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40085290372), Windows120846867400/Ubuntu120846867200 terminal SUCCESS. Full final logs independently read
(Windows15385/Ubuntu578 lines); policy214/Desktop6388/Instance30/HomeVPN4816, recover-invite10 and all prior
transport/ownership/cleanup/HTTPS/export suites PASS. Maintenance126/Shutdown47/Portable91 PASS.
Python28 includes2SKIPPED, not28 executed passes. Pack runner-only SUCCESS; uploads/prune SKIPPED.
Exact run artifacts API reports0. No repeat harness/new CI/code change in this step.

Sources: [original55](REMAINING_SUBTASKS_8.md), [approved31](APPROVED_PLAN_31.md),
[agent spec](SSH_AGENT_AUTOSTART_SPEC.md#критерии-приёмки). Matrix subdivisions are evidence rows, not new
requirement IDs or new acceptance conditions. PASS below applies ONLY its named isolated dev criterion,
never whole F31, target, package or production. SCM fixture outcomes are not real successful StartService.

## Criterion → evidence → gap

| Original criterion | Status | Exact CI661 evidence/test | Reality boundary | Exact remaining gap |
| --- | --- | --- | --- | --- |
| F31-R09 · ShareOrigin/home-address save | PASS isolated dev | HomeVpnWaitingTests; HomeVpnShareRecoveryTests:10:36:35 owned address write;10:41:19 frozen health+POST; HTTPS recovery11/fresh10 | Real protected files/DPAPI, worker gates and loopback HTTP; not live VPS | Owner-PC storage/performance/HTTPS acceptance NOT_RUN; no new dev harness needed |
| F31-R09 · token/owner/session preparation | PASS isolated dev | HomeVpnWaitingTests; HomeVpnSetupRecoveryTests: pair/import/receipt worker and real modal import/initial snapshot; HomeVPN4816 | Actual private-file I/O/DPAPI, test-owned session/process, frozen identity | Target install/profile/data migration NOT_RUN; old620 missing UI note superseded |
| F31-R09 · ACL/secret/helper I/O off UI | PASS isolated dev | Admin snapshot12/dispatcher5; owned repair8/list10/setup recover10/fresh10/invite8/revoke10/Reissue10/recover-invite10 | Actual helper staging/read/delete and owned worker; SSH executable/copy seams for Admin; no live mutation | No pending transport implementation after CI661; actual VPS acceptance NOT_RUN |
| F31-R09 · no late save after cancel/scope change | PASS isolated dev | HomeVpnShareRecoveryTests10:41:04–19 Stop refuses later save/consume; configure close10:41:23; import/pair/consumption regressions | Real journals and held worker I/O, explicit UI owner/lost-context scenarios | Owner-PC interruption acceptance NOT_RUN; no stale sync-UI FAIL demonstrated on current source |
| F31-R10 · visible incomplete cleanup + retained owner | PASS isolated dev | ownership8/cleanup4;10:38:25–26 real locked helper heartbeat; transport cleanup-lock cases incl new recover | NTFS locked files + physical task/cleanup ownership; false Stop until unlock, delete-only retry | Target antivirus/locks acceptance NOT_RUN; do not repeat known cleanup harness |
| F31-R10 · nonblocking Stop/Dispose/session deletion | PASS isolated dev | HomeVpnWaitingTests/OwnerWaitingTests/AdminRecoveryTests; Stop-copy/dispose/owner-dispose; Shutdown47/Maintenance126 | Actual task/process trees and modal pump; only owned children stopped | Target shutdown/update acceptance NOT_RUN; old desktop Stop/F09 not reopened |
| F31-R11 · bounded/shared async operation admission | PASS isolated dev | DesktopSshAgentProbeTests/StartTests/StartupTests/InventoryTests; AuthenticationPreparationBudgetAndFreshness | Unit/native-boundary held SCM fixtures, real UI heartbeat/deadline/shared slots | Real service lifecycle on owner machine NOT_RUN; no SCM changes made here |
| F31-R11/PGA scenarios1,5 · selected key/alias ready | PASS isolated dev | DesktopSshKeyMatchingTests/KeyReadinessTests; SshAuthenticationPreparationTests native + real tunnel; policy214 | Actual ssh-G/keygen/signing and test-owned agent endpoint; correct identity authenticated | Actual owner profiles/encrypted keys/agent account and alias environment NOT_RUN |
| F31-R11 scenarios2,3 · service start existing rights/no UAC | PASS dev contract; live NOT_RUN | DesktopSshAgentStartTests/StartupTests/StartUi tests;10:32:20 eligibility/denied/unavailable/held-call UI | SCM adapter outcomes injected; actual start tested with invalid handle, not successful real service mutation | Owner service allowed/denied scenario needs separate opt-in/approved Windows test; no pretend live start PASS |
| F31-R11 scenario4 · encrypted-key interactive add | PASS isolated dev | DesktopSshInteractiveKeyAddTests10:32:15 actual ssh-add unlocks selected encrypted fixture key, one console entry; key-add UI cases | Real ssh-add + encrypted synthetic key + isolated endpoint; production passphrase not read or stored | Owner encrypted key/console/desktop integration NOT_RUN |
| F31-R11 scenarios6,7 · missing key/unsafe ACL/errors | PASS dev classification | DesktopSshAuthPolicyTests/ConfigTests/KeyReadinessTests; SSH error diagnostic tests | Actual native Include/locked-key cases plus controlled error-output classification; no production ACL repair | Actual owner key/changed host-key refusal NOT_RUN; security approval needed for any live security mutation |
| F31-R11 scenario8 · agent optional/direct auth | PASS isolated integration | SshAuthenticationPreparationTests.AuthenticationPreparationRealTunnelCase direct startup/recovery10:32:00–02 | Actual owned SSH authentication→SOCKS5→HTTP without default agent dependency | Owner direct-auth/network NOT_RUN |
| F31-R11 scenario9 · backoff/recovery/cancel | PASS isolated integration | Same test direct/agent recovery after interrupt only owned SSH child; DesktopRouteDiagnostic partial SOCKS deadline/cancel | Real loopback SSH/socket/process recovery and controlled slow network peers | External network/drop/idle and authorized VPS acceptance NOT_RUN under F19/F22 |
| F31-R11 scenario10 · app close/owned-only cleanup | PASS isolated dev | Shutdown47/Maintenance126, native context ownership/Cancel Retry/Stop Dispose tests; fixture server cleanup deadline | Owned processes/modal UI tasks; unrelated process survival asserted | Installed target shutdown/terminal/agent sessions NOT_RUN |
| F31-R11 · slow disk/process/modal Cancel Retry | PASS isolated dev | HomeVpnWaitingTests/AdminRecoveryTests; export modal11/chooser3; actual NTFS locks, held filesystem calls, native Copy/Wait children | Real locked file/process boundaries plus explicit held fixture gates, not a physical slow-HDD benchmark | Actual HDD/AV/environment responsiveness belongs owner acceptance, NOT_RUN; no new harness required |
| F31-R11 · latency/CPU/RAM/process-I/O report | PASS measured isolated scope | SshAuthenticationPreparationTests.AuthenticationFixtureMetrics; four COMBINED_SSH_METRICS rows below | Actual exact owned SSH child lifetime GetProcessIoCounters/CPU/working set; startup/recovery elapsed and prep | Not physical disk totals or whole-app aggregate; not owner HDD acceptance; no new thresholds invented |
| F31-R11/PGA08 · accessible actions and fresh result | PASS automated native UI; target NOT_RUN | DesktopSshKeyReadinessUiTests/key-add/service-start UI; keyboard Tab/ShiftTab/explicit actions/heartbeat and stale evidence guards | Actual WinForms controls + controlled service states; policy UI native | Final installed DPI/screen-reader/owner desktop acceptance separate F16/F17 gates, NOT_RUN |
| PGA04 + scenario11 · installer opt-in/upgrade/uninstall | TODO/BLOCKED package-code gate | APPROVED_PLAN_31 + SSH_AGENT_AUTOSTART_SPEC PGA04 explicitly not implemented; Install-ProGo.ps1 has no agent opt-in | Existing installer is a source/build script; no standalone signed installer proof | F23 installer opt-in OFF default and preservation workflow still need separate bounded implementation/tests; not repaired by rerunning F31 harness |
| F31 target transfer + full acceptance | NOT_RUN | Portable91 explicitly NOT_CHECKED other Windows account/PC; CI uploaded artifacts0 | Runner only; isolated own files/processes | Owner PC/VPS/other-user/phone scenario and final install/update acceptance remain mandatory |

## Existing measured performance — do not rerun for the same report

| Mode/phase | elapsed ms | prep ms | child CPU ms | working/peak bytes | I/O operations read/write/other | I/O bytes read/write/other |
| --- | ---: | --- | ---: | --- | --- | --- |
| direct/startup | 1937 | 23 | 15.625 | 11452416/11530240 | 16/0/224 | 7256/0/8395 |
| direct/recovery | 1935 | 23,23 | 15.625 | 11427840/11501568 | 16/0/226 | 7256/0/8447 |
| agent/startup | 1980 | 67 | 15.625 | 11362304/11436032 | 14/6/207 | 3644/1319/9031 |
| agent/recovery | 1990 | 67,67 | 31.25 | 11354112/11423744 | 14/6/207 | 3644/1319/9031 |

These are measured child-lifetime counters, not write throughput/physical HDD load or totals for UI, agent
and server. Four deterministic isolated paths already satisfy the existing scoped report requirement; no
additional whole-app metric threshold is invented. Host-key known_hosts is fixture-pinned; this does not
prove owner changed-host-key handling in a real network. No user environment acceptance is credited.

## Exact private package plan — no binary created or transferred in this step

- VERSION **0.2.2**, build informational identity **0.2.2+77904dff703f65002d306bf5acc84a2f15df98ab**.
- Logical source **2b068cf909e761c27b9594bec91d78b582fce68d**, build/test checkout must be exact tested
  merge **77904dff703f65002d306bf5acc84a2f15df98ab**, clean isolated Windows checkout; record both IDs.
- Observed CI661 runner ZIP: **641099 bytes**, SHA256
  **a8a3ac56a7cadb711a3901aaa4c728cd5b4fb32456c0142b8481b12c8b9602b3**. This authenticates only that
  ephemeral runner ZIP; it is not an available/downloadable package, signed release or installed product.
  Artifacts0/uploads skipped. Do not advertise it as delivered or assign that hash to a rebuild.
- Shortest independent next step: **build/package only**, existing scripts/Build-ProGo.ps1 at that clean
  tested merge in an available isolated Windows environment with nonpublic persistent output. Reuse exact
  CI661 test evidence; do not repeat Test-ProGo/full CI merely to re-produce the ZIP. No current runner or
  Windows environment is accessible here. An approved private output/transfer route is a protected delivery
  dependency; public Actions uploads stay OFF, workflow unchanged. No binary transfer now.
- Tentative name: ProGo-0.2.2-ci661-77904dff-private-candidate.zip; create new SHA256 after the actual ZIP
  exists, version/source/merge/byte count/file SHA manifest and build-only log. ZIP timestamps can change
  hash even for identical binaries; verify assembly informational identity and full payload manifest against
  the pinned tree. Fresh rebuild SHA256 is **NOT_RUN**, never assumed equal to the observed runner hash.
- Package content gate: release payload + runtime scripts/licenses/readme/manifest + this test provenance,
  no build/vpn-fixture credentials, owner profiles, logs/config secrets or test keys. Re-run public/secret
  content checks on the actual packaged files; source check is not a package check. Extracted-launch smoke
  and signature/AV status must be recorded on the real package. No live connection or system network change.
- This ZIP would be a private engineering diagnostic bundle, **not the approved complete standalone test
  installer**. Existing Install-ProGo.ps1 invokes a source build and hardcodes its local application path;
  do not present it as a ready standalone installer or use it blindly to install a ZIP. F23 installer/updater,
  PGA04 opt-in OFF, path preservation/fallback and maintenance gates remain separate code/package work.

## Install/check/rollback protocol to include with the eventual package

No installation is authorized/executed here. Before owner-approved installation/update: record current
version/source, program/data locations, protected profiles/settings/key-reference inventory and encrypted
backup; never export plaintext credentials. Existing private data/keys must not be overwritten. Validate
actual package SHA256/manifest/build identity; select install/new or update explicitly using the completed
installer, not an ad-hoc copy over a running application. Check launch/UI response/profile/selected key and
agent/direct SSH→SOCKS5→HTTP on approved resources, Cancel/Retry/close/recovery and update preservation.
Only after acceptance promote candidate. Rollback: stop only candidate-owned processes, reinstall/restore
previous verified program payload and use its protected backup/data compatibility protocol; never delete
profiles/keys or overwrite newer data without compatible restore evidence. If cleanup/data compatibility
is unconfirmed, keep both payloads/backups, fail closed and ask owner before any restore. Reversible
isolated smoke may be scheduled after package exists; owner-PC/live checks remain NOT_RUN.

## Measurable residual — do not conflate three goals

**Personal private candidate:** no transport paths left unimplemented in the audited slice. Next1 build-only
pinned private bundle + new manifest/hash/content/smoke, dependent on isolated Windows/private output.
Then1 owner-approved target acceptance pass and its actual issues. This does not remove original installer,
key-agent, privacy, data-preservation or approved product requirements.

**Technical production / approved test installer:** F23 standalone installer/updater/fallback with intended
paths/launch/offline/cancel/hash/self-check/rollback and PGA04 OFF-by-default explicit opt-in; package/update/
uninstall preservation; F19/F22 real portability/user/PC/approved VPS/network/idle; F27 signing/timestamp and
Defender/Kaspersky; target DPI/assistive gates; final integration/merge/release and live-resource approvals.
No new security action is implied by an agent unit test. Source-level PGA04 installer preparation can be a
separate bounded stage; real service changes require the install-time explicit consent already specified.

**Full commercial:** existing F30 email/account/billing/dev-sandbox and dependent real delivery/payment/
webhooks/refunds/session revocation gates stay in the original registry; not added here, not removed and
not considered done by a personal diagnostic ZIP. Approved full test installer sequence is retained.

Conclusion: existing isolated F31 worker/lifecycle/selected-key/combined SSH/performance harness is covered;
no newly proven runtime/test defect warrants another CI in this step. Whole R09/R10 remain PARTIAL until
package/integration/target layers; R11 remains PARTIAL additionally because existing PGA04 installer gate
is open. No score change: fixed method58–80/22,+0. Old R09 FAIL notes are history, superseded for current dev
criterion only; they must not trigger another implementation of already verified UI migration.
