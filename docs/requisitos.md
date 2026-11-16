# Requisitos: motor de ejecución y automatización de flujos de trabajo

| Campo | Valor |
|---|---|
| Versión | 1.0 |
| Fecha límite | 2026-12-02 |
| Equipo | 2 personas + agentes de programación |
| Fuente del alcance | [project-idea.txt](../project-idea.txt) (enunciado de la materia) |
| Contexto, decisiones e historial | [humanos/](humanos/) (no necesario para implementar) |

---

## 0. Cómo usar este documento

1. **IDs estables.** `RF` requisito funcional, `RNF` no funcional, `RN` regla, `CA` criterio de aceptación, `RH` requisito del arnés, `AR` regla de arquitectura. Un ID nunca se reutiliza; los huecos de numeración son IDs retirados.
2. **CA.** Formato `Dado / Cuando / Entonces`. Todo CA se verifica sin juicio humano, salvo los marcados **[HUMANO]**.
3. **Tiempo.** Todo CA con tiempo se lee sobre un reloj controlable (RNF-08): "avanza 2 h" es tiempo simulado.
4. **Los CA son el contrato.** Un agente no modifica un CA para hacer pasar una prueba. Todo cambio de este documento se registra en `humanos/historial.md` (RH-02, RH-13).

---

## 1. Propósito y alcance

**Propósito.** Plataforma web para diseñar y automatizar secuencias de tareas lógicas y condicionales basadas en eventos.

**Ejemplo de referencia (del enunciado).** «Si un nuevo usuario se registra, esperar 2 horas, verificar si activó su cuenta; si no, enviarle un correo de recordatorio y notificar a soporte». Se entrega instalado como flujo de ejemplo (RF-19).

**Alcance: las cuatro tareas del enunciado y el acceso con roles (RF-30).**

| # | Tarea del enunciado | Requisitos |
|---|---|---|
| T1 | Interfaz gráfica para arrastrar, soltar y conectar nodos (Eventos, Condiciones, Acciones) | RF-01, RF-40 |
| T2 | Analizador en el backend que valida que no haya ciclos y transforma el diagrama en un JSON ejecutable (DAG) | RF-02, RF-03, RF-14 |
| T3 | Motor de ejecución asíncrono: evalúa condiciones, ejecuta acciones, soporta esperas y reintentos ante fallos de red | RF-04, RF-05, RF-07, RF-09 a RF-13, RF-18 |
| T4 | Webhooks salientes y simulación de APIs de terceros (correo, SMS, HTTP POST) | RF-06, RF-16, RF-90, RF-91 |

**Fuera de alcance.** Gestión de usuarios desde la interfaz, registro propio, recuperación de contraseña, roles distintos de Editor y Lector, permisos por flujo y varios inquilinos. Dominios de negocio concretos. Canales distintos de correo, SMS y HTTP POST, y servicios reales de terceros. Mensajes entrantes. Plugins. Ejecuciones en paralelo dentro de un flujo. Escalado a varias instancias. Aplicación móvil.

**Alcance comprometido:** los CA de `harness/scope.json` (protegido, RH-02). El resto es objetivo.

**Recortes, en este orden, si el plazo se compromete:** (1) CA-12b, (2) CA-04c (el evento de prueba se envía con un script), (3) CA-90d, (4) CA-30e (el Lector ve los controles, pero la API los rechaza, CA-30b).

---

## 2. Actores

| Actor | Descripción |
|---|---|
| **Editor** | Persona que diseña, valida, activa y desactiva flujos, envía eventos de prueba y consulta ejecuciones. Inicia sesión. |
| **Lector** | Persona que consulta flujos, ejecuciones e historial sin modificar nada. Inicia sesión. |
| **Sistema origen** | Aplicación externa que envía eventos al motor por HTTP con la clave de integración. En la demostración, un Editor desde la interfaz (CA-04c) o un script. |
| **Simulador de terceros** | Aplicación del proyecto que imita correo, SMS y un receptor HTTP POST, e inyecta fallos. |

## 3. Restricciones

- R-01 Sistema web, accesible desde el navegador sin instalar nada.
- R-02 Plazo: 2026-12-02. Equipo de 2 personas con agentes de programación.
- R-04 Interfaz en español.
- R-08 Se cumplen las cuatro tareas del enunciado (sección 1); fuera de ellas solo entra el acceso con roles (RF-30).

---

## 4. Glosario

| Término | Definición |
|---|---|
| **Flujo** | Definición de un proceso automático como nodos conectados. |
| **Nodo** | Elemento de un flujo, con un identificador legible único en el flujo (por ejemplo `verificar`). Tipos: **Evento** (inicio), **Condición**, **Acción** y **Temporizador** (espera). |
| **Diagrama** | Lo que guarda el editor: nodos, posiciones, parámetros y conexiones. |
| **DAG ejecutable** | Documento JSON, resultado de compilar un flujo válido: nodos, aristas, nodo de inicio, parámetros y orden topológico. Es lo único que el motor ejecuta. |
| **Versión** | Copia inmutable del DAG ejecutable creada al activar un flujo. |
| **Evento** | Hecho que llega al motor por HTTP: un **tipo** (por ejemplo `usuario.registrado`), un **identificador** único y **datos** JSON. |
| **Ejecución** | Una vez que un flujo corre por un evento. |
| **Paso** | Lo que hace una ejecución en un nodo. Cada intento de una acción es un paso. |
| **Contexto** | Datos que una ejecución puede leer: `evento.<campo>` (datos del evento) y `nodos.<id>.codigo` y `nodos.<id>.respuesta.<campo>` (resultado de un webhook ya ejecutado). |
| **Variable** | Texto `{ruta}` dentro de un parámetro, reemplazado por el valor del contexto al ejecutar (por ejemplo `{evento.email}`). |
| **Acción** | Tarea que un nodo ejecuta: **Enviar correo**, **Enviar SMS** o **Llamar webhook**. |
| **Webhook saliente** | Llamada HTTP POST a una URL como acción de un flujo. |
| **Referencia única** | Identificador de un envío (ejecución y nodo) que viaja con él, para que el receptor descarte duplicados. |
| **Simulador** | Aplicación del proyecto que imita los servicios de terceros e inyecta fallos. Sirve de demostración y de doble de pruebas. |
| **Rol** | Permiso fijo de una cuenta: **Editor** o **Lector**. Una cuenta tiene exactamente un rol. |
| **Clave de integración** | Secreto de la instalación con el que el sistema origen se identifica al enviar eventos. No es una cuenta de usuario. |
| **Reloj controlable** | Fuente de tiempo que las pruebas pueden adelantar. |

---

## 5. Estados

### 5.1 Ejecución (máquina de estados finita)

| Origen | Destinos permitidos |
|---|---|
| Pendiente | En curso |
| En curso | Esperando, Completada, Fallida |
| Esperando | En curso |
| Completada, Fallida | (ninguno) |

Una ejecución queda **Esperando** durante un temporizador o hasta el siguiente reintento. Queda **Fallida** cuando una acción agota sus reintentos o falla sin reintento (RN-29, RN-41); el motivo queda registrado.

### 5.2 Flujo

| Origen | Destinos permitidos |
|---|---|
| Borrador | Activo (solo si es válido) |
| Activo | Activo (al activar cambios: nueva versión), Inactivo |
| Inactivo | Activo (solo si es válido) |

Editar el diagrama de un flujo activo no cambia la versión que se ejecuta hasta que se vuelve a activar.

---

## 6. Reglas

| ID | Regla |
|---|---|
| RN-06 | Un mismo envío (misma ejecución, mismo nodo) se entrega **una sola vez**, aunque haya reintentos o reinicios: lleva una referencia única y el receptor descarta duplicados. |
| RN-09 | Una acción que falla por un error reintentable (RN-29) se reintenta hasta **3 veces** con esperas de **1 min, 5 min y 30 min**. Agotados los reintentos, la ejecución queda Fallida. |
| RN-10 | Un flujo es **válido** si: tiene exactamente un nodo Evento; no tiene ciclos; todos sus nodos son alcanzables desde el Evento; los identificadores de nodo son únicos; todo nodo que no es Condición tiene como máximo una salida; toda Condición tiene a lo sumo una salida «sí» y una «no»; todo Temporizador dura más de 0 y como máximo **90 días**; solo usa acciones registradas (RF-06) con parámetros válidos (URL bien formada, destinatario presente). |
| RN-11 | Cada activación de un flujo crea una versión. Una ejecución conserva la versión con la que empezó. |
| RN-12 | Cada paso queda registrado: nodo, inicio, fin, resultado, entrada resumida, número de intento y error si lo hubo. El registro no se modifica ni se borra. |
| RN-29 | Son reintentables: fallo de red, tiempo de espera agotado (**10 s**) y respuestas 5xx. Una respuesta 4xx no se reintenta. Aplica a las tres acciones, porque todas llaman a un tercero por HTTP. |
| RN-30 | Una ejecución solo cambia de estado mediante las transiciones de 5.1. Cada cambio queda registrado con su instante. |
| RN-39 | Un evento cuyo identificador ya se recibió se ignora. Un evento nuevo crea una ejecución por cada flujo activo cuyo nodo Evento declara ese tipo. |
| RN-40 | Una Condición compara un valor del contexto con un valor literal (número, booleano o texto) mediante `=`, `≠`, `>`, `≥`, `<`, `≤`, «contiene» o «existe». Verdadero sigue la salida «sí»; falso o dato inexistente, la salida «no», y el dato inexistente queda registrado. Si la salida elegida no está conectada, la ejecución termina Completada. |
| RN-41 | Las variables se rellenan al ejecutar el paso. Una variable sin valor en el contexto hace fallar el paso sin reintento y la ejecución queda Fallida con el nombre de la variable. |

---

## 7. Requisitos funcionales y criterios de aceptación

### 7.1 Definición, validación y compilación

**RF-01 Definir flujos.** Un Editor puede crear, guardar y recuperar flujos formados por nodos Evento, Condición, Acción y Temporizador, y sus conexiones.
- **CA-01a** Dado un Editor con sesión, cuando crea un flujo con 1 evento, 1 condición, 1 acción y 1 temporizador conectados y lo guarda, entonces queda en Borrador y al recuperarlo contiene los mismos nodos, parámetros y conexiones.
- **CA-01b** Cuando guarda un flujo con un nodo de tipo no soportado, entonces se rechaza con un mensaje que nombra ese nodo.

**RF-02 Validar flujos.** El sistema valida un flujo según RN-10 y devuelve **todos** los errores encontrados.
- **CA-02a** Dado un flujo sin evento de inicio, cuando se valida, entonces es inválido con el error "sin evento de inicio".
- **CA-02b** Dado un flujo con dos eventos de inicio, cuando se valida, entonces es inválido.
- **CA-02c** Dado un flujo con el ciclo A → B → A, cuando se valida, entonces es inválido y el error identifica los nodos A y B.
- **CA-02d** Dado un flujo con un nodo inalcanzable, cuando se valida, entonces es inválido y el error nombra ese nodo.
- **CA-02f** Dado un temporizador de 0 o de más de 90 días, cuando se valida, entonces es inválido.
- **CA-02g** Dado un flujo con tres errores distintos, cuando se valida, entonces el resultado lista los tres.
- **CA-02h** Dado un flujo válido de 50 nodos, cuando se valida, entonces responde en 1 s o menos.
- **CA-02i** Dado un flujo con un nodo Acción o Temporizador con dos salidas, cuando se valida, entonces es inválido y el error nombra ese nodo.

**RF-14 Compilación a DAG ejecutable.** Al validar con éxito un flujo, el sistema lo transforma en un documento JSON de grafo dirigido acíclico.
- **CA-14a** Dado un flujo válido, cuando se compila, entonces se obtiene un documento JSON con nodos, aristas, nodo de inicio y parámetros de cada nodo.
- **CA-14b** Dado el mismo flujo, cuando se compila dos veces, entonces el resultado es idéntico byte a byte.
- **CA-14c** Dado un flujo con ciclo, cuando se compila, entonces falla con los errores de RF-02 y no se produce documento.
- **CA-14d** El documento compilado admite un orden topológico, verificable por prueba.

**RF-03 Activar, desactivar y versionar.** Solo un flujo válido puede activarse.
- **CA-03a** Dado un flujo inválido, cuando se intenta activar, entonces se rechaza y permanece en su estado.
- **CA-03b** Dado un flujo válido, cuando se activa, entonces queda Activo con número de versión.
- **CA-03c** Dado un flujo activo en versión 1 con una ejecución en curso, cuando se modifica y se activa la versión 2, entonces la ejecución en curso termina con la versión 1 y las nuevas usan la 2 (RN-11).
- **CA-03d** Dado un flujo activo, cuando se desactiva, entonces no se crean ejecuciones nuevas y las que estaban en curso continúan hasta terminar.

### 7.2 Ejecución

**RF-04 Disparo por evento.** El sistema origen envía un evento con `POST /api/eventos` (tipo, identificador y datos) y la clave de integración (CA-30d). Un flujo activo se ejecuta cuando llega un evento del tipo que declara su nodo Evento (RN-39).
- **CA-04a** Dado un flujo activo cuyo evento es `usuario.registrado`, cuando llega un evento de ese tipo, entonces se crea exactamente una ejecución con la versión vigente y su contexto contiene los datos del evento.
- **CA-04b** Dado que no hay flujo activo para un tipo, cuando llega un evento de ese tipo, entonces se acepta y no se crea ejecución.
- **CA-04c** Un Editor puede enviar un evento de prueba desde la interfaz con tipo y datos JSON.
- **CA-04d** Dados dos flujos activos con el mismo tipo de evento, cuando llega un evento, entonces se crea una ejecución de cada uno.

**RF-05 Condiciones.** Un nodo Condición evalúa el contexto y elige una salida (RN-40).
- **CA-05a** Dada la condición `nodos.verificar.respuesta.activo = true`, cuando el valor es `true` entonces sigue la salida «sí»; cuando es `false`, la salida «no».
- **CA-05b** Cuando la condición usa un dato inexistente, entonces la ejecución no se detiene, sigue la salida «no» y el hecho queda registrado.

**RF-06 Catálogo de acciones.** Una acción solo puede usarse en un flujo si está registrada. Acciones: **Enviar correo** (destinatario, asunto y cuerpo), **Enviar SMS** (teléfono y texto) y **Llamar webhook** (RF-16). Las tres son llamadas HTTP a un tercero (en el MVP, el simulador) y siguen RN-09 y RN-29.
- **CA-06a** Cuando se ejecuta Enviar correo, entonces el simulador recibe exactamente un correo con las variables rellenas.
- **CA-06e** Dado un flujo con una acción no registrada, cuando se valida, entonces es inválido y el error nombra la acción.
- **CA-06f** Cuando se ejecuta Enviar SMS, entonces el simulador recibe exactamente un SMS con las variables rellenas.
- **CA-06g** Dada una variable sin valor en el contexto, cuando se ejecuta la acción, entonces no se envía nada y la ejecución queda Fallida con el nombre de la variable (RN-41).

**RF-07 Temporizadores.** Un nodo Temporizador espera una duración fija antes de seguir.
- **CA-07a** Dado un temporizador de 2 h iniciado en t0, cuando el reloj avanza 1 h 59 min, entonces la ejecución sigue Esperando; cuando llega a 2 h, entonces continúa con el nodo siguiente.

**RF-09 Reintentos.**
- **CA-09a** Dado un servicio que falla en los intentos 1 y 2 y acepta el 3, cuando se ejecuta la acción, entonces se hacen 3 intentos con esperas de 1 min y 5 min y el paso queda correcto con 3 intentos registrados.
- **CA-09b** Dado un servicio que siempre falla, cuando se ejecuta la acción, entonces se hacen 4 intentos en total y la ejecución queda Fallida (RN-09).

**RF-16 Webhook saliente.** La acción "Llamar webhook" hace un HTTP POST a una URL con cabeceras y un cuerpo con variables, y guarda la respuesta en el contexto.
- **CA-16a** Dada una acción con URL, cabeceras y cuerpo con `{evento.usuarioId}`, cuando se ejecuta, entonces el receptor simulado recibe exactamente un HTTP POST con el cuerpo relleno.
- **CA-16b** Dado un receptor que falla por red en los intentos 1 y 2 y responde 200 en el 3, entonces se reintenta con las esperas de RN-09 y el paso queda correcto con 3 intentos registrados.
- **CA-16c** Dado un receptor que responde 500, entonces se reintenta; dado uno que responde 400, entonces no se reintenta y la ejecución queda Fallida (RN-29).
- **CA-16d** Dado un receptor que no responde en 10 s, entonces cuenta como fallo de red (RN-29).
- **CA-16e** Cada llamada lleva su referencia única en una cabecera (RN-06).
- **CA-16f** Una URL con formato inválido hace inválido el flujo al validarlo (RN-10).
- **CA-16g** Dado un receptor que responde 200 con un cuerpo JSON, entonces el código y el cuerpo quedan en `nodos.<id>` del contexto y una condición posterior puede leerlos.

**RF-10 Persistencia y reanudación.** Las ejecuciones sobreviven a un reinicio del sistema.
- **CA-10a** Dadas 100 ejecuciones Esperando, cuando el sistema se detiene de forma abrupta y se reinicia, entonces las 100 siguen Esperando con el tiempo restante correcto y se reanudan al vencer.
- **CA-10b** Dada una ejecución cuyo temporizador venció mientras el sistema estaba detenido, cuando se reinicia, entonces continúa con el nodo siguiente y no repite pasos ya registrados.
- **CA-10c** Dada una caída justo después de que el simulador aceptó un envío, cuando el sistema se reinicia y reanuda, entonces el simulador registra un solo mensaje para esa referencia.

**RF-11 No duplicación.**
- **CA-11a** Dado el mismo evento (mismo identificador) entregado dos veces, entonces se crea una sola ejecución por flujo (RN-39).
- **CA-11b** Dado un servicio que acepta el mensaje pero responde después del tiempo de espera, cuando el sistema reintenta, entonces el simulador registra un solo mensaje para esa referencia.

**RF-13 Concurrencia.**
- **CA-13a** Dados 200 eventos que llegan a la vez, entonces se crean 200 ejecuciones, ninguna se pierde y ninguna mezcla el contexto de otra.
- **CA-13c** Dados dos procesadores en paralelo sobre la misma base de datos y 100 temporizadores vencidos, entonces cada paso se ejecuta exactamente una vez.

**RF-18 Estados de la ejecución.**
- **CA-18a** Para cada par de estados de la sección 5.1, la transición se acepta si está permitida y se rechaza si no (prueba parametrizada sobre la tabla completa).
- **CA-18b** Una ejecución en estado terminal no cambia de estado.
- **CA-18c** El historial muestra el estado actual y cada cambio de estado de la ejecución con su instante (RN-30).

**RF-12 Historial de ejecuciones.**
- **CA-12a** Dada una ejecución terminada, entonces el historial muestra por cada paso: nodo, inicio, fin, resultado (correcto o error), entrada resumida y número de intento (RN-12).
- **CA-12b** El historial puede filtrarse por flujo y por estado de la ejecución.

**RF-19 Flujo de ejemplo del enunciado.** La instalación trae el flujo «Recordatorio de activación», activo y válido: Evento `usuario.registrado` → Temporizador 2 h → Llamar webhook `verificar` (consulta el estado de la cuenta) → Condición `nodos.verificar.respuesta.activo = true`; en «no»: Enviar correo de recordatorio al usuario → Enviar SMS a soporte.
- **CA-19a** Tras la instalación, el flujo existe, está activo y pasa la validación.
- **CA-19b** Dado el receptor HTTP configurado para responder `{"activo": false}` y un evento `usuario.registrado` en t0, entonces a las 2 h se hace el POST de verificación, el simulador recibe un correo para el usuario y un SMS para soporte, y la ejecución queda Completada.
- **CA-19c** Dado el receptor configurado para responder `{"activo": true}`, entonces no se envía correo ni SMS y la ejecución queda Completada.

### 7.3 Editor visual

**RF-40 Editor visual.**
- **CA-40a** Cuando un Editor agrega nodos de los cuatro tipos, los conecta, los mueve y los elimina, entonces el diagrama refleja los cambios y se conserva al guardar y reabrir.
- **CA-40b** No se puede conectar un nodo consigo mismo.
- **CA-40c** El Editor puede validar el flujo desde el editor y ver el resultado de RF-02.
- **CA-40d** Dada una paleta con los tipos de nodo, cuando el Editor arrastra un tipo al lienzo y lo suelta, entonces se crea un nodo de ese tipo en esa posición.
- **CA-40e** Dado un flujo guardado en el editor, cuando se activa, entonces pasa por validación y compilación (RF-02, RF-14) y el editor muestra el resultado.
- **CA-40f** Al seleccionar un nodo, el Editor edita su identificador y sus parámetros (tipo de evento, condición, duración, destinatario, URL, cabeceras, cuerpo) y los cambios se conservan al guardar.

### 7.4 Acceso

**RF-30 Inicio de sesión y roles.** Acceso con usuario y contraseña. Dos roles fijos: **Editor** (diseña, valida, activa y desactiva flujos, envía eventos de prueba y consulta) y **Lector** (solo consulta flujos, ejecuciones e historial). Las cuentas se crean al instalar, desde la configuración. El sistema origen se identifica con la clave de integración, no con una cuenta. El simulador no exige inicio de sesión: representa a servicios externos.
- **CA-30a** Sin sesión, cualquier acceso a datos o acciones es denegado, salvo iniciar sesión y la recepción de eventos (CA-30d).
- **CA-30b** Un Lector puede consultar flujos, ejecuciones e historial; cuando intenta guardar, activar o desactivar un flujo o enviar un evento de prueba, entonces se rechaza sin cambiar nada. Un Editor puede hacerlo todo.
- **CA-30c** Tras 5 intentos fallidos consecutivos, la cuenta se bloquea 5 minutos.
- **CA-30d** Un evento enviado a `POST /api/eventos` sin la clave de integración válida se rechaza y no crea ejecuciones.
- **CA-30e** En la interfaz, el Lector ve el editor en solo lectura, sin controles para modificar, activar ni enviar eventos.
- **CA-30f** Al cerrar sesión, la sesión deja de ser válida.

### 7.5 Simulador de APIs de terceros

**RF-90 Simulador.** Aplicación aparte con receptores de **correo**, **SMS** y **HTTP POST**. Registra lo recibido (instante, destino, contenido y referencia), descarta duplicados con la misma referencia y lo muestra en su propia interfaz.
- **CA-90a** Dado un correo enviado, entonces el simulador lo muestra con destinatario, asunto, cuerpo y referencia.
- **CA-90b** Dados un SMS y un HTTP POST, entonces el simulador los muestra con destino, contenido y referencia.
- **CA-90c** Dados dos mensajes con la misma referencia, entonces el simulador registra uno solo y responde con éxito a ambos.
- **CA-90d** Quien opera el simulador puede filtrar lo recibido por servicio y vaciar el registro.
- **CA-90f** La interfaz del simulador indica que es una simulación y no un servicio real.
- **CA-90g** El receptor HTTP responde, por ruta, con el código y el cuerpo JSON que se configure (por defecto, 200 y `{}`).

**RF-91 Inyección de fallos.**
- **CA-91a** Quien opera el simulador puede configurar por servicio: error con código (4xx o 5xx), latencia en segundos, caída de red y respuesta tardía (registra el mensaje y responde después del tiempo de espera).
- **CA-91b** Un fallo configurado para las siguientes N solicitudes se aplica a esas N y luego se desactiva solo.
- **CA-91c** Los fallos se pueden activar desde la interfaz del simulador y desde las pruebas automáticas.

---

## 8. Requisitos no funcionales

| ID | Requisito | Criterio de verificación |
|---|---|---|
| RNF-08 | **Testabilidad.** | **CA-N08a** Toda espera se verifica con reloj controlable. **CA-N08b** El simulador sirve de doble de pruebas con la inyección de fallos de RF-91. **CA-N08c** La suite de aceptación completa se ejecuta en 10 min o menos sin acceso a servicios externos reales. |
| RNF-07 | **Seguridad.** | **CA-N07a** Ninguna operación devuelve una contraseña ni la clave de integración, y ninguna de las dos aparece en registros técnicos. |
| RNF-09 | **Localización.** | **CA-N09a** Interfaz en español; fechas dd/mm/aaaa y hora de 24 h. |
| RNF-14 | **Instalación reproducible.** | **CA-N14a** [HUMANO] Desde un repositorio limpio, siguiendo el README, `docker compose up` deja funcionando la aplicación, el simulador y el flujo de ejemplo en 15 min o menos. |

---

## 9. Requisitos del arnés y del experimento

**Objetivo del experimento.** Estudio de caso **exploratorio** sobre si agentes de programación pueden construir y mantener este sistema bajo un arnés (especificación versionada, pruebas como contrato, verificaciones automáticas). Con muestras pequeñas **no hay significancia estadística**: los resultados se presentan como observaciones, no como conclusiones generales. Inspiración: [OpenAI, "Harness engineering"](https://openai.com/es-419/index/harness-engineering/).

| ID | Requisito | Criterio de verificación |
|---|---|---|
| RH-01 | **Trazabilidad ejecutable.** Todo CA tiene al menos una prueba automática que lo cita por su ID. | El arnés informa CA con prueba / CA totales (excluye los **[HUMANO]**). Meta MVP: 100 % de los CA comprometidos en `harness/scope.json`; el resto de los CA de RF se informan como pendientes. Los CA de RNF se informan aparte con su propia meta. |
| RH-02 | **Las pruebas de aceptación son el contrato.** Los agentes no las modifican. | Si un cambio toca `tests/Acceptance/`, este documento, la sección 3 de la arquitectura o `harness/scope.json` sin la aprobación de una persona revisora, el arnés falla (AR-09). |
| RH-03 | **Tarea de agente.** Cada tarea nombra los RF y CA objetivo y se evalúa automáticamente (pasa o falla). | Una tarea sin CA objetivo es rechazada por el arnés. |
| RH-04 | **Registro por tarea.** Se registra: modelo, tarea, inicio y fin, iteraciones, consumo de uso o costo estimado, resultado e intervenciones humanas. | Cada tarea ejecutada tiene su registro completo. |
| RH-05 | **Reproducibilidad.** Una tarea puede repetirse desde un estado base identificado del repositorio. | Dos ejecuciones desde el mismo estado base usan la misma entrada y la misma especificación. |
| RH-06 | **Retroalimentación legible por agentes.** Un fallo indica el CA incumplido y qué se esperaba frente a qué se obtuvo. | Todo fallo de la suite cita un ID de CA. |
| RH-07 | **Reglas estructurales.** El arnés verifica automáticamente todas las reglas AR de [arquitectura.md](arquitectura.md), sección 3. | Todas las reglas AR se ejecutan en cada corrida; una violación hace fallar la corrida con un mensaje de remediación. |
| RH-08 | **Métricas.** M1 tareas aprobadas a la primera; M2 iteraciones por tarea; M3 intervenciones humanas por tarea; M4 regresiones tras un cambio de requisito; M5 consumo de uso por tarea; M6 tiempo hasta pasar todas las pruebas. | Las seis se calculan a partir de los registros de RH-04. |
| RH-09 | **Comparación con y sin arnés.** Las mismas tareas se ejecutan con y sin arnés, y entre los modelos disponibles. | Mínimo 8 tareas por condición (a ajustar según presupuesto). |
| RH-10 | **Escalabilidad.** M1 a M5 se miden cuando está implementado el 25 %, 50 % y 100 % de los RF. | Tres puntos de medición registrados. |
| RH-11 | **Cambios de requisito controlados.** Se introducen al menos 2 (por ejemplo, cambiar las esperas de RN-09 y añadir una acción al catálogo de RF-06). | Para cada cambio se registran: pruebas que fallan, archivos modificados e intervenciones. |
| RH-12 | **Revisión humana.** Cada cambio integrado tiene un revisor humano y una lista de comprobación. | 100 % de los cambios integrados con revisor registrado. Los autores pueden explicar cada parte entregada **[HUMANO]**. |
| RH-13 | **Especificación versionada.** Toda medición indica la versión de este documento. | Cada registro de RH-04 incluye la versión. |
| RH-14 | **Presupuesto de uso.** El arnés lleva la cuenta del uso por modelo y se detiene al alcanzar el límite configurado. | Al alcanzar el límite, no se inicia ninguna tarea nueva. |
