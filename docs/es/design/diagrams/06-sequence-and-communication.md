# 6. Secuencia y comunicación

Vistas simplificadas de los tres CU clave ([01-use-cases.md](01-use-cases.md)). B usa el flujo de ejemplo del enunciado (RF-19).

## A · Diseñar, validar y activar un flujo (CU-02 a CU-04)

```mermaid
sequenceDiagram
  autonumber
  actor U as Editor
  participant Web as web (editor)
  participant Api as Api
  participant Mot as IEngine
  participant Com as Compiler
  participant Val as Validator
  participant DB as PostgreSQL

  Note over U,Api: sesión iniciada (cookie, CA-30a)
  U->>Web: arrastra nodos, los conecta y edita parámetros
  Web->>Api: PUT /api/workflows/{id} (diagram)
  Api->>Api: ¿rol Editor? Si es Reader, rechazo sin cambios (CA-30b)
  Api->>Mot: SaveDraft(diagram)
  Mot->>DB: guarda el diagram
  U->>Web: Activate
  Web->>Api: POST /api/workflows/{id}/activate
  Api->>Mot: Activate(id)
  Mot->>Com: Compile(diagram)
  Com->>Val: Validate(diagram) según RN-10
  alt inválido
    Val-->>Com: todos los errores (CA-02g)
    Com-->>Mot: sin documento (CA-14c)
    Mot-->>Api: rechazo con errores, el estado no cambia (CA-03a)
  else válido
    Com->>Com: DAG con orden topológico, determinista (CA-14b)
    Com-->>Mot: documento compilado
    Mot->>DB: versión inmutable, flujo Active (CA-03b)
    Mot-->>Api: versión n
  end
  Api-->>Web: resultado
  Web-->>U: errores o versión activa (CA-40e)
```

## B · Ejecutar un flujo (CU-06 a CU-08)

### Secuencia

```mermaid
sequenceDiagram
  autonumber
  participant Ori as System origen
  participant Api as Api
  participant DB as PostgreSQL
  participant Pro as Worker
  participant Int as Interpreter
  participant Acc as IAction
  participant Sim as Simulator

  Ori->>Api: POST /api/events (user.registered) con X-Integration-Key
  Api->>Api: valida la clave, si no es válida rechaza sin crear ejecución (CA-30d)
  Api->>DB: evento único por id y una ejecución Pending por flujo active (CA-04a, CA-11a)
  Api-->>Ori: 202
  Pro->>DB: reclama Pendientes y vencidas (SKIP LOCKED, concesión)
  Pro->>Int: ExecuteStep (Timer 2 h)
  Int->>DB: paso registrado, Waiting hasta t0 + 2 h (CA-07a)
  Note over Pro,DB: 2 h después el procesador la reclama de nuevo
  Pro->>Int: ExecuteStep (Llamar webhook «verificar»)
  Int->>DB: guarda el envío con su reference (fase 1)
  Int->>Acc: ExecuteAsync(cuerpo relleno, reference)
  Acc->>Sim: HTTP POST /http/users/status
  alt red, 10 s agotados o 5xx
    Sim-->>Acc: error
    Int->>DB: paso con error, Waiting hasta el reintento a 1, 5 o 30 min (CA-16b)
    Note over Int,DB: al cuarto fallo, o con un 4xx, la ejecución queda Failed (CA-09b, CA-16c)
  else 200 {"active": false}
    Sim-->>Acc: 200
    Int->>DB: paso correcto, respuesta en nodes.check (fase 2, CA-16g)
  end
  Pro->>Int: ExecuteStep (Condición active = true)
  Int->>DB: salida «no» (CA-05a)
  Pro->>Int: ExecuteStep (Enviar correo, luego Enviar SMS)
  Int->>Acc: igual que el webhook: envío con reference y reintentos
  Acc->>Sim: POST /email · POST /sms (CA-06a, CA-06f)
  Int->>DB: ejecución Completed (CA-19b)
```

### Comunicación

```mermaid
flowchart LR
  Ori["Sistema origen"]
  Api[":Api"]
  Pro[":Worker"]
  Int[":Interpreter"]
  Acc[":IAction"]
  DB[(":PostgreSQL")]
  Sim[":Simulador"]

  Ori -->|"1: POST /api/events con clave"| Api
  Api -->|"2: evento y ejecución Pending"| DB
  Pro -->|"3: reclamar<br/>6: reclamar la vencida"| DB
  Pro -->|"4: ExecuteStep(Temporizador)<br/>7: ExecuteStep(Acción)"| Int
  Int -->|"5: Waiting<br/>8: guardar envío<br/>11: registrar resultado y avanzar"| DB
  Int -->|"9: ExecuteAsync()"| Acc
  Acc -->|"10: HTTP POST con reference"| Sim
```

Los nodos siguientes (Condición, correo y SMS) repiten 7 a 11.

## C · Reanudar tras un reinicio (CU-09)

```mermaid
sequenceDiagram
  autonumber
  participant Pro as Worker (tras reiniciar)
  participant DB as PostgreSQL
  participant Int as Interpreter
  participant Acc as IAction
  participant Sim as Simulator

  Note over Pro,DB: el proceso anterior cayó con ejecuciones Waiting y una En curso a medio envío
  Pro->>DB: reclama Waiting vencidas y En curso con concesión expirada (CA-10a, CA-10b)
  loop por cada ejecución reclamada
    Pro->>Int: ExecuteStep(nodo actual)
    Int->>DB: ¿hay un envío guardado para este nodo?
    alt envío guardado sin resultado (caída entre fase 1 y fase 2)
      Int->>Acc: ExecuteAsync(misma reference)
      Acc->>Sim: HTTP POST con la misma reference
      Sim-->>Acc: 200, el duplicado se descarta (CA-10c, CA-90c)
    else sin envío pendiente
      Int->>Int: ejecuta el nodo normalmente
    end
    Int->>DB: registra el resultado y avanza, los pasos ya registrados no se repiten
  end
  Note over Pro,DB: con dos procesadores, SKIP LOCKED reparte las filas y ningún paso se ejecuta dos veces (CA-13c)
```

## Conexiones personales (RF-100)

El titular configura las tres operaciones versionadas y la carpeta de destino antes de activar el flujo.

```mermaid
sequenceDiagram
  participant Shopify
  participant Api
  participant Engine
  participant OpenAI
  participant Google
  Shopify->>Api: Notificación de pedido
  Api->>Api: Verificar origen y deduplicar por conexión
  Api->>Engine: Iniciar la versión activa correspondiente
  Engine->>OpenAI: Resumir el pedido con la conexión del titular
  OpenAI-->>Engine: Texto del resumen
  Engine->>Google: Crear documento en la carpeta elegida
  Google-->>Engine: ID del documento
```
