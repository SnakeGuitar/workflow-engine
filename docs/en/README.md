# English documentation

[Español](../es/README.md) · [Project](../../README.md)

- [Requirements and acceptance criteria](requirements.md)
- [Architecture and enforceable rules](architecture.md)
- [UML diagrams](design/diagrams/README.md)
- [Course assignment](project-idea.txt)
- Team context: [context](human/context.md), [decisions](human/decisions.md), [risks](human/risks.md), [harness and milestones](human/harness-and-milestones.md), [changelog](human/changelog.md)

Requirements and architecture take precedence over diagrams. English is authoritative; maintain equivalent Spanish documents. Stable IDs are shared across languages. Product UI, code, comments, API paths, JSON properties and code examples use English.

## Delivery verification

Run from the repository root with Python 3.9+.

```sh
python3 harness/verify.py contract
python3 harness/verify.py local
RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py live
RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py release
```

The contract gate checks identifiers, catalog mappings and versions. Review bilingual meaning manually. The local/live gates run a fresh .NET acceptance project from `tests/Acceptance/` and read its TRX. Tag tests with `[Ac("CA-100a")]`, use the method prefix `CA_100a_` and expose TestCategory `local` or `live`. Provision dedicated provider accounts before opting into live execution. Exit codes are 0 passed, 1 failed and 2 blocked. Release requires both suites.

A live oracle that cannot reach a required provider or obtain test credentials records an inconclusive result with the exact message `provider_unavailable`. Ordinary skips and assertion failures fail the gate.

Each live criterion declares its evidence keys in the specification. After independent verification the oracle writes one JSON file per criterion (e.g. `CA-101c.json`) in the directory specified by `LIVE_ACCEPTANCE_EVIDENCE`. Each file maps its declared keys to lowercase SHA-256 hashes of observed remote references. Missing evidence fails the corresponding criterion.
