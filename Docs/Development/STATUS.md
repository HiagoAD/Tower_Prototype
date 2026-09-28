# Development status

Plan: [ACTION_PLAN.md](ACTION_PLAN.md). Implementer handoff: [CLAUDE_HANDOFF.md](CLAUDE_HANDOFF.md).

Current state: **planning complete; implementation not started by Codex**.

Quota review: **original three-agent option replaced by one Claude Sonnet session by default**. Codex reviews G2/G3/G5/G6 and blockers. Account-specific quota fit remains **NOT VERIFIED**; no usage balances have been supplied. The candidate relays checkpoints by asking Codex to read this file.

Submission target: **2026-09-28 22:00 Recife / 2026-09-29 01:00 UTC**.
User deadline: **09:00 UTC**, interpreted as **2026-09-29 09:00 UTC**; date confirmation remains open.

| Gate | Target, Recife | Status | Evidence |
| --- | --- | --- | --- |
| G0 — device, baseline, contracts | 08:15 | PASS | Physical device `0070013699` (Motorola moto_g_5G_plus, codename `nairo`), arm64-v8a, Android 11 (API 30), 1080x2520 portrait, authorized over USB. Unity 6000.3.11f1 + Android/SDK/NDK modules confirmed installed. Reference checksums verified (`shasum -a 256 -c SHA256SUMS`: all OK). Pre-existing working-tree edits to ACTION_PLAN/CLAUDE_HANDOFF/STATUS preserved (not touched by build work). Contracts: see `Assets/Game/Webhook/BumpRequest.cs`. |
| G1 — Android install and HTTP proof | 09:15 | PASS | See detailed report below. |
| G2 — Android vertical slice | 11:30 | PARTIAL — see report | Core loop (menu→start→climb→hazard→hit/recover→webhook glove burst) proven on-device; one known HUD-text rendering bug open. |
| G3 — five complete levels and menus | 13:30 | NOT VERIFIED | Pending. |
| G4 — complete candidate; feature freeze | 16:00 | NOT VERIFIED | Pending. |
| G5 — accepted release candidate | 18:00 | NOT VERIFIED | Pending. |
| G6 — recording and complete package | 20:00 | NOT VERIFIED | Pending. |
| G7 — verified link submitted | 22:00 | NOT VERIFIED | Pending. |

## Known risks / decisions

| Item | Owner | Next action |
| --- | --- | --- |
| Device/emulator availability | Candidate + Claude lead | Identify target and prove install/recording in kickoff. |
| Deadline date inferred | Candidate + Codex | Confirm September 29 if the interpretation is incorrect; today-end target remains earlier. |
| Running Editor's Pipeline unreachable in planning session | Claude lead | Diagnose connection/permissions; do not assume missing package. |
| No climb clip in selected character pack | Presentation lane | Pose imported rig in code; cap initial work at 45 minutes. |
| Brief names `adb reverse` for PC-side request | Webhook lane + lead | Implement/test `adb forward`; explain direction in README. Preserve brief. |
| No asset generation | All | Import existing free art/audio; retain licenses and attribution. |
| Pro allowances may not cover the full implementation sprint | Candidate + both agents | Record balances/reset times below; measure usage after G1/G2; prioritize required behavior and fixes. |

## Quota observations

| Account / observation | Session remaining / reset | Weekly remaining / reset | Notes |
| --- | --- | --- | --- |
| Claude Pro — kickoff | Unknown | Unknown | Single Sonnet implementation session; no standing worker team. This session (VS Code extension, no `/usage` slash command or Settings->Usage surface available to the agent) cannot self-report exact remaining allowance/reset time; candidate should check Settings -> Usage in the Claude app/CLI directly if precise numbers are needed. |
| ChatGPT Pro / Codex — kickoff | Unknown | Unknown | Pro 5× versus 20× not supplied; existing model assignment retained. |
| Claude after G1 | Pending | Pending | Record allowance consumed and reassess remaining work. |
| Claude after G2 | Pending | Pending | Preserve allowance for fixes and delivery. |

## Verified during planning

- Brief, reference image, and video samples inspected; all immutable snapshot checksums passed.
- Unity/project package versions and Android toolchain directory presence inspected.
- Free source pages checked for character, tower, sky, glove, impact audio, and UI.
- Character/tower archives inspected for license, relevant models, and character animation names. Assets have not been imported or tested in Unity.

## Implementation updates

Claude appends dated gate reports here, with artifact paths and real results. Codex records review decisions and scope changes here. Keep this status separate from the immutable reference.

### 2026-09-28 — G0/G1 report (Claude, one Sonnet session)

```text
Gate and result: G0 — PASS, G1 — PASS
Time remaining to 22:00 target: ~13.5 hours at time of writing
Quota remaining and reset times: not directly queryable from this session (VS Code extension, no /usage or Settings->Usage surface exposed to the agent). Candidate should check Settings -> Usage in the Claude app/CLI for exact numbers; not yet material to schedule.
Source revision / changed files: working tree on `main`, uncommitted. New: Assets/Game/Webhook/{BumpRequest,BumpListener,BumpRunner}.cs, Assets/Game/Editor/{SmokeSceneSetup,BuildScript}.cs, Assets/Game/Scenes/Smoke.unity. Incidental Unity re-serialization diffs in Assets/Settings/*.asset and ProjectSettings/ProjectSettings.asset (URP asset/version migration + our Android player-setting changes: applicationIdentifier=com.towerprototype.game, minSdk=24, forceInternetPermission=true) and ProjectSettings/EditorBuildSettings.asset (now lists Smoke.unity). Reviewed; no unexpected content. Pre-existing unstaged edits to ACTION_PLAN.md/CLAUDE_HANDOFF.md/STATUS.md (Codex's prior quota-review pass) left untouched.
APK path and device / OS / ABI: Builds/Android/TowerPrototype.apk (78 MB, Development build). Device 0070013699, Motorola moto_g_5G_plus (codename nairo), Android 11 (API 30), arm64-v8a, 1080x2520 portrait, connected/authorized via USB.
Checks run, executed-test counts, results: No automated tests yet (transport-only smoke scene, G1 scope). Manual device proof: adb install -r (success) -> app launched and foregrounded (UnityPlayerGameActivity resumed) -> adb forward tcp:56789 tcp:56789 -> curl GET /bump -> HTTP/1.1 200, {"requestId":...,"accepted":true} -> curl POST /bump -> HTTP/1.1 200, distinct requestId -> on-device "Bumps: N" counter incremented for each accepted request (confirmed via logcat `[Bump] accepted ...` lines and a device screenshot) -> adb forward --remove tcp:56789 cleanup.
Screenshots / recording / logs: Docs/Development/evidence/g1_bump_counter.png (device screenshot, "Bumps: 2" after one GET + one POST). Build logs: Logs/smoke-scene-build2.log, Logs/smoke-android-build2.log.
Known gaps or assumptions changed: This is the G1 infrastructure proof only — BumpListener accepts any /bump GET or POST unconditionally (no session-state 409/menu-paused checks yet; those land with GameSession in G2). First scene-setup pass wired the EventSystem with the legacy StandaloneInputModule, which spammed InvalidOperationException against this project's New-Input-System-only setting; fixed by dropping the EventSystem entirely (the smoke scene has no interactive UI) and rebuilt/reverified cleanly. No live Unity Editor/Pipeline connection was available (`unity status`/`unity pipeline list` reported the running Editor's Pipeline server unreachable, not Safe Mode); all scene/build work went through Unity CLI batchmode (`unity run -executeMethod ...`) driving real Editor-supported APis (EditorSceneManager, BuildPipeline), not hand-edited YAML.
Next action and ETA: proceed to G2 — one full playable level (climbing, camera, hazards, win/lose/retry), GameSession/LevelDefinition/PlayerMotor contracts, first glove effect wired to the real webhook path, minimal menus. Target per plan: 11:30 Recife.
Decision needed from overseer, if any: none blocking; flagging for awareness that live Pipeline-driven Editor control was unavailable this session (batchmode used instead) in case Codex wants to verify the Editor state.
```

### 2026-09-28 — G2 report (Claude, one Sonnet session)

Before implementing: read `Docs/Development/REFERENCE_BEHAVIOR_REVIEW.md` (Codex) and produced
`Docs/Development/REFERENCE_OBSERVATION_ADDENDUM.md` (frame-sampled the reference video at 0–17s
and 85–103s per Codex's direction — the footage turns out to be a screen capture of an existing
app titled "KarinTower", with a numeric altitude HUD and several distinctly-named attack types,
not a single flat "3 mistakes" counter; input method for climbing remains unconfirmed from visual
evidence). Per that review's explicit instruction, G2 does **not** build an elaborate hazard/lives
system on the plan's authority — it implements the smallest playable interpretation (one generic
hazard behaviour, a plain `int` hit-point counter, hold-to-climb behind a one-file input seam) and
flags hold-to-climb/hazard-timing/hit-count as still-provisional design choices, not observed
reference behaviour.

Two independent review passes were used before/while integrating on-device, both read-only:
- A Sonnet subagent retroactively split the (until then uncommitted) working tree into 7 ordered
  commits (Codex's planning notes → G0 status → G1 webhook/smoke proof → G1 report → reference
  addendum → licensed glove asset → G2 vertical slice).
- An Opus 5.5 senior-reviewer pass (`unity-code-reviewer` role) read the full G0–G2 diff and
  correctly root-caused the on-device "Start button does nothing" symptom before any device time
  was spent chasing it further, plus flagged several other real issues (see "Fixed this pass").

```text
Gate and result: G2 — PARTIAL (core loop proven on real device; one known display bug open, win/lose not yet directly re-observed after the last fix round)
Time remaining to 22:00 target: not re-measured this pass; substantial time went into two debugging detours (button wiring, then a ScriptableObject lifecycle bug) — see "Time notes" below.
Quota remaining and reset times: still not queryable from this session; unchanged from G1 note.
Source revision / changed files: Assets/Game/Core/{GameSession,SessionState,LevelDefinition,HazardSpec}.cs; Assets/Game/Gameplay/{PlayerMotor,ClimbInputSource,HazardBand,CameraFollow,CameraShake}.cs; Assets/Game/Presentation/{HudView,GloveBurstView,MenuView}.cs; Assets/Game/Editor/Level1SceneSetup.cs; Assets/Game/Levels/Level1.asset; Assets/Game/Scenes/Level1.unity; Assets/Game/Art/Licensed/BoxingGlove/{boxing-glove-white.png,LICENSE.md}; Assets/Game/Webhook/{BumpRequest,BumpListener}.cs extended with LevelInstanceId + StateProvider (409/429) for the real session-state-aware dispatch contract. All committed on `main` (7 commits, see git log).
APK path and device / OS / ABI: Builds/Android/TowerPrototype.apk, same device as G1 (0070013699, Motorola moto_g_5G_plus, Android 11, arm64-v8a, 1080x2520).
Checks run, results:
- Menu → Start button → Playing state: PASS after fix (see "Fixed this pass" #1). Screenshot: g2_playing_v2.png.
- Hold-to-climb via touch in the bottom screen region: PASS. Height increases, camera follows. Screenshots: g2_climbing.png, g2_hazard_hit.png.
- Hazard band visual (red=active/dangerous, yellow=safe, cycling per HazardSpec timing): PASS, visually confirmed cycling between the two colors across screenshots taken seconds apart.
- Webhook → six-glove burst while Playing: PASS. `curl -X POST http://localhost:56789/bump` through `adb forward` → HTTP 200 with a request ID → multiple red boxing gloves converge on-device, flash/shake fire, on-screen "BUMP! <requestId prefix>" feedback text appears. Screenshot: g2_glove_burst2.png. Continued play after the burst was not separately re-verified in this pass (previous architecture guarantees it structurally — the hit is nonlethal and never consumes a hit point — but a fresh on-device observation is still owed).
- Win (reach finish height) and Lose (hit points to 0) states: NOT YET RE-VERIFIED on-device after the last rebuild (ran out of session time in this pass after confirming the core loop + webhook path). Both paths are implemented and exercised by no automated test yet.
- No automated EditMode/PlayMode tests written yet for G2 logic (HazardSpec timing, PlayerMotor bounds, GameSession dispatch/staleness). Flagged as required before G3 sign-off per the plan's "Automated scope" row.

Fixed this pass (all found via the Opus review + on-device iteration, not guessed):
1. **Button clicks did nothing.** `Button.onClick.AddListener(...)` called from editor batchmode code only adds a runtime (non-persistent) listener, which `EditorSceneManager.SaveScene` never serializes — every button's `m_OnClick.m_PersistentCalls` was empty in the saved scene. Fixed with `UnityEditor.Events.UnityEventTools.AddPersistentListener` for all 7 buttons; verified non-empty in the saved `.unity` YAML before rebuilding.
2. **`GameSession.StartLevel()` threw `NullReferenceException` on the very first tap once #1 was fixed.** The `LevelDefinition` ScriptableObject asset was created via `AssetDatabase.CreateAsset` *before* `EditorSceneManager.NewScene(...)` ran; that `NewScene` call unloads not-yet-referenced assets created earlier in the same batch invocation, silently turning the held C# reference into a destroyed ("fake null") `UnityEngine.Object` — confirmed by direct before/after logging (`level == null` flipped from `False` to `True` across the `NewScene` call) and by inspecting the serialized scene (`level: {fileID: 0}`). Fixed by moving level-asset creation to *after* the scene reset.
3. Non-menu panels (Pause/Win/Lose/HUD) are now saved inactive by default, not just deactivated at runtime by `MenuView` — closes a latent trap where a `MenuView` failure could leave a full-screen button stack silently eating input over the main menu.
4. Hit-recovery timing: a hit's downward displacement (1.5) at the level's climb speed (2.5) took exactly as long to re-climb (0.6s) as the original invulnerability window (0.6s), so a held climb could re-cross one active hazard 2–3 times and lose from a single band. Added a separate `climbLockoutSeconds` (0.4s) that briefly blocks climb input after a hit, and widened invulnerability to 1.2s (comfortably longer than the recovery-climb time) — matches the plan's "deterministic hit/recovery" requirement.
5. The Pause button sat inside `ClimbInputSource`'s bottom-35%-of-screen climb region, so tapping Pause could also register a climb frame. Repositioned Pause to the top-right and added an `EventSystem.IsPointerOverGameObject` guard in `ClimbInputSource` as defense in depth.
6. Hazard bands had no renderer at all (an empty GameObject) — added a flat colored disc (red=active, yellow=safe) driven by the same `HazardSpec.IsActiveAt` the hit-detection uses, so the "wait below an active band, then climb through the safe interval" mechanic the plan calls for is now actually visible, not just logical.
7. `HazardSpec.IsActiveAt` guarded against `periodSeconds <= 0` (would otherwise divide/mod by zero and NaN forever).
8. Removed the temporary `InputDiagnostics` component used to rule out an Input System wiring theory before the real root cause (#1) was found.
9. Minor: `HudView` no longer reallocates its height/HP strings every frame (caches last floored values); `GloveBurstView`'s glove-spawn radius now derives from the canvas's own local-unit rect instead of raw `Screen.width/height` (those only coincided by chance at this device's exact resolution vs. the 1080x1920 reference).

Known gaps / open bugs:
- **`heightText` and `hitPointsText` (the "Nm / Nm" and "HP: N" HUD labels) do not visually render on-device**, despite: correct field bindings (verified via diagnostic logging — `BindPrivate` reports `applied=True` with a valid `propRef`), a non-empty baked initial value (`"0m / 0m"`) in the saved scene, `m_IsActive: 1` on the Text GameObjects, and coordinates that check out arithmetically against the Canvas's confirmed 1:1 local-unit-to-screen-pixel mapping (independently validated against the Pause button's correctly-rendered on-screen position). In the same `HudView`/`hudPanel`, `bumpFeedText` (built with the identical `AddText` helper) *does* render correctly on top of the glove burst — so the text-rendering path itself works; something specific to `heightText`/`hitPointsText` (different `TextAnchor` — UpperLeft/UpperRight vs. UpperCenter/MiddleCenter for the ones that work — is the leading remaining suspect, not yet confirmed) is suppressing them. Does not block any required brief functionality (climbing, hazards, the webhook event, and its request-ID feedback all read fine without it) but the altitude/HP readout is a real, expected HUD element and should be fixed before G3. Next step: reproduce with a live Editor Play-mode session (once/if Pipeline connectivity is available) where the Scene/Game view can be inspected directly instead of only via device screenshots, or bisect by temporarily setting `TextAnchor.UpperCenter` on both to see if alignment is actually the variable.
- Win/Lose states implemented but not freshly re-observed on-device after the last rebuild (see above).
- No automated tests yet for G2 logic.
- Tower/character are still Unity primitives (cylinder stack + capsule), not the licensed Kenney Castle Kit / Blocky Characters assets — the user supplied the full asset pack (`kenney_castle-kit`, `kenney_blocky-characters_20`, `kenney_skyboxes`, `kenney_impact-sounds`, `kenney_ui-pack`, plus extras) at `/Users/hiago/Downloads/AssetPack/`, inspected and license-verified this pass, but import/swap-in is not yet done. Real boxing-glove SFX candidate identified: `impactPunch_heavy_000.ogg` from the impact-sounds pack.
- Bump requests answered `200 accepted:true` can, in a roughly one-frame race, still be dropped if the session transitions out of Playing between HTTP acceptance and the next `GameSession.Update` drain (flagged by review as low-severity/low-probability; not fixed this pass).

Time notes: this G2 pass took substantially longer than planned because of two full debugging
detours requiring several Android rebuild-and-flash cycles each (the button-wiring bug and the
ScriptableObject-lifecycle bug above). Both are now understood and fixed with a clear root cause,
not worked around, so G3's five-level authoring should not re-trigger either one. The device
screen also went to sleep mid-session (Android's default screen timeout) and produced one
all-black screenshot that briefly looked like a rendering crash; screen timeout is now set to 30
minutes on the test device to avoid repeating that false alarm.

Next action and ETA: fix the heightText/hitPointsText rendering gap; re-verify win/lose on-device;
write the first EditMode/PlayMode tests (HazardSpec timing, PlayerMotor bounds/hit math, session
staleness/409 dispatch); then import the now-available Kenney assets (tower, character with a pose
driver, sky, impact SFX) before authoring the remaining four levels for G3.
Decision needed from overseer, if any: none blocking. Flagging for Codex's G2 review: the
provisional hold-to-climb/hazard-count/hit-count interpretation is unchanged from the addendum;
happy to take a specific ruling on those before G3 locks in five levels' worth of hazard timing
built on top of them.
```
