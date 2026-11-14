# Requisitos: motor de flujos web, abierto y extensible (primer caso: microempresas)

| Campo | Valor |
|---|---|
| Versión | 0.13 |
| Fecha límite | 2026-12-02 |
| Equipo | 2 personas + agentes de programación |
| Restricción de materia | Sistema web |
| Contexto, decisiones e historial | [humanos/](humanos/) (no necesario para implementar) |

---

## 0. Cómo usar este documento

1. **IDs estables.** `RF` requisito funcional, `RNF` no funcional, `RN` regla de negocio, `CA` criterio de aceptación, `RH` requisito del arnés, `AR` regla de arquitectura. Un ID nunca se reutiliza ni cambia de significado. Los `D`, `H`, `OQ` y `RI` viven en `humanos/`.
2. **CA.** Formato `Dado / Cuando / Entonces`. Todo CA se verifica sin juicio humano, salvo los marcados **[HUMANO]**.
3. **Tiempo.** Todo CA con tiempo se lee sobre un reloj controlable (RNF-08): "avanza 24 h" es tiempo simulado.
4. **Prioridad.** `M` imprescindible, `S` deseable, `C` opcional. **Etapa:** `MVP`, `EXT`, `FUT`. Solo `MVP` entra en el alcance del 2 de diciembre.
5. **Los CA son el contrato.** Un agente no modifica un CA para hacer pasar una prueba. Todo cambio de este documento se registra en `humanos/historial.md` (RH-02, RH-13).

---

## 1. Propósito y alcance del producto

**Propósito.** Sistema web, abierto y extensible, para diseñar, validar y ejecutar flujos automáticos: se arrastran nodos **Evento**, **Condición**, **Acción** y **Temporizador**; el sistema valida que no haya ciclos, compila a un DAG ejecutable y un motor asíncrono lo ejecuta con esperas, reintentos y trazabilidad. Primer caso: seguimiento de reparaciones de celulares con avisos por Correo, SMS y WhatsApp sobre un simulador de servicios de terceros.

**Visión (EXT).** El sistema lee los artefactos que el negocio ya genera, extrae casos y propone procesos que una persona confirma. Cadena: Fuente → Ingesta de solo lectura → Extracción de candidatos → Confirmación humana → Caso en el paquete → Propuesta de paquete y flujo → Confirmación humana → Motor.

**Escenario de referencia (tres locales de reparación).** Datos sintéticos; origen del corpus de EXT (RH-15).

| Local | Cómo trabaja | Artefactos | Estados visibles |
|---|---|---|---|
| E-01 | Un archivo de texto y una foto por caso, en una carpeta | Foto y `.txt` | Casi ninguno explícito |
| E-02 | Libreta | Fotos de las páginas | Ninguno explícito |
| E-03 | Hoja de cálculo y calendario | Hoja y eventos | Columna de estado y fecha prometida |

---

## 2. Alcance

| Etapa | Contenido |
|---|---|
| **MVP** | Las cuatro tareas del enunciado: editor visual de nodos, analizador y compilador a DAG ejecutable, motor asíncrono con esperas y reintentos, y webhooks salientes con simulador de APIs de terceros (correo, SMS, WhatsApp, HTTP POST). Dos paquetes de proceso: «Reparaciones» (primer caso) y «Citas». Temporizadores anclados a una fecha del caso y filtro y búsqueda por campos propios. Vista web móvil para el dueño. Canales Correo, SMS, WhatsApp y Messenger (los tres últimos simulados) con las restricciones de WhatsApp y Messenger modeladas. Respuestas del cliente por el canal y consultas de Marketplace (simuladas). Extensibilidad mínima y preparación para open source. |
| **EXT** | Pasos de agente, ingesta de fuentes, extracción de casos y propuestas de paquete y de flujo (RF-15, RF-60 a RF-72). Telegram (RF-32). Aviso asistido por WhatsApp (RF-34). WhatsApp, Messenger y Marketplace reales con las APIs de Meta (RF-82). Más paquetes (encargos, cotizaciones, talleres). Evento «caso creado» (RF-04). Editor visual completo. Varios negocios aislados entre sí. Carga de plugins desde carpeta. |
| **FUT** | Estandarización de paquetes entre negocios. Más oficios. Reportes. Modelos locales para evitar la salida de datos. Comunidad de plugins y conectores. |
| **Fuera de alcance** | Cobros y facturación. Inventario de refacciones. Aplicación móvil nativa. |

**Alcance comprometido:** los CA marcados como comprometidos en `harness/scope.json` (protegido, RH-02). El resto del MVP es objetivo.

**Escalera de recortes del MVP (en este orden, si el plazo se compromete):** (1) plugin de ejemplo y preparación para open source, RF-80, RF-81 y RNF-13; (2) parámetros y plantillas editables, RF-27 (quedan los valores por defecto); (3) estados de entrega de WhatsApp, RF-33; (4) aprobación de plantillas con retraso en el simulador, RF-93; (5) errores sobre el diagrama, RF-41. Si aun así no alcanza, y solo con un cambio aprobado de este documento que rebaje su prioridad (RH-02): (6) canal Messenger y consultas de Marketplace, RF-37, RF-38, CA-94a y CA-94b (un caso de Messenger solo nace de una consulta, CA-37c); (7) flujo «Seguimiento de cita» y temporizadores anclados, RF-53 y RF-17, conservando el paquete «Citas» como dato (RF-52). **No se recortan:** el alcance comprometido, las cuatro tareas del enunciado, el paquete «Reparaciones» con su flujo (RF-25), los campos propios consultables (RF-51), las respuestas del cliente por WhatsApp (RF-36), los canales Correo, SMS y WhatsApp, y el simulador.

---

## 3. Actores

| Actor | Descripción | Usa el sistema |
|---|---|---|
| **Dueño** | Dueño o encargado del taller. Sin conocimientos técnicos. | Sí (vista móvil simple) |
| **Operador** | Persona que instala, configura y da soporte: flujos, parámetros, plantillas y simulador. | Sí (consola con editor) |
| **Cliente final** | Persona que deja su equipo o pide una cita. Recibe avisos y puede responder por el canal (RF-36). | No usa la interfaz |
| **Canal de aviso** | Servicio externo que entrega mensajes al cliente. | Sistema externo |
| **Fuente de artefactos** | Carpeta, hoja de cálculo, calendario o imágenes del negocio. Se lee, nunca se modifica. | Sistema externo (solo lectura) |
| **Proveedor de modelo** | Servicio externo de modelos que analiza artefactos y propone (EXT). | Sistema externo |
| **Integrador** | Desarrollador que crea plugins (acciones, canales, lectores) sin modificar el motor. | No usa la interfaz; usa el contrato de extensión |
| **Simulador de terceros** | Aplicación del propio proyecto que imita correo, SMS, WhatsApp, Messenger y un receptor HTTP POST. | Sistema externo simulado |

---

## 4. Restricciones

**Restricciones**
- R-01 Sistema web, accesible desde navegador sin instalar nada.
- R-02 Plazo: 2026-12-02. Equipo de 2 personas con agentes de programación.
- R-03 Uso de modelos limitado por las suscripciones del equipo.
- R-04 Interfaz en español.
- R-05 Los agentes de descubrimiento solo producen **datos** (casos candidatos y propuestas); nunca código ni activaciones.
- R-06 La ingesta de fuentes es de solo lectura (RN-18).
- R-07 El proyecto se publica como código abierto; licencia por decidir.
- R-08 Se cumplen las tareas técnicas del enunciado de la materia.

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
| **Bandeja** | Lista de pendientes del dueño dentro del sistema. Una entrada puede ofrecer opciones que el dueño elige (por ejemplo, Marcar abandonado o Mantener, RF-28). |
| **Consentimiento** | Autorización de un cliente para recibir avisos por un canal, registrada con fecha y origen. Pertenece al contacto en ese canal, no al caso: la comparten todos sus casos y RN-35 la retira para todos. |
| **Ventana horaria** | Horas del día en que se permite enviar avisos. |
| **Canal de aviso** | Servicio externo que entrega los avisos. En el MVP: Correo, SMS, WhatsApp y Messenger (los tres últimos simulados). En EXT: Telegram y los canales reales de Meta. |
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
| **Campo ancla** | Campo propio de tipo fecha que un paquete marca como referencia para temporizadores (en «Citas», `fecha_cita`). |
| **Temporizador anclado** | Nodo Temporizador que espera hasta un campo ancla del caso más o menos un desplazamiento, en vez de una duración fija. |
| **Respuesta del cliente** | Mensaje entrante de un canal con respuesta (WhatsApp, Messenger) asociado a un caso. Emite el evento «el cliente responde». |
| **Consulta** | Mensaje de una persona sin caso que llega por Marketplace. No es un caso hasta que una persona lo convierte. |
| **Messenger** | Canal de mensajería de Facebook. Marketplace es el origen de algunas consultas, que llegan por Messenger. |

---

## 6. Modelo de estados: paquetes de referencia

Esta tabla es el paquete **«Reparaciones»**, entregado como dato (RF-50). El paquete «Citas» está en 6.2. Otro paquete define sus propios estados. Cada estado declara si es terminal, si una persona puede asignarlo y si la acción «Cambiar estado» puede asignarlo. Los **efectos automáticos** son cambios de estado que el sistema hace como consecuencia de otra operación: se declaran en el paquete, respetan la tabla de transiciones, registran «sistema» como autor (CA-21b) y emiten el evento de entrada al estado (RF-04).

**Estados:** Recibido (inicial), En diagnóstico, Cotización enviada, Sin respuesta, Aprobada, Rechazada, En reparación, Listo para recoger, Entregado, Cancelado, Abandonado.
**Terminales:** Entregado, Cancelado, Abandonado.
**Campos propios:** `marca` (texto, obligatorio), `modelo` (texto, obligatorio), `falla` (texto, obligatorio), `monto` (número, opcional; lo registra la cotización, RF-22).
**Asignables por una persona:** todos los destinos de la tabla (RF-21); Abandonado, solo por decisión explícita del dueño (RN-13).
**Asignable por la acción «Cambiar estado»:** Sin respuesta (RN-03).
**Efectos automáticos:** ninguno.

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

### 6.2 Paquete «Citas»

**Estados:** Solicitada (inicial), Agendada, Confirmada, Atendida, No asistió, Cancelada.
**Terminales:** Atendida, No asistió, Cancelada.
**Campos propios:** `fecha_cita` (fecha y hora, obligatorio, campo ancla, filtrable), `servicio` (texto, obligatorio, buscable), `duracion` (minutos, opcional, 30 por defecto).
**Asignables por una persona:** todos los destinos de la tabla (RF-21).
**Asignables por la acción «Cambiar estado»:** Confirmada y Cancelada (por respuesta del cliente, RF-36).
**Efectos automáticos:** Confirmada → Agendada al cambiar `fecha_cita` (CA-52d). Agendada no es asignable por la acción.

| Origen | Destinos permitidos |
|---|---|
| Solicitada | Agendada, Cancelada |
| Agendada | Confirmada, Atendida, No asistió, Cancelada |
| Confirmada | Agendada, Atendida, No asistió, Cancelada |
| Atendida, No asistió, Cancelada | (ninguno) |

---

## 7. Reglas de negocio

Valores por defecto entre paréntesis; el operador puede modificarlos (RF-27). RN-02 a RN-04 y RN-38 describen el comportamiento observable de los flujos incluidos en los paquetes «Reparaciones» y «Citas»: se implementan exclusivamente como datos en `packs/reparaciones.json` y `packs/citas.json`, y el motor no contiene estados concretos (AR-10).

| ID | Regla |
|---|---|
| RN-01 | Un caso tiene exactamente un estado. Solo se permiten las transiciones del paquete activo; en el paquete «Reparaciones», las de la sección 6. |
| RN-02 | Al entrar a **Cotización enviada** se envía un aviso con el monto. Al entrar a **Listo para recoger** se envía un aviso de recogida. |
| RN-03 | Si una cotización no recibe respuesta: recordatorio a las **24 h** y a las **72 h**; a las **120 h** el caso pasa a **Sin respuesta** y se crea una entrada en la bandeja del dueño. |
| RN-04 | Si un equipo en Listo para recoger no se recoge: recordatorios a los **3, 7 y 14 días**; a los **30 días** se crea una solicitud de decisión en la bandeja. El sistema **no** cambia el estado por sí mismo. |
| RN-05 | Cuando un caso cambia de estado, se cancelan las esperas del caso programadas antes del cambio: temporizadores, recordatorios y avisos diferidos (RN-08) o en espera de reintento (RN-09). La ejecución que esperaba queda Cancelada con el motivo «cambio de estado» y un aviso cancelado queda «cancelado» (RN-28). Excepciones: (a) **el paso que ejecuta el cambio** termina, se registra como correcto y su ejecución continúa con el nodo siguiente; (b) un flujo cuyo evento de inicio declara que **sobrevive al cambio de estado** no pierde sus esperas y solo se cancela cuando el caso llega a un estado terminal. Un caso tiene como máximo una ejecución en curso por flujo: si se crea otra, la anterior se cancela con el motivo «reemplazada». |
| RN-06 | Un mismo aviso (mismo caso, mismo evento, misma etapa de recordatorio) se entrega **una sola vez**, aunque haya reintentos, eventos repetidos o reinicios. |
| RN-07 | No se envía ningún aviso a un cliente sin consentimiento registrado o sin contacto válido. En ese caso se crea una entrada en la bandeja. Con WhatsApp, el consentimiento (opt-in) debe estar registrado antes del primer mensaje (RN-26). Con Telegram (EXT), el contacto solo es válido cuando el cliente completó la vinculación (RF-32), y **iniciar la conversación cuenta como consentimiento**. Con Messenger, y con una consulta de Marketplace convertida en caso, también cuenta como consentimiento que el cliente inicie la conversación (RN-36). |
| RN-08 | Los avisos solo se envían dentro de la ventana horaria (**09:00 a 20:00**, hora del negocio). Un aviso que vence fuera de ella se difiere al inicio de la siguiente. Las entradas de la bandeja no se difieren: no son avisos salientes y el dueño las ve al abrir la aplicación. La ventana horaria y los temporizadores anclados se calculan en la zona horaria del negocio, con sus cambios de horario; los temporizadores de duración fija miden tiempo transcurrido. |
| RN-09 | Un aviso fallido se reintenta hasta **3 veces** con esperas de **1 min, 5 min y 30 min**. Agotados los reintentos, el aviso queda "no entregado" y se crea una entrada en la bandeja. |
| RN-10 | Un flujo es **válido** si: tiene exactamente un evento de inicio; no tiene ciclos; todos sus nodos son alcanzables desde el inicio; todo nodo que no es Condición tiene como máximo una salida; toda condición tiene rama por defecto; todo temporizador de duración fija dura más de 0 y como máximo **90 días**, y todo temporizador anclado declara un campo ancla del paquete y un desplazamiento de como máximo **90 días**; solo usa acciones registradas (RF-06) con parámetros válidos. |
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
| RN-24 | En WhatsApp, un mensaje iniciado por el negocio fuera de la **ventana de conversación** (24 h desde el último mensaje del cliente) debe usar una **plantilla de mensaje aprobada**. Dentro de la ventana puede ser texto libre. |
| RN-25 | Una plantilla de mensaje de WhatsApp tiene estado: borrador, en revisión, aprobada o rechazada. Un aviso que la necesita no se envía mientras no esté aprobada: queda "pendiente de plantilla" y se crea una entrada en la bandeja. |
| RN-26 | No se envía un mensaje de WhatsApp sin el consentimiento (opt-in) del cliente, registrado con fecha. |
| RN-27 | Cada mensaje de WhatsApp registra su categoría y su costo estimado. Hay un tope mensual configurable: al alcanzarlo no se envían más mensajes de WhatsApp, quedan "retenidos por tope" y se crea una entrada en la bandeja. |
| RN-28 | Un aviso tiene estados de entrega: pendiente, enviado, entregado, leído (si el canal lo informa), fallido, no entregado y cancelado (RN-05). Los estados solo avanzan, nunca retroceden. |
| RN-29 | Un webhook saliente se reintenta ante fallo de red, tiempo de espera agotado (10 s) y respuestas 5xx, con las esperas de RN-09. No se reintenta ante respuestas 4xx. |
| RN-30 | Una ejecución solo cambia de estado mediante las transiciones de la sección 6.1. Cada cambio queda registrado con su instante. |
| RN-31 | Un plugin declara la versión del contrato de extensión que implementa; con una versión incompatible no se registra. Una excepción dentro de un plugin hace fallar el paso, no el motor. |
| RN-32 | Un temporizador anclado espera hasta el valor del campo ancla del caso más o menos un desplazamiento (antes o después), en hora del negocio. Si ese instante ya pasó al llegar al nodo, el paso se omite y queda registrado; la ejecución sigue. Un aviso «antes de» que la ventana horaria (RN-08) llevaría después de la fecha ancla no se envía: se crea una entrada en la bandeja. |
| RN-33 | Al cambiar un campo ancla, los temporizadores anclados pendientes del caso se recalculan en la misma transacción. La referencia única de un aviso incluye la fecha ancla vigente: el recordatorio de la nueva fecha es un aviso distinto (RN-06). |
| RN-34 | Un mensaje entrante se asocia al caso no terminal con el mismo canal y contacto; si hay varios, al más reciente y se crea una entrada en la bandeja. Sin caso asociado se crea una entrada en la bandeja (o una consulta, RN-37). Un mensaje se registra una sola vez por su identificador externo. El sistema no interpreta texto libre: una condición solo compara la respuesta con valores que el flujo declara, sin distinguir mayúsculas, acentos ni espacios sobrantes. Una respuesta no reconocida no cambia nada y crea una entrada en la bandeja. |
| RN-35 | Si el cliente escribe exactamente «baja» o «stop», se retira su consentimiento: no se le envía ningún aviso más y se crea una entrada en la bandeja. |
| RN-36 | En Messenger, el negocio solo puede iniciar un mensaje dentro de las **24 h** posteriores al último mensaje del cliente. Fuera de esa ventana el aviso no se envía: queda «bloqueado por ventana» y se crea una entrada en la bandeja. No hay plantillas ni costo por mensaje. Iniciar la conversación cuenta como consentimiento. |
| RN-37 | Una consulta de Marketplace no es un caso: no recibe avisos, no dispara flujos y se conserva hasta que una persona la convierte en caso o la descarta (RN-15). Un mensaje nuevo de la misma persona sobre la misma publicación se agrega a la consulta abierta. |
| RN-38 | Flujos por defecto del paquete «Citas» (valores modificables, RF-27). **«Seguimiento de cita»** se dispara al entrar a **Agendada** y **sobrevive al cambio de estado** (RN-05 b): las condiciones siguientes deciden qué se envía tras Confirmada, y se cancela al llegar el caso a un estado terminal. Al entrar a Agendada: aviso con fecha, hora y servicio que pide responder «1» para confirmar o «2» para cancelar. **24 h antes** de la cita: recordatorio, solo si no está Confirmada. **12 h antes**: si sigue Agendada, entrada en la bandeja para que el dueño llame. **2 h antes**: recordatorio, esté Agendada o Confirmada. **2 h después** de la hora de la cita: si el caso no es terminal, entrada en la bandeja «¿asistió?» con las opciones Atendida y No asistió; el sistema no cambia el estado solo. **«Respuesta de cita»** se dispara cuando el cliente responde (RF-36): «1» pasa el caso a Confirmada; «2», a Cancelada con una entrada informativa; cualquier otra respuesta sigue RN-34. |

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
- **CA-02i** Dado un flujo con un nodo Acción o Temporizador con dos salidas, cuando se valida, entonces es inválido y el error nombra ese nodo (RN-10).

**RF-03 (M, MVP) Activar, desactivar y versionar.** Solo un flujo válido puede activarse.
- **CA-03a** Dado un flujo inválido, cuando se intenta activar, entonces se rechaza y permanece en borrador.
- **CA-03b** Dado un flujo válido, cuando se activa, entonces queda activo con número de versión.
- **CA-03c** Dado un flujo activo en versión 1 con una ejecución en curso, cuando se modifica y se activa la versión 2, entonces la ejecución en curso termina con la versión 1 y las nuevas ejecuciones usan la 2 (RN-11).
- **CA-03d** Dado un flujo activo, cuando se desactiva, entonces no se crean ejecuciones nuevas y las que estaban en curso continúan hasta terminar.

**RF-04 (M, MVP) Disparo por evento.** Un flujo activo se ejecuta cuando ocurre su evento (el caso entra a uno de los estados que el evento declara, o el cliente responde, RF-36). En EXT se añade el evento «artefacto nuevo» (CA-61e). Un caso se crea siempre en el estado inicial de su paquete y la creación **no** emite el evento «entra al estado inicial»: solo lo emiten los cambios de estado (RN-01, RF-21), incluidos los efectos automáticos de la sección 6. Reaccionar a un caso nuevo queda en EXT como evento «caso creado».
- **CA-04a** Dado un flujo activo cuyo evento es "el caso entra a Cotización enviada", cuando un caso entra a ese estado, entonces se crea exactamente una ejecución asociada a ese caso y a la versión vigente.
- **CA-04b** Dado que no hay flujo activo para ese evento, cuando un caso cambia de estado, entonces el cambio se completa y no se crea ejecución.

**RF-05 (M, MVP) Condiciones.** Un nodo Condición evalúa datos del caso (y, en un flujo disparado por una respuesta, el texto de la respuesta) y elige una rama.
- **CA-05a** Dada una condición "monto > 1000" con ramas verdadero, falso y por defecto, cuando el monto es 1500 entonces sigue verdadero; con 500, falso; sin monto, la rama por defecto.
- **CA-05b** Cuando la condición usa un dato inexistente, entonces la ejecución no se detiene, sigue por la rama por defecto y el hecho queda registrado.

**RF-06 (M, MVP) Catálogo de acciones registradas.** Una acción solo puede usarse en un flujo si está **registrada** en la instalación (los plugins pueden registrar más, RF-80). Acciones del MVP: **Enviar aviso al cliente**, **Llamar webhook** (RF-16), **Notificar al dueño** (entrada en la bandeja), **Cambiar estado del caso** (solo a los destinos que el paquete declara asignables por la acción, sección 6) y **Registrar nota** en el caso. En EXT se añaden las acciones de agente de RF-15.
- **CA-06a** Dado un caso con consentimiento y contacto válido, cuando se ejecuta Enviar aviso, entonces el canal recibe exactamente un mensaje con la plantilla rellena y el aviso queda registrado como "enviado".
- **CA-06b** Cuando se ejecuta Notificar al dueño, entonces aparece una entrada en la bandeja con el texto y el caso.
- **CA-06c** Dado un flujo cuya acción Cambiar estado apunta a un estado que el paquete no permite que el sistema asigne (en «Reparaciones», solo Sin respuesta; en «Citas», Confirmada y Cancelada), cuando se valida, entonces es inválido (RN-10).
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

**RF-17 (M, MVP) Temporizador anclado a una fecha del caso.** Un nodo Temporizador puede esperar hasta un campo ancla del caso más o menos un desplazamiento (RN-32, RN-33).
- **CA-17a** Dado un caso con `fecha_cita` el 10/12 a las 15:00 y un temporizador «24 h antes», cuando la ejecución llega al nodo, entonces espera hasta el 09/12 a las 15:00 (hora del negocio) y continúa al llegar.
- **CA-17b** Dado que el instante calculado ya pasó cuando la ejecución llega al nodo, entonces el paso se omite sin enviar nada y el historial registra «instante vencido».
- **CA-17c** Dado un caso con temporizadores anclados pendientes, cuando se cambia el campo ancla, entonces todos se recalculan con la nueva fecha y ninguno conserva la anterior.
- **CA-17d** Dado un recordatorio ya enviado para la fecha anterior, cuando se cambia la fecha, entonces el recordatorio de la nueva fecha se envía una sola vez y el anterior no se repite (RN-33).
- **CA-17e** Dado un temporizador anclado a un campo que no existe en el paquete o que no es de fecha, cuando se valida el flujo, entonces es inválido y el error nombra el campo (RN-10).
- **CA-17f** Dado un aviso «antes de» que la ventana horaria llevaría después de la fecha ancla, entonces no se envía y aparece una entrada en la bandeja (RN-32).
- **CA-17g** Dado un caso que llega a un estado terminal, entonces sus temporizadores anclados pendientes se cancelan (RN-05).

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

*Nota:* "contacto" se valida según el canal elegido (RF-31). Con canal Telegram, el consentimiento queda pendiente hasta la vinculación (RF-32); con correo y WhatsApp, lo registra el dueño al crear el caso (RN-26).

**RF-21 (M, MVP) Cambiar estado.**
- **CA-21a** Para **cada** par origen-destino de la tabla de transiciones del paquete activo (en «Reparaciones», la sección 6), el cambio se acepta si el par está permitido y se rechaza con motivo si no lo está, sin cambiar el estado. (Prueba parametrizada sobre la tabla completa; la misma prueba se ejecuta sobre cualquier paquete.)
- **CA-21b** Cada cambio registra quién, cuándo, origen y destino.
- **CA-21c** Un caso en estado terminal no admite ningún cambio.

**RF-22 (M, MVP) Cotización.**
- **CA-22a** Dado un caso En diagnóstico, cuando se registra un monto mayor que 0 y se marca como enviada, entonces el caso pasa a Cotización enviada.
- **CA-22b** Cuando el monto es 0, negativo o no numérico, entonces se rechaza.

**RF-23 (M, MVP) Respuesta del cliente.** El dueño registra la respuesta.
- **CA-23a** Dado un caso en Cotización enviada o Sin respuesta, cuando el dueño registra aprobación, entonces pasa a Aprobada; cuando registra rechazo, pasa a Rechazada.

**RF-24 (M, MVP) Listado y búsqueda.**
- **CA-24a** Un filtro por estado devuelve exactamente los casos en ese estado.
- **CA-24b** La búsqueda por folio, nombre (parcial, sin distinguir mayúsculas) o teléfono devuelve los casos que coinciden.
- **CA-24c** La vista "requieren atención" muestra los casos con entradas de bandeja sin resolver o en Sin respuesta.

**RF-25 (M, MVP) Plantilla de flujo "Seguimiento de reparación".** Viene instalada, activa y válida; implementa RN-02 a RN-09. Es un solo flujo: su evento de inicio es la entrada a Cotización enviada o a Listo para recoger, y una condición sobre el estado elige la rama.
- **CA-25a** Tras la instalación, el flujo existe, está activo y pasa la validación.
- **CA-25b** Un caso entra a Cotización enviada en t0 y no hay respuesta: aviso en t0 (dentro de ventana), recordatorios a 24 h y 72 h, y a 120 h el caso pasa a Sin respuesta con una entrada en la bandeja.
- **CA-25c** Un caso entra a Listo para recoger: aviso inmediato, recordatorios a 3, 7 y 14 días y, a 30 días, una solicitud de decisión en la bandeja. El estado no cambia solo.
- **CA-25d** Un caso en Cotización enviada es aprobado a las 30 h: no se envía ningún recordatorio posterior de cotización.
- **CA-25e** Un caso sin consentimiento registrado: no se envía ningún aviso y hay una entrada en la bandeja (RN-07).

**RF-26 (S, MVP) Línea de tiempo del caso.**
- **CA-26a** Un caso muestra en orden cronológico sus cambios de estado, avisos con resultado, recordatorios, notas y decisiones del dueño.

**RF-27 (S, MVP) Parámetros y plantillas de aviso.** El operador modifica plazos, ventana horaria, zona horaria, reintentos y textos. Cambiar plazos o textos de un flujo crea una versión nueva del flujo (RN-11); la ventana horaria, la zona horaria y los reintentos son de la instalación y se aplican desde ese momento.
- **CA-27a** Dado que el operador cambia el primer recordatorio de 24 h a 12 h, cuando un caso entra después a Cotización enviada, entonces usa 12 h; una ejecución iniciada antes conserva 24 h (RN-11).
- **CA-27b** Un dueño no puede modificar parámetros ni plantillas.
- **CA-27c** Una plantilla con las variables `{folio}`, `{negocio}` y `{monto}` se rellena al enviar; una plantilla con una variable desconocida se rechaza al guardarla.

**RF-28 (M, MVP) Decisión de abandono.**
- **CA-28a** Dada una solicitud de decisión en la bandeja, cuando el dueño elige "Marcar abandonado", entonces el caso pasa a Abandonado; cuando elige "Mantener", entonces el caso conserva su estado y la decisión queda registrada. En ningún caso el sistema cambia el estado por sí mismo (RN-13).

**RF-29 (M, MVP) Bandeja del dueño.**
- **CA-29a** Cada entrada muestra caso, motivo y fecha, y puede marcarse como resuelta.
- **CA-29b** Las entradas sin resolver persisten tras cerrar sesión y tras reiniciar el sistema.

**RF-31 (M, MVP) Canal de aviso por caso.** Cada caso tiene un canal registrado en la instalación (en el MVP: Correo, SMS, WhatsApp o Messenger) y los avisos se envían por ese canal.
- **CA-31a** Dado el canal Correo, cuando se registra una dirección con formato inválido, entonces se rechaza; con formato válido, se acepta.
- **CA-31b** Dado un caso con canal Correo y otro con canal WhatsApp con consentimiento, cuando se envía un aviso a cada uno, entonces el correo simulado recibe exactamente 1 mensaje del primero y ninguno del segundo, y el WhatsApp simulado recibe exactamente 1 mensaje del segundo y ninguno del primero.
- **CA-31c** Dado un caso cuyo canal exige un consentimiento o una vinculación aún no registrados (WhatsApp sin opt-in o, en EXT, Telegram sin vincular), cuando se intenta enviar un aviso, entonces no se envía nada y aparece una entrada en la bandeja (RN-07).
- **CA-31d** Un caso no cambia de canal una vez que tiene avisos enviados.

**RF-32 (S, EXT) Vinculación de Telegram.** Para el canal Telegram, el sistema genera un enlace único por caso; el cliente lo abre e inicia la conversación, y el caso queda vinculado.
- **CA-32a** Dado un caso con canal Telegram, cuando se registra, entonces se genera un enlace de vinculación único para ese caso y el caso queda con "vinculación pendiente".
- **CA-32b** Dado un enlace válido, cuando el cliente inicia la conversación, entonces el caso queda vinculado y el consentimiento queda registrado con su fecha.
- **CA-32c** Dado un enlace ya usado, cuando se usa de nuevo (por la misma persona u otra), entonces no se modifica la vinculación existente.

**RF-33 (S, MVP) Estados de entrega.** El aviso refleja los estados que el canal informa.
- **CA-33a** Dado un mensaje de WhatsApp simulado que informa "entregado" y luego "leído", entonces el aviso muestra esos estados en orden (RN-28).
- **CA-33b** Dado un estado informado que retrocede (de "leído" a "entregado"), entonces se ignora y el hecho queda registrado.
- **CA-33c** Dado un estado "fallido" informado por el canal, entonces el aviso queda "fallido" y aparece una entrada en la bandeja.

**RF-34 (C, EXT) Aviso asistido por WhatsApp.** El sistema deja el mensaje preparado y el dueño lo envía desde su propio WhatsApp con un toque.
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

**RF-36 (M, MVP) Respuestas del cliente por el canal.** Un mensaje entrante de WhatsApp o Messenger se asocia a un caso (RN-34) y emite el evento «el cliente responde». Correo y SMS no reciben respuestas en el MVP: el dueño las registra (RF-23).
- **CA-36a** Dado un caso con canal WhatsApp y un mensaje entrante de su contacto, entonces el mensaje aparece en la línea de tiempo del caso, el evento se emite una vez y se abre la ventana de conversación de 24 h (RN-24).
- **CA-36b** Dado el mismo mensaje entregado dos veces con el mismo identificador externo, entonces se registra uno solo y se emite un solo evento.
- **CA-36c** Dado un flujo con evento «el cliente responde» y una condición `respuesta = "1"` que cambia el caso a Confirmada, cuando llega « 1 » (con mayúsculas, acentos o espacios distintos), entonces el caso pasa a Confirmada y se cancelan sus recordatorios de confirmación pendientes (RN-05).
- **CA-36d** Dado un mensaje entrante que ninguna condición reconoce, entonces el estado no cambia y se crea una entrada en la bandeja con el texto y el caso.
- **CA-36e** Dado un mensaje entrante de un contacto sin caso abierto, entonces se crea una entrada en la bandeja con el texto y no se crea ningún caso.
- **CA-36f** Dado un contacto con dos casos abiertos, cuando escribe, entonces el mensaje se asocia al más reciente y aparece una entrada en la bandeja que lo advierte.
- **CA-36g** Dado un cliente que escribe «baja» o «stop», entonces no se le envía ningún aviso más y aparece una entrada en la bandeja (RN-35).

**RF-37 (M, MVP) Canal Messenger.** Messenger es un canal por caso, con respuestas (RF-36) y la ventana de conversación de RN-36.
- **CA-37a** Dado un caso con canal Messenger cuyo cliente escribió hace menos de 24 h, cuando se envía un aviso, entonces el simulador de Messenger recibe exactamente un mensaje.
- **CA-37b** Dado el último mensaje del cliente hace más de 24 h, cuando se intenta enviar, entonces no se envía, el aviso queda «bloqueado por ventana» y aparece una entrada en la bandeja (RN-36).
- **CA-37c** Un caso con canal Messenger solo se crea desde una consulta (RF-38) y su contacto es el identificador de la conversación; no se pide teléfono ni correo.

**RF-38 (S, MVP) Consultas de Marketplace.** Un mensaje de Messenger marcado con origen Marketplace, de una persona sin caso abierto, crea una consulta (RN-37).
- **CA-38a** Dado ese mensaje, entonces aparece una consulta con texto, nombre, publicación de origen y fecha, y no se crea ningún caso.
- **CA-38b** Dada una consulta, cuando el dueño la convierte, entonces se piden los campos obligatorios que falten y se crea un caso en el paquete activo con canal Messenger, el nombre y el contacto de la consulta y el consentimiento registrado (RN-36).
- **CA-38c** Dada una consulta descartada, entonces queda registrada y no vuelve a mostrarse.
- **CA-38d** Dado un mensaje nuevo de la misma persona sobre la misma publicación, entonces se agrega a la consulta abierta y no se crea otra.
- **CA-38e** Dado el mismo mensaje entregado dos veces, entonces se crea una sola consulta.

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

**RF-51 (M, MVP) Campos comunes y campos propios.** Los campos comunes (folio, nombre, contacto, canal, estado, fecha) son siempre consultables. Los propios se validan contra el esquema del paquete; el paquete puede declararlos filtrables, buscables o campo ancla.
- **CA-51a** Dados casos de dos paquetes distintos, cuando se busca por nombre, entonces se devuelven casos de ambos.
- **CA-51b** Dado un campo propio obligatorio ausente, cuando se registra el caso, entonces se rechaza indicando el campo; un valor de tipo incorrecto también se rechaza.
- **CA-51c** Un campo propio se muestra en el caso y su cambio aparece en la línea de tiempo.
- **CA-51d** Dado un campo propio filtrable, cuando se filtra por él (igualdad, y rango si es fecha), entonces se devuelven exactamente los casos que coinciden.
- **CA-51e** Dado un campo propio de texto buscable, cuando se busca un texto parcial sin distinguir mayúsculas, entonces se devuelven los casos que lo contienen.
- **CA-51f** Con 1 000 casos, filtrar por un campo propio filtrable tarda p95 ≤ 2 s (RNF-03).
- **CA-51g** Un campo propio no declarado filtrable ni buscable no puede usarse como filtro y el error lo dice.

**RF-52 (M, MVP) Paquete «Citas».** Segundo paquete real, entregado como dato con los estados y campos de la sección 6.2 y los flujos «Seguimiento de cita» y «Respuesta de cita» de RN-38.
- **CA-52a** Dado el paquete «Citas», cuando se ejecuta la suite de conformidad, entonces pasa CA-21a y CA-21c sobre su tabla completa (CA-50d).
- **CA-52b** Dado un caso de «Citas» sin `fecha_cita` o sin `servicio`, cuando se registra, entonces se rechaza indicando el campo; una `fecha_cita` en el pasado también se rechaza.
- **CA-52c** Dado un caso Agendado o Confirmado cuya franja (`fecha_cita` y `duracion`) se traslapa con otro caso Agendado o Confirmado, cuando se agenda o se reprograma, entonces se muestra una advertencia con el caso en conflicto y la acción se permite.
- **CA-52d** Dado un caso Confirmado, cuando se cambia su `fecha_cita`, entonces vuelve a Agendada y se pide confirmar de nuevo (RN-33).
- **CA-52e** La vista «Agenda del día» muestra los casos Agendados y Confirmados de un día, ordenados por hora.

**RF-53 (M, MVP) Flujo «Seguimiento de cita».** Viene instalado, activo y válido con el paquete; implementa RN-38 sobre RF-17. Las respuestas del cliente las atiende el flujo «Respuesta de cita» (RF-52, RF-36).
- **CA-53a** Tras instalar el paquete, el flujo existe, está activo y pasa la validación.
- **CA-53b** Dada una cita para las 15:00 del día X, y un caso que entra a Agendada 3 días antes a las 10:00 sin respuesta del cliente: aviso inmediato; recordatorio un día antes a las 15:00; entrada en la bandeja a las 03:00 del día X; recordatorio a las 13:00; entrada «¿asistió?» a las 17:00.
- **CA-53c** Dada la respuesta «1» dos días antes de la cita: el caso pasa a Confirmada y no hay recordatorio de 24 h ni entrada de 12 h; el recordatorio de 2 h sí se envía.
- **CA-53d** Dada la respuesta «2»: el caso pasa a Cancelada, se cancelan las esperas pendientes y aparece una entrada informativa.
- **CA-53e** Dado un cambio de `fecha_cita` a otro día, entonces los recordatorios se calculan para la nueva fecha y ninguno queda para la anterior (CA-17c).
- **CA-53f** Dada una cita que se agenda 1 h antes de su hora: se envía el aviso inmediato y se omiten los recordatorios de 24 h, 12 h y 2 h (CA-17b).
- **CA-53g** Dada una cita pasada que sigue Agendada o Confirmada, cuando el dueño elige «No asistió», entonces el caso pasa a No asistió; el sistema no cambia el estado por sí mismo (RN-13).

### 8.6 Simulador de APIs de terceros

**RF-90 (M, MVP) Simulador de APIs de terceros.** El producto incluye un simulador con receptores de **correo**, **SMS**, **WhatsApp**, **Messenger** y **HTTP POST**. Registra lo recibido (instante, destino, contenido, referencia), descarta duplicados con la misma referencia y es consultable desde la interfaz del operador.
- **CA-90a** Dado un aviso por correo, cuando se envía, entonces el simulador lo muestra con destinatario, asunto, cuerpo y referencia.
- **CA-90b** Dados un SMS y un HTTP POST, cuando se envían, entonces el simulador los muestra con destino, contenido y referencia.
- **CA-90c** Dados dos mensajes con la misma referencia, entonces el simulador registra uno solo (CA-11b).
- **CA-90d** El operador puede filtrar lo recibido por canal y por rango de fechas, y vaciar el registro.
- **CA-90e** El simulador funciona sin acceso a internet (RNF-08; CA-N08c lo exige para la suite de aceptación).
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

**RF-94 (M, MVP) Simulador de Messenger, mensajes entrantes y Marketplace.**
- **CA-94a** Dado un aviso por Messenger, entonces el simulador lo muestra con el identificador de la conversación, el contenido y la referencia.
- **CA-94b** Dado un mensaje del negocio fuera de la ventana de 24 h, cuando llega al simulador, entonces lo rechaza con un código de error que identifica el motivo (RN-36).
- **CA-94c** El operador puede simular un mensaje entrante del cliente por WhatsApp o Messenger, desde la interfaz del simulador y desde las pruebas, con un identificador externo único y, en Messenger, con origen Marketplace y publicación de origen.
- **CA-94d** El simulador entrega el mensaje entrante al sistema por HTTP y, si el sistema no responde con éxito, reintenta con el mismo identificador externo (RN-34).

### 8.7 Extensibilidad

**RF-80 (S, MVP) Contrato de extensión.** Una acción, un canal o un lector de fuentes se añade como plugin sin modificar el motor. Cada plugin declara nombre, versión del contrato y el esquema de sus parámetros. *Límite declarado: los plugins son código de confianza, sin aislamiento de seguridad.*
- **CA-80a** Dado un plugin de ejemplo (una acción), cuando se registra, entonces aparece en la paleta y puede usarse en un flujo sin que cambie ningún archivo del motor (verificado por prueba).
- **CA-80b** Dado un flujo cuyos parámetros incumplen el esquema del plugin, cuando se valida, entonces falla nombrando el parámetro.
- **CA-80c** Dado un plugin con una versión de contrato incompatible, cuando se intenta registrar, entonces se rechaza con un mensaje que indica la versión esperada (RN-31).
- **CA-80d** Dado un plugin cuya acción lanza una excepción, cuando se ejecuta, entonces el paso falla con el error registrado y el motor y las demás ejecuciones continúan (RN-31).

**RF-81 (S, MVP) Plugin de ejemplo documentado.**
- **CA-81a** Existe un plugin de ejemplo con documentación paso a paso para crear uno propio.
- **CA-81b** [HUMANO] Una persona ajena al proyecto añade el plugin de ejemplo a una instalación limpia siguiendo la documentación, sin pasos no descritos.

**RF-82 (C, EXT) Canales reales de WhatsApp y Messenger como plugin.**
- **CA-82a** El adaptador de la API oficial de WhatsApp implementa el mismo contrato que el simulador y se activa por configuración.
- **CA-82b** El adaptador de Messenger implementa el mismo contrato, recibe los mensajes entrantes por el mismo punto de entrada (RF-36) y se activa por configuración.

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
| RNF-01 | **Usabilidad del registro.** Registrar un caso es casi tan rápido como apuntarlo. | **CA-N01a** El registro tiene como máximo 6 campos obligatorios. **CA-N01b** [HUMANO] En prueba con 3 o más personas, mediana de ≤ 30 s en el primer uso y ≤ 15 s desde el tercer registro (metas a calibrar). **CA-N01c** Ninguna pantalla del dueño contiene los términos: flujo, nodo, API, disparador, ejecución (búsqueda automática de texto). |
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
| RNF-13 | **Preparación para código abierto.** | **CA-N13a** El repositorio contiene archivo de licencia, README con instalación y ejecución, y guía de contribución. **CA-N13b** Una comprobación automática no encuentra secretos (claves o contraseñas) en el repositorio. **CA-N13c** [HUMANO] Las dependencias y sus licencias están listadas y son compatibles con la licencia elegida. |
| RNF-14 | **Instalación reproducible.** | **CA-N14a** [HUMANO] Desde un repositorio limpio, siguiendo el README, el sistema queda funcionando con datos de demostración y el simulador en 15 min o menos. |

---

## 10. Requisitos del arnés y del experimento

**Objetivo del experimento.** Estudio de caso **exploratorio** sobre si agentes de programación pueden construir y mantener este sistema bajo un arnés (especificación versionada, pruebas como contrato, verificaciones automáticas). Con muestras pequeñas **no hay significancia estadística**: los resultados se presentan como observaciones, no como conclusiones generales. Inspiración: [OpenAI, "Harness engineering"](https://openai.com/es-419/index/harness-engineering/).

| ID | Requisito | Criterio de verificación |
|---|---|---|
| RH-01 | **Trazabilidad ejecutable.** Todo CA tiene al menos una prueba automática que lo cita por su ID. | El arnés informa CA con prueba / CA totales (excluye los **[HUMANO]**). Meta MVP: 100 % de los CA comprometidos en `harness/scope.json`; el resto de los CA de RF de prioridad M se informan como pendientes. Los CA de RNF no tienen prioridad y se informan aparte con su propia meta. |
| RH-02 | **Las pruebas de aceptación son el contrato.** Los agentes no las modifican. | Si un cambio toca un archivo protegido de pruebas, de esta especificación, de la sección 3 de la arquitectura o `harness/scope.json` sin la aprobación de una persona revisora, el arnés falla. |
| RH-03 | **Tarea de agente.** Cada tarea nombra los RF y CA objetivo y se evalúa automáticamente (pasa o falla). | Una tarea sin CA objetivo es rechazada por el arnés. |
| RH-04 | **Registro por tarea.** Se registra: modelo, tarea, inicio y fin, iteraciones, consumo de uso o costo estimado, resultado e intervenciones humanas. | Cada tarea ejecutada tiene su registro completo. |
| RH-05 | **Reproducibilidad.** Una tarea puede repetirse desde un estado base identificado del repositorio. | Dos ejecuciones desde el mismo estado base usan la misma entrada y la misma especificación. |
| RH-06 | **Retroalimentación legible por agentes.** Un fallo indica el CA incumplido y qué se esperaba frente a qué se obtuvo. | Todo fallo de la suite cita un ID de CA. |
| RH-07 | **Reglas estructurales.** El arnés verifica automáticamente las reglas de estructura definidas en [arquitectura.md](arquitectura.md), sección 3 (todas las reglas AR). | Todas las reglas AR se ejecutan en cada corrida del arnés; una violación hace fallar la corrida con un mensaje de remediación. |
| RH-08 | **Métricas.** M1 tareas aprobadas a la primera; M2 iteraciones por tarea; M3 intervenciones humanas por tarea; M4 regresiones tras un cambio de requisito; M5 consumo de uso por tarea; M6 tiempo hasta pasar todas las pruebas. | Las seis se calculan a partir de los registros de RH-04. |
| RH-09 | **Comparación con y sin arnés.** Las mismas tareas se ejecutan con y sin arnés, y entre los modelos disponibles. | Mínimo 8 tareas por condición (a ajustar según presupuesto). |
| RH-10 | **Escalabilidad.** M1 a M5 se miden cuando está implementado el 25 %, 50 % y 100 % de los RF de prioridad M. | Tres puntos de medición registrados. |
| RH-11 | **Cambios de requisito controlados.** Se introducen al menos 2 (por ejemplo, cambiar los plazos de RN-03 y añadir un estado "Garantía"). | Para cada cambio se registran: pruebas que fallan, archivos modificados e intervenciones. |
| RH-12 | **Revisión humana.** Cada cambio integrado tiene un revisor humano y una lista de comprobación. | 100 % de los cambios integrados con revisor registrado. Los autores pueden explicar cada parte entregada **[HUMANO]**. |
| RH-13 | **Especificación versionada.** Toda medición indica la versión de este documento. | Cada registro de RH-04 incluye la versión. |
| RH-14 | **Presupuesto de uso.** El arnés lleva la cuenta del uso por modelo y se detiene al alcanzar el límite configurado. | Al alcanzar el límite, no se inicia ninguna tarea nueva. |
| RH-15 | **Corpus etiquetado (EXT).** Conjunto sintético de los tres locales (E-01 a E-03) con artefactos y resultado esperado (casos, campos y paquete esperado). Versionado y protegido como las pruebas de aceptación. | Al menos 15 casos por local. Un cambio al corpus sin aprobación humana hace fallar el arnés. |
| RH-16 | **Métricas de descubrimiento (EXT).** Precisión y exhaustividad de la extracción por tipo de fuente; proporción de estados esperados presentes en el paquete propuesto; proporción de propuestas aceptadas sin edición. | Se calculan sobre el corpus con un modelo real en cada punto de medición de RH-10. |
| RH-17 | **Respuestas grabadas (EXT).** Las pruebas con agentes usan respuestas del modelo grabadas, versionadas y reproducibles. | Regrabar una respuesta requiere aprobación humana (RH-02). |
| RH-18 | **Añadir un plugin como cambio controlado.** Mide la extensibilidad con el mismo experimento. | Se añade el plugin de ejemplo siguiendo la documentación y se registran: archivos del motor modificados (meta: 0), pruebas que fallan e intervenciones. |
