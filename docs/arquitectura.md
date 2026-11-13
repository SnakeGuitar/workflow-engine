# Arquitectura: motor de flujos para microempresas (MVP)

| Campo | Valor |
|---|---|
| Versión | 0.6 (propuesta) |
| Fecha | 2026-11-13 |
| Entrada | [requisitos.md](requisitos.md) v0.7 |
| Plazo | 2026-12-02 (20 días) |
| Equipo | 2 personas + agentes de programación |

**Cómo leer este documento.** Cada decisión (`ADR-xx`) indica **qué requisito la motiva**, qué alternativas se descartaron y qué se sacrifica. Las versiones de paquetes y librerías son las que conozco; **verificarlas al instalar** (H-25). Las reglas de la sección 6 son ejecutables y alimentan RH-07.

---

## 1. Fuerzas que moldean la arquitectura

| Fuerza | Origen | Consecuencia |
|---|---|---|
| Esperas que sobreviven a reinicios y no duplican avisos | RF-10, RF-11, RN-06, RNF-04 | Estado de ejecución y temporizadores **persistentes y transaccionales** |
| Verificar esperas de 120 h sin esperar | RNF-08, CA-07a, CA-25b | El tiempo es una **dependencia inyectable** |
| Canal de aviso externo con fallos | RF-09, RF-11b, RNF-08b | El canal es un **puerto** con adaptador real y doble de prueba |
| Motor genérico, negocio intercambiable | Propósito, RF-06 | El motor **no conoce** ningún dominio |
| Adaptarse a cada negocio sin reprogramar | RF-50, RF-51, D-09 | Los procesos son **paquetes declarativos** validados |
| Aprender del material del negocio (EXT) | RF-60 a RF-63, RF-70, D-10 | Ingesta de solo lectura, extracción por agente y **confirmación humana** |
| Salidas no deterministas de los agentes (EXT) | RF-15, RNF-11 | Contrato de paso agente con esquema, topes y **respuestas grabadas** |
| Datos del negocio salen a un tercero (EXT) | RN-20, RNF-10 | Puerto de proveedor de modelo con enmascarado y registro de salida |
| Cumplir las tareas técnicas del enunciado | RF-02, RF-14, RF-16, RF-40, RF-90 | Compilador a DAG, motor asíncrono con reintentos, webhook saliente y simulador de terceros |
| Simular terceros con fallos y restricciones | RF-90 a RF-93, RN-24 a RN-27 | Un simulador como componente del producto que también es el doble de pruebas |
| Extensibilidad y código abierto | RF-80 a RF-82, RN-31, RNF-13 | Contrato público de extensión en un proyecto aparte |
| Concurrencia sobre un mismo caso | RF-13, CA-13b | Control de concurrencia en el cambio de estado |
| Agentes que programan | RH-01 a RH-07 | Estructura **rígida y verificable por máquina**, tecnología conocida y poco exótica |
| 2 personas, 20 días | R-02 | Lo mínimo que satisface los requisitos; nada de infraestructura distribuida |
| Las personas deben defender el código | RH-12, H-20 | Stack que el equipo domina |

---

## 2. Decisiones

### ADR-01 Monolito modular, un solo despliegue
- **Motivo:** el enunciado original sugería microservicios, patrón Saga y bloqueos distribuidos. Ningún requisito del MVP lo exige: todo el estado vive en una base de datos y las transacciones son locales. Con 2 personas y 20 días, los servicios separados añaden fallos de red, contratos y despliegue sin aportar nada medible.
- **Alternativas descartadas:** microservicios (costo sin beneficio); Saga (no hay transacciones distribuidas en el MVP: el "deshacer" no existe en este proceso).
- **Sacrificio:** escalado independiente por componente. Se acepta.
- **Condición de corrección con varias instancias:** el motor reclama trabajo mediante la base de datos (ADR-03), así que dos instancias no ejecutan lo mismo dos veces. **El MVP corre con una instancia** y esto no se prueba en carga.

### ADR-02 Plataforma: .NET + PostgreSQL + React/TypeScript
- **Motivo:** el equipo domina .NET y aprendió arquitectura con esa plataforma (RH-12). C# tiene tipado fuerte, buen soporte de los agentes y herramientas maduras para hacer cumplir reglas de estructura (sección 6).
- **Componentes:**
  - **Backend:** ASP.NET Core, versión LTS vigente (conozco .NET 10 como LTS; verificar).
  - **Datos:** PostgreSQL, acceso con Entity Framework Core y Npgsql.
  - **Interfaz:** React con TypeScript (SPA). Editor de nodos con React Flow (`@xyflow/react`, verificar licencia y versión).
- **Alternativa considerada:** Blazor para tener todo en C#. **Descartada por decisión del equipo (2026-11-13)**, que lo considera poco maduro para este proyecto. Hay librerías de diagramas para Blazor (por ejemplo Z.Blazor.Diagrams), pero no se verificó su encaje con RF-40 ni el desempeño de los agentes con ellas (H-27); se deja constancia por si se revisa más adelante. Como el frontend queda tras el contrato de la API (ADR-08), cambiarlo después es de bajo costo.
- **Sacrificio:** dos lenguajes. La parte de TypeScript es delgada y la acota el contrato de la API (ADR-08).

### ADR-03 Persistencia y ejecución durables sobre PostgreSQL
- **Motivo:** RF-10, RF-11, RF-13, RN-06, RNF-04.
- **Decisión:**
  1. **Una ejecución es una fila** con su versión de flujo, caso, nodo actual, estado y contador de intentos. Cada paso se registra en una tabla **solo de inserción** (RN-12).
  2. **Cada avance es una transacción:** actualizar la ejecución, insertar el paso y programar lo siguiente (temporizador o aviso pendiente) se confirma o se revierte en bloque.
  3. **Temporizadores como filas con instante de vencimiento.** Un procesador en segundo plano reclama las vencidas con bloqueo de filas (`FOR UPDATE SKIP LOCKED`) y con una concesión que expira, de modo que una caída a mitad de paso deja el trabajo disponible para reintentarse (CA-10b).
  4. **Entrega de avisos con referencia única** guardada en la misma transacción que decide enviarlo (patrón de bandeja de salida). Tras una caída se reenvía con la **misma referencia** y el canal descarta duplicados (CA-10c, CA-11b). Es **entrega al menos una vez más idempotencia**, no "exactamente una vez" mágico.
  5. **Deduplicación de eventos** con restricción única sobre el identificador del evento de origen (CA-11a).
  6. **Orden por caso:** el cambio de estado bloquea la fila del caso y usa un número de versión; el segundo cambio incompatible se rechaza (CA-13b).
- **Sustituye a:** Redis y bloqueos distribuidos del enunciado. Un solo almacén transaccional basta y evita la inconsistencia entre dos almacenes.
- **Sacrificio:** el procesador consulta la base periódicamente; la precisión del temporizador depende de ese intervalo (RNF-05: ≤ 10 s en p95, intervalo propuesto 1 s).

### ADR-04 El tiempo es una dependencia
- **Motivo:** RNF-08, CA-07, CA-08, CA-25.
- **Decisión:** todo el código obtiene la hora por `TimeProvider` (abstracción estándar de .NET); las pruebas usan el reloj falso (`FakeTimeProvider`, paquete `Microsoft.Extensions.TimeProvider.Testing`). El cálculo de la ventana horaria (RN-08) usa la zona del negocio y se prueba con ese reloj.
- **Se aplica mecánicamente:** AR-03.

### ADR-05 Canales como puertos; simulados en el MVP, reales como plugins
- **Motivo:** RF-31, RF-35, RF-90 a RF-92, RN-24 a RN-28, D-15.
- **Decisión:**
  - `Sdk` define `IChannel` (enviar con referencia única, resultado explícito con estado de entrega) y `IChannelPolicy` (decisión pura y sin red: ¿se puede enviar y, si no, por qué?).
  - **Correo, SMS y WhatsApp** del MVP son adaptadores que llaman por HTTP al **simulador** (ADR-15) como a cualquier tercero. El adaptador de correo puede apuntar además a un servidor SMTP real (opcional).
  - La **política de WhatsApp** aplica RN-24 a RN-27 antes de cada envío: consentimiento, ventana de conversación, plantilla aprobada y tope mensual. Si bloquea, el aviso queda con el motivo y se crea una entrada en la bandeja.
  - **EXT:** adaptador de la API oficial de WhatsApp (mismo contrato, activado por configuración), Telegram con sondeo para la vinculación, y aviso asistido.
- **Sacrificio:** las restricciones modeladas son mi lectura de las reglas de la plataforma (H-40); un adaptador real podría revelar diferencias.

### ADR-06 Los flujos son datos validados, y el motor los interpreta
- **Motivo:** RF-01 a RF-03, RN-10, RN-11, D-01.
- **Decisión:**
  - Un flujo es un **documento JSON** (nodos, parámetros, conexiones) guardado por **versión inmutable**.
  - Un **validador puro**, sin acceso a datos, implementa RN-10 y devuelve todos los errores (CA-02g).
  - Un **intérprete** ejecuta una versión nodo a nodo. Los tipos de nodo y las acciones del catálogo se registran en un **registro único y cerrado** (RF-06): un flujo con una acción desconocida no se valida.
  - La cancelación por cambio de estado (RN-05) se modela con el parámetro `cancelarAlSalirDelEstado` del nodo Evento, activo por defecto.
- **Descartado:** ejecutar flujos como código generado (no se puede validar ni auditar tan bien).

### ADR-07 Capas rígidas; los dominios son paquetes de datos
- **Motivo:** el motor debe ser genérico (propósito), el sistema debe adaptarse a cada negocio sin reprogramarse (RF-50, D-09) y las reglas deben poder verificarse por máquina (RH-07).
- **Decisión:** módulos con dependencias de un solo sentido (más `Sdk` y `Simulator`, ADR-17 y ADR-15):
  - `Core`: motor (flujos, validación, ejecución), contrato de paquete, contrato de paso agente y puertos (canal, proveedor de modelo, reloj). **No conoce ningún dominio.**
  - `Cases`: casos con campos comunes y propios, aplicación de los paquetes, bandeja, línea de tiempo y los manejadores de las acciones del catálogo. Depende solo de `Core`.
  - `Discovery` (EXT): fuentes, ingesta, extracción, propuestas y su confirmación. Depende de `Core` y `Cases`.
  - `Infrastructure`: PostgreSQL, procesador de temporizadores, adaptadores de canal, lectores de fuentes y adaptador del proveedor de modelo. Implementa los puertos de `Core` y usa los tipos de `Cases` y `Discovery` para persistirlos.
  - `Api`: HTTP, autenticación y composición.
- **Dependencias permitidas:**

```
Api ──► Discovery ──► Cases ──► Core
 │                              ▲
 └──► Infrastructure ───────────┘   (también depende de Cases y Discovery)
```

- **Estructura de la solución:**

```
src/
  WorkflowEngine.Core/
  WorkflowEngine.Cases/
  WorkflowEngine.Discovery/
  WorkflowEngine.Infrastructure/
  WorkflowEngine.Api/
  WorkflowEngine.Sdk/              contratos públicos de extensión (ADR-17)
  WorkflowEngine.Simulator/       simulador de APIs de terceros (ADR-15)
  web/                            React + TypeScript
packs/
  reparaciones.json               paquete de referencia (datos, no código)
plugins/                          plugins de ejemplo
tests/
  Acceptance/                     un test por CA, etiquetado con su ID (contrato)
  Unit/
  Architecture/                   reglas de la sección 6
  Fixtures/                       corpus sintético y respuestas grabadas (protegidos)
  Evaluation/                     evaluación contra el corpus con modelo real (aparte)
docs/
  requisitos.md  arquitectura.md
AGENTS.md                         índice corto que remite a docs/
```

### ADR-08 API HTTP con contrato y sesión por cookie
- **Motivo:** RF-30, RNF-07, y que los agentes no desalineen frontend y backend.
- **Decisión:** API REST con JSON y especificación OpenAPI; el cliente TypeScript se **genera** a partir de ella. Autenticación y roles (Dueño, Operador) con ASP.NET Core Identity y cookie de sesión; el bloqueo tras 5 intentos (CA-30c) viene incluido.
- **Sacrificio:** la cookie exige que la interfaz y la API compartan origen. Se acepta.

### ADR-09 Estrategia de pruebas y arnés
- **Motivo:** RH-01 a RH-06.
- **Decisión:**
  - **Pruebas de aceptación** contra la aplicación completa en memoria (`WebApplicationFactory`), con PostgreSQL real de prueba (Testcontainers o una base dedicada), reloj falso y doble de canal.
  - Cada prueba lleva un atributo con su ID (por ejemplo `[Ac("CA-08a")]`). Un script cruza esos IDs con `docs/requisitos.md` y calcula la cobertura de CA (RH-01).
  - **Archivos protegidos (RH-02):** `tests/Acceptance/` y `docs/requisitos.md`. Un chequeo en CI falla si un cambio los toca sin un marcador de aprobación humana en el commit (por ejemplo la línea `Approved-By:`).
  - Los mensajes de fallo incluyen el ID del CA y qué se esperaba (RH-06).
  - Las pruebas de la interfaz (CA-40, CA-N02) usan un navegador automatizado (Playwright).
  - Las pruebas que involucran agentes usan **respuestas grabadas** de `tests/Fixtures/` (RH-17, CA-N11a); la evaluación con modelo real vive en `tests/Evaluation/` y no bloquea la suite (CA-N11b).
- **Registro del experimento (RH-04):** cada tarea de agente escribe una línea en un archivo de registro versionado con modelo, tarea, uso, iteraciones y resultado.

### ADR-10 Registros técnicos sin datos personales
- **Motivo:** RNF-07.
- **Decisión:** el historial de ejecuciones (RN-12) es **dato del dominio**, no un log. Los logs técnicos usan registro estructurado con un identificador de correlación por ejecución. Los datos de contacto y credenciales se modelan con tipos específicos que **no se serializan** en logs (AR-05).

### ADR-11 Paquetes de proceso declarativos (plugins de datos)
- **Motivo:** RF-50, RF-51, RN-21, RN-22, D-09, N-07.
- **Decisión:**
  - Un **paquete** es un documento versionado, inmutable por versión: estados (inicial y terminales), transiciones, campos propios (tipo y obligatoriedad), plantillas de aviso y flujos incluidos. El de «Reparaciones» vive en `packs/` como dato.
  - Un **validador de paquetes** puro (RN-21) reutiliza el validador de flujos (RN-10) para los flujos incluidos.
  - `Cases` aplica la tabla de transiciones y valida los campos propios. Ningún código nombra un estado concreto (AR-10).
  - Un caso se guarda con **columnas comunes** (folio, nombre, contacto, canal, estado, fecha, versión de paquete) y un **documento JSON** para los campos propios, validado contra el esquema del paquete.
  - Los **plugins de código** (acciones, canales, lectores) se describen en ADR-17.
  - La **suite de conformidad** (CA-50d, CA-50e) ejecuta las pruebas de transiciones sobre cualquier paquete.
- **Descartado:** paquetes como código; generar código desde propuestas de agentes (D-11).
- **Sacrificio:** solo los campos comunes son buscables de forma eficiente (CA-51a); buscar por campo propio es posterior.

### ADR-12 (EXT) El descubrimiento se modela como flujos con pasos de agente
- **Motivo:** RF-15, RF-60 a RF-63, RF-70 a RF-72, RN-15 a RN-19, D-10, D-11.
- **Decisión:**
  - **Ingesta de solo lectura:** un lector por tipo de fuente (carpeta de texto, hoja de cálculo; después calendario e imágenes) produce un **artefacto normalizado**: texto, metadatos y huella de contenido. La huella da idempotencia (RN-19). Ningún lector tiene operaciones de escritura (AR-11).
  - Al registrar un artefacto nuevo se emite el evento «artefacto nuevo» (CA-61e), que dispara un **flujo del propio motor** con el paso agente «Extraer casos». Así el descubrimiento reutiliza persistencia, reintentos, tope de costo e historial ya construidos.
  - **Contrato del paso agente** (en `Core`): entrada tipada, esquema de salida, validación estricta, hasta 2 reintentos, tope de costo por ejecución y registro de modelo, consumo y duración. Una salida inválida es un fallo y **nunca produce efectos parciales** (CA-15a).
  - **Evidencia:** cada candidato y cada propuesta guarda referencia al artefacto y al fragmento (posición o celda) de origen (RN-16).
  - **Activación solo con confirmación:** las operaciones de crear un caso desde un candidato y de activar un paquete exigen un valor `AprobacionHumana` que solo la capa `Api` puede construir (AR-14).
- **Ventaja:** el descubrimiento demuestra el motor. **Riesgo:** acopla el descubrimiento al calendario del motor; por eso queda en EXT (D-14).

### ADR-13 (EXT) Proveedor de modelo como puerto, con doble y política de salida
- **Motivo:** RN-20, RNF-10, RNF-11, RH-14, RH-17.
- **Decisión:**
  - `Core` define `IModelProvider`. `Infrastructure` tiene un adaptador real (el proveedor se elige en OQ-10) y un **doble** que sirve respuestas grabadas.
  - Un **filtro de salida** enmascara teléfonos y correos **antes** de que un adaptador real reciba el texto, y el **registro de salida** guarda qué se envió (CA-N10c). El adaptador real no se puede construir sin ese filtro (AR-12). Las fuentes deshabilitadas no llegan al puerto (CA-60b).
  - La interfaz admite sustituir el proveedor por un **modelo local** para evitar la salida de datos; no se evalúa en el MVP.
- **Sacrificio declarado:** el enmascarado por patrones **no detecta datos personales en texto libre** (por ejemplo nombres propios). Por eso cada fuente la habilita explícitamente el dueño (H-29) y se informa de este límite.

### ADR-14 (EXT) Evaluación de agentes sobre un corpus
- **Motivo:** RH-15, RH-16, RNF-11.
- **Decisión:** `tests/Fixtures/` contiene el corpus sintético de los tres locales (artefactos y resultado esperado). `tests/Evaluation/` ejecuta la extracción y la propuesta con un modelo real, calcula precisión y exhaustividad y las registra en cada punto de medición (RH-10). Es información para el experimento, **no una compuerta** del arnés, porque la salida de un modelo real varía.

### ADR-15 Simulador de APIs de terceros
- **Motivo:** el enunciado pide simular APIs de terceros; RF-90 a RF-93, RN-24 a RN-28, RNF-08, D-15, D-16.
- **Decisión:** `WorkflowEngine.Simulator` es una aplicación web **separada** dentro de la solución. No es un servicio del sistema: representa al mundo exterior. El motor la llama por HTTP como llamaría a cualquier tercero, lo que además demuestra el webhook saliente.
  - Endpoints que imitan **correo, SMS, WhatsApp** y un **receptor HTTP POST**; registra lo recibido, lo muestra en una interfaz y deduplica por referencia (CA-90c).
  - **Inyección de fallos** por canal, desde su interfaz o por una API de administración: error, latencia, duplicado, respuesta tardía y caída (RF-91).
  - El simulador de WhatsApp aplica **ventana de conversación, plantilla aprobada y consentimiento**, calcula categoría y costo con tarifas configurables, informa estados de entrega con retraso y puede simular mensajes entrantes del cliente (RF-92, RF-93).
  - **Un solo componente, dos papeles:** demostración (se ven llegar los mensajes) y **doble de pruebas** del arnés, alojado en memoria con el reloj falso. Así no hay dos dobles que diverjan.
- **Límite declarado:** no es WhatsApp, es un modelo de sus reglas (H-40). Su interfaz lo indica (CA-90f).
- **Descartado:** un doble solo para pruebas y nada para la demostración (el enunciado pide el simulador).

### ADR-16 Compilación a DAG y máquina de estados de la ejecución
- **Motivo:** RF-02, RF-14, RF-18, RN-10, RN-30; tareas del enunciado.
- **Decisión:**
  - El editor guarda el **diagrama** (posiciones, nodos y conexiones). Un **compilador puro** lo valida (RN-10) y produce un **documento JSON de DAG ejecutable**: nodos con identificador estable, aristas, nodo de inicio, parámetros y **orden topológico** precalculado. Es determinista (CA-14b) y se guarda por versión (RN-11).
  - **El intérprete solo ejecuta el documento compilado** (AR-17), nunca el diagrama.
  - La ejecución es una **máquina de estados finita** con la tabla de la sección 6.1 de requisitos en un único componente (AR-18); toda transición se registra (RN-30).
- **Saga no se usa** (ADR-18).

### ADR-17 Extensión por plugins
- **Motivo:** RF-80 a RF-82, RN-31, RNF-13, D-18.
- **Decisión:**
  - `WorkflowEngine.Sdk` contiene los contratos públicos (`IAction`, `IChannel`, `IChannelPolicy`, `ISourceReader`, metadatos y versión del contrato) y **no referencia nada**. Un plugin referencia **solo** `Sdk` (AR-15).
  - Cada plugin declara su nombre, la versión del contrato y el **esquema de sus parámetros**; el validador de flujos y la paleta del editor los usan (CA-80b).
  - Un plugin con versión de contrato incompatible no se registra (CA-80c). Una excepción en un plugin se captura **por paso** y falla ese paso, no el motor (CA-80d).
  - **MVP:** el plugin de ejemplo se registra por referencia en la composición de `Api`. **EXT:** carga desde la carpeta `plugins/`.
- **Límite declarado:** los plugins son **código de confianza**, sin aislamiento de seguridad. Un modelo seguro para plugins de terceros es otro proyecto.

### ADR-18 Qué se adopta y qué no de la arquitectura recomendada en el enunciado
- **Motivo:** D-17. El enunciado presenta esta arquitectura como **recomendada**, no como requisito.

| Recomendado | Decisión | Razón |
|---|---|---|
| Microservicios | Monolito modular con dependencias estrictas; el simulador ya es un proceso aparte | Ningún requisito del MVP exige servicios separados; 2 personas y 20 días (ADR-01) |
| Máquina de estados finita | **Sí** (ADR-16) | RN-30, RF-18 |
| Patrón Saga | No | No hay transacciones distribuidas: el estado vive en una base de datos |
| PostgreSQL | **Sí** (ADR-03) | Transacciones, bloqueo de filas, reanudación |
| Redis para bloqueos distribuidos | No | El bloqueo de filas de PostgreSQL basta y evita dos almacenes inconsistentes (ADR-03) |
| JSON ejecutable como DAG | **Sí** (ADR-16) | RF-14 |

- **Riesgo menor:** la rúbrica podría valorar la arquitectura recomendada aunque no la exija (RI-13). **Si se pidiera:** separar el procesador de temporizadores y ejecuciones como un proceso aparte que comparte la base de datos, sin cambiar el diseño.

---

## 3. Ciclo de vida de una ejecución

```
Evento (caso cambia de estado)
   │  dedup por identificador de evento (CA-11a)
   ▼
Ejecución creada (versión vigente del flujo)
   ▼
┌─► Reclamada por el procesador ──► paso ──► transacción:
│        (concesión con expiración)          · registra paso (RN-12)
│                                            · programa siguiente: temporizador / aviso pendiente
│                                            · o termina (completada, con fallo, cancelada)
└─────────── vence temporizador ◄────────────┘

Cambio de estado del caso ⇒ cancela ejecuciones con cancelarAlSalirDelEstado (RN-05)
Aviso pendiente ⇒ se entrega con referencia única; fallo ⇒ reintento con espera (RN-09)
```

### Ciclo del descubrimiento (EXT)

```
Fuente (carpeta de texto, hoja de cálculo; después calendario e imágenes)
   │  lector de solo lectura + huella de contenido (RN-18, RN-19)
   ▼
Artefacto registrado ──► evento «artefacto nuevo»
   ▼
Flujo del motor con paso agente «Extraer casos»
   │  esquema validado · hasta 2 reintentos · tope de costo (RF-15)
   ▼
Candidatos con confianza y evidencia
   ├─ confianza ≥ 0,7 ─► confirmación en lote ──┐
   └─ confianza  < 0,7 ─► revisión individual ──┤
                                                 ▼
                    Confirmación humana (RN-15) ─► caso en el paquete activo

Casos confirmados ─► paso agente «Proponer paquete» ─► validador de paquetes (RN-21)
   ─► propuesta con evidencia ─► edición y confirmación humana ─► nueva versión de paquete (RN-22)
```

---

## 4. Cómo se cubre cada requisito

| Requisito | Dónde se resuelve |
|---|---|
| RF-01 a RF-03, RN-10, RN-11 | ADR-06 (validador puro, versiones inmutables) |
| RF-04, RF-05, RF-06 | ADR-06 (intérprete, registro cerrado de acciones) |
| RF-07, RN-08 | ADR-03 (temporizadores) y ADR-04 (reloj, zona del negocio) |
| RF-08, RN-05 | ADR-06 (`cancelarAlSalirDelEstado`) |
| RF-09, RN-09 | ADR-03 (intentos y esperas persistidos) |
| RF-10, RF-11, RN-06, RNF-04 | ADR-03 (transacciones, concesiones, referencia única, restricciones únicas) |
| RF-12, RN-12, RNF-06 | ADR-03 (tabla de pasos solo de inserción) |
| RF-13, RN-01 | ADR-03 (bloqueo y versión del caso) |
| RF-20 a RF-29, RF-31, RF-32 | Paquete «Reparaciones» (ADR-11) sobre el módulo `Cases` (ADR-07) |
| RF-50, RF-51, RN-21, RN-22 | ADR-11 |
| RF-15 | ADR-12 (contrato del paso agente) |
| RF-60 a RF-63, RF-70 a RF-72, RN-15 a RN-19, RN-23 | ADR-12 |
| RN-20, RNF-10, RNF-12 | ADR-13 |
| RNF-11, RH-15 a RH-17 | ADR-09, ADR-13, ADR-14 |
| RF-02, RF-14, RF-18, RN-10, RN-30 | ADR-16 |
| RF-16, RN-29 | ADR-15 (receptor simulado) y acción webhook en `Infrastructure` |
| RF-31, RF-33, RF-35, RN-24 a RN-28 | ADR-05 (canales y política de WhatsApp), ADR-15 |
| RF-90 a RF-93 | ADR-15 |
| RF-80 a RF-82, RN-31, RNF-13 | ADR-17 |
| D-17 | ADR-18 |
| RF-30, RNF-07 | ADR-08, ADR-10 |
| RF-40 a RF-42 | `web/` (React Flow) y API de flujos |
| RNF-02 | `web/`, diseño móvil primero |
| RNF-08, RH-01 a RH-06 | ADR-04, ADR-05, ADR-09 |

---

## 5. Riesgos de arquitectura

| ID | Riesgo | Mitigación |
|---|---|---|
| AR-R1 | El equipo no domina React; el editor es lo más nuevo para ustedes | Editor mínimo (D-06); contrato OpenAPI; pruebas de interfaz; los agentes escriben el frontend y el equipo lo revisa contra los CA |
| AR-R2 | Concurrencia y reanudación son difíciles de acertar | Pruebas CA-10, CA-13 y CA-N04a desde el primer día, antes que los casos de reparación |
| AR-R3 | "Exactamente una vez" no existe entre sistemas | ADR-03 lo declara: al menos una vez más idempotencia; el requisito de referencia única se vuelve explícito para el canal |
| AR-R4 | Los agentes violan la estructura sin notarlo | Sección 6, reglas automáticas con mensajes de remediación |
| AR-R5 | El plazo es corto | Ver sección 7; el orden prioriza el motor y el arnés |
| AR-R6 | La vinculación de Telegram introduce un flujo de entrada (sondeo) y estados nuevos del caso | Aislarlo en `Infrastructure`; implementarlo después del correo; es lo primero que se recorta (D-08) |
| AR-R7 | El descubrimiento (EXT) podría reclamar tiempo del MVP | Está fuera del MVP (D-14); el núcleo no depende de `Discovery` |
| AR-R8 | Salidas no deterministas de los agentes | Esquema validado, respuestas grabadas, confirmación humana, evaluación aparte (ADR-12 a ADR-14) |
| AR-R9 | Salida de datos del negocio y de sus clientes a un tercero | ADR-13: habilitación por fuente, enmascarado, registro de salida; límite declarado con el texto libre |
| AR-R10 | Los campos propios en JSON limitan las consultas | Solo los campos comunes son buscables en el MVP (ADR-11) |
| AR-R11 | El simulador diverge de la plataforma real, sobre todo en WhatsApp | Se declara como modelo (H-40, CA-90f); el adaptador real es EXT y se validaría contra la plataforma |
| AR-R12 | Los plugins corren sin aislamiento | Se declara como código de confianza (ADR-17); un modelo seguro queda fuera de alcance |
| AR-R13 | La rúbrica podría valorar la arquitectura recomendada | ADR-18 justifica lo que se adopta y lo que no; alternativa de menor costo documentada |

---

## 6. Reglas verificables por máquina (alimentan RH-07)

| ID | Regla | Cómo se verifica |
|---|---|---|
| AR-01 | `Core` no referencia `Cases`, `Discovery`, `Infrastructure` ni `Api` | Prueba de arquitectura (por ejemplo NetArchTest) |
| AR-02 | `Cases` solo referencia `Core`; `Discovery` solo referencia `Core` y `Cases` | Idem |
| AR-03 | Ninguna llamada a `DateTime.Now`, `DateTime.UtcNow` ni `DateTimeOffset.UtcNow`; el tiempo solo por `TimeProvider` | Analizador de APIs prohibidas |
| AR-04 | Los clientes HTTP y SDK de canales solo existen en `Infrastructure` | Prueba de arquitectura |
| AR-05 | Los tipos de datos personales y credenciales no se pasan a registros técnicos | Prueba de arquitectura sobre firmas + analizador |
| AR-06 | Toda acción del catálogo está en el registro único y tiene al menos un CA que la ejercita | Prueba sobre el registro |
| AR-07 | Ningún archivo de código supera 400 líneas | Comprobación en CI |
| AR-08 | Todo CA de prioridad M tiene una prueba etiquetada | Script de trazabilidad (RH-01) |
| AR-09 | `tests/Acceptance/`, `tests/Fixtures/` y `docs/requisitos.md` no cambian sin aprobación humana | Chequeo en CI (RH-02, RH-15, RH-17) |
| AR-10 | Los estados y transiciones salen solo del paquete activo: ningún código de `Cases`, `Discovery` ni `Api` nombra un estado concreto de un paquete | Prueba de arquitectura y búsqueda de literales; los paquetes viven en `packs/` |
| AR-11 | Los lectores de fuentes no tienen operaciones de escritura y la ingesta deja los archivos con la misma huella | Prueba de arquitectura y CA-61a |
| AR-12 | Solo `Infrastructure` referencia clientes de modelos, y el adaptador real exige el filtro de enmascarado | Prueba de arquitectura y prueba de construcción |
| AR-13 | Todo paso de agente declara esquema de salida, tope de costo y límite de reintentos | Prueba sobre el registro de acciones |
| AR-14 | Crear un caso desde un candidato o activar un paquete exige un valor `AprobacionHumana` que solo `Api` puede construir | Prueba de arquitectura sobre el constructor |
| AR-15 | `Sdk` no referencia ningún otro proyecto y los plugins solo referencian `Sdk` | Prueba de arquitectura |
| AR-16 | Ningún proyecto de producción referencia `Simulator`; el motor lo llama solo por HTTP | Prueba de arquitectura |
| AR-17 | El intérprete recibe únicamente el documento compilado, nunca el diagrama | Prueba de arquitectura sobre la firma |
| AR-18 | Los cambios de estado de una ejecución pasan solo por el componente de la máquina de estados | Prueba de arquitectura y CA-18a |

Los mensajes de error de estas reglas **dicen cómo corregir** el problema, para que el agente lo resuelva sin ayuda.

---

## 7. Orden de construcción y calendario (estimación)

Es una estimación mía, no un compromiso; la validación real llega al primer hito.

| Fechas | Hito | Mide (RH-10) |
|---|---|---|
| 13 a 15 nov | Esqueleto y **arnés**: proyectos `Core`, `Cases`, `Sdk`, `Infrastructure`, `Api` y `Simulator`; reglas AR; reloj falso; simulador mínimo (correo y HTTP POST) con inyección de fallos; etiquetado de CA; protección de archivos; registro de tareas; `AGENTS.md` | Línea base |
| 16 a 21 nov | Analizador y compilador a DAG (RF-02, RF-14), motor asíncrono (RF-03 a RF-13, RF-18) y acción webhook (RF-16) | 25 % de los RF de prioridad M |
| 22 a 25 nov | Casos y paquete «Reparaciones» (RF-20 a RF-31, RF-50), canales Correo, SMS y WhatsApp con restricciones sobre el simulador (RF-35, RF-90 a RF-92), API y autenticación | 50 % |
| 26 a 29 nov | Editor visual (RF-40, RF-41), vista del dueño (RNF-01, RNF-02), estados de entrega y aprobación de plantillas (RF-33, RF-93), plugin de ejemplo y preparación para open source (RF-80 a RF-82, RNF-13) | 100 % |
| 30 nov a 2 dic | Cambios de requisito controlados (RH-11, RH-18), cierre de métricas y documento final | Resultados |

**Advertencia:** estimación optimista. Si algo debe ceder, se recorta según la escalera de la sección 2 de requisitos.
---

## 8. Decisiones abiertas

| ID | Pregunta |
|---|---|
| AD-01 | **Resuelta:** frontend en React con TypeScript y editor con React Flow (decisión del equipo, 2026-11-13). |
| AD-02 | **Resuelta:** ambos canales, correo como base y Telegram recortable (D-05, D-08). Falta elegir el servicio SMTP concreto. |
| AD-03 | ¿Qué valora la rúbrica del profesor? La arquitectura del enunciado es una recomendación, no una exigencia. (OQ-07) |
| AD-04 | ¿Dónde se ejecutará la demostración final (máquina local o servidor)? |
| AD-05 | ¿Qué proveedor de modelo y con qué presupuesto? (OQ-10) |
| AD-06 | ¿Se evalúa un modelo local para privacidad? No en el MVP; la interfaz lo permite (ADR-13). |
| AD-07 | ¿Qué licencia de código abierto? (OQ-15) |

---

## 9. Historial de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-11-12 | Propuesta inicial |
| 0.2 | 2026-11-13 | AD-01 resuelta: frontend en React. Se documenta el descarte de Blazor y se ajusta AR-R1. |
| 0.3 | 2026-11-13 | AD-02 resuelta: canales Correo y Telegram. ADR-05 con tres adaptadores, sondeo para la vinculación y nuevo riesgo AR-R6. |
| 0.4 | 2026-11-13 | Hacia la visión de adaptación a cada negocio: `Repairs` pasa a ser `Cases` más un paquete de datos; nuevo módulo `Discovery`; ADR-11 a ADR-14; AR-10 a AR-14; ciclo del descubrimiento; calendario con punto de decisión el 25 de noviembre; AR-R7 a AR-R10; AD-05 y AD-06. |
| 0.5 | 2026-11-13 | Alineación con el enunciado y con WhatsApp: ADR-05 reescrita (canales simulados con política de WhatsApp), ADR-15 (simulador), ADR-16 (compilación a DAG y máquina de estados), ADR-17 (plugins), ADR-18 (desviaciones del enunciado), AR-15 a AR-18, AR-R11 a AR-R13, calendario revisado, el descubrimiento pasa a EXT. |
| 0.6 | 2026-11-13 | La arquitectura del enunciado se trata como recomendación, no como exigencia: se ajustan ADR-18, AD-03 y AR-R13. |
