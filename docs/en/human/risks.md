# Risks

Harness risks are in [harness-and-milestones.md](harness-and-milestones.md), section 1.5.

| ID | Risk | Mitigation |
|---|---|---|
| RI-01 | Short deadline (17 days) | Limit scope to assignment plus access (D-23, D-25); requirements §1 cuts; milestone stop criteria |
| RI-03 | Requirement changes contaminate measurements | Versioned specification and version-tagged measurements (RH-13) |
| RI-04 | Exhausted model usage | Per-task budget cutoff (RH-14) |
| RI-05 | Team cannot explain agent-generated code | Human review of every change (RH-12); separate oracle for committed CA (ADR-09) |
| RI-13 | Rubric values recommended architecture despite being optional | ADR-18 justifies every choice; CA-13c demonstrates Redis is unnecessary |
| AR-R1 | Team lacks React experience; editor is the newest part | Minimal React Flow editor (D-06); OpenAPI contract; Playwright tests |
| AR-R2 | Concurrency and recovery are hard to implement correctly | CA-10, CA-11, CA-13 in H2 before the example workflow; stop criterion on November 24 |
| AR-R3 | Exactly-once delivery does not exist across systems | ADR-03: at-least-once plus idempotency, unique reference deduplicated by receiver |
| AR-R4 | Agents violate structure unknowingly | Automated AR rules with remediation messages (RH-07) |
