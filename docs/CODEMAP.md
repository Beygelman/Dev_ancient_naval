# Focused code map
Use rg for relevant symbols, then read their implementation and focused tests.
Paths below are relative to the named directory.

| Change | Start here |
|---|---|
| Player commands/input gate | src/Presentation: MainCommands.cs, Main.cs, MainTileSelection.cs |
| Opponent replay | src/Presentation/MainNavigation.cs |
| Attack sequencing/projectiles | src/Presentation/Map: FleetCommands.cs, FleetAnimation.cs |
| Sinking/crash/health | src/Presentation/Map: FleetSinking.cs, BalloonCrashAnimation.cs, AmphoraHealthAnimation.cs |
| Camera | src/Presentation: Main.cs, Camera/MapCamera.cs, Map/FleetAnimation.cs |
| Save/world replacement | src/Presentation: MainSaving.cs, MainSession.cs, SaveStore.cs |
| Exact-once staging | src/Core/Battle: PresentedCommand.cs, BattleSnapshot.cs |
| Combat/forecast/replies | src/Core/Battle: BattleCombat.cs, VillageCombat.cs, MortarSplash.cs |
| Ports/income/topology | src/Core/Battle: Ports.cs, PortRules.cs; src/Core/Navigation/TradeNetwork.cs |
| City facilities / service / AI | src/Core/Battle/CityInfrastructure.cs; src/Core/AI/CityInfrastructureInvestment.cs; src/Presentation/MainCityInfrastructure.cs; src/Presentation/UI/CityInfrastructureHud.cs |
| City/capacity | src/Core/Battle: Village.cs, VillageProgression.cs, FleetCapacity.cs |
| Rules/defaults/AOT | src/Core/Battle: BattleRules.cs, BattleRulesJsonConverter.cs, BattleJsonContext.cs; data/balance.json |
| Save DTO/validation | src/Core/Battle: BattleSave.cs, BattleSaveValidation.cs |
| City UI/localization | src/Presentation: MainVillageUpgrades.cs, UI/VillageHud.cs, UI/Language.cs, UI/LocalizedMessages.cs |
| AI investment | src/Core/AI: FleetInvestment.cs, VillageDevelopment.cs |
| Regression suites | tests/CoreChecks/Program.cs and focused files; tests/Runtime |
| Local Windows install | tools/Install-PlayableRelease.ps1; releases/v020.9x1/Install_Ancient_Naval_v0209x1.cmd |
| Version/release | project.godot, export_presets.cfg, README.md, CHANGELOG.md, tools, .github/workflows |

This is an index, not a requirement to read every file.

