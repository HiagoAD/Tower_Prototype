# Tower Prototype — remaining delivery plan

Updated September 28, 2026 to incorporate the ten player-feedback items below. Claude owns planning, prioritization, milestone review, implementation, integration, and delivery coordination following the user-requested handoff. This revision orders the remaining polish work; implementation and acceptance are still pending. The earlier 17:45 schedule is retained as historical context, not a current forecast.

The [immutable brief](../Reference/Unity-technical-test/Unity-technical-test.md), [image](../Reference/Unity-technical-test/attachments/ref.png), and [video](../Reference/Unity-technical-test/attachments/ref.mp4) remain preserved. Use the image as the visual target and video as secondary motion/event context. User clarifications and implementation assumptions are recorded here, outside the reference directory.

## 1. Confirmed social gameplay

The player climbs while viewers help or hinder through server-triggered bump commands. Positive bumps lift the character; negative bumps remove earned height. Community interactions should be the strongest gameplay feedback moments, while every level remains completable without viewer help.

- Retain hold-to-climb/release-to-cling, accepted planted grips/body swing, global pace and eased displacement/re-grip. Expand the touch area as specified below.
- The latest user decision restores lives and pause behind flags and ships both **enabled** in `Assets/Game/Settings/GameSettings.asset`. Hazard hits cost a life; zero lives shows GAME OVER with Continue/retry or Exit. Webhook bumps never cost lives. This supersedes the earlier no-terminal-loss/no-pause plan. Preserve the flags-off recoverable-setback mode.
- Every accepted social event gives visible feedback. After a finite burst ends, normal control must recover. Continuous negative requests may deliberately prevent progress; lingering effects or lost control after requests stop are defects.
- Positive help near the summit may complete the level normally. Clamp movement at level bounds and reject stale requests across transitions.
- Keep exactly five distinct, sequentially reachable, completable levels with shared visuals, main menu, HUD, pause, loss/retry, summit/next, final completion and menu return. No separate level-select screen is required.
- Streaming-platform integration, a remote relay, audience website, accounts and monetization remain outside this deliverable.

## 2. Current baseline

- Unity **6000.3.11f1**; physical Android evidence on Motorola moto g 5G plus, Android 11, arm64-v8a, serial `0070013699`.
- **G2 and G3 PASS** are recorded in [STATUS.md](STATUS.md) and [G3_REVIEW.md](G3_REVIEW.md): five levels, sequential/final flow and social-loop evidence exist. G4–G7 remain open.
- Last accepted campaign APK: `261e913853fa13de9da0977d51dd6919a883e087974a157ce5661799cf310bce`. Later level tuning, restored lives/pause and settings consolidation need a new Android build and device regression.
- Latest implementation report records **191/191 EditMode and 22/22 PlayMode passed** after settings consolidation; these were not rerun for this planning update.
- All new feedback tuning belongs in `Assets/Game/Settings/GameSettings.asset`. Preserve current user settings, level data and concurrent implementation edits.
- Final current-build feel/visual/audio review, performance observation, submission README, final recording and verified delivery link remain outstanding.

## 2A. Ordered player-feedback backlog

All ten original items are mapped below. Order is implementation order: fix interaction/readability first, establish the dominant social-event moment, then tune the smaller feedback around it. **P0** = usability fixes; **P1** = core feel; **P2** = supporting polish; **P3** = stretch. All rows are planned, not completed. Optional techniques within a row can be cut after device review without hiding the omission.

| Order / priority | Category | Original feedback | Planned change and acceptance |
| --- | --- | --- | --- |
| 1 / P0 | Controls and usability | **#2 — full-screen input** | Hold anywhere in the gameplay area to climb, including top/middle/bottom and noninteractive HUD. Interactive buttons/panels consume their touches. Release, cancellation, pause/background and transitions clear held input; touching Pause/Next/Exit must never start climbing. Update the control hint and verify on Android. |
| 2 / P0 | UI readability | **#1 — EXIT looks like EHIT** | Inspect glyph, outline and spacing at actual portrait size; adjust styling or use an existing licensed legible font. EXIT must read unambiguously on every panel; check other button labels for the same issue. Preserve the reference's bold UI character. |
| 3 / P1 | Community interaction and game feel | **#9 — bumps as the main gameplay moment** | Increase positive/negative displacement beyond the current one-body-height baseline with independently tunable strengths. Synchronize glove impact, displacement, audio, shake and a short camera zoom pulse; evaluate restrained chromatic aberration/bloom tweens on device. Use FOV only if the camera supports it, otherwise the corresponding zoom control. Social bumps must feel stronger than normal hazard hits, with clear polarity, bounded movement, readable character/HUD and reliable re-grip. Keep bare GET/POST as the required negative boxing event with at least 4–6 full-screen gloves. No webhook life loss. Tune exact distances by play, not an untested fixed multiplier. |
| 4 / P1 | Obstacle feedback | **#4 — small hit shake** | Add a brief, smaller shake at confirmed hazard impact, synchronized with knockback. Blend/arbitrate overlapping camera effects so a simultaneous bump cannot leave a persistent offset. Confirm lives still decrement once per valid hazard hit. |
| 5 / P1 | Climb feedback | **#3 — hand-grab particles** | Small, short-lived particles at each actual planted hand contact, aligned to the hand and reused/pool-limited. Do not emit every frame, while idle, or during a suspended climb. Keep the accepted grip animation and silhouette readable. |
| 6 / P1 | HUD feedback | **#5 — reactive height text** | Tint height green while gaining height, red while losing it, and restore the normal tint when stable. Gently scale the current-height label during active climbing and return to baseline on stop/hit; allow a readable gain/loss cue for bumps too. Derive direction from actual altitude change, prevent jitter, clipping and accumulated scale, and reset on transitions. |
| 7 / P2 | Victory presentation | **#6 — victory fanfare** | Add a short tweened victory heading, confetti and celebratory audio, with a modest character-focused camera move if framing allows. Preserve summit placement and Next/Exit usability; distinguish intermediate wins from the final TOWER CLEARED screen. Source particle/audio assets from existing free licensed packs. |
| 8 / P2 | Tactile feedback | **#7 — light/heavy haptics** | If supported by the target device, light pulses on real hand grabs and stronger pulses on obstacle hit and win. Provide an enable/intensity setting, rate-limit repeated grabs, and avoid a new pulse while paused/backgrounded. Verify actual device output; unsupported hardware must still play normally. |
| 9 / P2 | Audio | **#8 — music loop and SFX** | Inventory/reuse current impact audio first. Add a quiet free licensed loop and missing grab/hit/win cues, with music/SFX volume controls in the shared settings asset and sensible overlap limits. Source/verify essential bump and victory cues alongside rows 3 and 7; the background loop is nice-to-have. Listen on device and preserve source/license records. No generated audio. |
| 10 / P3 | Victory camera spectacle | **#10 — tower orbit and post-processing** | After the basic win sequence works, prototype a short tower-orbit choreography with zoom/FOV, restrained aberration and bloom. Inspect the existing tower/character/background from those angles first: do not require an environment/art rebuild for this effect. If an orbit exposes incomplete geometry or breaks the reference composition, use a short front-facing arc or push-in and record the fallback. Restore all camera/effect state on Next/Exit/retry. Ship only if framing and Android performance pass. |

### Dependencies, visual hierarchy and scope

Feedback intensity should read **hand grab < obstacle hit < community bump**; victory has its own brief celebration. Rows 3, 4, 7 and 10 share camera/effect ownership so their tweens cannot fight or accumulate. Rows 5, 8 and 9 should use the same actual grab timing; do not replace the accepted climb motion just to add feedback.

These requests extend presentation beyond the still reference, particularly the victory orbit and post-processing. Keep the immutable brief's fidelity and performance targets: restore the normal reference framing after effects and preserve the required gloves, five playable levels and recovery. Treat the full orbit as a conditional experiment, not a new art-direction requirement. No generated art/audio or asset-generation pipeline; existing free licensed assets may be sourced where needed, with attribution.

### Implementation batches and acceptance

1. **Usability:** rows 1–2; verify full-screen holds, UI consumption and portrait text.
2. **Core feel:** rows 3–6, including the essential impact cue. Compare normal climbing, hazards and real positive/negative HTTP bumps on device. Recheck all-five playability after stronger displacement; assistance may finish a level, negative bumps must remain recoverable.
3. **Supporting polish:** rows 7–9; confirm celebrations, audible cues and supported haptics. Then attempt row 10 only if time and device performance permit.
4. **G4/G5 regression and freeze:** build the current source, run appropriate input/transition/bump regression tests, and verify lives/pause enabled plus the supported flags-off paths. On Android, check menu/retry/next/win/loss, pause/background, finite mixed request bursts and a five-minute performance run. All particles, HUD colors/scales, camera offsets/post-processing and audio must reset correctly. No gate is promoted by this plan.
5. **G6/G7 delivery:** record the accepted APK with all five completions and real webhook requests; finish README/licenses, clean package and verified access.

Re-estimate after the first two batches and the new device build. The old 45–60-minute tuning allocation does **not** cover all this added scope. Preserve at least 90 minutes for final recording/package/access checks; if time is short, cut row 10 first, then background music and optional effects before compromising controls, community-event clarity or delivery evidence. Log deferred items explicitly in STATUS and the final handoff.

## 3. Historical estimates and schedule (superseded)

The following was the 17:45 forecast, before G2/G3 closure and this expanded feedback scope. Its clock windows and unfinished-level assumptions are historical. Use §2A for current remaining work; no new completion time is claimed until implementation is re-estimated.

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

## 4. Five-level tuning reference (campaign implemented)

Retain current JSON loading, one gameplay scene, the ordered campaign list and implemented next/final flow. Do not resurrect deleted worktree drafts or add a second bootstrap. Ensure the tower, cloud field and camera cover the greatest scaled finish height across all five configurations and the supported pace range.

| Level | Distinct challenge | Initial clean-run tuning target |
| --- | --- | --- |
| 1 — First Ascent | Generous opening and familiar widely separated bands | 20–30 s |
| 2 — Higher Climb | Longer ascent with separated bands and different phases | 25–35 s |
| 3 — Rhythm | Alternating active windows with safe resting spaces | 30–40 s |
| 4 — Pressure | Close pair with a safe gap, followed by recovery space | 35–45 s |
| 5 — Summit | Longer combination of learned patterns and clear final summit | 40–50 s |

These were initial targets. The later data pass measured simulated clean runs of 14/19/27/34/43 seconds; see [LEVEL_FEEL_PASS.md](LEVEL_FEEL_PASS.md). Reassess on device after feedback changes. All five must be completable without viewer help. Assistance can shorten runs; negative events extend them. Do not require external requests to unlock or finish a level. Preserve shared visuals and reference proportions. Only the limited licensed asset sourcing in §2A is planned; art/audio generation remains prohibited.

Reset held input, motor/recovery state, level clock, camera offsets, event overlays and hazard instances on restart/new level. Requests from an earlier attempt/level cannot carry forward. Menu and pause reject new gameplay bumps rather than storing a future volley.

## 5. Social-event and device acceptance

Retain the existing listener; no transport rewrite. See [BUMP_EVENTS.md](BUMP_EVENTS.md) for optional polarity/type/tag fields. Bare GET/POST `/bump` must remain the default negative boxing event expected by the brief.

| Check | Required evidence |
| --- | --- |
| Real HTTP in Editor and Android | GET and POST while playing return accepted request IDs and visibly trigger full-screen gloves; positive and negative commands affect altitude in the intended direction. No debug-button substitute. |
| Recovery and boundaries | Negative bump at nonzero height visibly loses progress and re-grips; near-bottom hit remains bounded; positive bump near summit completes normally; after rapid finite mixed requests stop, input and climbing work. |
| Visual/audio | At least 4–6 recognizable gloves cover the playfield (current implementation uses 14), flash/shake are readable, impact sound is actually heard, and the character remains readable. Final device still compared with ref.png. |
| Session/lifecycle | Backgrounding freezes gameplay; with the shipped pause flag on, returning waits for Continue (flag off resumes automatically); menu/restart/next clear transient effects. Requests during non-playing states are rejected. Background/resume and listener restart do not replay stale events or crash. |
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

Claude owns planning, prioritization, implementation/integration, milestone acceptance review, blocker resolution, forecasts, and delivery coordination. Claude maintains ACTION_PLAN, CLAUDE_HANDOFF and STATUS and reviews the evidence before recording gate decisions; no Codex sign-off is required. The candidate supplies device feedback and submits the final link. Keep one Editor/build/device owner. Preserve other sessions' uncommitted changes. The standing quota fallback permits Codex to cover the work if Claude reaches its usage limit and Codex has quota; Claude resumes full ownership when available.

No standing agent team or resurrected campaign worktrees. Unknown quota and review findings are estimate risks; report them instead of assuming additional allowance. Keep logs/evidence focused and avoid duplicate full-project reviews. Escalate any blocking problem after 15 minutes with a fallback and revised ETA.

At G2/G3/G5/G6, provide the gate result, source revision, APK hash, actual test counts, device details, evidence paths, remaining gaps and next ETA in STATUS. Use the advisory [Jev workflow](../../Tools/Jev/README.md); missing API access never blocks ordinary review or promotes a gate. Continue independent documentation while a review is pending.

The older [fidelity review](CURRENT_FIDELITY_REVIEW.md) remains useful for image direction. Its pending-climb/lose-retry requirements and older schedule are superseded by this plan and the accepted grip/social design.

## 7. Recovery rules and delivery

- Keep optional new event types, environments, streaming integrations and animation rewrites out of scope. The bounded audio/presentation work in §2A is now planned.
- G2/G3 are closed; preserve those functional requirements while revalidating changed behavior. Do not remove recording or actual device proof to preserve a clock target.
- Freeze feature work at the complete-candidate checkpoint after §2A regression. Thereafter fix acceptance blockers only. Preserve at least 90 minutes for recording/package/access checks; the baseline reserves two hours.
- If a final code/asset change invalidates the APK or footage, rebuild, verify and record the replacement. Do not label older footage as the final build.
- If the interpreted hard deadline becomes threatened, deliver the best functional state with explicit omissions, as the brief permits, rather than claiming missing levels or evidence passed.

The single submission location contains `TowerPrototype.apk`, `TowerPrototype-demo.mp4`, a clean project ZIP or repository link, and a README. The source archive excludes Library/Temp/Logs/obj/build outputs while retaining Assets/Packages/ProjectSettings and licenses.

README: Unity version; controls; build/open instructions; five-level progression; positive/negative social setbacks, hazard-only lives and configurable pause/loss behavior; portrait adaptation; actual assets/authors/licenses/source links; curl/ADB instructions; device/test results and known limitations. Confirm accessible glove attribution in the packaged result.

Record the final APK running on the device, showing menu, all five levels played/completed, real positive/negative viewer-style commands including the default boxing event, and continued play. Correlate requests with IDs/timestamps or visible curl evidence. Review the actual video/audio, reinstall the packaged APK and verify downloads/access before the candidate sends the link.
