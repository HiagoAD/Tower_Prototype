import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import review


class ReviewTests(unittest.TestCase):
    def setUp(self):
        self.request = {"model": review.MODEL, "state": {"gate": "G2", "assessed_build": "test"},
                        "questions": review.questions("G2")}
        self.response = {"model": review.MODEL, "answers": {
            key: {"type": "choice", "choice": "missing_evidence", "confidence": 1,
                  "probabilities": {choice: int(choice == "missing_evidence") for choice in review.CHOICES}}
            for key in self.request["questions"]}}

    def test_missing_answer_is_rejected(self):
        del self.response["answers"]["impact_audio"]
        with self.assertRaises(ValueError):
            review.validate_response(self.response, self.request)

    def test_non_object_response_rejected(self):
        with self.assertRaises(ValueError):
            review.validate_response([], self.request)

    def test_invalid_confidence_and_model_rejected(self):
        for value in [float("nan"), 2, True]:
            response = copy.deepcopy(self.response)
            response["answers"]["impact_audio"]["confidence"] = value
            with self.assertRaises(ValueError):
                review.validate_response(response, self.request)
        self.response["model"] = "other"
        with self.assertRaises(ValueError):
            review.validate_response(self.response, self.request)

    def test_unknown_choice_rejected(self):
        self.response["answers"]["impact_audio"]["choice"] = "PASS"
        with self.assertRaises(ValueError):
            review.validate_response(self.response, self.request)

    def test_stale_all_green_result_never_approves_or_changes_status(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            status = root / "STATUS.md"
            status.write_text("PARTIAL")
            run = root / "run"
            run.mkdir()
            manifest = {"head_at_preparation": "old", "request_sha256": "hash",
                        "files": [{"path": "STATUS.md", "sha256": review.sha(b"older")} ]}
            for answer in self.response["answers"].values():
                answer["choice"] = "reported_verified"
                answer["probabilities"] = {c: int(c == "reported_verified") for c in review.CHOICES}
            with patch.object(review, "ROOT", root), patch.object(review, "revision", return_value="new"):
                review.record(run, self.request, manifest, self.response, "test fixture")
                with self.assertRaises(ValueError):
                    review.record(run, self.request, manifest, self.response, "test fixture")
            text = (run / "review.md").read_text()
            self.assertIn("STALE", text)
            self.assertIn("REQUIRES CODEX REVIEW", text)
            self.assertEqual(status.read_text(), "PARTIAL")

    def test_payload_tampering_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            folder = root / review.RUNS / "test"
            folder.mkdir(parents=True)
            review.write_json(folder / "request.json", self.request)
            review.write_json(folder / "manifest.json", {"request_sha256": "mismatch"})
            with patch.object(review, "ROOT", root), self.assertRaises(ValueError):
                review.load_run(str(folder))

    def test_reference_cannot_be_selected_as_output(self):
        with self.assertRaises(ValueError):
            review.load_run("Docs/Reference/Unity-technical-test")


if __name__ == "__main__":
    unittest.main()
