# 7. Despliegue

Convenciones: los subgrafos son nodos «device» o «execution environment»; los rectángulos internos son artefactos desplegados. Una sola instancia de la aplicación (ADR-01). La topología es la misma en una máquina local o en un servidor.

```mermaid
flowchart TB
  subgraph PC["«device» Equipo del usuario"]
    Nav["«execution environment»<br/>Navegador"]
  end
  Origen["«device» Sistema origen<br/>(script o aplicación)"]

  subgraph Host["«device» Equipo local o servidor con Docker"]
    subgraph Compose["«execution environment» Docker Compose"]
      subgraph CApp["«container» app"]
        AApi["«artifact»<br/>WorkflowEngine.Api.dll<br/>API REST + SPA"]
        AProc["«artifact»<br/>procesador en segundo plano"]
        ASample["«artifact»<br/>samples/recordatorio-activacion.json"]
      end
      subgraph CSim["«container» simulador"]
        ASim["«artifact»<br/>WorkflowEngine.Simulator.dll"]
      end
      subgraph CDb["«container» postgres"]
        ADb[("«artifact»<br/>PostgreSQL<br/>volumen de datos")]
      end
    end
  end

  Nav -->|"HTTP"| AApi
  Nav -->|"HTTP"| ASim
  Origen -->|"HTTP POST /api/eventos<br/>clave de integración"| AApi
  AApi -->|"TCP 5432"| ADb
  AProc -->|"TCP 5432"| ADb
  AProc -->|"HTTP · correo, SMS y webhooks"| ASim
```

| Elemento | Notas |
|---|---|
| `app` | Un proceso: API, SPA en el mismo origen (cookie, ADR-08) y procesador en segundo plano. Configuración: cadena de conexión, URL del simulador, cuentas iniciales con su rol y clave de integración (como secretos, nunca en el repositorio). Al arrancar siembra las cuentas e instala el flujo de ejemplo (RF-19). |
| `simulador` | Aplicación aparte; solo se comunica por HTTP (AR-16). Su interfaz indica que es una simulación (CA-90f). |
| `postgres` | Único estado del sistema; un volumen para que sobreviva a `docker compose down`. |
| Instalación | `docker compose up` siguiendo el README, en 15 min o menos (CA-N14a). |

**Pruebas automáticas (CI).** `dotnet test` levanta la aplicación y el simulador en memoria (`WebApplicationFactory`) con `FakeTimeProvider` y un PostgreSQL de prueba en contenedor (Testcontainers), sin red externa (CA-N08c). Las pruebas del editor usan Playwright contra `docker compose`.
