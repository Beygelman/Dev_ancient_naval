# v020.7 correction — artifact verification, 5 October 2026

**Delivery status: verification candidate. Final frame-pacing approval is pending.**
The gameplay version remains v020.7. No final corrected tag/package is claimed.

## Source and preservation

Correction source: branch `codex/v0207-corrections`, commit
`697db77336f3053493276ba56607e727cbcf16a4`. Its starting checkpoint was
`376c8b3537ef602f386427021d53efe548cab15b`, with gameplay baseline
`b03c5bdc85498f5f5d76f03f4d070adcaa13aa34`.

The canonical checkout remains `main` at
`299d99c434475b0b83ef17c30fd1450e978a6788`, containing newer intentional v020.8
work. Compatible corrections were applied there without resetting or replacing
that work. A hash audit of 2,416 original files finds 38 authorized modified
paths, no missing original file, no unexpected modification, and unchanged
Git index/version metadata. The two historical finalized release ZIP hashes
remain unchanged. No remote push, merge or new PR was performed.

No unresolved merge markers were found in current source, configuration or
documentation. Generated caches/build outputs remain ignored. Original changed
primary files were preserved in separate local ZIP checkpoints before editing.

## Implemented request

A port has no base trade income. It earns 1 Thor for each **other connected
friendly port city**, including any number of lighthouse relays. Four linked
cities earn 3 each; an isolated port earns 0. Destroyed/foreign cities and broken
relay routes stop contributing. Existing shipyard discount and movement rules
are unchanged. Historical saves retain their embedded fixed-income policy.

New prices: Support Brig 5, Brig 6, Galleon 9, Kolonel 16, Granado 22; radar 4,
Mothership mortar equipment 14. Infrastructure prices remain unchanged.

The preceding requested amphora/Mothership icon, right-edge Info, centered
fold/burn action captions, menu translations, saved pirate option and persistent
merchant voyages through lighthouse chains are also included. Current source
behavior, serialized ship identities and newer primary features were preserved.

## Executed checks

- Correction and primary Debug builds: zero errors/warnings.
- Correction full Release CoreChecks: all suites pass, including 23 historical
  save/validation checks, eight complete AI matches, 204 optional-pirate checks
  and 161 connected-port checks.
- All 24 requested native correction/runtime cases pass. These cover combat,
  exact-once animation effects, saves, menus, input, localization, town/ship UI,
  pirate choices and decorative merchant traffic. Economy UI: 227 checks;
  merchant chains/construction/schedules/RNG: 30,740 checks.
- Primary full Release CoreChecks exits zero, including 23 historical save
  checks, 161 port checks and its retained 187 v020.8 pirate checks. Primary
  native economic UI (221) and merchant traffic (30,740) pass.
- Native amphora reference images before/after grain grouping are
  pixel-identical. This did not lower the recorded renderer draw-call count;
  no frame-speed improvement from grouping alone is claimed.
- Actual exported candidate launches in EN/UK/NL and map-preview mode: exit
  zero, eight native captured frames per case, no engine/runtime errors.
- Exact candidate source ZIP, extracted outside the active project, builds
  with zero warnings/errors and passes the full CoreChecks suite with historical
  fixtures, exit zero.
- Windows re-export from that exact extracted source succeeds; its PCK passes
  inspection. That rebuilt executable also passes EN/UK/NL/map-preview startup.
- Repeated packaging with identical inputs produces identical player/source
  ZIP bytes and SHA256 hashes using the same PowerShell/.NET compressor.

All automated saves/preferences are explicit disposable paths outside the
active project. Real user save slots and valid backups were not used or changed.

## Performance status and limits

Final native frame gate is **not passed**. The first corrected Pangaea pan p95
was 25.320 ms versus the unchanged 25 ms limit. A later repeat had p95 36.858 ms
and one 805.746 ms pan stall. GPU/process inspection found another old player
instance running from `outputs/Ancient Naval v020.7/Ancient Naval.exe`; it was
not terminated. The user was asked to close it before a quiet repeat.

Both failures are retained. The current serial gate stopped at Pangaea;
there is no passed corrected live-fog gate. No thresholds were relaxed and
headless results are not used as evidence of smoothness. Repeat both native
cases after the final Debug build, with the competing player closed; investigate
any persistent failure before producing the final corrected package.

## Packaging and hashes

Player: 190 ZIP entries; PCK: 169 inspected resources. The complete required
.NET runtime is retained. No PDB, development diagnostics, source documentation,
test saves or Preview map command enters the player. The raw export was already
clean, so its excluded-file list is empty; no runtime DLL was removed.

- Windows candidate, 86,780,944 bytes:
  `97dd1ec07f9c9ddc80ceabf2a77ba29b5f05123302b50f13c0a6f181548d2b9d`
- Source candidate, 17,998,590 bytes:
  `538d4f5a94d44b4900d5d91ac88df7e6085e648d240bed94c4946665bfb34d5d`

Canonical delivery folder:
`C:/__Beygelman/! -11/dev-ancient-naval/releases/v020.7/corrected-2026-10-05/`.
Use the explicitly named CANDIDATE ZIPs; `player/Ancient Naval.exe` is the local
unpacked test copy. Keep the PCK and full runtime next to it. Do not extract the
source ZIP inside the active project, where nested C# files can be compiled twice.

## Remaining work

Only final quiet native frame validation and the resulting final corrected
artifact checkpoint remain. Android/device validation and a complete v020.8
finalization were not performed. After v020.7 clears its gate, the next scope is
to reconcile and validate the preserved v020.8 scroll/input work separately;
no additional mechanics or version bump were implemented here.
