# Smart Routing (experimental branch, Windows 11)

This feature is **opt-in** and does not change the existing SSH/SOCKS and Windows
proxy behavior unless enabled. It is **not** included in a published ProGo release.

## User experience

In **Настройки → Приложения**, ProGo discovers running programs and installed
programs that expose an executable in their uninstall registry entry.
**Выбрать программу…** opens a standard Windows `.exe` file picker. Rules are
matched by the *full executable path*, not just the filename.

Each application supports:

- **Авто**: safe default **DIRECT** for now; adaptive diagnosis and learning
  are **not implemented**. In the next phase this may select a route dynamically.
- **VPS**: TCP through the ProGo SSH SOCKS5 tunnel; UDP for this process is
  explicitly rejected when TUN is running, avoiding silent direct UDP fallback.
- **Прямо**: direct connection even when the TUN engine is active.

Rules are saved in the existing `settings.json` as `ApplicationRoutes`.
Unknown and duplicate entries are normalized without rewriting secrets.
Windows system executables, ProGo itself, and sing-box are not allowed as
forced proxy processes.

## Technical architecture

The TUN engine is **sing-box 1.14+**, distributed separately. No binary is
downloaded or auto-executed by ProGo. Obtain `sing-box.exe` from the official
project, verify its provenance, and select it on the Applications tab. The
default manual install location is:

`%LOCALAPPDATA%\ProGo\tools\sing-box.exe`

Enabling **SmartRoutingEnabled** makes ProGo try to start the engine once its
SSH tunnel is ready. The engine is checked with `sing-box check -c` before
startup. Only the child process created by this ProGo instance is stopped.
A limited retry mechanism handles transient engine errors; a bad binary or
missing privileges is reported without an endless restart loop.

**TUN requires administrator rights**. The current version requires ProGo to
be started as administrator to start the routing engine. A permanent
least-privilege Windows service and an installer with one-time UAC consent are
future work.

The new TUN mode is incompatible with the old automatic global Windows HTTP
proxy. Turn off `AutoSystemProxy` in Automation, and restore the manually
applied Windows proxy once before enabling TUN. The engine will restore only
a Windows proxy value already known to belong to ProGo; it does not alter
third-party proxy settings.

Routing configuration is generated at
`%LOCALAPPDATA%\ProGo\smart-routing\active.json` and includes:

- TCP SOCKS5 outbound at the existing local SSH `SocksHost:SocksPort`
- direct outbound for unknown connections and private/LAN IPs
- explicit process-path rules for VPS/direct
- DNS hijacking handled by local DNS and IPv4/IPv6 TUN addresses
- `auto_detect_interface` to reduce routing loops

## Safety and limitations

- **This is not a privacy kill switch.** If the TUN process exits, the OS may
  resume direct networking. Never rely on `VPS` mode alone for anonymity or
  confidentiality of sensitive traffic; full fail-closed WFP policy is future
  work. While active, unsupported UDP for VPS-selected programs is rejected.
- DNS resolution still uses the local resolver. A proxied app's DNS queries
  may disclose domains outside the VPS. Remote DNS routing is future work.
- Per-process attribution is dependent on the operating system. Connections
  from child processes, Windows services or webview/browser subprocesses
  may need additional explicit rules.
- TUN may interfere with Hyper-V, WSL, firewalls and other VPNs; smoke-test
  these on an isolated Windows VM before deployment.
- There is **no real automatic fallback learning yet**. The Autо UI option is
  intentionally DIRECT until an audited route classifier exists.
- Editing rules requires Save; updating a running configuration restarts only
  ProGo's own TUN process, which can interrupt active connections.
- ProGo never installs kernel drivers, certificate roots, or firewall
  exceptions automatically. Back up `settings.json` before experiments.

## Acceptance steps

1. On Windows CI, build ProGo with `scripts/Test-ProGo.ps1`; check that
   `ProGo.exe --self-check` passes and the routing JSON is parseable.
2. Install a verified supported sing-box Windows binary **in a disposable
   VM**, run `sing-box check -c active.json` and confirm no schema warnings.
3. Verify Battle.net / Let's Encrypt CRL by direct TLS and Windows CAPI2:
   no permanent global HTTP proxy, no `0x80092013`.
4. Verify a selected TCP test program appears at the VPS exit IP; unselected
   programs and LAN/Hyper-V remain direct.
5. Confirm that UDP from the selected VPS program is rejected (no direct
   UDP leak) while TUN is active.
6. Stop and crash-test the engine; document that direct fallback currently
   occurs. Do not release for privacy-critical traffic before a kill switch.
7. Reboot and confirm opt-in auto-start (requires administrator context);
   ensure no unrelated services or network settings are modified.
8. Verify restore of the previous installer/version and settings backup.

Do not publish as a production release until Windows TUN, firewall, DNS
and Hyper-V acceptance tests are passed.
