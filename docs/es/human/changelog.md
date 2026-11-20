# Historial de cambios

## requirements.md

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1–0.13 | 2026-11-12 a 13 | Especificación ampliada más allá del enunciado: paquetes «Reparaciones» y «Citas», canales WhatsApp, Messenger y Telegram, bandeja del dueño, plugins, descubrimiento con agentes (EXT) y experimento con arnés. El detalle está en git (último commit de esa etapa: `79bf38e`). |
| 1.0 | 2026-11-15 | **Reestructuración al alcance del enunciado más el acceso con roles (D-23 a D-26).** Ver la sección siguiente. |

## architecture.md

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1–0.11 | 2026-11-12 a 13 | Arquitectura para la especificación ampliada (módulos `Cases`, `Discovery`, `Sdk`; ADR-01 a ADR-21). Detalle en git. |
| 1.0 | 2026-11-15 | Reestructuración al nuevo alcance. Ver la sección siguiente. |

## Reestructuración 1.0 (2026-11-15)

El alcance pasa a ser el de `project-idea.txt` más el inicio de sesión con dos roles fijos. El ejemplo del enunciado sustituye al seguimiento de reparaciones como caso de demostración. El arnés y el experimento se mantienen, adaptados. El informe de arnés e hitos del 2026-11-13 pasa a [harness-and-milestones.md](harness-and-milestones.md), reescrito para el nuevo alcance; los otros dos informes del 2026-11-13 y `abiertas.md` se eliminan (siguen en git; las preguntas vigentes pasan a `context.md`). Los diagramas de `design/diagrams/` se rehacen.

**IDs retirados** (no se reutilizan):
- **RF:** RF-08, RF-15, RF-17, RF-20 a RF-29, RF-31 a RF-38, RF-41, RF-42, RF-50 a RF-53, RF-60 a RF-63, RF-70 a RF-72, RF-80 a RF-82, RF-92 a RF-94, con todos sus CA.
- **CA de RF vigentes:** CA-02e, CA-06b, CA-06c, CA-06d, CA-07b, CA-07c, CA-12c, CA-13b, CA-14e, CA-90e.
- **RN:** RN-01 a RN-05, RN-07, RN-08, RN-13 a RN-28, RN-31 a RN-38.
- **RNF:** RNF-01 a RNF-06, RNF-10 a RNF-13, con sus CA-N; de RNF-07, CA-N07b y CA-N07c.
- **RH:** RH-15 a RH-18 (corpus, métricas de descubrimiento, respuestas grabadas y plugin como cambio controlado).
- **R:** R-03, R-05 a R-07.
- **ADR:** ADR-11 a ADR-14, ADR-17, ADR-19 a ADR-21 (la atomicidad por paso de ADR-20 queda en ADR-03.2).
- **AR:** AR-02, AR-10 a AR-15, AR-19.
- **D, OQ, H, RI, AD:** todos salvo D-01, D-06, D-15 a D-17, OQ-07, OQ-08, AD-04, RI-01, RI-03 a RI-05, RI-13 y AR-R1 a AR-R4.

**Nuevos:** RF-19 (flujo de ejemplo); RN-39 (eventos), RN-40 (condiciones), RN-41 (variables); CA-04c, CA-04d, CA-06f, CA-06g, CA-13c, CA-16g, CA-30d, CA-30e, CA-30f, CA-40f, CA-90g; D-23 a D-26.

**Cambios de significado:**
- El evento deja de ser «el caso entra a un estado» y pasa a ser un evento externo con tipo, identificador y datos (RF-04, CA-04a, CA-04b, CA-11a, CA-13a).
- La Condición tiene salidas «sí» y «no»; un dato inexistente sigue «no» (RF-05, CA-05a, CA-05b, RN-10).
- Acciones: Enviar correo, Enviar SMS y Llamar webhook (RF-06). El webhook guarda su respuesta en el contexto (RF-16).
- Una acción que agota sus reintentos o falla sin reintento deja la ejecución Fallida; desaparece «Completada con fallo» y no hay bandeja (RN-09, RN-29, §5.1, CA-09b, CA-16c).
- La ejecución deja de tener el estado Cancelada: no hay cambios de estado de un caso que la cancelen.
- RN-06: un envío es una ejecución y un nodo. CA-07a usa 2 h.
- RF-30: roles Editor y Lector en lugar de Dueño y Operador; cuentas sembradas desde la configuración; clave de integración para los eventos (CA-30a, CA-30b). RNF-07 queda en CA-N07a, ampliado a la clave de integración.
- Simulador solo con correo, SMS y HTTP POST, con respuesta configurable y sin inicio de sesión (RF-90, RF-91).
- RH-01: los CA pendientes son todos los no comprometidos (ya no hay prioridades). RH-02 y AR-09 dejan de proteger `tests/Fixtures/`. RH-10 mide sobre todos los RF. RH-11 cambia sus ejemplos.
- `harness/scope.json`: añade CA-30a y CA-19b a los comprometidos.
- AR-01: `Core` no referencia infraestructura; no existen `Cases`, `Discovery` ni `Sdk` (ADR-07). AR-05 se limita a contraseñas y clave de integración. AR-08 informa como pendientes todos los CA no comprometidos.
- ADR-08: roles Editor y Lector y clave de integración. ADR-09 sin respuestas grabadas ni evaluación con modelo real. ADR-18 desarrolla la justificación de no adoptar la arquitectura recomendada.

## Migración de idioma 1.1 (2026-11-17)

La instrucción del usuario (D-27) establece inglés para todo el proyecto y documentación en ambos idiomas. La fuente normativa pasa a `docs/en/`; la versión española está en `docs/es/`. Los nombres de archivo y los ejemplos ejecutables usan inglés en ambas versiones. Se conserva todo ID estable y el conjunto de CA comprometidos y de soporte.

- Requisitos 1.1: R-04 y CA-N09a requieren interfaz en inglés, fechas YYYY-MM-DD y hora de 24 h.
- Arquitectura 1.1: identificadores, roles, rutas de API, cabecera de integración, rutas del contexto y ejemplos en inglés; protección de ambas versiones de requisitos y de la sección 3 de ambas versiones de arquitectura.
- `harness/scope.json`: claves `spec`, `description`, `committed` y `support`; referencia a `docs/en/requirements.md 1.1`.
- Se regeneran las imágenes de los diagramas desde Mermaid en ambos idiomas.
- Todavía no hay código de aplicación ni arnés ejecutable. Se registra esta tarea manualmente; el agente no escribe una línea de aprobación en un commit.

## 1.2 — Conexiones personales

RF-100, ADR-100 y RH-100 especifican conexiones personales, operaciones de proveedores y evidencia local y real por separado.

## 1.3 — Servicios extensibles y artefactos locales

RF-101, RF-102 y ADR-101 definen la incorporación independiente de proveedores, las tareas sin red y la publicación de artefactos. La evidencia real se declara por criterio.
