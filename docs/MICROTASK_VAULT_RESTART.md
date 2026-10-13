# F09 — micro23: native daily-vault restart proof

Status: source implemented; native Windows CI pending. F09 remains OPEN until the full expiry/lifecycle packet passes.

CI #536 (`38447079663`, tested source `4872593`, merge `ebaadcb`) stopped on the first `missing` child after confirmed process/pipe settlement. The original parent hid child stderr, so that run did not identify the child failure phase. No downstream restart/runtime/desktop PASS is claimed for that run. The follow-up source makes private stdin decoding explicit and adds strictly validated numeric failure diagnostics; a fresh native run is required. Argv quoting was checked and left unchanged.

`tests/VaultDailyRestartTests.cs` is an independent executable. It launches its own executable as fresh child processes under the current Windows SID and uses the production real-vault reader/writer, daily DPAPI codec, recovery/store, lease, fake-data factory and expiry worker with explicit owned temporary paths.

The child scenarios cover:

- startup with an absent daily file leaves it absent;
- a first process creates the real vault, commits one accepted mismatch and saves Add/Edit/Delete changes through the production persistence API;
- a fresh process on the same day, including 23:59:59, reloads all IDs, fields, secrets and null-versus-empty values exactly without calling the factory;
- another mismatch returns exactly `Неверный пин-код`, while the correct PIN opens the original real data;
- pre-cancelled next-day cleanup retains the exact committed daily bytes;
- a fresh process on a new day calls the production expiry worker before opening, verifies a committed unbound generation without the old PIN/data, then obtains a new binding with fresh IDs and secrets;
- deleting all entries remains a bound empty dataset across a fresh process and permits no second PIN;
- a backwards date change expires the previous binding and produces another independent dataset;
- an actual `FileShare.None` read lock and corrupted DPAPI bytes fail closed for the daily route, preserve evidence and leave correct-PIN authentication available;
- every daily operation leaves the exact real ciphertext unchanged and no transaction staging/recovery files behind.

PINs are passed through redirected stdin, never through argv or test output. The fixture request is ASCII-only and its parent encoding is checked for identical UTF-8 payload bytes. The headless child uses its own strict UTF-8 reader rather than Framework `Console.In`, recognizes one UTF-8 transport preamble and retains the bounded input limit. Expected DTOs are kept only in an owned temporary proof protected with CurrentUser DPAPI and a separate test purpose.

Child stdout contains one static success/count/BOM-metadata frame. On failure, the parent reconstructs only bounded numeric phase/check/status/BOM metadata and whitelisted exception/status labels; raw stdout/stderr, paths, input and DTOs are never forwarded. The first stage also reports only the writer preamble length and child leading-preamble flag. Each child has a 45-second execution deadline and bounded pipe settlement. EOF handlers use completion sources that remain safe for late callbacks. Owned temporary fixtures are removed only after child termination is confirmed; a child that cannot be stopped fails the suite and retains its owned fixture rather than racing directory removal against a writer.

This is a native same-SID process-restart test with injected local dates. It does not claim an OS reboot, hardware memory erasure, trusted wall-clock protection, a hostile same-user filesystem sandbox or inspection of the owner's installed application/devices. UI Add/Edit/Delete and active expiry are covered by the corresponding desktop fixtures, rather than this process harness.

Source implementation / independent contracts review: **APPROVE / FROZEN**. Native compile / full CI остаются **PENDING**.
