# 6. Sequence and communication

Simplified views of the three key use cases ([01-use-cases.md](01-use-cases.md)). B uses the assignment example (RF-19).

## A · Design, validate and activate (CU-02 through CU-04)

```mermaid
sequenceDiagram
  autonumber
  actor U as Editor
  participant Web as web (editor)
  participant Api as Api
  participant Engine as IEngine
  participant Compiler as Compiler
  participant Validator as Validator
  participant DB as PostgreSQL
  Note over U,Api: signed in (cookie, CA-30a)
  U->>Web: drag nodes, connect and edit parameters
  Web->>Api: PUT /api/workflows/{id} (diagram)
  Api->>Api: Editor role? Reject Reader without changes (CA-30b)
  Api->>Engine: SaveDraft(diagram)
  Engine->>DB: save diagram
  U->>Web: Activate
  Web->>Api: POST /api/workflows/{id}/activate
  Api->>Engine: Activate(id)
  Engine->>Compiler: Compile(diagram)
  Compiler->>Validator: Validate(diagram) using RN-10
  alt invalid
    Validator-->>Compiler: all errors (CA-02g)
    Compiler-->>Engine: no document (CA-14c)
    Engine-->>Api: reject with errors, preserve state (CA-03a)
  else valid
    Compiler->>Compiler: deterministic DAG with topological order (CA-14b)
    Compiler-->>Engine: compiled document
    Engine->>DB: immutable version, Active workflow (CA-03b)
    Engine-->>Api: version n
  end
  Api-->>Web: result
  Web-->>U: errors or active version (CA-40e)
```

## B · Execute a workflow (CU-06 through CU-08)

### Sequence

```mermaid
sequenceDiagram
  autonumber
  participant Source as Source system
  participant Api as Api
  participant DB as PostgreSQL
  participant Worker as Worker
  participant Interpreter as Interpreter
  participant Action as IAction
  participant Sim as Simulator
  Source->>Api: POST /api/events (user.registered) with X-Integration-Key
  Api->>Api: validate key, reject invalid key without execution (CA-30d)
  Api->>DB: unique event ID and one Pending execution per active workflow (CA-04a, CA-11a)
  Api-->>Source: 202
  Worker->>DB: claim Pending and due executions (SKIP LOCKED, lease)
  Worker->>Interpreter: ExecuteStep (Timer 2 h)
  Interpreter->>DB: recorded step, Waiting until t0 + 2 h (CA-07a)
  Note over Worker,DB: worker claims execution again after 2 h
  Worker->>Interpreter: ExecuteStep (Call webhook check)
  Interpreter->>DB: save delivery with reference (phase 1)
  Interpreter->>Action: ExecuteAsync(resolved body, reference)
  Action->>Sim: HTTP POST /http/users/status
  alt network error, 10 s timeout or 5xx
    Sim-->>Action: error
    Interpreter->>DB: failed step, Waiting for retry after 1, 5 or 30 min (CA-16b)
    Note over Interpreter,DB: fourth failure or any 4xx causes Failed (CA-09b, CA-16c)
  else 200 with active false
    Sim-->>Action: 200
    Interpreter->>DB: successful step, response in nodes.check (phase 2, CA-16g)
  end
  Worker->>Interpreter: ExecuteStep (Condition active = true)
  Interpreter->>DB: choose no output (CA-05a)
  Worker->>Interpreter: ExecuteStep (Send email, then Send SMS)
  Interpreter->>Action: same delivery reference and retry protocol as webhook
  Action->>Sim: POST /email and POST /sms (CA-06a, CA-06f)
  Interpreter->>DB: execution Completed (CA-19b)
```

### Communication

```mermaid
flowchart LR
  Source["Source system"]
  Api[":Api"]
  Worker[":Worker"]
  Interpreter[":Interpreter"]
  Action[":IAction"]
  DB[(":PostgreSQL")]
  Sim[":Simulator"]
  Source -->|"1: POST /api/events with key"| Api
  Api -->|"2: event and Pending execution"| DB
  Worker -->|"3: claim<br/>6: claim due execution"| DB
  Worker -->|"4: ExecuteStep(Timer)<br/>7: ExecuteStep(Action)"| Interpreter
  Interpreter -->|"5: Waiting<br/>8: save delivery<br/>11: record result and advance"| DB
  Interpreter -->|"9: ExecuteAsync()"| Action
  Action -->|"10: HTTP POST with reference"| Sim
```

Subsequent nodes (Condition, email and SMS) repeat 7 through 11.

## C · Resume after restart (CU-09)

```mermaid
sequenceDiagram
  autonumber
  participant Worker as Worker (after restart)
  participant DB as PostgreSQL
  participant Interpreter as Interpreter
  participant Action as IAction
  participant Sim as Simulator
  Note over Worker,DB: previous process crashed with Waiting executions and a Running delivery in progress
  Worker->>DB: claim due Waiting and expired-lease Running executions (CA-10a, CA-10b)
  loop each claimed execution
    Worker->>Interpreter: ExecuteStep(current node)
    Interpreter->>DB: saved delivery for this node?
    alt saved delivery without result (crash between phases 1 and 2)
      Interpreter->>Action: ExecuteAsync(same reference)
      Action->>Sim: HTTP POST with same reference
      Sim-->>Action: 200, discard duplicate (CA-10c, CA-90c)
    else no pending delivery
      Interpreter->>Interpreter: execute node normally
    end
    Interpreter->>DB: record result and advance, do not repeat recorded steps
  end
  Note over Worker,DB: two workers divide rows using SKIP LOCKED, no step executes twice (CA-13c)
```
