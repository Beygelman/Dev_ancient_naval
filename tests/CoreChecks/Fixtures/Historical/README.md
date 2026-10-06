# Historical session fixtures

These are unchanged pre-refactor session saves preserved in the original 0.14.1 diagnostic work. The earlier milestone `docs/MILESTONE-0.14.1.md` records the two real pre-refactor fixtures. Copied on 4 October 2026 from the existing `work/refactor-persistence/baseline-menu-*.json` files; these are controlled diagnostic fixtures, not the player’s current save slot. SHA256.txt records exact bytes.

Use the following from the source root. The suite reads these files without changing them and writes compatibility/recovery trials to its own temporary directories.

```powershell
$env:ANCIENT_NAVAL_LEGACY_FIXTURES = (Resolve-Path tests/CoreChecks/Fixtures/Historical).Path
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

For native Continue trials, copy a fixture to a new disposable `--save-file=` path first. Never pass this fixture path, the player’s real slot, or its backup as a writable test slot.
