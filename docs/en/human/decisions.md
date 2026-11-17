# Decisions

## Product

| ID | Decision | Status |
|---|---|---|
| D-23 | **Scope is the assignment ([project-idea.txt](../project-idea.txt)):** node editor, parser/DAG compiler, asynchronous engine with waits/retries, webhooks and email/SMS/HTTP POST simulator. Only D-25 access is added. | Team decision (2026-11-15) |
| D-24 | Demo the assignment's example (user registration → wait 2 h → check → email and support notification) as the RF-19 example workflow. No specific business domain. | Team decision (2026-11-15) |
| D-25 | **Limited role-based sign-in:** fixed Editor/Reader roles, configuration-seeded accounts at installation, no UI user management. Source systems use an integration key. | Team decision (2026-11-15) |
| D-26 | **Keep the harness and experiment** (RH-01 through RH-14), adapted to the new scope. Operational plan: [harness-and-milestones.md](harness-and-milestones.md). | Team decision (2026-11-15) |
| D-01 | Workflows are acyclic graphs. Bounded repetitions (retries) are engine properties, not diagram cycles. | Confirmed (2026-11-12) |
| D-06 | The visual editor is mandatory and minimal. | Confirmed |
| D-15 | **Simulate** email, SMS and HTTP POST third-party services. | Adjusted 2026-11-15 by D-23 |
| D-16 | The simulator is both a product component and the harness test double. | Confirmed |
| D-17 | Modular monolith and PostgreSQL; no microservices, Saga or Redis. Execution is a finite state machine. Rationale: [ADR-18](../architecture.md#adr-18-the-assignments-recommended-architecture-is-not-adopted). | Confirmed (instructor stated the assignment architecture is a recommendation) |
| D-27 | English product UI, code, identifiers, API contracts and configuration; equivalent English/Spanish documentation. Keep stable requirement IDs. | User instruction (2026-11-17) |

Other decisions (D-02 through D-05, D-07 through D-14, D-18 through D-22: repairs, appointments, Meta channels, agent discovery, open source) are **retired by D-23** and remain in Git history. Harness decisions are in [harness-and-milestones.md](harness-and-milestones.md), section 4.

## Architecture: rationale and alternatives

Current ADRs live in [architecture.md](../architecture.md). This document explains their rationale.

### Architectural forces

| Force | Source | Consequence |
|---|---|---|
| Waits survive restarts without duplicate deliveries | RF-10, RF-11, RN-06 | **Persistent, transactional** execution state and waits (ADR-03) |
| Test hours-long waits without waiting | RNF-08, CA-07a | Time as an **injectable dependency** (ADR-04) |
| Third-party failures | RF-09, RN-29 | Actions as a **port**, simulator failure injection (ADR-05, ADR-15) |
| Assignment technical tasks | T1–T4 | DAG compiler, interpreter, state machine, webhook, simulator (ADR-15, ADR-16) |
| Agents coding under a measured harness | RH-01 through RH-14 | **Rigid, machine-verifiable** structure, familiar technology, separate oracle (ADR-09) |
| 2 people, 17 days | R-02 | Minimum needed to satisfy requirements; no distributed infrastructure (ADR-18) |
| People must defend the code | — | A stack the team knows (ADR-02) |

### ADR-02 Stack
- **Motivation:** the team knows .NET; C# offers strong typing, good agent support and mature structural enforcement tools.
- **Rejected:** Blazor for an all-C# solution (the team considers it insufficiently mature for the node editor). The OpenAPI boundary (ADR-08) makes later UI replacement inexpensive.
- **Tradeoff:** two languages; a thin TypeScript layer.

### ADR-03 PostgreSQL persistence
- **Replaces:** Redis/distributed locks. One transactional store is sufficient and avoids cross-store inconsistency (ADR-18).

### ADR-06 Interpreted workflow data
- **Rejected:** generating code from diagrams, which is harder to validate and audit.

### ADR-15 Simulator
- **Rejected:** a tests-only double without a demo application; the assignment requires a simulator.

### ADR-18 Recommended architecture
- **Risk:** the rubric may reward the recommendation even if optional (RI-13). Prepared response: ADR-18 comparison, CA-13c demonstrating Redis is unnecessary, and a scaling path using more workers on the same database.
