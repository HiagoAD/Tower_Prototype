"""Check evidence interpretation and read-only file boundaries using temporary fixtures."""

import hashlib
import tempfile
import unittest
from pathlib import Path

import server


class PanelTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.write("Docs/Development/STATUS.md", """# Development status
Submission target: **2026-09-28 22:00 Recife / 2026-09-29 01:00 UTC**.
User deadline: **09:00 UTC**, interpreted as **2026-09-29 09:00 UTC**.
| Gate | Target, Recife | Status | Evidence |
| --- | --- | --- | --- |
| G0 — baseline | 08:15 | PASS | Device verified. |
| G1 — HTTP proof | 09:15 | NOT VERIFIED | Pending. |

### First report
```text
Gate and result: G0 — PASS
```
""")
        self.write("Docs/Development/ACTION_PLAN.md", """| Recife, Sep 28 | Work | Parallel opportunity | Required evidence / gate |
| --- | --- | --- | --- |
| 08:15–09:15 | HTTP | Tests | **G1:** Actual device request proof. |
""")
        self.write(server.REFERENCE + "ref.txt", "Immutable original")
        digest = hashlib.sha256(b"Immutable original").hexdigest()
        self.write(server.REFERENCE + "SHA256SUMS", digest + "  ref.txt\n")

    def tearDown(self):
        self.temp.cleanup()

    def write(self, path, content):
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(content)
        return file

    def test_records_reload_without_artifacts_promoting_gate(self):
        self.write("Builds/Android/proof.apk", "placeholder")
        self.write("Docs/Development/evidence/g1_proof.png", "placeholder")
        first = server.snapshot(self.root)
        self.assertEqual(first["gates"][1]["status"], "NOT VERIFIED")
        self.assertEqual(first["gates"][1]["acceptance"], "Actual device request proof.")
        self.assertEqual(len(first["apks"]), 1)
        path = self.root / "Docs/Development/STATUS.md"
        path.write_text(path.read_text().replace("NOT VERIFIED", "PASS"))
        self.assertEqual(server.snapshot(self.root)["gates"][1]["status"], "PASS")

    def test_current_plan_overrides_schedule_and_reloads(self):
        path = self.root / "Docs/Development/ACTION_PLAN.md"
        path.write_text(path.read_text() + "\n| Checkpoint | Current acceptance |\n| --- | --- |\n| G1 | Corrected device still. |\n\n| Order | Current work | Required evidence |\n| --- | --- | --- |\n| 1 — now | Image fidelity | Device comparison. |\n")
        status = self.root / "Docs/Development/STATUS.md"
        status.write_text("Current implementation priority: **Image first**.\n" + status.read_text())
        result = server.snapshot(self.root)
        self.assertEqual(result["gates"][1]["acceptance"], "Corrected device still.")
        self.assertEqual(result["priority"], "Image first.")
        self.assertEqual(result["roadmap"][0]["work"], "Image fidelity")
        path.write_text(path.read_text().replace("Image fidelity", "Climbing feel"))
        self.assertEqual(server.snapshot(self.root)["roadmap"][0]["work"], "Climbing feel")

    def test_not_complete_is_explicit_and_never_passed(self):
        path = self.root / "Docs/Development/STATUS.md"
        path.write_text(path.read_text().replace("NOT VERIFIED", "NOT COMPLETE — one level authored"))
        result = server.snapshot(self.root)
        self.assertEqual(result["gates"][1]["status"], "NOT COMPLETE")
        self.assertFalse(result["issues"])

    def test_unknown_gate_value_is_not_accepted(self):
        path = self.root / "Docs/Development/STATUS.md"
        path.write_text(path.read_text().replace("NOT VERIFIED", "PROBABLY DONE"))
        result = server.snapshot(self.root)
        self.assertEqual(result["gates"][1]["status"], "NOT VERIFIED")
        self.assertTrue(any("unrecognized" in issue for issue in result["issues"]))

    def test_missing_status_does_not_invent_progress(self):
        (self.root / "Docs/Development/STATUS.md").unlink()
        result = server.snapshot(self.root)
        self.assertEqual(result["gates"], [])
        self.assertIsNone(result["target"])
        self.assertTrue(result["issues"])

    def test_partial_report_is_preserved_without_counting_as_pass(self):
        path = self.root / "Docs/Development/STATUS.md"
        path.write_text(path.read_text().replace("NOT VERIFIED", "PARTIAL — see report"))
        result = server.snapshot(self.root)
        self.assertEqual(result["gates"][1]["status"], "PARTIAL")
        self.assertEqual(result["gates"][1]["reportedStatus"], "PARTIAL — see report")
        self.assertEqual(sum(g["status"] == "PASS" for g in result["gates"]), 1)
        self.assertFalse(result["issues"])

    def test_deadline_timezones_and_report_body(self):
        result = server.snapshot(self.root)
        self.assertEqual(result["target"], "2026-09-28T22:00:00-03:00")
        self.assertEqual(result["deadline"], "2026-09-29T09:00:00Z")
        self.assertEqual(result["reports"][0]["body"], "Gate and result: G0 — PASS")

    def test_reference_change_is_detected_without_rewriting_manifest(self):
        manifest = (self.root / server.REFERENCE / "SHA256SUMS").read_bytes()
        self.assertTrue(server.reference_integrity(self.root)["ok"])
        self.write(server.REFERENCE + "ref.txt", "Changed reference")
        self.assertFalse(server.reference_integrity(self.root)["ok"])
        self.assertEqual((self.root / server.REFERENCE / "SHA256SUMS").read_bytes(), manifest)

    def test_file_access_is_limited_and_symlinks_do_not_escape(self):
        self.assertIsNotNone(server.allowed_file(self.root, "Docs/Development/STATUS.md"))
        self.write(".git/config", "private")
        self.assertIsNone(server.allowed_file(self.root, ".git/config"))
        self.assertIsNone(server.allowed_file(self.root, "Docs/../.git/config"))
        self.assertIsNone(server.allowed_file(self.root, "../../etc/passwd"))
        private = self.write("secret.md", "private")
        (self.root / "Docs/linked.md").symlink_to(private)
        self.assertIsNone(server.allowed_file(self.root, "Docs/linked.md"))
        with tempfile.TemporaryDirectory() as other:
            outside = Path(other) / "outside.md"
            outside.write_text("private")
            (self.root / "Docs/outside.md").symlink_to(outside)
            self.assertIsNone(server.allowed_file(self.root, "Docs/outside.md"))


if __name__ == "__main__":
    unittest.main()
