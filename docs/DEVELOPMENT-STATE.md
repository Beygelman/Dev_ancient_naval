# Development state — 17 October 2026

Version: v020.9x2-r1, experimental. Branch: codex/ancient-naval-v020.9x2.
Implementation is based on v020.9x1 permanent release c76314e and the reviewed
specialist design in docs/design/BALANCE-REDESIGN-2026-10-17.md.

## Current changes
- New-voyage flags ExperimentalEconomy, ExperimentalCombat, StrategicAi default
  false when absent in embedded historical rules.
- Defendable port export income, stationary naval interception and real rerouting.
- Separate hull roles and movement/fire constraints, unchanged Mothership.
- Two-work city projects, competing city actions, separate economic/military gains.
- Persistent strategic AI styles/goals and capture-priority/captor-retention fixes.
- Localized UI, real native screenshots and focused new regressions.

## Validation and delivery
The CI release pipeline gates retention on the full Core/historical suite, native
graphical checks and exported Windows EXE startup. The retained release records
its exact tested source in releases/v020.9x2-r1/VALIDATION.json. Do not infer success
from this development note: inspect that evidence and the actual workflow result.
Old regressions use immutable balance-v0209x1.json; new checks use balance.json.
Old release archives and checksums are preserved; main is not overwritten.

The Windows launcher prepares the player at
C:/__Beygelman/! -11/dev-ancient-naval/releases/v020.9x2-r1/playable/Ancient Naval.exe
when executed on the user's PC. This cloud checkout cannot install to their C:.

## Practical limits
Automated matches and scenario screenshots establish execution/invariants, not
competitive balance or a human Windows playthrough. Physical Android/iPhone and
final native frame-pacing gates remain separate. Preserve fog, save/AOT contracts,
nonblocking own effects and opponent-only camera focus in future changes.

Revision r1 fixes simultaneous trade cuts: every confirmed hostile interception zone participates in the operational path search, including a second ship on a bypass absent from structural route ink. The initial v020.9x2 tag and archives are retained.
