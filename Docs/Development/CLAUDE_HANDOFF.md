# Claude project ownership handoff

Use [ACTION_PLAN.md §2A](ACTION_PLAN.md#2a-ordered-player-feedback-backlog), updated September 28 with the ordered player-feedback backlog. At the user’s request, Claude now owns planning, prioritization, milestone review, implementation, integration, and delivery coordination. No Codex planning or review checkpoint is required. Quota fallback remains available; Claude resumes full ownership after recovery. Keep a single Editor/build/device owner. Preserve concurrent work. This handoff updates the plan; it does not launch another agent or restore deleted worktrees.

## Responsibilities transferred — September 28, 2026

Maintain the action plan and backlog, choose the next work, revise estimates, resolve blockers, review source/test/device evidence, and record milestone decisions in STATUS. Own G4–G7 through freeze, regression, recording, README/licenses, packaging and delivery-access verification. The candidate retains subjective feedback and final submission. Preserve existing G2/G3 acceptance and historical review attribution; the handoff passes no new gate.

Start by reconciling the current working tree with the backlog. At handoff, uncommitted work exists in GameSession, GameSettings, ClimberPoseDriver, Level1SceneSetup, the new Core/Settings directory, and Level1Audio/Level1CameraFx/Level1GrabFx builders, as well as these development documents. Inspect and integrate that work before implementing overlapping changes; its presence does not prove completion or validation. Check tool availability before taking exclusive Editor/build/device control. The latest recorded cleanup batch validation passed compilation and scene/dependency checks; it did not rerun gameplay or Android acceptance.

Then complete the ordered feedback batches below, collect fresh build-linked Android evidence, and reforecast the remaining delivery work. Retain the immutable brief, no-generation constraint, shared settings asset, shipped lives/pause, and evidence requirements.

## Gameplay decision

This is a social climbing game: viewers send positive bumps to help or negative bumps to remove progress. **Social bumps cause lost height without life loss. Hazard hits use the restored lives/lose system, enabled in the shipped settings asset.** The earlier no-terminal-loss decision is superseded by the feature-flag decision below.

**Optional lives and pause menu (user decision, September 28 evening).** The user asked for both removed features back behind feature flags, because the brief describes them but the reference does not show them. The `features` section of `Assets/Game/Settings/GameSettings.asset` holds `livesEnabled` (hazard hits cost a life; at zero the GAME OVER panel offers Continue to retry the level, or Exit), `startingLives` (1–5) and `pauseMenuEnabled` (HUD pause button and PAUSED panel; backgrounding then stays paused until Continue). The user then turned both **on** for the shipped build (the class defaults stay off; the asset sets them). Views read the flags at runtime, so toggling needs no scene rebuild, and the scene builder never overwrites the asset's tuning. Webhook bumps never cost a life. Do not remove either feature again or change the shipped values unless the user says so.

**One settings asset (user decision, September 28 evening).** Every tuning value the game reads lives in `Assets/Game/Settings/GameSettings.asset` (`GameSettings`: features, pace, bump types, motor, input, camera, HUD, summit, burst, climber pose, webhook port). Components reference it through a `settings` field and read `GameSettings.OrDefaults(settings)`. Put new tuning there, never on a component or in a builder constant. The builder creates the asset only if missing and writes only the measured `pace.bodyHeight`. A PlayMode test fails if any component in Level1 references a different asset. Scene measurements (body height, tower and summit geometry, camera offset), Level1Layout composition constants, level JSON and BumpListener's transport limits stay where they are.

Hold/release controls, accepted planted grips/body swing, global pace, hazard setbacks and clear summit wins remain. Finite request bursts must end with controllable play; endless hostile requests can intentionally delay progress. Positive help near the summit may win normally. No streaming-platform connector, relay or audience UI is in this scope.

**No pause feature (user decision, September 28).** The user decided to remove pause because neither reference (ref.png, ref.mp4) shows one. This is superseded by the pause-menu flag above, which now ships on: the HUD has a pause button and PAUSED panel, and returning from background stays paused. With the flag off, backgrounding still freezes play internally (`/bump` answers 409) and returning resumes automatically. The brief's §3 menu list, which names pause, remains unchanged; document this deliberate decision in the README alongside the lose-state interpretation.

## Current baseline and next work

G2 and G3 have passed. Five sequential levels and final completion exist. Latest reported settings-consolidation checks: **191/191 EditMode, 22/22 PlayMode**; no new Android evidence for the later tuning, restored features or settings consolidation. The earlier 17:45 forecast is historical.

Implement the ten feedback items in this order, with details and acceptance in ACTION_PLAN §2A:

1. **P0 usability:** full-screen gameplay holds (#2), excluding interactive UI; fix EXIT glyph readability (#1).
2. **P1 community moment:** stronger positive/negative displacement and synchronized glove/audio/camera effects (#9). Preserve default boxing contract, no life loss, boundaries and recovery. Evaluate zoom/FOV, aberration and bloom within device limits.
3. **P1 climb/hit feedback:** smaller obstacle shake (#4), particles on actual grabs (#3), altitude-direction tint and climb-scale HUD (#5).
4. **P2 supporting polish:** basic victory fanfare/tween/confetti (#6), supported light/heavy haptics (#7), licensed SFX/music (#8). Essential impact and victory audio can be integrated with their earlier batches; background music remains nice-to-have.
5. **P3 stretch:** choreographed win orbit and post-processing (#10), conditional on existing scene coverage, readability and performance. Use a front-facing arc/push-in if the scene cannot support an orbit; record the fallback.
6. **G4/G5:** current APK regression, player review, five-minute performance observation and freeze. Then **G6/G7:** recording, README/licenses, clean package and verified delivery access.

Put all new tuning in the one settings asset. Share grab timing across particles/audio/haptics and coordinate camera effects so overlapping events do not compete. All transient effects must freeze/cancel/reset appropriately on pause/background, loss, win, menu and next/retry. Preserve the accepted grip motion and current user settings.

Re-estimate after usability/core-feel batches; the previous 45–60-minute tuning estimate no longer covers the expanded scope. Protect at least 90 minutes for recording/package/access checks. Cut the elaborate win camera first if needed, then optional music/effects; record omissions. This ownership handoff does not itself implement or validate these changes.

## Completion contract

- Exactly five distinct, completable sequential levels; shared presentation and one gameplay scene.
- Main menu, HUD, touch climb/idle, readable hit/setback/recovery, shipped lives/loss/retry and pause, summit/next, final completion and menu return. Voluntary restart if retained resets the active level fully.
- Existing art direction from ref.png; existing licensed assets only, no generated art/audio. Final device visual approval and audible impact.
- Editor and Android local GET/POST `http://localhost:56789/bump`; default boxing burst retained. Positive/negative commands, visible feedback for accepted requests, bounded motion, no stale events across levels and continued control after a finite burst.
- Build-linked tests/device evidence, performance observation, all-five APK recording, README/licenses/attribution, clean source and verified single delivery link.

Use `adb forward` for PC → device; explain the brief's opposite-direction `adb reverse` example in README without editing the brief. Stop Editor Play or use host 56790 forwarded to device 56789.

## Coordination and checkpoints

One implementation session by default. Do not launch a standing team. Keep scene/import/test/build/device operations under one owner. Preserve unrelated changes and use Unity-supported authoring operations. Read AGENTS and the immutable brief/reference media before implementation; never alter the reference snapshot.

Update [STATUS.md](STATUS.md) at gates. Claude owns milestone reviews and blockers, including the remaining G4–G7 decisions, and prepares/reviews the Jev evidence checks at the documented checkpoints. Supply result, source/change manifest, APK hash, device/OS/ABI, executed test counts and XML, screenshot/recording/log paths, unresolved items and next ETA. Advisory [Jev](../../Tools/Jev/README.md) does not replace device/visual/audio review; API unavailability is nonblocking.

Escalate blockers after 15 minutes. Reforecast after reconciling in-progress work and after the usability/core-feel batches. Freeze once the complete candidate passes the feedback regression, then fix blockers only. Preserve recording/packaging time and record the actual final APK. No gate passes from compilation or elapsed target time alone.
