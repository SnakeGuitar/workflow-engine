# Informe técnico: correcciones, arnés e hitos incrementales

| Campo | Valor |
|---|---|
| Proyecto | Motor de flujos para microempresas |
| Fuentes | `docs/requisitos.md` v0.10, `docs/arquitectura.md` v0.8, `docs/humanos/*`, `AGENTS.md` |
| Fecha | 2026-11-13 |
| Plazo | 2026-12-02 |
| Estado del repositorio | 0 líneas de código |
| Uso previsto | Revisión externa (Claude) antes de aplicar cambios |

> **Correcciones posteriores:** la [auditoría del 2026-11-13](2026-11-13-auditoria.md) revisa este informe y registra los cambios de requisitos v0.12 y arquitectura v0.10. La Parte C ya incorpora el calendario corregido.

> **Naturaleza de este documento.** Es una propuesta de análisis y planificación, no fuente de verdad para implementar. Las correcciones que tocan `docs/requisitos.md` requieren un commit con `Approved-By:` según AR-09 y RH-02. Los IDs existentes no se reutilizan ni cambian de significado.

---

## Resumen ejecutivo

1. La especificación es sólida y verificable: 59 RF, 210 CA funcionales + 30 de RNF, 38 RN, 18 AR (recuento en requisitos v0.10 y arquitectura v0.8; tras la [auditoría](2026-11-13-auditoria.md): 211 CA y 19 AR). El problema no es la calidad, es que **240 CA no caben en 19 días** con 2 personas.
2. Hay **8 defectos que bloquean la implementación o invalidan la verificación**, no solo erratas: falta ADR-18, falta el concepto de transacción, dependencias contradictorias, reglas de negocio de paquete en la tabla global y una escalera de recortes que recorta requisitos M.
3. El arnés propuesto separa **oráculo** (quien escribe pruebas) de **implementación** (quien escribe código), con CI en dos velocidades y trazabilidad CA → prueba como artefacto generado, no manual.
4. Los hitos son **6 cortes verticales + 1 condicional**; cada uno entrega un sistema desplegable, con demo y tag, nunca una capa a medias.
5. Decisión pendiente más urgente: **qué 30–40 CA forman el MVP-0**. Sin esa congelación, los hitos no sirven.

---

# Parte A — Inconsistencias

> **Estado:** las correcciones A1–A17 están aplicadas en los documentos fuente (requisitos v0.11, arquitectura v0.9). Esta parte se conserva como registro del diagnóstico; el detalle está en el Registro de aplicación, al final.

## A.1 Bloqueantes

| # | Ubicación | Problema | Corrección |
|---|---|---|---|
| A1 | `arquitectura.md` (salta ADR-17 → ADR-19) | **ADR-18 no existe en el documento de arquitectura.** Solo está en `humanos/decisiones.md:93`. `contexto.md:17` y `decisiones.md:91` lo citan como justificación vigente. Un agente que solo lea arquitectura no sabe por qué no hay Saga/Redis. | Insertar ADR-18 entre ADR-17 y ADR-19 con la tabla de `decisiones.md:96-105`. |
| A2 | `arquitectura.md` (ADR-03, ADR-07) | **No existe un puerto de transacción/unidad de trabajo.** ADR-03 exige atomicidad ("cada avance es una transacción") y RN-33 exige recalcular temporizadores "en la misma transacción" desde `Cases`; pero `Cases` solo depende de `Core` y `Core` no tiene abstracción de transacción. En la práctica se violará AR-01/AR-02 o se abrirán transacciones en `Api`. | Añadir **ADR-20 Unidad de trabajo**: `Core` define `IUnitOfWork.ExecuteInTransactionAsync(...)`; `Cases` orquesta puertos dentro del ámbito; `Infrastructure` lo implementa con EF Core. Añadir **AR-19**: ninguna transacción se abre fuera de `IUnitOfWork`. |
| A3 | `AGENTS.md:15` | Dice `Infrastructure → Core`, pero ADR-07 y AR-02 permiten que `Infrastructure` dependa de `Cases` y `Discovery` para persistir sus tipos. Un agente literal rechazará el diseño correcto. | Reescribir: `Api → Discovery → Cases → Core`; `Infrastructure → Cases, Discovery, Core`; `Core` no referencia a nadie. |
| A4 | `humanos/riesgos.md:37` (AR-R10) | Dice "solo los campos comunes son buscables en el MVP", en contradicción con ADR-11 ("campos propios consultables" con índice JSON) y con **RF-51, prioridad M** (CA-51d a CA-51g). | Sustituir por: "Los campos propios no declarados no son consultables; los declarados `filtrable`/`buscable` usan índice JSON (ADR-11)". |
| A5 | `requisitos.md:180` (RN-05) vs `:347` (CA-25b) | A las 120 h **la propia ejecución** cambia el caso a *Sin respuesta*. RN-05 dice que todo cambio de estado cancela esperas pendientes: ¿se cancela a sí misma antes de registrar el paso? | Añadir a RN-05: "La cancelación no alcanza al paso que originó el cambio, que termina y se registra como correcto; sí alcanza al resto de esperas del caso, incluidas las de la misma ejecución." |
| A6 | `requisitos.md:128-141` y `:159` | **"Asignable por el sistema" solo está definido para «Citas»**, no para «Reparaciones» (§6 no lo declara; solo CA-06c menciona *Sin respuesta*). Además se mezclan tres conceptos: transición permitida a humanos, transición permitida a la acción *Cambiar estado*, y efecto automático (reprogramar → Agendada). | Declarar en el paquete un campo por estado: `humanAllowed`, `systemActionAllowed`, `autoEffect`. Añadir a §6: "Asignables por el sistema: Sin respuesta". En §6.2, `Agendada` debe ser `autoEffect`, **no** `systemActionAllowed`, y CA-06c debe dejar de listarla. |
| A7 | `requisitos.md:177-179, 213` (RN-02, RN-03, RN-04, RN-38) | Son **reglas de negocio de un paquete** (nombran Cotización enviada, Listo para recoger, Agendada) en la tabla global. Si un agente las implementa en código, viola **AR-10** ("ningún código nombra un estado concreto"). Además duplican lo que ya describen los flujos RF-25 y RF-53. | Mantener las RN como semántica observable, pero añadir: "Se implementan exclusivamente en el flujo incluido del paquete (`packs/reparaciones.json`, `packs/citas.json`); el motor no contiene estados concretos." Y que los CA apunten al flujo, no a una regla codificada. |
| A8 | `requisitos.md:43, 48, 475-479` + escalera de recortes | La escalera permite recortar **SMS** (punto 5), pero **RF-90 es M** y CA-90b exige SMS. Se recorta un requisito de prioridad M sin cambiar su prioridad. | **Resuelto:** SMS se queda M; el quinto recorte pasa a CA-94c/d (después RF-41, ver la [auditoría](2026-11-13-auditoria.md)). |

## A.2 Ambigüedades que cambian pruebas

| # | Ubicación | Problema | Decisión propuesta |
|---|---|---|---|
| A9 | `requisitos.md:241` (RF-04) | No se dice si **crear** un caso emite el evento "el caso entra al estado inicial". Afecta a flujos que arrancan en Recibido/Solicitada. | **Resuelto:** no se emite; RF-04 lo declara y «caso creado» queda en EXT. |
| A10 | `requisitos.md:598` (RH-01) | "100 % de los CA de prioridad M": los CA de RNF no tienen prioridad, así que la meta es ambigua. | "100 % de los CA de RF prioridad M **del MVP-0**; los CA de RNF se declaran aparte con su propia meta." |
| A11 | `requisitos.md:467` (CA-53b) vs RN-08 | Una entrada de bandeja a las **03:00**. RN-08 limita *avisos* a 09:00–20:00, y una entrada no es un aviso: es legal pero inútil (nadie llama a las 3 a.m.). | **Resuelto:** no se difiere; RN-08 aclara que la ventana aplica solo a avisos salientes. CA-53b no cambia. |
| A12 | `AGENTS.md`, AR-09, RH-02 | Protegen `tests/Acceptance/`, `tests/Fixtures/` y `docs/requisitos.md`, pero **no `docs/arquitectura.md`**. Un agente puede debilitar AR-01..18 para pasar CI. | Añadir `docs/arquitectura.md` (al menos §3) a la lista protegida y a AR-09/RH-02. |

## A.3 Higiene

| # | Ubicación | Problema | Corrección |
|---|---|---|---|
| A13 | 4 casos (`CA-81b`, `CA-N01b`, `CA-N13c`, `CA-N14a`) | `[HUMANO]` está **dentro** de la negrita (`**CA-81b [HUMANO]**`), lo que rompe cualquier parser de CA. | Formato único: `**CA-81b** [HUMANO]` y ajustar el script de trazabilidad. |
| A14 | `arquitectura.md:82` | `Discovery` figura en la estructura de la solución del MVP aunque es EXT. Un directorio vacío invita a rellenarlo. | Anotar "(EXT; no se crea en el MVP)" o sacarlo del árbol MVP. |
| A15 | `requisitos.md:481` (CA-90e) | "Funciona sin internet (CA-N08c)": CA-N08c habla de la suite, no del simulador. | Referenciar RNF-08/CA-N08c como motivación, no como criterio equivalente. |
| A16 | `requisitos.md:7` | "Equipo: 2 personas". El equipo mencionó 3. | **Resuelto:** 2 personas + agentes. |
| A17 | `requisitos.md:176` (RN-01) | La coletilla "en Reparaciones, no pasa a En reparación sin haber estado Aprobada" es redundante con §6 y es un literal de dominio en una RN global. | Eliminarla; la tabla de transiciones ya lo garantiza. |

## A.4 Textos listos para aplicar

### ADR-18 (insertar en `arquitectura.md` entre ADR-17 y ADR-19)

> **Motivo:** el enunciado recomienda microservicios, Saga y Redis; ningún requisito del MVP los exige.
> **Decisión:** se adopta la máquina de estados finita (ADR-16), PostgreSQL (ADR-03) y el DAG ejecutable (ADR-16). **No** se adoptan microservicios, Saga ni Redis: el estado vive en una base de datos y las transacciones son locales; `FOR UPDATE SKIP LOCKED` sustituye a los bloqueos distribuidos.
> **Límite:** escalado independiente por componente. Si se exigiera, el procesador de ejecución se separa como proceso aparte que comparte la base de datos, sin cambiar el diseño.

### ADR-20 (nuevo, tras ADR-19)

> **Motivo:** ADR-03 exige atomicidad por paso; RN-33 exige recalcular temporizadores en la misma transacción desde `Cases`.
> **Decisión:** `Core` define `IUnitOfWork` con `ExecuteInTransactionAsync`. `Cases` y el motor componen operaciones dentro de un ámbito; `Infrastructure` lo implementa con EF Core. Ninguna capa abre transacciones por su cuenta.
> **Regla:** AR-19.

### RN-05 (reemplazo)

> Cuando un caso cambia de estado se cancelan los recordatorios y esperas pendientes del estado anterior, incluidas las de la ejecución que originó el cambio, **salvo el paso que ejecuta el cambio**, que termina y se registra como correcto.

### Paquete, esquema de estados (añadir a §6 y §6.2)

> Cada estado declara: `terminal`, `humanAllowed`, `systemActionAllowed` y `autoEffect`. Solo `systemActionAllowed` puede usarse en la acción *Cambiar estado* (CA-06c). `autoEffect` son cambios producidos por el propio sistema como consecuencia de otra operación (por ejemplo, reprogramar una cita Confirmada).

---

# Parte B — Estructura del arnés

## B.1 Principios

1. **El arnés no es un test runner: es el ejecutor del contrato.** Su salida es un veredicto por CA, no un porcentaje de cobertura.
2. **Dos velocidades:** gate de PR ≤ 3 min; suite completa ≤ 10 min; lento/noche aparte.
3. **Determinismo total:** reloj falso, sin red, semilla fija, Postgres compartido por job.
4. **Oráculo independiente:** quien escribe la prueba no es quien implementa.
5. **Todo artefacto protegido se verifica por hash**, no por convención.

## B.2 Estructura de repositorio

```
tools/
  Harness/                      # consola .NET (System.CommandLine)
    Commands/Verify.cs          # build + tests + AR + traza
    Commands/Trace.cs           # CA -> prueba
    Commands/Freeze.cs          # manifiesto de archivos protegidos
    Commands/Task.cs            # registro RH-04 (start/end)
    Commands/Budget.cs          # tope RH-14
    Commands/Report.cs          # métricas M1..M6
  Harness/protected.json        # sha256 + reglas de aprobación
  Harness/budget.json           # tokens/costo por modelo y global
tests/
  Support/                      # infraestructura de pruebas, no protegida (ADR-09)
    ApiFactory.cs               # WebApplicationFactory + Postgres
    FakeClock.cs                # TimeProvider + avance manual
    FakeChannel.cs              # doble con inyección de fallos
    CaAssert.cs                 # fallo con ID de CA (RH-06)
  Acceptance/
    CA01_FlowsTests.cs          # [Ac("CA-01a")]
    CA02_ValidationTests.cs
    ...
  Architecture/
    LayersTests.cs              # AR-01, 02, 04, 15, 16, 17
    ForbiddenApiTests.cs        # AR-03 (Roslyn)
    FileSizeTests.cs            # AR-07
    PersonalDataTests.cs        # AR-05
    RegistryTests.cs            # AR-06, 13
    StateLiteralTests.cs        # AR-10
  Unit/
  Evaluation/                   # EXT, no bloquea
harness/
  tasks.ndjson                  # registro por tarea (append-only)
  metrics/                      # trace.json, trace.md, m1..m6
  docker-compose.test.yml
```

## B.3 Los cinco componentes

### 1. Trazabilidad (`trace`)

- Extrae `[Ac("CA-XXy")]` de `tests/Acceptance` y `tests/Architecture`.
- Cruza con las definiciones y prioridades de `requisitos.md`.
- Falla si: CA M del MVP-0 sin prueba, prueba cita CA inexistente, o CA `[HUMANO]` tratado como automático.
- Emite `trace.json` y `trace.md` con versión de especificación + commit.

### 2. Freeze

- `protected.json` con sha256 de: `docs/requisitos.md`, `docs/arquitectura.md` (§3), `tests/Acceptance/`, `tests/Fixtures/`, `harness/scope.json`.
- CI compara el hash; si cambió, exige que el PR tenga la aprobación de un propietario (CODEOWNERS, protección de rama) y que `Approved-By:` nombre a esa persona. Un agente puede escribir la línea, no puede aprobar el PR.
- Añadir `docs/arquitectura.md` cierra A12.

### 3. Registro de tareas (`task start|end`)

- Una línea NDJSON por tarea: `spec_version`, `commit_base`, `model`, `task`, `rf[]`, `ca[]`, `start`, `end`, `iterations`, `tokens`, `cost`, `result`, `human_interventions`.
- `task start` rechaza la tarea si no declara CA (RH-03) o si el presupuesto está agotado (RH-14).
- De aquí salen M1–M6 sin trabajo manual.

### 4. CI en dos velocidades

| Etapa | Disparador | Contenido | Presupuesto |
|---|---|---|---|
| `gate` | cada push | build `-warnaserror`, Unit, Architecture, tamaño, gitleaks, freeze, trace | ≤ 3 min |
| `acceptance` | cada PR | Postgres como *service container* (no Testcontainers por test), aceptación excluyendo `Slow`; incluye reinicio simulado para CA-10a/b (se cierra el host y se abre otro sobre la misma base, con el reloj falso) | ≤ 10 min |
| `ui` | merge a main / manual | Playwright contra `docker compose` | ≤ 5 min |
| `nightly` | cron | CA-10 con `kill -9` real, escala al 10 % de RNF, Evaluation (EXT), reporte de flakiness | sin límite |
| `release` | tag | build de imagen, `compose up`, smoke CA-N14 | ≤ 15 min |

Nota: Testcontainers por prueba hará imposible el límite de 10 min. Un contenedor por job y limpieza por `TRUNCATE` por clase.

### 5. Oráculo cruzado (la pieza experimental)

- Alcance: los CA comprometidos de `harness/scope.json` (decisión 7, ADR-09); después, mismo agente con revisión por muestreo.
- El contrato OpenAPI del hito (ADR-08) se fija antes de escribir las pruebas; sin él, el oráculo inventa endpoints.
- Carril `oracle`: un modelo escribe las pruebas solo con el texto del CA y el contrato OpenAPI. Nunca ve la implementación.
- Carril `impl`: otro modelo implementa sin ver `tests/Acceptance`.
- `tests/Acceptance` queda en solo lectura para el carril `impl` (permisos + freeze + CI).
- Un humano revisa el test antes de congelarlo; a partir de ahí es contrato.
- Esto convierte la ventaja multi-modelo en rigor y arregla el defecto señalado: hoy el arnés mide que el agente es coherente consigo mismo.

## B.4 Contrato de fallo (RH-06)

```csharp
public static class Ca
{
    public static void Expect(string ca, object expected, object actual, string context = "") { }
}
// Salida: CA-08a · esperaba "cancelada por cambio de estado", obtuve "en curso" · caso 1234
```

`verify` termina con la lista de CA incumplidos, no con un volcado de stack traces.

## B.5 Riesgos del arnés

| Riesgo | Mitigación |
|---|---|
| El arnés consume la semana 1 | Reutilizar xUnit + NetArchTest; no construir UI propia |
| Pruebas de aceptación lentas | Un Postgres por job; sin red; paralelismo por clase |
| Agentes "arreglan" pruebas | Freeze + oráculo separado + revisión humana |
| Presupuesto agotado | `budget.json` + corte por tarea, no por día |
| Regla de 400 líneas genera fricción en React | Excluir `web/` de AR-07 o subir el límite solo ahí |

---

# Parte C — Hitos incrementales

## C.1 Regla común

Cada hito entrega **una versión completa del sistema**: `docker compose up` desde cero, login, flujo demo, tag `v0.x`, demo grabada, CI verde, AR verde, `trace.md` actualizado. Nada de "esto queda a medias para el siguiente".

## C.2 Calendario

Dos carriles humanos (decisión 1): **motor** (backend, persistencia, canales) y **producto** (editor, dominio, vista del dueño, QA). Ningún hito usa algo que entregue un hito posterior.

| Hito | Fechas | Días | Carril motor | Carril producto | Demo | Mide (RH-10) |
|---|---|---|---|---|---|---|
| **H0** Esqueleto vertical | 13–15 nov | 3 | Arnés mínimo, compilador, motor sin esperas, webhook | Login, paquete sintético, casos mínimos | Cambiar el estado de un caso de un paquete sintético y ver llegar el POST al simulador | Línea base |
| **H1** Editor + paquete | 16–18 nov | 3 | Validador completo, activación y versión, disparo por evento | Editor, «Reparaciones» como dato, casos y transiciones | Dibujar un ciclo, ver el error, corregir, activar y dispararlo con un cambio de estado | — |
| **H2** Motor durable | 19–21 nov | 3 | Temporizadores, reintentos, reanudación, no duplicación, concurrencia | Acciones del catálogo, historial, inyección de fallos | `kill -9` durante una espera; el flujo continúa sin repetir pasos | 25 % |
| **H3** «Reparaciones» + respuestas | 22–25 nov | 4 | Correo, SMS y WhatsApp con política; respuestas entrantes | Cotización, bandeja, línea de tiempo, campos propios, vista del dueño | Caso → cotización → 24/72/120 h → *Sin respuesta* → bandeja; el cliente responde por WhatsApp | 50 % |
| **H4** «Citas» + anclados | 26–28 nov | 3 | Temporizadores anclados y recálculo | Paquete y flujo «Citas», agenda del día | Reprogramar una cita Confirmada; respuesta «1» | — |
| **H5** Resto de la escalera | 29–30 nov | 2 | Messenger y Marketplace, estados de entrega | Plantillas de WhatsApp, plugin de ejemplo, errores sobre el diagrama | Lo que llegue, en orden inverso a la escalera | 100 % de lo implementado |
| **H6** Cierre | 1–2 dic | 2 | Instalación limpia, RH-11, RH-18 | README, licencia, métricas, vídeo | Instalación en 15 min | Resultados |

RH-09 (con y sin arnés) se ejecuta durante H1–H4 sobre tareas ya cerradas, no al final.

## C.3 Fichas

### H0 — Esqueleto vertical (13–15 nov)

- **Versión completa:** login con cookie; un paquete sintético de prueba (datos, como en CA-50e) con un flujo sembrado y compilado; un cambio de estado de un caso dispara la ejecución por evento (RF-04), que hace `POST` al simulador; historial con los pasos. No hay disparo manual: ningún RF lo define.
- **CA:** CA-30a, CA-14a, CA-16a, CA-90a, CA-12a.
- **Incluye arnés mínimo:** CI (`gate`), Postgres por job, reloj falso, `tests/Support/`, freeze con CODEOWNERS, trace contra `harness/scope.json`, registro de tareas, AR-01/02/07/19.
- **No incluye:** editor, validación completa, esperas.
- **Criterio de muerte (15 nov, carril motor):** sin demo, las dos personas siguen en H0 y se aplica el recorte (1); el día perdido sale de H5.

### H1 — Editor + paquete «Reparaciones» mínimo (16–18 nov)

- **Versión completa:** el operador dibuja, valida (todos los errores), activa y versiona; un cambio de estado de un caso de «Reparaciones» dispara el flujo dibujado.
- **CA:** CA-01a/b, CA-02a–i, CA-03a–d, CA-14b–e, CA-40a–e, CA-41a; CA-50a/b/d, CA-20a–d, CA-21a–c, CA-04a/b.
- **Demo:** ciclo A→B→A marcado sobre los nodos; corregir; activar v1; cambiar el estado de un caso y ver la ejecución; compilar dos veces y comparar bytes.
- **Criterio (18 nov, carril producto):** CA comprometidos de H1 en verde; si no, recortes (1) y (2).

### H2 — Motor durable (19–21 nov)

- **Versión completa:** ejecuciones que sobreviven reinicio, temporizadores, reintentos con esperas, cancelación por estado (RN-05), sin duplicados.
- **CA:** CA-05a/b, CA-06a/b/d/e, CA-07a–c, CA-08a/b, CA-09a/b, CA-10a–c, CA-11a/b, CA-12b/c, CA-13a/b, CA-16b–f, CA-18a–c, CA-91a–c.
- **Demo:** `kill -9` durante una espera; reiniciar; el flujo continúa sin repetir pasos; canal que falla dos veces y acierta a la tercera.
- **Criterio de muerte (21 nov, carril motor):** CA-10a/b en verde. Si no: recortes (1)–(5) inmediatos, las dos personas al motor y plan B técnico (reclamo por sondeo de la tabla de temporizadores, sin concesión, con una sola instancia, ADR-01). Si el 23 sigue en rojo, se pide el cambio aprobado de requisitos para los recortes (6) y (7). No se tocan ADR-03, RN-06, CA-10c, CA-11 ni el arnés.

### H3 — «Reparaciones» completo + respuestas (22–25 nov)

- **Versión completa:** registrar caso, transiciones, cotización, flujo de seguimiento instalado, bandeja, línea de tiempo, vista del dueño, avisos por Correo, SMS y WhatsApp, respuestas del cliente.
- **CA:** RF-22 a RF-29, RF-31 (Correo, SMS y WhatsApp), RF-35, RF-36, RF-51, RF-90, RF-92, CA-94c/d, CA-06c, CA-30b/c.
- **Demo:** crear caso → En diagnóstico → Cotización 1500 → aviso → 24 h y 72 h → 120 h a *Sin respuesta* → bandeja; el cliente responde por WhatsApp y aparece en la línea de tiempo.
- **Criterio (25 nov, ambos):** si la demo falla, se pide ya el recorte (6).

### H4 — «Citas» + temporizadores anclados (26–28 nov)

- **No es condicional:** «Citas» solo sale por el recorte (7), que exige un cambio aprobado de requisitos.
- **CA:** RF-17, RF-52, RF-53.
- **Demo:** cambiar `fecha_cita` de una cita Confirmada: vuelve a Agendada, se pide confirmar y los recordatorios se calculan para la nueva fecha; respuesta «1» → Confirmada.
- **Criterio (28 nov, carril producto):** si no está, recorte (7).

### H5 — Resto de la escalera (29–30 nov)

- En orden inverso a la escalera: RF-37, RF-38 y CA-94a/b, RF-41, RF-93, RF-33, RF-80/81 y RNF-13 (RF-27 va en H3). Lo que no esté el 30 queda recortado.

### H6 — Cierre (1–2 dic)

- **Versión completa:** instalación en 15 min (CA-N14a), dos cambios de requisito controlados (RH-11), plugin como cambio controlado si existe (RH-18), métricas M1–M6, vídeo.

## C.4 Puntos de control

| Fecha | Responsable | Medición | Decisión |
|---|---|---|---|
| 15 nov | Carril motor | Demo de H0 | Sin demo: recorte (1) y se sigue en H0 |
| 18 nov | Carril producto | CA comprometidos de H1 en verde (`harness/scope.json`) | Si no: recortes (1)–(2) |
| 21 nov | Carril motor | CA-10a/b en verde | Si no: recortes (1)–(5) y plan B técnico; el 23, cambio aprobado para (6)–(7) |
| 25 nov | Ambos | Demo de H3 | Si falla: recorte (6) |
| 28 nov | Carril producto | Demo de H4 | Si falla: recorte (7) |
| 30 nov | Ambos | Congelación de features | Solo correcciones |

La medición no es «CA por día», que un agente puede inflar: es CA comprometidos y CA del hito en curso en verde. Los recortes siguen siempre la escalera de la sección 2 de requisitos.

## C.5 Qué no entra en ningún hito

`Discovery`, pasos de agente, evaluación con modelo real, campos propios avanzados, Telegram, canales reales de Meta, plugins cargados desde carpeta, aislamiento multi-negocio. Quedan en EXT con sus IDs intactos.

---

# Anexo — Decisiones (resueltas el 2026-11-13)

| # | Decisión | Resolución |
|---|---|---|
| 1 | Tamaño del equipo | **2 personas + agentes** (confirma `requisitos.md:7`). Dos carriles humanos: motor/backend y frontend/dominio/QA. |
| 2 | SMS | Se queda **M**, implementado como alias del canal HTTP. La quinta bala de recorte pasa a CA-94c/d (después RF-41, ver la [auditoría](2026-11-13-auditoria.md)). |
| 3 | Evento al crear | **No se emite.** Documentado en RF-04; «caso creado» queda en EXT. |
| 4 | Bandeja y horario | **No se difiere.** Aclarado en RN-08: la ventana aplica solo a avisos salientes. |
| 5 | Proteger arquitectura | **Sí, la sección 3**, vía AR-09, RH-02 y ADR-09. Mecanismo: `harness/ar-contract.json` (pendiente de crear). |
| 6 | MVP-0 | **12 CA comprometidos + 3 de soporte**; 30 como objetivo, con criterio de muerte el 21 de noviembre. Ampliado a 16 comprometidos tras la auditoría. |
| 7 | Oráculo | **Separación parcial**: oráculo independiente en el primer corte (ADR-09); después, mismo agente con revisión por muestreo. |

## Registro de aplicación

### `requisitos.md` → v0.11

- A5: RN-05 exime al paso que origina el cambio.
- A8: la escalera de recortes ya no recorta SMS; el quinto recorte es CA-94c/d (después RF-41, ver la [auditoría](2026-11-13-auditoria.md)). SMS entra en la lista de no recortables.
- A9: RF-04 declara que la creación no emite evento de estado.
- A11: RN-08 declara que las entradas de bandeja no se difieren.
- A6: §6 y §6.2 separan «asignable por la acción Cambiar estado» de «efecto automático».
- A7: nota en §7: RN-02 a RN-04 y RN-38 se implementan en los flujos de `packs/`, no en código (AR-10).
- A10: RH-01 acota la meta a los CA de RF prioridad M del alcance comprometido.
- A12: RH-02 protege también la sección 3 de la arquitectura.
- A15: CA-90e referencia RNF-08 como criterio.
- A13: `[HUMANO]` sale de la negrita en CA-81b, CA-N01b, CA-N13c y CA-N14a.
- A17: se elimina la coletilla redundante de RN-01.

### `arquitectura.md` → v0.9

- A1: ADR-18 insertado entre ADR-17 y ADR-19.
- A2: ADR-20 (unidad de trabajo) y AR-19.
- A12: AR-09 y ADR-09 protegen la sección 3.
- Decisión 7: ADR-09 añade el oráculo separado.
- A14: ADR-07 marca `Discovery` como EXT y aclara que `Api` no lo referencia en el MVP.
- A6: ADR-11 declara la asignabilidad de estados en el paquete.

### `AGENTS.md`, `humanos/riesgos.md`, `humanos/historial.md`

- A3: dependencias corregidas (`Infrastructure → Cases, Discovery, Core`).
- A12: sección 3 de arquitectura añadida a archivos protegidos.
- A4: AR-R10 alineado con RF-51 y ADR-11.
- Historial: filas 0.11 y 0.9.

## MVP-0

**Comprometidos (16):** CA-40a, CA-40d, CA-40e, CA-02a, CA-02c, CA-02g, CA-14a, CA-14b, CA-14d, CA-03b, CA-04a, CA-07a, CA-09a, CA-10b, CA-16a, CA-16b. Los cuatro últimos añadidos (CA-03b, CA-40e, CA-04a, CA-10b) salen de la auditoría: unen editor, compilador y motor.

**Soporte (3):** CA-10a (reanudación), CA-12a (historial), CA-90a (simulador recibe).

El MVP-0 vive en `harness/scope.json` (protegido).

Cubre las cuatro tareas del enunciado: editor, validador/compilador, motor con esperas y reintentos, y webhook saliente con simulador. CA-10a es el diferenciador: sin durabilidad no hay motor, solo un diagrama que se ejecuta.

## Pendiente

1. Commit con `Approved-By:` — `docs/requisitos.md`, la sección 3 de `docs/arquitectura.md` y `harness/scope.json` están protegidos.
2. `harness/ar-contract.json`: se crea al montar el arnés (decisión 5).
3. El MVP-0 vive en `harness/scope.json`; RH-01 y AR-08 lo usan como meta.
4. A16 (2 vs 3 personas) queda resuelto: 2 personas + agentes.

---

**Nota operativa:** los cambios en `docs/requisitos.md` requieren un commit con `Approved-By:` según la regla propia del proyecto. Los de `arquitectura.md`, `AGENTS.md` y `humanos/*` no, salvo los de la sección 3 de arquitectura, que quedan protegidos a partir de esta versión.

