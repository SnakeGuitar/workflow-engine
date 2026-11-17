# 3. Architecture

## 3.1 Modules and allowed dependencies (ADR-07)

```mermaid
flowchart TB
  Web["web<br/>React + TypeScript"]
  Api["Api<br/>HTTP · authentication · composition"]
  Infra["Infrastructure<br/>EF Core · worker · HTTP actions"]
  Core["Core<br/>validator · compiler · interpreter<br/>state machine · ports"]
  Sim["Simulator<br/>separate application"]
  Api --> Infra
  Api --> Core
  Infra --> Core
  Web -.->|"generated client (OpenAPI)"| Api
  Infra -.->|"HTTP only (AR-16)"| Sim
```

| Rule | Prevents |
|---|---|
| AR-01 | `Core` knowing `Infrastructure`, `Api`, EF Core, ASP.NET Core or HTTP |
| AR-04 | Third-party HTTP clients outside `Infrastructure` |
| AR-16 | Project references to `Simulator` |
| AR-17 | Passing diagrams instead of compiled DAGs to the interpreter |
| AR-18 | Execution state changes outside the state machine |

## 3.2 Containers

```mermaid
flowchart LR
  User["«person»<br/>Editor or Reader"]
  Source["«external system»<br/>Source system"]
  subgraph Product["Product (one installation)"]
    App["«container»<br/>ASP.NET Core application<br/>REST API + SPA + worker"]
    DB[("«container»<br/>PostgreSQL<br/>accounts, workflows, versions, events,<br/>executions, steps, deliveries")]
    Sim["«container»<br/>Third-party simulator<br/>email, SMS, HTTP receiver"]
  end
  User -->|"HTTP · session cookie<br/>editor and history"| App
  User -->|"HTTP · simulator UI<br/>(no session)"| Sim
  Source -->|"HTTP POST /api/events<br/>integration key"| App
  App -->|SQL| DB
  App -->|"HTTP · email, SMS, webhooks<br/>unique reference"| Sim
```

## 3.3 Synchronous and asynchronous processing

HTTP requests only **record** facts in a transaction. A background worker reads database tables to handle time and third-party calls, so crashes do not lose work (ADR-03).

```mermaid
flowchart LR
  subgraph Sync["HTTP request (one transaction)"]
    P1["Receive event"]
    P2["Activate workflow"]
  end
  subgraph Tables["PostgreSQL"]
    T1[("events<br/>unique identifier")]
    T2[("executions<br/>current node, state,<br/>due_at, lease")]
    T3[("steps<br/>insert-only")]
    T4[("deliveries<br/>unique reference")]
    T5[("workflow versions<br/>compiled DAG")]
  end
  subgraph Async["Background (every 1 s)"]
    W1["Worker<br/>FOR UPDATE SKIP LOCKED<br/>+ lease"]
  end
  P1 --> T1
  P1 -->|"one Pending per active workflow"| T2
  P2 --> T5
  W1 -->|claims| T2
  W1 -->|reads| T5
  W1 -->|records| T3
  W1 -->|"saves before calling"| T4
  W1 -->|HTTP| Ext["Simulator"]
```

## 3.4 Recommended versus adopted architecture (ADR-18)

The left side interprets the assignment's recommendation; the right side shows what will be built. ADR-18 explains each omitted component.

```mermaid
flowchart LR
  subgraph Recommended["Recommended (not adopted)"]
    R1["Workflow service"]
    R2["Orchestration service<br/>state machine + Saga"]
    R3["Action service"]
    RR[("Redis<br/>distributed locks")]
    RP[("PostgreSQL")]
    R1 <-->|network| R2
    R2 <-->|network| R3
    R2 --> RR
    R3 --> RR
    R1 --> RP
    R2 --> RP
  end
  subgraph Adopted["Adopted"]
    A1["One application<br/>Core + Infrastructure + Api<br/>state machine, no Saga"]
    AP[("PostgreSQL<br/>state + claims using<br/>FOR UPDATE SKIP LOCKED")]
    A1 --> AP
  end
  Recommended ~~~ Adopted
```
