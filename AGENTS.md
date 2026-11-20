# AGENTS.md

Web workflow engine: node editor → validation and DAG compilation → asynchronous engine with waits/retries → webhooks and email/SMS/HTTP POST simulator, with sign-in and two roles (Editor and Reader). Scope: [project-idea.txt](project-idea.txt) plus role-based access, personal connections, extensible services and local artifact tasks (RF-100, RF-101, RF-102). Deadline: 2026-12-02.

## What to read

- [docs/en/requirements.md](docs/en/requirements.md): what to build. **CA IDs are the contract.** Read the task's RF/CA plus §4 (glossary), §5 (states), §6 (rules).
- [docs/en/architecture.md](docs/en/architecture.md): how. Current ADRs, solution structure (ADR-07), machine-verifiable **AR** rules (§3).
- [docs/en/design/diagrams/](docs/en/design/diagrams/): UML views. Requirements and architecture take precedence over conflicting diagrams.
- `docs/en/human/`: team context, decisions and history. **Do not read for implementation.**
- [docs/es/](docs/es/): equivalent Spanish documentation. English is authoritative; update both languages together.

## Hard rules

- Write code, identifiers, comments, UI text, API contracts, configuration keys and file names in **English**. Documentation has **English and Spanish** versions; executable examples use English in both.
- Preserve stable RF, RNF, RN, CA, RH, AR, ADR and CU IDs across languages. Their prefixes are identifiers, not code naming conventions.
- Implementation tasks name target RF/CA (RH-03); ask for clarification if absent. Documentation-only maintenance names the relevant specification/rule IDs and records checks instead of inventing product criteria.
- Nothing beyond `docs/en/requirements.md` is implemented.
- Do not modify `tests/Acceptance/`, either requirements document, section 3 of either architecture document or `harness/scope.json` without human approval (a commit `Approved-By:` line naming the PR approver). Do not author that line yourself. Explicit user instructions take precedence; never fabricate review evidence.
- Committed scope lives in `harness/scope.json` (`spec`, `description`, `committed`, `support`).
- Record each task in `harness/tasks.ndjson` using the harness `task` command (RH-04). Until the harness exists, append an honest manual record identifying that limitation.
- Every acceptance test carries its CA ID (`[Ac("CA-07a")]`). Failures cite the CA.
- Never log passwords, provider credentials or integration keys (AR-05).
- Obtain time only through `TimeProvider` (AR-03). Never `DateTime.Now`/`UtcNow`.
- One-way dependencies: `Api → Infrastructure → Core`, `Api → Core`. `Core` knows no database, HTTP or ASP.NET (AR-01). No project references `Simulator` (AR-16).
- No code file exceeds 400 lines (AR-07).

## Stack

.NET LTS + ASP.NET Core, PostgreSQL (EF Core/Npgsql), React + TypeScript (React Flow). Modular monolith, one instance; no microservices, Saga or Redis (ADR-18). Verify package versions at installation.

## Documentation changes

Requirements and architecture describe only the current state, without change history or “previously” notes. History belongs in `docs/en/human/changelog.md` and its Spanish counterpart. Never reuse IDs. Keep both language versions equivalent; all API paths, JSON examples, class/member names and configuration keys remain English.
