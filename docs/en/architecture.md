# Architecture: workflow execution and automation engine

| Field | Value |
|---|---|
| Version | 1.3 |
| Input | [requirements.md](requirements.md) v1.3 |
| Rationale and alternatives | [human/decisions.md](human/decisions.md) (not needed for implementation) |
| Harness and milestones | [human/harness-and-milestones.md](human/harness-and-milestones.md) |
| Diagrams | [design/diagrams/](design/diagrams/) |

Each `ADR-xx` records a current decision and its motivating requirements; numbering gaps represent retired ADRs. Package versions are known versions: **verify them at installation**. Section 3 contains executable rules used by RH-07.

## 1. Decisions

### ADR-18 The assignment's recommended architecture is not adopted

The assignment **recommends**, rather than requires, orchestrated microservices with a finite state machine, Saga if needed, PostgreSQL for state and Redis for distributed locks. Adopt only what the requirements justify:

| Recommendation | Decision | Rationale |
|---|---|---|
| Microservices | **No.** Modular monolith, one instance (ADR-01) | No assignment task requires independently deploying or scaling components. With 2 people and 17 days, each extra service adds fallible network calls, versioned contracts and coordinated deployments without improving any CA. Only the simulator runs separately, representing real third parties (ADR-15). |
| Finite state machine | **Yes** (ADR-16) | Execution has explicit states and transitions (requirements §5.1, RF-18). |
| Saga pattern | **No** | Saga compensates transactions distributed across services with separate databases. All state here lives in one database and each advancement is a local transaction (ADR-03). Third-party actions (email, SMS, webhook) are not undone; retry them with a unique reference (RN-06, RN-09). |
| PostgreSQL | **Yes** (ADR-03) | Transactions, row locking and restart recovery (RF-10). |
| Redis distributed locks | **No** | PostgreSQL prevents duplicate work: workers claim rows using `SELECT … FOR UPDATE SKIP LOCKED` and an expiring lease (ADR-03). A second store adds operations and risks lock/state inconsistency after crashes. CA-13c demonstrates two workers on one database. |
| Executable JSON as DAG | **Yes** (ADR-16) | Assignment task 2 (RF-14). |

- **Accepted tradeoff:** no independent scaling by component.
- **If scaling becomes required:** run more worker instances against the same database. The design supports this (CA-13c) without Redis or code changes.

### ADR-01 Modular monolith, single deployment
- **Decision:** one ASP.NET Core application containing the API, UI and background worker, plus a separate simulator application. The MVP runs **one instance**.
- **Correctness with multiple instances:** database claims (ADR-03) prevent two instances from executing the same step (CA-13c).

### ADR-02 Stack: .NET + PostgreSQL + React/TypeScript
- **Backend:** ASP.NET Core, current LTS (known: .NET 10; verify).
- **Data:** PostgreSQL with Entity Framework Core and Npgsql.
- **UI:** React with TypeScript (SPA). React Flow node editor (`@xyflow/react`; verify license and version).

### ADR-03 Durable persistence and execution on PostgreSQL
- **Motivation:** RF-09 through RF-11, RF-13, RN-06, RN-09.
- **Decision:**
  1. **An execution is a row** holding its workflow version, JSON context, current node, state, attempt number and, while waiting, its due timestamp (UTC). Record each step in an **insert-only** table (RN-12).
  2. **Each advancement is a transaction:** update the execution, insert the step and schedule the next work atomically. Only `Infrastructure` opens transactions, behind the `Core` storage port.
  3. **A background worker** polls every 1 s for Pending executions, due Waiting executions and Running executions whose lease expired. Claim them with `FOR UPDATE SKIP LOCKED` and an **expiring lease**: a new claim resumes work after a mid-step crash (CA-10b).
  4. For simulator deliveries: **Deliveries carry a unique reference** (`execution:node`). Before calling a third party, a transaction saves the delivery and reference; after the call, another records the result and advances. If a crash occurs between them, recovery repeats the call with the **same reference** and the receiver discards the duplicate (CA-10c, CA-11b). This is **at-least-once delivery plus idempotency**, not exactly-once delivery.
  5. **A retry waits at the same node:** the execution becomes Waiting until the next attempt (1, 5, 30 min, RN-09), then repeats the node with the next attempt number.
  6. **Event deduplication** uses a unique constraint on (authenticated source, event identifier) (CA-11a).
- **Limit:** timer precision depends on the 1 s polling interval.

### ADR-04 Time is a dependency
- **Motivation:** RNF-08, CA-07a, CA-09a.
- **Decision:** all code gets time through `TimeProvider`; tests use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`). Store timestamps in UTC and add durations as elapsed time.
- **Mechanical enforcement:** AR-03.

### ADR-05 Actions as a port; third parties over HTTP
- **Motivation:** RF-06, RF-16, RN-29.
- **Decision:**
  - `Core` defines `IAction`: name, parameter schema and `ExecuteAsync(resolvedParameters, reference)`, returning **Success** (with status code/body when applicable), **Retryable** or **PermanentFailure**, according to RN-29.
  - `Infrastructure` implements the three RF-06 actions with `HttpClient` (10 s timeout). Email and SMS call simulator receivers; Call webhook uses the node URL. Simulator URLs are configuration.
  - The interpreter resolves variables before calling the action (RN-41) and stores webhook responses in `nodes.<id>` (CA-16g).

### ADR-06 Workflows are validated data interpreted by the engine
- **Motivation:** RF-01 through RF-05, RN-10, RN-11, RN-40.
- **Decision:**
  - A workflow is a **JSON document** stored in **immutable versions**.
  - A **pure validator**, without data access, implements RN-10 and returns all errors (CA-02g).
  - Node types and actions use a **single versioned operation registry**; unknown actions fail validation (CA-06e).
  - **Event** declares an event type. **Condition** declares a context path, operator and value, with “yes”/“no” outputs. **Timer** declares a duration. **Action** declares an action and parameters.
  - Non-Condition nodes have at most one output, so an execution has only one current node (ADR-03).

### ADR-07 Solution structure and dependencies
- **Decision:** projects with one-way dependencies:
  - `Core`: engine (validator, compiler, interpreter, state machine) and ports (storage, `IAction`). No database, HTTP or ASP.NET knowledge.
  - `Infrastructure`: EF Core/PostgreSQL, background worker and HTTP actions. Implements `Core` ports.
  - `Api`: HTTP, authentication and roles, composition and compiled SPA.
  - `Simulator`: separate application; no project references it (ADR-15).

```text
Api ──► Infrastructure ──► Core
 └─────────────────────────►┘
Simulator  (no references; HTTP only)
```

```text
src/
  WorkflowEngine.Core/
  WorkflowEngine.Infrastructure/
  WorkflowEngine.Api/
  WorkflowEngine.Simulator/
  web/                            React + TypeScript (React Flow)
samples/
  activation-reminder.json        assignment example workflow (RF-19)
tests/
  Acceptance/                     one test per CA, tagged with its ID (protected)
  Unit/
  Architecture/                   section 3 rules
  Support/                        application factory, fake clock, CA-tagged assertions
tools/
  Harness/                        CLI: verify, trace, freeze, task, budget, report
harness/
  scope.json                      committed criteria (protected)
  tasks.ndjson                    per-task records (RH-04, append-only)
  metrics/                        traceability and metrics M1–M6
docs/
  en/                             authoritative English documentation
    requirements.md
    architecture.md
    design/diagrams/              UML views
    human/                        context, decisions, risks, changelog
  es/                             equivalent Spanish documentation
README.md
AGENTS.md
project-idea.txt
docker-compose.yml
```

### ADR-08 Contract-based HTTP API, cookie session and integration key
- **Motivation:** RF-30, RNF-07, preventing agent-induced frontend/backend drift.
- **Decision:**
  - JSON REST API with an OpenAPI specification; **generate** the TypeScript client from it. Serve the SPA from the same origin as the API.
  - ASP.NET Core Identity with session cookies; built-in lockout after 5 failures (CA-30c). Fixed roles `Editor` and `Reader`, enforced through authorization policies per resource: both can read, only `Editor` can write (CA-30b).
  - Seed accounts at startup from installation configuration (username, initial password, role). No user-management resources.
  - `POST /api/events` does not use a session: require `X-Integration-Key`, compared with the configured key in constant time (CA-30d).
- **Resources:** `/api/session` (sign in/out), `/api/workflows` (save, validate, activate/deactivate), `/api/events` (receive event, RF-04), `/api/executions` (history, RF-12).
- **Limit:** the cookie requires a shared UI/API origin. Accepted.

### ADR-09 Test strategy and harness
- **Motivation:** RH-01 through RH-14, RNF-08. Operational details (components, CI, milestones) are in [human/harness-and-milestones.md](human/harness-and-milestones.md).
- **Decision:**
  - **Local acceptance tests** exercise the complete application in memory (`WebApplicationFactory`), a real test PostgreSQL database (one container per job), a fake clock and the in-memory simulator as the third-party double.
  - Every test has an ID attribute (`[Ac("CA-07a")]`). `trace` matches IDs against `docs/en/requirements.md` and `harness/scope.json`, calculating CA coverage (RH-01). Failure messages include CA ID and expected behavior (RH-06).
  - **Protected files (RH-02):** `tests/Acceptance/`, both language versions of `requirements.md`, section 3 of both architecture documents, and `harness/scope.json`. CI compares fingerprints and rejects unapproved changes: the main branch requires owner review (CODEOWNERS), and the commit's `Approved-By:` line must name the person approving the PR. Agents must not author that line.
  - **Committed scope:** `harness/scope.json` lists committed CA; traceability (RH-01, AR-08) is measured against it. JSON keys are `spec`, `description`, `committed`, `support`.
  - **Separate oracle:** write committed acceptance tests using only CA text and the OpenAPI contract (ADR-08), without implementation access. A human reviews them before freezing; record the condition in task records (RH-04).
  - Test infrastructure (application factory, fake clock, CA-tagged assertions) lives in unprotected `tests/Support/`.
  - UI tests (RF-40, CA-30e, CA-04c) use Playwright.
  - **Experiment records (RH-04):** each agent task appends a line to `harness/tasks.ndjson` with specification version, base commit, model, task, RF/CA, start/end, iterations, usage, outcome and interventions. These produce M1–M6 (RH-08). `task` rejects missing CA (RH-03) or exhausted budget (RH-14).

### ADR-10 History is domain data; credentials are never logged
- **Motivation:** RNF-07.
- **Decision:** execution history (RN-12, RN-30) is PostgreSQL domain data. Technical logs are structured and correlate by execution identifier. Model passwords and integration keys with dedicated types that **do not serialize** into logs (AR-05).

### ADR-15 Third-party API simulator
- **Motivation:** RF-90, RF-91, RNF-08.
- **Decision:** `WorkflowEngine.Simulator` is a **separate** web application representing the outside world; the engine calls it over HTTP like any third party.
  - **Email**, **SMS** and **HTTP POST** receivers; record deliveries, display them and deduplicate references (CA-90c).
  - HTTP responses are configurable per route, with status code and body (CA-90g), allowing the example to “check” account status.
  - Per-service **failure injection** through the UI or management API (RF-91).
  - **One component, two roles:** demo and **test double**, hosted in memory with a fake clock. Avoid divergent doubles.
- **Limit:** its UI identifies it as a simulation (CA-90f).

### ADR-16 DAG compilation and execution state machine
- **Motivation:** RF-02, RF-14, RF-18, RN-10, RN-30.
- **Decision:**
  - The editor saves the **diagram**. A **pure compiler** validates it (RN-10) and produces the **executable DAG**: stable node identifiers, edges, start node, parameters and precomputed **topological order**. Compilation is deterministic (CA-14b); save by version (RN-11).
  - **The interpreter executes only the compiled document** (AR-17), never the diagram.
  - Execution is a **finite state machine** with the requirements §5.1 table implemented in a single component (AR-18); record every transition (RN-30).

### ADR-100 Provider adapters and personal connections

RF-100 operations share a versioned registry of schemas, permissions and effect policies. The UI consumes catalog metadata through the API. Core validates mappings and compiles operation version and opaque connection ID. Api checks workflow and connection ownership on writes. Infrastructure resolves encrypted per-owner credentials at dispatch, checks revocation, manages provider subscriptions and verifies incoming notifications before deriving their source. The generic integration-key endpoint retains its own source. Bind authorization callbacks to the initiating Editor. Credential rotation preserves connection identity. Keep secrets out of serialized workflow data and observability.

Persist the intent to dispatch before a remote effect. Recover an outstanding provider intent using CA-100e. Local tests use controlled adapters and the fake clock. The separate live oracle creates a fresh order, correlates the provider request and reads the resulting document independently with authorized test accounts (CA-100g). RH-100 requires both verdicts for release.

### ADR-101 Extensible operations and local execution

Core resolves names against an immutable catalog snapshot at compilation. The DAG pins operation version, adapter digest and bindings. Infrastructure loads installed adapter packages at startup and interprets HTTP descriptors from the registry. The UI renders operation schemas. Adding a provider requires only a descriptor or an adapter package. Scope credentials to the authorized destination, including redirects. Apply RF-100 ownership, revocation and uncertain-delivery rules to remote operations.

Infrastructure runs local operations under an isolated identity in a temporary workspace on the engine host. Stage declared inputs read-only, expose a writable output directory, enforce a deadline and deny network access to offline tasks. Package the runtime and declared libraries separately from private host data. Pass arguments as values. Store final bytes durably before committing artifact metadata and advancing the node in one database transaction. A uniqueness constraint on execution, node and output name makes recovery idempotent. Discard abandoned staging files. The API authorizes downloads by owner and streams the stored bytes. Core sees typed artifact references. No provider account is needed for a local-only workflow.

## 2. Execution lifecycle

```text
POST /api/events or authenticated provider notification
   │ deduplicate source and event identifier (CA-11a)
   ▼
One Pending execution per active workflow matching source and type (current version)
   ▼
Worker claims it (SKIP LOCKED + lease)
   ▼
Execute current node:
  Condition: choose yes/no
  Action: deliver with unique reference
  Timer: schedule due time
   ▼
Transaction: record step (RN-12), advance to next node,
             or become Waiting, or finish Completed/Failed
   │
   └── when timer/retry becomes due, worker claims it again
```

## 3. Machine-verifiable rules (used by RH-07)

| ID | Rule | Verification |
|---|---|---|
| AR-01 | `Core` must not reference `Infrastructure`, `Api`, EF Core, ASP.NET Core or `System.Net.Http` | Architecture test (NetArchTest) |
| AR-03 | No calls to `DateTime.Now`, `DateTime.UtcNow` or `DateTimeOffset.UtcNow`; obtain time only through `TimeProvider` | Forbidden API analyzer |
| AR-04 | Third-party HTTP clients exist only in `Infrastructure` | Architecture test |
| AR-05 | Never pass passwords, provider credentials or the integration key to technical logs | Signature architecture test + analyzer |
| AR-06 | Every registered action has at least one CA exercising it | Registry test |
| AR-07 | No code file exceeds 400 lines | CI check |
| AR-08 | Every committed CA in `harness/scope.json` has a tagged test; report other criteria as pending | Traceability script (RH-01) |
| AR-09 | `tests/Acceptance/`, both requirements documents, section 3 of both architecture documents and `harness/scope.json` require human reviewer approval for changes | CI check against PR approver (RH-02) |
| AR-16 | No project references `Simulator`; the engine calls it only over HTTP | Architecture test |
| AR-17 | The interpreter accepts only the compiled document, never the diagram | Signature architecture test |
| AR-18 | Execution state changes go only through the state machine component | Architecture test and CA-18a |

Rule failures **explain how to fix** the problem so agents can resolve it without help.
