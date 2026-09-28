# Reference behavior review — before G2

Codex review, September 28, 2026. This is an observation note outside the immutable reference, not a replacement brief. No implementation files were changed.

## Evidence examined

The original image and video were inspected through chronological frame samples. The initial pass covered the full 103.21-second video at five-second intervals and its first ten seconds at one-second intervals. This follow-up adds half-second samples from approximately 5–17 seconds and one-second samples from 85–103 seconds. These are frame inspections, not verified continuous playback or an input trace. Audio was not assessed here.

## Observed

- In the opening, the character stays on the front of the tower and alternates limb poses while progressing vertically. Tower features pass relative to the character as the camera follows. Ascending and relatively stationary moments both occur.
- Around 9–11 seconds, many red gloves and impact elements occupy much of the screen. Further effects overlap or follow. Large altitude/camera changes make it unsafe to assign every displacement to a single glove hit from these samples.
- Later events include much greater camera pullbacks and other spectacle elements. Reproducing all those event types is outside the required boxing-glove feature.
- Near the ending, the character is briefly below an overhanging tower section. Further effects precede a large countdown and trophy imagery, then a screen labeled GAME OVER with Continue and Exit.

## Not established

The footage does not expose keyboard/touch state. It cannot establish whether climbing is automatic, held, tapped, or externally controlled. It also does not establish three-hit lives, timed obstacle bands, safe-crossing windows, or five-level rules. The final GAME OVER label does not by itself prove a defeat condition; the preceding trophy/countdown makes that interpretation uncertain.

## Direction for Claude

Proceed with Android deployment and webhook transport; those do not depend on this uncertainty. Before implementing the gameplay controller, play the source clip directly, focusing on 0–17 and 85–103 seconds. Separate observed movement from inferred controls in the status report.

The action plan's hold-to-climb input, obstacle bands, and three-mistake rule remain provisional design proposals, not verified reference behavior or additional requirements from the brief. Do not build an elaborate hazard/lives system on their authority. Resolve the smallest playable interpretation with the candidate/Codex before committing to those rules, while keeping movement and input separate so changing control style is inexpensive.

The brief still requires playable levels, appropriate character states, a clear win condition, and continued play after the webhook animation. Implement those requirements under an explicitly recorded interpretation. Do not claim reference parity for control or failure rules that the evidence does not establish.
