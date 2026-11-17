# 3. Arquitectura

Monolito modular con un solo despliegue (ADR-01), estado durable en PostgreSQL (ADR-03) y terceros simulados por HTTP (ADR-15). La arquitectura recomendada por el enunciado no se adopta (ADR-18, vista 3.4).

## 3.1 Módulos y dependencias permitidas (ADR-07)

Las flechas continuas son referencias de proyecto permitidas. Lo que no tiene flecha está prohibido y lo comprueban las reglas AR.

```mermaid
flowchart TB
  Web["web/<br/>React + TypeScript"]
  Api["Api<br/>HTTP · composición · SPA"]
  Infra["Infrastructure<br/>PostgreSQL · procesador · acciones HTTP"]
  Core["Core<br/>validador · compilador · intérprete<br/>máquina de estados · puertos"]
  Sim["Simulator<br/>aplicación aparte"]

  Api --> Infra
  Api --> Core
  Infra --> Core
  Web -.->|"cliente generado (OpenAPI)"| Api
  Infra -.->|"solo HTTP (AR-16)"| Sim
```

| Regla | Qué impide |
|---|---|
| AR-01 | Que `Core` conozca `Infrastructure`, `Api`, EF Core, ASP.NET Core o HTTP |
| AR-04 | Clientes HTTP hacia terceros fuera de `Infrastructure` |
| AR-16 | Referencias de proyecto a `Simulator` |
| AR-17 | Que el intérprete reciba el diagrama en vez del DAG compilado |
| AR-18 | Cambios de estado de una ejecución fuera de la máquina de estados |

## 3.2 Contenedores

```mermaid
flowchart LR
  User["«persona»<br/>Editor o Reader"]
  Source["«sistema externo»<br/>Sistema origen"]

  subgraph Product["Producto (una instalación)"]
    App["«contenedor»<br/>Aplicación ASP.NET Core<br/>API REST + SPA + procesador"]
    DB[("«contenedor»<br/>PostgreSQL<br/>cuentas, flujos, versiones, eventos,<br/>ejecuciones, pasos, envíos")]
    Sim["«contenedor»<br/>Simulador de terceros<br/>correo, SMS, receptor HTTP"]
  end

  User -->|"HTTP · cookie de sesión<br/>editor e historial"| App
  User -->|"HTTP · interfaz del simulador<br/>(sin sesión)"| Sim
  Source -->|"HTTP POST /api/events<br/>clave de integración"| App
  App -->|"SQL"| DB
  App -->|"HTTP · correo, SMS y webhooks<br/>con reference única"| Sim
```

## 3.3 Procesamiento síncrono y asíncrono

Las peticiones HTTP solo **registran** hechos en una transacción. El trabajo con tiempo y con terceros lo hace un procesador en segundo plano que lee de tablas, así que una caída no pierde nada (ADR-03).

```mermaid
flowchart LR
  subgraph Sync["Petición HTTP (una transacción)"]
    direction TB
    P1["Recibir evento"]
    P2["Activate flujo"]
  end

  subgraph Tables["PostgreSQL"]
    direction TB
    T1[("eventos<br/>único por identificador")]
    T2[("ejecuciones<br/>nodo actual, estado,<br/>vence_en, concesión")]
    T3[("pasos<br/>solo inserción")]
    T4[("envíos<br/>reference única")]
    T5[("versiones de flujo<br/>DAG compilado")]
  end

  subgraph Async["Segundo plano (cada 1 s)"]
    W1["Worker<br/>FOR UPDATE SKIP LOCKED<br/>+ concesión"]
  end

  P1 --> T1
  P1 -->|"una Pending por flujo activo"| T2
  P2 --> T5
  W1 -->|"reclama"| T2
  W1 -->|"lee"| T5
  W1 -->|"registra"| T3
  W1 -->|"guarda antes de llamar"| T4
  W1 -->|"HTTP"| Ext["Simulador"]
```

## 3.4 Arquitectura recomendada frente a la adoptada (ADR-18)

A la izquierda, una lectura de lo que recomienda el enunciado. A la derecha, lo que se construye. Cada pieza de la izquierda que no aparece a la derecha se justifica en ADR-18.

```mermaid
flowchart LR
  subgraph Recommended["Recomendada (no se adopta)"]
    direction TB
    R1["Servicio de flujos"]
    R2["Servicio orquestador<br/>máquina de estados + Saga"]
    R3["Servicio de acciones"]
    RR[("Redis<br/>bloqueos distribuidos")]
    RP[("PostgreSQL")]
    R1 <-->|"red"| R2
    R2 <-->|"red"| R3
    R2 --> RR
    R3 --> RR
    R1 --> RP
    R2 --> RP
  end

  subgraph Adopted["Adoptada"]
    direction TB
    A1["Una aplicación<br/>Core + Infrastructure + Api<br/>máquina de estados, sin Saga"]
    AP[("PostgreSQL<br/>estado + reclamo con<br/>FOR UPDATE SKIP LOCKED")]
    A1 --> AP
  end

  Recommended ~~~ Adopted
```
