# Historial de cambios

## requisitos.md

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-11-12 | Borrador provisional inicial. Sin validación con usuarios. |
| 0.2 | 2026-11-12 | Confirmadas las decisiones D-01 a D-05. Sin cambios en RF, RN, RNF ni CA. |
| 0.3 | 2026-11-12 | RH-07 enlaza con las reglas AR-01 a AR-09 de la arquitectura. Añadida H-25. Sin cambios en RF, RN, RNF ni CA. |
| 0.4 | 2026-11-13 | Canales Correo y Telegram. Nuevos RF-31 y RF-32 (CA-31a a CA-32c), glosario, D-08, H-28, RI-07. RN-07 ampliada para Telegram (iniciar la conversación cuenta como consentimiento). Nota bajo RF-20: el contacto se valida según el canal; el significado de los CA-20a a CA-20d no cambia. |
| 0.5 | 2026-11-13 | Confirmada la decisión D-08. Sin cambios en RF, RN, RNF ni CA. |
| 0.6 | 2026-11-13 | Visión de adaptación a cada negocio: escenario de tres locales, etapa MVP-V, paquetes de proceso (RF-50, RF-51), pasos de agente (RF-15), fuentes, extracción y propuestas (RF-60 a RF-63, RF-70 a RF-72), RN-15 a RN-23, RNF-10 a RNF-12, RH-15 a RH-17, D-09 a D-13, H-29 a H-33. **Cambios de significado:** RN-01 y CA-21a se generalizan del paquete «Reparaciones» a «el paquete activo» (el comportamiento de «Reparaciones» no cambia). D-07 modificada. Sección 6 pasa a ser el paquete de referencia. |
| 0.7 | 2026-11-13 | Alineación con el enunciado del profesor y con la visión abierta y extensible. Nuevos: sección 1.1, RF-14 (compilación a DAG), RF-16 (webhook saliente), RF-18 (estados de la ejecución), RF-33 a RF-35 (WhatsApp), RF-80 a RF-82 (extensibilidad), RF-90 a RF-93 (simulador), RN-24 a RN-31, RNF-13, RNF-14, RH-18, D-14 a D-18, OQ-14 a OQ-17, H-34 a H-40, RI-12, RI-13. **Cambios de etapa:** el descubrimiento y los pasos de agente pasan a EXT (la etapa MVP-V desaparece); Telegram pasa a EXT; RF-51 pasa a S. **Cambios de significado:** RF-06 (catálogo de acciones registradas), CA-06c, RF-31, CA-31b, CA-31c y RN-07 se generalizan para admitir varios canales y paquetes. D-05, D-07, D-08 y D-12 modificadas. |
| 0.8 | 2026-11-13 | El profesor indicó que la arquitectura del enunciado es una recomendación, no una exigencia: se ajustan D-17, OQ-16, RI-13 y la fila de arquitectura de la sección 1.1. Sin cambios en RF, RN, RNF ni CA. |
| 0.9 | 2026-11-13 | Reestructuración de la documentación (ver más abajo). Sin cambios en RF, RN, RNF ni CA. |
| 0.10 | 2026-11-13 | Generalidad para citas, encargos, cotizaciones y talleres, y Facebook. Nuevos: RF-17, RF-36 a RF-38, RF-52, RF-53, RF-94, RN-32 a RN-38, glosario, sección 6.2 (paquete «Citas»), D-19 a D-22, H-41 a H-43, OQ-18, OQ-19, RI-14 a RI-16, N-10. **Cambios de etapa:** el segundo paquete y las respuestas del cliente pasan de EXT a MVP; RF-51 pasa de S a M. **Cambios de significado:** RN-10 (temporizador anclado), RN-07 (Messenger), RF-04, RF-05, RF-31, RF-90, RF-82 y CA-06c. D-02 modificada. |
| 0.11 | 2026-11-13 | Correcciones del informe de revisión. **Cambios de significado:** RN-05 (el paso que origina el cambio no se cancela), RN-08 (las entradas de bandeja no se difieren), RF-04 (la creación no emite evento de estado), CA-06c (en «Citas», Agendada deja de ser asignable por un flujo), RH-01 (la meta pasa a ser el alcance comprometido), RH-02 (protege la sección 3 de arquitectura). **Sin cambio de significado:** RN-01 (se elimina la coletilla redundante), §6 y §6.2 (esquema de estados y asignabilidad), escalera de recortes (SMS deja de ser recortable; el quinto recorte pasa a CA-94c y CA-94d), CA-90e y formato `[HUMANO]`. |
| 0.12 | 2026-11-13 | Correcciones de la auditoría del 2026-11-13 (ver [informes/2026-11-13-auditoria.md](informes/2026-11-13-auditoria.md)). Nuevo: CA-02i. **Cambios de significado:** RN-05 (esperas programadas antes del cambio, incluidos avisos diferidos o en reintento; excepción para flujos que sobreviven al cambio de estado; una ejecución en curso por flujo y caso), RN-10 (un nodo que no es Condición tiene como máximo una salida), RN-28 (estado «cancelado»), RN-38 (el flujo sobrevive al cambio de estado), RF-04 (un caso se crea en el estado inicial; los efectos automáticos emiten evento), RH-01 y RH-02 (`harness/scope.json`), escalera de recortes (el quinto recorte pasa a RF-41; los recortes 6 y 7 exigen rebajar la prioridad; «Citas», RF-17 y Messenger dejan la lista de no recortables). **Sin cambio de significado:** §6 y §6.2 (asignables por una persona, efectos automáticos y estado inicial de «Reparaciones»), RF-06 (terminología), RH-07 (todas las AR), evento «caso creado» añadido a EXT. |

## arquitectura.md

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-11-12 | Propuesta inicial |
| 0.2 | 2026-11-13 | AD-01 resuelta: frontend en React. Se documenta el descarte de Blazor y se ajusta AR-R1. |
| 0.3 | 2026-11-13 | AD-02 resuelta: canales Correo y Telegram. ADR-05 con tres adaptadores, sondeo para la vinculación y nuevo riesgo AR-R6. |
| 0.4 | 2026-11-13 | Hacia la visión de adaptación a cada negocio: `Repairs` pasa a ser `Cases` más un paquete de datos; nuevo módulo `Discovery`; ADR-11 a ADR-14; AR-10 a AR-14; ciclo del descubrimiento; calendario con punto de decisión el 25 de noviembre; AR-R7 a AR-R10; AD-05 y AD-06. |
| 0.5 | 2026-11-13 | Alineación con el enunciado y con WhatsApp: ADR-05 reescrita (canales simulados con política de WhatsApp), ADR-15 (simulador), ADR-16 (compilación a DAG y máquina de estados), ADR-17 (plugins), ADR-18 (desviaciones del enunciado), AR-15 a AR-18, AR-R11 a AR-R13, calendario revisado, el descubrimiento pasa a EXT. |
| 0.6 | 2026-11-13 | La arquitectura del enunciado se trata como recomendación, no como exigencia: se ajustan ADR-18, AD-03 y AR-R13. |
| 0.7 | 2026-11-13 | Reestructuración de la documentación (ver más abajo). |
| 0.8 | 2026-11-13 | ADR-19 (respuestas entrantes y consultas); ADR-03 (temporizadores anclados), ADR-05 (Messenger), ADR-06, ADR-07, ADR-11 (campos propios consultables) y ADR-15 ajustados; ciclo de vida ampliado; AR-14 cubre consultas. |
| 0.9 | 2026-11-13 | ADR-18 recuperado desde `humanos/`; ADR-20 (unidad de trabajo) y AR-19; AR-09 y ADR-09 protegen la sección 3; ADR-09 añade el oráculo separado; ADR-07 marca `Discovery` como EXT; ADR-11 declara la asignabilidad de estados. |
| 0.10 | 2026-11-13 | Correcciones de la auditoría: ADR-06 (excepción de la ejecución que origina el cambio, un único nodo actual, ejecuciones reemplazadas), ADR-07 (puertos de `Core` y referencias de `Api`), ADR-09 (aprobación por persona revisora, `harness/scope.json`, oráculo sobre los CA comprometidos, `tests/Support/`), ADR-11 (efectos automáticos en el paquete), ADR-20 (anidamiento), ciclo de vida, estructura de la solución; AR-08, AR-09 y AR-19 ajustadas. |

## Reestructuración de la documentación (2026-11-13)

- requisitos.md 0.9 y arquitectura.md 0.7: se separa lo que implementa un agente de lo que lee una persona. Esto pasó a `humanos/`: decisiones D, preguntas OQ, hipótesis H, riesgos, supuestos, necesidades, trazabilidad, calendario, razones y alternativas de los ADR, y estos historiales. Sin cambios en RF, RN, RNF, CA ni AR.
- RH-07 apuntaba a AR-01 a AR-09; se corrige a AR-01 a AR-18.
- Las referencias a D, H, OQ, RI y AD se quitaron de los documentos de implementación.
- Arquitectura: las secciones 1, 4, 5, 7, 8 y 9 pasaron a `humanos/`; la antigua sección 6 pasa a ser la 3 (las referencias externas a «sección 6» pasan a «sección 3»).
