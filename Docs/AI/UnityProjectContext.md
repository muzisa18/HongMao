# HongMao Unity Project Context

- Unity: 6000.3.23f1
- Render pipeline: Universal Render Pipeline 17.3.0, 2D Renderer
- Input: Input System 1.20.0; keyboard and mouse prototype controls
- AI integration: Unity AI Assistant 2.19.0-pre.2; official Unity MCP relay
- Project root: repository root (`HongMao`)
- Runtime root: `Assets/HongMao/Runtime`
- Prototype scene: `Assets/HongMao/Scenes/PrototypeCombat.unity`
- Assemblies: `HongMao.Runtime`, `HongMao.Editor`, `HongMao.Tests.EditMode`, `HongMao.Tests.PlayMode`
- Scene composition: `PrototypeInstaller` wires serialized scene references; the player, enemy, geometry and color sources are authored objects that remain visible outside Play Mode. Combat systems use explicit references and no global singleton.
- State authority: `CombatResourceModel` owns charge, color slots, empowered attack, and mask duration. `CombatantBody` owns health, poise, invulnerability, and parry resolution. `CombatTimeController` alone writes `Time.timeScale`.
- Verification: compile in Unity, inspect Console through Unity MCP, run EditMode/PlayMode tests, then perform the documented manual route in Play Mode.

## Editable prototype assets

- Player prefab: `Assets/HongMao/Prefabs/PlayerPrototype.prefab`
- Enemy prefab: `Assets/HongMao/Prefabs/CombatBotPrototype.prefab`
- Red source prefab: `Assets/HongMao/Prefabs/RedColorSource.prefab`
- Player balance: `Assets/HongMao/Configs/PlayerConfig.asset`
- Enemy balance: `Assets/HongMao/Configs/CombatBotConfig.asset`
- Attack balance: `Assets/HongMao/Resources/Attacks`
- Scene hierarchy: `World/Geometry`, `World/Decoration`, `World/Color Sources`, and `World/Actors`
- Debug gizmos: select `Combat Systems` and edit `Combat Debug Overlay`, including the attack asset shown as the yellow preview box.
- Scene template command: `HongMao > 重建可编辑战斗测试场景`. This intentionally replaces the current prototype-room layout with the template, so ordinary layout work should be saved directly instead.

## Prototype controls

- A/D: move
- Space: jump
- Shift: dodge
- Left mouse: attack/confirm absorb
- Right mouse: parry/cancel absorb
- Q: enter or cancel absorb
- R: activate red mask
- W + left mouse: empowered uppercut
- Airborne S + left mouse: empowered dive slam
- Enter: restart after victory or defeat

## Expanded battle test room (2026-10-17)

- `Assets/HongMao/Scenes/BattleTestRoom.unity`: separate 64-unit room with 18 one-way platforms and tutorial environment artwork; original prototype retained.
- `CombatRoomCamera` follows the player on a parent rig; `PrototypeFeedback` applies shake in camera-local space.
- Authoring: `CombatRoomAuthoring` builds from the existing scene without regenerating actor prefabs; refuses to overwrite an existing new room.
- Local Unity 6000.3.23f1 batch tooling available; no callable Unity MCP exposed in this session.
- Layout and manual test route: `Docs/AI/BattleTestRoomGuide.md`.

## Placeholder NPC dialogue (2026-10-25)

- `BattleTestRoom` now includes `World/Actors/TutorialGuide` on East Step 7 and `Dialogue Systems` with explicit serialized references.
- `DialogueDefinition` stores speaker name and ordered pages; edit `Configs/TutorialDialogue.asset` for the nine-page beginner guide. No story branching, quests or persistence yet.
- `DialogueController` owns the session; `DialogueView` uses the existing IMGUI/CJK font approach. `DialogueNpc` owns the non-colliding tutorial robot visual and interaction range.
- Input order: `PlayerInputReader` (-200), dialogue (-100), combat/movement (0). E starts/advances, Esc closes; gameplay buttons are consumed while reading and on the closing frame.
- `CombatTimeController` remains the sole runtime owner of time scale and now combines dialogue pause with hit-stop/absorb. Dialogue never re-enables combat after victory/defeat.
- Setup, editing and smoke route: `Docs/AI/DialogueGuide.md`. PlayMode coverage: `DialogueTests`.
- Repository baseline inspected: `26ee22e`; Unity 6000.3.23f1 batch tooling available. No callable Unity MCP in this session. Runtime/test assemblies otherwise unchanged, except PlayMode tests also reference the existing Input System assembly for simulated keyboard/mouse input.
