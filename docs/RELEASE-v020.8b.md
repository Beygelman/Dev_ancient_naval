# Ancient Naval v020.8b — portable delivery, 7 October 2026

Game source: `bc28085f986c2e8022ae02c3af9fd6f307473718` on `main`.
The accompanying delivery commit adds the verified packages and stable Godot
import UIDs; gameplay and compiled presentation are unchanged.

## Play

Extract the complete `Ancient_Naval_v020.8b_Windows.zip` folder and start
`Ancient Naval v020.8b/Ancient Naval.exe`. Preserve the PCK and complete runtime
directory beside it. No editor or separate .NET installation is required.

Canonical local launch:
`C:/__Beygelman/! -11/dev-ancient-naval/releases/v020.8b/playable/Ancient Naval v020.8b/Ancient Naval.exe`.
The Windows/source ZIPs and SHA256 manifest are also copied to the conversation's
`C:/Users/User/Documents/Codex/2026-09-29/x20/outputs` directory.

Touch the upper nation relic to end your turn; touch its inset count to visit
the next object that can act. Hints-on confirmation still lists remaining
actions. Scrolls own their mouse wheel. Continue preserves historical voyage
geometry, RNG and embedded rules: new Granado movement/pirate policy apply to
new voyages, not retroactively to existing ones.

## Verification

- Debug build: zero warnings/errors; full Core suite and historical saves pass.
- Native advice/input/nation/ship-icon suite: 194 checks. Navigation 229,
  guidance 47; retained combat, tutorial, merchants, menus, sea and world pass.
- Serial release pacing on RTX 4060 Laptop / Godot 4.7.2 Compatibility:
  Pangaea frame p95 17.380 ms; live fog 17.987 ms. Unchanged 25 ms p95 gates pass.
- Raw export and ZIP-extracted player: EN/UK/NL and map preview each render
  eight native frames and exit 0 without engine errors.
- Clean player: 190 files, including complete required .NET runtime; 198 PCK
  resources. No PDBs, development docs/commands, transient saves or nested clone.
- Source ZIP: 1,171 entries, including selected test/performance evidence and
  per-file inventory. The three generated script UIDs were explicitly reviewed.

SHA256:

```text
e0e6bdcd205570bed528656785c20ac0ebaadef674ab0bf87af1cd79b48e2268  Ancient_Naval_v020.8b_Windows.zip
30fe01804a4fba99733db71c305ee749bcd769063547a94632167dcfa4f77c55  Ancient_Naval_v020.8b_Source.zip
```

All historical archives are untouched. Native pacing is measured on this host,
not Android or every GPU. See `MILESTONE-v020.8b.md` for the inherited
unseen-whirlpool path-estimation limitation and the exact verification scope.
