# Arquitectura: motor de ejecución y automatización de flujos de trabajo

| Campo | Valor |
|---|---|
| Versión | 1.0 |
| Entrada | [requisitos.md](requisitos.md) v1.0 |
| Razones y alternativas | [humanos/decisiones.md](humanos/decisiones.md) (no necesario para implementar) |
| Arnés e hitos | [humanos/arnes-e-hitos.md](humanos/arnes-e-hitos.md) |
| Diagramas | [diseno/diagramas/](diseno/diagramas/) |

Cada `ADR-xx` fija una decisión vigente y los requisitos que la motivan; los huecos de numeración son ADR retiradas. Las versiones de paquetes son las conocidas: **verificarlas al instalar**. Las reglas de la sección 3 son ejecutables y alimentan RH-07.

---

## 1. Decisiones

### ADR-18 No se adopta la arquitectura recomendada por el enunciado

El enunciado **recomienda** (no exige) microservicios orquestados con una máquina de estados finita, patrón Saga si hiciera falta, PostgreSQL para el estado y Redis para bloqueos distribuidos. Se adopta solo lo que los requisitos justifican:

| Recomendado | Decisión | Por qué |
|---|---|---|
| Microservicios | **No.** Monolito modular, una instancia (ADR-01) | Ninguna tarea del enunciado exige desplegar o escalar componentes por separado. Con 2 personas y 17 días, cada servicio extra añade llamadas de red que pueden fallar, contratos que versionar y despliegues que coordinar, sin mejorar ningún CA. El único proceso aparte es el simulador, porque representa a terceros reales (ADR-15). |
| Máquina de estados finita | **Sí** (ADR-16) | La ejecución tiene estados y transiciones explícitos (requisitos §5.1, RF-18). |
| Patrón Saga | **No** | Saga compensa transacciones repartidas entre varios servicios con bases de datos propias. Aquí todo el estado vive en una sola base y cada avance es una transacción local (ADR-03): no hay nada repartido que compensar. Las acciones hacia terceros (correo, SMS, webhook) no se deshacen; se reintentan con referencia única (RN-06, RN-09). |
| PostgreSQL | **Sí** (ADR-03) | Transacciones, bloqueo de filas y reanudación tras un reinicio (RF-10). |
| Redis para bloqueos distribuidos | **No** | Su fin es que una tarea no se ejecute dos veces con varios servidores. PostgreSQL ya lo resuelve: cada procesador reclama trabajo con `SELECT … FOR UPDATE SKIP LOCKED` y una concesión que expira (ADR-03). Un segundo almacén añadiría otra pieza que operar y la posibilidad de que el candado y el estado no coincidan tras una caída. CA-13c lo demuestra con dos procesadores sobre la misma base. |
| JSON ejecutable como DAG | **Sí** (ADR-16) | Tarea 2 del enunciado (RF-14). |

- **Sacrificio aceptado:** no hay escalado independiente por componente.
- **Si se exigiera escalar:** se arrancan más instancias del procesador contra la misma base. El diseño ya lo admite (CA-13c), sin Redis ni cambios de código.

### ADR-01 Monolito modular, un solo despliegue
- **Decisión:** una aplicación ASP.NET Core con la API, la interfaz y el procesador en segundo plano, más el simulador como aplicación aparte. El MVP corre con **una instancia**.
- **Condición de corrección con varias instancias:** el reclamo por base de datos (ADR-03) impide que dos instancias ejecuten el mismo paso (CA-13c).

### ADR-02 Plataforma: .NET + PostgreSQL + React/TypeScript
- **Backend:** ASP.NET Core, versión LTS vigente (conocida: .NET 10; verificar).
- **Datos:** PostgreSQL con Entity Framework Core y Npgsql.
- **Interfaz:** React con TypeScript (SPA). Editor de nodos con React Flow (`@xyflow/react`; verificar licencia y versión).

### ADR-03 Persistencia y ejecución durables sobre PostgreSQL
- **Motivo:** RF-09 a RF-11, RF-13, RN-06, RN-09.
- **Decisión:**
  1. **Una ejecución es una fila** con su versión de flujo, contexto (JSON), nodo actual, estado, número de intento y, si espera, el instante en que vence (UTC). Cada paso se registra en una tabla **solo de inserción** (RN-12).
  2. **Cada avance es una transacción:** actualizar la ejecución, insertar el paso y programar lo siguiente se confirma o se revierte en bloque. Solo `Infrastructure` abre transacciones, detrás del puerto de almacenamiento de `Core`.
  3. **Un procesador en segundo plano** consulta cada 1 s las ejecuciones Pendientes, las Esperando ya vencidas y las En curso cuya concesión expiró, y las reclama con `FOR UPDATE SKIP LOCKED` y una **concesión que expira**: si el proceso cae a mitad de paso, otro reclamo la retoma (CA-10b).
  4. **Envíos con referencia única** (`ejecución:nodo`). Antes de llamar al tercero, una transacción guarda el envío con su referencia; después de la llamada, otra registra el resultado y avanza. Si el proceso cae entre ambas, al reanudar se repite la llamada con la **misma referencia** y el receptor descarta el duplicado (CA-10c, CA-11b). Es **entrega al menos una vez más idempotencia**, no "exactamente una vez".
  5. **Un reintento es una espera del mismo nodo:** la ejecución pasa a Esperando hasta el siguiente intento (1, 5 y 30 min, RN-09) y repite el nodo con el número de intento siguiente.
  6. **Deduplicación de eventos** con restricción única sobre el identificador del evento (CA-11a).
- **Límite:** la precisión de un temporizador depende del intervalo de consulta (1 s).

### ADR-04 El tiempo es una dependencia
- **Motivo:** RNF-08, CA-07a, CA-09a.
- **Decisión:** todo el código obtiene la hora por `TimeProvider`; las pruebas usan `FakeTimeProvider` (paquete `Microsoft.Extensions.TimeProvider.Testing`). Los instantes se guardan en UTC y una duración se suma en tiempo transcurrido.
- **Se aplica mecánicamente:** AR-03.

### ADR-05 Acciones como puerto; terceros por HTTP
- **Motivo:** RF-06, RF-16, RN-29.
- **Decisión:**
  - `Core` define `IAccion`: nombre, esquema de parámetros y `EjecutarAsync(parámetros resueltos, referencia)`, que devuelve **Correcto** (con código y cuerpo, si los hay), **Reintentable** o **Definitivo** según RN-29.
  - `Infrastructure` implementa las tres acciones con `HttpClient` (tiempo máximo 10 s). Correo y SMS llaman a los receptores del simulador; Llamar webhook, a la URL del nodo. Las URL del simulador son configuración.
  - El intérprete resuelve las variables antes de llamar a la acción (RN-41) y guarda la respuesta del webhook en `nodos.<id>` del contexto (CA-16g).

### ADR-06 Los flujos son datos validados, y el motor los interpreta
- **Motivo:** RF-01 a RF-05, RN-10, RN-11, RN-40.
- **Decisión:**
  - Un flujo es un **documento JSON** guardado por **versión inmutable**.
  - Un **validador puro**, sin acceso a datos, implementa RN-10 y devuelve todos los errores (CA-02g).
  - Los tipos de nodo y las acciones se registran en un **registro único y cerrado**: un flujo con una acción desconocida no se valida (CA-06e).
  - **Evento** declara un tipo de evento. **Condición** declara ruta del contexto, operador y valor, y tiene salidas «sí» y «no». **Temporizador** declara una duración. **Acción** declara la acción y sus parámetros.
  - Un nodo que no es Condición tiene como máximo una salida, así que una ejecución tiene un único nodo actual (ADR-03).

### ADR-07 Estructura de la solución y dependencias
- **Decisión:** proyectos con dependencias de un solo sentido:
  - `Core`: motor (validador, compilador, intérprete, máquina de estados) y puertos (almacenamiento, `IAccion`). **No conoce** la base de datos, HTTP ni ASP.NET.
  - `Infrastructure`: EF Core y PostgreSQL, procesador en segundo plano y acciones HTTP. Implementa los puertos de `Core`.
  - `Api`: HTTP, autenticación y roles, composición y la SPA compilada.
  - `Simulator`: aplicación aparte; nadie la referencia (ADR-15).

```
Api ──► Infrastructure ──► Core
 └─────────────────────────►┘
Simulator  (sin referencias; solo HTTP)
```

```
src/
  WorkflowEngine.Core/
  WorkflowEngine.Infrastructure/
  WorkflowEngine.Api/
  WorkflowEngine.Simulator/
  web/                            React + TypeScript (React Flow)
samples/
  recordatorio-activacion.json    flujo de ejemplo del enunciado (RF-19)
tests/
  Acceptance/                     un test por CA, etiquetado con su ID (protegido)
  Unit/
  Architecture/                   reglas de la sección 3
  Support/                        fábrica de la aplicación, reloj falso, aserciones con ID de CA
tools/
  Harness/                        consola del arnés: verify, trace, freeze, task, budget, report
harness/
  scope.json                      CA comprometidos (protegido)
  tasks.ndjson                    registro por tarea (RH-04, solo se añaden líneas)
  metrics/                        trazabilidad y métricas M1 a M6
docs/
  requisitos.md  arquitectura.md
  diseno/diagramas/               vistas UML
  humanos/                        contexto, decisiones, riesgos e historial
docker-compose.yml
AGENTS.md
```

### ADR-08 API HTTP con contrato, sesión por cookie y clave de integración
- **Motivo:** RF-30, RNF-07, y que los agentes no desalineen la interfaz y el backend.
- **Decisión:**
  - API REST con JSON y especificación OpenAPI; el cliente TypeScript se **genera** a partir de ella. La API sirve la SPA desde el mismo origen.
  - Autenticación con ASP.NET Core Identity y cookie de sesión; el bloqueo tras 5 intentos (CA-30c) viene incluido. Dos roles fijos, `Editor` y `Lector`, aplicados con políticas de autorización en cada recurso: lectura para ambos, escritura solo para `Editor` (CA-30b).
  - Las cuentas se siembran al arrancar desde la configuración de la instalación (usuario, contraseña inicial y rol). No hay recursos de gestión de usuarios.
  - `POST /api/eventos` no usa sesión: exige la clave de integración en la cabecera `X-Clave-Integracion`, comparada en tiempo constante con la de la configuración (CA-30d).
- **Recursos:** `/api/sesion` (iniciar y cerrar), `/api/flujos` (guardar, validar, activar, desactivar), `/api/eventos` (recibir evento, RF-04), `/api/ejecuciones` (historial, RF-12).
- **Límite:** la cookie exige que la interfaz y la API compartan origen. Se acepta.

### ADR-09 Estrategia de pruebas y arnés
- **Motivo:** RH-01 a RH-14, RNF-08. El detalle operativo (componentes, CI, hitos) está en [humanos/arnes-e-hitos.md](humanos/arnes-e-hitos.md).
- **Decisión:**
  - **Pruebas de aceptación** contra la aplicación completa en memoria (`WebApplicationFactory`), con PostgreSQL real de prueba (un contenedor por job), reloj falso y el simulador en memoria como doble de los terceros.
  - Cada prueba lleva un atributo con su ID (`[Ac("CA-07a")]`). El comando `trace` cruza esos IDs con `docs/requisitos.md` y `harness/scope.json` y calcula la cobertura de CA (RH-01). Los mensajes de fallo incluyen el ID del CA y qué se esperaba (RH-06).
  - **Archivos protegidos (RH-02):** `tests/Acceptance/`, `docs/requisitos.md`, la sección 3 de este documento y `harness/scope.json`. Un chequeo en CI compara su huella y falla si cambian sin la aprobación de una persona revisora: la rama principal exige revisión de los propietarios (CODEOWNERS) y la línea `Approved-By:` del commit debe nombrar a quien aprobó el PR.
  - **Alcance comprometido:** `harness/scope.json` lista los CA comprometidos; la trazabilidad (RH-01, AR-08) se mide contra él.
  - **Oráculo separado:** las pruebas de aceptación de los CA comprometidos se escriben sin acceso a la implementación, a partir del texto del CA y del contrato OpenAPI (ADR-08), y una persona las revisa antes de congelarlas; el registro de tareas anota la condición (RH-04).
  - La infraestructura de pruebas (fábrica de la aplicación, reloj falso, aserciones con ID de CA) vive en `tests/Support/` y no está protegida.
  - Las pruebas de la interfaz (CA-40, CA-30e, CA-04c) usan un navegador automatizado (Playwright).
  - **Registro del experimento (RH-04):** cada tarea de agente escribe una línea en `harness/tasks.ndjson` con versión de la especificación, commit base, modelo, tarea, RF y CA, inicio y fin, iteraciones, uso, resultado e intervenciones. De ahí salen M1 a M6 (RH-08). El comando `task` rechaza una tarea sin CA (RH-03) o con el presupuesto agotado (RH-14).

### ADR-10 El historial es dato, no log; las credenciales no se registran
- **Motivo:** RNF-07.
- **Decisión:** el historial de ejecuciones (RN-12, RN-30) es dato del dominio en PostgreSQL. Los logs técnicos son estructurados y llevan el identificador de la ejecución como correlación. Las contraseñas y la clave de integración se modelan con tipos específicos que **no se serializan** en logs (AR-05).

### ADR-15 Simulador de APIs de terceros
- **Motivo:** RF-90, RF-91, RNF-08.
- **Decisión:** `WorkflowEngine.Simulator` es una aplicación web **separada**: representa al mundo exterior y el motor la llama por HTTP como a cualquier tercero.
  - Receptores de **correo**, **SMS** y **HTTP POST**; registra lo recibido, lo muestra en su interfaz y deduplica por referencia (CA-90c).
  - El receptor HTTP responde por ruta con el código y el cuerpo configurados (CA-90g); así el flujo de ejemplo «verifica» la cuenta.
  - **Inyección de fallos** por servicio, desde su interfaz o por una API de administración (RF-91).
  - **Un componente, dos papeles:** demostración y **doble de pruebas**, alojado en memoria con el reloj falso. No hay dos dobles que diverjan.
- **Límite:** su interfaz indica que es una simulación (CA-90f).

### ADR-16 Compilación a DAG y máquina de estados de la ejecución
- **Motivo:** RF-02, RF-14, RF-18, RN-10, RN-30.
- **Decisión:**
  - El editor guarda el **diagrama**. Un **compilador puro** lo valida (RN-10) y produce el **DAG ejecutable**: nodos con identificador estable, aristas, nodo de inicio, parámetros y **orden topológico** precalculado. Es determinista (CA-14b) y se guarda por versión (RN-11).
  - **El intérprete solo ejecuta el documento compilado** (AR-17), nunca el diagrama.
  - La ejecución es una **máquina de estados finita** con la tabla de requisitos §5.1 en un único componente (AR-18); toda transición se registra (RN-30).

---

## 2. Ciclo de vida de una ejecución

```
POST /api/eventos
   │  dedup por identificador de evento (CA-11a)
   ▼
Una ejecución Pendiente por cada flujo activo con ese tipo de evento (versión vigente)
   ▼
┌─► Reclamada por el procesador ──► paso según el nodo actual ──► transacción:
│     (SKIP LOCKED + concesión)      · Condición: elige «sí» o «no»     · registra el paso (RN-12)
│                                    · Acción: envío con referencia     · avanza al siguiente nodo
│                                    · Temporizador: programa el vence  · o queda Esperando
│                                                                       · o termina: Completada / Fallida
└──────────── vence la espera (temporizador o reintento) ◄──────────────┘
```

---

## 3. Reglas verificables por máquina (alimentan RH-07)

| ID | Regla | Cómo se verifica |
|---|---|---|
| AR-01 | `Core` no referencia `Infrastructure`, `Api`, EF Core, ASP.NET Core ni `System.Net.Http` | Prueba de arquitectura (NetArchTest) |
| AR-03 | Ninguna llamada a `DateTime.Now`, `DateTime.UtcNow` ni `DateTimeOffset.UtcNow`; el tiempo solo por `TimeProvider` | Analizador de APIs prohibidas |
| AR-04 | Los clientes HTTP hacia terceros solo existen en `Infrastructure` | Prueba de arquitectura |
| AR-05 | Las contraseñas y la clave de integración no se pasan a registros técnicos | Prueba de arquitectura sobre firmas + analizador |
| AR-06 | Toda acción del registro tiene al menos un CA que la ejercita | Prueba sobre el registro |
| AR-07 | Ningún archivo de código supera 400 líneas | Comprobación en CI |
| AR-08 | Todo CA comprometido en `harness/scope.json` tiene una prueba etiquetada; los demás CA se informan como pendientes | Script de trazabilidad (RH-01) |
| AR-09 | `tests/Acceptance/`, `docs/requisitos.md`, la sección 3 de este documento y `harness/scope.json` no cambian sin la aprobación de una persona revisora | Chequeo en CI contra el aprobador del PR (RH-02) |
| AR-16 | Ningún proyecto referencia `Simulator`; el motor lo llama solo por HTTP | Prueba de arquitectura |
| AR-17 | El intérprete recibe únicamente el documento compilado, nunca el diagrama | Prueba de arquitectura sobre la firma |
| AR-18 | Los cambios de estado de una ejecución pasan solo por el componente de la máquina de estados | Prueba de arquitectura y CA-18a |

Los mensajes de error de estas reglas **dicen cómo corregir** el problema, para que el agente lo resuelva sin ayuda.
