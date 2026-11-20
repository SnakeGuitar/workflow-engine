# Changelog

## requirements.md

| Version | Date | Change |
|---|---|---|
| 0.1–0.13 | 2026-11-12 through 13 | Specification expanded beyond assignment: Repairs/Appointments packages, WhatsApp/Messenger/Telegram, owner inbox, plugins, agent discovery (EXT), harness experiment. Details in Git (last commit of that stage: `79bf38e`). |
| 1.0 | 2026-11-15 | **Restructure to assignment scope plus role-based access (D-23 through D-26).** See below. |
| 1.1 | 2026-11-17 | English UI/code/contracts; equivalent English/Spanish documentation. R-04 and CA-N09a require English, YYYY-MM-DD and 24-hour time. Stable IDs and committed criteria retained. |

## architecture.md

| Version | Date | Change |
|---|---|---|
| 0.1–0.11 | 2026-11-12 through 13 | Architecture for expanded specification (`Cases`, `Discovery`, `Sdk`; ADR-01 through ADR-21). Details in Git. |
| 1.0 | 2026-11-15 | Restructure to new scope. See below. |
| 1.1 | 2026-11-17 | English identifiers, roles, API paths/header, context paths and samples; bilingual documentation paths and protection. Scope keys are `description`, `committed`, `support`; `spec` points to the English specification. |

## Restructure 1.0 (2026-11-15)

Scope becomes `project-idea.txt` plus sign-in with two fixed roles. The assignment example replaces repair tracking as the demo. Keep and adapt the harness/experiment. Rewrite the 2026-11-13 harness report as [harness-and-milestones.md](harness-and-milestones.md); remove the other two reports and `abiertas.md` (retained in Git; current questions move to `context.md`). Rebuild diagrams in `design/diagrams/`.

**Retired IDs** (never reused):
- **RF:** RF-08, RF-15, RF-17, RF-20 through RF-29, RF-31 through RF-38, RF-41, RF-42, RF-50 through RF-53, RF-60 through RF-63, RF-70 through RF-72, RF-80 through RF-82, RF-92 through RF-94, and all their CA.
- **CA of current RF:** CA-02e, CA-06b, CA-06c, CA-06d, CA-07b, CA-07c, CA-12c, CA-13b, CA-14e, CA-90e.
- **RN:** RN-01 through RN-05, RN-07, RN-08, RN-13 through RN-28, RN-31 through RN-38.
- **RNF:** RNF-01 through RNF-06, RNF-10 through RNF-13 and their CA-N; CA-N07b and CA-N07c from RNF-07.
- **RH:** RH-15 through RH-18 (corpus, discovery metrics, recorded responses, plugin as controlled change).
- **R:** R-03, R-05 through R-07.
- **ADR:** ADR-11 through ADR-14, ADR-17, ADR-19 through ADR-21 (ADR-20 step atomicity moves to ADR-03.2).
- **AR:** AR-02, AR-10 through AR-15, AR-19.
- **D, OQ, H, RI, AD:** all except D-01, D-06, D-15 through D-17, OQ-07, OQ-08, AD-04, RI-01, RI-03 through RI-05, RI-13, AR-R1 through AR-R4.

**New:** RF-19 (example workflow); RN-39 (events), RN-40 (conditions), RN-41 (variables); CA-04c, CA-04d, CA-06f, CA-06g, CA-13c, CA-16g, CA-30d, CA-30e, CA-30f, CA-40f, CA-90g; D-23 through D-26.

**Meaning changes:**
- Events change from “case enters state” to external events with type, identifier, data (RF-04, CA-04a, CA-04b, CA-11a, CA-13a).
- Conditions have yes/no outputs; missing data follows no (RF-05, CA-05a, CA-05b, RN-10).
- Actions: Send email, Send SMS, Call webhook (RF-06). Webhooks store responses in context (RF-16).
- Exhausted retries or nonretryable action errors cause Failed; remove “Completed with failure” and the inbox (RN-09, RN-29, §5.1, CA-09b, CA-16c).
- Remove execution state Cancelled: no case state changes to cancel executions.
- RN-06: a delivery is an execution plus node. CA-07a uses 2 h.
- RF-30: Editor/Reader replace Owner/Operator; configuration-seeded accounts; integration key for events (CA-30a, CA-30b). RNF-07 becomes CA-N07a, extended to the integration key.
- Simulator limited to email/SMS/HTTP POST with configurable responses and no sign-in (RF-90, RF-91).
- RH-01: pending means all uncommitted CA (no priorities). RH-02/AR-09 stop protecting `tests/Fixtures/`. RH-10 measures all RF. RH-11 changes its examples.
- `harness/scope.json`: add CA-30a and CA-19b to committed scope.
- AR-01: `Core` does not reference infrastructure; no `Cases`, `Discovery`, `Sdk` (ADR-07). AR-05 limited to passwords/integration key. AR-08 reports all uncommitted CA as pending.
- ADR-08: Editor/Reader and integration key. ADR-09 removes recorded responses and real-model evaluation. ADR-18 expands rationale for omitting the recommended architecture.

## Language migration 1.1 (2026-11-17)

User instruction D-27 authorizes English throughout the project and English/Spanish documentation. English is authoritative at `docs/en/`; Spanish lives at `docs/es/`. File names and executable examples use English in both. JPG exports are regenerated from current Mermaid sources in both languages. No application code or executable harness exists yet, so this migration changes specifications, examples and scope metadata only. No approval commit trailer is authored by the agent.

## 1.2 — Personal connections

RF-100, ADR-100 and RH-100 specify personal connections, provider operations and separate local and live evidence.

## 1.3 — Extensible services and local artifacts

RF-101, RF-102 and ADR-101 define independent provider installation, offline tasks and artifact publication. Live evidence is declared per criterion.
