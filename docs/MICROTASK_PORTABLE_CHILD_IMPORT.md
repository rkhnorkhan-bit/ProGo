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
are unchanged in this patch. Independent source review and local whitespace
checks pass. This step does not claim a production archive defect.

## First native diagnostic and correction

[Run 38167482711](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38167482711)
tested functional head `8453e9e97d50fce71c4553e63371577210b91a07` through merge
`ed2bab8d395ec2a2ffcad8bb90f7e8e3bd50acb4`. Windows compilation, VaultRead
(165), daily policy (134), desktop (3485), full Test-ProGo and Home VPN (975)
passed. Windows job `114552949650` then failed the new EOF check at 19:30:34 UTC:
exit=1, no success marker, zero stdout characters, 53 stderr characters,
no truncation, fixed phase/type `prepare/portable`. Server job `114552949771`
passed. Later portable and Windows Python discovery were not reached; release
scan/packing/uploads were skipped.

The parent writes no password line for this case, yet the child crossed its
empty-input guard. This establishes a stdin/EOF fixture transport discrepancy.
The exact runtime code pages and transported bytes were not observed; a
specific byte sequence is not claimed as the native cause.

The correction reads the child's private stdin stream with a BOM-aware
StreamReader and UTF8 fallback, without changing Console.InputEncoding or
requiring a console handle. Eight memory vectors cover no-BOM UTF8, UTF8/UTF16
preambles, preamble-only EOF, empty EOF, a wrong literal and line termination.
The actual console-less child, original success requirements and all negative
destination-preservation checks remain mandatory. This is a test transport
helper, not a new production password decoding policy. Fresh native validation
of this correction is **PENDING**.

## Scope

Only synthetic archives and destinations on the isolated CI runner are used.
Import under another Windows account or on another PC, owner installation,
whole-installer acceptance and release remain pending. Historical 22/31 is
unchanged; no full F-point is closed by this harness correction.
