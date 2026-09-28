# Project instructions

## Immutable project brief

The user has designated the [Unity technical test](Docs/Reference/Unity-technical-test/Unity-technical-test.md) as the gospel and source of truth for this project. Read it and consult its reference image and video before planning or implementing project work. All project decisions and implementation must follow this brief.

Original Notion page: https://app.notion.com/p/Unity-technical-test-3e6959eb5a20808e8bdced6497aaa4bf

Do not modify the original Notion page or any file under `Docs/Reference/Unity-technical-test/` at any point in the project. This includes the Markdown copy, raw source, attachments, provenance, and checksum manifest. Do not rewrite, correct, reformat, regenerate, replace, delete, or update them from Notion. Preserve even apparent errors in the original wording. Do not remove their read-only permissions or regenerate checksums to conceal changes.

Keep plans, progress, interpretations, assumptions, and implementation notes outside that directory. If an implementation issue conflicts with the brief, surface it explicitly without changing the reference.

To verify the snapshot, run `shasum -a 256 -c SHA256SUMS` from `Docs/Reference/Unity-technical-test/`.

## Development coordination

Codex owns planning, prioritization, and milestone review. Claude owns implementation and integration. Follow the [action plan](Docs/Development/ACTION_PLAN.md) and [Claude handoff](Docs/Development/CLAUDE_HANDOFF.md); record progress and evidence in [development status](Docs/Development/STATUS.md). These working documents never override the immutable brief.

Quota fallback (standing rule): if either Codex or Claude hits its usage limit while the other still has quota, the available one takes over both roles (planning/review and implementation) until the other is back, then the normal split resumes. When taking over, preserve the other agent's uncommitted edits, and make sure only one agent drives the Unity Editor, builds or the device at a time. Use the advisory [Jev evidence workflow](Tools/Jev/README.md) at milestone reviews; Jev classifications do not authorize PASS or replace executed tests and device observations.

The user has ruled out asset generation for this project. Use existing free, appropriately licensed art and audio; preserve source and license records. Do not generate new art/audio or introduce an asset-generation pipeline.
