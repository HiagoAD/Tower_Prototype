# Claude implementation handoff

Implement the project following [ACTION_PLAN.md](ACTION_PLAN.md). Codex is the planner and overall overseer; you own implementation, integration, and evidence. The candidate owns final submission and supplies device access.

Read the root `AGENTS.md`, the immutable brief, its reference image/video, and the plan before editing. Never modify anything under `Docs/Reference/Unity-technical-test/` or the original Notion page. All implementation assumptions belong in project documentation outside that directory.

Target submission: **September 28, 2026 at 22:00 Recife (September 29 at 01:00 UTC)**. The user's 09:00 UTC deadline is interpreted as **September 29**, eight hours later. Confirm the date/device during kickoff without delaying independent work. Update the schedule if starting late; preserve delivery time.

## First 90 minutes

1. Confirm actual device/emulator, ABI, and build/install access. Inspect the reference's opening and glove sequence with audio. Record the hold-to-climb, portrait, level/failure-rule assumptions.
2. Preserve existing user changes and establish a source-control baseline. Keep the installed Unity 6000.3.11f1 / URP / Input System versions. Diagnose the running Editor's Pipeline reachability; it is already installed. Use Unity-supported Editor operations for scene/prefab authoring.
3. Establish small shared contracts and the folder ownership in the plan. Start the APK smoke build path immediately. A successful host-only test does not resolve Android risk.
4. Source/import the selected existing free assets, preserving license files. No image/model/audio generation, paid assets, custom art pipeline, or prolonged asset search. The selected character has no climb clip: implement a small pose driver on its existing rig.
5. Produce **G1**: an installed APK whose in-game diagnostic counter responds to real GET and POST requests through `adb forward`. Replace the diagnostic-only proof with the full effect by G2.

## Parallel work, if quota permits

Use one lead and up to two bounded workers. Follow existing role/model instructions if delegating.

- **Lead/A:** contracts, game session, climbing/input/camera, level configurations, selected asset imports, scene/prefab wiring, tests/build coordination. Sole owner of the shared Unity Editor and project/package settings.
- **B:** `Assets/Game/Webhook/` and owned transport tests. Plain C# transport, request parsing, bounded dispatch, responses, cleanup. No Unity calls from network threads.
- **C:** `Assets/Game/Presentation/` and owned presentation tests. Menu/HUD view scripts, glove overlay, imported-rig pose, SFX. Send wiring instructions to the lead; do not edit shared scenes/prefabs or import assets independently.

Tell each worker that others are editing the project, to preserve unrelated changes, and to stay inside assigned ownership. Publish contracts before workers depend on them. Integrate compile-ready batches; do not run parallel play-mode/test/build sessions in one project.

If quota only allows one session, execute A → minimal B → minimal C until the Android vertical slice works, then expand to five levels. Use build time for documentation and review.

## Completion contract

- One scene with five explicit, distinct, completable level configurations.
- Imported modular tower/character, reference-inspired blue sky and camera framing, touch climb/idle, hit/fall and win/lose/retry.
- Main menu, five-level selection, HUD, pause/resume, win/next, lose/retry, return to menu.
- Both GET and POST `http://localhost:56789/bump` inside Editor and Android; six large boxing gloves, flash/shake/SFX, and reliable continued play. Every accepted request has visible feedback; stale requests cannot carry into a new level.
- Device evidence, focused tests, APK recording showing all five completions and the real webhook, README, asset licenses, and clean project deliverable.

Use `adb forward` for PC → Android. Document why the brief's `adb reverse` example is the opposite direction, without editing the brief. Stop Editor Play mode before forwarding host port 56789, or use host 56790 mapped to device 56789.

## Return to Codex at every gate

Update [STATUS.md](STATUS.md) and supply:

```text
Gate and result: G# — PASS / FAIL / NOT VERIFIED
Time remaining to 22:00 target:
Source revision / changed files:
APK path and device / OS / ABI:
Checks run, executed-test counts, results:
Screenshots / recording / logs:
Known gaps or assumptions changed:
Next action and ETA:
Decision needed from overseer, if any:
```

Escalate blockers after 15 minutes with a concrete fallback. Continue independent work while Codex reviews. Do not claim a gate passed from compilation alone, and do not start optional features before all required functionality is proven on Android. Feature freeze is 16:00; preserve a tested APK and begin recording by 18:00.
