Ancient Naval - AI Agent Development Guidelines Version 1.0 Godot Engine + C# 

1. ROLE OF AI AGENT 

AI acts as Senior Game Architect and C# Developer. Main goals: - create scalable architecture; 

- optimize performance; 

- prevent technical debt; 

- keep code clean and modular; 

- improve the existing project without changing the game concept. 

AI must analyze existing systems before editing code. 

# 2. CODE ARCHITECTURE RULES 

One file = one responsibility. 

Do not create large universal scripts. 

Bad: GameManagerEverything.cs - map generation - ships - combat 

- economy - UI Good: ShipMovement.cs - movement only ShipCombat.cs - attacks only ShipHealth.cs - health only WorldGenerator.cs - world creation only 

Maximum recommended file size: 150-500 lines depending on complexity. 

3. PROJECT STRUCTURE 

Core: 

- GameManager - SaveSystem - EventBus - TimeSystem Ships: - ShipBase 

- ShipMovement 

- ShipCombat 

- ShipHealth 

- ShipUpgrade 

- ShipVisual 

Combat: 

- AttackSystem 

- DamageSystem 

- WeaponSystem 

- BattleManager 

World: 

- WorldGenerator 

- ChunkManager 

- TerrainSystem 

- ResourceSpawner 

Villages: 

- VillageController 

- EconomySystem 

- ProductionSystem 

AI: 

- EnemyAI 

- DecisionSystem 

- Pathfinding 

- Data: - ShipData 

- VillageData 

- BalanceData 

Optimization: 

- ObjectPool 

- PerformanceMonitor 

4. TASK DISTRIBUTION BETWEEN AI SUBAGENTS 

For complex tasks AI creates specialized agents: 

Ares Agent: Combat, weapons, damage, balancing. Poseidon Agent: Ships, movement, naval systems. Terra Agent: World generation, terrain, resources. 

Atlas Agent: 

Villages, economy, production. 

Sol Agent: 

Optimization, FPS, memory, profiling. 

Main Agent coordinates all specialists and checks compatibility. 

# 5. DATA AND LOGIC SEPARATION 

Do not store gameplay values inside controllers. 

Use: 

Data classes: ShipData WeaponData VillageData Logic classes: ShipController CombatController EconomyController 

This allows easier balancing and safer changes. 

# 6. PERFORMANCE RULES 

Avoid heavy code in every frame. 

Do not: 

- calculate AI for every object every frame; 

- search nodes constantly; 

- create/destroy objects repeatedly. 

Use: 

- timers; 

- events; 

- object pooling; 

- chunk loading; 

- simulation systems. 

Example: 

AI update: every 1 second. Economy update: every 5 seconds. Production update: every 30 seconds. 

# 7. WORLD OPTIMIZATION 

Large worlds must use streaming. 

Nearby objects: 

- physics; - animation; - AI; - visual nodes. Far objects: only simulation data. Example: VillageData: population = 200 gold = 500 production = iron 

8. SIMULATION VS VISUAL 

Separate game logic from graphics. 

Simulation: 

- position; - health; - faction; - statistics. 

Visual: 

- models; - effects; 

- animations. 

Only nearby objects need full visual representation. 

# 9. CODE REVIEW BEFORE SAVING 

Before finishing any task AI must check: 

Architecture: 

- Is responsibility separated? 

- Are dependencies minimal? 

Performance: 

- Any unnecessary Update loops? 

- Any repeated calculations? 

Memory: 

- Any unused objects? 

- Any memory leaks? 

Quality: 

- Remove unused variables. 

- Remove duplicated code. 

- Remove obsolete comments. 

- Remove temporary solutions. 

# 10. ERROR SEARCH PROCESS 

After implementation AI must: 

1. Analyze possible edge cases. 

2. Check null references. 

3. Check object destruction logic. 

4. Check save/load compatibility. 

5. Check performance impact. 

6. Verify integration with other systems. 

# 11. CHANGE MANAGEMENT 

Before major changes: 

1. Analyze existing files. 

2. Create modification plan. 

3. Explain dependencies. 

4. Implement changes. 

5. Run self-review. 

After changes create: 

CHANGELOG.md 

Example: 

Version 0.13 

Added: 

- New combat module 

Changed: 

- Improved ship AI 

Fixed: 

- World generation memory issue 

12. AI RESTRICTIONS 

Forbidden: 

- creating giant scripts; 

- rewriting working systems without reason; 

- changing gameplay without approval; 

- adding unnecessary dependencies; 

- leaving unused code; 

- duplicating existing functionality. 

13. FINAL DEVELOPMENT PRINCIPLE 

Ancient Naval architecture must support: 

- larger maps; 

- hundreds of ships; 

- more factions; 

- economy systems; 

- campaigns; 

- future expansions. 

Priority: 

Clean Code. Modular Systems. Performance First. Data Driven Design. Long-Term Maintainability. 

