# Decisiones

## De producto

| ID | Decisión | Estado |
|---|---|---|
| D-23 | **El alcance es el enunciado ([project-idea.txt](../../project-idea.txt)):** editor de nodos, analizador y compilador a DAG, motor asíncrono con esperas y reintentos, y webhooks con simulador de correo, SMS y HTTP POST. Fuera de eso solo entra el acceso de D-25. | Decidido por el equipo (2026-11-15) |
| D-24 | El caso de demostración es el ejemplo del propio enunciado (registro de usuario → esperar 2 h → verificar → correo y aviso a soporte), entregado como flujo de ejemplo (RF-19). Ningún dominio de negocio concreto. | Decidido por el equipo (2026-11-15) |
| D-25 | **Inicio de sesión con roles acotados:** dos roles fijos (Editor y Lector), cuentas creadas al instalar desde la configuración, sin gestión de usuarios en la interfaz. El sistema origen usa una clave de integración. | Decidido por el equipo (2026-11-15) |
| D-26 | **Se mantiene el arnés y el experimento** (RH-01 a RH-14), adaptados al nuevo alcance. Plan operativo en [arnes-e-hitos.md](arnes-e-hitos.md). | Decidido por el equipo (2026-11-15) |
| D-01 | Los flujos son grafos sin ciclos. Las repeticiones acotadas (reintentos) son propiedades del motor, no ciclos del diagrama. | Confirmado (2026-11-12) |
| D-06 | El editor visual es obligatorio y mínimo. | Confirmado |
| D-15 | Los servicios de terceros se **simulan**: correo, SMS y HTTP POST. | Ajustado el 2026-11-15 por D-23 |
| D-16 | El simulador es un componente del producto y también el doble de pruebas del arnés. | Confirmado |
| D-17 | Monolito modular y PostgreSQL, sin microservicios, Saga ni Redis; la ejecución es una máquina de estados finita. Justificación en [ADR-18](../arquitectura.md#adr-18-no-se-adopta-la-arquitectura-recomendada-por-el-enunciado). | Confirmado (el profesor indicó que la arquitectura del enunciado es una recomendación) |

Las demás decisiones (D-02 a D-05, D-07 a D-14, D-18 a D-22: reparaciones, citas, canales de Meta, descubrimiento con agentes, código abierto) quedan **retiradas por D-23**. Están en el historial de git. Las decisiones propias del arnés están en [arnes-e-hitos.md](arnes-e-hitos.md), sección 4.

## De arquitectura: razones y alternativas

Cada ADR vigente está en [../arquitectura.md](../arquitectura.md). Aquí va lo que explica por qué.

### Fuerzas que moldean la arquitectura

| Fuerza | Origen | Consecuencia |
|---|---|---|
| Esperas que sobreviven a reinicios sin duplicar envíos | RF-10, RF-11, RN-06 | Estado de ejecución y esperas **persistentes y transaccionales** (ADR-03) |
| Verificar esperas de horas sin esperar | RNF-08, CA-07a | El tiempo es una **dependencia inyectable** (ADR-04) |
| Terceros que fallan | RF-09, RN-29 | Acciones como **puerto**, simulador con inyección de fallos (ADR-05, ADR-15) |
| Tareas técnicas del enunciado | T1 a T4 | Compilador a DAG, intérprete, máquina de estados, webhook y simulador (ADR-15, ADR-16) |
| Agentes que programan bajo un arnés medido | RH-01 a RH-14 | Estructura **rígida y verificable por máquina**, tecnología conocida, oráculo separado (ADR-09) |
| 2 personas, 17 días | R-02 | Lo mínimo que satisface los requisitos; nada de infraestructura distribuida (ADR-18) |
| Las personas deben defender el código | — | Plataforma que el equipo domina (ADR-02) |

### ADR-02 Plataforma
- **Motivo:** el equipo domina .NET; C# tiene tipado fuerte, buen soporte de los agentes y herramientas maduras para hacer cumplir reglas de estructura.
- **Descartado:** Blazor para tener todo en C# (el equipo lo considera poco maduro para el editor de nodos). Como la interfaz queda tras el contrato OpenAPI (ADR-08), cambiarla después es barato.
- **Sacrificio:** dos lenguajes; la parte de TypeScript es delgada.

### ADR-03 Persistencia sobre PostgreSQL
- **Sustituye a:** Redis y bloqueos distribuidos. Un solo almacén transaccional basta y evita la inconsistencia entre dos almacenes (ADR-18).

### ADR-06 Flujos como datos interpretados
- **Descartado:** generar código a partir del diagrama (no se puede validar ni auditar tan bien).

### ADR-15 Simulador
- **Descartado:** un doble solo para pruebas y nada para la demostración; el enunciado pide el simulador.

### ADR-18 Arquitectura recomendada
- **Riesgo:** la rúbrica podría valorar la arquitectura recomendada aunque no la exija (RI-13). Respuesta preparada: la tabla de ADR-18, CA-13c como demostración de que no hace falta Redis, y el camino para escalar (más instancias del procesador contra la misma base).
