# Especificación de requisitos: motor de flujos web, abierto y extensible (primer caso: microempresas)

| Campo | Valor |
|---|---|
| Versión | 0.8 (borrador provisional) |
| Fecha | 2026-11-12 |
| Fecha límite del proyecto | 2026-12-02 |
| Estado | Borrador provisional, alineado con las tareas técnicas del enunciado (sección 1.1). Los escenarios de negocio son supuestos del equipo (ver H-16, H-17, H-33) |
| Equipo | 2 personas + agentes de programación (varios proveedores) |
| Restricción de materia | Sistema web |

---

## 0. Cómo leer y usar este documento (personas y agentes)

1. **Identificadores estables.** `N` necesidad, `RF` requisito funcional, `RNF` requisito no funcional, `RN` regla de negocio, `CA` criterio de aceptación, `RH` requisito del arnés/experimento, `D` decisión, `H` hipótesis, `OQ` pregunta abierta. Un ID nunca se reutiliza ni cambia de significado.
2. **Formato de los criterios de aceptación.** `Dado / Cuando / Entonces`. Todo CA se verifica sin juicio humano, salvo los marcados **[HUMANO]**.
3. **Tiempo.** Todo CA con tiempo se lee sobre un **reloj controlable** (RNF-08): "el reloj avanza 24 h" significa que el tiempo simulado avanza, no que se espera.
4. **Prioridad.** `M` imprescindible, `S` deseable, `C` opcional. **Etapa:** `MVP`, `EXT` (extendida), `FUT` (futura). Solo `MVP` entra en el alcance del 2 de diciembre.
5. **Este documento no fija tecnologías ni arquitectura.** Eso se decide en un documento aparte, posterior.
6. **Los criterios de aceptación son el contrato.** Un agente no modifica un CA para hacer pasar una prueba. Todo cambio de este documento se registra en la sección 14 (RH-02, RH-13).
7. **Separación.** Las secciones 1 a 9 describen el **producto**. La sección 10 describe el **arnés y el experimento**. No se mezclan sus criterios de éxito.

---

## 1. Propósito, contexto y necesidades

**Propósito.** Un sistema web, abierto y extensible, para diseñar, validar y ejecutar flujos de trabajo automáticos: se arrastran nodos de **Evento**, **Condición**, **Acción** y **Temporizador**, el sistema valida que no haya ciclos y lo compila a un grafo ejecutable, y un motor asíncrono lo ejecuta con esperas, reintentos y trazabilidad. Cumple las tareas técnicas del enunciado de la materia (sección 1.1). El primer caso de uso es el seguimiento de reparaciones de celulares, con avisos por Correo, SMS y WhatsApp sobre un simulador de servicios de terceros. A futuro (EXT), el sistema se adaptaría a cada negocio a partir de lo que el negocio ya produce.

**Visión (EXT).** Que el sistema observe los artefactos que un negocio ya genera, **extraiga** de ellos los casos y **proponga** procesos a automatizar que una persona confirma. El dueño no cambia sus hábitos: el sistema los aprovecha. Queda fuera del MVP del 2 de diciembre (D-14); sus requisitos se conservan en las secciones 8.8 y 8.9.

**Dos problemas distintos.** (i) *El proceso:* qué estados atraviesa un trabajo. (ii) *La fuente de verdad:* dónde queda registrado. En el escenario de abajo el proceso es casi el mismo y lo que cambia es la fuente; por eso el primer valor está en la **extracción hacia un modelo común de caso**. El **descubrimiento de procesos distintos** cobra más peso en oficios cuyo proceso sí varía entre negocios (H-32).

**Escenario de referencia (tres locales de reparación).** Lo plantea el equipo; las pruebas usan **datos sintéticos** (H-33).

| Local | Cómo trabaja | Artefactos que genera | Estados visibles en el artefacto |
|---|---|---|---|
| E-01 | Cotiza, toma foto del equipo, escribe un archivo de texto por caso y lo guarda en una carpeta (Drive o local); repara y entrega | Una foto y un `.txt` por caso | Casi ninguno explícito; se infieren del texto |
| E-02 | Cotiza, anota en una libreta; repara y entrega | Fotos de las páginas de la libreta | Ninguno explícito |
| E-03 | Cotiza, registra en una hoja de cálculo y revisa el calendario; repara y entrega | Hoja de cálculo y eventos de calendario | Columna de estado y fecha prometida |

Lo común: llega un cliente con un equipo con una falla, se cotiza, se repara y se entrega. Lo que cambia es **dónde queda registrado**. Este escenario es el caso de uso de la visión (EXT) y el origen de su corpus de pruebas.

**Cadena de la visión:** Fuente → Ingesta de solo lectura → Extracción de casos candidatos → Confirmación humana → Caso en el paquete de proceso → Propuesta de paquete y de flujo → Confirmación humana → Motor de flujos.

**Competencia real a superar:** la libreta, la hoja de cálculo y las notas del teléfono. La libreta es memoria pasiva; el sistema debe ser **memoria que actúa** (avisa, recuerda, vigila) con un costo de registro comparable al de apuntar a mano.

### 1.1 Trazabilidad con el enunciado del profesor

| Tarea técnica del enunciado | Requisitos |
|---|---|
| Interfaz gráfica interactiva para arrastrar, soltar y conectar nodos (Eventos, Condiciones, Acciones) | RF-40, RF-41; CA-40a, CA-40d |
| Analizador en el backend que valida que no haya ciclos y transforma el diagrama en un JSON ejecutable (DAG) | RF-02, RF-14, RN-10 |
| Motor de ejecución asíncrono que evalúa condiciones y ejecuta acciones, con retrasos y reintentos automáticos ante fallos de red | RF-04 a RF-13, RF-18, RN-05, RN-09, RN-30 |
| Webhooks salientes y simulación de APIs de terceros (correos, SMS, HTTP POST) | RF-16, RF-90 a RF-92, RN-29 |
| Arquitectura recomendada (máquina de estados finita, Saga, microservicios; PostgreSQL y Redis) | Es una **recomendación**, no un requisito (D-17): se adoptan la máquina de estados finita (RN-30), el DAG y PostgreSQL; microservicios, Saga y Redis no se usan, con justificación en ADR-18 |

### Necesidades (HIPÓTESIS, no validadas por entrevistas)

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

---

## 2. Alcance

| Etapa | Contenido |
|---|---|
| **MVP** | Las cuatro tareas del enunciado: editor visual de nodos, analizador y compilador a DAG ejecutable, motor asíncrono con esperas y reintentos, y webhooks salientes con simulador de APIs de terceros (correo, SMS, WhatsApp, HTTP POST). Primer caso: seguimiento de reparaciones como paquete de proceso. Vista web móvil para el dueño. Canales Correo, SMS y WhatsApp (los dos últimos simulados) con las restricciones de WhatsApp modeladas. Extensibilidad mínima y preparación para open source. |
| **EXT** | Pasos de agente, ingesta de fuentes, extracción de casos y propuestas de paquete y de flujo (RF-15, RF-60 a RF-72). Telegram (RF-32). Aviso asistido por WhatsApp (RF-34). WhatsApp con la API oficial de Meta (RF-82). Segundo paquete real (citas). Editor visual completo. Varios negocios aislados entre sí. Respuestas del cliente recibidas por el canal. Carga de plugins desde carpeta. |
| **FUT** | Estandarización de paquetes entre negocios. Más oficios. Reportes. Modelos locales para evitar la salida de datos. Comunidad de plugins y conectores. |
| **Fuera de alcance** | Cobros y facturación. Inventario de refacciones. Aplicación móvil nativa. |

**Escalera de recortes del MVP (en este orden, si el plazo se compromete):** (1) plugin de ejemplo y preparación para open source, RF-80 a RF-82 y RNF-13; (2) estados de entrega de WhatsApp, RF-33; (3) aprobación de plantillas con retraso en el simulador, RF-93; (4) campos propios, RF-51; (5) canal SMS. **No se recortan:** las cuatro tareas del enunciado, el paquete «Reparaciones», el canal Correo y el simulador con WhatsApp y sus restricciones.

---

## 3. Actores

| Actor | Descripción | Usa el sistema |
|---|---|---|
| **Dueño** | Dueño o encargado del taller. Sin conocimientos técnicos. | Sí (vista móvil simple) |
| **Operador** | Persona que instala, configura y da soporte: flujos, parámetros, plantillas y simulador. | Sí (consola con editor) |
| **Cliente final** | Persona que deja su equipo. Recibe avisos. | No |
| **Canal de aviso** | Servicio externo que entrega mensajes al cliente. | Sistema externo |
| **Fuente de artefactos** | Carpeta, hoja de cálculo, calendario o imágenes del negocio. Se lee, nunca se modifica. | Sistema externo (solo lectura) |
| **Proveedor de modelo** | Servicio externo de modelos que analiza artefactos y propone (EXT). | Sistema externo |
| **Integrador** | Desarrollador que crea plugins (acciones, canales, lectores) sin modificar el motor. | No usa la interfaz; usa el contrato de extensión |
| **Simulador de terceros** | Aplicación del propio proyecto que imita correo, SMS, WhatsApp y un receptor HTTP POST. | Sistema externo simulado |

---

## 4. Restricciones y supuestos

**Restricciones**
- R-01 Sistema web, accesible desde navegador sin instalar nada.
- R-02 Plazo: 2026-12-02. Equipo de 2 personas con agentes de programación.
- R-03 Uso de modelos limitado por las suscripciones del equipo (H-21).
- R-04 Interfaz en español.
- R-05 Los agentes de descubrimiento solo producen **datos** (casos candidatos y propuestas); nunca código ni activaciones (D-11).
- R-06 La ingesta de fuentes es de solo lectura (RN-18).
- R-07 El proyecto se publica como código abierto; la licencia se decide en OQ-15 (D-18).
- R-08 Se cumplen las tareas técnicas del enunciado de la materia (sección 1.1).

**Supuestos (a verificar)**
- S-01 El taller cuenta con teléfono con navegador y conexión a internet.
- S-02 El cliente final aporta un contacto válido y acepta recibir avisos (H-17).
- S-03 Correo y SMS entregan mensajes sin aprobación previa de plantillas; WhatsApp sí la exige y se modela en el simulador (RN-24, RN-25, D-15).

---

## 5. Glosario

| Término | Definición |
|---|---|
| **Caso** | Un trabajo del negocio (en el paquete de referencia, una reparación). Tiene campos comunes (folio, nombre, contacto, canal, estado, fecha) y campos propios del paquete. |
| **Folio** | Identificador único y no reutilizable de un caso. |
| **Estado** | Etapa del caso (sección 6). |
| **Flujo** | Definición de un proceso automático como nodos conectados. |
| **Nodo** | Elemento de un flujo. Tipos: **Evento** (inicio), **Condición**, **Acción**, **Temporizador**. |
| **Ejecución** | Una vez que un flujo corre para un caso. |
| **Paso** | Lo que hace una ejecución en un nodo. |
| **Aviso** | Mensaje que el sistema envía al cliente. |
| **Recordatorio** | Aviso repetido por falta de respuesta o de recogida. |
| **Plantilla de aviso** | Texto con variables de un aviso. *No confundir con plantilla de flujo ni con las plantillas de mensajes de WhatsApp.* |
| **Plantilla de flujo** | Flujo predefinido listo para instalar. |
| **Bandeja** | Lista de pendientes del dueño dentro del sistema. |
| **Ventana horaria** | Horas del día en que se permite enviar avisos. |
| **Canal de aviso** | Servicio externo que entrega los avisos. En el MVP: Correo, SMS y WhatsApp (SMS y WhatsApp simulados). En EXT: Telegram y WhatsApp real. |
| **Vinculación (EXT)** | Asociación entre un caso y la conversación de Telegram del cliente, que se logra cuando el cliente abre el enlace del caso e inicia la conversación. |
| **Paquete de proceso** | Definición declarativa de un tipo de negocio: estados, transiciones, campos propios, plantillas de aviso y flujos incluidos. Es dato, no código. |
| **Fuente** | Lugar donde un negocio guarda sus artefactos (carpeta, hoja de cálculo, calendario, imágenes). |
| **Artefacto** | Un archivo o registro de una fuente. Se identifica por su huella de contenido. |
| **Extracción** | Paso que convierte artefactos en casos candidatos. |
| **Caso candidato** | Caso propuesto por una extracción, con campos, confianza y evidencia, aún sin confirmar. |
| **Evidencia** | Artefacto y fragmento del que sale un dato o una propuesta. |
| **Confianza** | Número de 0 a 1 que acompaña a cada extracción. |
| **Propuesta** | Paquete o flujo sugerido por un agente. No hace nada hasta que una persona lo confirma. |
| **Corpus** | Conjunto sintético y etiquetado de artefactos de los tres locales, con el resultado esperado. |
| **Simulador** | Aplicación del proyecto que imita servicios de terceros, aplica sus restricciones e inyecta fallos. Sirve de demostración y de doble de pruebas. |
| **Plantilla de mensaje de WhatsApp** | Texto que WhatsApp debe aprobar antes de que el negocio pueda iniciar una conversación con él. No es la plantilla de aviso ni la de flujo. |
| **Ventana de conversación** | Las 24 h posteriores al último mensaje del cliente, durante las cuales WhatsApp permite texto libre. |
| **Webhook saliente** | Llamada HTTP POST que el sistema hace a una URL externa como acción de un flujo. |
| **DAG ejecutable** | Documento JSON, resultado de compilar un flujo válido, con nodos, aristas, nodo de inicio y orden topológico. |
| **Plugin** | Pieza de código que añade una acción, un canal o un lector de fuentes mediante el contrato de extensión. |
| **Reloj controlable** | Fuente de tiempo que las pruebas pueden adelantar. |

---

## 6. Modelo de estados: paquete de referencia «Reparaciones»

Esta tabla es el **paquete de referencia**, entregado como dato (RF-50). Otro paquete define sus propios estados.

**Estados:** Recibido, En diagnóstico, Cotización enviada, Sin respuesta, Aprobada, Rechazada, En reparación, Listo para recoger, Entregado, Cancelado, Abandonado.
**Terminales:** Entregado, Cancelado, Abandonado.

| Origen | Destinos permitidos |
|---|---|
| Recibido | En diagnóstico, Cancelado |
| En diagnóstico | Cotización enviada, Cancelado |
| Cotización enviada | Aprobada, Rechazada, Sin respuesta, Cancelado |
| Sin respuesta | Aprobada, Rechazada, Cancelado |
| Aprobada | En reparación, Cancelado |
| Rechazada | Listo para recoger |
| En reparación | Listo para recoger |
| Listo para recoger | Entregado, Abandonado |
| Entregado, Cancelado, Abandonado | (ninguno) |

### 6.1 Estados de la ejecución (máquina de estados finita)

Una ejecución es **Completada con fallo** cuando termina pero alguna acción agotó sus reintentos; es **Fallida** cuando el motor no puede continuarla (error irrecuperable).

| Origen | Destinos permitidos |
|---|---|
| Pendiente | En curso, Cancelada |
| En curso | Esperando, Completada, Completada con fallo, Cancelada, Fallida |
| Esperando | En curso, Cancelada, Fallida |
| Completada, Completada con fallo, Cancelada, Fallida | (ninguno) |

---

## 7. Reglas de negocio

Valores por defecto entre paréntesis; el operador puede modificarlos (RF-27).

| ID | Regla |
|---|---|
| RN-01 | Un caso tiene exactamente un estado. Solo se permiten las transiciones del paquete activo; en el paquete «Reparaciones», las de la sección 6. En ese paquete, un caso no pasa a En reparación sin haber estado Aprobada. |
| RN-02 | Al entrar a **Cotización enviada** se envía un aviso con el monto. Al entrar a **Listo para recoger** se envía un aviso de recogida. |
| RN-03 | Si una cotización no recibe respuesta: recordatorio a las **24 h** y a las **72 h**; a las **120 h** el caso pasa a **Sin respuesta** y se crea una entrada en la bandeja del dueño. |
| RN-04 | Si un equipo en Listo para recoger no se recoge: recordatorios a los **3, 7 y 14 días**; a los **30 días** se crea una solicitud de decisión en la bandeja. El sistema **no** cambia el estado por sí mismo. |
| RN-05 | Cuando un caso cambia de estado, se cancelan los recordatorios y esperas pendientes del estado anterior. |
| RN-06 | Un mismo aviso (mismo caso, mismo evento, misma etapa de recordatorio) se entrega **una sola vez**, aunque haya reintentos, eventos repetidos o reinicios. |
| RN-07 | No se envía ningún aviso a un cliente sin consentimiento registrado o sin contacto válido. En ese caso se crea una entrada en la bandeja. Con WhatsApp, el consentimiento (opt-in) debe estar registrado antes del primer mensaje (RN-26). Con Telegram (EXT), el contacto solo es válido cuando el cliente completó la vinculación (RF-32), y **iniciar la conversación cuenta como consentimiento**. |
| RN-08 | Los avisos solo se envían dentro de la ventana horaria (**09:00 a 20:00**, hora del negocio). Un aviso que vence fuera de ella se difiere al inicio de la siguiente. |
| RN-09 | Un aviso fallido se reintenta hasta **3 veces** con esperas de **1 min, 5 min y 30 min**. Agotados los reintentos, el aviso queda "no entregado" y se crea una entrada en la bandeja. |
| RN-10 | Un flujo es **válido** si: tiene exactamente un evento de inicio; no tiene ciclos; todos sus nodos son alcanzables desde el inicio; toda condición tiene rama por defecto; todo temporizador dura más de 0 y como máximo **90 días**; solo usa acciones registradas (RF-06) con parámetros válidos. |
| RN-11 | Cada activación de un flujo crea una versión. Una ejecución conserva la versión con la que empezó, aunque se active una versión nueva. |
| RN-12 | Cada paso de una ejecución queda registrado: nodo, inicio, fin, resultado, entrada resumida, número de intento y error si lo hubo. El registro no se modifica ni se borra. |
| RN-13 | El estado **Abandonado** solo se asigna por decisión explícita del dueño. |
| RN-14 | Un folio es único y nunca se reutiliza. |
| RN-15 | Ningún caso candidato ni propuesta (de paquete o de flujo) se crea o se activa sin la **confirmación explícita** de una persona (dueño u operador). Los candidatos de confianza alta pueden confirmarse en lote. |
| RN-16 | Toda propuesta cita su **evidencia**: el artefacto y el fragmento de origen de cada estado, campo o recordatorio propuesto. Lo que no tiene evidencia no se muestra. |
| RN-17 | Toda extracción lleva una **confianza** de 0 a 1. Por debajo del umbral (**0,7**) el candidato queda "por revisar", no puede confirmarse en lote y requiere revisión individual. |
| RN-18 | La ingesta es de **solo lectura**: el sistema nunca modifica, mueve ni borra los artefactos del negocio. |
| RN-19 | Un artefacto sin cambios (misma huella de contenido) no se reprocesa ni genera casos duplicados. |
| RN-20 | Solo se envían a un proveedor de modelo datos de fuentes que el dueño **habilitó**. Los contactos (teléfonos y correos) se **enmascaran** antes de enviarse. Cada envío queda en un registro de salida. |
| RN-21 | Un paquete es **válido** si: tiene un estado inicial; todos sus estados son alcanzables; las transiciones solo unen estados declarados; tiene al menos un estado terminal; todo campo propio declara tipo y obligatoriedad; y cada flujo que incluye es válido según RN-10. |
| RN-22 | Cada activación de un paquete crea una versión. Un caso conserva la versión con la que se creó. |
| RN-23 | Los artefactos importados y los textos extraídos se conservan **30 días** o hasta que el dueño los borre. Borrar una fuente elimina sus artefactos y fragmentos de evidencia; los casos ya confirmados se conservan. |
| RN-24 | En WhatsApp, un mensaje iniciado por el negocio fuera de la **ventana de conversación** (24 h desde el último mensaje del cliente) debe usar una **plantilla de mensaje aprobada**. Dentro de la ventana puede ser texto libre (H-40). |
| RN-25 | Una plantilla de mensaje de WhatsApp tiene estado: borrador, en revisión, aprobada o rechazada. Un aviso que la necesita no se envía mientras no esté aprobada: queda "pendiente de plantilla" y se crea una entrada en la bandeja. |
| RN-26 | No se envía un mensaje de WhatsApp sin el consentimiento (opt-in) del cliente, registrado con fecha. |
| RN-27 | Cada mensaje de WhatsApp registra su categoría y su costo estimado. Hay un tope mensual configurable: al alcanzarlo no se envían más mensajes de WhatsApp, quedan "retenidos por tope" y se crea una entrada en la bandeja. |
| RN-28 | Un aviso tiene estados de entrega: pendiente, enviado, entregado, leído (si el canal lo informa), fallido y no entregado. Los estados solo avanzan, nunca retroceden. |
| RN-29 | Un webhook saliente se reintenta ante fallo de red, tiempo de espera agotado (10 s) y respuestas 5xx, con las esperas de RN-09. No se reintenta ante respuestas 4xx. |
| RN-30 | Una ejecución solo cambia de estado mediante las transiciones de la sección 6.1. Cada cambio queda registrado con su instante. |
| RN-31 | Un plugin declara la versión del contrato de extensión que implementa; con una versión incompatible no se registra. Una excepción dentro de un plugin hace fallar el paso, no el motor. |

---

## 8. Requisitos funcionales y criterios de aceptación

### 8.1 Motor de flujos (genérico, independiente del negocio)

**RF-01 (M, MVP) Definir flujos.** El operador puede crear, guardar y recuperar flujos formados por nodos Evento, Condición, Acción y Temporizador, y sus conexiones.
- **CA-01a** Dado un operador autenticado, cuando crea un flujo con 1 evento, 1 condición, 1 acción y 1 temporizador conectados y lo guarda, entonces queda en estado borrador y al recuperarlo contiene los mismos nodos, parámetros y conexiones.
- **CA-01b** Cuando guarda un flujo con un nodo de tipo no soportado, entonces se rechaza con un mensaje que nombra ese nodo.

**RF-02 (M, MVP) Validar flujos.** El sistema valida un flujo según RN-10 y devuelve **todos** los errores encontrados.
- **CA-02a** Dado un flujo sin evento de inicio, cuando se valida, entonces es inválido con el error "sin evento de inicio".
- **CA-02b** Dado un flujo con dos eventos de inicio, cuando se valida, entonces es inválido.
- **CA-02c** Dado un flujo con el ciclo A → B → A, cuando se valida, entonces es inválido y el error identifica los nodos A y B.
- **CA-02d** Dado un flujo con un nodo inalcanzable, cuando se valida, entonces es inválido y el error nombra ese nodo.
- **CA-02e** Dado un flujo con una condición sin rama por defecto, cuando se valida, entonces es inválido.
- **CA-02f** Dado un temporizador de 0 o de más de 90 días, cuando se valida, entonces es inválido.
- **CA-02g** Dado un flujo con tres errores distintos, cuando se valida, entonces el resultado lista los tres.
- **CA-02h** Dado un flujo válido de 50 nodos, cuando se valida, entonces responde en 1 s o menos (RNF-03).

**RF-03 (M, MVP) Activar, desactivar y versionar.** Solo un flujo válido puede activarse.
- **CA-03a** Dado un flujo inválido, cuando se intenta activar, entonces se rechaza y permanece en borrador.
- **CA-03b** Dado un flujo válido, cuando se activa, entonces queda activo con número de versión.
- **CA-03c** Dado un flujo activo en versión 1 con una ejecución en curso, cuando se modifica y se activa la versión 2, entonces la ejecución en curso termina con la versión 1 y las nuevas ejecuciones usan la 2 (RN-11).
- **CA-03d** Dado un flujo activo, cuando se desactiva, entonces no se crean ejecuciones nuevas y las que estaban en curso continúan hasta terminar.

**RF-04 (M, MVP) Disparo por evento.** Un flujo activo se ejecuta cuando ocurre su evento (el caso entra a un estado). En EXT se añade el evento «artefacto nuevo» (CA-61e).
- **CA-04a** Dado un flujo activo cuyo evento es "el caso entra a Cotización enviada", cuando un caso entra a ese estado, entonces se crea exactamente una ejecución asociada a ese caso y a la versión vigente.
- **CA-04b** Dado que no hay flujo activo para ese evento, cuando un caso cambia de estado, entonces el cambio se completa y no se crea ejecución.

**RF-05 (M, MVP) Condiciones.** Un nodo Condición evalúa datos del caso y elige una rama.
- **CA-05a** Dada una condición "monto > 1000" con ramas verdadero, falso y por defecto, cuando el monto es 1500 entonces sigue verdadero; con 500, falso; sin monto, la rama por defecto.
- **CA-05b** Cuando la condición usa un dato inexistente, entonces la ejecución no se detiene, sigue por la rama por defecto y el hecho queda registrado.

**RF-06 (M, MVP) Catálogo de acciones registradas.** Una acción solo puede usarse en un flujo si está **registrada** en la instalación (los plugins pueden registrar más, RF-80). Acciones del MVP: **Enviar aviso al cliente**, **Llamar webhook** (RF-16), **Notificar al dueño** (entrada en la bandeja), **Cambiar estado del caso** (solo a los destinos que el paquete permite al sistema) y **Registrar nota** en el caso. En EXT se añaden las acciones de agente de RF-15.
- **CA-06a** Dado un caso con consentimiento y contacto válido, cuando se ejecuta Enviar aviso, entonces el canal recibe exactamente un mensaje con la plantilla rellena y el aviso queda registrado como "enviado".
- **CA-06b** Cuando se ejecuta Notificar al dueño, entonces aparece una entrada en la bandeja con el texto y el caso.
- **CA-06c** Dado un flujo cuya acción Cambiar estado apunta a un estado que el paquete no permite que el sistema asigne (en «Reparaciones», solo Sin respuesta), cuando se valida, entonces es inválido (RN-10).
- **CA-06d** Cuando se ejecuta Registrar nota, entonces la nota aparece en la línea de tiempo del caso (RF-26).
- **CA-06e** Dado un flujo con una acción no registrada, cuando se valida, entonces es inválido y el error nombra la acción (RN-10).

**RF-07 (M, MVP) Temporizadores y ventana horaria.**
- **CA-07a** Dado un temporizador de 24 h iniciado en t0, cuando el reloj avanza 23 h 59 min, entonces la ejecución sigue en espera; cuando llega a 24 h, entonces continúa con el nodo siguiente.
- **CA-07b** Dada la ventana 09:00 a 20:00 y un aviso que vence a las 22:00, cuando llega esa hora, entonces no se envía y se envía a las 09:00 del día siguiente (hora del negocio).
- **CA-07c** Dado un aviso que vence a las 15:00, cuando llega esa hora, entonces se envía a las 15:00.

**RF-08 (M, MVP) Cancelación de esperas.**
- **CA-08a** Dado un caso en Cotización enviada con recordatorios pendientes a 24 h y 72 h, cuando pasa a Aprobada a las 10 h, entonces al avanzar el reloj 200 h no se envía ningún recordatorio.
- **CA-08b** En ese caso, la ejecución queda "cancelada" con el motivo "cambio de estado" visible en el historial.

**RF-09 (M, MVP) Reintentos.**
- **CA-09a** Dado un canal que falla en los intentos 1 y 2 y acepta el 3, cuando se envía un aviso, entonces se hacen 3 intentos con esperas de 1 min y 5 min y el aviso queda "enviado" con 3 intentos registrados.
- **CA-09b** Dado un canal que siempre falla, cuando se envía un aviso, entonces se hacen 4 intentos en total, el aviso queda "no entregado" y aparece una entrada en la bandeja (RN-09).

**RF-10 (M, MVP) Persistencia y reanudación.** Las ejecuciones sobreviven a un reinicio del sistema.
- **CA-10a** Dadas 100 ejecuciones en espera, cuando el sistema se detiene de forma abrupta y se reinicia, entonces las 100 siguen en espera con el tiempo restante correcto y se reanudan al vencer.
- **CA-10b** Dada una ejecución cuyo temporizador venció mientras el sistema estaba detenido, cuando se reinicia, entonces continúa con el siguiente nodo y no repite pasos ya registrados.
- **CA-10c** Dada una caída justo tras entregar un aviso al canal, cuando el sistema se reinicia y reanuda, entonces el canal de prueba, que descarta duplicados con la misma referencia, contiene un solo mensaje. Cada aviso lleva una referencia única entregada al canal.

**RF-11 (M, MVP) No duplicación.**
- **CA-11a** Dado el mismo evento (mismo caso, misma transición) entregado dos veces, entonces se crea una sola ejecución.
- **CA-11b** Dado un canal que acepta el mensaje pero responde después del tiempo de espera, cuando el sistema reintenta, entonces el canal registra un solo mensaje para esa referencia.

**RF-12 (M, MVP) Historial de ejecuciones.**
- **CA-12a** Dada una ejecución terminada, entonces el historial muestra por cada paso: nodo, inicio, fin, resultado (correcto, error o cancelado), entrada resumida y número de intento (RN-12).
- **CA-12b** El historial puede filtrarse por caso, flujo, estado de la ejecución y rango de fechas.
- **CA-12c** Ningún rol puede editar ni borrar registros del historial desde la interfaz.

**RF-13 (M, MVP) Concurrencia.**
- **CA-13a** Dados 200 casos que cambian de estado a la vez, entonces se crean 200 ejecuciones, ninguna se pierde y ninguna mezcla datos de otro caso.
- **CA-13b** Dados dos cambios de estado simultáneos sobre el mismo caso que no pueden darse ambos según RN-01, entonces se aplica uno, el otro se rechaza con motivo y el estado final es válido.

**RF-14 (M, MVP) Compilación a DAG ejecutable.** Al validar con éxito un flujo, el sistema lo transforma en un documento JSON de grafo dirigido acíclico, que es lo único que el motor ejecuta.
- **CA-14a** Dado un flujo válido, cuando se compila, entonces se obtiene un documento JSON con nodos, aristas, nodo de inicio y parámetros de cada nodo.
- **CA-14b** Dado el mismo flujo, cuando se compila dos veces, entonces el resultado es idéntico byte a byte.
- **CA-14c** Dado un flujo con ciclo, cuando se compila, entonces falla con los errores de RF-02 y no se produce documento.
- **CA-14d** El documento compilado admite un orden topológico, verificable por prueba.
- **CA-14e** Dado un diagrama modificado y sin recompilar, cuando se crean ejecuciones nuevas, entonces siguen usando el documento compilado anterior (RN-11).

**RF-16 (M, MVP) Acción webhook saliente.** Una acción "Llamar webhook" hace un HTTP POST a una URL con cabeceras y un cuerpo con variables.
- **CA-16a** Dada una acción con URL, cabeceras y cuerpo con `{folio}` y `{negocio}`, cuando se ejecuta, entonces el receptor simulado recibe exactamente un HTTP POST con el cuerpo relleno.
- **CA-16b** Dado un receptor que falla por red en los intentos 1 y 2 y responde 200 en el 3, entonces se reintenta con las esperas de RN-09 y la llamada queda "enviada" con 3 intentos registrados.
- **CA-16c** Dado un receptor que responde 500, entonces se reintenta; dado uno que responde 400, entonces no se reintenta y la llamada queda "fallida" (RN-29).
- **CA-16d** Dado un receptor que no responde en 10 s, entonces cuenta como fallo de red (RN-29).
- **CA-16e** Cada llamada lleva una referencia única en una cabecera, para que el receptor deduplique (RN-06).
- **CA-16f** Una URL con formato inválido hace inválido el flujo al validarlo (RN-10).

**RF-18 (M, MVP) Estados de la ejecución.**
- **CA-18a** Para cada par de estados de la sección 6.1, la transición se acepta si está permitida y se rechaza si no (prueba parametrizada sobre la tabla completa).
- **CA-18b** Una ejecución en estado terminal no cambia de estado.
- **CA-18c** El historial muestra el estado actual y cada cambio de estado de la ejecución con su instante (RN-30).

### 8.2 Seguimiento de reparaciones (paquete de referencia)

*RF-20 a RF-32 describen el comportamiento del paquete «Reparaciones» sobre la base genérica de RF-50 y RF-51.*

**RF-20 (M, MVP) Registrar caso.**
- **CA-20a** Dado un dueño autenticado, cuando registra nombre y contacto del cliente, marca, modelo, falla y consentimiento, entonces se crea un caso en Recibido con folio único y fecha.
- **CA-20b** Cuando falta un campo obligatorio, entonces se rechaza indicando el campo.
- **CA-20c** Cuando el contacto tiene formato inválido, entonces se rechaza.
- **CA-20d** Dados dos registros simultáneos, entonces los folios son distintos (RN-14).

*Nota (v0.4):* "contacto" se valida según el canal elegido (RF-31). Con canal Telegram, el consentimiento queda pendiente hasta la vinculación (RF-32); con correo y WhatsApp, lo registra el dueño al crear el caso (RN-26).

**RF-21 (M, MVP) Cambiar estado.**
- **CA-21a** Para **cada** par origen-destino de la tabla de transiciones del paquete activo (en «Reparaciones», la sección 6), el cambio se acepta si el par está permitido y se rechaza con motivo si no lo está, sin cambiar el estado. (Prueba parametrizada sobre la tabla completa; la misma prueba se ejecuta sobre cualquier paquete.)
- **CA-21b** Cada cambio registra quién, cuándo, origen y destino.
- **CA-21c** Un caso en estado terminal no admite ningún cambio.

**RF-22 (M, MVP) Cotización.**
- **CA-22a** Dado un caso En diagnóstico, cuando se registra un monto mayor que 0 y se marca como enviada, entonces el caso pasa a Cotización enviada.
- **CA-22b** Cuando el monto es 0, negativo o no numérico, entonces se rechaza.

**RF-23 (M, MVP) Respuesta del cliente.** El dueño registra la respuesta (D-02).
- **CA-23a** Dado un caso en Cotización enviada o Sin respuesta, cuando el dueño registra aprobación, entonces pasa a Aprobada; cuando registra rechazo, pasa a Rechazada.

**RF-24 (M, MVP) Listado y búsqueda.**
- **CA-24a** Un filtro por estado devuelve exactamente los casos en ese estado.
- **CA-24b** La búsqueda por folio, nombre (parcial, sin distinguir mayúsculas) o teléfono devuelve los casos que coinciden.
- **CA-24c** La vista "requieren atención" muestra los casos con entradas de bandeja sin resolver o en Sin respuesta.

**RF-25 (M, MVP) Plantilla de flujo "Seguimiento de reparación".** Viene instalada, activa y válida; implementa RN-02 a RN-09.
- **CA-25a** Tras la instalación, el flujo existe, está activo y pasa la validación.
- **CA-25b** Un caso entra a Cotización enviada en t0 y no hay respuesta: aviso en t0 (dentro de ventana), recordatorios a 24 h y 72 h, y a 120 h el caso pasa a Sin respuesta con una entrada en la bandeja.
- **CA-25c** Un caso entra a Listo para recoger: aviso inmediato, recordatorios a 3, 7 y 14 días y, a 30 días, una solicitud de decisión en la bandeja. El estado no cambia solo.
- **CA-25d** Un caso en Cotización enviada es aprobado a las 30 h: no se envía ningún recordatorio posterior de cotización.
- **CA-25e** Un caso sin consentimiento registrado: no se envía ningún aviso y hay una entrada en la bandeja (RN-07).

**RF-26 (S, MVP) Línea de tiempo del caso.**
- **CA-26a** Un caso muestra en orden cronológico sus cambios de estado, avisos con resultado, recordatorios, notas y decisiones del dueño.

**RF-27 (S, MVP) Parámetros y plantillas de aviso.** El operador modifica plazos, ventana horaria, zona horaria, reintentos y textos.
- **CA-27a** Dado que el operador cambia el primer recordatorio de 24 h a 12 h, cuando un caso entra después a Cotización enviada, entonces usa 12 h; una ejecución iniciada antes conserva 24 h (RN-11).
- **CA-27b** Un dueño no puede modificar parámetros ni plantillas.
- **CA-27c** Una plantilla con las variables `{folio}`, `{negocio}` y `{monto}` se rellena al enviar; una plantilla con una variable desconocida se rechaza al guardarla.

**RF-28 (M, MVP) Decisión de abandono.**
- **CA-28a** Dada una solicitud de decisión en la bandeja, cuando el dueño elige "Marcar abandonado", entonces el caso pasa a Abandonado; cuando elige "Mantener", entonces el caso conserva su estado y la decisión queda registrada. En ningún caso el sistema cambia el estado por sí mismo (RN-13).

**RF-29 (M, MVP) Bandeja del dueño.**
- **CA-29a** Cada entrada muestra caso, motivo y fecha, y puede marcarse como resuelta.
- **CA-29b** Las entradas sin resolver persisten tras cerrar sesión y tras reiniciar el sistema.

**RF-31 (M, MVP) Canal de aviso por caso.** Cada caso tiene un canal registrado en la instalación (en el MVP: Correo, SMS o WhatsApp) y los avisos se envían por ese canal.
- **CA-31a** Dado el canal Correo, cuando se registra una dirección con formato inválido, entonces se rechaza; con formato válido, se acepta.
- **CA-31b** Dado un caso con canal Correo y otro con canal WhatsApp con consentimiento, cuando se envía un aviso a cada uno, entonces el correo simulado recibe exactamente 1 mensaje del primero y ninguno del segundo, y el WhatsApp simulado recibe exactamente 1 mensaje del segundo y ninguno del primero.
- **CA-31c** Dado un caso cuyo canal exige un consentimiento o una vinculación aún no registrados (WhatsApp sin opt-in o, en EXT, Telegram sin vincular), cuando se intenta enviar un aviso, entonces no se envía nada y aparece una entrada en la bandeja (RN-07).
- **CA-31d** Un caso no cambia de canal una vez que tiene avisos enviados.

**RF-32 (S, EXT) Vinculación de Telegram.** Para el canal Telegram, el sistema genera un enlace único por caso; el cliente lo abre e inicia la conversación, y el caso queda vinculado. *Telegram pasa a EXT (D-15).*
- **CA-32a** Dado un caso con canal Telegram, cuando se registra, entonces se genera un enlace de vinculación único para ese caso y el caso queda con "vinculación pendiente".
- **CA-32b** Dado un enlace válido, cuando el cliente inicia la conversación, entonces el caso queda vinculado y el consentimiento queda registrado con su fecha.
- **CA-32c** Dado un enlace ya usado, cuando se usa de nuevo (por la misma persona u otra), entonces no se modifica la vinculación existente.

**RF-33 (S, MVP) Estados de entrega.** El aviso refleja los estados que el canal informa.
- **CA-33a** Dado un mensaje de WhatsApp simulado que informa "entregado" y luego "leído", entonces el aviso muestra esos estados en orden (RN-28).
- **CA-33b** Dado un estado informado que retrocede (de "leído" a "entregado"), entonces se ignora y el hecho queda registrado.
- **CA-33c** Dado un estado "fallido" informado por el canal, entonces el aviso queda "fallido" y aparece una entrada en la bandeja.

**RF-34 (C, EXT) Aviso asistido por WhatsApp.** El sistema deja el mensaje preparado y el dueño lo envía desde su propio WhatsApp con un toque (H-39).
- **CA-34a** Dado un caso con WhatsApp asistido, cuando vence un aviso, entonces se crea una entrada en la bandeja con el texto y un enlace que abre el chat con el texto escrito.
- **CA-34b** El aviso queda "enviado" solo cuando el dueño lo marca (RN-28).

**RF-35 (M, MVP) Restricciones de WhatsApp en el envío.** Antes de enviar por WhatsApp, el sistema aplica las reglas RN-24 a RN-27.
- **CA-35a** Dado un caso con WhatsApp sin consentimiento registrado, cuando se intenta enviar, entonces no se envía y aparece una entrada en la bandeja (RN-26).
- **CA-35b** Dado un aviso iniciado por el negocio fuera de la ventana de conversación y una plantilla aprobada, cuando se envía, entonces se envía usando esa plantilla (RN-24).
- **CA-35c** Dado lo mismo con una plantilla en borrador, en revisión o rechazada, entonces no se envía, el aviso queda "pendiente de plantilla" y aparece una entrada en la bandeja (RN-25).
- **CA-35d** Dado un cliente que escribió hace menos de 24 h, cuando se envía un aviso, entonces puede usar texto libre (RN-24).
- **CA-35e** Dado el tope mensual alcanzado, cuando se intenta enviar, entonces el aviso queda "retenido por tope", no se envía y aparece una entrada en la bandeja (RN-27).
- **CA-35f** Cada mensaje enviado registra categoría y costo estimado, y el total del mes es consultable (RN-27).
- **CA-35g** El estado de cada plantilla se muestra al operador y solo cambia por el flujo de aprobación (RF-93).

### 8.3 Acceso

**RF-30 (M, MVP) Autenticación y roles.** Roles: Dueño y Operador.
- **CA-30a** Sin autenticación, cualquier acceso a datos o acciones es denegado.
- **CA-30b** Un dueño no accede al editor ni a los parámetros; un operador sí.
- **CA-30c** Tras 5 intentos fallidos consecutivos, la cuenta se bloquea 5 minutos.

### 8.4 Editor visual (solo operador)

**RF-40 (M, MVP) Editor visual mínimo.**
- **CA-40a** Dado un operador, cuando agrega nodos de los cuatro tipos, los conecta, los mueve y los elimina, entonces el diagrama refleja los cambios y se conserva al guardar y reabrir.
- **CA-40b** No se puede conectar un nodo consigo mismo.
- **CA-40c** El operador puede validar el flujo desde el editor y ver el resultado de RF-02.
- **CA-40d** Dada una paleta con los tipos de nodo, cuando el operador arrastra un tipo al lienzo y lo suelta, entonces se crea un nodo de ese tipo en esa posición.
- **CA-40e** Dado un flujo guardado en el editor, cuando se activa, entonces pasa por validación y compilación (RF-02, RF-14) y el editor muestra el resultado.

**RF-41 (S, MVP) Errores sobre el diagrama.**
- **CA-41a** Dado un flujo inválido, cuando se valida desde el editor, entonces cada error se muestra junto al o los nodos afectados.

**RF-42 (C, EXT) Ver una ejecución sobre el diagrama.**
- **CA-42a** Al abrir una ejecución desde el historial, cada nodo muestra su estado (pendiente, ejecutado, cancelado o con error).

### 8.5 Paquetes de proceso (base de la adaptación a cada negocio)

**RF-50 (M, MVP) Paquetes de proceso declarativos.** Un paquete define estados (con inicial y terminales), transiciones, campos propios con tipo y obligatoriedad, plantillas de aviso y los flujos que instala. El sistema carga, valida y activa paquetes. «Reparaciones» se entrega como paquete.
- **CA-50a** Dado un paquete válido, cuando se carga y se activa, entonces los casos nuevos usan sus estados y campos.
- **CA-50b** Dado un paquete con un estado inalcanzable, una transición hacia un estado no declarado, sin estado inicial o sin estado terminal, cuando se valida, entonces es inválido y se listan todos los errores (RN-21).
- **CA-50c** Dado un paquete en versión 1 con casos, cuando se activa la versión 2, entonces los casos existentes conservan la versión 1 y los nuevos usan la 2 (RN-22).
- **CA-50d** Dado el paquete «Reparaciones», cuando se ejecuta la suite de conformidad, entonces pasa CA-21a y CA-21c sobre su tabla completa.
- **CA-50e** Dado un segundo paquete sintético de prueba, cuando se ejecuta la misma suite de conformidad, entonces pasa sin escribir pruebas nuevas.
- **CA-50f** Dado un paquete que incluye un flujo inválido según RN-10, cuando se valida, entonces el paquete es inválido.

**RF-51 (S, MVP) Campos comunes y campos propios.** Los campos comunes (folio, nombre, contacto, canal, estado, fecha) son siempre consultables; los propios se validan contra el esquema del paquete.
- **CA-51a** Dados casos de dos paquetes distintos, cuando se busca por nombre, entonces se devuelven casos de ambos.
- **CA-51b** Dado un campo propio obligatorio ausente, cuando se registra el caso, entonces se rechaza indicando el campo; un valor de tipo incorrecto también se rechaza.
- **CA-51c** Un campo propio se muestra en el caso y su cambio aparece en la línea de tiempo.

### 8.6 Simulador de APIs de terceros

**RF-90 (M, MVP) Simulador de APIs de terceros.** El producto incluye un simulador con receptores de **correo**, **SMS**, **WhatsApp** y **HTTP POST**. Registra lo recibido (instante, destino, contenido, referencia), descarta duplicados con la misma referencia y es consultable desde la interfaz del operador.
- **CA-90a** Dado un aviso por correo, cuando se envía, entonces el simulador lo muestra con destinatario, asunto, cuerpo y referencia.
- **CA-90b** Dados un SMS y un HTTP POST, cuando se envían, entonces el simulador los muestra con destino, contenido y referencia.
- **CA-90c** Dados dos mensajes con la misma referencia, entonces el simulador registra uno solo (CA-11b).
- **CA-90d** El operador puede filtrar lo recibido por canal y por rango de fechas, y vaciar el registro.
- **CA-90e** El simulador funciona sin acceso a internet (CA-N08c).
- **CA-90f** La interfaz del simulador indica que es una simulación y no un servicio real.

**RF-91 (M, MVP) Inyección de fallos.**
- **CA-91a** El operador puede configurar por canal: error, latencia (en segundos), duplicado, respuesta tardía y caída.
- **CA-91b** Un fallo configurado para las siguientes N solicitudes se aplica a esas N y luego se desactiva solo.
- **CA-91c** Los fallos se pueden activar desde la interfaz y desde las pruebas automáticas (CA-N08b).

**RF-92 (M, MVP) Simulador de WhatsApp con restricciones.**
- **CA-92a** Dado un mensaje iniciado por el negocio fuera de la ventana de conversación y sin plantilla aprobada, cuando llega al simulador, entonces lo rechaza con un código de error que identifica el motivo.
- **CA-92b** Dado un mensaje entrante simulado del cliente, cuando el negocio responde dentro de 24 h con texto libre, entonces el simulador lo acepta.
- **CA-92c** El simulador informa los estados de entrega (enviado, entregado, leído) con un retraso configurable (RF-33).
- **CA-92d** El simulador calcula categoría y costo estimado por mensaje con tarifas configurables (RN-27).

**RF-93 (S, MVP) Aprobación de plantillas en el simulador.**
- **CA-93a** Una plantilla enviada a revisión queda "en revisión" y pasa a "aprobada" o "rechazada" por acción del operador o tras un retraso configurable.
- **CA-93b** Una plantilla rechazada incluye un motivo.

### 8.7 Extensibilidad

**RF-80 (S, MVP) Contrato de extensión.** Una acción, un canal o un lector de fuentes se añade como plugin sin modificar el motor. Cada plugin declara nombre, versión del contrato y el esquema de sus parámetros. *Límite declarado: los plugins son código de confianza, sin aislamiento de seguridad.*
- **CA-80a** Dado un plugin de ejemplo (una acción), cuando se registra, entonces aparece en la paleta y puede usarse en un flujo sin que cambie ningún archivo del motor (verificado por prueba).
- **CA-80b** Dado un flujo cuyos parámetros incumplen el esquema del plugin, cuando se valida, entonces falla nombrando el parámetro.
- **CA-80c** Dado un plugin con una versión de contrato incompatible, cuando se intenta registrar, entonces se rechaza con un mensaje que indica la versión esperada (RN-31).
- **CA-80d** Dado un plugin cuya acción lanza una excepción, cuando se ejecuta, entonces el paso falla con el error registrado y el motor y las demás ejecuciones continúan (RN-31).

**RF-81 (S, MVP) Plugin de ejemplo documentado.**
- **CA-81a** Existe un plugin de ejemplo con documentación paso a paso para crear uno propio.
- **CA-81b [HUMANO]** Una persona ajena al proyecto añade el plugin de ejemplo a una instalación limpia siguiendo la documentación, sin pasos no descritos.

**RF-82 (C, EXT) Canal de WhatsApp real como plugin.**
- **CA-82a** El adaptador de la API oficial de WhatsApp implementa el mismo contrato que el simulador y se activa por configuración.

### 8.8 Pasos de agente (EXT)

**RF-15 (S, EXT) Pasos de agente.** El catálogo incluye las acciones **Extraer casos de un artefacto** y **Proponer paquete de proceso**. Cada una consulta a un proveedor de modelo, valida la salida contra un esquema y respeta límites.
- **CA-15a** Dada una salida del modelo que no cumple el esquema, cuando se ejecuta el paso, entonces se reintenta hasta 2 veces y, si persiste, el paso falla con el motivo registrado y no se crea ningún caso ni propuesta.
- **CA-15b** Dado un tope de costo configurado, cuando una ejecución lo alcanza, entonces se detiene con el motivo "presupuesto" y queda registrado.
- **CA-15c** Cada paso de agente registra en el historial: modelo, consumo, duración y número de intento (RN-12).
- **CA-15d** Con el proveedor sustituido por un doble que devuelve respuestas grabadas, la ejecución produce siempre el mismo resultado.

### 8.9 Fuentes, extracción y propuestas (EXT)

**RF-60 (S, EXT) Registrar fuentes.** Tipos del EXT: **carpeta de texto** y **hoja de cálculo**. Recortables: **calendario** e **imágenes**.
- **CA-60a** Dado un dueño, cuando registra una fuente con tipo y ubicación válidos, entonces queda registrada y habilitada; con una ubicación inexistente se rechaza.
- **CA-60b** Dada una fuente deshabilitada, cuando se ejecuta la ingesta, entonces no se lee ni se envía nada de ella (RN-20).
- **CA-60c** Cuando se borra una fuente, entonces sus artefactos y fragmentos de evidencia se eliminan en 1 min o menos y los casos ya confirmados se conservan (RN-23).

**RF-61 (S, EXT) Ingesta de solo lectura.**
- **CA-61a** Dada una carpeta con 3 archivos, cuando se ingesta, entonces se registran 3 artefactos y los archivos originales no cambian (huella idéntica, RN-18).
- **CA-61b** Dado un artefacto ya ingerido y sin cambios, cuando se vuelve a ingerir, entonces no se reprocesa ni genera casos duplicados (RN-19).
- **CA-61c** Dado un artefacto modificado, cuando se ingesta, entonces se registra una nueva versión del artefacto y se procesa.
- **CA-61d** Dado un archivo ilegible entre varios, cuando se ingesta, entonces queda registrado con error y el resto se procesa.
- **CA-61e** Cuando se registra un artefacto nuevo, entonces se emite el evento «artefacto nuevo», que puede disparar un flujo (RF-04).

**RF-62 (S, EXT) Extracción de casos candidatos.**
- **CA-62a** Dado un artefacto del corpus (RH-15) y una respuesta grabada, cuando se extrae, entonces cada candidato contiene los campos esperados, su confianza y su evidencia (artefacto y fragmento).
- **CA-62b** Dado un candidato con confianza menor que el umbral, cuando se extrae, entonces queda "por revisar", no puede confirmarse en lote y no crea caso por sí solo (RN-17).
- **CA-62c** Dado un candidato, cuando una persona lo confirma, entonces se crea el caso en el paquete activo con referencia a su artefacto de origen; cuando lo rechaza, se descarta y se registra (RN-15).
- **CA-62d** Dado un contacto en el texto del artefacto, cuando se arma lo que se envía al modelo, entonces el contacto va enmascarado (RN-20).
- **CA-62e** Dado el mismo artefacto procesado dos veces, entonces no se crean casos duplicados (RN-19).

**RF-63 (S, EXT) Revisión de candidatos.**
- **CA-63a** Cada candidato se muestra con sus campos, su confianza y el fragmento de origen.
- **CA-63b** La persona puede editar campos antes de confirmar y el resultado se valida de nuevo contra el paquete (RF-51).

**RF-70 (S, EXT) Propuesta de paquete.**
- **CA-70a** Dados los casos confirmados y los artefactos de un negocio, cuando se pide una propuesta, entonces se genera un paquete candidato que se valida (RN-21) antes de mostrarse; si no es válido, se descarta y se registra.
- **CA-70b** Cada estado y cada campo propuesto cita al menos una evidencia (RN-16); sin evidencia, no se muestra.
- **CA-70c** Una propuesta no activada no genera ejecuciones ni cambia ningún caso (RN-15).
- **CA-70d** Cuando una persona edita y confirma, entonces se crea una versión activa del paquete (RN-22) y los casos existentes no se alteran.
- **CA-70e** Dado el corpus del local con columna de estado (E-03), cuando se pide una propuesta con respuesta grabada, entonces el paquete incluye los estados de esa columna.

**RF-71 (C, EXT) Propuesta de flujo.**
- **CA-71a** Una propuesta de flujo solo usa acciones del catálogo y pasa RN-10 antes de mostrarse.
- **CA-71b** Cada recordatorio propuesto cita su evidencia.

**RF-72 (S, EXT) Historial de propuestas.**
- **CA-72a** Toda propuesta registra su decisión (aceptada, modificada o rechazada), quién y cuándo.
- **CA-72b** Se puede calcular la proporción de propuestas aceptadas sin edición.

---

## 9. Requisitos no funcionales

| ID | Requisito | Criterio de verificación |
|---|---|---|
| RNF-01 | **Usabilidad del registro.** Registrar un caso es casi tan rápido como apuntarlo. | **CA-N01a** El registro tiene como máximo 6 campos obligatorios. **CA-N01b [HUMANO]** En prueba con 3 o más personas, mediana de ≤ 30 s en el primer uso y ≤ 15 s desde el tercer registro (metas a calibrar). **CA-N01c** Ninguna pantalla del dueño contiene los términos: flujo, nodo, API, disparador, ejecución (búsqueda automática de texto). |
| RNF-02 | **Compatibilidad.** | **CA-N02a** En 360×640 px no hay desplazamiento horizontal en ninguna pantalla del dueño. **CA-N02b** Funciona en las dos últimas versiones estables de Chrome y Firefox. **CA-N02c** "Registrar caso" es alcanzable en 2 interacciones o menos desde el inicio. |
| RNF-03 | **Rendimiento.** | **CA-N03a** Validar un flujo de 50 nodos: ≤ 1 s. **CA-N03b** Listar o buscar sobre 1 000 casos: p95 ≤ 2 s. **CA-N03c** Registrar un caso con 10 usuarios simultáneos: p95 ≤ 1 s. |
| RNF-04 | **Fiabilidad.** Ninguna ejecución se pierde ni duplica avisos. | **CA-N04a** Con 500 ejecuciones simultáneas y un reinicio abrupto a la mitad: 0 ejecuciones perdidas y 0 avisos duplicados. |
| RNF-05 | **Puntualidad.** | **CA-N05a** Con reloj real y 500 ejecuciones en espera, la desviación de un temporizador es ≤ 10 s en p95 y ≤ 30 s como máximo. |
| RNF-06 | **Auditoría.** | **CA-N06a** El 100 % de los cambios de estado y de configuración registra quién, cuándo y qué cambió. Además, CA-12a y CA-12c. |
| RNF-07 | **Seguridad y privacidad.** | **CA-N07a** Ninguna operación devuelve una contraseña en claro y ninguna aparece en registros técnicos. **CA-N07b** Contactos de clientes y credenciales del canal no aparecen en registros técnicos ni en pantallas de dueño. **CA-N07c** Los datos de un cliente solo son accesibles con sesión válida (CA-30a). |
| RNF-08 | **Testabilidad.** | **CA-N08a** Toda espera puede verificarse con reloj controlable. **CA-N08b** El canal de aviso puede sustituirse por un doble que inyecta: error, latencia, duplicado, respuesta tardía y caída (el simulador, RF-91, cumple este papel). **CA-N08c** La suite de aceptación completa se ejecuta en ≤ 10 min sin acceso a servicios externos reales. |
| RNF-09 | **Localización.** | **CA-N09a** Interfaz en español; fechas dd/mm/aaaa y hora de 24 h. |
| RNF-10 | **Privacidad de salida de datos (EXT).** | **CA-N10a** Ningún dato de una fuente deshabilitada se envía al proveedor de modelo. **CA-N10b** Los contactos se enmascaran antes de enviarse. **CA-N10c** Existe un registro de salida consultable por el dueño con qué se envió (artefacto, campos, destino, hora). |
| RNF-11 | **Evaluabilidad de los agentes (EXT).** | **CA-N11a** Las pruebas de aceptación que involucran agentes usan respuestas grabadas y son deterministas. **CA-N11b** La evaluación contra el corpus (RH-15) se ejecuta aparte con un modelo real, reporta las métricas de RH-16 y no bloquea la suite (CA-N08c). |
| RNF-12 | **Retención (EXT).** | **CA-N12a** Artefactos y textos extraídos se eliminan automáticamente a los 30 días salvo que se renueven. **CA-N12b** A petición del dueño, el borrado completo ocurre en 1 min o menos (CA-60c). |
| RNF-13 | **Preparación para código abierto.** | **CA-N13a** El repositorio contiene archivo de licencia, README con instalación y ejecución, y guía de contribución. **CA-N13b** Una comprobación automática no encuentra secretos (claves o contraseñas) en el repositorio. **CA-N13c [HUMANO]** Las dependencias y sus licencias están listadas y son compatibles con la licencia elegida (OQ-15). |
| RNF-14 | **Instalación reproducible.** | **CA-N14a [HUMANO]** Desde un repositorio limpio, siguiendo el README, el sistema queda funcionando con datos de demostración y el simulador en 15 min o menos. |

---

## 10. Requisitos del arnés y del experimento

**Objetivo del experimento.** Estudio de caso **exploratorio** sobre si agentes de programación pueden construir y mantener este sistema bajo un arnés (especificación versionada, pruebas como contrato, verificaciones automáticas). Con muestras pequeñas **no hay significancia estadística**: los resultados se presentan como observaciones, no como conclusiones generales. Inspiración: [OpenAI, "Harness engineering"](https://openai.com/es-419/index/harness-engineering/).

| ID | Requisito | Criterio de verificación |
|---|---|---|
| RH-01 | **Trazabilidad ejecutable.** Todo CA tiene al menos una prueba automática que lo cita por su ID. | El arnés informa CA con prueba / CA totales (excluye los **[HUMANO]**). Meta MVP: 100 % de los CA de prioridad M. |
| RH-02 | **Las pruebas de aceptación son el contrato.** Los agentes no las modifican. | Si un cambio toca un archivo protegido de pruebas o de esta especificación sin aprobación humana, el arnés falla. |
| RH-03 | **Tarea de agente.** Cada tarea nombra los RF y CA objetivo y se evalúa automáticamente (pasa o falla). | Una tarea sin CA objetivo es rechazada por el arnés. |
| RH-04 | **Registro por tarea.** Se registra: modelo, tarea, inicio y fin, iteraciones, consumo de uso o costo estimado, resultado e intervenciones humanas. | Cada tarea ejecutada tiene su registro completo. |
| RH-05 | **Reproducibilidad.** Una tarea puede repetirse desde un estado base identificado del repositorio. | Dos ejecuciones desde el mismo estado base usan la misma entrada y la misma especificación. |
| RH-06 | **Retroalimentación legible por agentes.** Un fallo indica el CA incumplido y qué se esperaba frente a qué se obtuvo. | Todo fallo de la suite cita un ID de CA. |
| RH-07 | **Reglas estructurales.** El arnés verifica automáticamente las reglas de estructura definidas en [arquitectura.md](arquitectura.md), sección 6 (AR-01 a AR-09). | Todas las reglas AR se ejecutan en cada corrida del arnés; una violación hace fallar la corrida con un mensaje de remediación. |
| RH-08 | **Métricas.** M1 tareas aprobadas a la primera; M2 iteraciones por tarea; M3 intervenciones humanas por tarea; M4 regresiones tras un cambio de requisito; M5 consumo de uso por tarea; M6 tiempo hasta pasar todas las pruebas. | Las seis se calculan a partir de los registros de RH-04. |
| RH-09 | **Comparación con y sin arnés.** Las mismas tareas se ejecutan con y sin arnés, y entre los modelos disponibles. | Mínimo 8 tareas por condición (a ajustar según presupuesto, H-21). |
| RH-10 | **Escalabilidad.** M1 a M5 se miden cuando está implementado el 25 %, 50 % y 100 % de los RF de prioridad M. | Tres puntos de medición registrados. |
| RH-11 | **Cambios de requisito controlados.** Se introducen al menos 2 (por ejemplo, cambiar los plazos de RN-03 y añadir un estado "Garantía"). | Para cada cambio se registran: pruebas que fallan, archivos modificados e intervenciones. |
| RH-12 | **Revisión humana.** Cada cambio integrado tiene un revisor humano y una lista de comprobación. | 100 % de los cambios integrados con revisor registrado. Los autores pueden explicar cada parte entregada **[HUMANO]**. |
| RH-13 | **Especificación versionada.** Toda medición indica la versión de este documento. | Cada registro de RH-04 incluye la versión. |
| RH-14 | **Presupuesto de uso.** El arnés lleva la cuenta del uso por modelo y se detiene al alcanzar el límite configurado. | Al alcanzar el límite, no se inicia ninguna tarea nueva. |
| RH-15 | **Corpus etiquetado (EXT).** Conjunto sintético de los tres locales (E-01 a E-03) con artefactos y resultado esperado (casos, campos y paquete esperado). Versionado y protegido como las pruebas de aceptación. | Al menos 15 casos por local. Un cambio al corpus sin aprobación humana hace fallar el arnés. |
| RH-16 | **Métricas de descubrimiento (EXT).** Precisión y exhaustividad de la extracción por tipo de fuente; proporción de estados esperados presentes en el paquete propuesto; proporción de propuestas aceptadas sin edición. | Se calculan sobre el corpus con un modelo real en cada punto de medición de RH-10. |
| RH-17 | **Respuestas grabadas (EXT).** Las pruebas con agentes usan respuestas del modelo grabadas, versionadas y reproducibles. | Regrabar una respuesta requiere aprobación humana (RH-02). |
| RH-18 | **Añadir un plugin como cambio controlado.** Mide la extensibilidad con el mismo experimento. | Se añade el plugin de ejemplo siguiendo la documentación y se registran: archivos del motor modificados (meta: 0), pruebas que fallan e intervenciones. |

---

## 11. Trazabilidad

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

---

## 12. Decisiones y preguntas abiertas

### Decisiones

| ID | Decisión | Estado |
|---|---|---|
| D-01 | **Los flujos del MVP son grafos sin ciclos.** Las repeticiones acotadas (recordatorios, reintentos) son propiedades de los nodos, no ciclos del diagrama. | Confirmado (2026-11-12) |
| D-02 | En el MVP el dueño registra manualmente la respuesta del cliente. | Confirmado (2026-11-12) |
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

### Preguntas abiertas

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

### Hipótesis a verificar

H-03 El dueño no es técnico. H-15 La API oficial de WhatsApp exige plantillas aprobadas y plazos de verificación. H-16 El dolor principal es el olvido y el seguimiento. H-17 Los clientes aceptan recibir avisos. H-20 La política académica admite código generado por agentes. H-21 Las suscripciones permiten el número de ejecuciones del experimento. H-22 Los equipos no recogidos son un problema frecuente. H-23 Un registro de 15 s compite con la libreta. H-24 Los operadores prefieren plantillas editables a flujos escritos desde cero. H-25 Las versiones de plataformas y paquetes citadas en arquitectura.md son las vigentes. H-28 Un bot de Telegram no puede iniciar una conversación con un usuario hasta que este la inicia; por eso hace falta la vinculación (RF-32). Es mi conocimiento y debe verificarse. H-29 Enviar datos del negocio a un proveedor de modelo es aceptable para los dueños si cada fuente se habilita explícitamente y los contactos se enmascaran. H-30 Los chats de WhatsApp solo pueden ingresarse como exportación manual: la API oficial no da acceso al historial de la aplicación (conocimiento propio; verificar). H-31 Los modelos extraen casos con precisión suficiente de fotos de libreta manuscrita (desconocido). H-32 El dueño reconoce sus procesos en una propuesta y le aporta valor; en el escenario de tres locales el proceso es casi el mismo y el valor está en la extracción (no verificada). H-33 Los artefactos de los tres locales descritos son representativos de negocios reales (los plantea el equipo; no hay datos verificados). H-27 Las librerías de diagramas para Blazor no se evaluaron: su encaje con RF-40 y el desempeño de los agentes con ellas son desconocidos. H-34 La licencia de n8n restringe ofrecerlo como servicio alojado a terceros, y la consultoría sobre él está permitida (verificar con n8n). H-35 Los dueños pagarían por el servicio. H-36 Una persona puede dar soporte a varios locales. H-37 La titularidad del código generado por agentes permite publicarlo con la licencia elegida (revisar los términos de cada proveedor). H-38 Existe un hueco frente a alternativas abiertas (validación previa, paquetes adaptables, descubrimiento) y hay desarrolladores que querrían extender el motor. H-39 Un enlace de chat con texto prellenado permite al dueño enviar por WhatsApp con un toque. H-40 Las reglas de WhatsApp (ventana de 24 h, consentimiento, plantillas aprobadas, costo por mensaje) son las modeladas en RN-24 a RN-27; solo plantillas, verificación de la cuenta y costo por mensaje aparecen en las fuentes consultadas.

---

## 13. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| RI-01 | Plazo de 20 días insuficiente | Alcance alineado con el enunciado; descubrimiento en EXT (D-14); escalera de recortes de la sección 2; prioridades M/S/C |
| RI-02 | Las entrevistas contradicen las hipótesis | Revisar la versión 0.2 con los hallazgos antes de congelar la especificación |
| RI-03 | Cambios de requisito contaminan las mediciones | Versionado y congelamiento por etapas (RH-13) |
| RI-04 | Agotar el uso de los modelos | RH-14 |
| RI-05 | El equipo no entiende lo generado por agentes | RH-12 |
| RI-06 | Un canal de aviso impone aprobaciones o costos no previstos | D-05; doble de canal en pruebas (RNF-08) |
| RI-07 | Los canales simulados se confunden con servicios reales en una demostración | La interfaz del simulador declara que es una simulación (CA-90f); WhatsApp real es EXT (D-15) |
| RI-08 | Las reglas de WhatsApp modeladas difieren de las reales | Marcadas como hipótesis (H-40); el adaptador real es EXT y se validaría contra la plataforma |
| RI-09 | Datos del negocio y de sus clientes salen a un proveedor externo | RN-20, RNF-10, habilitación explícita por fuente (H-29) |
| RI-10 | Las salidas de los agentes no son deterministas | Respuestas grabadas (RH-17), validación de esquema, confirmación humana (RN-15) |
| RI-11 | Precisión baja al leer fotos manuscritas | Las imágenes son parte de EXT; confianza y revisión humana (RN-17) |
| RI-12 | Un proyecto abierto y extensible invita a más alcance del que cabe | Extensibilidad mínima (RF-80 a RF-82); comunidad y conectores fuera de alcance (FUT) |
| RI-13 | La rúbrica podría valorar la arquitectura recomendada aunque no la exija | La arquitectura se justifica por requisitos (ADR-18); si se pidiera, separar el procesador de ejecución como proceso aparte |

---

## 14. Historial de cambios

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
