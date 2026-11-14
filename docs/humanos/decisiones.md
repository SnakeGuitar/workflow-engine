# Decisiones

## De producto

| ID | Decisión | Estado |
|---|---|---|
| D-01 | **Los flujos del MVP son grafos sin ciclos.** Las repeticiones acotadas (recordatorios, reintentos) son propiedades de los nodos, no ciclos del diagrama. | Confirmado (2026-11-12) |
| D-02 | El dueño registra manualmente la respuesta del cliente (alternativa para Correo y SMS). | Modificado el 2026-11-13 por D-21 (antes: única vía en el MVP) |
| D-03 | El MVP atiende a **un negocio** por instalación. El aislamiento entre negocios es EXT. | Confirmado (2026-11-12) |
| D-04 | Las notificaciones al dueño son solo entradas en la bandeja. | Confirmado (2026-11-12) |
| D-05 | Canales de aviso: Correo, SMS y WhatsApp en el MVP (SMS y WhatsApp simulados, D-15); Telegram y WhatsApp real en EXT. | Modificado el 2026-11-13 (antes: Correo y Telegram) |
| D-06 | El editor visual es obligatorio, mínimo y solo para el operador. | Confirmado por el usuario (el profesor pidió una propuesta) |
| D-07 | La generación de flujos por agentes queda fuera del MVP (EXT). | Confirmado por plazo; restablecida el 2026-11-13 por D-14 |
| D-08 | Correo es el canal base. Telegram pasa a EXT (D-15). | Modificado el 2026-11-13 |
| D-09 | Los procesos son **paquetes declarativos** desde el MVP (RF-50, RF-51); «Reparaciones» es el paquete de referencia, no código. | Propuesto por el analista; confirmar |
| D-10 | El sistema mantiene su propio registro de casos, **derivado** de los artefactos con ingesta de solo lectura. No reemplaza los hábitos del negocio. La escritura de vuelta a Excel o Drive es FUT. | Propuesto por el analista; confirmar |
| D-11 | Los agentes producen **datos** (casos candidatos, propuestas), **nunca código ni activaciones**. Toda activación requiere confirmación humana (RN-15). | Propuesto por el analista; confirmar |
| D-12 | *(Reemplazada por D-14.)* La etapa MVP-V (MVP-visión) ya no existe. | Retirada el 2026-11-13 |
| D-13 | El escenario de tres locales, con datos sintéticos, es la demostración y el corpus de pruebas de la visión (EXT). | Propuesto por el analista; confirmar |
| D-14 | El descubrimiento desde artefactos, los pasos de agente y las propuestas pasan de la etapa MVP-V a **EXT**. El MVP se alinea con las cuatro tareas del enunciado. | Propuesto por el analista; confirmar |
| D-15 | Los servicios de terceros del MVP se **simulan**: correo, SMS, WhatsApp y HTTP POST. El simulador modela las restricciones de WhatsApp. WhatsApp real, Telegram y el aviso asistido son EXT. | Propuesto por el analista; confirmar |
| D-16 | El simulador es un componente del producto y también el doble de pruebas del arnés: un solo componente (RF-90, RF-91). | Propuesto por el analista; confirmar |
| D-17 | Monolito modular y PostgreSQL, sin Redis. La ejecución es una máquina de estados finita; no se usa Saga porque no hay transacciones distribuidas. Lo recomendado en el enunciado (microservicios, Redis, Saga) no es obligatorio: se adopta solo lo que los requisitos justifican. | Propuesto por el analista; confirmar |
| D-18 | El proyecto se publica como código abierto; la licencia se decide en OQ-15. | Propuesto por el analista; confirmar |
| D-19 | El MVP incluye dos paquetes reales: «Reparaciones» y «Citas». Encargos, cotizaciones y talleres son paquetes de datos posteriores (EXT). | Decidido por el equipo (2026-11-13); antes, el segundo paquete era EXT |
| D-20 | Entran al MVP los temporizadores anclados a una fecha del caso y el filtro y la búsqueda por campos propios (RF-17, RF-51 pasa a M). | Decidido por el equipo (2026-11-13) |
| D-21 | Entran al MVP las respuestas del cliente por el canal (WhatsApp y Messenger), con interpretación solo por valores declarados en el flujo. | Decidido por el equipo (2026-11-13) |
| D-22 | Facebook entra como canal (Messenger) y como fuente de consultas (Marketplace), ambos simulados en el MVP; los reales son EXT. | Decidido por el equipo (2026-11-13) |

## De arquitectura: razones, alternativas y límites

Cada ADR vigente está en [../arquitectura.md](../arquitectura.md). Aquí va lo que explica por qué.

### Fuerzas que moldean la arquitectura

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

### ADR-01 Monolito modular, un solo despliegue
- **Motivo:** el enunciado original sugería microservicios, patrón Saga y bloqueos distribuidos. Ningún requisito del MVP lo exige: todo el estado vive en una base de datos y las transacciones son locales. Con 2 personas y 20 días, los servicios separados añaden fallos de red, contratos y despliegue sin aportar nada medible.
- **Alternativas descartadas:** microservicios (costo sin beneficio); Saga (no hay transacciones distribuidas en el MVP: el "deshacer" no existe en este proceso).
- **Sacrificio:** escalado independiente por componente. Se acepta.

### ADR-02 Plataforma: .NET + PostgreSQL + React/TypeScript
- **Motivo:** el equipo domina .NET y aprendió arquitectura con esa plataforma (RH-12). C# tiene tipado fuerte, buen soporte de los agentes y herramientas maduras para hacer cumplir reglas de estructura (sección 6).
- **Alternativa considerada:** Blazor para tener todo en C#. **Descartada por decisión del equipo (2026-11-13)**, que lo considera poco maduro para este proyecto. Hay librerías de diagramas para Blazor (por ejemplo Z.Blazor.Diagrams), pero no se verificó su encaje con RF-40 ni el desempeño de los agentes con ellas (H-27); se deja constancia por si se revisa más adelante. Como el frontend queda tras el contrato de la API (ADR-08), cambiarlo después es de bajo costo.
- **Sacrificio:** dos lenguajes. La parte de TypeScript es delgada y la acota el contrato de la API (ADR-08).

### ADR-03 Persistencia y ejecución durables sobre PostgreSQL
- **Sustituye a:** Redis y bloqueos distribuidos del enunciado. Un solo almacén transaccional basta y evita la inconsistencia entre dos almacenes.

### ADR-05 Canales como puertos; simulados en el MVP, reales como plugins
- **Sacrificio:** las restricciones modeladas son mi lectura de las reglas de la plataforma (H-40); un adaptador real podría revelar diferencias.

### ADR-06 Los flujos son datos validados, y el motor los interpreta
- **Descartado:** ejecutar flujos como código generado (no se puede validar ni auditar tan bien).

### ADR-07 Capas rígidas; los dominios son paquetes de datos
- **Motivo:** el motor debe ser genérico (propósito), el sistema debe adaptarse a cada negocio sin reprogramarse (RF-50, D-09) y las reglas deben poder verificarse por máquina (RH-07).

### ADR-08 API HTTP con contrato y sesión por cookie
- **Motivo:** RF-30, RNF-07, y que los agentes no desalineen frontend y backend.

### ADR-11 Paquetes de proceso declarativos (plugins de datos)
- **Descartado:** paquetes como código; generar código desde propuestas de agentes (D-11).

### ADR-12 (EXT) El descubrimiento se modela como flujos con pasos de agente
- **Ventaja:** el descubrimiento demuestra el motor. **Riesgo:** acopla el descubrimiento al calendario del motor; por eso queda en EXT (D-14).

### ADR-15 Simulador de APIs de terceros
- **Motivo:** el enunciado pide simular APIs de terceros; RF-90 a RF-93, RN-24 a RN-28, RNF-08, D-15, D-16.
- **Descartado:** un doble solo para pruebas y nada para la demostración (el enunciado pide el simulador).

### ADR-16 Compilación a DAG y máquina de estados de la ejecución
- **Motivo:** RF-02, RF-14, RF-18, RN-10, RN-30; tareas del enunciado.
- **Saga no se usa** (ADR-18).

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
