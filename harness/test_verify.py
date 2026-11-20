"""Regression tests for release decisions and bilingual contract drift."""
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

from verify import contract, evaluate, live_evidence


class DeliveryGateTests(unittest.TestCase):
    def results(self, outcomes, required=None, live=False):
        case = "CA-100g"
        method = "CA_100g_CreateDocument"
        root = ET.Element("TestRun", xmlns="urn:trx")
        results = ET.SubElement(root, "Results")
        for outcome, message in outcomes:
            node = ET.SubElement(results, "UnitTestResult", testName=method, outcome=outcome)
            error = ET.SubElement(ET.SubElement(node, "Output"), "ErrorInfo")
            ET.SubElement(error, "Message").text = message
        with tempfile.TemporaryDirectory() as folder:
            trx = Path(folder) / "results.trx"
            ET.ElementTree(root).write(trx)
            return evaluate(trx, required or {case}, {(case, method)}, live)

    def test_all_scenarios_must_pass(self):
        report = self.results([("Passed", ""), ("Failed", "private provider data")])
        self.assertEqual(report["passed"], 0)
        self.assertEqual(report["errors"], ["CA-100g: expected Passed, observed Failed"])

    def test_passed_and_missing_reported_separately(self):
        report = self.results([("Passed", "")], {"CA-100g", "CA-100b"})
        self.assertEqual(report["passed_ids"], ["CA-100g"])
        self.assertEqual(report["errors"], ["CA-100b: missing result"])

    def test_unavailable_requires_live_inconclusive_marker(self):
        outcomes = [("Inconclusive", "provider_unavailable")]
        self.assertEqual(self.results(outcomes, live=True)["blocked_ids"], ["CA-100g"])
        self.assertTrue(self.results(outcomes)["errors"])
        self.assertTrue(self.results([("NotExecuted", "")], live=True)["errors"])

    def test_live_report_excludes_unrecognized_fields_and_rejects_raw_ids(self):
        references = dict.fromkeys(("shopify_order", "openai_response", "google_document"), "a" * 64)
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "evidence.json"
            path.write_text(json.dumps(references | {"token": "private"}))
            self.assertEqual(live_evidence(path, tuple(references)), references)
            path.write_text(json.dumps(references | {"google_document": "private"}))
            self.assertIsNone(live_evidence(path, tuple(references)))

    def test_live_profiles_and_offline_tasks_have_independent_gates(self):
        scope = json.loads(Path(__file__).with_name("scope.json").read_text())
        errors, live = contract(scope)
        self.assertEqual(errors, [])
        self.assertEqual(live["CA-101c"], ("service_operation", "remote_result"))
        self.assertNotEqual(live["CA-101c"], live["CA-100g"])
        self.assertTrue({"CA-101a", "CA-101b", "CA-102a", "CA-102b", "CA-102c",
                         "CA-102d", "CA-102e"} <= set(scope["committed"]) - live.keys())

    def test_unknown_scope_and_wrong_revision_are_rejected(self):
        path = Path(__file__).with_name("scope.json")
        scope = json.loads(path.read_text())
        scope["committed"].append("CA-999z")
        scope["spec"] = "obsolete"
        errors, _ = contract(scope)
        self.assertIn("undefined scope criterion CA-999z", errors)
        self.assertIn("scope.spec does not match requirements", errors)
