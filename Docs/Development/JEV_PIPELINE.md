# Jev milestone evidence workflow

Jev is an advisory classification step at G2, G3, G5 and G6. It does not execute Unity, inspect media, grant permission, or mark milestones complete.

1. The implementer supplies a dated report naming the actual assessed source/APK, checks executed, evidence paths and unresolved requirements. Preserve failures and unknowns; do not rewrite the report to obtain a preferred verdict.
2. Codex selects relevant report text and prepares a request with `Tools/Jev/review.py prepare`. Inspect `state.json` before sending. The tool records source hashes, current Git HEAD, requested build revision and optional APK hash.
3. Run one explicit API evaluation using locally configured `TYPESAFE_API_KEY`. Until the key is available to the invoking process, use exported Playground State/Questions and import the plain JSON result. No key belongs in source, chat, the dashboard, or the APK.
4. Jev classifies each requirement as reported verified, reported failed, missing evidence or conflicting evidence. Even all-green results require Codex review. Low confidence is a reason to inspect the evidence, not a calibrated numerical acceptance threshold.
5. Codex checks code, fresh executed test results and device/media evidence. Resolve disagreements by investigating evidence. Only then update STATUS; explicit missing requirements stay open regardless of model confidence.

Run at checkpoints or when material evidence changes, not on every edit. API failure or unavailable quota does not halt ordinary implementation/review. Skip redundant re-evaluations; save the reason when a checkpoint proceeds without Jev.

The existing G2 Playground trials matched the expected classifications on straightforward supplied records. They do not establish independent validation of the APK or measured accuracy on ambiguous reports. G2 remains partial until its outstanding device checks and corrected climbing preview are assessed.

Commands, limits and evidence storage: [Tools/Jev/README.md](../../Tools/Jev/README.md).

Official references: [request/response API](https://docs.typesafe.ai/api), [text-only model capabilities](https://docs.typesafe.ai/models), [known failure modes](https://docs.typesafe.ai/model-jaggedness/jev-1.13).
