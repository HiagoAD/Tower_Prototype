# Tower Prototype — God Tower reproduction

A Unity reproduction of the *God Tower* reference: a blocky climber scales a stone tower in the sky while viewers help or hinder with webhook-triggered boxing-glove bumps. Five sequential levels, one gameplay scene, Android APK.

## Engine and build

- **Unity 6000.3.11f1** (Unity 6.3 LTS), Universal Render Pipeline 17.3.0, Input System 1.19.0.
- Target: Android, minimum API 25, arm64, application id `com.towerprototype.game`, portrait orientation.
- Open the project folder in that editor version. The gameplay scene is `Assets/Game/Scenes/Level1.unity`; all five levels run in it and load their data from `Assets/Game/Levels/Level1.json` … `Level5.json`.
- Build the delivery APK from **Tower → Build Android APK (Release)** (output `Builds/Android/TowerPrototype-release.apk`). **Tower → Build Android APK** makes a development build with script debugging. Batch mode:

  ```sh
  Unity -batchmode -quit -projectPath . -buildTarget Android \
    -executeMethod Game.Editor.BuildScript.BuildAndroidRelease
  ```

- Tests: EditMode and PlayMode suites in `Assets/Game/Tests` (Window → General → Test Runner).

## How to play

- **Hold anywhere** on the screen to climb; **release** to grip the tower and stop.
- Red rings are active hazards; wait for them to turn green, then climb through. A hazard hit knocks you down and costs one of three lives. At zero lives, GAME OVER offers **Continue** (retry the level) or **Exit**.
- Reach the summit to win. **Next Level** advances; after level 5 the game shows TOWER CLEARED.
- The **II** button pauses (Continue / Exit). Leaving the app pauses it; returning keeps it paused until you press Continue.

| Level | Name | Height | Hazards |
| --- | --- | --- | --- |
| 1 | First Ascent | 30 | 2 |
| 2 | Higher Climb | 40 | 3 |
| 3 | Rhythm | 50 | 5 |
| 4 | Pressure | 60 | 6 |
| 5 | Summit | 70 | 8 |

Difficulty rises through taller towers, more hazards and shorter safe windows.

## Webhook: `/bump`

While the app is running, it listens on **`localhost:56789`** and accepts **GET or POST `/bump`**. During a level, a request triggers a full-screen boxing-glove burst: several gloves fly in and strike the climber, with screen shake, a zoom pulse, an impact flash, bloom and chromatic-aberration pulses, and punch sound effects. After the burst, play continues normally.

```sh
curl -X POST http://localhost:56789/bump
```

A bare request is a **negative** bump: the gloves knock the climber down 1.75 body heights. Bumps never cost a life. You can also send a **positive** bump, which lifts the climber 2 body heights, and an optional sender tag shown on the on-screen card:

```sh
curl "http://localhost:56789/bump?polarity=positive&tag=Viewer42"
curl -X POST http://localhost:56789/bump -H "Content-Type: application/json" \
  -d '{"polarity":"negative","type":"boxing","tag":"Viewer42"}'
```

Responses: `200 {"requestId":"…","accepted":true,"polarity":"negative","type":"boxing"}` when accepted; `409 {"error":"not_playing"}` on the menu, while paused or during a win/loss screen; `400` for an invalid polarity, type or body; `429` when the queue is full. The full contract is in [Docs/Development/BUMP_EVENTS.md](Docs/Development/BUMP_EVENTS.md).

### Triggering it on a phone or emulator (port forwarding)

The listener runs **on the device**, so a `curl` on the PC must be forwarded to the device:

```sh
adb forward tcp:56789 tcp:56789
curl -X POST http://localhost:56789/bump
```

The brief mentions `adb reverse`, which forwards the other way (a port on the device to the PC). Use `adb reverse tcp:56789 tcp:56789` only when the listener runs on the PC, for example the Unity Editor in Play mode, and the request is sent from the device. For the APK and a PC-side curl, use `adb forward` as above. If the Editor is also in Play mode on the same PC, port 56789 is already taken there. Stop Play mode, or forward a different host port: `adb forward tcp:56790 tcp:56789`, then `curl -X POST http://localhost:56790/bump`.

In the Editor, enter Play mode, start a level and use `curl -X POST http://localhost:56789/bump` directly.

### Implementation notes

`BumpListener` is a minimal `TcpListener` HTTP server on a background thread. It parses and validates each request, then queues it. `BumpRunner` drains the queue on Unity's main thread, so no GameObject is touched off the main thread. The response is sent after the main thread accepts or rejects the bump, so the status code reflects the game state. Connections, request size and queue length are capped. The burst is pooled, freezes with pause, and is cancelled on level change, win or loss, so a bump cannot carry over to another level.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Game/Core` | Game session, level data, settings (`GameSettings`) |
| `Assets/Game/Gameplay` | Player motor, climber pose, hazards, camera follow and effects |
| `Assets/Game/Presentation` | HUD, menus, win/loss, bump burst, audio, haptics |
| `Assets/Game/Webhook` | HTTP listener, request parsing, main-thread runner |
| `Assets/Game/PostFx` | URP bloom and chromatic-aberration pulses |
| `Assets/Game/Editor` | Scene builder and build script |
| `Assets/Game/Settings/GameSettings.asset` | Every tuning value: feature flags, pace, bumps, camera, audio, haptics, webhook port |
| `Assets/Game/Levels` | The five level definitions (JSON) |
| `Assets/Game/Tests` | EditMode and PlayMode tests |

## Third-party assets

All assets are free to use. No asset was generated. Each folder under `Assets/Game/Art/Licensed/` has a `LICENSE.md` with the source, the files kept and any modification.

| Asset | Author | License | Source | Use |
| --- | --- | --- | --- | --- |
| Blocky Characters (`character-b`) | Kenney | CC0 1.0 | https://kenney.nl/assets/blocky-characters | Climber; texture palette recoloured toward the reference climber's colours |
| Castle Kit (`tower-base`, `tower-top`) | Kenney | CC0 1.0 | https://kenney.nl/assets/castle-kit | Tower pedestal and crown |
| Skyboxes (`skybox-day`) | Kenney | CC0 1.0 | https://kenney.nl/assets/skyboxes | Clouds |
| UI Pack (star sprite) | Kenney | CC0 1.0 | https://kenney.nl/assets/ui-pack | Victory confetti and grab sparks |
| Boxing glove icon | Lorc, game-icons.net | CC BY 3.0 | https://game-icons.net/1x1/lorc/boxing-glove.html | Bump gloves and card badge (tinted at runtime) |
| Lilita One font | Juan Montoreano | SIL OFL 1.1 | https://github.com/google/fonts/tree/main/ofl/lilitaone | Titles, HUD numbers, prompts |
| Impact Sounds | Kenney | CC0 1.0 | https://kenney.nl/assets/impact-sounds | Bump punches, hazard hit |
| RPG Audio | Kenney | CC0 1.0 | https://kenney.nl/assets/rpg-audio | Grab |
| Digital Audio | Kenney | CC0 1.0 | https://kenney.nl/assets/digital-audio | Positive bump |
| Music Jingles | Kenney | CC0 1.0 | https://kenney.nl/assets/music-jingles | Level win and final win |
| Interface Sounds | Kenney | CC0 1.0 | https://kenney.nl/assets/interface-sounds | Button click |
| Happy Lullaby (song17) | cynicmusic | CC0 1.0 | https://opengameart.org/content/happy-lullaby-song17 | Background music |

Attribution: "Boxing glove" icon by Lorc (https://lorcblog.blogspot.com), from game-icons.net, CC BY 3.0. Kenney assets from www.kenney.nl. The tower shaft, collars and windows use Unity's built-in cylinder and cube meshes with flat materials.

## Assumptions and decisions

- **Visual target.** The reference image is the primary art target and the video is secondary motion and event context. The reference uses a Dragon Ball-style tower and climber. No Dragon Ball asset is used; a CC0 blocky character is recoloured to that palette and the tower is assembled from CC0 and built-in meshes.
- **Controls.** The video suggests one-touch climbing, so the game uses hold-to-climb and release-to-grip, with hand-over-hand planted grips.
- **The bump as a social event.** The brief requires a full-screen glove burst on `/bump`. The game treats it as a viewer interaction: a bare request knocks the climber down, and `polarity=positive` lifts them. Neither costs a life, and every level can be completed without bumps.
- **Lives and pause.** The brief lists pause and win/lose screens; the reference does not show them. Both are included behind flags in `GameSettings.asset` (`livesEnabled`, `startingLives`, `pauseMenuEnabled`) and are on in the delivered build. With lives off, hazard hits only knock the climber down.
- **Level flow.** Levels are sequential (Next Level after each summit) rather than chosen from a separate level-select screen. The brief accepts either.
- **One scene.** The five levels share one gameplay scene and differ by data (height, hazard count, timing). Their shared visual theme follows the reference; difficulty provides the progression.
