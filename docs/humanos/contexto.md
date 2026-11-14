# Contexto, trazabilidad y calendario

## Contexto del producto

**Dos problemas distintos.** (i) *El proceso:* qué estados atraviesa un trabajo. (ii) *La fuente de verdad:* dónde queda registrado. En el escenario de abajo el proceso es casi el mismo y lo que cambia es la fuente; por eso el primer valor está en la **extracción hacia un modelo común de caso**. El **descubrimiento de procesos distintos** cobra más peso en oficios cuyo proceso sí varía entre negocios (H-32).

**Competencia real a superar:** la libreta, la hoja de cálculo y las notas del teléfono. La libreta es memoria pasiva; el sistema debe ser **memoria que actúa** (avisa, recuerda, vigila) con un costo de registro comparable al de apuntar a mano.

## Trazabilidad con el enunciado del profesor

| Tarea técnica del enunciado | Requisitos |
|---|---|
| Interfaz gráfica interactiva para arrastrar, soltar y conectar nodos (Eventos, Condiciones, Acciones) | RF-40, RF-41; CA-40a, CA-40d |
| Analizador en el backend que valida que no haya ciclos y transforma el diagrama en un JSON ejecutable (DAG) | RF-02, RF-14, RN-10 |
| Motor de ejecución asíncrono que evalúa condiciones y ejecuta acciones, con retrasos y reintentos automáticos ante fallos de red | RF-04 a RF-13, RF-18, RN-05, RN-09, RN-30 |
| Webhooks salientes y simulación de APIs de terceros (correos, SMS, HTTP POST) | RF-16, RF-90 a RF-92, RN-29 |
| Arquitectura recomendada (máquina de estados finita, Saga, microservicios; PostgreSQL y Redis) | Es una **recomendación**, no un requisito (D-17): se adoptan la máquina de estados finita (RN-30), el DAG y PostgreSQL; microservicios, Saga y Redis no se usan, con justificación en ADR-18 |

## Necesidades (hipótesis, no validadas por entrevistas)

| ID | Necesidad | Hipótesis asociada |
|---|---|---|
| N-01 | El dueño olvida avisar al cliente y dar seguimiento a cotizaciones sin respuesta | H-16 |
| N-02 | Los equipos listos que no se recogen ocupan espacio y dinero inmovilizado | H-22 |
| N-03 | La libreta no avisa nada; todo depende de la memoria del dueño | H-16 |
| N-04 | Registrar un trabajo debe ser tan rápido como apuntarlo | H-23 |
| N-05 | El dueño no tiene conocimientos técnicos y no debe ver conceptos técnicos | H-03 |
| N-06 | El operador (equipo) necesita definir y verificar procesos sin programar cada uno | H-24 |
| N-07 | Cada negocio registra sus trabajos de forma distinta y no quiere cambiar sus hábitos | H-33 |
| N-08 | El dueño no sabe formalizar sus procesos, pero sus propios artefactos los revelan | H-32 |
| N-09 | Un integrador o desarrollador necesita añadir acciones, canales o fuentes sin modificar el motor | H-38 |
| N-10 | Un negocio que agenda necesita recordatorios relativos a la fecha de la cita y que el cliente confirme o cancele por el mismo canal | H-43 |

## Trazabilidad necesidad → requisito

| Necesidad | Requisitos | Reglas |
|---|---|---|
| N-01 | RF-04, RF-06, RF-07, RF-25, RF-31, RF-32 | RN-02, RN-03, RN-05 |
| N-02 | RF-25, RF-28, RF-29 | RN-04, RN-13 |
| N-03 | RF-04, RF-07, RF-29 | RN-03, RN-04 |
| N-04 | RF-20, RF-24, RNF-01 | RN-14 |
| N-05 | RNF-01, RNF-02, RF-30 | (CA-N01c) |
| N-06 | RF-01, RF-02, RF-03, RF-40, RF-41 | RN-10, RN-11 |
| N-07 | RF-15, RF-50, RF-51, RF-60 a RF-63 | RN-17 a RN-20 |
| N-08 | RF-15, RF-70 a RF-72 | RN-15, RN-16, RN-21 |
| N-09 | RF-80 a RF-82, RNF-13 | RN-31 |
| Enunciado del profesor | RF-02, RF-14, RF-16, RF-18, RF-40, RF-90 a RF-92 (ver sección 1.1) | RN-10, RN-29, RN-30 |
| WhatsApp con restricciones | RF-31, RF-33, RF-35, RF-92, RF-93 | RN-24 a RN-28 |
| Confiabilidad del motor | RF-08 a RF-13, RNF-04 a RNF-06 | RN-05, RN-06, RN-09, RN-12 |

## Cómo se cubre cada requisito en la arquitectura

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

## Orden de construcción y calendario (estimación)

> **Sustituido** por el calendario de hitos de [informes/2026-11-13-arnes-e-hitos.md](informes/2026-11-13-arnes-e-hitos.md), sección C.2. Esta tabla se conserva como la estimación inicial.

Es una estimación mía, no un compromiso; la validación real llega al primer hito.

| Fechas | Hito | Mide (RH-10) |
|---|---|---|
| 13 a 15 nov | Esqueleto y **arnés**: proyectos `Core`, `Cases`, `Sdk`, `Infrastructure`, `Api` y `Simulator`; reglas AR; reloj falso; simulador mínimo (correo y HTTP POST) con inyección de fallos; etiquetado de CA; protección de archivos; registro de tareas; `AGENTS.md` | Línea base |
| 16 a 21 nov | Analizador y compilador a DAG (RF-02, RF-14), temporizador anclado (RF-17), motor asíncrono (RF-03 a RF-13, RF-18) y acción webhook (RF-16) | 25 % de los RF de prioridad M |
| 22 a 25 nov | Casos y paquetes «Reparaciones» y «Citas» (RF-20 a RF-31, RF-50, RF-52, RF-53), filtros por campo propio (RF-51), respuestas del cliente (RF-36), canales Correo, SMS y WhatsApp con restricciones sobre el simulador (RF-35, RF-90 a RF-92), API y autenticación | 50 % |
| 26 a 29 nov | Canal Messenger y consultas de Marketplace (RF-37, RF-38, RF-94), editor visual (RF-40, RF-41), vista del dueño (RNF-01, RNF-02), estados de entrega y aprobación de plantillas (RF-33, RF-93), plugin de ejemplo y preparación para open source (RF-80 a RF-82, RNF-13) | 100 % |
| 30 nov a 2 dic | Cambios de requisito controlados (RH-11, RH-18), cierre de métricas y documento final | Resultados |

**Advertencia:** estimación optimista. Si algo debe ceder, se recorta según la escalera de la sección 2 de requisitos.
