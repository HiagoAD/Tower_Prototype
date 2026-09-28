# Five-level feel pass — September 28, 2026

This pass keeps exactly five levels, the accepted planted-grip movement, hold/release controls, social setbacks, licensed visuals and existing campaign flow. It changes authored level data, not the motor or webhook behavior. The immutable brief, reference image and sampled video were reviewed; all six snapshot checksums passed.

The previous Android continuous-hold runs took approximately 22, 41, 70, 92 and 103 seconds (see G3_REVIEW.md). Those runs demonstrated completion, but the later levels accumulated repeated hits. The tuning aims to make timing rewarding without making mistakes dominate the ascent.

| Level | Bands | Finish height | Green window | Active window | Intent |
| --- | ---: | ---: | ---: | ---: | --- |
| First Ascent | 2 | 30 | 3.7 s | 1.3 s | Two forgiving, separated timing lessons |
| Higher Climb | 3 | 40 | 3.7 s | 1.7 s | Repeat the lesson with one additional obstacle |
| Rhythm | 5 | 50 | 3.4 s | 1.8 s | Short groups separated by a longer recovery stretch |
| Pressure | 6 | 60 | 3.1 s | 1.9 s | Denser sequences with a deliberate breathing gap |
| Summit | 8 | 70 | 2.8 s | 2.0 s | Combine learned timing into the longest ascent |

All levels keep climbSpeed 2.5 and the shipped global pace of 0.92 body heights per second. The number of obstacles and active-time fraction increase through the campaign. Each opening has at least nine authored units before the first band; adjacent bands have at least seven units, and every finish has a clear final ascent.

At the minimum supported pace, crossing one band takes 2.5 seconds because the character's body does not shrink with level distance scaling. The former late-level green windows were shorter than that. The new minimum window is 2.8 seconds. Seven-unit spacing also leaves room for the body and the existing hazard recovery clearance at this pace. These are fairness constraints, not a claim that every player will find every level easy.

Validation results will be recorded below after the fresh Unity runs. Automated traversal cannot establish subjective enjoyment; final Android touch-play remains part of G4/G5 acceptance.

The data author's deterministic cautious-climber model measured approximately **14 / 19 / 27 / 34 / 43 seconds** at the default pace, including **2 / 3 / 7 / 10 / 15 seconds** of waiting. These are modeled clean runs, not observed Android runs. Phase offsets use tenths of a second; starts delayed by human input need separate coverage rather than relying solely on one opening alignment.

Changed production files: `Assets/Game/Levels/Level1.json` through `Level5.json`. Regression coverage lives in `Assets/Game/Tests/EditMode/LevelCampaignDataTests.cs`. Existing scene capacity covers all five unchanged finish heights, so scene regeneration is unnecessary.

A separate approximate source-based continuous-hold model compared HEAD with the tune. Hit counts changed from **2/4/17/11/33** to **2/3/6/8/15**. It modeled cubic knockback, re-grip, body clearance and stationary overlap at 240 steps per second. This model underpredicts the earlier recorded Android times and therefore supports the direction of the change only; it is not device evidence. A delayed-start sweep from 0 to 4.4 seconds in 0.4-second increments produced non-overlapping cautious duration ranges: **12–15 / 16–20 / 23.4–28.2 / 29.6–34 / 38.6–43 seconds**.

Independent scoped review: **APPROVE**, no findings in the final five JSON files and campaign test changes. The review checked the pace normalization, spacing, timing margins, five-file cap and delayed-start/frame-rate coverage. It did not operate Unity or certify Android playability.

Fresh Unity EditMode result: **162/162 passed, zero failed/skipped**, September 28 at 23:23:21–23:23:23 UTC. Saved NUnit XML: [level_feel_editmode.xml](evidence/level_feel_editmode.xml). Executed clean-run measurements confirm **14.0 / 19.0 / 27.0 / 34.0 / 43.0 seconds**. The campaign checks cover all five levels at minimum, shipped and maximum pace, at 30/60 FPS, and starts delayed by 0, 0.37, 1.13 and 2.41 seconds.

Earlier validation attempts were not passes: one caught the intentionally changed Level 1 phase in `LevelDefinitionJsonTests` (expectation updated from 2 to 2.6); another had a transient existing `BumpSessionTests.MissingFields_ResolveToCatalogDefaults` response-body failure. The next complete suite passed without any production transport change. This does not establish that the intermittent test issue is fixed.

Fresh Unity PlayMode result: **13/13 passed, zero failed/skipped**, September 28 at 23:24:03–23:24:23 UTC. Saved NUnit XML: [level_feel_playmode.xml](evidence/level_feel_playmode.xml). These tests cover the real scene and campaign transitions; they do not substitute for human touch-play or a recording of the tuned APK.
