# Reference observation addendum — Claude, before G2

Follow-up to [REFERENCE_BEHAVIOR_REVIEW.md](REFERENCE_BEHAVIOR_REVIEW.md). Not a replacement for
that review or the immutable brief; no reference files were touched. Produced by extracting 1
frame/second from `ref.mp4` at 0–17s and 85–103s (via local `ffmpeg`, not committed) and viewing
them directly, per Codex's direction to look at those windows before building the controller.

## New observations

- The footage is a screen capture of an existing desktop/emulator window titled **"KarinTower"**,
  with a **"trial version"** watermark visible in-frame. This is recorded gameplay of a real
  existing app, not an edited trailer. We do not have and must not seek that app or extract its
  assets — brief section 4.3 requires our own free/licensed or AI-generated assets — but this
  explains why the reference looks and behaves like a finished commercial idle/climber game
  rather than a prototype.
- The HUD includes a numeric counter (values seen: 34, 42, 56, 63, 76, 88, 96, 101 …) next to a
  vertical "Speed" bar on the left edge — most consistent with a live altitude/height readout, not
  a hit/mistake counter.
- During and after the glove burst, a second HUD block appears listing several distinctly named
  attack/event types, each with its own icon and a small heart/life count: **TikTikBox, Boxing,
  WaterCannon, JetPack, AOE** (exact spelling approximate — text is small/compressed in the
  capture). This is a materially richer hit-economy than a single "boxing glove hit" with a flat
  three-mistake counter: it reads as several distinct hazard/attack types, each tracked
  separately.
- The 85–103s window shows rockets, explosions, and a falling car/vehicle prop before returning to
  plain climbing shots, then a countdown ("5", "4", "2" visible) rendered over trophy/medal
  imagery, then a **GAME OVER** screen with **Continue** and **Exit** buttons. The countdown-over-
  trophy framing still does not, by itself, prove a normal defeat condition — it reads at least as
  plausibly as a bonus-round or end-of-run summary that happens to end on a GAME OVER menu. Treat
  "GAME OVER" as evidence a terminal/menu state exists, not as proof of what triggers it.
- No frame in either sampled window exposes on-screen touch indicators, button prompts, or any
  other cue that would establish hold-to-climb, tap-to-climb, or automatic climbing. Input method
  remains unresolved from visual evidence.

## Standing direction (unchanged)

Per REFERENCE_BEHAVIOR_REVIEW.md: hold-to-climb, timed obstacle bands, and the three-mistake rule
remain our own provisional design choices, not confirmed reference mechanics. This addendum adds
detail (multiple named attack types, an altitude readout, an ambiguous end screen) but does not
resolve the open questions, so it does not upgrade any of those provisional rules to "observed."

Given the explicit instruction not to build an elaborate hazard/lives system on the plan's
authority, G2 implements the **smallest playable interpretation** that still satisfies the brief's
required states (idle, move/climb, hit reaction, win/lose):

- One generic hazard behavior (not five named attack types) with a bounded hit-point counter
  (starts at 3, configurable) instead of a dedicated "lives" subsystem — a single `int` on
  `PlayerState`, decremented on hazard/webhook hit, with 0 triggering lose.
- Input is read behind a small seam (`IClimbInput` / a single bool "is holding climb" per frame)
  so swapping hold-to-climb for tap-to-climb or another scheme later touches one file, not the
  motor or session logic.
- The altitude/height readout from the reference is worth keeping visually (a vertical progress
  bar plus a numeric height label reads as an easy, low-risk visual-fidelity win) — added to the
  HUD contract for G2, not read as a confirmed mechanic beyond "display current height."

Escalate to the candidate/Codex if a decision is needed on the specific hazard count/timing before
authoring all five levels (G3); G2's single level uses the placeholder numbers above and calls
them out as assumptions in STATUS/README.
