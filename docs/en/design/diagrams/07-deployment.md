# 7. Deployment

Conventions: subgraphs are “device” or “execution environment” nodes; inner rectangles are deployed artifacts. One application instance (ADR-01). The topology is the same locally and on a server.

```mermaid
flowchart TB
  subgraph PC["«device» User computer"]
    Browser["«execution environment»<br/>Browser"]
  end
  Source["«device» Source system<br/>(script or application)"]
  subgraph Host["«device» Local computer or Docker server"]
    subgraph Compose["«execution environment» Docker Compose"]
      subgraph CApp["«container» app"]
        AApi["«artifact»<br/>WorkflowEngine.Api.dll<br/>REST API + SPA"]
        AWorker["«artifact»<br/>background worker"]
        ASample["«artifact»<br/>samples/activation-reminder.json"]
      end
      subgraph CSim["«container» simulator"]
        ASim["«artifact»<br/>WorkflowEngine.Simulator.dll"]
      end
      subgraph CDb["«container» postgres"]
        ADb[("«artifact»<br/>PostgreSQL<br/>data volume")]
      end
    end
  end
  Browser -->|HTTP| AApi
  Browser -->|HTTP| ASim
  Source -->|"HTTP POST /api/events<br/>integration key"| AApi
  AApi -->|"TCP 5432"| ADb
  AWorker -->|"TCP 5432"| ADb
  AWorker -->|"HTTP · email, SMS and webhooks"| ASim
```

| Element | Notes |
|---|---|
| `app` | One process: API, same-origin SPA (cookie, ADR-08) and background worker. Configuration: connection string, simulator URL, initial accounts/roles and integration key (secrets, never committed). Startup seeds accounts and installs the example workflow (RF-19). |
| `simulator` | Separate application; HTTP communication only (AR-16). UI identifies it as a simulation (CA-90f). |
| `postgres` | The system's only state store; a volume preserves data through `docker compose down`. |
| Installation | `docker compose up` following the README, within 15 min (CA-N14a). |

**Automated tests (CI).** `dotnet test` hosts the app and simulator in memory (`WebApplicationFactory`) with `FakeTimeProvider` and a PostgreSQL test container (Testcontainers), without external networking (CA-N08c). Editor tests use Playwright against `docker compose`.
