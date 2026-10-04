# v020.5 verification — 3 October 2026

Debug and isolated Release Core builds: zero warnings and zero errors. The full Core run passed 1,203,842 assertions across sixteen reported suites, including eight complete AI matches and saved historical-rule fixtures. The new v020.5 suite covers 89 assertions: three-HP repair/action locks, zero-income Support Brig construction, dock income, town prices, six/seven-transition route boundaries, independent persisted map area and deterministic Lake/four-rival worlds. Twelve sampled small-world combinations retain fifteen separated towns and complete fleets; logged generation was 22–819 ms.

Native OpenGL Compatibility functional checks (real renderer; mouse/wheel input used for setup, blueprint and parchment actions):

| Suite | Assertions |
| --- | ---: |
| ui0205 | 78 |
| construction0205 | 40 |
| world-visual0205 | 60 |
| world-visual0204 | 30 |
| ui0204 | 142 |
| language0202 | 1223 |
| trade-glyph0204 | 18 |
| menu | 150 |
| ui0203 | 65 |
| refinement021 | 172 |
| victory | 110 |

The setup check exercises all three languages at both UI-scale endpoints, actual selection/scrolling, the complete handprint/fold/black-fade/camera/welcome sequence and Continue. World checks validate river joins and noncrossing rules, obstacle masks, beach/hole clipping, connected rounded gradient estuaries, medium town fit and retained shore-cache reuse/new-world invalidation. Native screenshots were visually inspected; the disconnected sand gap and self-crossing mouth geometry were repaired before final timing.

Timing gates ran serially on the final Debug assembly, on the largest Ocean area with four rivals. No Core tests, builds or other task-owned CPU-heavy jobs ran concurrently. Host: Windows, NVIDIA RTX 4060 Laptop GPU, OpenGL Compatibility. Limits remain pan p95 <=25 ms/max <=80 ms, interaction p95 <=25 ms/max <=100 ms, hover handler max <=16 ms.

### performance-pangaea

```text
PAN frames=119, p50_ms=16.655, p95_ms=17.328, max_ms=41.576
INTERACTION world=Pangaea, wide_view=True, tiles=1759, opponents=4, live_fog=False, hover_calls=64, hover_p50_ms=0.045, hover_p95_ms=0.118, hover_max_ms=0.632, frame_p95_ms=17.151, frame_max_ms=45.791, process_p95_ms=41.516, draw_calls_p95=3181, movement_elapsed_ms=1569.965
```

### performance-live-fog

```text
PAN frames=119, p50_ms=16.675, p95_ms=17.366, max_ms=35.163
INTERACTION world=Oceans, wide_view=False, tiles=1759, opponents=4, live_fog=True, hover_calls=64, hover_p50_ms=0.041, hover_p95_ms=0.090, hover_max_ms=0.627, frame_p95_ms=18.098, frame_max_ms=68.998, process_p95_ms=67.573, draw_calls_p95=877, movement_elapsed_ms=1600.765
```

An initial largest-Pangaea run measured a 102.512 ms interaction frame with repeated shore-mask work. Profiling attributed 57.600 ms to Ambience.Refresh. Retaining geometry by world removes that repeated clipping; final reports above supersede that failed pre-fix attempt. Local measurements do not guarantee every seed, computer, driver or background workload.

Windows export carries product version 0.20.5. EN/UK/NL title startup and map-preview startup were rendered and captured with disposable save/preferences paths; all exited successfully without error reports. The complete PCK and .NET runtime are included. Export excludes unused sound-source collections, repository release archives, tests and diagnostics resources.

The source ZIP retains every previous source/test/data/asset entry. Selected new PNG previews and all text evidence are included; full new capture sequences remain Git-visible in the repository. ZIP integrity, prior-release SHA256 preservation and complete release-mirror copy checks are performed by the packaging/sync tools. Disposable saves, build caches and credentials are excluded.

Committed conflict-marker originals were archived before recovery. The recovered readable user roadmap stays byte-for-byte unchanged. Publication uses a virtual index with the user's existing local commit as parent, leaving the user's HEAD and real index intact.
