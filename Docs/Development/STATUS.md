# Development status

Latest ownership update — 2026-09-28: **Claude owns planning, prioritization, milestone review, implementation, integration, and delivery coordination**, at the user's request. No Codex sign-off is required. Claude maintains the plan, backlog, forecasts and gate evidence. Quota fallback remains available; Claude resumes full ownership after recovery. Keep one Editor/build/device owner.

Plan: [ACTION_PLAN.md](ACTION_PLAN.md). Project ownership handoff: [CLAUDE_HANDOFF.md](CLAUDE_HANDOFF.md). Earlier assessment: [GOALS_REVIEW.md](GOALS_REVIEW.md).

Current state: **G2 and G3 PASS; G4–G7 NOT VERIFIED.** Five sequential levels and campaign/menu flow are accepted for the recorded baseline. Lives and pause now ship enabled behind flags in `Assets/Game/Settings/GameSettings.asset`; webhook bumps never cost lives. These later decisions supersede the earlier no-lives/no-pause summaries. See [G3 review](G3_REVIEW.md) and the dated implementation reports below.

Current priority: reconcile existing uncommitted implementation work with the [ordered player-feedback backlog](ACTION_PLAN.md#2a-ordered-player-feedback-backlog), complete the remaining batches, then collect current-build Android evidence for G4/G5. Latest reported settings-consolidation results are **191/191 EditMode and 22/22 PlayMode**. Later cleanup batch validation passed compilation and scene/dependency checks; it did not rerun gameplay or Android. This handoff performs no new implementation validation or gate acceptance.

Forecast: the earlier 17:45 schedule and **4½–6½-hour** estimate below are historical and do not cover the expanded feedback scope. Claude must re-estimate from actual remaining work and preserve at least 90 minutes for recording/package/access checks. The deadline remains interpreted as **September 29 09:00 UTC / 06:00 Recife**; no new date confirmation is asserted.

| Gate | Revised target, Recife Sep 28 | Status | Evidence / remaining acceptance |
| --- | --- | --- | --- |
| G0 — device, baseline, contracts | Completed earlier | PASS | moto g 5G plus `0070013699`, Android 11, arm64-v8a; Unity 6000.3.11f1; immutable hashes verified. |
| G1 — Android install and HTTP proof | Completed earlier | PASS | Earlier installed APK responded to GET/POST; detailed historical reports below. |
| G2 — integrated social vertical slice | Closed Sep 28 | PASS | User confirmed all four current-build acceptance checks passed: artifact provenance, real HTTP/social recovery, lifecycle cleanup, and audible impact/current visuals. See closure record below. |
| G3 — five complete levels and menus | Closed Sep 28 | PASS | Five distinct levels, sequential/final/menu flow and Android completion evidence reviewed. User retains a tuning pass before freeze; see G3_REVIEW.md. |
| G4 — complete candidate; feature freeze | 20:00–21:00 | NOT VERIFIED | Complete content and focused tuning; freeze when candidate actually passes. |
| G5 — accepted release candidate | 21:00 | NOT VERIFIED | Code-review fixes, final device/lifecycle/performance evidence and APK/source provenance. |
| G6 — recording and complete package | 22:15 | NOT VERIFIED | All-five final APK video, README/credits, clean source and packaged reinstall. |
| G7 — verified link submitted | 23:00 | NOT VERIFIED | Upload/download/access checks and candidate submission. |

Gate target times above are historical estimates, not current forecasts or acceptance. Historical reports below retain their original findings, ownership and assumptions; current decisions in ACTION_PLAN and CLAUDE_HANDOFF supersede them. The prior Jev request `jev/20260928T203943015475Z-G2` describes an earlier assessment and is not current-plan acceptance evidence.

## Known risks / decisions

| Item | Owner | Next action |
| --- | --- | --- |
| Device ownership | Candidate + Claude lead | Known moto g 5G plus; coordinate exclusive build/device time and confirm recording/audio capture. |
| Deadline date inferred | Candidate + Claude | Confirm September 29 if the interpretation is incorrect; today-end target remains earlier. |
| Running Editor's Pipeline unreachable in planning session | Claude lead | Diagnose connection/permissions; do not assume missing package. |
| Final integrated acceptance | Claude | Preserve accepted planted-grip feel; verify social recovery, audio and current visuals on Android. |
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

## Development control panel

2026-09-28 — Codex added a local, read-only project monitor in `Tools/DevelopmentPanel/`.
Start it from the project root with `python3 Tools/DevelopmentPanel/server.py`, then open
<http://127.0.0.1:8765>. See [panel instructions](../../Tools/DevelopmentPanel/README.md).
It refreshes the status/plan records and artifact inventory every 15 seconds while visible,
preserves reported partial/unverified results, and checks the immutable reference hashes.
It does not run Unity or change gate acceptance.

Verification: 7 Python fixture tests passed; JavaScript syntax check passed; headless Chrome
checks passed for milestone search/filtering, document/image/video viewers, manual refresh,
disconnection/recovery, video range requests, and restricted file access. All five views were
checked at 390, 768, and 1024 pixels without page overflow; the 1440-pixel desktop and 390-pixel
mobile screenshots were visually inspected. All 6 immutable reference checksums match.

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

### 2026-09-28 — G2 overseer review (Codex)

**G2 remains PARTIAL, not accepted.** See [G2_REVIEW.md](G2_REVIEW.md) for the ordered repair handoff. Source reviewed at `e55825a`; saved device screenshots inspected; all immutable reference checksums passed. Codex did not run a new APK/device session or automated tests in this review.

Confirmed source findings: off-screen HUD text geometry; POST-body bytes discarded by the header parser; successful HTTP replies preceding main-thread acceptance; glove impact aimed at screen center rather than the character; no assigned impact audio clip. The saved device screenshots also show a missing `CapsuleCollider` runtime error. Win/lose/retry and post-webhook continued play still need fresh evidence on the repaired APK.

Design ruling: retain the isolated hold-to-climb input and one generic configurable hazard/HP model as documented assumptions under brief section 10. They are not established reference mechanics. Finish the G2 repair/check cycle, then integrate licensed presentation and five-level progression for G3. Keep the quota-saving default of one Claude Sonnet implementation session.

### 2026-09-28 — G2 repair report (Claude)

```text
Gate and result: G2 — READY FOR G2 CLOSURE CHECK (Codex decides; not self-declared PASS)
Time remaining to 22:00 Recife target: ~11h20m at time of writing (10:42 Recife)
Quota remaining and reset times: still not queryable from this session; unchanged from prior notes.
Source revision / changed files: commit b71c989a0a43301924ebae3ea7519beda05e8cb7 on `main`
  ("Repair G2: HUD anchors, webhook body/acceptance, glove target+SFX, hazard prefab, EditMode
  tests"), scoped to Assets/Game/** only. Full file list: Assets/Game/Core/GameSession.cs,
  Assets/Game/Editor/Level1SceneSetup.cs, Assets/Game/Gameplay/HazardBand.cs,
  Assets/Game/Levels/Level1.asset, Assets/Game/Presentation/GloveBurstView.cs (+ new
  SafeAreaFitter.cs), Assets/Game/Scenes/Level1.unity, Assets/Game/Webhook/{BumpListener,
  BumpRequest,BumpRunner}.cs, new Assets/Game/{Game.asmdef,Editor/Game.Editor.asmdef,
  Tests/EditMode/Game.Tests.EditMode.asmdef + 3 EditMode test files}, new
  Assets/Game/Art/Materials/{HazardActive,HazardSafe}.mat, new
  Assets/Game/Prefabs/HazardVisual.prefab, new
  Assets/Game/Art/Licensed/ImpactSounds/{impactPunch_heavy_000.ogg,LICENSE.md}. Docs/ and Tools/
  left untouched/uncommitted (pre-existing edits preserved).
  Since that commit, a second, test-only commit landed: 194fbd3bb348bab2e4cb58cc8ece1289f440af0a
  ("Add GameSession lose->retry EditMode test; ignore perf-test run output"), containing only
  Assets/Game/Tests/EditMode/GameSessionLoseRetryTests.cs (+ .meta, covers Lose -> Retry reset;
  see item 3 below and "Known gaps") and the `.gitignore` line for
  `/[Aa]ssets/[Rr]esources/PerformanceTestRun*` (see "Tooling side effects" below). This second
  commit does not touch gameplay/transport/presentation code -- the built APK's source revision
  stays b71c989; nothing in 194fbd3 changes runtime behaviour.
APK path and device / OS / ABI: Builds/Android/TowerPrototype.apk (132.2 MiB / 138,669,830 bytes
  on disk; BuildReport's internal summary.totalSize read 1,326,596,037, which does not match the
  on-disk APK size and is not used here), built from commit b71c989 via
  Game.Editor.BuildScript.BuildAndroid, Result=Succeeded, Errors=0
  (Logs/g2r-android-build.log). Device 0070013699, Motorola moto_g_5G_plus (nairo), Android 11
  (API 30), arm64-v8a, 1080x2520 portrait, USB-connected/authorized.
Checks run, executed-test counts, results:
  - EditMode suite: the committed revision b71c989 contains 17 EditMode tests (all passing). A
    separate 18th test, GameSessionLoseRetryTests (Lose -> Retry reset coverage), was written
    afterwards; the local suite with it included is 18 total, 18 passed, 0 failed,
    0 inconclusive, 0 skipped (duration 0.85s, Logs/editmode-test-final.log; no `error CS`).
    Includes the 750ms-bounded loopback expiry test (asserts ~753ms elapsed, confirming a real
    bounded wait, not a trivial pass).
  - Asset-GUID stability fix verified: regenerated Level1 scene twice in a row
    (Logs/level1-scene-build9.log, Logs/level1-scene-build10.log) after switching
    BuildHazardVisualAssets/CreateOrReplaceMaterial to create-if-missing/update-in-place. The
    three asset .meta GUIDs (HazardActive.mat=e0d1706a676134affbb246d1a6f078f6,
    HazardSafe.mat=e9f1b4ff2264c42d68a5b44f25df5a38,
    HazardVisual.prefab=d4ee4d69bd7044ce6bedda4843121983) were identical across both runs, and
    the scene's own references to those three GUIDs matched too. Level1.unity's *own* file
    content still differs between the two runs (line count identical, 5069 lines, but every
    GameObject's internal fileID changes) -- this is pre-existing behaviour of the whole
    Level1SceneSetup.Build() method, which rebuilds the entire scene from scratch every run
    (tower segments, camera, UI, etc. all get fresh Unity-assigned fileIDs each time); it was not
    part of the requested fix and was not touched.
  - Tooling side effects flagged by the reviewer: the reviewer directly observed, around 10:13
    while the Android build was linking, untracked `Assets/Resources/PerformanceTestRunInfo.json`
    and `PerformanceTestRunSettings.json` (each with a `.meta`), plus a `preloadedAssets` entry
    for `InputSystem_Actions.inputactions` (guid `052faaac...`) in the `ProjectSettings.asset`
    diff. Both were gone by the end of this run -- what removed them is unknown (likely Unity's
    test runner/build cleanup); no guess beyond that. The APK was therefore probably built with
    those two small JSON files present in Resources, which is harmless. `ProjectSettings.asset`
    is clean against HEAD now. The requested `.gitignore` line was added defensively (see above).
Screenshots / recording / logs: all under Docs/Development/evidence/, g2r_* prefix (25
  screenshots + 1 screen recording + 2 extracted burst frames + logcat dump). See checklist below
  for which file backs which claim.
Known gaps or assumptions changed:
  - GameSession stale-level/not-Playing rejection is still not covered by an AddComponent test:
    OnEnable() opens a real socket on the hardcoded port as a side effect of construction, and
    EditMode never ticks Update(), so the natural DrainBumpQueue path can't be exercised without
    reflecting into two layers of private state. Accepted as-is per prior review.
  - Pause timing (HazardBand keyed off global Time.time, not a frozen level clock) and
    tower/character composition (still primitives) are unchanged G3 items per G2_REVIEW.md.
  - Lose is very hard to reach in a single honest normal-play climb with the current 2-band/3-HP
    configuration -- see checklist item 3 for the empirical evidence and the separate EditMode
    test added to cover the Lose -> Retry reset itself.
Next action and ETA: awaiting Codex's G2 closure decision; G3 (licensed presentation import,
  five-level authoring) is next once that lands.
Decision needed from overseer, if any: G3 level tuning must make Lose reachable in normal play
  (e.g. band count >= HP on later levels, or revisit the post-hit pass-through), since the brief
  requires win/lose states.
```

**G2_REVIEW.md findings 1-5, resolved this pass:**

1. **HUD off-screen labels** — `HeightText`/`HitPointsText` now use real top-left/top-right
   anchors+pivots (480/280px wide, 32px margin, not 1000px wide at a centered offset), a new
   `SafeAreaFitter` component shrinks the whole HUD panel to `Screen.safeArea` so the device's
   115px top camera-cutout inset is respected, and `PauseButton` moved to a second row under
   `HeightText` so it never overlaps `HitPointsText`. Verified in the regenerated scene YAML and
   on-device at both 1080x2520 and the 1080x1920 reference aspect (checklist items 1 and 6).
2. **POST body causing a socket-timeout wait** — `BumpListener.TryReadRequest` now carries over
   bytes already read past the header terminator instead of discarding them, and rejects
   malformed/oversized/negative/duplicate-conflicting `Content-Length` immediately (400/413, no
   wait). On-device regression check: a real POST with a JSON body returned 200 in 0.026s
   (checklist item 5), not the ~3s timeout the review found by inspection.
3. **200 before main-thread acceptance** — `BumpRequest` is now a CAS-based Pending -> Accepted/
   Rejected/Expired state machine; the network worker blocks up to 750ms on the main thread's
   decision (`GameSession.DrainBumpQueue`), and only replies after that decision is known.
   Verified end-to-end on a real loopback socket in `BumpListenerLoopbackTests` (200 in ~12ms when
   drained-and-accepted, 503 at ~753ms when deliberately left undrained) and on-device (all
   webhook checks in item 5).
4. **Glove effect misses the character, no audio** — `GloveBurstView` now converges on the
   player's `Camera.WorldToScreenPoint` -> `RectTransformUtility.ScreenPointToLocalPointInRectangle`
   position, recomputed every frame of the burst, and `impactAudioSource.PlayOneShot` fires the
   licensed `impactPunch_heavy_000.ogg` (CC0, Kenney). Verified on-device: extracted burst frames
   (`g2r_burst_converge.png`, `g2r_burst_impact.png`) show all six gloves landing directly on the
   character, not centered on the canvas.
5. **Runtime `CapsuleCollider` error** — `HazardBand` no longer calls
   `GameObject.CreatePrimitive` at runtime; `Level1SceneSetup` bakes a shared
   `HazardVisual.prefab` (mesh/renderer only, no collider) and two shared materials at editor
   time, and `HazardBand` only `Instantiate`s the prefab and swaps `sharedMaterial`. Verified: the
   prefab YAML contains only Transform/MeshFilter/MeshRenderer, and a full device logcat capture
   after exercising climbing, hazards, pause/resume, the webhook, and both aspect ratios shows
   zero hits for "CapsuleCollider" (checklist item 7).

**Device checklist (G2_REVIEW #6), APK from commit b71c989, device 0070013699:**

| # | Item | Result | Evidence |
| --- | --- | --- | --- |
| 1 | Menu → Start → HUD visible, readable, clear of cutout, Pause doesn't overlap | PASS | `g2r_01_menu.png`, `g2r_02_hud.png` |
| 2 | Climb → hazard hit (HP decrements) → recovery → continue → summit → Win → Menu | PASS | `g2r_03_climb1.png` .. `g2r_07_climb4.png` (HP 3→2 at `g2r_05_climb3.png`, recovers and continues), `g2r_08_win.png` (Summit Reached!), `g2r_09_backtomenu.png` |
| 3 | Lose → Retry: fresh level, HP=3, height=0 | Lose not reached in normal play (see below); Retry reset covered separately | `g2r_10_lose_attempt_start.png` .. `g2r_16_lose_attempt_end.png`; EditMode test `GameSessionLoseRetryTests.Lose_ThenRetry_ResetsHitPointsAndHeight` (passing, see "Known gaps") |
| 4 | Pause → Resume: climbing stops/resumes; POST while paused → 409, no effect | PASS | `g2r_18_before_pause.png`, `g2r_19_paused.png`, `g2r_20_paused_after_webhook.png` (409, height/HP unchanged), `g2r_21b_resumed.png` (climbing resumed) |
| 5 | Webhook: GET, POST+body (regression), POST no body, 5x rapid POST, burst converges, climbing resumes | PASS | GET 200/0.042s, POST+body 200/0.026s, POST-no-body 200/0.016s, burst-trigger 200/0.017s, 5x rapid POST all 200 (0.019-0.027s); `g2r_burst_converge.png`/`g2r_burst_impact.png` (gloves on character); `g2r_22_post_burst_climb.png` (climbing continued after); `adb forward` removed at the end |
| 6 | HUD at reference aspect 1080x1920 | PASS | `g2r_24_ref_aspect_menu.png` (confirmed exact 1080x1920), `g2r_25_ref_aspect_hud.png` (labels/Pause correctly placed, no overlap); `wm size reset` confirmed back to 1080x2520 |
| 7 | Clean log: zero Error/Exception/CapsuleCollider | PASS | `g2r_logcat.txt` — grep hits: Error=0, Exception=0, CapsuleCollider=0 |
| 8 | Audio: clip in build, playback triggered, audible | Clip confirmed wired; trigger confirmed in code path exercised above; audible = NOT VERIFIED (pending candidate) | Committed `Level1.unity`'s `AudioSource.m_Resource` GUID (`40c04fad94666447697d262d3dffd463`) matches the imported `.ogg`'s own `.meta` GUID exactly; `impactAudioSource.PlayOneShot(clip)` is the same code path exercised for item 5's burst frames; no audio-related errors in `g2r_logcat.txt` |

**Item 3 detail (Lose reachability):** two full, honest climb attempts were made, each
deliberately lingering near both hazard bands (short repeated swipes) to try to catch their
active windows. Across both attempts (4 total band crossings), only 2 hits landed (one per
attempt), never the 3 needed to reach 0 HP from `startingHitPoints=3`. Mechanism, not just
observation: under a continuous hold, the 1.2s invulnerability window outlasts the 0.4s climb
lockout plus the ~0.6s re-climb back up to the band's height (0.4 + 0.6 = 1.0s < 1.2s), so the
player passes back through the band while still invulnerable -- at most one hit per band per
pass. With 2 bands and 3 HP, a player who just holds cannot lose Level 1. A player who
deliberately releases below a band and climbs into its *next* red window (4-5s period) could in
principle take repeated hits from the same band -- that's possible in principle but was not
demonstrated on device. Per instruction, this is reported plainly rather than claimed reachable.
The Lose → Retry *reset* itself (HP back to 3, height back to 0, state back to Playing) is
instead covered by a new, non-brittle EditMode test
(`Assets/Game/Tests/EditMode/GameSessionLoseRetryTests.cs`) that drives `GameSession` through its
public API only (`StartLevel`, `OnHazardHit`, `Retry`), using `PlayerMotor.ResetState` between
hits to clear invulnerability instead of depending on real-time hazard-cycle luck or reflection
into private queues. It passed (18/18 in the local suite with it included; see "Checks run"
above for the exact committed-vs-local counts).

### 2026-09-28 — G2 repair closure review (Codex)

**Repair pass accepted for progression to G3; G2 remains PARTIAL pending two device checks: lose → retry and audible punch sound.** Close these in the first G3 APK verification. See [G2_REVIEW.md](G2_REVIEW.md), repair closure section, for reviewed revisions, APK hash, evidence and the next instructions.

Codex inspected the code changes, successful build log, saved screenshots, logcat and actual NUnit XML: **18 passed, 0 failed, 0 skipped**. The result XML is preserved as `evidence/g2r_editmode_final.xml`. No new Unity/device test run was performed by Codex. All reference checksums passed. HUD geometry, POST body handling, main-thread HTTP acceptance, glove targeting and runtime primitive creation fixes are accepted. Audio is wired, but the burst recording has no audio stream and cannot prove audibility.

G3 priorities: licensed presentation and character/tower contact, five complete levels/menus, reachable failure and device retry proof, pause/reset timing, explicit glove coroutine/visual cleanup, meaningful nonzero-height retry coverage and stale-session dispatch coverage. Keep the single implementation-session default and no generated assets.

### 2026-09-28 — Candidate feedback: visuals and gameplay feel

Candidate reports the character/tower alignment and gameplay feel are wrong. Codex confirmed the current scene places the player beside the tower with a lateral gap, while camera framing crops the tower; the motor has constant upward translation, no climbing pose driver and an instantaneous hit displacement. The reference image and opening video samples instead show front-surface attachment and alternating limb poses.

**Reordered G3:** complete the focused [first-level feel correction](G3_FEEL_CORRECTION.md) and provide a 15–20 second physical-device preview before expanding the presentation into five levels. Candidate clarified that movement feels like floating/sliding instead of climbing: prioritize surface contact and synchronized reach/pull/body-rise motion, retaining current controls for this pass. Claude retains implementation ownership. No implementation or immutable reference files changed in this planning update.

### 2026-09-28 — Claude quota handover and Jev integration

The candidate reports Claude reached its limit and explicitly authorized a Sol implementation agent under Codex supervision. Sol continues Claude's uncommitted licensed character/tower/sky imports, pose driver, controls hint, smooth knockback and pause-clock work. Claude's last scene regeneration succeeded (`Logs/g3a-scene.log`); the installed/built G2 APK predates those changes. No G3 device proof is claimed from that older APK. Initial scope is the corrected one-level preview and outstanding G2 checks, before five-level expansion.

Codex maintains planning/review ownership. One implementation agent exclusively controls Unity scene/build/device mutations; a separate Sol test engineer prepares a read-only test plan and waits for explicit Editor ownership release before running tests. Existing user changes are preserved; no generated assets or immutable-reference edits are authorized.

Jev is now wired into the development process through [Tools/Jev](../../Tools/Jev/README.md): local request preparation, optional direct API execution, Playground response import, input hashes and advisory result documents. Six local tooling tests passed. A G2 request was prepared under `Docs/Development/jev/20260928T150037828597Z-G2`; **no live API call has run**. The candidate will configure `TYPESAFE_API_KEY` locally; it was absent from the invoking process when checked. Jev never changes gate status and cannot replace actual test/device/visual/audio evidence.

### 2026-09-28 — G2 follow-up: visual fidelity pass (Claude, covering Codex's role under the quota fallback)

Candidate direction: make the game look as close to the reference as possible; game feel out of scope for this pass. Candidate clarified that the reference tower is Korin Tower (Dragon Ball) and the climber is Goku, and approved a Goku-styled palette recolour of the CC0 character. The gate hold stays at G2; this is not G3 work.

**Framing baseline changed.** The earlier constants were measured off the landscape `ref.png` (a different, Roblox-style capture), which shrank the tower to ~10% of the width. The portrait `ref.mp4` (576x1080) matches the Android target orientation and shows the glove event, so it is now the baseline: column ~27% of screen width, climber ~1.4x the column width tall, climber at vertical centre. Palette sampled from its frames.

Changes (all in `Level1SceneSetup` output plus the presentation scripts):
- Sky: screen-space gradient skybox (`Game/GradientSky`, #113CBD top → #2878B8 → #1E72BA) replaces the pale Kenney panorama. Clouds are world-space quads behind the tower that scroll with the climb; `Game/CloudCutout` keys individual clouds out of the licensed skybox texture at render time (no new image files).
- Tower: flat sage stone material (colormap dropped), squashed pieces for tighter banding, a wide ledge every 5th band, stone pedestal over a finite sea at the base (sea leaves the frame as the camera climbs). Hazard rings are stone flanges when safe and red when active.
- Climber: Kenney character-b with `texture-b-goku.png` (palette recolour: orange gi, blue sleeves/boots, black hair; recorded in the character LICENSE.md). Arms splay outward in the grip pose so the hands read beside the head, like the reference's V grip. No Dragon Ball asset is used.
- HUD: reference altitude bar (cyan fill on a dark track, finish label above, live number on a marker; display = world height x100), heart icons built from UI primitives (the built-in font has no heart glyph on Android), small pause button. `EventFeedView` shows reference-style event cards (glove badge, request-ID prefix, "Boxing*1") per accepted bump, replacing the "BUMP!" text. The white touch-region band was removed; the prompt text remains.
- Menus: GAME OVER layout from the reference (dimmed scene, bold white title at ~22% height, stock Unity buttons: light "Continue", dark "Exit"), applied to main menu, pause, win and lose.
- Glove event: 14 staggered gloves fountain up from below and the lower sides through the character, plus a white impact glow, 16 yellow stars (Kenney UI star, CC0), flash, shake and SFX, modelled on the reference's barrage.
- New editor tool `Tower/Capture Scene Previews` (`ScenePreviewCapture`) renders menu/start/climb/bump/lose stills at the reference and device aspects without Play mode.

Checks: EditMode 25/25 passed (last run before the final editor-only text-weight tweak; runtime code unchanged since). Android build Succeeded (`Logs/g2v-android4.log`, APK sha256 `a955adcb14c84624c6b91c2b249129859721b700dc36e817d91d51d09d5a53a7`), installed on device 0070013699. Device screenshots `evidence/g2v_*` show menu, climb, webhook burst (POST → 200 accepted), event card, pause. No Unity exceptions in logcat during that run. Side-by-side with the reference: `evidence/g2v_reference_comparison.png`.

Not verified in this pass: GAME OVER screen on device (checked only in editor previews), audible SFX (unchanged path), five-level expansion (held at G2 by the candidate).


### 2026-09-28 — Codex current-work review and roadmap correction

Candidate requested immediate roadmap attention after live-device comparison and clarified that the preview image, not the video copy, is the visual target. **G2 stays PARTIAL; image fidelity is first, climbing feel/device closure next, five-level expansion afterward.** [CURRENT_FIDELITY_REVIEW.md](CURRENT_FIDELITY_REVIEW.md) defines the tasks and acceptance evidence and supersedes the earlier video-baseline decision. ACTION_PLAN and CLAUDE_HANDOFF now use that same order.

Source inspection confirms existing licensed presentation, distance-driven limb posing, eased knockback/re-grip, pause/reset clock, explicit effect-disable cleanup, and a retry test starting at nonzero height. Only one level asset exists. Saved test XML independently checked: **25 total / 25 passed / 0 failed / 0 skipped / 0 inconclusive**, preserved as `evidence/look-review-editmode.xml`; no new test run. Latest build log reports Succeeded / Errors=0; local APK hash matches the earlier `a955adcb...` report. Exact dirty-tree/installed-package provenance is not independently established.

The preceding direct device inspection saved `evidence/look-review-device.png` and `.mp4`: front-of-tower placement, ascent, a hazard hit and recovery observed; image fidelity and convincing climbing remain open. No new audio, webhook or lose/retry check is claimed. No implementation files changed; existing working-tree edits preserved. Original schedule times remain historical targets, not evidence of completion.

Validation for this roadmap update: all six immutable-reference checksums pass; local document links resolve. Jev advisory request prepared at `Docs/Development/jev/20260928T173844040542Z-G2`; no API classification was run or used to approve a gate.

### 2026-09-28 — G2 image-fidelity checkpoint: corrected device still (Claude)

```text
Gate and result: G2 — PARTIAL. Step 1–4 still delivered for candidate feedback and Codex review; not PASS.
Time remaining to 22:00 target: ~6h45m at 15:15 Recife.
Quota remaining and reset times: not queryable from this session; unchanged from earlier notes.
Source revision / changed files: uncommitted working tree on `main` (HEAD 1d2781e plus the pre-existing
  staged/unstaged/untracked changes, all preserved). This pass changed:
  Assets/Game/Editor/Level1SceneSetup.cs (palette, framing, tower assembly, clouds, lighting/shadows, HUD,
    fonts, event-card position), Assets/Game/Presentation/HudView.cs (dot-grouped altitude numbers),
  Assets/Game/Presentation/ClimberPoseDriver.cs (armSplayDegrees 20 -> 40),
  Assets/Game/Art/Shaders/CloudCutout.shader (_Opacity), Assets/Game/Art/Shaders/GradientSky.shader (comment),
  Assets/Game/Art/Licensed/Character/texture-b-goku.png (recolour shifted to ref.png colours),
  new Assets/Game/Art/Licensed/UI/Fonts/KenneyFuture.ttf, license records (Character/UI/Tower LICENSE.md),
  Assets/Settings/{Mobile,PC}_RPAsset.asset (soft main-light shadows, shadow distance 50 -> 14),
  regenerated Assets/Game/Scenes/Level1.unity and materials (TowerStone/Sea/SkyGradient/Cloud0-5 updated;
  new WindowFrame.mat, WindowPane.mat). Dashboard/Codex files untouched except the ACTION_PLAN order table
  and this file's gate row.
APK path and device / OS / ABI: Builds/Android/TowerPrototype.apk, 138,700,118 bytes, SHA-256
  cff0462ff04d0fe5368c09a384ef9f673316ab5063f6e25e1539be845eee4c1e (Logs/fid-android1.log: Result=Succeeded,
  Errors=0). Installed on 0070013699 (moto g 5G plus, Android 11, arm64-v8a, 1080x2520); the base.apk pulled
  back from the device hashes identically. No source changed after the build.
Checks run, executed-test counts, results:
  - EditMode: 25 total / 25 passed / 0 failed / 0 skipped (evidence/fid_editmode.xml, run after the build,
    same source).
  - Device: menu -> Start -> 5 s hold-to-climb -> release; stills captured at each step.
  - Logcat for the session (evidence/fid_logcat.txt): no Unity errors or exceptions; the only "Exception"
    lines are Play Store (Finsky) storage-stat warnings unrelated to the app.
Screenshots / recording / logs: evidence/fid_device_vs_ref.png (ref.png | device now | previous device
  still), evidence/fid_01_menu.png, fid_02_start.png, fid_03_climb.png, fid_04_idle.png. Scene/preview logs
  Logs/fid-scene1..4.log, Logs/fid-preview1..4.log.
Known gaps or assumptions changed: see "Asset limitations" and "Open" below.
Next action and ETA: candidate visual feedback on the still, then step 5 (climbing feel) and step 6
  (device sequence clip, audible impact, lose/retry, pause-during-recovery, cleanup). ~2 h for 5–6 if the
  still is accepted as is.
Decision needed from overseer, if any: (1) accept the engine-primitive shaft/collars/windows (below);
  (2) confirm the portrait framing numbers; (3) the altitude readout uses ref.png's dot grouping ("3.000").
```

What changed on screen, against `ref.png`:

- **Tower:** pale white/light-blue shaft (`#D6E2F2` base, lit), thin single-band collars 1.11x the shaft radius and 0.12 of its diameter tall, separating alternating short (0.7 D) and tall (1.2 D) storeys. Each storey has two columns of small blue windows (dark frame, lighter pane), 2 or 3 rows, alternating between ±40° and ±27° like the image's staggered columns. Everything is static-batched.
- **Sky:** bright cyan gradient (`#3EA6E6` top → `#80D2F2` bottom), with 30 soft, semi-transparent cloud streaks keyed from the licensed Kenney panorama, including its two broad cumulus masses.
- **Lighting and contact:** stronger direct light, lower ambient and soft main-light shadows. The climber drops a soft shadow down-left onto the shaft, visible on device. The climber now rests against the collar line, so collars no longer cut through the body.
- **Character:** the approved CC0 recolour moved to the image's golden-orange gi and azure sleeves/boots. The arm spread is wider (V grip). The character's arms-down height is 0.82 of the shaft diameter, making the climber about as tall as the shaft is wide in the grip pose, as in the image.
- **Composition (portrait adaptation):** the shaft spans 26% of screen width. The image's raw 10% would be a sliver on a phone and was not copied. The camera is raised, not tilted, so the climber's chest sits 34% up from the bottom (the image's is ~29%), leaving a long run of tower overhead.
- **HUD:** 48-unit dark rounded track filling yellow, red outlined goal altitude above it, yellow outlined current altitude riding the top of the fill. Numbers use the image's dot grouping. Kenney Future (CC0) is used for numbers, titles, buttons and the prompt, with a thick dark stroke. The functional hearts and pause remain; the pause button is restyled to the HUD palette. Event cards moved to the right-hand sky so they no longer cover the meter. Their small text keeps the built-in font, because Kenney Future's lowercase `x` reads as `H`. No WINS/Heroes/Villanos counters were added.

Asset limitations (recorded in `Art/Licensed/Tower/LICENSE.md`):

- The supplied packs have no plain round shaft or single-band collar. The castle kit's `tower-base` is a spool whose two wide bands fill 57% of its height (measured from the OBJ twin). At full size it reads as the old heavy banding; squashed into a collar it reads as a double line (tried in this pass, preview only). The shaft, collars and windows are therefore Unity built-in cylinder/cube meshes with flat colour materials. No model or texture was created. The licensed `tower-top` forms the crown and `tower-base` the pedestal.
- The image's rounded cartoon display font is not available in the supplied packs. Kenney Future is the closest licensed option.
- The blocky character has no spiky hair or emblem. Hair reads as a black block from behind.

Open (not claimed by this checkpoint): climbing-feel improvement (step 5); the idle → climb → release → webhook → recovery → climb clip; audible impact; device lose → retry from nonzero height; pause during recovery; effect cleanup across menu/retry (step 6). The start frame still shows the pedestal and sea below the tower, which the image (mid-climb) does not show. The image's windows are slightly larger and more numerous per storey than ours.


### 2026-09-28 — Parallel sequential-level implementation authorized

Candidate narrowed levels to difficulty variations with shared visuals and sequential progression, then explicitly requested parallel worktrees using lower-cost Codex agents without Claude. Two Sol implementation agents own `codex/sequential-levels` (`/private/tmp/tower-sequential-levels`) and `codex/level-difficulty` (`/private/tmp/tower-level-difficulty`); a Sol test engineer prepares focused tests alongside runtime work. Base: `1d2781e`. Main workspace visual edits are preserved; campaign agents do not drive the shared Editor/device or edit Level1SceneSetup/scene YAML. New Editor wiring utility will integrate the campaign through supported Unity APIs after visual handoff. Work in progress; no G3 completion claimed.

### 2026-09-28 — G2 climbing animation: hand-over-hand instead of running (Claude)

The candidate reported that the climb read as running. The cause was that both legs swung fore-aft in opposition and the arms made a small symmetric pump. `ClimberPoseDriver` now poses a hand-over-hand climb modelled on the reference video's motion:

- Each hand in turn reaches straight up over 30% of its cycle, then holds and pulls down to shoulder height, so one hand is always on the tower.
- The body sways, rolls and twists toward the pulling hand.
- The legs hang and swing together behind the body like a pendulum, with a small knee lift opposite the reaching arm.
- Released, the climber holds a wide two-handed grip (ref.png's V). Hit, win and lose poses are kept. The pose now freezes while paused.
- Limbs are aimed at directions in the character's space, turned from their rest axis, instead of set by fixed Euler angles, so arms keep reaching for the tower while the torso rolls.
- The cycle still advances by climbed distance, not time: 1.2 world units per full cycle, about 0.48 s at climb speed 2.5, close to the reference video's hand switches. Motor height stays authoritative.
- `ScenePreviewCapture` now renders an 8-frame cycle strip.

Checks: APK built (`Logs/anim-android1.log`, Result=Succeeded, Errors=0), SHA-256 `9a86d99d67dacca00f113db64a0c06962f9ffccbc75a007155eb6d8ee25a9d96`; the installed base.apk hashes identically. EditMode 25/25 (`evidence/anim_editmode.xml`). Device recording of start → climb → release → climb, with no Unity errors in logcat.

Evidence: `evidence/anim_climb_device.mp4` (14 s, downscaled), `evidence/anim_climb_device_frames.png` (1 s at 16 fps), `evidence/anim_climb_cycle_preview.png` (hold + 8 cycle phases from the editor).

Limits: the limbs are rigid blocks with no elbows or knees, so "pull" is shown by the arm swinging from overhead down to shoulder height. The tower scrolls faster than a hand could stay fixed on it, so the grip is stylised, as in the reference video. Webhook hit/recovery, audible impact, lose/retry and pause-during-recovery were not re-checked in this pass.

### 2026-09-28 — G2 climbing: planted grips and swinging body (Claude)

The candidate said the hand-over-hand pass still slid: hands moved with the body instead of holding the tower. They asked that a grabbing hand stay put while the other reaches, with the body swinging in response. Return point before this change: commit `27a7f32`.

- **Planted grips.** `ClimberPoseDriver` now runs a small state machine per hand. A planted hand keeps a fixed world point on the tower while PlayerMotor raises the character. When its hold falls to shoulder height and the other hand is holding, it lets go, arcs out from the wall and grabs 0.95 arm lengths above the shoulder. If the player stops mid-reach, the hand finishes the reach by time; after a knockback, both hands re-grip. Rigid arms can't change length, so the arm's depth (the camera's view axis) absorbs the changing shoulder-to-grip distance, and the hand stays exactly on its hold on screen.
- **Swinging body.** Damped springs move the body sideways toward the hand that holds alone and yank it up on every grab, with a matching roll and twist. The legs hang as a lagging pendulum.
- **Presentation scale.** Real grips cap how fast the climber can go: an arm length of reach per grab. At speed 2.5 against the old 0.6-unit character that would have been about 11 grabs a second. `Level1SceneSetup.WorldScale = 4.5` scales the camera distance, and with it the tower, climber, clouds, sea, shadow distance and camera shake. On screen everything looks the same, except the climb now runs at about 0.9 body heights per second, roughly 3 grabs a second. `LevelDefinition` values (heights, speed, hazards) are untouched. The hazard ring thickness is now a share of its diameter (`HazardBand`).
- **Verification.** A temporary editor probe (removed) logged arm-tip world positions through a simulated 60 fps climb (`evidence/grip_probe.txt`). While a hand holds, its tip's x and y stay constant to the millimetre (e.g. `(-0.807, 15.554)` over 21 frames); only depth varies. Each hold lasts about 0.35 s and each reach about 0.2 s.

Checks: APK built (`Logs/grip-android1.log`, Succeeded, Errors=0), SHA-256 `93b136718be5a66a17e806a272a258539654565fe4cd852d93c0ae8c5dcaaf54`; the installed base.apk hashes identically. EditMode 25/25 (`evidence/grip_editmode.xml`). Device recording with no Unity errors in logcat.

Evidence: `evidence/grip_climb_device.mp4`, `evidence/grip_climb_device_frames.png` (0.6 s at 30 fps), `evidence/grip_climb_preview.png` (editor simulation at 20 fps).

Consequences to review:
- On-screen climb pace is about 4.5x slower than before for the same level data. The level's 30 units now span about 11 body heights over 12 s. The parallel difficulty work may want different heights or speeds.
- PlayerMotor's 1.5-unit knockback is now about 0.55 body heights (was about 2.5), so a hit reads smaller.
- Webhook hit/recovery, audible impact, lose/retry and pause-during-recovery were not re-checked in this pass.

### 2026-09-28 — Climb pace as a single variable (Claude)

The candidate accepted the planted-grip climb feel and asked for climb speed to be one variable that everything else follows. They chose a global pace, with level heights and hazard spacing scaling so each level keeps its duration.

- **The variable.** `Assets/Game/Levels/ClimbPace.asset` (`Game.Core.ClimbPace`), field `bodyHeightsPerSecond`: the on-screen climb speed in climber body heights per second, range 0.4–2, default 0.92 (the accepted feel). The scene builder writes the climber's measured height into the same asset and keeps the tuned pace across rebuilds.
- **Gameplay follows.** `GameSession.StartLevel` multiplies the level's authored speed and every authored distance by `ClimbPace.DistanceScaleFor(level)` = pace × body height ÷ authored climbSpeed: finish height, hazard band heights, and PlayerMotor's knockback (`PlayerMotor.DistanceScale`). Each level therefore takes exactly as long as authored, and hazard periods and windows are unchanged. The HUD shows altitude in the level's authored units (`GameSession.DistanceScale`), so its numbers stay the same at any pace (goal "3.000"). Without a pace asset, a level plays as authored.
- **Presentation follows.** The grab cadence already follows distance climbed. `ClimberPoseDriver` scales its swing, yank and leg-pendulum springs, twist rate and timed reaches by `ClimbPace.PresentationRate` (pace ÷ 0.92), and the springs now substep at 120 Hz so they stay stable at fast paces. `CameraFollow` scales its follow rate the same way, so the camera trails by the same share of the climber at any pace.
- **Scene sized for any pace.** `Level1SceneSetup` builds the tower and cloud field for the fastest allowed pace (82 units at the current scale), so changing the pace needs no rebuild.

Checks: APK built (`Logs/pace-android2.log`, Succeeded, Errors=0), SHA-256 `f63e51563c87f271d61bd709875bbeabb2efee023db2033cf3900ddcfe65e8e6`; the installed base.apk hashes identically. Device climb at the default pace with no Unity errors; HUD goal reads 3.000 (`evidence/pace_device_hud.png`). EditMode 27/27 (`evidence/pace_editmode.xml`). The two new `ClimbPaceTests` cover the scale factor and a session run at pace 1.5: speed ×1.2, finish ×1.2, unchanged duration, knockback ×1.2. An editor simulation at pace 1.8 (`evidence/pace_1.8_preview.png`) keeps planted reaches and a stable swing; the asset was restored to 0.92 afterwards.

Integration note: `GameSession`'s changes touch the same lines as `codex/sequential-levels` (level speed/finish/hazards). The merge should apply `_distanceScale` to `CurrentLevel` the same way. The difficulty branch's five levels all author climbSpeed 2.5 and finish 30, so they scale uniformly.


### 2026-09-28 — Parallel level work cancelled

User cancelled the campaign work because gameplay will change; resume only on new instruction. No campaign agents remain active. Runtime draft commit `c38b604` remains on `codex/sequential-levels`, with uncommitted test drafts; configuration utility drafts remain in `codex/level-difficulty`. These were not merged into main by this task, and no Unity tests/build/device run was performed for them. Preserve the drafts without treating them as the approved future design.


### 2026-09-28 — Cancelled campaign worktrees removed

At the user’s request, removed `/private/tmp/tower-sequential-levels` and `/private/tmp/tower-level-difficulty`, including uncommitted drafts, and deleted `codex/sequential-levels` and `codex/level-difficulty`. Main workspace and the unrelated `claude/bump-types` worktree were left untouched by the removal. This supersedes the preceding draft-preservation note.


## 2026-09-28 — requested worktree integration

Merged `claude/level-json` (`85712a8`) and `claude/remove-lives-hazards` (`d30abc0`) into main. Both worktrees were committed before integration. Resolved the retry test to bind JSON without hit points, and regenerated Level1 through `Game.Editor.Level1SceneSetup.Build` with the merged source. Existing local planning/status edits were preserved.

Validation: scene regeneration and compilation succeeded; EditMode **53/53 passed**, zero failed/skipped (`/tmp/tower-merge-editmode.xml`, log `/tmp/tower-merge-editmode.log`). Reference snapshot checksums all passed. No Android build or device acceptance was performed; existing milestone gates are unchanged. The merged branch removes hazard-driven loss, so the brief's expected lose state still needs a gameplay decision. Branches and worktrees remain available.

## 2026-09-28 - Typed bump events (worktree claude/bump-types)

`/bump` now takes optional `polarity`, `type` and `tag` (query, JSON or form body); see `BUMP_EVENTS.md`. `BumpCatalog` holds the types and defaults; `PlayerMotor.ApplyBump` lifts or drops the climber; `GloveBurstView` became `BumpBurstView` (mirrored for negative); the feed is two columns (positive left, negative right). EditMode suite in batch mode: 68 total, 68 passed, 0 failed (`Logs/editmode-results.xml`). Device run not done.

Review follow-ups (same day): rich text off on card lines, single-line ellipsis for long tags, card pop multiplies the built scale, badge cleared for icon-less types, invulnerability covers the whole eased move, stacked bumps accumulate, empty values/BOM/format characters handled by the parser, duplicate defaults removed (catalog owns them), burst direction frames rendered (`Logs/previews/burst_positive.png`, `burst_negative.png`). EditMode 74 total / 74 passed / 0 failed (`Logs/editmode-results.xml`).


## 2026-09-28 — bump rework integration

Merged `claude/bump-types` (`273db0e`) into main after the JSON and hazard changes. Resolved session/setup conflicts without restoring lives, adapted the bump session tests to JSON, and kept hazard clearance targets alongside stacked signed bump displacement. Added a regression for a bump during hazard knockback. Regenerated Level1 through Unity with the JSON level, bump catalog, two event-feed columns and updated hazard body height.

Validation: compilation and scene regeneration succeeded (`/tmp/tower-bump-merge-scene.log`); EditMode **101/101 passed**, zero failed/skipped (`/tmp/tower-bump-merge-editmode.xml`, log `/tmp/tower-bump-merge-editmode.log`). Reference checksums all passed. No Android/device validation or milestone gate change. Pre-existing local documentation edits are preserved separately from this merge.

### 2026-09-28 — G2 follow-up: bumps you can see, tower ends at the finish (Claude)

The candidate watched a device run of the integrated build (APK `4ab1eb8a…`, EditMode 101/101) and reported that bumps did nothing and that the tower continued past the finish. Implemented by two parallel Sonnet 5.5 agents with disjoint file ownership; Claude integrated, ran Unity and tested on the device.

- **Bumps move the climber by a visible distance.** `BumpType` distances are now `liftBodyHeights` / `dropBodyHeights` in climber body heights, per type (boxing = 1, user's choice; previously 1.5 authored units ≈ 0.55 body heights). The move starts when the first glove lands (`GameSession.bumpImpactDelaySeconds` 0.275, the single timing source for `BumpBurstView`), lasts `bumpMoveSeconds` 0.6 (hazard knockback stays 0.35), and the camera follows at 0.25× during it, so the climber visibly travels on screen. Impact glow alpha 0.95→0.5 and scale 1.2→0.8; full-screen flash 0.65→0.3. An upward bump plays a boosted pose (arms overhead) instead of the hit flail. Pending bumps freeze with pause and are dropped on level start.
- **The tower ends at the finish.** `TowerSummit` places the crown at each level's finish plus a hand-reach lip (0.85 body heights) at runtime, ends the shaft there and hides collars/windows above. The scene is still built for the tallest finish at the fastest pace, so five levels and pace changes need no rebuild. On a win, `SummitSlideView` slides the climber over the lip onto the crown over 0.8 s, turning to face the camera. The win panel waits for the slide and its Exit button moved to the bottom.
- **Review fixes.** A backgrounded app pauses (`/bump` answers 409 instead of a 503 timeout); returning to the menu resets the climber to the base.

Checks: scene regenerated via `Level1SceneSetup.Build`; EditMode **123/123 passed** (`evidence/bumpfeel_summit_editmode.xml`). One new summit test asserted a piece above the crown stayed visible; the test was wrong (code correct) and was fixed. APK built (Succeeded, Errors=0), SHA-256 `4229fe2d071da42e97bb86019423f8e54f757e4c411bc92260e1acc654b5602c`; the installed base.apk matches. Reference checksums all OK.

Device (moto g 5G plus `0070013699`), no Unity warnings/errors in logcat:
- Positive POST mid-climb: altitude 701 → 973 (+272 = one body height), gloves from below, boosted pose, camera catches up (`evidence/bumpfeel_positive_frames.png`, 0.2 s steps).
- Negative POST: gloves from above, climber drops on screen. The same run also recorded the first device hazard hit: the band turned active over the climber during the 0.275 s impact delay (knocked to 701), then the bump stacked (→ 429) (`evidence/bumpfeel_negative_frames.png`, `evidence/bumpfeel_device.mp4`).
- Summit: tower top comes into view, climber slides onto the crown and stands facing the camera, "SUMMIT REACHED" then appears with Exit below (`evidence/summit_climb_1fps.png`, `evidence/summit_standing.png`, `evidence/summit_device.mp4`).
- Menu/won/paused/backgrounded requests return 409; Exit returns to the menu with the climber at the base; foregrounding shows the pause panel.

Not verified: impact audio audibility (not listened to); the `MenuView` win-panel delay has no automated test; 1080x1920 layout. Nothing committed yet.

### 2026-09-28 — G2 follow-up: code-quality review fixes for expandability (Claude)

Acted on a read-only code-quality review (7/10; priorities: lifecycle correctness, PlayMode transition tests, split scene construction). Four parallel Sonnet 5.5 agents (`claude-sonnet-5-5`) with disjoint file ownership; Claude integrated, ran Unity and diffed the scene. No gameplay tuning, no five-level work.

- **Lifecycle seam.** `GameSession.LevelStarted` (every StartLevel/Retry) and `GameSession.PlayDeltaTime` (0 unless Playing). A time-based effect advances by `PlayDeltaTime` to freeze on pause, and cancels on `StateChanged` (Menu/Won) + `LevelStarted`. `BumpBurstView` uses it: pause freezes gloves, flash, camera shake and impact audio; menu, win and restart cancel all of it synchronously (`BumpBurstView.Cancel`, `CameraShake.Paused/Cancel`). Same audit fixed `EventFeedView` (cards kept fading under the pause panel and survived a restart; now they hold while paused and clear on level start), `CameraFollow` (snaps on level start/menu instead of gliding down from the summit; new `session` binding), and `ClimberPoseDriver` (froze on `motor.Paused`, which is also set on Won, so the win-arms pose never played; now freezes only on session Paused and resets its blend on restart). **Visible change to verify on device: arms-up win pose on the summit.**
- **Dead lose state removed.** `SessionState.Lost`, `MenuView.losePanel`, the GAME OVER panel, lose pose fields and preview shot; `GameSessionLoseRetryTests` → `GameSessionRetryTests`.
- **Listener hardening.** Active clients tracked; at most `QueueCapacity + 4` (20) concurrent connections, extra ones get an immediate `503 {"error":"busy"}` without reading them; `Stop()` closes client sockets, rejects queued/in-flight requests, joins accept thread and workers within 1 s total; Start after Stop rebinds; Faulted not raised for shutdown errors.
- **Level validation.** `HazardSpec.Validate(finishHeight)`: 0 < height < finish, period > 0, 0 < active < period, finite phase; non-finite finish/speed rejected. Errors name the entry, e.g. `hazards[1].activeSeconds`.
- **Scene builder split.** `Level1SceneSetup.cs` 1,569 → 70-line orchestrator over `Editor/Level1/*` (paths, palette, layout, assets, environment, tower, player, camera, UI, HUD, burst, wiring) and reusable `Editor/Scene/*` (`SceneBinding` fail-loud binder, `SceneWiringCheck` post-build check that fails the build on any unbound Game component object field, `SceneAssetUtil`, `UiKit`). Adding a layer/panel/HUD element = one builder method called from the orchestrator.
- **PlayMode tests** (new `Game.Tests.PlayMode`): load the real Level1 scene, drive UI buttons and real HTTP to 56789: wiring, menu→play, pause/resume, 409 in menu/paused, bump burst + climber move + cleanup + continued climbing, pause/menu/start/retry/win during a burst, win panel → exit at height 0.

Checks: all four csproj compile. Scene regenerated (`Logs/expand-scene.log`, wiring check passed); canonical scene diff vs the previous scene shows only the removed LosePanel subtree, removed lose pose fields, removed `losePanel` binding and the new `CameraFollow.session` binding. EditMode **147/147** (`Logs/expand-editmode.xml`). PlayMode **12/12** (`Logs/expand-playmode.xml`). Mutation check: with the previous `BumpBurstView`, exactly the 5 pause/transition tests fail (`Logs/expand-playmode-mutant.xml`). Reference checksums all OK. Not done: Android build/device run, win-pose visual check.

Build + device (same day): APK built (`Logs/expand-android.log`, Succeeded, Errors=0), SHA-256 `8101cfd225973fee80c353b1194a805945c7a50fce41f243830a9ff3518bdbd1`; installed base.apk matches. On moto g 5G plus `0070013699` via `adb forward tcp:56790 tcp:56789`: menu 409; positive POST 200 (altitude 0→192, burst over the climber, `evidence/expand_burst.png`); default GET 200 negative (back to 0, burst cleared, `evidence/expand_after.png`); pause mid-burst freezes it (two frames 1.5 s apart byte-identical, `evidence/expand_paused2.png`), paused POST 409, Continue finishes the burst (`evidence/expand_resumed.png`); held climb to the summit, arms-up win pose on the crown (`evidence/expand_summit.png`), won POST 409; Exit returns to the menu with the climber at the base (`evidence/expand_exit_menu.png`); backgrounded POST 409. No Unity warnings/errors in logcat. Open: the frozen burst draws over the pause panel (burst root is above PausePanel in canvas order).

Pause-panel fix (same day, Editor-validated only): the scene builder now makes `PausePanel` the last canvas child, so its dim and buttons draw over a frozen burst (`Level1Ui.Build`). Scene regenerated (`Logs/expand-scene2.log`, wiring check passed). In the live Editor: StartLevel, POST `/bump` 200, pause mid-burst → burst dimmed under the panel, Continue/Exit on top (`evidence/expand_editor_paused_burst.png`, 1280x720 Game view); Resume → burst finishes (0 active elements), no console errors. PlayMode 12/12 in the Editor. Not rebuilt for Android: APK `8101cfd2…` predates this fix.

### 2026-09-28 — G2 follow-up: pause UI removed (Claude)

The user decided to remove the pause feature because neither reference (ref.png, ref.mp4) shows one. The HUD pause button and PausePanel are gone (`Level1Hud`, `Level1Ui`, `MenuView.pausePanel`, preview capture). **Brief note:** §3 lists "pause" among the menus to match; this follows the references over that list and should be stated in the README. `GameSession.Pause/Resume` stay as the internal freeze used when the app is backgrounded; since there is no Continue button, returning to the app now resumes (`OnApplicationPause(false)` → `Resume`), which avoids a frozen game with no way out. The previous pause-panel draw-order fix is superseded.

Editor-validated only: scene rebuilt in the live Editor (wiring check passed; scene diff removes only PauseButton and the PausePanel subtree). Play mode: HUD without pause button, positive POST 200 lifts to 192 with the burst (`evidence/editor_nopause_hud.png`, `evidence/editor_nopause_burst.png`); background → Paused, return → Playing. EditMode 147/147, PlayMode 11/11 (button-driven pause test removed; session-level freeze tests kept for backgrounding). Android APK `8101cfd2…` predates this.

### 2026-09-28 — G2 follow-up: five sequential levels (Claude)

The user asked for sequential levels: four new ones for a total of five, each passed by reaching the summit. (The user withdrew their question about lose behaviour: the game has no lose condition.) Three parallel Sonnet 5.5 agents (`claude-sonnet-5-5`) worked on disjoint files. Claude integrated the work, fixed two test bugs, renamed the final title and ran Unity in the live Editor.

- **Campaign runtime.** `GameSession.levelFiles` (ordered `TextAsset[]`) replaces `levelJson`. New members: `LevelIndex`, `LevelCount`, `CurrentLevel` (parsed lazily per level), `IsFinalLevel`, `StartCampaign()` (main menu Start) and `StartNextLevel()` (only from Won and not on the last level). `StartLevel`/`Retry` restart the current level. Stale `/bump` requests from the previous level are rejected by the existing instance id. An empty campaign throws.
- **UI.** The win panel "SUMMIT REACHED" has Next Level (primary) and Exit. After level 5, `FinalWinPanel` "TOWER CLEARED" shows Exit only (`MenuView.finalWinPanel`). HUD `LevelLabel`, top centre: "LEVEL n/5" over the level name.
- **Levels** (`Assets/Game/Levels/Level2..5.json`, all climbSpeed 2.5). Simulated clean runs, per the level-data agent's report:
  - 2 Higher Climb: finish 40, 4 bands, 20 s
  - 3 Rhythm: finish 50, 6 bands with alternating phases, 28 s
  - 4 Pressure: finish 60, 3 close pairs, 33 s
  - 5 Summit: finish 70, 9 bands, 44 s
  - Level 1 is unchanged at 15 s.

  The smallest safe window is 1.7 s against a 1.09 s band crossing. Levels 1–4 sit below the plan's duration targets.
- **Scene.** The tower and clouds are sized for the tallest level at the fastest pace: 54 storeys, 170 units. The cloud count scales with height (cap 150). The scene was regenerated with `Tower/Build Level 1 Scene`, and the wiring check passed.
- **Tests.** New `GameSessionCampaignTests`: advance, no-ops, retry, menu restart, stale bump, empty/malformed campaign. New `LevelCampaignDataTests`: parse/progression, safe windows, and a cautious-climber simulation proving every level completes without help or hits, with clean-run times increasing. Two PlayMode flow tests: Next to level 2 with a real HTTP bump, and all five levels to the final panel, then Exit and restart at level 1.

Checks (live Editor 6000.3.11f1): EditMode **159/159** (`evidence/campaign_editmode.json`), PlayMode **13/13** (`evidence/campaign_playmode.json`). In Play mode:
- level 2 HUD (`evidence/campaign_level2_hud.png`)
- win panel with Next Level/Exit below the standing climber (`evidence/campaign_win_next.png`)
- level 5 final panel (`evidence/campaign_final_win.png`)

Reference checksums all OK.

Android build + device (same day): APK built through the live Editor (`Tower/Build Android APK`, Succeeded, Errors=0), SHA-256 `261e913853fa13de9da0977d51dd6919a883e087974a157ce5661799cf310bce`; installed base.apk matches. On moto g 5G plus `0070013699` (Android 11), via `adb forward tcp:56790 tcp:56789`, a scripted run used only real touch (tap Start/Next Level; 4 s holds in the climb region, with no hazard timing) and real HTTP:
- **All five levels completed in sequence.** Blind-hold times including hazard setbacks: about 22, 41, 70, 92 and 103 s (recording lengths minus about 4 s of overhead). Level 4's 6 s frames show red-band knockbacks, stalls and recovery, ending on the crown (`evidence/campaign_device_level4_6s.png`).
- **Panels.** Win panel with Next Level/Exit after levels 1–4, "TOWER CLEARED" after level 5. Exit returned to the menu with the climber at the base, and Start restarted at "LEVEL 1/5".
- **Bumps on level 2.** Positive GET 200: gloves from below, 1.292→1.316. Negative POST 200: gloves from above, 1.316→1.044. Default GET 200.
- **409 responses** in the menu and on every win/final panel. A positive bump after the restart returned 200.
- **Logs.** No Unity warnings or errors in logcat for the game process.

Evidence: `evidence/campaign_device_sheet.png` and `evidence/campaign_device_all5.mp4` (all five levels, 360p, no audio).

Not done: hand-played feel and difficulty tuning by a person. Audio not listened to. Nothing committed.


### 2026-09-28 — G2 closure (Codex; user-confirmed acceptance)

**G2 PASS.** The user reported: “The tests just finished and all your 4 points passed, you can close G2”. This confirms the current-build acceptance checklist:

- Source/build provenance and installed APK hash match.
- Real bare GET/POST and positive/negative bumps, visible displacement, and continued climbing after a finite rapid burst.
- Background/foreground recovery and cleanup across menu/restart, with the user-requested pause UI removal respected.
- Actually heard impact audio and reviewed current device visuals.

Latest Claude build report identifies APK SHA-256 `261e913853fa13de9da0977d51dd6919a883e087974a157ce5661799cf310bce`, matching installed base.apk on moto g 5G plus `0070013699` (Android 11). It records campaign tests of 159/159 EditMode and 13/13 PlayMode and `evidence/campaign_device_all5.mp4` / `evidence/campaign_device_sheet.png`. The recording has no audio; audio acceptance and the complete four-point checklist are attributed to the user's subsequent confirmation, not inferred from that recording or independently rerun by Codex. That confirmation supersedes the preceding report's audio-not-listened-to limitation for G2 acceptance.

No Editor/build/device operations were performed for this closure. G3–G7 are not promoted by this decision. Earlier PARTIAL assessments and schedule estimates remain historical; next checkpoint is G3 review, including player feel/difficulty and a revised remaining-delivery forecast.

Advisory Jev request prepared at `jev/20260928T230037619018Z-G2`; local APK hash matches the report. API check unavailable because `TYPESAFE_API_KEY` is unset. Per the evidence workflow this is nonblocking; no Jev verdict is claimed.


### 2026-09-28 — G3 closure (Codex)

**G3 PASS** for the current five-level campaign. [Review and evidence limits](G3_REVIEW.md): source and saved device evidence support five distinct playable levels, sequential wins, final completion, menu return and restart. Local APK hash matches Claude’s reported installed artifact. Saved results report 159 EditMode / 13 PlayMode passes; the EditMode export needs format cleanup for packaging. No tests or device session rerun.

The user will improve levels and game feel before freeze; this is explicitly retained as tuning work rather than an unfulfilled G3 functional requirement. G4–G7 remain unchanged. Revalidate affected content after that pass. Remaining ETA depends on the user’s tuning scope; the earlier clock schedule is historical, not a refreshed forecast.

Advisory Jev request: `jev/20260928T230555106520Z-G3`. API unavailable (`TYPESAFE_API_KEY` unset); no Jev verdict claimed. Nonblocking per the evidence workflow.

### 2026-09-28 — five-level difficulty and feel tuning (Codex, delegated Sol agents)

Completed a focused data pass requested by the user, keeping exactly five levels and the accepted motor/control feel. Hazard counts now rise **2/3/5/6/8**, with green windows **3.7/3.7/3.4/3.1/2.8 seconds** and longer safe gaps between groups. Finish heights and global pace stay unchanged. The minimum supported pace can now cross every band; former late-level windows were shorter than the body crossing time at that setting. See [tuning rationale and evidence](LEVEL_FEEL_PASS.md).

Fresh executed checks: **162/162 EditMode and 13/13 PlayMode passed**, zero failed/skipped, with XML in `evidence/level_feel_editmode.xml` and `evidence/level_feel_playmode.xml`. Clean simulated durations measured by Unity are **14/19/27/34/43 seconds**. New coverage checks the five-file cap, supported pace range, 30/60 FPS and delayed starts. A stale Level 1 authored-value assertion was updated; an unrelated intermittent HTTP response-body test failure passed on the next full run without a transport change. Independent scoped review approved. All six immutable reference checksums passed.

This is a source/data tuning result, not a new G4/G5 acceptance decision. Final Android touch-play, subjective feel and build-linked device regression remain necessary before freeze. Earlier G2/G3 device evidence applies to the previous APK. Pre-existing STATUS, G3_REVIEW and ProjectSettings edits were preserved.

### 2026-09-28 — lives and pause menu restored behind feature flags (Claude, delegated Sonnet 5.5 agents)

At the user's request, the removed lives/lose feature and pause menu are back as **off-by-default feature flags** in `Assets/Game/Levels/GameFeatures.asset`: the brief describes them, but the reference does not show them. With both flags off, gameplay is unchanged. Lives: hazard hits cost a life (webhook bumps never do). At zero the session enters `SessionState.Lost`, the climber takes the restored lose pose and the GAME OVER panel offers Continue (retry the level with full lives) or Exit. The HUD shows up to five primitive-built hearts under a top-right pause button. Pause menu: the HUD `II` button opens the PAUSED panel (Continue/Exit). Backgrounding stays paused on return instead of auto-resuming. Views read the flags at runtime; the scene builder creates the asset only if missing and never overwrites it.

Three parallel agents did the work: runtime, scene builder and tests. The lead fixed one test-setup error (the webhook test had no BumpCatalog, so the fallback logged an error), regenerated Level1 in the running Editor and ran both suites there: **179/179 EditMode, 21/21 PlayMode passed**, including 17 new EditMode tests and 8 new PlayMode tests. `GameFeatures.asset` still reads both flags off after the runs. Flag-on flows (HUD hearts and pause button, PAUSED, GAME OVER) were captured in play mode with an in-memory flags object: `evidence/editor_flags_on_{hud,pause,lose}.png`. The Game view was 1280×720 landscape, so the portrait HUD placement still needs checking on the device. No Android build or device run was done for this change.

Follow-up the same evening: the user asked for both flags **on** in the shipped `GameFeatures.asset` (the class defaults stay off). The PlayMode check on the shipped asset now expects both on, and the two flags-off flows bind their own in-memory flags. Rerun in the Editor: **179/179 EditMode, 21/21 PlayMode passed**.

### 2026-09-28 — game settings moved into one asset (Claude, delegated Sonnet 5.5 agents)

At the user's request, every tuning value now comes from one asset, `Assets/Game/Settings/GameSettings.asset`. The asset has sections for features, pace, bump types, motor, input, camera, HUD, summit, burst, climber pose and webhook port. It replaces the separate ClimbPace, BumpCatalog and GameFeatures assets, which are deleted, plus about 60 tuning fields that lived on 14 components and two builder constants (CameraShakeMagnitude, SummitSlideSeconds). Before this change, tuning lived mostly as component field defaults that each scene rebuild copied from code, so a value tuned in the scene was lost on rebuild. The asset survives rebuilds; the builder writes only the measured body height. The win-panel delay and the summit slide now share `summit.slideSeconds`.

Migration was lossless: every scene value equalled its code default except camera shake, which the builder derived as 0.35 × WorldScale 4.5 = 1.575, now its default. Pace 0.92, body height 2.725, the boxing type and its glove icon/tint, and the lives and pause flags (on) all carried over. Rebuilding over the existing asset left it byte-identical. `ClimbPace.bodyHeight` now defaults to 0 ("unmeasured"), which plays levels as authored, so a component without an asset behaves as before. The lead fixed two integration errors: a per-level bump-type refresh that threw in EditMode because the listener is only created in Awake (reverted to Awake-only, as before), and one test that still wrote the old delay field. Rerun in the Editor: **191/191 EditMode, 22/22 PlayMode passed**, including new `GameSettingsTests` (pinned defaults) and a wiring test requiring every Level1 component to reference the one shipped asset. No Android build or device run.



### 2026-09-28 — player feedback categorized and ordered (Codex; planning only)

Incorporated all ten feedback items into [ACTION_PLAN §2A](ACTION_PLAN.md#2a-ordered-player-feedback-backlog) and synchronized [CLAUDE_HANDOFF.md](CLAUDE_HANDOFF.md). Order: full-screen controls (#2), EXIT readability (#1), stronger community bumps (#9), obstacle shake (#4), grab particles (#3), reactive height HUD (#5), basic victory fanfare (#6), supported haptics (#7), audio/music (#8), and optional win-camera choreography (#10). Categories, P0–P3 priorities, dependencies, device acceptance and cut order are recorded. Essential audio is integrated with the corresponding effects; background music and the elaborate orbit are optional.

Refreshed stale plan baseline to G2/G3 PASS, five implemented levels, shipped lives/pause enabled and the single settings asset. Prior clock estimates are historical; expanded polish needs a revised estimate. No gate changed. Implementation, tests, Editor/build/device operations and subjective acceptance were not performed for this update. Read the immutable brief and inspected its image plus sampled video frames; all six reference checksum entries passed. Existing implementation edits were preserved.


### 2026-09-28 — Unused asset cleanup (Codex, user-requested)

Removed the unused template `Assets/Scenes/SampleScene.unity`, `Assets/Readme.asset`,
and `Assets/TutorialInfo/` (welcome scripts, icon and layout), with their metadata
and the empty `Assets/Scenes` folder metadata: 17 files, 64,257 bytes total.
Audited GUID references across Assets, ProjectSettings, Packages and Tools, plus
script-based asset loading and build paths. Kept the build fallback Smoke scene,
configured input actions, both render pipelines and their SampleSceneProfile,
and the original character texture documented as recolour source material.
All gameplay art, audio and source/license records remain.

Validation: no retained project references to removed GUIDs; no orphaned `.meta`
files; all six immutable-reference checksums passed. Unity is running but its
Pipeline server is unreachable (not Safe Mode), so removal used the filesystem;
Editor reimport, compilation and gameplay were not executed. No milestone gate
changed. Existing planning/status edits were preserved.


### 2026-09-28 — Asset cleanup batch validation (Codex)

After the user closed the Editor and requested the batch workflow, Unity
6000.3.11f1 completed a direct `-batchmode -nographics -quit -executeMethod`
validation with exit code 0 and an explicit `CLEANUP_VALIDATION_PASS` marker.
Compilation succeeded; Level1 and Smoke opened successfully. Checked 805
GameObjects and 66 recursive dependencies: no missing scripts, broken serialized
component object references, missing dependency files, or deleted template assets
remaining in AssetDatabase. All six immutable-reference checksums passed again.
Temporary diagnostic/validation scripts and their metadata were removed.
This supersedes the earlier unverified import/compilation note; gameplay and
Android testing were not rerun. Pipeline discovery troubleshooting was abandoned
at the user's request. No milestone gate changed.


### 2026-09-28 — Project ownership transferred to Claude (user-requested)

Claude takes over Codex's planning, prioritization, forecasts, milestone/evidence review and blocker coordination, alongside implementation and integration, through remaining delivery gates G4–G7. Updated AGENTS, ACTION_PLAN, CLAUDE_HANDOFF, the current STATUS summary and the Jev/panel workflow documentation. No Codex approval remains required. Quota fallback returns ownership to Claude after recovery; the candidate retains final submission.

Existing implementation and documentation edits were preserved. Historical reports retain their original authors and evidence limits. No Unity, build, device or gameplay checks were run for this documentation handoff; no gate changed. All six immutable-reference checksums passed.


### 2026-09-28 — player-feedback backlog rows 1–10 implemented (Claude; five parallel Sonnet 5.5 agents, lead integration)

All ten ACTION_PLAN §2A rows now have an implementation; none is accepted yet. Five `claude-sonnet-5-5` agents worked in the same tree with disjoint file ownership. The lead first added shared seams: `GameSession.HazardHit`, `ClimberPoseDriver.HandPlanted`, six new settings sections (`cameraFx`, `grab`, `heightFx`, `victory`, `haptics`, `sound`) and three builder steps. The lead also owned the only Unity, build and device session.

- **Rows 1, 2, 6 (input, EXIT, height HUD).**
  - Holding anywhere climbs; the touch-region setting is removed. Touches that begin on a `Selectable`, or before climbing is allowed, are never claimed. Pause, background and transitions clear held input.
  - The display font is now Lilita One (SIL OFL 1.1): Kenney Future's X glyph is an H with small notches, so EXIT read as EHIT.
  - The height label tints green or red and scales with the actual altitude trend, using a deadband and hold. It resets on level start, menu, win and loss.
- **Rows 3, 4, 10 (bumps, hit shake, camera).**
  - The boxing bump is now 2 body heights up and 1.75 down (was 1 and 1).
  - A new `CameraEffects` owner drives, at glove impact:
    - a polarity-dependent zoom pulse (positive pulls out, negative punches in);
    - a per-channel shake that takes the maximum rather than the sum;
    - URP bloom and chromatic-aberration pulses in the new `Game.PostFx` assembly (post-processing is on only while a pulse is active).
  - Hazard hits get a smaller hit-channel shake.
  - On win, the camera pushes in with a 10° arc.
  - Lead fix: `CameraShake` now always zeroes its offset once no shake is active. The approximate `Vector3 !=` guard left a sub-tolerance offset, which two new tests caught.
- **Rows 5 and 8 (grab particles, haptics).**
  - Pooled sparks play at the real grip point. The agent corrected the lead's `GripWorldPoint` depth.
  - Grabs count only while climbing, so idle-finished reaches give no feedback.
  - Android Vibrator haptics: light on grab (rate-limited), heavy on hit, a win pulse, and a triple pulse on the final win. Enable and intensity settings; silent when paused or backgrounded. VIBRATE is present in the APK manifest (`aapt dump permissions`).
- **Row 7 (victory UI).** A heading pop with wobble and a pooled confetti burst (not a raycast target) play after the summit slide. The final TOWER CLEARED win gets a larger, longer, two-burst version. Everything clears on Next, Exit, Retry and menu.
- **Row 9 (audio).**
  - `GameAudio` plays a music loop (ducked while paused, won or lost) and pooled SFX with per-cue cooldowns and overlap caps.
  - Cues: grab, hit, positive-bump at impact, level-win and final-win jingles, and UI click (the lead wired it to every button).
  - Sources, all under `Assets/Game/Art/Licensed/Audio/*/LICENSE.md`: Kenney RPG Audio, Impact Sounds, Digital Audio, Music Jingles and Interface Sounds (all CC0), and cynicmusic "Happy Lullaby (song17)" (CC0, OpenGameArt). No generated audio.

**Validation**
- Scene rebuilt; SceneWiringCheck passed.
- Tests: **253/253 EditMode** and **22/22 PlayMode** passed (`evidence/feedback_editmode.xml`, `feedback_playmode.xml`).
- Preview capture reviewed: the Exit/Next labels are legible (`evidence/feedback_win_preview.png`).
- APK SHA-256 `8bed1c7631d10e2739a5b452bfd185c003b20b03d73445aad137e248df63ed2c`, installed on moto g 5G plus `0070013699`. The source is uncommitted work on top of `5532ffd`.
- Device smoke run over adb forward (`evidence/feedback_device_*.png`):
  - A hold near the top of the screen climbed.
  - A positive GET, a bare GET (negative, request ID `93d6436785544aa5aa52bb8460f750b7`) and a bare POST all returned 200/accepted.
  - The negative bump showed the punch-in, aberration, red height and glove card.
  - After the burst, a bottom hold climbed again with green height. A hazard hit during that climb took one life; the bumps took none.
  - No Unity or AndroidRuntime errors in logcat.
- Immutable reference checksums: all six OK.

**Not yet done.** Human feel, visual and audio review on the phone:
- bump distances and effect strengths;
- whether the victory arc exposes the tower;
- confetti and heading;
- whether the grab haptic tick is felt;
- the music loop point and mix.

Also still open: a five-level natural-play regression, a five-minute performance run, and the G4/G5 freeze. No gate changed.


### 2026-09-28 — bump punch volley SFX (Claude, user-requested)

The bump now plays a punch volley. The main impact punch still plays when the first glove hits. Each later glove that passes the climber adds a punch from nine more Kenney Impact Sounds clips (CC0; the same pack, verified by the byte-identical `impactPunch_heavy_000.ogg`).

- The volley is spaced at least 0.055 s apart, caps at 7 punches per bump counting the impact punch, and plays at 0.6 volume.
- Tuning lives in `burst.volley*`; `PunchVolley` holds the spacing logic.
- The volley freezes with pause and stops on cancel.

Validation: scene rebuilt and wiring check passed. **258/258 EditMode** (including 5 new `PunchVolleyTests`) and **22/22 PlayMode** passed (`evidence/feedback_volley_editmode.xml`, `feedback_volley_playmode.xml`). APK SHA-256 `8786e31715bb0642208c559c0677e7f6947eac772273cb839fd462d592e0b3c8`, installed on `0070013699`. Not yet listened to on device.


### 2026-09-28 — G4/G5 closure (Claude, completing Codex's in-progress evaluation)

**G4/G5 PASS**, on the user's report: "I've validate all the levels, we're getting short on time, lets move on". The user validated all five levels on the phone. This closure records that decision. The evidence below is what Codex and Claude executed.

- **Build link.** The installed `base.apk` on moto g 5G plus `0070013699` (Android 11, arm64-v8a, 1080x2520) hashes to `8786e31715bb0642208c559c0677e7f6947eac772273cb839fd462d592e0b3c8`, the same as `Builds/Android/TowerPrototype.apk`. That source is commit `e64248e`. `ProjectSettings.asset` was re-saved 11 s after the build, but its content matches both the commit and its parent.
- **Tests (Codex, same source):** 258/258 EditMode and 22/22 PlayMode passed (`evidence/feedback_volley_*.xml`).
- **Device, touch only (Claude)** (`evidence/g45/`).
  - A closed-loop controller injected real touch-down/up events through monkey's network mode and read hazard colours from screen captures.
  - Main menu → Start. Level 1 won; Next advanced to Level 2, which was won in 28.5 s (`touch_win_L1.png`, `touch_win_L2.png`).
  - On Level 3, lives ran out and GAME OVER → Continue retried the level (`touch_gameover_L3.png`). The losses came from the controller's own 0.5 s capture latency near closely spaced bands, not from a game fault. Hits cost one life each; the retry restarted the level.
  - The run was stopped at the user's request after they validated all levels by hand.
  - Pause panel (`menu.png`) and main menu (`main_menu.png`) captured by Codex.
- **Not executed:** a separate five-minute performance capture, and a scripted pause/background cycle on this APK. Audio mix, haptics and effect strength are the user's judgment and are covered by their validation, not by new measurements here.

Freeze: from here, only blocker fixes. G6/G7 work started: a `Tower → Build Android APK (Release)` entry point (no Development overlay or script debugging), README with engine version, asset licenses, `/bump` instructions and the `adb forward` / `adb reverse` explanation, and a stale pause note in BUMP_EVENTS corrected.

Release APK built from this tree (`BuildScript.BuildAndroidRelease`, result Succeeded, 0 errors): `Builds/Android/TowerPrototype-release.apk`, 35.1 MB (the development APK is 139.1 MB), SHA-256 `80ae5c1cb491ec40227ed4eeef9bebd07770d60285115f0948a6d03d56697318`. Installed on `0070013699`. Device smoke test after the user unlocked the phone (the first attempt ran with the screen locked and is void). Frames are in `evidence/g45/release_smoke_sheet.png`:
- `/bump` on the main menu → `409 not_playing`.
- In Level 1: bare POST, bare GET and `?polarity=positive&tag=Viewer42` all → `200 accepted`, with resolved polarity negative/negative/positive.
- No Development Build watermark.
- The negative burst showed gloves, stars, flash and aberration. The positive burst lifted the climber to 451 with a green height tint and a `Viewer42` card.
- A hold afterwards climbed normally to 627. All three lives survived the bumps; a later blind hold into an active band took one.
- No Unity errors or crashes in logcat.


### 2026-09-29 — pause button fix (Claude, user-reported blocker)

**Bug.** The user reported that the HUD pause button did nothing. The scene saved its image with `raycastTarget` off, so taps fell through to the climb input. Cause: `Level1Hud.Build` removes raycasts from every HUD graphic except those that have a `Selectable` parent. The lookup used `GetComponentInParent<Selectable>()`, which skips inactive objects, and the HUD panel is built inactive. The lookup therefore missed the button itself. The earlier PAUSED screenshot came from the background path, which opens the same panel without the button.

**Fix.** Changed the lookup to `GetComponentInParent<Selectable>(true)` and rebuilt the scene; the wiring check passed. Added the PlayMode test `EverySelectable_HasARaycastTargetGraphic`. It **failed on the old scene**, naming `Canvas/HudPanel/PauseButton`, and passes on the rebuilt one.

**Validation.**
- **Tests:** 258/258 EditMode and 23/23 PlayMode passed (`evidence/g45/pausefix_*mode.xml`).
- **APK:** `TowerPrototype-release.apk`, 35.1 MB, SHA-256 `22cbf1966c10c5022b69258315c46b41ed77acd074c86bf79200b5f1daa44382`. It supersedes `80ae5c1c…`.
- **Device** (`evidence/g45/pausefix_device_sheet.png`):
  - Tapping II shows PAUSED, and `/bump` answers `409 not_playing` while paused.
  - Continue resumes, and the next `/bump` → `200 accepted`.
  - Pause → Exit returns to the main menu.
  - No Unity errors.
- The device was set to stay awake while on USB (`svc power stayon usb`). Revert with `adb shell svc power stayon false`.


### 2026-09-29 — delivery video (user-recorded)

The user recorded the §6.2 video on the device and shared it: https://drive.google.com/file/d/1cr_5mld6p29mh-v1Yg_5i94HKrBN_ZMF/view?usp=sharing. A signed-out request returned HTTP 200 with the file page for `Tower_Prototype.mp4` (video/mp4) and no request-access prompt. The video content was not reviewed here; the user recorded it. Link added to the README Deliverables section. Still open for G7: the APK download link and pushing the repository.
