# PINK APOCALYPSE Sprint 01

This Unity 6 project contains the first playable foundation for a browser focused third person skateboard prototype. Sprint 01 uses the existing URP setup and the installed Unity Input System package. The project targets a Web build profile; final Web compatibility and browser performance still need to be checked in Unity.

## Open the project

Open this folder in Unity Hub with **Unity 6000.6.10f1**. Open `Assets/Scenes/SampleScene.unity` and press Play. The scene is the Sprint 01 prototype and includes a simple test lane, obstacles, a directional light, a spawn point, a primitive player placeholder, a skateboard and a third person camera.

## Controls

- **WASD** or arrow keys: move
- **Shift**: run while walking
- **Space**: jump
- **E**: mount or dismount the skateboard
- **Mouse**: orbit the camera after clicking the game view
- **Escape**: release the mouse cursor
- While riding, **W** accelerates, **S** brakes, and **A/D** steer
- **1 / 2**: previous / next soundtrack track

### Gamepad

- **Left stick**: move; while riding, push up to accelerate and down to brake
- **Left stick press (L3)**: run while walking
- **Right stick**: orbit the camera without locking the mouse cursor
- **South face button**: jump (A on Xbox-style controllers, Cross on PlayStation-style controllers)
- **North face button**: mount or dismount the skateboard (Y on Xbox-style controllers, Triangle on PlayStation-style controllers)
- **D-pad left / right**: previous / next soundtrack track

The playlist contains the MP3 files from `AUDIORU`, copied into `Assets/Audio/Soundtrack` for Unity import. Tracks play in filename order, advance automatically, and wrap from the last song back to the first. Web builds wait for a mouse click or keyboard press before starting music because browsers block audio autoplay; after that, the gamepad can control playback.

## Project structure

- `Assets/Art`, `Audio`, `Materials`, `Prefabs`: prototype content locations
- `Assets/Scenes`: Unity scenes
- `Assets/Scripts/Player`: input adapter and CharacterController movement
- `Assets/Scripts/Camera`: smoothed third person camera and collision avoidance
- `Assets/Scripts/Skateboard`: mount state and board visual transition
- `Assets/Scripts/Interaction`, `Managers`: reserved for later sprint systems
- `Assets/Settings`: URP assets and Web build profile

The existing `Assets/InputSystem_Actions.inputactions` asset is used directly. Its Player map already provides Move, Look, Jump, Sprint and Interact bindings, so no package or input asset replacement was needed.

## Web build

In Unity, select the existing Web build profile under **File > Build Profiles**, confirm `Assets/Scenes/SampleScene.unity` is included in that profile's scene list, then build. Keep the existing URP asset assigned and validate the result in a desktop browser. The existing profile currently has an empty explicit scene list, so confirm/add the prototype scene before building.

## Sprint 01 implementation report

Implemented the third person player motor, camera, skateboard mode, pink primitive placeholder visuals, test environment and this README. The motor uses one `CharacterController` in both movement modes; skateboard handling is arcade style and intentionally avoids Rigidbody/CharacterController interaction. The `Pink Apocalypse/Generate Sprint 01 Prototype` editor menu can regenerate the prototype scene.

Unity 6000.6.10f1, URP 17.6.0 and Input System 1.20.1 were detected in the project. Unity batch mode generated the scene and imported the scripts successfully; the command exited with code 0 and the log contained no C# compilation errors. Play Mode behavior, a Web build and browser compatibility have not yet been verified. Check mouse lock behavior, camera collision near obstacles, ramp traversal, and the empty Web profile scene list during manual validation.
