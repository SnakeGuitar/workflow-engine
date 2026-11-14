# Preguntas abiertas, hipótesis y supuestos

## Preguntas abiertas

| ID | Pregunta |
|---|---|
| OQ-01 | ¿Qué política legal aplica a equipos abandonados? Los 30 días de RN-04 son un valor provisional. |
| OQ-02 | ¿Cómo se maneja una reparación en garantía? |
| OQ-03 | ¿Se puede cancelar un caso En reparación? Hoy no. |
| OQ-04 | ¿Puede entregarse un equipo ya marcado Abandonado? Hoy es terminal. |
| OQ-05 | ¿Cómo se registra el consentimiento del cliente (verbal, firma, mensaje)? |
| OQ-06 | ¿Cómo se trata el cambio de horario de verano en la ventana horaria? |
| OQ-07 | ¿Qué rúbrica aplica el profesor y qué dice sobre uso de agentes (H-20)? |
| OQ-08 | ¿Cuántas tareas y modelos caben en el presupuesto de uso (H-21)? |
| OQ-09 | ¿Los valores de RN-03, RN-04 y RNF-01 coinciden con lo que los dueños dicen en entrevista? |
| OQ-10 | ¿Qué proveedor de modelo se usa y con qué presupuesto (H-21)? |
| OQ-11 | ¿El umbral de confianza de 0,7 es adecuado? Se calibra con el corpus (RH-16). |
| OQ-12 | ¿Cómo se leen las imágenes: visión del modelo o reconocimiento de texto aparte? |
| OQ-13 | ¿Qué política de salida de datos aplica por defecto: habilitar fuente por fuente (propuesto) u otra? |
| OQ-14 | Si se vende el servicio, ¿qué condiciones de uso y de privacidad aplican por los datos de los clientes de cada negocio? |
| OQ-15 | ¿Qué licencia de código abierto se usa? |
| OQ-16 | *(Resuelta el 2026-11-13: el profesor indicó que la arquitectura del enunciado es una recomendación, no una exigencia.)* |
| OQ-17 | ¿Cuál es el tope mensual de WhatsApp por defecto y qué tarifas se simulan (OQ-10 aplica a modelos)? |
| OQ-18 | ¿Qué horario de atención y qué días no laborables tiene un negocio con citas? Hoy solo hay ventana horaria de avisos. |
| OQ-19 | ¿Hacen falta citas recurrentes o por varios recursos (varios profesionales)? Hoy no. |

## Hipótesis a verificar

H-03 El dueño no es técnico. H-15 La API oficial de WhatsApp exige plantillas aprobadas y plazos de verificación. H-16 El dolor principal es el olvido y el seguimiento. H-17 Los clientes aceptan recibir avisos. H-20 La política académica admite código generado por agentes. H-21 Las suscripciones permiten el número de ejecuciones del experimento. H-22 Los equipos no recogidos son un problema frecuente. H-23 Un registro de 15 s compite con la libreta. H-24 Los operadores prefieren plantillas editables a flujos escritos desde cero. H-25 Las versiones de plataformas y paquetes citadas en arquitectura.md son las vigentes. H-28 Un bot de Telegram no puede iniciar una conversación con un usuario hasta que este la inicia; por eso hace falta la vinculación (RF-32). Es mi conocimiento y debe verificarse. H-29 Enviar datos del negocio a un proveedor de modelo es aceptable para los dueños si cada fuente se habilita explícitamente y los contactos se enmascaran. H-30 Los chats de WhatsApp solo pueden ingresarse como exportación manual: la API oficial no da acceso al historial de la aplicación (conocimiento propio; verificar). H-31 Los modelos extraen casos con precisión suficiente de fotos de libreta manuscrita (desconocido). H-32 El dueño reconoce sus procesos en una propuesta y le aporta valor; en el escenario de tres locales el proceso es casi el mismo y el valor está en la extracción (no verificada). H-33 Los artefactos de los tres locales descritos son representativos de negocios reales (los plantea el equipo; no hay datos verificados). H-27 Las librerías de diagramas para Blazor no se evaluaron: su encaje con RF-40 y el desempeño de los agentes con ellas son desconocidos. H-34 La licencia de n8n restringe ofrecerlo como servicio alojado a terceros, y la consultoría sobre él está permitida (verificar con n8n). H-35 Los dueños pagarían por el servicio. H-36 Una persona puede dar soporte a varios locales. H-37 La titularidad del código generado por agentes permite publicarlo con la licencia elegida (revisar los términos de cada proveedor). H-38 Existe un hueco frente a alternativas abiertas (validación previa, paquetes adaptables, descubrimiento) y hay desarrolladores que querrían extender el motor. H-39 Un enlace de chat con texto prellenado permite al dueño enviar por WhatsApp con un toque. H-41 Las APIs de Meta permiten a una aplicación de terceros recibir los mensajes de Marketplace de un negocio y responderlos; no verificado y es la mayor duda de RF-38 y RF-82. H-42 Las reglas de Messenger son las modeladas en RN-36 (ventana de 24 h, sin plantillas); no verificado. H-43 Encargos, cotizaciones y talleres se cubren con paquetes de datos sin cambiar el motor; no probado. H-40 Las reglas de WhatsApp (ventana de 24 h, consentimiento, plantillas aprobadas, costo por mensaje) son las modeladas en RN-24 a RN-27; solo plantillas, verificación de la cuenta y costo por mensaje aparecen en las fuentes consultadas.

## Decisiones de arquitectura abiertas

| ID | Pregunta |
|---|---|
| AD-01 | **Resuelta:** frontend en React con TypeScript y editor con React Flow (decisión del equipo, 2026-11-13). |
| AD-02 | **Resuelta:** ambos canales, correo como base y Telegram recortable (D-05, D-08). Falta elegir el servicio SMTP concreto. |
| AD-03 | ¿Qué valora la rúbrica del profesor? La arquitectura del enunciado es una recomendación, no una exigencia. (OQ-07) |
| AD-04 | ¿Dónde se ejecutará la demostración final (máquina local o servidor)? |
| AD-05 | ¿Qué proveedor de modelo y con qué presupuesto? (OQ-10) |
| AD-06 | ¿Se evalúa un modelo local para privacidad? No en el MVP; la interfaz lo permite (ADR-13). |
| AD-07 | ¿Qué licencia de código abierto? (OQ-15) |

## Supuestos (a verificar)
- S-01 El taller cuenta con teléfono con navegador y conexión a internet.
- S-02 El cliente final aporta un contacto válido y acepta recibir avisos (H-17).
- S-03 Correo y SMS entregan mensajes sin aprobación previa de plantillas; WhatsApp sí la exige y se modela en el simulador (RN-24, RN-25, D-15).
