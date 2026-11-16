# Ripptopia

Ripptopia is a Unity action/fighting game prototype built around side-view arena combat. The current project includes menu and character-selection flow, player and enemy combat, score-based matches, character upgrade UI, cinematic camera transitions, environmental hazards, and multiple gameplay scenes.

## Technology

- Unity `6000.3.20f1`
- C# / .NET through Unity
- Universal Render Pipeline (URP) `17.3.0`
- Unity Input System `1.19.0`, with some gameplay scripts still using the legacy `Input` API
- Cinemachine `3.1.7`
- Unity Timeline and Playables
- Unity AI Navigation and the bundled A* Pathfinding Project
- TextMesh Pro and Unity UI
- Target presentation: 1920 x 1080

- Photon Quantum: Photon-Quantum-3.0.14-Stable-2241
- Facebook SDK: facebook-unity-sdk-18.1.1

## Current Game Flow

```text
Main Menu
    -> Select game mode
    -> Character Upgrade / Selection
    -> Load the scene assigned to the selected mode
    -> Countdown and match
    -> Win/Lose result
```

`GameSession` persists the selected game mode and character between scenes. `GameFlowSceneLoader` validates scene names before loading them and provides the shared navigation path between the main menu, character-selection screen, and gameplay.

The current scene assignments are:

| Game mode | Scene | Status |
| --- | --- | --- |
| Story | `Ripptopia` | Assigned |
| Brawl | `MapLab` | Assigned |
| Grid Wars Online | Not assigned | Placeholder only |
| Lab | `MapLab` | Assigned |
| Simulation | `MapSimulation` | Assigned |

## System Overview

### Game flow and scene management

- `GameSession` is a persistent singleton that stores the selected mode and character ID.
- `MainMenuManager` records the chosen mode and opens character selection.
- `CharacterSelectManager` validates and confirms a character before loading gameplay.
- `GameFlowSceneLoader` maps modes to scenes and prevents invalid scene loads.
- `SimulationMenuManager` controls pause, resume, move-list display, and return to the main menu.
- The scene utility scripts provide scene reset, direct scene switching, and Brawl/Lab object configuration.

### Player controller

- `PlayerInputController` reads movement, jump, dash, double-tap dash, and buffered attack inputs.
- `PlayerMovement` handles horizontal movement, jumping, variable fall gravity, dashing, facing, knockback, platform/enemy pass-through, falling, score penalties, and respawning.
- `PlayerFightingController` maps attacks to animator triggers and hitboxes, applies cooldown and Ripp-resource costs, plays special-attack timelines, and coordinates movement locks during attacks.
- `PlayerAnimator` synchronizes movement states with the Animator.
- `PlayerCameraController` switches between tracking, train-event, point-zoom, respawn, and end-game camera targets.

Default keyboard controls configured in code:

| Action | Input |
| --- | --- |
| Move | `A` / `D` or Left / Right Arrow |
| Jump | `Space` |
| Dash | Left/Right `Shift` or double-tap a movement direction |
| Light attack | `M` |
| Heavy attack | `,` |
| Punch | `J` |
| Kick | `L` |
| Cyclone | `K` |
| Parry | `P` |
| Special | `B` |
| Down light | `S + M` |
| Special quake | `S + A/D + B` |

Inputs and attack bindings are serialized and can be changed in the Unity Inspector.

### Combat and character state

- `HitBoxController` activates attack colliders, prevents repeated hits on the same target, applies owner rewards, and supports both 2D and 3D overlap checks.
- `HurtBoxCollider` processes damage, hit reactions, knockback, dizzy state, death flight, camera shake, sound, VFX, and temporary control locks.
- `StatsController` owns health and the Ripp combat resource, updates UI bindings, and restores stats on respawn.
- `ScoreController` stores and displays each fighter's score.
- Player- and enemy-specific hurtbox classes extend the shared combat behavior with their own respawn handling.
- `ComboManager` tracks consecutive hits, updates combo UI, shakes the counter, and plays milestone sounds.
- `CombatInfoDisplay` shows damage and combo statistics for combat feedback or training-style UI.

### Enemy AI

- `EnemyMovement` follows the player with a `NavMeshAgent`, maintains a stopping distance, faces the target, traverses off-mesh jump links, and supports movement locks and knockback.
- `EnemyAnimator` manages locomotion and selects attacks when the player is in range. Enemy attacks can consume Ripp and activate hitboxes through animation events.
- Enemy head and hurtbox triggers handle collision separation, player pass-through behavior, reactions, and respawn recovery.

### Match lifecycle

- `MatchManager` controls the pre-match lock, match start, countdown timer, configured gameplay components, and end-of-match shutdown.
- When time expires, the player and enemy scores are compared and the Win or Lose UI is displayed. Ties currently favor the player by default.
- `EndBoardSequencer`, `TimeoutMatchResultUI`, and `UiFunctionEndGame` coordinate result presentation and end-game UI behavior.
- `MyPoolManager` and `SimplePool` reuse VFX and temporary objects to avoid repeated instantiate/destroy operations.

### Character selection and upgrade UI

- The character grid supports keyboard and mouse navigation, locked/unlocked slots, hover feedback, and final selection.
- Current character identifiers are Seinna, RX9, Xandros, Kaitana, and Ion.
- The serum UI offers Pink, Blue, Red, Green, and Purple serum types.
- `SerumInjectorManager`, `SerumSlot`, and `SerumPlatformManager` preview serums, insert them into available slots, remove them, and compact the remaining slot data.
- Hover, glow, scale, toggle, cursor, and scrolling components provide reusable UI feedback.

### Camera, environment, audio, and presentation

- Cinemachine camera switching supports combat tracking and scripted event views.
- `GameEvent` repeatedly runs the train Timeline, moves the camera to the train, and restores the correct view after the event.
- `TrainCollider` connects train impacts to fatal-hit and camera behavior.
- Environment scripts control light flicker, emission changes, shadows, and special scene objects.
- `PersistentMusicManager` keeps background music alive across scene changes.
- `SoundController` and `SoundCombo` play named combat and combo audio.
- Custom URP renderer and shader code supports glow, decals, shadows, and selective post-processing.

## Project Structure

```text
Assets/
|-- Main/
|   |-- Scripts/
|   |   |-- ControllerBothEnemyPlayer/  Shared combat, stats, score, and sound
|   |   |-- Enemy/                      Enemy navigation, animation, and collisions
|   |   |-- GameFlow/                   Menu, session, mode, and scene flow
|   |   |-- MatchManagerScripts/        Match timer, results, events, and pooling
|   |   |-- Object/                     Environment and train behavior
|   |   |-- Player/                     Input, movement, combat, animation, and camera
|   |   `-- UI/                         Combat information and UI audio
|   |-- NewScripts/                     Character UI, serum UI, cursors, scene helpers,
|   |                                    rendering features, utilities, and VFX helpers
|   |-- Animations/                      Character and gameplay animations
|   |-- Prefab/                          Gameplay prefabs
|   |-- Timeline/                        Cinematic and special-event timelines
|   |-- UI/                              Menus and HUD assets
|   `-- SHADERS/                         Project-specific shaders
|-- Scenes/                              Main menu, selection, and gameplay scenes
|-- AstarPathfindingProject/             Third-party pathfinding package
|-- GG Camera Shake/                     Third-party camera-shake package
BackendDev/                              Full backend (Fastify + Prisma + PostgreSQL)
DemoBackend/                             Database-free backend for HTTPS login demos
Packages/                                Unity package declarations
ProjectSettings/                         Unity project and build configuration
```

## Scenes in the Build

1. `MainMenuScene`
2. `CharacterUpgradeScene`
3. `MapLab`
4. `MapSimulation`
5. `Ripptopia`

## Running the Project

1. Install Unity Hub and Unity Editor `6000.3.20f1`.
2. Add this repository as an existing Unity project.
3. Allow Unity to restore packages and import assets.
4. Open `Assets/Scenes/MainMenuScene.unity`.
5. Enter Play Mode.

For a standalone build, keep all five scenes enabled in the order shown above and use the Windows build profile under `Assets/Settings/Build Profiles`.

## Current Development Notes

- `GridWarsOnline` exists as a game-mode option but has no scene or networking implementation assigned yet.
- `BackendDev` currently contains only a placeholder file; there is no backend service in this repository.
- No project-owned automated tests are currently present.
- Most gameplay configuration is serialized in scenes and prefabs, so code changes may also require Inspector updates.
- `Assets/_Recovery` contains Unity recovery scenes and is not part of the normal game flow.
"# FightingOnline" 
