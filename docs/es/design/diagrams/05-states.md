# 5. Máquinas de estado

## 5.1 Ejecución (requisitos §5.1, RN-30, AR-18)

```mermaid
stateDiagram-v2
  state "Pendiente" as Pending
  state "En curso" as Running
  state "Esperando" as Waiting
  state "Completada" as Completed
  state "Fallida" as Failed

  [*] --> Pending : llega un evento del tipo del flujo
  Pending --> Running : el procesador la reclama
  Running --> Waiting : Timer o reintento programado
  Running --> Completed : último nodo o salida sin conectar
  Running --> Failed : reintentos agotados, 4xx o variable sin valor
  Waiting --> Running : vence la espera
  Completed --> [*]
  Failed --> [*]

  note right of Running
    Concesión con expiración: si el proceso cae,
    otro reclamo la retoma (CA-10b).
  end note
```

## 5.2 Flujo (requisitos §5.2, RN-11)

```mermaid
stateDiagram-v2
  [*] --> Draft : crear
  Draft --> Draft : guardar · validar con errores (CA-03a)
  Draft --> Active : activar [válido] / compila y crea la versión 1
  Active --> Active : activar cambios [válidos] / versión n+1 (CA-03c)
  Active --> Inactive : desactivar (CA-03d)
  Inactive --> Active : activar [válido] / versión n+1

  note right of Active
    Editar el diagram no cambia la versión
    que se ejecuta hasta volver a activar.
  end note
```
