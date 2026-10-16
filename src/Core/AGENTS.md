# Core contract
Read the root guide and only the relevant source sections.
- Pure .NET; no Godot, scene paths, UI state or disk adapters.
- BattleState owns state through commands. Cohesive partials separate rule areas;
  independent algorithms belong in separate classes.
- Damage, replies, rewards, crashes and fleet collapse apply exactly once.
  Counterattacks never recurse; animation timing cannot change outcomes.
- Prepare/PresentedCommand snapshot restoration invalidates derived caches.
- Typed rule fields belong in data, embedded save snapshots and validation.
  Missing fields preserve historical behavior; explicit false/zero remain meaningful.
  Keep BattleRulesJsonConverter in both source-generated/AOT JSON contexts.
- Persist city state explicitly and validate enums, duplicates, slots and levels.
  Never reroll Continue or rename serialized identities for prettier labels.
- AI uses observed information and the same legality/price/forecast functions.
  Radar cannot reveal hull identity, HP or optical-only rewards.
- Trade topology invalidates on construction/capture/defeat/hazards/restoration.
  Derive current income rather than storing it into old fixed-income sources.
- Preview, movement and AI share navigation policy; no stale mutable query caching.
- Preserve weighted capacity, actual-price refunds and historical fixtures.
- Run relevant CoreChecks; when defaults/saves change include
  ANCIENT_NAVAL_LEGACY_FIXTURES=tests/CoreChecks/Fixtures/Historical.
