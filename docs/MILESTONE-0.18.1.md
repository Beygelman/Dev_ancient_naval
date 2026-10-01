# 0.18.1 — Readable Charts

Delivered 1 October 2026. This follow-up retains the gameplay, prices, ports,
trade movement, difficulty policies, visibility and rendering improvements of
[0.18](MILESTONE-0.18.md). Core/data files are unchanged against the pre-edit
snapshot. The authoritative source and release copies remain in
`C:/__Beygelman/! -11/dev-ancient-naval`.

## Information cards

Each object opens with one short keeper-style description. Named sections then
show current characteristics, weapons, crew/economy and installed improvements
as separate label/value rows with dividers. Shipyards list currently available
classes, without a future unlock tree. Unbuilt ports and outposts do not appear
in the improvements section. Existing mortar/radar, earned veterancy and chosen
Mother rewards do, with their numerical effects.

Cards use a fixed title and close button above a scrollable body. A short
resource card fits without scrolling; long equipped ships/cities remain within
the viewport and scroll. Unchanged refreshes retain reading position. A town's
own name appears as its title. The rightmost Info sectors and right-click
cancellation continue to work.

Radar contacts in uncharted water previously failed the explored-terrain gate.
The gate now accepts an anonymous radar contact too. Its card reveals neither
identity nor HP and does not discover that terrain. Unknown uncontacted water
still cannot be inspected.

Numbers come from the active voyage's rules and current state, including old
saved prices/repair values and port discounts. Neutral or defeated towns report
zero income. All text remains English; the requested v0.13 title signature is
unchanged.

## Verification and limits

- Clean .NET build: no warnings or errors.
- Windows graphical menu/new-game/save/recovery checks: **141 passed**; fresh
  process Continue checks: **120 passed**. These include all 11 ship classes,
  one-sentence descriptions, installed-vs-unchosen improvements, a veteran tower,
  base town and port city, custom saved numeric rules, actual Info/right-click
  input, scrolling and anonymous radar in unknown water. Test saves are explicit
  disposable files; the user's save is untouched.
- **12 city/port/scroll checks** confirm foreground town UI, rightmost Info,
  retained overlays while panning and refreshed town HP after repair.
- PNGs of base/equipped Mother, installed improvements, city and resources were
  inspected for line wrapping, spacing, clipping and fixed title placement.
- The Windows export is separately launch-tested and copies are SHA256-verified.
  Previous source/Windows archives are preserved; release ZIPs remain Git-visible
  while the oversized unpacked launch folder remains local-only.

Largest map / four rivals, normal fog, seed 731, Windows D3D12 Mobile:
120 pan frames and 64 cursor previews followed by actual Brig sailing. Pan p50
**16.716 ms**, p95 **17.006 ms**, max **18.185 ms**; hover handler p95
**0.136 ms**; hover/movement frame p95 **17.721 ms**, max **106.149 ms**.
There are still occasional long frames. This is a regression measurement of the
0.18 rendering fixes, not a promise of perfectly constant FPS. The animation
itself lasts about 1.57 seconds by design. No new per-frame card rebuilds were
introduced: content is rebuilt only when its visible facts change.

[Raw performance](diagnostics/0.18.1/performance4.txt),
[menu checks](diagnostics/0.18.1/release-menu.log),
[Continue checks](diagnostics/0.18.1/release-resume.log),
[city checks](diagnostics/0.18.1/release-ports.log).

New game uses the 0.18 numeric balance. Continue preserves the previous voyage's
saved rules, while applying the new information layout.
