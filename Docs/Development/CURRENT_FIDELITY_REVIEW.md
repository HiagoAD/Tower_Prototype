# Current work review and immediate fidelity checkpoint — 2026-09-28

**Superseding clarification, September 28:** the user defines loss as viewer-induced loss of progress/frustration, with continued play. A terminal lose state is not planned. The updated [action plan](ACTION_PLAN.md) supersedes this review's unresolved failure-rule, deferred-gameplay and lose/retry requirements, and contains fresh estimates. Preserve this document's older findings as historical evidence; it does not establish acceptance of the current Android build.

Codex reviewed the working tree at HEAD `88ef630` with existing staged, unstaged and untracked implementation changes preserved. This is a focused progress/evidence review, not a full code review or milestone PASS. No Unity build or new automated test run was performed for this review.

## Reference direction

The candidate clarified that **the preview image is the visual target; the video shows a different copy**. Use `ref.png` for tower design, palette, lighting, character proportions, composition and HUD style. Use the video only as secondary motion/event context; do not copy its olive tower, dark sky, cyan meter or framing as the art baseline. Portrait Android remains the delivery adaptation: preserve the image's relationships and hierarchy rather than transplanting its landscape pixel measurements. Do not infer controls or animation timing from a still image.

This supersedes the video-baseline decision in the earlier STATUS visual-pass report and the old portrait-baseline instruction in G3_FEEL_CORRECTION. Historical reports remain evidence of what was done, not current direction.

## Work checked

| Area | Evidence checked | Assessment / next action |
| --- | --- | --- |
| Licensed presentation | Current scene builder, materials, pose driver and live device screenshot | Imported character/tower/sky are present; avatar now overlaps the tower front. Primitive-era lateral-gap diagnosis is historical. Art fidelity remains open. |
| Tower / atmosphere / UI | Scene builder explicitly samples video palette; device capture shows olive segmented tower, isolated clouds, cyan meter | Implemented toward the wrong visual baseline. Correct now using the image. |
| Climb animation | `ClimberPoseDriver` advances limb phase from ascended distance; arms/legs are rigid mesh parts, not an articulated knee/elbow rig | Implemented, but device motion still reads as sliding with a persistent V silhouette. Diagnose pose visibility, contact and pull rhythm; do not claim there is no pose driver. |
| Knockback / recovery | `PlayerMotor` uses bounded eased displacement and re-grip lockout; short live capture includes hazard hit and resumed ascent | Implemented and basic recovery observed. Convincing weight transfer and re-grip remain unaccepted. |
| Pause / retry clock | Session clock advances only while Playing, resets on start; motor timers check Paused; hazards use session clock | Implemented in source. Retain targeted device pause-during-hit/retry verification. |
| Effect cleanup | `GloveBurstView.OnDisable` and retrigger call `StopOwnedEffects`, which stops the owned routine and hides visuals/flash | Earlier missing-OnDisable-cleanup issue is repaired in source. Menu/retry interruption and rapid bursts still need end-to-end checks. |
| Retry test | Starts at height 7 and asserts that precondition before lose/retry | Earlier zero-height test weakness is corrected. |
| Automated results | Saved original XML copied to `evidence/look-review-editmode.xml` | 25 total, 25 passed, 0 failed/skipped/inconclusive; 2026-09-28 17:22 UTC. This is the implementer's saved run, not a new execution. Report says final editor-only text tweak followed this run. |
| Android build | `Logs/g2v-android4.log` says Succeeded / Errors=0; local APK hash matches reported artifact | SHA-256 `a955adcb14c84624c6b91c2b249129859721b700dc36e817d91d51d09d5a53a7`. Installed package was not independently hashed; exact dirty-tree build provenance is not proven. |
| Device | Moto g 5G plus `0070013699`; direct screenshot and ten-second capture in preceding review | Current look, ascent, hazard damage and recovery observed. No new webhook/audio/lose-retry verification in this review. |
| Five levels | Only `Assets/Game/Levels/Level1.asset`; session holds one level configuration | Four additional levels and level-selection/progression integration remain outstanding. |
| HTTP/session regression | Existing dispatch code rejects stale/not-Playing/expired requests; current test inventory inspected | Preserve transport fixes. Session-level stale dispatch integration coverage remains outstanding; parser tests alone do not close it. |

Evidence: [device still](evidence/look-review-device.png), [device recording](evidence/look-review-device.mp4), [saved test XML](evidence/look-review-editmode.xml). Existing `g2v_*` captures/report support the implementer's menu, burst and pause claims; they do not establish audible sound or device lose/retry.

## Parallel scope clarification — cancelled subsequently

**Latest instruction:** defer level implementation because gameplay will change. Campaign worktrees and draft branches were subsequently removed at the user’s request; the prior authorization below is historical.

User subsequently authorized Sol agents in isolated worktrees to implement five difficulty variations and sequential progression during the visual pass. All levels share one presentation; no level-select screen or visual themes are required. This supersedes the implementation sequencing below: campaign code/configuration proceeds now, while final scene integration and device checks await exclusive Editor/device access.

## Execute now — G2 fidelity correction

Keep one implementation owner and one Editor/build/device driver. Claude owns implementation when available; Codex covers under the standing quota fallback. This roadmap update does not launch another implementation session.

1. **Image fidelity first.** Rework the existing licensed tower assembly toward a pale white/light-blue, smoother cylinder with thinner, more widely spaced rings and blue window details. Use licensed existing parts/materials; if available assets cannot achieve a feature without prohibited custom modeling/generated art, document the gap and choose the closest licensed option. Replace the dark blue/isolated-puff appearance with bright cyan, softer cloud coverage and gentler lighting. Retain source/license records.
2. **Composition and character.** Place the climber lower in the portrait frame, with clear tower headroom. Reassess proportions from image-relative measurements: character height versus local shaft width and character position versus screen height. Avoid shrinking the tower to the landscape image's raw percentage of screen width. Improve the compact orange/blue silhouette, head/hair readability, separated feet and visible surface contact using licensed assets and supported poses. Do not require an unlicensed character clone.
3. **HUD.** Use a substantial dark altitude track, yellow fill and readable outlined yellow/red numbers. Carry the image's bold outlined typography into necessary controls. Keep HP/pause legible as documented gameplay adaptations. Do not add fake WINS/Heroes/Villains counters without corresponding mechanics.
4. **Device still checkpoint before animation expansion.** Compare the corrected portrait capture directly with `ref.png` at useful viewing sizes. Show tower, atmosphere, silhouette and HUD together; label portrait adaptations. Fix those major discrepancies before spending time on optional event cards, extra particles or menu embellishment. Target 45–60 minutes to a concrete still or a specific asset blocker; this is a reporting timebox, not automatic acceptance.
5. **Climbing feel next, on the corrected composition.** Keep hold-to-climb. Make alternating reaches, a planted-hand phase, body pull and leg separation visible at device scale. The current rigid-part model limits knee articulation: improve the pose within its limits or select a suitable existing licensed rig, without custom modeling. Preserve authoritative motor height, hazard/finish correctness and bounded recovery.
6. **Close the device checkpoint.** Record 15–20 seconds of idle → climb → release → actual webhook → recovery → continued climb. Also verify audible impact, ordinary lose → retry from nonzero height, and pause during recovery. Recheck current-build GET/POST, burst teardown on menu/retry and runtime logs. Supply APK hash and source/change manifest. Candidate visual feedback plus evidence-backed Codex review is required before duplicating presentation into five levels.

## Remaining sequence and acceptance

**G2 remains PARTIAL / fidelity correction active.** Technical progress is retained; neither 25 passing tests nor a successful build establishes a visual match. The image-directed still must show the corrected tower/sky/HUD identity, and the motion clip must show credible contact/pull/recovery. Device audio and lose/retry remain open.

After that checkpoint: **G3** adds exactly five distinct completable levels and selection/progression, with all-five device evidence and remaining session/lifecycle tests. **G4** integrates and tunes a complete candidate. **G5** performs device regression/performance/license checks. **G6** records and packages the final APK/project/README. **G7** verifies and submits the shared link. Original same-day times are historical targets, not achieved milestones; report fresh estimates after the first corrected still while preserving the existing submission target. Do not let the elapsed 13:30 expansion rule bypass this checkpoint.

No new assets may be generated. Do not edit the immutable reference. No implementation files changed in this review.
