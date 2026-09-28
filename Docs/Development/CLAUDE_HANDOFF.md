# Claude implementation handoff

Ownership: Codex plans and reviews; Claude implements. If either one hits its usage limit while the other has quota, the available one covers both roles until the other returns (see the quota fallback rule in AGENTS.md). The same scope, reference and evidence rules apply either way.

Milestone evidence now includes an advisory Jev pass using [Tools/Jev](../../Tools/Jev/README.md). Implementer supplies a focused, build-linked report; Codex prepares/runs the evidence classification and independently reviews the underlying artifacts. Jev never changes gate status or substitutes for device/visual/audio checks. API unavailability does not block implementation or ordinary review.

Implement the project following [ACTION_PLAN.md](ACTION_PLAN.md). Codex is the planner and overall overseer; you own implementation, integration, and evidence. The candidate owns final submission and supplies device access.

**Current priority after G2 repair review:** the candidate reports misaligned visuals and incorrect gameplay feel. Follow [G3_FEEL_CORRECTION.md](G3_FEEL_CORRECTION.md) first: correct tower contact/framing and a readable climb/hit/recovery loop on one level, then obtain a short device-preview check before authoring the other four levels. This changes G3's execution order; all required scope remains.

Read the root `AGENTS.md`, the immutable brief, its reference image/video, and the plan before editing. Never modify anything under `Docs/Reference/Unity-technical-test/` or the original Notion page. All implementation assumptions belong in project documentation outside that directory.

Target submission: **September 28, 2026 at 22:00 Recife (September 29 at 01:00 UTC)**. The user's 09:00 UTC deadline is interpreted as **September 29**, eight hours later. Confirm the date/device during kickoff without delaying independent work. Update the schedule if starting late; preserve delivery time.

## First 90 minutes

1. Confirm actual device/emulator, ABI, and build/install access. Inspect the reference's opening and glove sequence with audio. Record the hold-to-climb, portrait, level/failure-rule assumptions.
2. Preserve existing user changes and establish a source-control baseline. Keep the installed Unity 6000.3.11f1 / URP / Input System versions. Diagnose the running Editor's Pipeline reachability; it is already installed. Use Unity-supported Editor operations for scene/prefab authoring.
3. Establish small shared contracts and the folder ownership in the plan. Start the APK smoke build path immediately. A successful host-only test does not resolve Android risk.
4. Source/import the selected existing free assets, preserving license files. No image/model/audio generation, paid assets, custom art pipeline, or prolonged asset search. The selected character has no climb clip: implement a small pose driver on its existing rig.
5. Produce **G1**: an installed APK whose in-game diagnostic counter responds to real GET and POST requests through `adb forward`. Replace the diagnostic-only proof with the full effect by G2.

## Pro-plan execution and parallel opportunities

Default to **one active Claude Code session using Sonnet**, implementing all three workstreams below. Check `/usage` or Settings → Usage before beginning and after G1/G2; record remaining session/weekly allowances and reset times in STATUS. No paid overflow or upgrade is included. The current account balances are unknown, so completion within quota is not guaranteed.

Do not start a standing multi-agent team. If measured headroom justifies it, at most one short worker may take a bounded independent task. Follow existing role/model instructions if delegating. Every worker draws from the same Claude allowance.

- **Lead/A:** contracts, game session, climbing/input/camera, level configurations, selected asset imports, scene/prefab wiring, tests/build coordination. Sole owner of the shared Unity Editor and project/package settings.
- **B:** `Assets/Game/Webhook/` and owned transport tests. Plain C# transport, request parsing, bounded dispatch, responses, cleanup. No Unity calls from network threads. Lead owns this unless explicitly delegated.
- **C:** `Assets/Game/Presentation/` and owned presentation tests. Menu/HUD view scripts, glove overlay, imported-rig pose, SFX. Lead owns this unless explicitly delegated; a worker sends wiring instructions and does not edit shared scenes/prefabs or import assets independently.

Tell each worker that others are editing the project, to preserve unrelated changes, and to stay inside assigned ownership. Publish contracts before workers depend on them. Integrate compile-ready batches; do not run parallel play-mode/test/build sessions in one project.

Execute A → minimal B → minimal C until the Android vertical slice works, then expand to five levels. Overlap builds with documentation, candidate playtesting, and Codex review of fixed revisions. Keep logs in files, return focused error summaries, and avoid repeated full-plan reads or whole-project scans. Reserve allowance for fixes; use the measured burn at G1/G2 to reassess the schedule. If approaching a limit, checkpoint work and report the reset time instead of silently entering paid usage.

## Completion contract

- One scene with five explicit, distinct, completable level configurations.
- Imported modular tower/character, reference-inspired blue sky and camera framing, touch climb/idle, hit/fall and win/lose/retry.
- Main menu, five-level selection, HUD, pause/resume, win/next, lose/retry, return to menu.
- Both GET and POST `http://localhost:56789/bump` inside Editor and Android; six large boxing gloves, flash/shake/SFX, and reliable continued play. Every accepted request has visible feedback; stale requests cannot carry into a new level.
- Device evidence, focused tests, APK recording showing all five completions and the real webhook, README, asset licenses, and clean project deliverable.

Use `adb forward` for PC → Android. Document why the brief's `adb reverse` example is the opposite direction, without editing the brief. Stop Editor Play mode before forwarding host port 56789, or use host 56790 mapped to device 56789.

## Status at every gate; four scheduled Codex reviews

Update [STATUS.md](STATUS.md) at every gate. Request focused Codex review at **G2, G3, G5, and G6**, plus any blocking issue. The candidate prompts Codex to read STATUS; there is no automatic cross-app monitoring. Supply:

```text
Gate and result: G# — PASS / FAIL / NOT VERIFIED
Time remaining to 22:00 target:
Quota remaining and reset times (at G0/G1/G2 and on warnings):
Source revision / changed files:
APK path and device / OS / ABI:
Checks run, executed-test counts, results:
Screenshots / recording / logs:
Known gaps or assumptions changed:
Next action and ETA:
Decision needed from overseer, if any:
```

Escalate blockers after 15 minutes with a concrete fallback. Continue independent work while Codex reviews. Do not claim a gate passed from compilation alone, and do not start optional features before all required functionality is proven on Android. Feature freeze is 16:00; preserve a tested APK and begin recording by 18:00.
