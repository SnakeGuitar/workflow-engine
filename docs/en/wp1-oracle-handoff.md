# WP1 assignment for the independent acceptance agent

Start in a fresh session without implementation history. Your deliverables are the public OpenAPI contract and one runnable .NET acceptance project under `tests/Acceptance/`. The human reviewer owns approval and freezing of the oracle. This assignment was prepared against `7837577`, specification v1.3. Record the actual base SHA when you start.

## Inputs and independence

Read [AGENTS.md](../../AGENTS.md), the glossary, states, rules and target CA in [requirements.md](requirements.md), and ADR-08 and ADR-09 in [architecture.md](architecture.md). Use [scope.json](../../harness/scope.json) for coverage and [verify.py](../../harness/verify.py) for runner conventions. Derive expected behavior solely from the CA and the public contract. Keep product source, implementation PRs, unit tests and implementation chat history outside the oracle session. Use the public test-host and clock interfaces in `tests/Support/` for wiring. If a fixture capability is missing, describe what observation or control the CA requires. Refer unresolved contract decisions to the reviewer.

## Coverage for v1.3

| Run | Criteria |
|---|---|
| Local · diagrams and versions | CA-02a, CA-02c, CA-02g, CA-14a, CA-14b, CA-14d, CA-03b |
| Local · execution and deliveries | CA-04a, CA-07a, CA-09a, CA-10b, CA-16a, CA-16b, CA-19b |
| Local · access and editor | CA-30a, CA-40a, CA-40d, CA-40e |
| Local · connections and extensibility | CA-100a, CA-100b, CA-100c, CA-100d, CA-100e, CA-100f, CA-101a, CA-101b |
| Local · artifacts | CA-102a, CA-102b, CA-102c, CA-102d, CA-102e |
| Live · dedicated accounts | CA-100g, CA-101c |
| Supporting scenarios, reported separately | CA-10a, CA-12a, CA-90a |

## Work sequence

1. Draft OpenAPI in English from the public requirements. Describe the requests, responses, schemas, security and error behavior needed to drive the scenarios. Include session cookies, Editor/Reader permissions, integration-key event intake, workflows, executions, connections, catalog and artifact downloads. Identify contract choices that need human resolution before encoding them as assertions.
2. Write black-box scenarios in one project directly under `tests/Acceptance/`. Use the application factory, real test PostgreSQL, a controllable clock and controlled services for local runs. Exercise UI criteria in Playwright. Advance the clock for timers and retries. Check observable results, including the absence of side effects where required.
3. Tag every method with its exact CA and a matching name, for example `[Ac("CA-07a")]` and `CA_07a_TimerResumesAfterDelay`. Set the test trait so the runner selects `TestCategory=local` or `TestCategory=live`. Each failing assertion identifies the CA, the expected result and the observed result. Exercise the behavior even when implementation is pending.
4. For each live CA, independently read the result at the provider. On success write `<CA-ID>.json` under the runner-supplied `LIVE_ACCEPTANCE_EVIDENCE` directory. Use the evidence keys declared on that CA and one 64-character lowercase hexadecimal redacted reference per key. Keep credentials and provider payloads out of logs and reports. Record unavailable live prerequisites as blocked.
5. Publish small, compilable commits as each step is checked. Run the warnings-as-errors build, Unit, Architecture, contract, and whitespace checks before committing. Record genuine acceptance failures in the PR so the implementer can act on them. Record the task under RH-04. A human reviews the contract and protected tests, supplies the required approval under AR-09 and freezes the oracle.

## Run and report

From the repository root, run the contract check before and after the work. With the project and its local prerequisites available, run the local suite.

```sh
python3 harness/verify.py contract
python3 harness/verify.py local
```

The local suite must discover all 31 committed local criteria and finish within 600 seconds. Report required, covered and passed counts separately, alongside each failing or missing CA. Diagnose runner or discovery failures separately from failed product assertions. For dedicated, authorized real accounts run `RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py live`. Use `python3 harness/verify.py release` for the combined verdict. Exit codes are 0 for passed, 1 for failed and 2 for blocked. Include the base SHA, specification version, actual verdicts and human review status in the handoff. WP1 establishes the reviewed measuring instrument; subsequent packages implement the behavior it measures.
