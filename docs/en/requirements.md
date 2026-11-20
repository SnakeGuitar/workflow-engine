# Requirements: workflow execution and automation engine

| Field | Value |
|---|---|
| Version | 1.3 |
| Deadline | 2026-12-02 |
| Team | 2 people + coding agents |
| Scope source | [project-idea.txt](project-idea.txt) (course assignment) |
| Context, decisions and history | [human/](human/) (not needed for implementation) |

## 0. How to use this document

1. **Stable IDs.** `RF`: functional requirement; `RNF`: nonfunctional requirement; `RN`: rule; `CA`: acceptance criterion; `RH`: harness requirement; `AR`: architecture rule. Never reuse an ID; numbering gaps represent retired IDs. These identifiers remain unchanged in both languages for traceability.
2. **Acceptance criteria.** Use `Given / When / Then`. Every CA is verifiable without human judgment except those marked **[HUMAN]**.
3. **Time.** Local time-based criteria use a controllable clock (RNF-08): “advance 2 h” means simulated time.
4. **Acceptance criteria are the contract.** Agents must not change a CA to make a test pass. Record changes to this document in `human/changelog.md` (RH-02, RH-13).

## 1. Purpose and scope

**Purpose.** A web platform for designing and automating event-driven sequences of logical and conditional tasks.

**Reference example (from the assignment).** “When a new user registers, wait 2 hours, check whether they activated their account; if not, send a reminder email and notify support.” Ship it as an installed example workflow (RF-19).

**Scope.** The assignment, role-based access, personal connections, extensible integrations and local artifact tasks (RF-30, RF-100, RF-101, RF-102).

| # | Assignment task | Requirements |
|---|---|---|
| T1 | Graphical interface for dragging, dropping and connecting nodes (Events, Conditions, Actions) | RF-01, RF-40 |
| T2 | Backend parser that rejects cycles and transforms the diagram into executable JSON (DAG) | RF-02, RF-03, RF-14 |
| T3 | Asynchronous execution engine: evaluates conditions, performs actions, supports delays and retries on network failures | RF-04, RF-05, RF-07, RF-09 through RF-13, RF-18 |
| T4 | Outgoing webhooks and simulated third-party APIs (email, SMS, HTTP POST) | RF-06, RF-16, RF-90, RF-91 |

**Out of scope.** Account administration in the UI, self-registration, password recovery, additional roles, configurable workflow ACLs, multiple tenants, parallel branches, multiple instances and mobile apps. Service compatibility depends on an accessible interface, authorization and an installed operation descriptor or adapter (RF-101).

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
- R-08 Deliver the four assignment tasks and RF-30, RF-100, RF-101 and RF-102.

## 4. Glossary

| Term | Definition |
|---|---|
| **Workflow** | Definition of an automated process as connected nodes. |
| **Node** | Workflow element with a readable identifier unique within the workflow (e.g. `check`). Types: **Event** (start), **Condition**, **Action**, **Timer** (delay). |
| **Diagram** | Editor data: nodes, positions, parameters and connections. |
| **Executable DAG** | JSON document compiled from a valid workflow: nodes, edges, start node, parameters and topological order. The engine executes only this document. |
| **Version** | Immutable copy of the executable DAG created when a workflow is activated. |
| **Event** | Fact received over HTTP: a **type** (e.g. `user.registered`), an **identifier** unique within its authenticated source, and JSON **data**. |
| **Execution** | One run of a workflow triggered by an event. |
| **Step** | Work done by an execution at a node. Each action attempt is a step. |
| **Context** | Readable execution data: `event.<field>` (event data), `nodes.<id>.statusCode` and `nodes.<id>.response.<field>` (result of a completed webhook or provider action). |
| **Variable** | `{path}` in a parameter, replaced with a context value during execution (e.g. `{event.email}`). |
| **Action** | Registered task from RF-06, RF-100, RF-101 or RF-102. |
| **Connector** | Provider descriptor or adapter with versioned trigger or action operations. |
| **Connection** | Authorization granted by one Editor for one provider account. A workflow node references it by ID. |
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
| RN-06 | For simulator deliveries: The same delivery (same execution and node) is delivered **once**, despite retries or restarts: it carries a unique reference and the receiver discards duplicates. |
| RN-09 | Retry an action that fails with a retryable error (RN-29 or CA-100e) up to **3 times**, after **1 min, 5 min and 30 min**. Once retries are exhausted, the execution becomes Failed. |
| RN-10 | A workflow is **valid** if it has exactly one Event node; has no cycles; every node is reachable from the Event; node identifiers are unique; each non-Condition node has at most one output; each Condition has at most one “yes” and one “no” output; every Timer is longer than 0 and at most **90 days**; and it uses only registered actions (RF-06, RF-100, RF-101, RF-102) with valid parameters (well-formed URL, recipient present). |
| RN-11 | Each activation creates a workflow version. An execution keeps its starting version. |
| RN-12 | Record every step: node, start, end, result, summarized input, attempt number and error, if any. Records cannot be changed or deleted. |
| RN-29 | For RF-06: Retryable errors: network failure, timeout (**10 s**) and 5xx responses. Do not retry 4xx responses. Applies to all three actions, since all call third parties over HTTP. |
| RN-30 | Execution state changes must follow section 5.1. Record each change and its timestamp. |
| RN-39 | Deduplicate by (authenticated source, event ID). Match active workflows by source and type. The integration-key endpoint is one source; each authenticated trigger connection is a separate source. |
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

**RF-04 Event trigger.** The source system sends an event to `POST /api/events` (type, identifier and data) with the integration key (CA-30d). An active workflow runs when an event matches its Event node's source and declared type (RN-39).
- **CA-04a** Given an active workflow with event type `user.registered`, when an event of that type arrives, then create exactly one execution using the current version and place the event data in its context.
- **CA-04b** Given no active workflow for an event type, when such an event arrives, then accept it and create no execution.
- **CA-04c** An Editor can send a test event from the UI with a type and JSON data.
- **CA-04d** Given two active workflows with the same source and event type, when an event arrives, then create one execution of each.

**RF-05 Conditions.** A Condition node evaluates the context and chooses an output (RN-40).
- **CA-05a** Given `nodes.check.response.active = true`, when the value is `true`, then follow “yes”; when `false`, follow “no”.
- **CA-05b** When a condition uses missing data, then execution continues through “no” and records that fact.

**RF-06 Assignment action catalog.** RF-100 adds provider operations. Workflows may use only registered actions. Assignment actions are **Send email** (recipient, subject, body), **Send SMS** (phone, text), and **Call webhook** (RF-16). All three call a third party over HTTP (the simulator in the MVP) and follow RN-09 and RN-29.
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

**RF-30 Sign-in and roles.** Username/password access with two fixed roles: **Editor** (design, validate, activate/deactivate workflows, send test events and read) and **Reader** (read workflows, executions and history only). Create accounts from configuration at installation. The generic event endpoint authenticates the source system with the installation key. Provider notifications follow RF-100. The simulator represents external services and does not require sign-in.
- **CA-30a** Without a session, deny all data access and actions except sign-in and authenticated event reception (CA-30d, RF-100).
- **CA-30b** A Reader can view workflows, executions and history; attempts to save, activate or deactivate a workflow or send a test event are rejected without changes. Editor operations on connected workflows require ownership under RF-100.
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

### 7.6 Personal provider connections

**RF-100 Personal provider connections.** The Editor authorizes a provider account from the UI and selects the connection and operation at each node. The reference workflow exercises these operations. RF-101 defines how to add services.

| Operation (v1) | Input | Output | Acceptance |
|---|---|---|---|
| `shopify.order.created` | Authorized store | `event.orderId`, `event.items` | CA-100b, CA-100g |
| `openai.summarize` | Order items, instructions | `nodes.summary.response.text` | CA-100c, CA-100g |
| `google.createDocument` | Drive folder ID, title, text | `nodes.document.response.documentId` | CA-100c, CA-100g |

Order IDs, instructions, folder IDs, titles, summary text and document IDs are strings. Order items are a JSON array. The order ID maps to the document title and the summary maps to its body. Output examples use node IDs `summary` and `document`. Google creates a native Google Doc in the selected Drive folder. Each connection grants only the permissions required for its operations.

- **CA-100a** Given two Editors and a Reader, only the owning Editor can list, select, use or revoke their connections. Saving, editing, activating, deactivating or testing a connected workflow requires ownership of the workflow and every referenced connection. Rejected requests cause zero changes and zero provider calls.
- **CA-100b** Given active triggers for stores A and B, delivering the same authenticated order event twice for A creates one execution for A and zero for B. Delivering the same event ID from B creates one execution for B. An unauthenticated notification creates zero executions.
- **CA-100c** Given connected accounts and a workflow owned by the Editor who created it, the UI offers the catalog inputs and preceding node outputs. Saving and reopening preserves the mapping. Activation rejects an unknown output or incompatible input, naming the node. Valid activation pins operation versions and connection IDs in the DAG. Publishing a new operation version leaves the active DAG bound to its original version.
- **CA-100d** Given a revoked connection, activation is rejected. An in-flight execution reaching an action with that connection becomes Failed before sending it. Subsequent notifications for the revoked trigger create zero executions.
- **CA-100e** Given a provider action whose response is lost after dispatch, including worker restart, stop with Failed and reason `delivery_uncertain` and send zero further requests for that step. Apply RN-09 only to failures known to precede dispatch. Explicit provider errors terminate the step.
- **CA-100f** Given a unique credential marker for each provider, zero markers appear in API responses, the diagram, compiled DAG, history, technical logs or harness reports.
- **CA-100g** [LIVE] [evidence: shopify_order, openai_response, google_document] Given dedicated real accounts connected through the UI and an active workflow, create a uniquely identified order in the authorized Shopify store. Within 5 minutes observe one completed execution, a successful OpenAI summary call with nonempty output, and exactly one Google Doc with the order ID as title in the selected Drive folder. Read its content independently through Google and compare it with the summary output. Record only redacted provider references and verdicts.

### 7.7 Extensible services and local work

**RF-101 Service-independent operations.** An Editor registers a versioned HTTP operation descriptor from the UI, binds a personal connection, maps inputs and outputs, and uses the operation in the same editor as RF-100. Descriptors declare operation ID, version, trigger/action kind, input/output schemas, HTTP method, endpoint and credential binding. For other protocols, protocols with custom authentication, or native SDKs, the operator installs a versioned adapter implementing the same operation contract. Provider credentials and verified event sources remain scoped to their connection.

- **CA-101a** Given an HTTP service with a fresh provider ID and an operation chosen by the oracle after the application is built, register it, bind a personal credential, send a mapped request and read its mapped response in the next node. The oracle compares method, path and payload. The application build hash stays identical.
- **CA-101b** Given an independently supplied adapter with a non-HTTP fixture protocol, install it, authorize its connection, receive an authenticated trigger and invoke its action. Verify the returned value, rejection of another owner's connection, and identical engine and editor binaries. Removing a required adapter version prevents new activation and fails the affected step explicitly.
- **CA-101c** [LIVE] [evidence: service_operation, remote_result] Given an authorized real service outside the RF-100 example, register its operation and execute a flow with a unique input. Within five minutes the oracle reads the correlated result independently at that service and compares the value with the expected input. The application build hash stays identical.

**RF-102 Local tasks and artifacts.** An Editor can compose local steps on the machine running the engine, including writing a script, executing it and publishing files. Inputs can be user-supplied files, mapped values, or artifacts from previous nodes. Local execution works offline. Each completed artifact has an immutable ID, filename, media type, byte count and SHA-256 digest. Downstream nodes consume it by ID and its owner can download it from execution history.

Local operation examples use the same catalog. The evidence profile for these rows is `offline`.

| Operation (v1) | Input | Output | Acceptance |
|---|---|---|---|
| `local.document` | Content and format | DOCX, PPTX or XLSX artifact | offline CA-102a |
| `local.analyze` | Dataset and analysis | Report artifact | offline CA-102b |
| `local.writeScript` | Source text | Script artifact | offline CA-102c |
| `local.runScript` | Script artifact and arguments | Declared output artifacts | offline CA-102c, CA-102d |

- **CA-102a** Given an offline workflow and input marker, generate DOCX, PPTX and XLSX artifacts. Download and open them with independent format readers. Assert the marker in the document body, a slide title and workbook cell A1 respectively. Check each reported byte count and digest against the downloaded bytes.
- **CA-102b** Given a CSV containing groups A, B, A with values 2, 4, 6, analyze the data locally and produce a report containing counts A=2, B=1 and means A=4, B=4. Repeat with different input and compare the downloaded report against aggregates computed independently from that input. For the ordered series 2, 4, 6, 3, 2, identify the longest strictly increasing contiguous run as 2, 4, 6 and report its length as 3.
- **CA-102c** Given a script-writing step, download the generated Python script and pass its artifact ID plus a new input value to a local execution step. For the acceptance case the input is a string. The returned JSON file must contain that value and its length. Check the result after restarting the engine and verify the same artifact ID, bytes and digest.
- **CA-102d** Given a timed-out or nonzero-exit script, fail the step, stop its child processes and publish zero partial files. A script cannot read another execution's workspace, host credentials or files outside its assigned inputs and workspace. Deny downloads by a different user. On recovery publish each successful output once per execution, node and output name.
- **CA-102e** Given a flow mapping an external operation's output into a local report and its resulting artifact into a second external operation, the receiver obtains the same bytes and digest as the owner downloads from history.

## 8. Nonfunctional requirements

| ID | Requirement | Verification criterion |
|---|---|---|
| RNF-08 | **Testability.** | **CA-N08a** Verify local waits with a controllable clock. **CA-N08b** The simulator acts as a test double with RF-91 failure injection. **CA-N08c** The complete local acceptance suite runs within 10 min without real external services. |
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
| RH-100 | **Delivery gates.** Check bilingual IDs, references, scope version and operation-to-CA mappings. For each local/live suite report committed CA covered, passed and required separately. | Require 100% passed, zero skipped and local duration ≤600 s. Missing runner, prerequisites or live credentials yields blocked. Failed assertions yield failed. Record baseline and spec with each result. M1–M6 measure agent work separately. |
