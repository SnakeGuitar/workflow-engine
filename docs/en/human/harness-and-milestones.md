# Harness and milestones

| Field | Value |
|---|---|
| Sources | [requirements.md](../requirements.md) v1.1 (RH-01 through RH-14), [architecture.md](../architecture.md) v1.1 (ADR-09, section 3) |
| Deadline | 2026-12-02 |
| Team | 2 people + agents, in **engine** (backend, persistence, simulator) and **UI** (editor, access, history, QA) lanes |

> **Nature of this document.** Operational harness/calendar plan, not implementation authority. RH, ADR-09 and AR rules are normative. Replaces the 2026-11-13 report for the previous specification (retained in Git).

## 1. Harness

### 1.1 Principles

1. **The harness executes the contract.** It returns a verdict per CA, not a coverage percentage.
2. **Two speeds:** push gate ≤ 3 min; acceptance suite ≤ 10 min; slow work runs nightly.
3. **Determinism:** fake clock, no external network, fixed seed, one PostgreSQL per job.
4. **Independent oracle:** the committed-CA test author differs from its implementer.
5. **Fingerprint every protected artifact** and require human approval for changes.

### 1.2 Structure

```text
tools/
  Harness/                      .NET CLI (System.CommandLine)
    Commands/Verify.cs          build + tests + AR rules + trace
    Commands/Trace.cs           CA → test
    Commands/Freeze.cs          protected-file fingerprints
    Commands/Task.cs            RH-04 records (start/end)
    Commands/Budget.cs          RH-14 limit
    Commands/Report.cs          M1–M6 metrics
    protected.json             SHA-256 fingerprints, including architecture section 3
    budget.json                per-model and global usage
tests/
  Support/                      unprotected
    ApiFactory.cs               WebApplicationFactory + PostgreSQL + Editor/Reader session
    FakeClock.cs                manually advanced TimeProvider
    SimulatorHost.cs            in-memory simulator with failure injection
    CaAssert.cs                 failures include CA ID (RH-06)
  Acceptance/                   protected
    CA02_ValidationTests.cs     [Ac("CA-02a")] …
    CA30_AccessTests.cs
  Architecture/
    LayersTests.cs              AR-01, AR-04, AR-16, AR-17
    ForbiddenApiTests.cs        AR-03
    CredentialsTests.cs         AR-05
    RegistryTests.cs            AR-06
    FileSizeTests.cs            AR-07
    StateMachineTests.cs        AR-18
  Unit/
harness/
  scope.json                    committed CA (protected)
  tasks.ndjson                  one line per task (append-only)
  metrics/                      trace.json, trace.md, M1–M6
```

### 1.3 Components

**1. Traceability (`trace`).** Extract `[Ac("CA-XXy")]` from `tests/Acceptance` and `tests/Architecture`, cross-reference `docs/en/requirements.md` and `harness/scope.json`. Fail if a committed CA lacks a test, a test cites a nonexistent CA, or a **[HUMAN]** CA is treated as automatic. Emit `trace.json`/`trace.md` with specification version and commit (RH-01, RH-13, AR-08).

**2. Freeze.** `protected.json` fingerprints both requirements documents, section 3 of both architecture documents, `tests/Acceptance/` and `harness/scope.json`. Changed fingerprints require PR owner approval (CODEOWNERS/branch protection) and an `Approved-By:` line naming that person. Agents must not write this line or approve their own PR (RH-02, AR-09).

**3. Task records (`task start|end`).** One NDJSON record per task: `spec_version`, `commit_base`, `model`, `task`, `rf[]`, `ca[]`, `oracle` (boolean), `start`, `end`, `iterations`, `tokens`, `cost`, `result`, `human_interventions`. Reject starts without CA (RH-03) or with exhausted budget (RH-14). Derive M1–M6 automatically (RH-04, RH-05, RH-08).

**4. Two-speed CI.**

| Stage | Trigger | Contents | Budget |
|---|---|---|---|
| `gate` | every push | build `-warnaserror`, Unit, Architecture, file size, gitleaks, freeze, trace | ≤ 3 min |
| `acceptance` | every PR | PostgreSQL service container, acceptance except `Slow`; simulated CA-10a/b restart by reopening a host on the same database with fake clock | ≤ 10 min |
| `ui` | merge to main or manual | Playwright against `docker compose` (CA-40, CA-30e, CA-04c) | ≤ 5 min |
| `nightly` | cron | CA-10 real `kill -9`, CA-13c two processes, flaky-test report | unlimited |
| `release` | tag | image, `compose up`, CA-N14a smoke test | ≤ 15 min |

One PostgreSQL container per job; `TRUNCATE` cleanup per class. A container per test cannot meet 10 min.

**5. Cross-model oracle.**
- Applies to committed criteria in `harness/scope.json`; use the same agent with sampled review for others.
- Freeze the milestone's OpenAPI contract (ADR-08) before tests, so the oracle does not invent resources.
- `oracle` lane: a model writes tests from CA text and OpenAPI only; never sees implementation.
- `impl` lane: another model implements without viewing `tests/Acceptance`, which is read-only through permissions, freeze and CI.
- A human reviews every test before freezing; it then becomes contract.

### 1.4 Failure contract (RH-06)

```csharp
public static class Ca
{
    public static void Expect(string ca, object expected, object actual, string context = "") { }
}
// Output: CA-09a · expected "3 attempts", got "2" · execution 7f3c…
```

`verify` ends with violated CA IDs, not a stack-trace dump.

### 1.5 Harness risks

| Risk | Mitigation |
|---|---|
| Harness consumes first week | Reuse xUnit/NetArchTest; no custom UI; initially only H0 needs |
| Slow acceptance tests | One PostgreSQL per job; no external network; class-level parallelism |
| Agents “fix” tests | Freeze, separate oracle, human review |
| Exhausted budget | `budget.json`; per-task rather than daily cutoff |
| 400-line rule conflicts with React | Review in H1 whether `web/` needs a different limit (section 3 change, approval required) |

## 2. Milestones

### 2.1 Shared rule

Each milestone delivers **a complete system version**: clean `docker compose up`, sign-in, demo workflow, `v0.x` tag, recorded demo, passing CI/AR rules and updated `trace.md`. No unfinished pieces or dependencies on later milestones.

### 2.2 Calendar

| Milestone | Dates (2026) | Engine lane | UI lane | Demo | Measurement (RH-10) |
|---|---|---|---|---|---|
| **H0** Vertical skeleton | Nov 15–17 | Minimal harness/compiler, engine without waits, webhook, HTTP simulator | Seeded-account sign-in, SPA skeleton, OpenAPI client | Sign in; send `user.registered` with key; POST reaches simulator | Baseline |
| **H1** Definition | Nov 18–20 | Full validator/compiler, activation/versions | Editor | Draw cycle, see errors, fix, activate, trigger event | 25% |
| **H2** Durable engine | Nov 21–24 | Timers, retries, recovery, deduplication, concurrency, state machine, failure injection | History, UI roles | `kill -9` during wait; resume without repeats; Reader cannot activate | 50% |
| **H3** Full example | Nov 25–28 | Conditions/context, email/SMS, example workflow | Simulator UI, test event | End-to-end assignment example | 100% |
| **H4** Closeout | Nov 29–Dec 2 | Clean install, controlled requirement changes (RH-11) | README, M1–M6, presentation | 15 min installation | Results |

Run RH-09 comparisons during H1–H3 using completed tasks, not at the end. RH-10 percentages use the specification's 20 RF.

### 2.3 Milestone details

**H0 — Vertical skeleton (Nov 15–17)**
- **Complete version:** cookie sign-in, seeded accounts/workflow, compiled workflow, integration-key event creates execution and POST to simulator, step history.
- **CA:** CA-30a, CA-30d, CA-14a, CA-04a, CA-16a, CA-90a, CA-12a.
- **Minimum harness:** CI gate, PostgreSQL per job, fake clock, `tests/Support/`, CODEOWNERS freeze, scope-based trace, task records, AR-01, AR-03, AR-07.
- **Stop criterion (Nov 17, engine):** without demo, both people stay on H0; consume H4 slack.

**H1 — Definition (Nov 18–20)**
- **Complete version:** Editor draws, validates all errors, activates/versions; an event triggers the drawn workflow.
- **CA:** CA-01a/b, CA-02a–d, CA-02f–i, CA-03a–d, CA-14b–d, CA-06e, CA-16f, CA-40a–f.
- **Demo:** A→B→A cycle/error; fix; activate v1; send event/view execution; compile twice and compare bytes.
- **Criterion (Nov 20, UI):** committed H1 criteria pass; otherwise engine helps for one day and apply cuts (1)/(2).

**H2 — Durable engine (Nov 21–24)**
- **Complete version:** restart-safe executions, timers, delayed retries, no duplicates, two workers, API/UI roles.
- **CA:** CA-07a, CA-09a/b, CA-10a–c, CA-11a/b, CA-13a, CA-13c, CA-16b–e, CA-18a–c, CA-91a–c, CA-12b, CA-30b, CA-30c, CA-30e, CA-30f.
- **Demo:** `kill -9` during wait, restart/resume without repeats; service fails twice/succeeds third; Reader cannot activate.
- **Stop criterion (Nov 24, engine):** CA-10a/b pass. Otherwise cuts (1)–(4), both people on engine, technical plan B (polling claims without leases, one instance; CA-13c pending). Do not change ADR-03, RN-06, CA-10c, CA-11a/b or harness.

**H3 — Full example (Nov 25–28)**
- **Complete version:** installed example with conditions/context/email/SMS; simulator UI with configurable responses.
- **CA:** CA-04b–d, CA-05a/b, CA-06a, CA-06f, CA-06g, CA-16g, CA-19a–c, CA-90b–g, CA-N07a.
- **Demo:** `user.registered` with `{"active": false}` (email/SMS arrive) and `{"active": true}` (nothing sent).
- **Criterion (Nov 28, both):** failing demo triggers cuts in order.

**H4 — Closeout (Nov 29–Dec 2)**
- 15 min install (CA-N14a), two controlled requirement changes (RH-11), RH-09 results, M1–M6, README/presentation. Feature freeze November 30.

### 2.4 Checkpoints

| Date (2026) | Owner | Measurement | Decision |
|---|---|---|---|
| Nov 17 | Engine | H0 demo | Without demo, stay on H0 |
| Nov 20 | UI | Committed H1 CA pass | Otherwise cuts (1)/(2) |
| Nov 24 | Engine | CA-10a/b pass | Otherwise cuts (1)–(4), technical plan B |
| Nov 28 | Both | H3 demo | Failure triggers ordered cuts |
| Nov 30 | Both | Feature freeze | Fixes only |

Measure passing committed/current-milestone CA rather than easily inflated “CA per day”. Use requirements section 1 for cuts.

## 3. MVP-0 (committed scope)

**Committed (18):** CA-40a, CA-40d, CA-40e, CA-02a, CA-02c, CA-02g, CA-14a, CA-14b, CA-14d, CA-03b, CA-04a, CA-07a, CA-09a, CA-10b, CA-16a, CA-16b, CA-30a, CA-19b.

**Support (3):** CA-10a (recovery), CA-12a (history), CA-90a (simulator receives).

Together they cover all four assignment tasks and access: editor, validator/compiler, engine waits/retries, webhook/simulator, sign-in. CA-19b connects them in the example. Stored in protected `harness/scope.json`.

## 4. Harness decisions

| # | Decision |
|---|---|
| 1 | 2 people + agents, engine/UI lanes. |
| 2 | Partially separate oracle: independent for committed CA; same agent with sampled review afterward. |
| 3 | Protect architecture section 3, requirements, acceptance tests and scope; protection applies to both documentation languages. |
| 4 | Keep experiment (RH-04, RH-08 through RH-11, RH-14) with reduced scope; retire corpus, recorded responses and plugin-as-controlled-change (formerly RH-15 through RH-18) along with their modules. |
