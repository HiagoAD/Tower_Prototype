# Jev evidence checks

Python 3.9+ standard library; development tooling only. No Unity dependency.

At G2/G3/G5/G6, the implementer writes a focused report with the assessed APK revision, real results, evidence paths and explicit unknowns. Codex prepares a Jev request from that report, then reviews Jev's classifications against source/test/device evidence. Jev never edits STATUS or approves a milestone. Keep expected answers out of the supplied state. Send original relevant report excerpts, including negative findings, rather than a summary written to obtain a preferred verdict.

```sh
python3 Tools/Jev/review.py prepare --gate G2 --build-revision BUILD_REVISION --report Docs/Development/STATUS.md --apk Builds/Android/TowerPrototype.apk
```

The command prints a new directory under `Docs/Development/jev/`. Large reports are rejected instead of truncated: create a focused, dated report outside the immutable reference directory when needed. `--report` can be repeated. APK binaries are hashed locally, never uploaded. Explicitly supplied text files are sent in full when running the API command; review `state.json` first.

Set `TYPESAFE_API_KEY` locally in the environment of the terminal/agent that will run this command. Do not place the value in chat, source files or the APK. An export in a separate terminal does not update an already-running agent's environment.

```sh
python3 Tools/Jev/review.py run Docs/Development/jev/RUN_DIRECTORY
```

One API call, 15-second timeout, no background polling or automatic retries. Missing keys, service failures and invalid responses remain unverified; continue ordinary code/device review. For 429/529, wait before manually retrying. Uses the official endpoint and pins `jev-1.13.0`. Existing results are never overwritten. File hashes and Git HEAD are checked for freshness before sending; supplied build revision remains the author's assertion, not proven build provenance.

Playground fallback: paste `state.json` and `questions.json` into their fields, select the pinned model, then save the raw JSON response locally and import it:

```sh
python3 Tools/Jev/review.py import Docs/Development/jev/RUN_DIRECTORY --response /private/tmp/jev-response.json
```

Export plain JSON, not HTML-escaped chat text. Playground request/response pairing is manually asserted and labeled accordingly. Imported stale evidence is marked STALE, never silently treated as current.

Every completed run saves `response.json`, metadata and `review.md`. Results always require Codex review, even with unanimous high-confidence answers. Jev assesses text claims; it cannot inspect referenced screenshots, audio or video. Code checks exact hashes/counts; humans and device tests establish actual behavior. Do not send credentials or full unrelated logs as report inputs.

Verification: `python3 -m unittest discover -s Tools/Jev -p 'test_*.py' -v`

Official sources checked September 28, 2026: [API](https://docs.typesafe.ai/api), [models and text-only input](https://docs.typesafe.ai/models), [known limitations](https://docs.typesafe.ai/model-jaggedness/jev-1.13).
