## Independent review corrections (2026-10-29)

Reviewed baseline: clean617bf17c4e580863401405554ff17e9df0b284f4 /
tree817662bb1f1347d5ea186eccc0c0496c9236e868; cumulative patch178842 bytes,
SHA256 e38ba18023c67a56fb82798f3b354d14061e52e0c495a49e1b9902d9d1fa7d4b.
The three confirmed defects are corrected in this bounded follow-up:

- Both canonical readers require any relocation directory to begin at the unique
  non-writable/non-executable .reloc and fit both its virtual and raw extents.
  Only its RVA is normalized; directory size and complete .reloc bytes remain
  hashed. A missing directory is accepted only with no .reloc section. Redirection
  into excluded .rsrc, partial/out-of-bounds redirection and removal are refused.
  Python mutations cover the reported DIR64 block in resource slack, relocation
  byte/length changes, and permitted resource growth with .reloc raw/RVA movement.
  The Windows consumed C# seal fixture independently rejects resource redirection.
  Actual UpdateResource/MSVC output validation is still NOT_RUN.
- The native fixture leaf uses sizeof(literal) rather than magic lengths and
  requires exactly32 lowercase hexadecimal GUID characters. Portable tests execute
  that exact predicate for valid, short, long, uppercase and non-hex leaves.
- Launch-test unknown-tree state is monotonic. Exit3, timeout, launch/observation
  error cannot be cleared by launcher WaitForExit, Kill, or a later successful
  launch. Unknown retains the fixture. Windows regressions deliberately produce
  exit3, a later exit0 and timeout/kill/join, and retain their evidence. The native
  fixture also reports3 after timeout or nonempty/unknown job accounting rather
  than claiming descendants settled because its direct host exited.

Conditional DLL-origin finding: the original reparse/origin/hash guards did NOT
exclude unrelated DLLs already present in staging, and no ACL admission was added.
The native parent's SetDefaultDllDirectories is not child process policy. All
three actual ktmw32 P/Invoke declarations now carry method-level
DefaultDllImportSearchPaths(System32); no AssemblyDirectory/UserDirectories/legacy
fallback is requested on supported Windows10+/.NET Framework4.8. The production
host additionally starts with System32 as CWD; all sealed script/assembly paths
remain explicit and fixed. No real ACL change or DLL exploit reproduction occurred.
The Windows fixture checks the compiled declarations' attributes without calling
KTM/HKLM. Actual Windows DLL resolution remains NOT_RUN, rather than exploit proof.
OS-controlled System32/loader and absence of arbitrary injection remain assumptions.
Microsoft API contract:
https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.defaultdllimportsearchpathsattribute?view=netframework-4.8.1
https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.dllimportsearchpath?view=netframework-4.8.1

Existing suites, scheduler, deadlines, privacy, ownership guards and transaction
ordering are retained. No push/newCI, tool installation or real UAC/HKLM/ProgramFiles/
ACL/PC action. New source requires repeat independent review before publication.

# Native personal installation broker — local review candidate

Base checkpoint: `77e0ec291626598b3a214c5643c9cf2b559a0401`, tree
`8920488b5f322cd353bf440652a0e64311d910c7`. Reviewed CI763 source remains
`a517dab25ed0a5daeda040b368f5282e49bf5481`, content tree
`e0b4a28fe565d8e0608842250c5a5419c297181e`. This change has NOT run Windows CI.
No publication, real elevation, machine registration, installation or owner-PC proof.

## Consumed path

1. Build uses the existing MSVC x64 toolchain/SDK and Python, with no install or
   download fallback. An ephemeral RSA-3072 build seal stays in builder memory;
   only its public key is compiled into the native launcher. The launcher code,
   import inventory and manifest are measured before embedding any application
   payload. The managed executable embeds that native identity and public key.
   The complete fixed release inventory plus `PersonalBroker.ps1` is signed and
   added as native resources. Sealing must leave native code identity unchanged.
2. `ProGo.Setup.exe` (default/`--install`, or `--uninstall`) is NATIVE, not a CLR
   executable. It imports only Kernel32/static CRT and resolves optional Windows
   libraries by absolute System32 paths. Its adjacent managed `.config` is never
   interpreted as CLR startup configuration. Original process token and fixed
   KnownFolder directory handles are captured before ordinary `runas` UAC.
3. Native parent/worker authenticate actual kernel pipe peer PIDs, held process
   handles, physical image identity, same SID/session and the original limited
   token versus the elevated worker's linked token AuthenticationId. Already-full
   original tokens, standard-user/alternate-admin flows and unsupported identities
   are refused. No credentials are requested or handled by application code.
4. The native worker verifies the entire signed archive and exact compiled name
   inventory before creating payload leaves. Runtime is fixed ProgramFiles/ProGo;
   user data is fixed original-token LocalAppData/ProGo. KnownFolder resolution uses
   DONT_VERIFY and drive-device checks before filesystem access, refusing UNC,
   mapped network drives and unsupported device aliases without following them. Original ProgramFiles,
   LocalAppData, Programs and Startup directory handles cross the authenticated
   channel via DuplicateHandle, not caller paths or SID strings. Physical origins
   must agree across elevation/device namespaces. Original user write admission
   is required for user KnownFolders, never for ProgramFiles. Only fixed local
   volumes and non-reparse directory chains are admitted.
5. Complete sealed payload and native retry launcher are retained under fixed
   runtime/backups/personal-broker. CreateNew or exact pinned-byte reuse only;
   foreign/changed leaves are not replaced. Existing runtime/data/backup ownership
   is not inferred from this staging. Actual installer/uninstaller admission still
   consumes the pre-existing journal, terminal, installed-inventory and leaf guards.
6. Before ANY managed host starts, native code constructs a sorted allowlisted
   Unicode environment. No inherited CLR manager/profiler/startup-hook variables,
   PowerShell profiles or caller module search paths are copied. It launches the
   fixed System32 Windows PowerShell executable with explicit application path,
   protected working directory, `-NoProfile -NonInteractive -File` and the sealed
   driver. No ExecutionPolicy override or script-text evaluation is used.
   Machine/current-user execution policy can refuse this path; there is no bypass.
7. The managed session authenticates the native kernel pipe server against its
   embedded code identity and verifies the build seal. The native image handle,
   directory handles and original process token bind physical origin and lifetime.
   It also verifies the loaded managed assembly is the admitted staged file.
   Both existing Open-ProGoPersonal*Transaction factories now consume this live,
   single-use session. No production path/SID/hash-array/boolean factory exists.
8. Existing PersonalInstallTransaction/native HKLM64 ARP is consumed unchanged in
   ordering: install registration last; uninstall removal last. Success settlement
   additionally requires completed native registration and disposed transaction.
   ARP points at retained native `ProGo.Setup.exe --uninstall`; legacy retry source
   and its self-contained admission stage remain retained as recovery evidence.

## Authority and settlement

The initial trust root is the user-selected native bootstrap and ordinary OS UAC
consent for that image, on a trusted Windows installation. This does not authenticate
a publisher. The per-build RSA seal binds payload to the bootstrap's compiled public
key; it is NOT Authenticode and does not make an untrusted initial EXE trustworthy.
Independent candidate provenance/delivery remains required before owner-PC use.
Compromised OS/administrator code and arbitrary code injection into participating
processes are outside this boundary. The original native execution is trusted; PID
and image handles do not purport to prove arbitrary process-memory integrity.
Hash equality is revalidation, never a bootstrap
authority or substitute for the original native process/token/handle binding.

The native code identity includes loader headers, all non-resource code/data/import
and relocation bytes, and the embedded manifest. Resource/relocation placement fields
that UpdateResource changes are normalized. Only signed payload resources may vary.
Managed PE, TLS callbacks, delay imports, non-Kernel32 imports, executable/mutable
resource sections, overlaps and ambiguous resource inventories are refused. Native
DLL resolution is restricted before optional system libraries load.

Fixed frames: parent sends operation DWORD and four original directory references;
worker sends operation/original PID, four directory references and its pinned image
reference. Individual frames are at most eight bytes. References are routing to actual
kernel objects held by authenticated native processes, not serialized path authority.
One connection/request/claim/completion per process; wrong operation/replay is refused.
Heartbeat revalidates original liveness, fixed-root origins and the 300000ms lifetime.
Connect/read/result and settlement waits are bounded; pre-consent OS UI is controlled
by Windows/user, not an admitted privileged mutation.

The managed host is created suspended, assigned to a private kill-on-close job and
then resumed. No breakaway/policy override. Closing authority gives cooperative
catch/finally 5000ms; a non-settling exact job-owned tree is terminated, with bounded
exit observation. Forced/unknown settlement returns RECOVERY_REQUIRED (native code3),
never rollback-success. Pending/terminal/snapshot/staged bytes are retained. Cancelled
overlapped I/O keeps any not-yet-completed buffers/events owned until process exit;
there is no pending stack buffer or unbounded cancellation join.

Retained evidence is not deleted. No recursive delete fallback, service, autostart,
network, arbitrary command, new credentials or personal userdata removal is added.
Personal uninstall does not enter the legacy firewall policy cleanup; that separate
legacy path is unchanged. Auxiliary/pending/rollback shortcut defaults consume
read-only roots captured from the live personal capability, including nested dot
sourcing, rather than resolving the elevated environment again.
Legacy flows and the original33 Installer suites remain present. New build artifacts
are bootstrap-only, not new runtime delete inventory leaves. A different build's
retained broker or foreign registration is conservatively refused; this checkpoint
does not add a personal upgrade/migration authority.

## Verification separation

Local: Python198 tests =195PASS+3SKIP, including an actually compiled/executed portable
C harness using the same native state/environment/routing functions. Hostile inherited
manager/profiler variables cannot enter that launch block; wrong facts/operation,
replay, expiry, premature/double settlement are exercised. Synthetic PE tests bind
code/key/header/manifest and refuse managed/TLS/overlap/truncated input. These are
NOT Windows compilation, native token capture, CLR startup or elevated proof.

Prepared mandatory Windows tests (NOT_RUN):

- NativeBrokerLaunchTests: actual benign AppDomainManager pre-Main positive control;
  native adjacent-config and hostile inherited CLR/profiler variables; same native
  System32 launch function, no elevation and only fixed isolated temporary paths.
- PersonalBrokerTests: actual consumer factories, session Claim/Complete, runtime,
  registration-last and retained uninstall retry with file-backed fixture ARP.
  OS/elevation evidence is explicitly simulated under the compile-only fixture flag;
  the consumer factories are NOT overridden. Real build-seal/code substitution
  checks use the same managed image admission method against the produced artifact.
  Includes wrong origin, pre-CLR evidence refusal, alternate admin/session/dead
  original, operation, replay, expiry, runtime/registration/late-uninstall faults.
- Original Personal18/Native15/Stage10 and all mandatory suites/assertions/deadlines
  remain. Only the expected ARP command changes to the actual retained entrypoint.

MSVC/C# compilation, PowerShell AST/runtime, Windows fixtures, actual UAC/token/OS
code-origin proof, HKLM/ProgramFiles/ACL adversarial proof and owner-PC acceptance
are NOT_RUN. Missing local pwsh/dotnet/mono/csc is unchanged; no tools installed.
CI763 evidence applies ONLY to its old source. New source requires independent full
diff review, then a separately approved single Windows CI, with no duplicate run.

Windows API references checked during implementation:

- https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath
  Original-user tokens need QUERY/IMPERSONATE and sometimes DUPLICATE rights for
  this API. No thread impersonation or token privilege adjustment is performed.
- https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
- https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getnamedpipeserverprocessid
- https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getnamedpipeclientprocessid
- https://learn.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/appdomainmanagerassembly-element
