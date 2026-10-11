# Micro09 — independent portable child import

Previous run [38164605392](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38164605392)
passed 14 portable checks, then failed the combined separate-process import
assertion. Its failing operand and import cause were not established. Parent
archive authentication and clean-destination DPAPI import had passed; this
does not establish success in the independent child.

## Bounded diagnostic correction

The test-only child now emits fixed phase/type codes for stdin, prepare and
import. Empty stdin is rejected before encoding or archive access. Exception
messages, passwords, tokens and paths are never printed by this child.

The parent drains stdout and stderr concurrently, retaining at most 4096
characters per channel and rejecting truncation. The original 60-second child
deadline includes stream capture. An unsuccessful owned child is killed with
a bounded two-second exit wait; captured pipe handles are closed on failure.
This supervises the fixture's single process, not arbitrary descendant trees.

Named diagnostics contain only exit code, success-marker presence, character
counts, truncation and allowlisted phase/type. Raw captured output is not
included in a failure message. The original success requirements remain:
exit zero, a child PASS marker and no token. Empty stderr and absence of both
fixture passwords on both channels are additional requirements.

Regressions exercise stdin EOF, a wrong password and refusal to replace an
existing destination. Each requires nonzero exit, no PASS marker, the expected
phase/type and no secret output. EOF/wrong-password cases leave the destination
absent; existing-target refusal preserves every original byte. The successful
child still must agree with the independently imported DPAPI destination.

Application archive code and the existing cryptographic/deadline conditions
are unchanged in this diagnostic patch. Independent source review and local
whitespace checks pass. Native compilation, the new regressions and the actual
failed operand remain **NOT_CHECKED** until the fresh Windows run. This step
does not yet claim an application defect or a completed import fix.

## Scope

Only synthetic archives and destinations on the isolated CI runner are used.
Import under another Windows account or on another PC, owner installation,
whole-installer acceptance and release remain pending. Historical 22/31 is
unchanged; no full F-point is closed by this harness correction.
