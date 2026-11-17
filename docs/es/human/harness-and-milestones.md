# Arnés e hitos

| Campo | Valor |
|---|---|
| Fuentes | [requirements.md](../requirements.md) v1.1 (RH-01 a RH-14), [architecture.md](../architecture.md) v1.1 (ADR-09, sección 3) |
| Plazo | 2026-12-02 |
| Equipo | 2 personas + agentes, en dos carriles: **motor** (backend, persistencia, simulador) e **interfaz** (editor, acceso, historial, QA) |

> **Naturaleza de este documento.** Plan operativo del arnés y del calendario, no fuente de verdad para implementar. Lo normativo está en los RH, en ADR-09 y en las reglas AR. Sustituye al informe del 2026-11-13, que planificaba la especificación anterior (está en git).

---

## 1. Arnés

### 1.1 Principios

1. **El arnés no es un test runner: es el ejecutor del contrato.** Su salida es un veredicto por CA, no un porcentaje de cobertura.
2. **Dos velocidades:** gate de cada push ≤ 3 min; suite de aceptación ≤ 10 min; lo lento va de noche.
3. **Determinismo:** reloj falso, sin red externa, semilla fija, un PostgreSQL por job.
4. **Oráculo independiente:** quien escribe la prueba de un CA comprometido no es quien lo implementa.
5. **Todo artefacto protegido se verifica por huella**, y su cambio exige la aprobación de una persona, no una convención.

### 1.2 Estructura

```text
tools/
  Harness/                      .NET CLI (System.CommandLine)
    Commands/Verify.cs          build + tests + AR rules + trace
    Commands/Trace.cs           CA → test
    Commands/Freeze.cs          protected-file fingerprints
    Commands/Task.cs            RH-04 records (start/end)
    Commands/Budget.cs          RH-14 limit
    Commands/Report.cs          M1–M6 metrics
    protected.json             SHA-256 fingerprints, including architecture section 3
    budget.json                per-model and global usage
tests/
  Support/                      unprotected
    ApiFactory.cs               WebApplicationFactory + PostgreSQL + Editor/Reader session
    FakeClock.cs                manually advanced TimeProvider
    SimulatorHost.cs            in-memory simulator with failure injection
    CaAssert.cs                 failures include CA ID (RH-06)
  Acceptance/                   protected
    CA02_ValidationTests.cs     [Ac("CA-02a")] …
    CA30_AccessTests.cs
  Architecture/
    LayersTests.cs              AR-01, AR-04, AR-16, AR-17
    ForbiddenApiTests.cs        AR-03
    CredentialsTests.cs         AR-05
    RegistryTests.cs            AR-06
    FileSizeTests.cs            AR-07
    StateMachineTests.cs        AR-18
  Unit/
harness/
  scope.json                    committed CA (protected)
  tasks.ndjson                  one line per task (append-only)
  metrics/                      trace.json, trace.md, M1–M6
```

### 1.3 Componentes

**1. Trazabilidad (`trace`).** Extrae `[Ac("CA-XXy")]` de `tests/Acceptance` y `tests/Architecture` y lo cruza con `docs/en/requirements.md` y `harness/scope.json`. Falla si un CA comprometido no tiene prueba, si una prueba cita un CA inexistente o si un CA **[HUMANO]** se trata como automático. Emite `trace.json` y `trace.md` con la versión de la especificación y el commit (RH-01, RH-13, AR-08).

**2. Freeze.** `protected.json` guarda la huella de ambas versiones de requisitos y la sección 3 de ambas versiones de arquitectura, `tests/Acceptance/` y `harness/scope.json`. Si una huella cambia, CI exige que el PR tenga la aprobación de un propietario (CODEOWNERS y protección de rama) y que la línea `Approved-By:` nombre a esa persona. Un agente no puede escribir esa línea ni aprobar su propio PR (RH-02, AR-09).

**3. Registro de tareas (`task start|end`).** Una línea NDJSON por tarea: `spec_version`, `commit_base`, `model`, `task`, `rf[]`, `ca[]`, `oracle` (booleano), `start`, `end`, `iterations`, `tokens`, `cost`, `result`, `human_interventions`. `task start` rechaza la tarea si no declara CA (RH-03) o si el presupuesto está agotado (RH-14). De aquí salen M1 a M6 sin trabajo manual (RH-04, RH-05, RH-08).

**4. CI en dos velocidades.**

| Etapa | Disparador | Contenido | Presupuesto |
|---|---|---|---|
| `gate` | cada push | build con `-warnaserror`, Unit, Architecture, tamaño de archivos, gitleaks, freeze, trace | ≤ 3 min |
| `acceptance` | cada PR | PostgreSQL como contenedor de servicio, aceptación sin las marcadas `Slow`; reinicio simulado para CA-10a/b (se cierra el host y se abre otro sobre la misma base, con el reloj falso) | ≤ 10 min |
| `ui` | merge a main o manual | Playwright contra `docker compose` (CA-40, CA-30e, CA-04c) | ≤ 5 min |
| `nightly` | cron | CA-10 con `kill -9` real, CA-13c con dos procesos, informe de pruebas inestables | sin límite |
| `release` | tag | imagen, `compose up`, humo de CA-N14a | ≤ 15 min |

Un contenedor de PostgreSQL por job y limpieza con `TRUNCATE` por clase; un contenedor por prueba no cabe en 10 min.

**5. Oráculo cruzado.**
- Alcance: los CA comprometidos de `harness/scope.json`. El resto, mismo agente con revisión por muestreo.
- El contrato OpenAPI del hito (ADR-08) se fija antes de escribir las pruebas; sin él, el oráculo inventa recursos.
- Carril `oracle`: un modelo escribe las pruebas solo con el texto del CA y el contrato OpenAPI; nunca ve la implementación.
- Carril `impl`: otro modelo implementa sin ver `tests/Acceptance`, que queda en solo lectura para él (permisos, freeze y CI).
- Una persona revisa cada prueba antes de congelarla; desde ahí es contrato.

### 1.4 Contrato de fallo (RH-06)

```csharp
public static class Ca
{
    public static void Expect(string ca, object expected, object actual, string context = "") { }
}
// Output: CA-09a · expected "3 attempts", got "2" · execution 7f3c…
```

`verify` termina con la lista de CA incumplidos, no con un volcado de trazas de pila.

### 1.5 Riesgos del arnés

| Riesgo | Mitigación |
|---|---|
| El arnés consume la primera semana | Reutilizar xUnit y NetArchTest; sin interfaz propia; solo lo de H0 al principio |
| Pruebas de aceptación lentas | Un PostgreSQL por job; sin red; paralelismo por clase |
| Los agentes "arreglan" las pruebas | Freeze, oráculo separado y revisión humana |
| Presupuesto agotado | `budget.json` y corte por tarea, no por día |
| La regla de 400 líneas choca con componentes de React | Revisar en H1 si `web/` necesita otro límite (cambio de la sección 3, con aprobación) |

---

## 2. Hitos

### 2.1 Regla común

Cada hito entrega **una versión completa del sistema**: `docker compose up` desde cero, inicio de sesión, flujo de demostración, tag `v0.x`, demo grabada, CI en verde, reglas AR en verde y `trace.md` actualizado. Nada queda a medias para el siguiente hito, y ninguno usa algo que entregue uno posterior.

### 2.2 Calendario

| Hito | Fechas | Carril motor | Carril interfaz | Demo | Mide (RH-10) |
|---|---|---|---|---|---|
| **H0** Esqueleto vertical | 15–17 nov | Arnés mínimo, compilador mínimo, motor sin esperas, webhook, simulador con receptor HTTP | Inicio de sesión con cuentas sembradas, esqueleto de la SPA, cliente OpenAPI | Iniciar sesión; enviar `user.registered` con la clave; el POST llega al simulador | Línea base |
| **H1** Definición | 18–20 nov | Validador completo, compilador, activación y versiones | Editor | Dibujar un ciclo, ver los errores, corregir, activar y dispararlo con un evento | 25 % |
| **H2** Motor durable | 21–24 nov | Temporizadores, reintentos, reanudación, no duplicación, concurrencia, máquina de estados, inyección de fallos | Historial, roles en la interfaz | `kill -9` durante una espera; continúa sin repetir pasos; el Lector no puede activar | 50 % |
| **H3** Ejemplo completo | 25–28 nov | Condiciones y contexto, correo y SMS, flujo de ejemplo | Interfaz del simulador, evento de prueba | El ejemplo del enunciado de punta a punta | 100 % |
| **H4** Cierre | 29 nov–2 dic | Instalación limpia, cambios de requisito controlados (RH-11) | README, métricas M1 a M6, presentación | Instalación en 15 min | Resultados |

RH-09 (con y sin arnés) se ejecuta durante H1 a H3 sobre tareas ya cerradas, no al final. Los porcentajes de RH-10 son sobre los 20 RF de la especificación.

### 2.3 Fichas

**H0 — Esqueleto vertical (15–17 nov)**
- **Versión completa:** inicio de sesión con cookie y cuentas sembradas; un flujo sembrado y compilado; un evento con clave de integración crea la ejecución, que hace el POST al simulador; historial con los pasos.
- **CA:** CA-30a, CA-30d, CA-14a, CA-04a, CA-16a, CA-90a, CA-12a.
- **Arnés mínimo:** CI (`gate`), PostgreSQL por job, reloj falso, `tests/Support/`, freeze con CODEOWNERS, `trace` contra `harness/scope.json`, registro de tareas, AR-01, AR-03 y AR-07.
- **Criterio de muerte (17 nov, carril motor):** sin demo, las dos personas siguen en H0; el día perdido sale de la holgura de H4.

**H1 — Definición (18–20 nov)**
- **Versión completa:** el Editor dibuja, valida (todos los errores), activa y versiona; un evento dispara el flujo dibujado.
- **CA:** CA-01a/b, CA-02a–d, CA-02f–i, CA-03a–d, CA-14b–d, CA-06e, CA-16f, CA-40a–f.
- **Demo:** ciclo A→B→A con su error; corregir; activar v1; enviar un evento y ver la ejecución; compilar dos veces y comparar bytes.
- **Criterio (20 nov, carril interfaz):** CA comprometidos de H1 en verde; si no, el carril motor ayuda un día y se aplican los recortes (1) y (2).

**H2 — Motor durable (21–24 nov)**
- **Versión completa:** ejecuciones que sobreviven a un reinicio, temporizadores, reintentos con esperas, sin duplicados, con dos procesadores; roles aplicados en API e interfaz.
- **CA:** CA-07a, CA-09a/b, CA-10a–c, CA-11a/b, CA-13a, CA-13c, CA-16b–e, CA-18a–c, CA-91a–c, CA-12b, CA-30b, CA-30c, CA-30e, CA-30f.
- **Demo:** `kill -9` durante una espera, reiniciar y continuar sin repetir pasos; un servicio que falla dos veces y acierta a la tercera; un Lector que no puede activar.
- **Criterio de muerte (24 nov, carril motor):** CA-10a/b en verde. Si no: recortes (1) a (4), las dos personas al motor y plan B técnico (reclamo por sondeo sin concesión, con una instancia; CA-13c queda pendiente). No se tocan ADR-03, RN-06, CA-10c, CA-11a/b ni el arnés.

**H3 — Ejemplo completo (25–28 nov)**
- **Versión completa:** el flujo de ejemplo instalado y ejecutándose con condiciones, contexto, correo y SMS; interfaz del simulador con respuestas configurables.
- **CA:** CA-04b–d, CA-05a/b, CA-06a, CA-06f, CA-06g, CA-16g, CA-19a–c, CA-90b–g, CA-N07a.
- **Demo:** `user.registered` con el receptor respondiendo `{"active": false}` (llegan correo y SMS) y `{"active": true}` (no llega nada).
- **Criterio (28 nov, ambos):** si la demo falla, se aplican los recortes en orden.

**H4 — Cierre (29 nov–2 dic)**
- Instalación en 15 min (CA-N14a), dos cambios de requisito controlados (RH-11), resultados de RH-09, métricas M1 a M6, README y presentación. Congelación de funcionalidades el 30 de noviembre.

### 2.4 Puntos de control

| Fecha | Responsable | Medición | Decisión |
|---|---|---|---|
| 17 nov | Carril motor | Demo de H0 | Sin demo: se sigue en H0 |
| 20 nov | Carril interfaz | CA comprometidos de H1 en verde | Si no: recortes (1) y (2) |
| 24 nov | Carril motor | CA-10a/b en verde | Si no: recortes (1) a (4) y plan B técnico |
| 28 nov | Ambos | Demo de H3 | Si falla: recortes en orden |
| 30 nov | Ambos | Congelación | Solo correcciones |

La medición no es «CA por día», que un agente puede inflar: son los CA comprometidos y los del hito en curso en verde. Los recortes siguen la lista de requirements.md, sección 1.

---

## 3. MVP-0 (alcance comprometido)

**Comprometidos (18):** CA-40a, CA-40d, CA-40e, CA-02a, CA-02c, CA-02g, CA-14a, CA-14b, CA-14d, CA-03b, CA-04a, CA-07a, CA-09a, CA-10b, CA-16a, CA-16b, CA-30a, CA-19b.

**Soporte (3):** CA-10a (reanudación), CA-12a (historial), CA-90a (el simulador recibe).

Cubren las cuatro tareas del enunciado y el acceso: editor, validador y compilador, motor con esperas y reintentos, webhook con simulador e inicio de sesión. CA-19b une todo en el ejemplo del enunciado. Viven en `harness/scope.json` (protegido).

## 4. Decisiones del arnés

| # | Decisión |
|---|---|
| 1 | 2 personas + agentes, en dos carriles (motor e interfaz). |
| 2 | Oráculo con separación parcial: independiente para los CA comprometidos; después, mismo agente con revisión por muestreo. |
| 3 | La sección 3 de ambas versiones de arquitectura está protegida, igual que ambas versiones de requisitos, las pruebas de aceptación y `harness/scope.json`. |
| 4 | El experimento (RH-04, RH-08 a RH-11, RH-14) se mantiene con el alcance reducido; el corpus, las respuestas grabadas y el plugin como cambio controlado (antes RH-15 a RH-18) se retiran con los módulos que los usaban. |
