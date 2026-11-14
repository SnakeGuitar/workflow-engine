# Arquitectura: motor de flujos para microempresas (MVP)

| Campo | Valor |
|---|---|
| Versión | 0.11 |
| Entrada | [requisitos.md](requisitos.md) v0.13 |
| Razones, alternativas y riesgos | [humanos/](humanos/) (no necesario para implementar) |

Cada `ADR-xx` fija una decisión vigente y los requisitos que la motivan. Las versiones de paquetes son las que conozco: **verificarlas al instalar**. Las reglas de la sección 3 son ejecutables y alimentan RH-07.

---

## 1. Decisiones

### ADR-01 Monolito modular, un solo despliegue
- **Condición de corrección con varias instancias:** el motor reclama trabajo mediante la base de datos (ADR-03), así que dos instancias no ejecutan lo mismo dos veces. **El MVP corre con una instancia** y esto no se prueba en carga.

### ADR-02 Plataforma: .NET + PostgreSQL + React/TypeScript
- **Componentes:**
  - **Backend:** ASP.NET Core, versión LTS vigente (conozco .NET 10 como LTS; verificar).
  - **Datos:** PostgreSQL, acceso con Entity Framework Core y Npgsql.
  - **Interfaz:** React con TypeScript (SPA). Editor de nodos con React Flow (`@xyflow/react`, verificar licencia y versión).

### ADR-03 Persistencia y ejecución durables sobre PostgreSQL
- **Motivo:** RF-10, RF-11, RF-13, RF-17, RN-06, RN-33, RNF-04.
- **Decisión:**
  1. **Una ejecución es una fila** con su versión de flujo, caso, nodo actual, estado y contador de intentos. Cada paso se registra en una tabla **solo de inserción** (RN-12).
  2. **Cada avance es una transacción:** actualizar la ejecución, insertar el paso y programar lo siguiente (temporizador o aviso pendiente) se confirma o se revierte en bloque.
  3. **Temporizadores como filas con instante de vencimiento.** Un procesador en segundo plano reclama las vencidas con bloqueo de filas (`FOR UPDATE SKIP LOCKED`) y con una concesión que expira, de modo que una caída a mitad de paso deja el trabajo disponible para reintentarse (CA-10b).
  4. **Entrega de avisos con referencia única** guardada en la misma transacción que decide enviarlo (patrón de bandeja de salida). Tras una caída se reenvía con la **misma referencia** y el canal descarta duplicados (CA-10c, CA-11b). Es **entrega al menos una vez más idempotencia**, no "exactamente una vez" mágico.
  5. **Deduplicación de eventos** con restricción única sobre el identificador del evento de origen (CA-11a).
  6. **Orden por caso:** el cambio de estado bloquea la fila del caso y usa un número de versión; el segundo cambio incompatible se rechaza (CA-13b).
  7. **Temporizadores anclados:** la fila guarda el campo ancla y el desplazamiento. Cuando `Cases` cambia el campo, la misma transacción recalcula el vencimiento de las filas pendientes del caso (RN-33). La referencia única del aviso incluye la fecha ancla vigente, así el recordatorio de una fecha nueva es otro aviso.
- **Límite:** el procesador consulta la base periódicamente; la precisión del temporizador depende de ese intervalo (RNF-05: ≤ 10 s en p95, intervalo propuesto 1 s).

### ADR-04 El tiempo es una dependencia
- **Motivo:** RNF-08, CA-07, CA-08, CA-25.
- **Decisión:** todo el código obtiene la hora por `TimeProvider` (abstracción estándar de .NET); las pruebas usan el reloj falso (`FakeTimeProvider`, paquete `Microsoft.Extensions.TimeProvider.Testing`). El cálculo de la ventana horaria (RN-08) usa la zona del negocio y se prueba con ese reloj.
  - Los instantes se guardan en UTC. La zona del negocio es un identificador IANA; la ventana horaria y los temporizadores anclados se convierten a UTC con sus reglas de cambio de horario. Una duración fija se suma en tiempo transcurrido (24 h son 24 h aunque cambie el horario).
- **Se aplica mecánicamente:** AR-03.

### ADR-05 Canales como puertos; simulados en el MVP, reales como plugins
- **Motivo:** RF-31, RF-35 a RF-37, RF-90 a RF-92, RF-94, RN-24 a RN-28, RN-34 a RN-36.
- **Decisión:**
  - `Sdk` define `IChannel` (enviar con referencia única, resultado explícito con estado de entrega) y `IChannelPolicy` (decisión pura y sin red: ¿se puede enviar y, si no, por qué?).
  - Un canal con respuesta además implementa `IInboundChannel`: recibe mensajes entrantes por un punto de entrada HTTP de `Api` (ADR-19).
  - **Correo, SMS, WhatsApp y Messenger** del MVP son adaptadores que llaman por HTTP al **simulador** (ADR-15) como a cualquier tercero. El adaptador de correo puede apuntar además a un servidor SMTP real (opcional).
  - La **política de WhatsApp** aplica RN-24 a RN-27 antes de cada envío: consentimiento, ventana de conversación, plantilla aprobada y tope mensual. Si bloquea, el aviso queda con el motivo y se crea una entrada en la bandeja.
  - La **política de Messenger** aplica RN-36 (ventana de 24 h y consentimiento al iniciar la conversación). No hay plantillas ni tope de costo.
  - **EXT:** adaptadores de las APIs oficiales de WhatsApp y Messenger (mismo contrato, activados por configuración), Telegram con sondeo para la vinculación, y aviso asistido.

### ADR-06 Los flujos son datos validados, y el motor los interpreta
- **Motivo:** RF-01 a RF-03, RF-17, RF-36, RN-10, RN-11, RN-32.
- **Decisión:**
  - Un flujo es un **documento JSON** (nodos, parámetros, conexiones) guardado por **versión inmutable**.
  - Un **validador puro**, sin acceso a datos, implementa RN-10 y devuelve todos los errores (CA-02g).
  - Un **intérprete** ejecuta una versión nodo a nodo. Los tipos de nodo y las acciones del catálogo se registran en un **registro único y cerrado** (RF-06): un flujo con una acción desconocida no se valida.
  - El nodo **Temporizador** tiene dos modos: duración fija, o anclado (campo ancla del paquete, desplazamiento y sentido). El validador comprueba el campo contra el paquete activo (CA-17e).
  - El nodo **Evento** admite «el caso entra a uno de estos estados» (uno o varios, RF-25) y «el cliente responde». La Condición puede leer el texto de la respuesta, normalizado (RN-34).
  - La acción **Notificar al dueño** admite opciones: destinos de estado entre los que el dueño elige desde la bandeja (RF-28, RN-38). Elegir una opción es un cambio de estado hecho por una persona (RF-21).
  - Un nodo que no es Condición tiene como máximo una salida (RN-10), así que una ejecución tiene un único nodo actual (ADR-03).
  - La cancelación por cambio de estado (RN-05) se modela con el parámetro `cancelarAlSalirDelEstado` del nodo Evento, activo por defecto; en falso, la ejecución solo se cancela cuando el caso llega a un estado terminal. La ejecución cuyo paso origina el cambio no se cancela por él y continúa con el nodo siguiente.
  - Al crear una ejecución de un flujo para un caso, la que siga en curso del mismo flujo y caso se cancela con el motivo «reemplazada» (RN-05).

### ADR-07 Capas rígidas; los dominios son paquetes de datos
- **Decisión:** módulos con dependencias de un solo sentido (más `Sdk` y `Simulator`, ADR-17 y ADR-15):
  - `Core`: motor (flujos, validación, ejecución), contrato de paquete, contrato de paso agente y puertos (proveedor de modelo, reloj y unidad de trabajo, ADR-20); los contratos de canal los toma de `Sdk` (ADR-05). **No conoce ningún dominio.**
  - `Cases`: casos con campos comunes y propios, aplicación de los paquetes, bandeja, línea de tiempo, mensajes entrantes, consultas y los manejadores de las acciones del catálogo. Depende solo de `Core`.
  - `Discovery` (EXT): fuentes, ingesta, extracción, propuestas y su confirmación. Depende de `Core` y `Cases`.
  - `Infrastructure`: PostgreSQL, procesador de temporizadores, adaptadores de canal, lectores de fuentes y adaptador del proveedor de modelo. Implementa los puertos de `Core` y usa los tipos de `Cases` y `Discovery` para persistirlos.
  - `Api`: HTTP, autenticación y composición. Referencia `Cases`, `Infrastructure` y `Core` (y `Discovery` en EXT).
- **Dependencias permitidas:**

```
Api ──► Discovery ──► Cases ──► Core
 │                              ▲
 └──► Infrastructure ───────────┘   (también depende de Cases y Discovery)
```

En el MVP, `Discovery` no se crea y `Api` no lo referencia.

- **Estructura de la solución:**

```
src/
  WorkflowEngine.Core/
  WorkflowEngine.Cases/
  WorkflowEngine.Discovery/        (EXT; no se crea en el MVP)
  WorkflowEngine.Infrastructure/
  WorkflowEngine.Api/
  WorkflowEngine.Sdk/              contratos públicos de extensión (ADR-17)
  WorkflowEngine.Simulator/       simulador de APIs de terceros (ADR-15)
  web/                            React + TypeScript
packs/
  reparaciones.json               paquete de referencia (datos, no código)
  citas.json                      segundo paquete real (datos)
plugins/                          plugins de ejemplo
tests/
  Acceptance/                     un test por CA, etiquetado con su ID (contrato)
  Unit/
  Architecture/                   reglas de la sección 3
  Fixtures/                       corpus sintético y respuestas grabadas (protegidos)
  Support/                        infraestructura de pruebas: fábrica, reloj falso, aserciones (no protegida)
  Evaluation/                     evaluación contra el corpus con modelo real (aparte)
harness/
  scope.json                      CA comprometidos (protegido)
docs/
  requisitos.md  arquitectura.md
  humanos/                        contexto, decisiones, riesgos e historial (no necesarios para implementar)
AGENTS.md                         índice corto que remite a docs/
```

### ADR-08 API HTTP con contrato y sesión por cookie
- **Decisión:** API REST con JSON y especificación OpenAPI; el cliente TypeScript se **genera** a partir de ella. Autenticación y roles (Dueño, Operador) con ASP.NET Core Identity y cookie de sesión; el bloqueo tras 5 intentos (CA-30c) viene incluido.
- **Límite:** la cookie exige que la interfaz y la API compartan origen. Se acepta.

### ADR-09 Estrategia de pruebas y arnés
- **Motivo:** RH-01 a RH-06.
- **Decisión:**
  - **Pruebas de aceptación** contra la aplicación completa en memoria (`WebApplicationFactory`), con PostgreSQL real de prueba (Testcontainers o una base dedicada), reloj falso y doble de canal.
  - Cada prueba lleva un atributo con su ID (por ejemplo `[Ac("CA-08a")]`). Un script cruza esos IDs con `docs/requisitos.md` y calcula la cobertura de CA (RH-01).
  - **Archivos protegidos (RH-02):** `tests/Acceptance/`, `tests/Fixtures/`, `docs/requisitos.md`, la sección 3 de este documento y `harness/scope.json`. Un chequeo en CI falla si un cambio los toca sin la aprobación de una persona revisora: la rama principal exige revisión de los propietarios (CODEOWNERS) y la línea `Approved-By:` del commit debe nombrar a quien aprobó el PR.
  - **Alcance comprometido:** `harness/scope.json` lista los CA comprometidos; la trazabilidad (RH-01, AR-08) se mide contra él.
  - **Oráculo separado:** las pruebas de aceptación de los CA comprometidos se escriben sin acceso a la implementación, a partir del texto del CA y del contrato OpenAPI (ADR-08), y una persona las revisa antes de congelarlas; el registro de tareas anota la condición (RH-04).
  - La infraestructura de pruebas (fábrica de la aplicación, reloj falso, aserciones con ID de CA) vive en `tests/Support/` y no está protegida.
  - Los mensajes de fallo incluyen el ID del CA y qué se esperaba (RH-06).
  - Las pruebas de la interfaz (CA-40, CA-N02) usan un navegador automatizado (Playwright).
  - Las pruebas que involucran agentes usan **respuestas grabadas** de `tests/Fixtures/` (RH-17, CA-N11a); la evaluación con modelo real vive en `tests/Evaluation/` y no bloquea la suite (CA-N11b).
- **Registro del experimento (RH-04):** cada tarea de agente escribe una línea en un archivo de registro versionado con modelo, tarea, uso, iteraciones y resultado.

### ADR-10 Registros técnicos sin datos personales
- **Motivo:** RNF-07.
- **Decisión:** el historial de ejecuciones (RN-12) es **dato del dominio**, no un log. Los logs técnicos usan registro estructurado con un identificador de correlación por ejecución. Los datos de contacto y credenciales se modelan con tipos específicos que **no se serializan** en logs (AR-05).

### ADR-11 Paquetes de proceso declarativos (plugins de datos)
- **Motivo:** RF-50 a RF-53, RN-21, RN-22, RN-38.
- **Decisión:**
  - Un **paquete** es un documento versionado, inmutable por versión: estados (inicial y terminales, y por cada uno si una persona puede asignarlo y si la acción «Cambiar estado» puede asignarlo), transiciones, efectos automáticos (operación que los provoca, origen y destino; por ejemplo, cambio del campo ancla en Confirmada → Agendada), campos propios (tipo, obligatoriedad y si son filtrables, buscables o campo ancla), plantillas de aviso y flujos incluidos. El de «Reparaciones» vive en `packs/` como dato.
  - Un **validador de paquetes** puro (RN-21) reutiliza el validador de flujos (RN-10) para los flujos incluidos.
  - `Cases` aplica la tabla de transiciones y los efectos automáticos leyéndolos del paquete, y valida los campos propios. Ningún código nombra un estado concreto (AR-10).
  - Un caso se guarda con **columnas comunes** (folio, nombre, contacto, canal, estado, fecha, versión de paquete) y un **documento JSON** para los campos propios, validado contra el esquema del paquete.
  - El **consentimiento** se guarda aparte, por canal y contacto, con fecha, origen y, si se retira, fecha de retirada (RN-26, RN-35). Un caso lo consulta por su canal y contacto; no lo copia.
  - Las **variables de plantilla** son los campos comunes, los campos propios del paquete y `negocio`. Una plantilla con otra variable se rechaza al guardarla (CA-27c).
  - Los **plugins de código** (acciones, canales, lectores) se describen en ADR-17.
  - La **suite de conformidad** (CA-50d, CA-50e) ejecuta las pruebas de transiciones sobre cualquier paquete.
- **Campos propios consultables:** los que el paquete declara `filtrable` o `buscable` se indexan con un índice sobre la expresión JSON en PostgreSQL, y las consultas de RF-51 solo los usan a ellos (CA-51d a CA-51g).
- **Límite:** un campo propio no declarado no es consultable; cambiar eso exige una versión nueva del paquete.

### ADR-12 (EXT) El descubrimiento se modela como flujos con pasos de agente
- **Motivo:** RF-15, RF-60 a RF-63, RF-70 a RF-72, RN-15 a RN-19.
- **Decisión:**
  - **Ingesta de solo lectura:** un lector por tipo de fuente (carpeta de texto, hoja de cálculo; después calendario e imágenes) produce un **artefacto normalizado**: texto, metadatos y huella de contenido. La huella da idempotencia (RN-19). Ningún lector tiene operaciones de escritura (AR-11).
  - Al registrar un artefacto nuevo se emite el evento «artefacto nuevo» (CA-61e), que dispara un **flujo del propio motor** con el paso agente «Extraer casos». Así el descubrimiento reutiliza persistencia, reintentos, tope de costo e historial ya construidos.
  - **Contrato del paso agente** (en `Core`): entrada tipada, esquema de salida, validación estricta, hasta 2 reintentos, tope de costo por ejecución y registro de modelo, consumo y duración. Una salida inválida es un fallo y **nunca produce efectos parciales** (CA-15a).
  - **Evidencia:** cada candidato y cada propuesta guarda referencia al artefacto y al fragmento (posición o celda) de origen (RN-16).
  - **Activación solo con confirmación:** las operaciones de crear un caso desde un candidato y de activar un paquete exigen un valor `AprobacionHumana` que solo la capa `Api` puede construir (AR-14).

### ADR-13 (EXT) Proveedor de modelo como puerto, con doble y política de salida
- **Motivo:** RN-20, RNF-10, RNF-11, RH-14, RH-17.
- **Decisión:**
  - `Core` define `IModelProvider`. `Infrastructure` tiene un adaptador real (proveedor por decidir) y un **doble** que sirve respuestas grabadas.
  - Un **filtro de salida** enmascara teléfonos y correos **antes** de que un adaptador real reciba el texto, y el **registro de salida** guarda qué se envió (CA-N10c). El adaptador real no se puede construir sin ese filtro (AR-12). Las fuentes deshabilitadas no llegan al puerto (CA-60b).
  - La interfaz admite sustituir el proveedor por un **modelo local** para evitar la salida de datos; no se evalúa en el MVP.
- **Límite declarado:** el enmascarado por patrones **no detecta datos personales en texto libre** (por ejemplo nombres propios). Por eso cada fuente la habilita explícitamente el dueño y se informa de este límite.

### ADR-14 (EXT) Evaluación de agentes sobre un corpus
- **Motivo:** RH-15, RH-16, RNF-11.
- **Decisión:** `tests/Fixtures/` contiene el corpus sintético de los tres locales (artefactos y resultado esperado). `tests/Evaluation/` ejecuta la extracción y la propuesta con un modelo real, calcula precisión y exhaustividad y las registra en cada punto de medición (RH-10). Es información para el experimento, **no una compuerta** del arnés, porque la salida de un modelo real varía.

### ADR-15 Simulador de APIs de terceros
- **Decisión:** `WorkflowEngine.Simulator` es una aplicación web **separada** dentro de la solución. No es un servicio del sistema: representa al mundo exterior. El motor la llama por HTTP como llamaría a cualquier tercero, lo que además demuestra el webhook saliente.
  - Endpoints que imitan **correo, SMS, WhatsApp, Messenger** y un **receptor HTTP POST**; registra lo recibido, lo muestra en una interfaz y deduplica por referencia (CA-90c).
  - **Inyección de fallos** por canal, desde su interfaz o por una API de administración: error, latencia, duplicado, respuesta tardía y caída (RF-91).
  - El simulador de WhatsApp aplica **ventana de conversación, plantilla aprobada y consentimiento**, calcula categoría y costo con tarifas configurables, informa estados de entrega con retraso y puede simular mensajes entrantes del cliente (RF-92, RF-93).
  - **Mensajes entrantes:** el operador o las pruebas crean un mensaje del cliente por WhatsApp o Messenger, con identificador externo y, en Messenger, origen Marketplace y publicación. El simulador lo entrega a `Api` por HTTP y reintenta con el mismo identificador (RF-94).
  - **Un solo componente, dos papeles:** demostración (se ven llegar los mensajes) y **doble de pruebas** del arnés, alojado en memoria con el reloj falso. Así no hay dos dobles que diverjan.
- **Límite declarado:** no es WhatsApp ni Messenger, es un modelo de sus reglas. Su interfaz lo indica (CA-90f).

### ADR-16 Compilación a DAG y máquina de estados de la ejecución
- **Decisión:**
  - El editor guarda el **diagrama** (posiciones, nodos y conexiones). Un **compilador puro** lo valida (RN-10) y produce un **documento JSON de DAG ejecutable**: nodos con identificador estable, aristas, nodo de inicio, parámetros y **orden topológico** precalculado. Es determinista (CA-14b) y se guarda por versión (RN-11).
  - **El intérprete solo ejecuta el documento compilado** (AR-17), nunca el diagrama.
  - La ejecución es una **máquina de estados finita** con la tabla de la sección 6.1 de requisitos en un único componente (AR-18); toda transición se registra (RN-30).

### ADR-17 Extensión por plugins
- **Motivo:** RF-80 a RF-82, RN-31, RNF-13.
- **Decisión:**
  - `WorkflowEngine.Sdk` contiene los contratos públicos (`IAction`, `IChannel`, `IInboundChannel`, `IChannelPolicy`, `ISourceReader`, metadatos y versión del contrato) y **no referencia nada**. Un plugin referencia **solo** `Sdk` (AR-15).
  - Cada plugin declara su nombre, la versión del contrato y el **esquema de sus parámetros**; el validador de flujos y la paleta del editor los usan (CA-80b).
  - Un plugin con versión de contrato incompatible no se registra (CA-80c). Una excepción en un plugin se captura **por paso** y falla ese paso, no el motor (CA-80d).
  - **MVP:** el plugin de ejemplo se registra por referencia en la composición de `Api`. **EXT:** carga desde la carpeta `plugins/`.
- **Límite declarado:** los plugins son **código de confianza**, sin aislamiento de seguridad. Un modelo seguro para plugins de terceros es otro proyecto.

### ADR-18 Qué se adopta y qué no de la arquitectura recomendada
- **Motivo:** el enunciado recomienda microservicios, Saga y Redis; ningún requisito del MVP los exige.
- **Decisión:** se adoptan la máquina de estados finita (ADR-16), PostgreSQL (ADR-03) y el DAG ejecutable (ADR-16). **No** se adoptan microservicios, Saga ni Redis: el estado vive en una base de datos y las transacciones son locales; `FOR UPDATE SKIP LOCKED` sustituye a los bloqueos distribuidos.
- **Límite:** no hay escalado independiente por componente. Si se exigiera, el procesador de ejecución se separa como proceso aparte que comparte la base de datos, sin cambiar el diseño.

### ADR-19 Respuestas entrantes y consultas
- **Motivo:** RF-36 a RF-38, RN-34 a RN-37.
- **Decisión:**
  - `Api` expone un punto de entrada por canal (`POST /entrantes/{canal}`) que recibe identificador externo, contacto del cliente, texto y, opcionalmente, origen Marketplace con su publicación.
  - Los mensajes se guardan con restricción única por canal e identificador externo; el segundo envío se ignora y responde con éxito (idempotente, CA-36b).
  - **Asociación:** por canal y contacto entre los casos no terminales. Varios casos: el más reciente y una entrada en la bandeja. Ninguno: con origen Marketplace se crea una consulta; si no, una entrada en la bandeja (RN-34).
  - En una sola transacción se guarda el mensaje, se renueva la ventana de conversación y se emite el evento «el cliente responde», con la deduplicación de ADR-03.5.
  - **El motor no interpreta texto libre.** La Condición compara con valores declarados por el flujo, tras normalizar minúsculas, acentos y espacios. «baja» y «stop» se atienden antes que cualquier flujo (RN-35).
  - Las **consultas** viven en `Cases`. Convertir una consulta en caso exige `AprobacionHumana` (AR-14).
- **Límite declarado:** el origen Marketplace es un dato que trae el mensaje en el simulador. Que las APIs reales de Meta lo entreguen a una aplicación de terceros no está verificado.

### ADR-20 Unidad de trabajo y transacciones
- **Motivo:** ADR-03 exige atomicidad por paso y RN-33 exige recalcular temporizadores en la misma transacción desde `Cases`.
- **Decisión:** `Core` define `IUnitOfWork` con `ExecuteInTransactionAsync`. `Cases`, el motor y `Api` componen operaciones dentro de un ámbito; `Infrastructure` lo implementa con EF Core. Ninguna capa abre transacciones por su cuenta.
  - **Anidamiento:** un ámbito abierto dentro de otro se une al exterior; solo el exterior confirma o revierte. Así, el paso del motor que ejecuta «Cambiar estado» confirma en una sola transacción el paso, el cambio de estado, las cancelaciones (RN-05) y el recálculo de temporizadores (RN-33).
  - Solo la implementación de `IUnitOfWork` llama a `SaveChanges` o abre transacciones; el reclamo con `FOR UPDATE SKIP LOCKED` (ADR-03) también pasa por ella.
- **Regla:** AR-19.

### ADR-21 Parámetros de flujo y de instalación
- **Motivo:** RF-27, RN-11, CA-27a.
- **Decisión:** los plazos y textos de un flujo forman parte de su documento; cambiarlos activa una versión nueva y las ejecuciones en curso conservan la suya (RN-11). La ventana horaria, la zona horaria del negocio y la política de reintentos son **configuración de la instalación**: se leen al decidir cada envío y se aplican desde que cambian.

---

## 2. Ciclo de vida de una ejecución

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

Cambio de estado del caso ⇒ cancela ejecuciones con cancelarAlSalirDelEstado, salvo la que lo originó (RN-05)
Estado terminal ⇒ cancela todas las ejecuciones en curso del caso (RN-05)
Nueva ejecución del mismo flujo y caso ⇒ la anterior se cancela como «reemplazada» (RN-05)
Aviso pendiente ⇒ se entrega con referencia única; fallo ⇒ reintento con espera (RN-09)
Temporizador anclado ⇒ vence en campo ancla ± desplazamiento; si el campo cambia, se recalcula (RN-33)
Mensaje entrante ⇒ dedup por id externo ⇒ caso ⇒ evento «el cliente responde» (ADR-19)
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

## 3. Reglas verificables por máquina (alimentan RH-07)

| ID | Regla | Cómo se verifica |
|---|---|---|
| AR-01 | `Core` no referencia `Cases`, `Discovery`, `Infrastructure` ni `Api` | Prueba de arquitectura (por ejemplo NetArchTest) |
| AR-02 | `Cases` solo referencia `Core`; `Discovery` solo referencia `Core` y `Cases` | Idem |
| AR-03 | Ninguna llamada a `DateTime.Now`, `DateTime.UtcNow` ni `DateTimeOffset.UtcNow`; el tiempo solo por `TimeProvider` | Analizador de APIs prohibidas |
| AR-04 | Los clientes HTTP y SDK de canales solo existen en `Infrastructure` | Prueba de arquitectura |
| AR-05 | Los tipos de datos personales y credenciales no se pasan a registros técnicos | Prueba de arquitectura sobre firmas + analizador |
| AR-06 | Toda acción del catálogo está en el registro único y tiene al menos un CA que la ejercita | Prueba sobre el registro |
| AR-07 | Ningún archivo de código supera 400 líneas | Comprobación en CI |
| AR-08 | Todo CA comprometido en `harness/scope.json` tiene una prueba etiquetada; los demás CA de prioridad M se informan como pendientes | Script de trazabilidad (RH-01) |
| AR-09 | `tests/Acceptance/`, `tests/Fixtures/`, `docs/requisitos.md`, la sección 3 de este documento y `harness/scope.json` no cambian sin la aprobación de una persona revisora | Chequeo en CI contra el aprobador del PR (RH-02, RH-15, RH-17) |
| AR-10 | Los estados y transiciones salen solo del paquete activo: ningún código de `Cases`, `Discovery` ni `Api` nombra un estado concreto de un paquete | Prueba de arquitectura y búsqueda de literales; los paquetes viven en `packs/` |
| AR-11 | Los lectores de fuentes no tienen operaciones de escritura y la ingesta deja los archivos con la misma huella | Prueba de arquitectura y CA-61a |
| AR-12 | Solo `Infrastructure` referencia clientes de modelos, y el adaptador real exige el filtro de enmascarado | Prueba de arquitectura y prueba de construcción |
| AR-13 | Todo paso de agente declara esquema de salida, tope de costo y límite de reintentos | Prueba sobre el registro de acciones |
| AR-14 | Crear un caso desde un candidato o una consulta, o activar un paquete, exige un valor `AprobacionHumana` que solo `Api` puede construir | Prueba de arquitectura sobre el constructor |
| AR-15 | `Sdk` no referencia ningún otro proyecto y los plugins solo referencian `Sdk` | Prueba de arquitectura |
| AR-16 | Ningún proyecto de producción referencia `Simulator`; el motor lo llama solo por HTTP | Prueba de arquitectura |
| AR-17 | El intérprete recibe únicamente el documento compilado, nunca el diagrama | Prueba de arquitectura sobre la firma |
| AR-18 | Los cambios de estado de una ejecución pasan solo por el componente de la máquina de estados | Prueba de arquitectura y CA-18a |
| AR-19 | Ninguna capa abre transacciones fuera de `IUnitOfWork` (ADR-20) | Prueba de arquitectura: solo la implementación de `IUnitOfWork` llama a `SaveChanges`, `BeginTransaction` o `Database.ExecuteSql*` |

Los mensajes de error de estas reglas **dicen cómo corregir** el problema, para que el agente lo resuelva sin ayuda.
