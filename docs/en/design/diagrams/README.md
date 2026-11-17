# Diagrams

UML views derived from [requirements.md](../../requirements.md) v1.1 and [architecture.md](../../architecture.md) v1.1. **Those documents take precedence over conflicting diagrams**; correct the diagram when they disagree.

Notation: [Mermaid](https://mermaid.js.org/), rendered by GitHub in Markdown. Mermaid lacks UML use-case, component, communication and deployment diagrams; these use `flowchart` and the conventions in each file.

| # | File | Contents |
|---|---|---|
| 1 | [01-use-cases.md](01-use-cases.md) | CU catalog, diagram and key case descriptions |
| 2 | [02-components.md](02-components.md) | Components and interfaces |
| 3 | [03-architecture.md](03-architecture.md) | Modules, containers, asynchronous processing, recommended versus adopted architecture |
| 4 | [04-classes.md](04-classes.md) | Model, engine and ports |
| 5 | [05-states.md](05-states.md) | Execution and workflow |
| 6 | [06-sequence-and-communication.md](06-sequence-and-communication.md) | Three key sequences and engine communication |
| 7 | [07-deployment.md](07-deployment.md) | Docker Compose installation |

## Key use cases

| Key | CU | Assignment task |
|---|---|---|
| A | CU-02 through CU-04: design, validate, activate | T1 editor, T2 parser/DAG |
| B | CU-06 through CU-08: execute | T3 asynchronous engine, T4 webhooks/simulator |
| C | CU-09: resume after restart | T3 durability; supports the decision to omit Redis (ADR-18) |

Access (CU-01, RF-30) appears in use cases, components, containers, class model and sequence A.

## Images

Generated from the Mermaid blocks using `@mermaid-js/mermaid-cli` at scale 2, white background, then converted to JPG. Regenerate images whenever diagrams change.

| Source | Images |
|---|---|
| [01-use-cases.md](01-use-cases.md) | [1](img/01-use-cases-01.jpg) |
| [02-components.md](02-components.md) | [1](img/02-components-01.jpg) |
| [03-architecture.md](03-architecture.md) | [1](img/03-architecture-01.jpg) · [2](img/03-architecture-02.jpg) · [3](img/03-architecture-03.jpg) · [4](img/03-architecture-04.jpg) |
| [04-classes.md](04-classes.md) | [1](img/04-classes-01.jpg) · [2](img/04-classes-02.jpg) |
| [05-states.md](05-states.md) | [1](img/05-states-01.jpg) · [2](img/05-states-02.jpg) |
| [06-sequence-and-communication.md](06-sequence-and-communication.md) | [1](img/06-sequence-and-communication-01.jpg) · [2](img/06-sequence-and-communication-02.jpg) · [3](img/06-sequence-and-communication-03.jpg) · [4](img/06-sequence-and-communication-04.jpg) |
| [07-deployment.md](07-deployment.md) | [1](img/07-deployment-01.jpg) |
