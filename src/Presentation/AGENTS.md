# Presentation contract
Read the root guide and relevant docs/CODEMAP.md row.
- Main composes commands/session/views. No alternate UI gameplay rules.
- Own actions never auto-focus. Only observable opponent replay may request action
  focus. Manual navigation and initial world fitting remain separate.
- Player attacks/sinking allow the next legal order; safely settle remaining
  cosmetics when necessary. Never apply Core effects twice. Ordered opponent replay.
- Capture original battle/lifecycle around awaits. World replacement resolves
  pending waits; stale continuations cannot save, refresh or mutate a new world.
  Save only stable simulation state.
- Keep fog/radar anonymity in wrecks/projectiles/HP/labels. God's eye is display-only.
- Retain static art; no terrain rebuild on hover/pan. Cache visibility correctly,
  bound cosmetic populations and stop hidden animation processing.
- Native modal wheel/hit regions/touch ownership remain authoritative. Preserve
  portrait dock, landscape anchors, safe-area transforms and gesture cancellation.
- New visible strings need EN/UK/NL. Preserve physical papyrus/ink/clay art,
  fixed-size clipped reveals and safe interruption of asynchronous papers.
- Preserve iOS net8.0 NativeAOT/generated JSON/compile exclusions and script paths.
- Build/headless does not prove pixels or input. Use existing tests/Runtime with
  disposable save/capture paths; inspect captures and report unavailable checks.


Native input fixtures must release every synthetic mouse/wheel button. An unreleased
wheel keeps GUI mouse focus captured and invalidates later clicks. Wait for radial
paper reveal/processing to finish; exercise native hit regions instead of signals.
