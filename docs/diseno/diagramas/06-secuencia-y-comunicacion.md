# 6. Secuencia y comunicación

Vistas simplificadas de los tres CU clave ([01-casos-de-uso.md](01-casos-de-uso.md)). B usa el flujo de ejemplo del enunciado (RF-19).

## A · Diseñar, validar y activar un flujo (CU-02 a CU-04)

```mermaid
sequenceDiagram
  autonumber
  actor U as Editor
  participant Web as web (editor)
  participant Api as Api
  participant Mot as IMotor
  participant Com as Compilador
  participant Val as Validador
  participant DB as PostgreSQL

  Note over U,Api: sesión iniciada (cookie, CA-30a)
  U->>Web: arrastra nodos, los conecta y edita parámetros
  Web->>Api: PUT /api/flujos/{id} (diagrama)
  Api->>Api: ¿rol Editor? Si es Lector, rechazo sin cambios (CA-30b)
  Api->>Mot: GuardarBorrador(diagrama)
  Mot->>DB: guarda el diagrama
  U->>Web: Activar
  Web->>Api: POST /api/flujos/{id}/activar
  Api->>Mot: Activar(id)
  Mot->>Com: Compilar(diagrama)
  Com->>Val: Validar(diagrama) según RN-10
  alt inválido
    Val-->>Com: todos los errores (CA-02g)
    Com-->>Mot: sin documento (CA-14c)
    Mot-->>Api: rechazo con errores, el estado no cambia (CA-03a)
  else válido
    Com->>Com: DAG con orden topológico, determinista (CA-14b)
    Com-->>Mot: documento compilado
    Mot->>DB: versión inmutable, flujo Activo (CA-03b)
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
  participant Ori as Sistema origen
  participant Api as Api
  participant DB as PostgreSQL
  participant Pro as Procesador
  participant Int as Interprete
  participant Acc as IAccion
  participant Sim as Simulador

  Ori->>Api: POST /api/eventos (usuario.registrado) con X-Clave-Integracion
  Api->>Api: valida la clave, si no es válida rechaza sin crear ejecución (CA-30d)
  Api->>DB: evento único por id y una ejecución Pendiente por flujo activo (CA-04a, CA-11a)
  Api-->>Ori: 202
  Pro->>DB: reclama Pendientes y vencidas (SKIP LOCKED, concesión)
  Pro->>Int: EjecutarPaso (Temporizador 2 h)
  Int->>DB: paso registrado, Esperando hasta t0 + 2 h (CA-07a)
  Note over Pro,DB: 2 h después el procesador la reclama de nuevo
  Pro->>Int: EjecutarPaso (Llamar webhook «verificar»)
  Int->>DB: guarda el envío con su referencia (fase 1)
  Int->>Acc: EjecutarAsync(cuerpo relleno, referencia)
  Acc->>Sim: HTTP POST /http/usuarios/estado
  alt red, 10 s agotados o 5xx
    Sim-->>Acc: error
    Int->>DB: paso con error, Esperando hasta el reintento a 1, 5 o 30 min (CA-16b)
    Note over Int,DB: al cuarto fallo, o con un 4xx, la ejecución queda Fallida (CA-09b, CA-16c)
  else 200 {"activo": false}
    Sim-->>Acc: 200
    Int->>DB: paso correcto, respuesta en nodos.verificar (fase 2, CA-16g)
  end
  Pro->>Int: EjecutarPaso (Condición activo = true)
  Int->>DB: salida «no» (CA-05a)
  Pro->>Int: EjecutarPaso (Enviar correo, luego Enviar SMS)
  Int->>Acc: igual que el webhook: envío con referencia y reintentos
  Acc->>Sim: POST /correo · POST /sms (CA-06a, CA-06f)
  Int->>DB: ejecución Completada (CA-19b)
```

### Comunicación

```mermaid
flowchart LR
  Ori["Sistema origen"]
  Api[":Api"]
  Pro[":Procesador"]
  Int[":Interprete"]
  Acc[":IAccion"]
  DB[(":PostgreSQL")]
  Sim[":Simulador"]

  Ori -->|"1: POST /api/eventos con clave"| Api
  Api -->|"2: evento y ejecución Pendiente"| DB
  Pro -->|"3: reclamar<br/>6: reclamar la vencida"| DB
  Pro -->|"4: EjecutarPaso(Temporizador)<br/>7: EjecutarPaso(Acción)"| Int
  Int -->|"5: Esperando<br/>8: guardar envío<br/>11: registrar resultado y avanzar"| DB
  Int -->|"9: EjecutarAsync()"| Acc
  Acc -->|"10: HTTP POST con referencia"| Sim
```

Los nodos siguientes (Condición, correo y SMS) repiten 7 a 11.

## C · Reanudar tras un reinicio (CU-09)

```mermaid
sequenceDiagram
  autonumber
  participant Pro as Procesador (tras reiniciar)
  participant DB as PostgreSQL
  participant Int as Interprete
  participant Acc as IAccion
  participant Sim as Simulador

  Note over Pro,DB: el proceso anterior cayó con ejecuciones Esperando y una En curso a medio envío
  Pro->>DB: reclama Esperando vencidas y En curso con concesión expirada (CA-10a, CA-10b)
  loop por cada ejecución reclamada
    Pro->>Int: EjecutarPaso(nodo actual)
    Int->>DB: ¿hay un envío guardado para este nodo?
    alt envío guardado sin resultado (caída entre fase 1 y fase 2)
      Int->>Acc: EjecutarAsync(misma referencia)
      Acc->>Sim: HTTP POST con la misma referencia
      Sim-->>Acc: 200, el duplicado se descarta (CA-10c, CA-90c)
    else sin envío pendiente
      Int->>Int: ejecuta el nodo normalmente
    end
    Int->>DB: registra el resultado y avanza, los pasos ya registrados no se repiten
  end
  Note over Pro,DB: con dos procesadores, SKIP LOCKED reparte las filas y ningún paso se ejecuta dos veces (CA-13c)
```
