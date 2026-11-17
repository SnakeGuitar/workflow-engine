# Requirements: workflow execution and automation engine

| Field | Value |
|---|---|
| Version | 1.1 |
| Deadline | 2026-12-02 |
| Team | 2 people + coding agents |
| Scope source | [project-idea.txt](project-idea.txt) (course assignment) |
| Context, decisions and history | [human/](human/) (not needed for implementation) |

## 0. How to use this document

1. **Stable IDs.** `RF`: functional requirement; `RNF`: nonfunctional requirement; `RN`: rule; `CA`: acceptance criterion; `RH`: harness requirement; `AR`: architecture rule. Never reuse an ID; numbering gaps represent retired IDs. These identifiers remain unchanged in both languages for traceability.
2. **Acceptance criteria.** Use `Given / When / Then`. Every CA is verifiable without human judgment except those marked **[HUMAN]**.
3. **Time.** Time-based criteria use a controllable clock (RNF-08): “advance 2 h” means simulated time.
4. **Acceptance criteria are the contract.** Agents must not change a CA to make a test pass. Record changes to this document in `human/changelog.md` (RH-02, RH-13).

## 1. Purpose and scope

**Purpose.** A web platform for designing and automating event-driven sequences of logical and conditional tasks.

**Reference example (from the assignment).** “When a new user registers, wait 2 hours, check whether they activated their account; if not, send a reminder email and notify support.” Ship it as an installed example workflow (RF-19).

**Scope: the assignment's four tasks plus role-based access (RF-30).**

| # | Assignment task | Requirements |
|---|---|---|
| T1 | Graphical interface for dragging, dropping and connecting nodes (Events, Conditions, Actions) | RF-01, RF-40 |
| T2 | Backend parser that rejects cycles and transforms the diagram into executable JSON (DAG) | RF-02, RF-03, RF-14 |
| T3 | Asynchronous execution engine: evaluates conditions, performs actions, supports delays and retries on network failures | RF-04, RF-05, RF-07, RF-09 through RF-13, RF-18 |
| T4 | Outgoing webhooks and simulated third-party APIs (email, SMS, HTTP POST) | RF-06, RF-16, RF-90, RF-91 |

**Out of scope.** User management through the UI, self-registration, password recovery, roles beyond Editor and Reader, per-workflow permissions and multiple tenants. Specific business domains. Channels beyond email, SMS and HTTP POST, and real third-party services. Incoming messages. Plugins. Parallel execution within a workflow. Multiple-instance scaling. Mobile application.

**Committed scope:** the criteria in `harness/scope.json` (protected, RH-02). The remainder is a target.

**Cuts, in this order, if the deadline is at risk:** (1) CA-12b; (2) CA-04c (send test events with a script); (3) CA-90d; (4) CA-30e (Readers see controls, but the API rejects changes, CA-30b).

## 2. Actors

| Actor | Description |
|---|---|
| **Editor** | Signs in, designs, validates, activates and deactivates workflows, sends test events and views executions. |
| **Reader** | Signs in and views workflows, executions and history without making changes. |
| **Source system** | External application sending events over HTTP with the integration key. In the demo, an Editor using the UI (CA-04c) or a script. |
| **Third-party simulator** | Project application that simulates email, SMS and an HTTP POST receiver, and injects failures. |

## 3. Constraints

- R-01 Web system, accessible in a browser without installation.
- R-02 Deadline: 2026-12-02. Team of 2 people with coding agents.
- R-04 Interface in English.
- R-08 Fulfill the four assignment tasks (section 1); the only addition is role-based access (RF-30).

## 4. Glossary

| Term | Definition |
|---|---|
| **Workflow** | Definition of an automated process as connected nodes. |
| **Node** | Workflow element with a readable identifier unique within the workflow (e.g. `check`). Types: **Event** (start), **Condition**, **Action**, **Timer** (delay). |
| **Diagram** | Editor data: nodes, positions, parameters and connections. |
| **Executable DAG** | JSON document compiled from a valid workflow: nodes, edges, start node, parameters and topological order. The engine executes only this document. |
| **Version** | Immutable copy of the executable DAG created when a workflow is activated. |
| **Event** | Fact received over HTTP: a **type** (e.g. `user.registered`), a unique **identifier**, and JSON **data**. |
| **Execution** | One run of a workflow triggered by an event. |
| **Step** | Work done by an execution at a node. Each action attempt is a step. |
| **Context** | Readable execution data: `event.<field>` (event data), `nodes.<id>.statusCode` and `nodes.<id>.response.<field>` (result of a completed webhook). |
| **Variable** | `{path}` in a parameter, replaced with a context value during execution (e.g. `{event.email}`). |
| **Action** | Node task: **Send email**, **Send SMS**, or **Call webhook**. |
| **Outgoing webhook** | HTTP POST to a URL as a workflow action. |
| **Unique reference** | Delivery identifier (execution and node) included in the request so the receiver can discard duplicates. |
| **Simulator** | Project application simulating third-party services and injecting failures, used for demonstrations and as a test double. |
| **Role** | Fixed account permission: **Editor** or **Reader**. Each account has exactly one role. |
| **Integration key** | Installation secret identifying the source system when it sends events. It is not a user account. |
| **Controllable clock** | Time source that tests can advance. |

## 5. States

### 5.1 Execution (finite state machine)

| Source | Allowed targets |
|---|---|
| Pending | Running |
| Running | Waiting, Completed, Failed |
| Waiting | Running |
| Completed, Failed | (none) |

An execution is **Waiting** during a timer or until the next retry. It becomes **Failed** when an action exhausts its retries or fails without retry (RN-29, RN-41); record the reason.

### 5.2 Workflow

| Source | Allowed targets |
|---|---|
| Draft | Active (only if valid) |
| Active | Active (activating changes creates a new version), Inactive |
| Inactive | Active (only if valid) |

Editing an active workflow's diagram does not change the version being executed until the workflow is activated again.

## 6. Rules

| ID | Rule |
|---|---|
| RN-06 | The same delivery (same execution and node) is delivered **once**, despite retries or restarts: it carries a unique reference and the receiver discards duplicates. |
| RN-09 | Retry an action that fails with a retryable error (RN-29) up to **3 times**, after **1 min, 5 min and 30 min**. Once retries are exhausted, the execution becomes Failed. |
| RN-10 | A workflow is **valid** if it has exactly one Event node; has no cycles; every node is reachable from the Event; node identifiers are unique; each non-Condition node has at most one output; each Condition has at most one “yes” and one “no” output; every Timer is longer than 0 and at most **90 days**; and it uses only registered actions (RF-06) with valid parameters (well-formed URL, recipient present). |
| RN-11 | Each activation creates a workflow version. An execution keeps its starting version. |
| RN-12 | Record every step: node, start, end, result, summarized input, attempt number and error, if any. Records cannot be changed or deleted. |
| RN-29 | Retryable errors: network failure, timeout (**10 s**) and 5xx responses. Do not retry 4xx responses. Applies to all three actions, since all call third parties over HTTP. |
| RN-30 | Execution state changes must follow section 5.1. Record each change and its timestamp. |
| RN-39 | Ignore events whose identifier has already been received. Each new event creates one execution per active workflow whose Event node declares that type. |
| RN-40 | A Condition compares a context value with a literal (number, boolean or string) using `=`, `≠`, `>`, `≥`, `<`, `≤`, “contains” or “exists”. True follows “yes”; false or missing data follows “no”, and missing data is recorded. If the selected output is unconnected, the execution ends as Completed. |
| RN-41 | Resolve variables when executing the step. A variable missing from the context fails the step without retry and leaves the execution Failed with the variable name. |

## 7. Functional requirements and acceptance criteria

### 7.1 Definition, validation and compilation

**RF-01 Define workflows.** An Editor can create, save and retrieve workflows containing Event, Condition, Action and Timer nodes and their connections.
- **CA-01a** Given a signed-in Editor, when they create and save a workflow with 1 event, 1 condition, 1 action and 1 timer connected together, then it is Draft and retrieving it returns the same nodes, parameters and connections.
- **CA-01b** When a workflow containing an unsupported node type is saved, then reject it with a message naming that node.

**RF-02 Validate workflows.** Validate against RN-10 and return **all** errors found.
- **CA-02a** Given a workflow without a start event, when validated, then it is invalid with the error “missing start event”.
- **CA-02b** Given a workflow with two start events, when validated, then it is invalid.
- **CA-02c** Given a workflow containing cycle A → B → A, when validated, then it is invalid and the error identifies nodes A and B.
- **CA-02d** Given a workflow with an unreachable node, when validated, then it is invalid and the error names that node.
- **CA-02f** Given a timer of 0 or more than 90 days, when validated, then it is invalid.
- **CA-02g** Given a workflow with three different errors, when validated, then the result lists all three.
- **CA-02h** Given a valid 50-node workflow, when validated, then the response takes at most 1 s.
- **CA-02i** Given an Action or Timer node with two outputs, when validated, then the workflow is invalid and the error names that node.

**RF-14 Compile to executable DAG.** Successfully validating a workflow transforms it into a directed acyclic graph JSON document.
- **CA-14a** Given a valid workflow, when compiled, then the resulting JSON contains nodes, edges, a start node and each node's parameters.
- **CA-14b** Given the same workflow, when compiled twice, then both results are byte-for-byte identical.
- **CA-14c** Given a cyclic workflow, when compiled, then compilation fails with RF-02 errors and produces no document.
- **CA-14d** The compiled document admits a topological order, verifiable by a test.

**RF-03 Activate, deactivate and version.** Only valid workflows may be activated.
- **CA-03a** Given an invalid workflow, when activation is attempted, then reject it and preserve its state.
- **CA-03b** Given a valid workflow, when activated, then it becomes Active with a version number.
- **CA-03c** Given an active version-1 workflow with a running execution, when it is modified and version 2 is activated, then the running execution finishes with version 1 and new executions use version 2 (RN-11).
- **CA-03d** Given an active workflow, when deactivated, then no new executions are created and existing executions continue to completion.

### 7.2 Execution

**RF-04 Event trigger.** The source system sends an event to `POST /api/events` (type, identifier and data) with the integration key (CA-30d). An active workflow runs when an event matches its Event node's declared type (RN-39).
- **CA-04a** Given an active workflow with event type `user.registered`, when an event of that type arrives, then create exactly one execution using the current version and place the event data in its context.
- **CA-04b** Given no active workflow for an event type, when such an event arrives, then accept it and create no execution.
- **CA-04c** An Editor can send a test event from the UI with a type and JSON data.
- **CA-04d** Given two active workflows with the same event type, when an event arrives, then create one execution of each.

**RF-05 Conditions.** A Condition node evaluates the context and chooses an output (RN-40).
- **CA-05a** Given `nodes.check.response.active = true`, when the value is `true`, then follow “yes”; when `false`, follow “no”.
- **CA-05b** When a condition uses missing data, then execution continues through “no” and records that fact.

**RF-06 Action catalog.** Workflows may use only registered actions: **Send email** (recipient, subject, body), **Send SMS** (phone, text), and **Call webhook** (RF-16). All three call a third party over HTTP (the simulator in the MVP) and follow RN-09 and RN-29.
- **CA-06a** When Send email executes, then the simulator receives exactly one email with variables resolved.
- **CA-06e** Given a workflow with an unregistered action, when validated, then it is invalid and the error names the action.
- **CA-06f** When Send SMS executes, then the simulator receives exactly one SMS with variables resolved.
- **CA-06g** Given a variable missing from the context, when the action executes, then send nothing and leave the execution Failed with the variable name (RN-41).

**RF-07 Timers.** A Timer node waits a fixed duration before continuing.
- **CA-07a** Given a 2 h timer started at t0, when the clock advances 1 h 59 min, then the execution remains Waiting; when it reaches 2 h, then continue to the next node.

**RF-09 Retries.**
- **CA-09a** Given a service failing on attempts 1 and 2 and accepting attempt 3, when the action executes, then make 3 attempts separated by 1 min and 5 min waits, and record success with 3 attempts.
- **CA-09b** Given a service that always fails, when the action executes, then make 4 attempts in total and leave the execution Failed (RN-09).

**RF-16 Outgoing webhook.** Call webhook performs an HTTP POST to a URL with headers and a body containing variables, and stores the response in the context.
- **CA-16a** Given an action with a URL, headers and a body containing `{event.userId}`, when executed, then the simulated receiver receives exactly one HTTP POST with the resolved body.
- **CA-16b** Given a receiver with network failures on attempts 1 and 2 and a 200 response on attempt 3, then retry with RN-09 delays and record success with 3 attempts.
- **CA-16c** Given a receiver returning 500, then retry; given one returning 400, then do not retry and leave the execution Failed (RN-29).
- **CA-16d** Given a receiver not responding within 10 s, then treat it as a network failure (RN-29).
- **CA-16e** Every call carries its unique reference in a header (RN-06).
- **CA-16f** An invalid URL format makes the workflow invalid during validation (RN-10).
- **CA-16g** Given a receiver returning 200 with a JSON body, then store its status code and body in `nodes.<id>` so a later condition can read them.

**RF-10 Persistence and recovery.** Executions survive system restarts.
- **CA-10a** Given 100 Waiting executions, when the system stops abruptly and restarts, then all 100 remain Waiting with the correct remaining time and resume when due.
- **CA-10b** Given an execution whose timer expired while the system was stopped, when restarted, then continue to the next node without repeating recorded steps.
- **CA-10c** Given a crash just after the simulator accepted a delivery, when the system restarts and resumes, then the simulator records only one message for that reference.

**RF-11 No duplication.**
- **CA-11a** Given the same event (same identifier) delivered twice, then create only one execution per workflow (RN-39).
- **CA-11b** Given a service accepting a message but responding after the timeout, when the system retries, then the simulator records only one message for that reference.

**RF-13 Concurrency.**
- **CA-13a** Given 200 simultaneous events, then create 200 executions without loss or mixed contexts.
- **CA-13c** Given two parallel workers on the same database and 100 expired timers, then execute each step exactly once.

**RF-18 Execution states.**
- **CA-18a** For every pair of states in section 5.1, accept allowed transitions and reject all others (parameterized test over the entire table).
- **CA-18b** An execution in a terminal state cannot change state.
- **CA-18c** History shows the current state and every execution state change with its timestamp (RN-30).

**RF-12 Execution history.**
- **CA-12a** Given a finished execution, then its history shows each step's node, start, end, result (success or error), summarized input and attempt number (RN-12).
- **CA-12b** History can be filtered by workflow and execution state.

**RF-19 Assignment example workflow.** Install the valid, active “Activation reminder” workflow: Event `user.registered` → Timer 2 h → Call webhook `check` (account status) → Condition `nodes.check.response.active = true`; on “no”: Send reminder email to the user → Send SMS to support.
- **CA-19a** After installation, the workflow exists, is active and passes validation.
- **CA-19b** Given the HTTP receiver configured to return `{"active": false}` and a `user.registered` event at t0, then at 2 h the verification POST occurs, the simulator receives an email for the user and an SMS for support, and the execution becomes Completed.
- **CA-19c** Given a receiver returning `{"active": true}`, then send neither email nor SMS and leave the execution Completed.

### 7.3 Visual editor

**RF-40 Visual editor.**
- **CA-40a** When an Editor adds all four node types, connects, moves and deletes them, then the diagram reflects the changes and preserves them after saving and reopening.
- **CA-40b** A node cannot be connected to itself.
- **CA-40c** The Editor can validate a workflow from the editor and see the RF-02 result.
- **CA-40d** Given a node-type palette, when the Editor drags a type onto the canvas and drops it, then create a node of that type at that position.
- **CA-40e** Given a workflow saved in the editor, when activated, then validate and compile it (RF-02, RF-14) and show the result in the editor.
- **CA-40f** Selecting a node lets the Editor edit its identifier and parameters (event type, condition, duration, recipient, URL, headers, body); saving preserves the changes.

### 7.4 Access

**RF-30 Sign-in and roles.** Username/password access with two fixed roles: **Editor** (design, validate, activate/deactivate workflows, send test events and read) and **Reader** (read workflows, executions and history only). Create accounts from configuration at installation. The source system uses the integration key, not an account. The simulator represents external services and does not require sign-in.
- **CA-30a** Without a session, deny all data access and actions except sign-in and event reception (CA-30d).
- **CA-30b** A Reader can view workflows, executions and history; attempts to save, activate or deactivate a workflow or send a test event are rejected without changes. An Editor can perform all operations.
- **CA-30c** After 5 consecutive failed attempts, lock the account for 5 minutes.
- **CA-30d** Reject events sent to `POST /api/events` without a valid integration key and create no executions.
- **CA-30e** Readers see a read-only editor without controls to modify, activate or send events.
- **CA-30f** Signing out invalidates the session.

### 7.5 Third-party API simulator

**RF-90 Simulator.** Separate application with **email**, **SMS** and **HTTP POST** receivers. Record received timestamp, destination, content and reference; discard duplicate references and show records in its own UI.
- **CA-90a** Given a sent email, then the simulator shows recipient, subject, body and reference.
- **CA-90b** Given an SMS and an HTTP POST, then show destination, content and reference.
- **CA-90c** Given two messages with the same reference, then record only one and respond successfully to both.
- **CA-90d** Operators can filter received messages by service and clear the records.
- **CA-90f** The simulator UI indicates that it is a simulation, not a real service.
- **CA-90g** The HTTP receiver responds per route with the configured status code and JSON body (defaults: 200 and `{}`).

**RF-91 Failure injection.**
- **CA-91a** Operators can configure per service: error status (4xx or 5xx), latency in seconds, network failure and late response (record the message and respond after the timeout).
- **CA-91b** A failure configured for the next N requests applies to those N requests and then disables itself.
- **CA-91c** Failures can be enabled through the simulator UI and automated tests.

## 8. Nonfunctional requirements

| ID | Requirement | Verification criterion |
|---|---|---|
| RNF-08 | **Testability.** | **CA-N08a** Verify every wait with a controllable clock. **CA-N08b** The simulator acts as a test double with RF-91 failure injection. **CA-N08c** The complete acceptance suite runs within 10 min without real external services. |
| RNF-07 | **Security.** | **CA-N07a** No operation returns a password or integration key, and neither appears in technical logs. |
| RNF-09 | **Localization.** | **CA-N09a** English UI; YYYY-MM-DD dates and 24-hour time. |
| RNF-14 | **Reproducible installation.** | **CA-N14a** [HUMAN] From a clean repository, following the README, `docker compose up` brings up the application, simulator and example workflow within 15 min. |

## 9. Harness and experiment requirements

**Experiment objective.** An **exploratory** case study of whether coding agents can build and maintain this system under a harness (versioned specification, tests as contract, automated checks). Small samples provide **no statistical significance**: report observations rather than general conclusions. Inspiration: [OpenAI, “Harness engineering”](https://openai.com/index/harness-engineering/).

| ID | Requirement | Verification criterion |
|---|---|---|
| RH-01 | **Executable traceability.** Every CA has at least one automated test citing its ID. | Report CA with tests / total CA, excluding **[HUMAN]**. MVP target: 100% of committed CA in `harness/scope.json`; report remaining RF criteria as pending. Report RNF criteria separately with their own target. |
| RH-02 | **Acceptance tests are the contract.** Agents do not modify them. | Changes to `tests/Acceptance/`, this document, architecture section 3 or `harness/scope.json` without human reviewer approval fail the harness (AR-09). |
| RH-03 | **Agent task.** Every task names target RF and CA and receives an automated pass/fail verdict. | Reject tasks without target CA. |
| RH-04 | **Per-task record.** Record model, task, start/end, iterations, usage or estimated cost, outcome and human interventions. | Every executed task has a complete record. |
| RH-05 | **Reproducibility.** A task can be repeated from an identified repository baseline. | Two runs from the same baseline use the same input and specification. |
| RH-06 | **Agent-readable feedback.** Failures identify the violated CA and expected versus actual behavior. | Every suite failure cites a CA ID. |
| RH-07 | **Structural rules.** Automatically verify every AR rule in [architecture.md](architecture.md), section 3. | Run all AR rules on every run; violations fail with remediation messages. |
| RH-08 | **Metrics.** M1 first-attempt task passes; M2 iterations per task; M3 human interventions per task; M4 regressions following a requirement change; M5 usage per task; M6 time until all tests pass. | Calculate all six from RH-04 records. |
| RH-09 | **Comparison with and without harness.** Run the same tasks with and without it and across available models. | At least 8 tasks per condition (adjust to budget). |
| RH-10 | **Scalability.** Measure M1–M5 at 25%, 50% and 100% RF implementation. | Three recorded measurement points. |
| RH-11 | **Controlled requirement changes.** Introduce at least 2 (e.g. change RN-09 delays and add an RF-06 action). | Record failing tests, changed files and interventions for each change. |
| RH-12 | **Human review.** Every integrated change has a human reviewer and checklist. | Record a reviewer for 100% of integrated changes. Authors can explain every delivered part **[HUMAN]**. |
| RH-13 | **Versioned specification.** Every measurement identifies this document's version. | Every RH-04 record includes the version. |
| RH-14 | **Usage budget.** Track usage per model and stop at the configured limit. | Start no new task once the limit is reached. |
