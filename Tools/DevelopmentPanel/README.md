# Development control panel

A local, read-only dashboard for Tower Prototype. It follows the development records and artifacts on disk, without changing Unity assets or gate results. Requires Python 3.9 or newer; no packages, account connections, or build step.

From the project root:

```sh
python3 Tools/DevelopmentPanel/server.py
```

Open **http://127.0.0.1:8765**. Stop the server with Ctrl+C. If that port is occupied, add `--port 8766` and open the corresponding URL. The server binds only to this computer's loopback interface.

## What it follows

- **Overview:** immediate priority and ordered execution plan, reported gate progress, the next unpassed gate, submission target, source freshness, repository revision, level configuration count, and reference integrity.
- **Milestones:** searchable and filterable gate results with acceptance criteria and recorded evidence.
- **Evidence & builds:** downloadable APKs, screenshot previews, playable recordings, build logs, and imported license records.
- **Risks & decisions:** the risk/owner/action table and quota observations.
- **Project documents:** searchable, read-only source documents.

The page refreshes every 15 seconds while visible, when returning to the tab, or when pressing Refresh. If the server stops, the page labels its last successful snapshot as disconnected.

## Updating the information

Continue updating [STATUS.md](../../Docs/Development/STATUS.md) and [ACTION_PLAN.md](../../Docs/Development/ACTION_PLAN.md) through the Claude-owned workflow in [AGENTS.md](../../AGENTS.md). The panel reads them on every refresh. No second progress database needs to be maintained.

Keep the existing Markdown table headings and columns:

- STATUS: `Gate`, `Item`, and `Account / observation`.
- ACTION_PLAN: the schedule table (`Recife, …`) and `Level`; `Order / Current work / Required evidence` supplies the current execution sequence, and `Checkpoint / Current acceptance` overrides historical schedule criteria.
- STATUS: `Current implementation priority:` supplies the overview priority.
- Append dated implementation reports with `###` headings in STATUS.

Gate states supported: `PASS`, `FAIL`, `NOT VERIFIED`, `NOT COMPLETE`, `IN PROGRESS`, `PARTIAL`, and `BLOCKED`, including annotations such as `PARTIAL — see report`. Unknown values are shown as NOT VERIFIED with a warning. The original status wording remains in the milestone details. The panel uses the gate table as its status source; an appended report does not silently override that table.

It scans:

- `Builds/**/*.apk` for Android artifacts.
- `Builds/**/*.mp4`, `Builds/**/*.mov`, and videos in `Docs/Development/evidence/` for recordings.
- Images in `Docs/Development/evidence/` for screenshots. A `g2_` filename associates an image with G2, without asserting acceptance.
- `Assets/Game/Levels/*.asset` for configuration count only.
- `Assets/Game/Art/**/LICENSE*` (including common capitalization variants) for license records.
- `Logs/*.log` and `Logs/*.txt` for the twelve most recent logs. The viewer shows at most the last 250 KB; downloads include the full file.

Submission and interpreted deadline timestamps come from STATUS. Schedule dates use the submission target date; this dashboard is designed for the current same-day plan. Recife is UTC−03:00. The hard deadline remains explicitly marked as interpreted until its source is updated.

## Evidence boundaries

A PASS is a **reported** result, not a new automated acceptance decision. APK presence, screenshots, and level files do not prove successful installation, five completed levels, or correct gameplay. The percentage is the fraction of recorded gates passed, not estimated project completion. The panel does not monitor agent sessions, account balances, the Unity Editor, or a connected Android device.

Reference hashes are verified against the existing immutable `SHA256SUMS`. Unchanged file hashes are cached using size and filesystem timestamps. Nothing in the reference directory is written. The server exposes only the dashboard, documents, and permitted artifact types; it does not expose the repository as an unrestricted file server.

## Verification

```sh
python3 -m unittest discover -s Tools/DevelopmentPanel -p 'test_*.py' -v
node --check Tools/DevelopmentPanel/app.js
```

The Python tests use temporary project fixtures for changed reports, unknown gate values, missing records, checksum mismatches, and file-path boundaries. They do not change project evidence or the reference snapshot.
