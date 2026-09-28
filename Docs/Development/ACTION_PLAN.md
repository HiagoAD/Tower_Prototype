# Tower Prototype — remaining delivery plan

Replanned September 28, 2026 at approximately **17:45 Recife / 20:45 UTC**. Codex owns planning/review; Claude owns implementation/integration. This revision supersedes the expired morning schedule and earlier terminal-failure assumptions. It schedules remaining work; it does not launch implementation or restore the cancelled parallel worktrees.

The [immutable brief](../Reference/Unity-technical-test/Unity-technical-test.md), [image](../Reference/Unity-technical-test/attachments/ref.png), and [video](../Reference/Unity-technical-test/attachments/ref.mp4) remain preserved. Use the image as the visual target and video as secondary motion/event context. User clarifications and implementation assumptions are recorded here, outside the reference directory.

## 1. Confirmed social gameplay

The player climbs while viewers help or hinder through server-triggered bump commands. Positive bumps lift the character; negative bumps remove earned height. **Losing progress and the frustration of recovering are the loss experience. There is no terminal defeat, lives counter or mandatory game-over/retry loop.** Reaching the summit remains the clear win condition.

The brief explicitly lists win/lose among expected character states. The user's clarified interpretation replaces terminal loss with a recoverable setback. Record that difference honestly in the README; do not claim a conventional lose state was implemented, or add lives/death to satisfy an old checklist. Removing terminal loss is no longer an unresolved design blocker.

- Retain hold-to-climb/release-to-cling, accepted planted-grip motion, body swing, global climb pace, eased bump displacement and re-grip.
- Retain current hazard knockback as recoverable lost progress. No hit-point depletion. Difficulty comes from climb duration, obstacle placement/timing and viewer interference.
- Every accepted social event gives visible feedback. After a finite burst ends, normal control must recover. Continuous negative requests may deliberately prevent progress; a stuck motor, lost input or lingering effect after requests stop is a defect.
- Positive help near the summit may complete the level normally. Clamp movement at the level bounds and reject stale requests across transitions.
- Keep exactly five distinct, sequentially reachable, completable levels with shared visuals. No separate level-select screen is required. Main menu, HUD, pause/resume, summit/next, final completion and return to menu are required. A voluntary restart may remain useful but is not a lose/retry gate.
- This plan covers the game's local listener and viewer-style commands. A streaming-platform integration, remote relay, audience website, accounts or monetization is not part of the current deliverable.

## 2. Current baseline

- Unity **6000.3.11f1**; physical Android proof already exists on Motorola moto g 5G plus, Android 11, arm64-v8a, serial `0070013699`.
- One authored JSON level. Planted-grip climbing and global pace implemented; candidate acceptance of climbing feel recorded.
- JSON levels, removal of lives and typed positive/negative bumps integrated into main. Saved integrated EditMode results: **101 passed, zero failed/skipped**. Claude is independently reviewing code quality; findings may add work.
- Local APK hash `f63e51563c87f271d61bd709875bbeabb2efee023db2033cf3900ddcfe65e8e6` is the older pace build. It does not validate the subsequent integrations on Android.
- Image-directed visuals are substantially improved; final current-build visual approval, audible impact, lifecycle checks and performance observations remain open.
- Four additional levels, next/final flow, submission README, complete device recording and verified delivery link remain outstanding.
- G0/G1 retain historical PASS. G2 remains PARTIAL. G3–G7 have not passed. See [GOALS_REVIEW.md](GOALS_REVIEW.md) and [STATUS.md](STATUS.md).

## 3. Remaining estimates and schedule

Estimate: **4½–6½ hours elapsed** with one implementation owner, functioning toolchain/device, and no major code-review finding or quota interruption. This includes builds, routine fixes, validation, recording and packaging; upload speed and substantial review defects can extend it. Read-only review and documentation can overlap builds, but Editor/build/device use stays serialized.

**Working delivery target: 23:00 Recife September 28 / 02:00 UTC September 29.** Expected range from a 17:45 start: **22:15–00:15 Recife** (01:15–03:15 UTC September 29). The previous 22:00 target is now a stretch, not the baseline. The user's 09:00 UTC deadline remains interpreted as September 29, **06:00 Recife**, leaving approximately 5¾–7¾ hours after the forecast range. Its date was inferred earlier; this revision does not assert a new date confirmation. Shift forecasts by any delay in resuming implementation.

| Work | Remaining estimate | Working window, Recife Sep 28 | Acceptance |
| --- | --- | --- | --- |
| Integrated one-level APK and social-loop closure | 30–45 min | 17:45–18:30 | **G2:** current source built/installed; bare GET/POST and positive/negative requests; loss of height, recovery, continued climb, pause/menu cleanup; audible impact and reviewed device still/clip. |
| Five JSON levels and sequential flow | 75–105 min | 18:30–20:00 | **G3:** five distinct levels, next and final completion, transition tests and all-five Android completion evidence. Includes a campaign build/test pass. |
| Review fixes, focused tuning and final regression | 45–60 min | 20:00–21:00 | **G4/G5:** complete candidate frozen; necessary review fixes resolved; social-event/lifecycle regression, final look/audio and five-minute performance observation. Hash and preserve accepted APK/source. |
| Final recording, README, credits and clean package | 60–75 min | 21:00–22:15 | **G6:** final APK footage shows menu, all five completions, real requests and resumed play; README and source/licenses complete; packaged APK reinstall checked. README inventory may start during builds. |
| Upload, download/access check and handoff for submission | 30–45 min | 22:15–23:00 | **G7:** one usable sharing location with APK/video/project, verified access and candidate submission. |
| Additional repair reserve | 30–60 min | Use only when a check fails | Rebuild/retest affected behavior and replace recordings if necessary. This reserve is included in the overall 4½–6½-hour range. |

The task ranges total 4–5½ hours before the additional reserve. The working windows are a midpoint allocation, not promised completion times. Record actual elapsed time and reforecast after G2 and G3; do not interpret a clock time as a passed gate.

## 4. Five-level implementation plan

Use current JSON loading and one gameplay scene. Add a small ordered campaign list and next/final flow. Do not resurrect deleted worktree drafts or add a second bootstrap. Ensure the tower, cloud field and camera cover the greatest scaled finish height across all five configurations and the supported pace range.

| Level | Distinct challenge | Initial clean-run tuning target |
| --- | --- | --- |
| 1 — First Ascent | Generous opening and familiar widely separated bands | 20–30 s |
| 2 — Higher Climb | Longer ascent with separated bands and different phases | 25–35 s |
| 3 — Rhythm | Alternating active windows with safe resting spaces | 30–40 s |
| 4 — Pressure | Close pair with a safe gap, followed by recovery space | 35–45 s |
| 5 — Summit | Longer combination of learned patterns and clear final summit | 40–50 s |

These are tuning targets to measure, not existing content or verified durations. All five must be completable without viewer help. Assistance can shorten runs; negative events extend them. Do not require external requests to unlock or finish a level. Preserve shared visuals and reference proportions. No new asset search or art generation is scheduled.

Reset held input, motor/recovery state, level clock, camera offsets, event overlays and hazard instances on restart/new level. Requests from an earlier attempt/level cannot carry forward. Menu and pause reject new gameplay bumps rather than storing a future volley.

## 5. Social-event and device acceptance

Retain the existing listener; no transport rewrite. See [BUMP_EVENTS.md](BUMP_EVENTS.md) for optional polarity/type/tag fields. Bare GET/POST `/bump` must remain the default negative boxing event expected by the brief.

| Check | Required evidence |
| --- | --- |
| Real HTTP in Editor and Android | GET and POST while playing return accepted request IDs and visibly trigger full-screen gloves; positive and negative commands affect altitude in the intended direction. No debug-button substitute. |
| Recovery and boundaries | Negative bump at nonzero height visibly loses progress and re-grips; near-bottom hit remains bounded; positive bump near summit completes normally; after rapid finite mixed requests stop, input and climbing work. |
| Visual/audio | At least 4–6 recognizable gloves cover the playfield (current implementation uses 14), flash/shake are readable, impact sound is actually heard, and the character remains readable. Final device still compared with ref.png. |
| Session/lifecycle | Pause during recovery freezes gameplay; resume works; menu/restart/next clear transient effects. Requests during non-playing states are rejected. Background/resume and listener restart do not replay stale events or crash. |
| Five-level completion | Per-level normal touch play, obstacle interaction, summit and next; final completion returns to menu. No skip/debug teleport as proof. |
| Focused automated checks | Existing suite plus meaningful campaign/transition coverage and regressions for changes from Claude's review. Save actual executed counts/XML. No duplicate tests solely to increase counts. |
| Performance | Five-minute Android run with repeated social events and transitions; record measured frame rate, stalls and memory/object-growth observations. Target stable 30 FPS minimum, preferably 60. |
| Final provenance | Exact source revision/change manifest, APK hash, installed artifact match and build-linked device evidence. Recheck immutable-reference hashes. |

ADB setup for a PC request to the game's Android listener, with Editor Play stopped:

```sh
adb -s 0070013699 forward tcp:56789 tcp:56789
curl --max-time 3 -i http://localhost:56789/bump
curl --max-time 3 -i -X POST http://localhost:56789/bump
curl --max-time 3 -i 'http://localhost:56789/bump?polarity=positive&type=boxing&tag=Helper'
curl --max-time 3 -i 'http://localhost:56789/bump?polarity=negative&type=boxing&tag=Challenger'
adb -s 0070013699 forward --remove tcp:56789
```

If host 56789 is occupied by the Editor, map host 56790 to device 56789 and use 56790 in PC curl commands. Document `adb forward` for PC → phone. The brief's `adb reverse tcp:56789 tcp:56789` routes phone → host and is contextual explanation, not a required setup step. Preserve the original brief unchanged.

## 6. Ownership and review

Claude continues implementation/integration after its current code-quality check. Codex handles planning and milestone review; the candidate supplies device feedback and submits the final link. Keep one Editor/build/device owner. Preserve other sessions' uncommitted changes. The standing quota fallback permits the available assistant to cover both roles when the other is unavailable.

No standing agent team or resurrected campaign worktrees. Unknown quota and review findings are estimate risks; report them instead of assuming additional allowance. Keep logs/evidence focused and avoid duplicate full-project reviews. Escalate any blocking problem after 15 minutes with a fallback and revised ETA.

At G2/G3/G5/G6, provide the gate result, source revision, APK hash, actual test counts, device details, evidence paths, remaining gaps and next ETA in STATUS. Use the advisory [Jev workflow](../../Tools/Jev/README.md); missing API access never blocks ordinary review or promotes a gate. Continue independent documentation while a review is pending.

The older [fidelity review](CURRENT_FIDELITY_REVIEW.md) remains useful for image direction. Its pending-climb/lose-retry requirements and older schedule are superseded by this plan and the accepted grip/social design.

## 7. Recovery rules and delivery

- No optional new event types, environments, streaming integrations, music systems or animation rewrites before five levels and delivery are complete.
- If G2 misses 18:30, carry its slip into the forecast; prioritize broken behavior over further cosmetic revisions.
- If G3 misses 20:00, shorten/simplify the remaining level patterns while retaining five meaningfully distinct, completable levels. Do not remove recording or actual device proof to preserve a clock target.
- Freeze feature work at the complete-candidate checkpoint, forecast 20:00–21:00. Thereafter fix acceptance blockers only. Preserve at least 90 minutes for recording/package/access checks; the baseline reserves two hours.
- If a final code/asset change invalidates the APK or footage, rebuild, verify and record the replacement. Do not label older footage as the final build.
- If the interpreted hard deadline becomes threatened, deliver the best functional state with explicit omissions, as the brief permits, rather than claiming missing levels or evidence passed.

The single submission location contains `TowerPrototype.apk`, `TowerPrototype-demo.mp4`, a clean project ZIP or repository link, and a README. The source archive excludes Library/Temp/Logs/obj/build outputs while retaining Assets/Packages/ProjectSettings and licenses.

README: Unity version; controls; build/open instructions; five-level progression; the social setback interpretation and its difference from a terminal lose state; portrait adaptation; actual assets/authors/licenses/source links; curl/ADB instructions; device/test results and known limitations. Confirm accessible glove attribution in the packaged result.

Record the final APK running on the device, showing menu, all five levels played/completed, real positive/negative viewer-style commands including the default boxing event, and continued play. Correlate requests with IDs/timestamps or visible curl evidence. Review the actual video/audio, reinstall the packaged APK and verify downloads/access before the candidate sends the link.
