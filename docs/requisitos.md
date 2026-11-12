# Especificación de requisitos: motor de flujos para microempresas (MVP)

| Campo | Valor |
|---|---|
| Versión | 0.2 (borrador provisional) |
| Fecha | 2026-11-12 |
| Fecha límite del proyecto | 2026-12-02 |
| Estado | Pendiente de validar con entrevistas a dueños de negocio (ver H-16, H-17) |
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

**Propósito.** Un sistema web que permite definir, validar y ejecutar flujos de trabajo (evento → condiciones → acciones → esperas) y lo aplica a un proceso real de un negocio pequeño: el seguimiento de reparaciones de celulares.

**Competencia real a superar:** la libreta, la hoja de cálculo y las notas del teléfono. La libreta es memoria pasiva; el sistema debe ser **memoria que actúa** (avisa, recuerda, vigila) con un costo de registro comparable al de apuntar a mano.

### Necesidades (HIPÓTESIS, no validadas por entrevistas)

| ID | Necesidad | Hipótesis asociada |
|---|---|---|
| N-01 | El dueño olvida avisar al cliente y dar seguimiento a cotizaciones sin respuesta | H-16 |
| N-02 | Los equipos listos que no se recogen ocupan espacio y dinero inmovilizado | H-22 |
| N-03 | La libreta no avisa nada; todo depende de la memoria del dueño | H-16 |
| N-04 | Registrar un trabajo debe ser tan rápido como apuntarlo | H-23 |
| N-05 | El dueño no tiene conocimientos técnicos y no debe ver conceptos técnicos | H-03 |
| N-06 | El operador (equipo) necesita definir y verificar procesos sin programar cada uno | H-24 |

---

## 2. Alcance

| Etapa | Contenido |
|---|---|
| **MVP** | Motor de flujos con validación previa, esperas, cancelación, reintentos, persistencia y trazabilidad. Un proceso: seguimiento de reparaciones. Vista web móvil para el dueño. Consola web para el operador con editor visual mínimo. Avisos al cliente por un canal de efecto inmediato. |
| **EXT** | Generación de flujos por agentes con confirmación del dueño. Segunda plantilla (citas con recordatorio y lista de espera). Editor visual completo. Varios negocios aislados entre sí. Respuestas del cliente recibidas por el canal. |
| **FUT** | WhatsApp oficial. Estandarización de plantillas personalizadas. Más oficios. Reportes. |
| **Fuera de alcance** | Cobros y facturación. Inventario de refacciones. Aplicación móvil nativa. |

---

## 3. Actores

| Actor | Descripción | Usa el sistema |
|---|---|---|
| **Dueño** | Dueño o encargado del taller. Sin conocimientos técnicos. | Sí (vista móvil simple) |
| **Operador** | Persona del equipo que configura flujos, parámetros y plantillas. | Sí (consola con editor) |
| **Cliente final** | Persona que deja su equipo. Recibe avisos. | No |
| **Canal de aviso** | Servicio externo que entrega mensajes al cliente. | Sistema externo |

---

## 4. Restricciones y supuestos

**Restricciones**
- R-01 Sistema web, accesible desde navegador sin instalar nada.
- R-02 Plazo: 2026-12-02. Equipo de 2 personas con agentes de programación.
- R-03 Uso de modelos limitado por las suscripciones del equipo (H-21).
- R-04 Interfaz en español.

**Supuestos (a verificar)**
- S-01 El taller cuenta con teléfono con navegador y conexión a internet.
- S-02 El cliente final aporta un contacto válido y acepta recibir avisos (H-17).
- S-03 Existe un canal de aviso que entrega mensajes sin aprobación previa de plantillas (D-05).

---

## 5. Glosario

| Término | Definición |
|---|---|
| **Caso** | Una reparación: cliente, equipo, falla, estado, folio. |
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
| **Canal de aviso** | Servicio externo que entrega los avisos. |
| **Reloj controlable** | Fuente de tiempo que las pruebas pueden adelantar. |

---

## 6. Modelo de estados del caso

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

---

## 7. Reglas de negocio

Valores por defecto entre paréntesis; el operador puede modificarlos (RF-27).

| ID | Regla |
|---|---|
| RN-01 | Un caso tiene exactamente un estado. Solo se permiten las transiciones de la sección 6. Un caso no pasa a En reparación sin haber estado Aprobada. |
| RN-02 | Al entrar a **Cotización enviada** se envía un aviso con el monto. Al entrar a **Listo para recoger** se envía un aviso de recogida. |
| RN-03 | Si una cotización no recibe respuesta: recordatorio a las **24 h** y a las **72 h**; a las **120 h** el caso pasa a **Sin respuesta** y se crea una entrada en la bandeja del dueño. |
| RN-04 | Si un equipo en Listo para recoger no se recoge: recordatorios a los **3, 7 y 14 días**; a los **30 días** se crea una solicitud de decisión en la bandeja. El sistema **no** cambia el estado por sí mismo. |
| RN-05 | Cuando un caso cambia de estado, se cancelan los recordatorios y esperas pendientes del estado anterior. |
| RN-06 | Un mismo aviso (mismo caso, mismo evento, misma etapa de recordatorio) se entrega **una sola vez**, aunque haya reintentos, eventos repetidos o reinicios. |
| RN-07 | No se envía ningún aviso a un cliente sin consentimiento registrado o sin contacto válido. En ese caso se crea una entrada en la bandeja. |
| RN-08 | Los avisos solo se envían dentro de la ventana horaria (**09:00 a 20:00**, hora del negocio). Un aviso que vence fuera de ella se difiere al inicio de la siguiente. |
| RN-09 | Un aviso fallido se reintenta hasta **3 veces** con esperas de **1 min, 5 min y 30 min**. Agotados los reintentos, el aviso queda "no entregado" y se crea una entrada en la bandeja. |
| RN-10 | Un flujo es **válido** si: tiene exactamente un evento de inicio; no tiene ciclos; todos sus nodos son alcanzables desde el inicio; toda condición tiene rama por defecto; todo temporizador dura más de 0 y como máximo **90 días**; solo usa acciones del catálogo (RF-06). |
| RN-11 | Cada activación de un flujo crea una versión. Una ejecución conserva la versión con la que empezó, aunque se active una versión nueva. |
| RN-12 | Cada paso de una ejecución queda registrado: nodo, inicio, fin, resultado, entrada resumida, número de intento y error si lo hubo. El registro no se modifica ni se borra. |
| RN-13 | El estado **Abandonado** solo se asigna por decisión explícita del dueño. |
| RN-14 | Un folio es único y nunca se reutiliza. |

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

**RF-04 (M, MVP) Disparo por evento.** Un flujo activo se ejecuta cuando ocurre su evento (el caso entra a un estado).
- **CA-04a** Dado un flujo activo cuyo evento es "el caso entra a Cotización enviada", cuando un caso entra a ese estado, entonces se crea exactamente una ejecución asociada a ese caso y a la versión vigente.
- **CA-04b** Dado que no hay flujo activo para ese evento, cuando un caso cambia de estado, entonces el cambio se completa y no se crea ejecución.

**RF-05 (M, MVP) Condiciones.** Un nodo Condición evalúa datos del caso y elige una rama.
- **CA-05a** Dada una condición "monto > 1000" con ramas verdadero, falso y por defecto, cuando el monto es 1500 entonces sigue verdadero; con 500, falso; sin monto, la rama por defecto.
- **CA-05b** Cuando la condición usa un dato inexistente, entonces la ejecución no se detiene, sigue por la rama por defecto y el hecho queda registrado.

**RF-06 (M, MVP) Catálogo cerrado de acciones.** El sistema ofrece solo estas acciones: **Enviar aviso al cliente**, **Notificar al dueño** (entrada en la bandeja), **Cambiar estado del caso** (solo a Sin respuesta), **Registrar nota** en el caso.
- **CA-06a** Dado un caso con consentimiento y contacto válido, cuando se ejecuta Enviar aviso, entonces el canal recibe exactamente un mensaje con la plantilla rellena y el aviso queda registrado como "enviado".
- **CA-06b** Cuando se ejecuta Notificar al dueño, entonces aparece una entrada en la bandeja con el texto y el caso.
- **CA-06c** Dado un flujo cuya acción Cambiar estado apunta a un estado distinto de Sin respuesta, cuando se valida, entonces es inválido (RN-10).
- **CA-06d** Cuando se ejecuta Registrar nota, entonces la nota aparece en la línea de tiempo del caso (RF-26).

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

### 8.2 Seguimiento de reparaciones (producto)

**RF-20 (M, MVP) Registrar caso.**
- **CA-20a** Dado un dueño autenticado, cuando registra nombre y contacto del cliente, marca, modelo, falla y consentimiento, entonces se crea un caso en Recibido con folio único y fecha.
- **CA-20b** Cuando falta un campo obligatorio, entonces se rechaza indicando el campo.
- **CA-20c** Cuando el contacto tiene formato inválido, entonces se rechaza.
- **CA-20d** Dados dos registros simultáneos, entonces los folios son distintos (RN-14).

**RF-21 (M, MVP) Cambiar estado.**
- **CA-21a** Para **cada** par origen-destino de la sección 6, el cambio se acepta si el par está permitido y se rechaza con motivo si no lo está, sin cambiar el estado. (Prueba parametrizada sobre la tabla completa.)
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

**RF-41 (S, MVP) Errores sobre el diagrama.**
- **CA-41a** Dado un flujo inválido, cuando se valida desde el editor, entonces cada error se muestra junto al o los nodos afectados.

**RF-42 (C, EXT) Ver una ejecución sobre el diagrama.**
- **CA-42a** Al abrir una ejecución desde el historial, cada nodo muestra su estado (pendiente, ejecutado, cancelado o con error).

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
| RNF-08 | **Testabilidad.** | **CA-N08a** Toda espera puede verificarse con reloj controlable. **CA-N08b** El canal de aviso puede sustituirse por un doble que inyecta: error, latencia, duplicado, respuesta tardía y caída. **CA-N08c** La suite de aceptación completa se ejecuta en ≤ 10 min sin acceso a servicios externos reales. |
| RNF-09 | **Localización.** | **CA-N09a** Interfaz en español; fechas dd/mm/aaaa y hora de 24 h. |

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
| RH-07 | **Reglas estructurales.** El arnés verifica automáticamente las reglas de estructura que se definan en la fase de arquitectura. | **Pendiente de la fase de arquitectura.** |
| RH-08 | **Métricas.** M1 tareas aprobadas a la primera; M2 iteraciones por tarea; M3 intervenciones humanas por tarea; M4 regresiones tras un cambio de requisito; M5 consumo de uso por tarea; M6 tiempo hasta pasar todas las pruebas. | Las seis se calculan a partir de los registros de RH-04. |
| RH-09 | **Comparación con y sin arnés.** Las mismas tareas se ejecutan con y sin arnés, y entre los modelos disponibles. | Mínimo 8 tareas por condición (a ajustar según presupuesto, H-21). |
| RH-10 | **Escalabilidad.** M1 a M5 se miden cuando está implementado el 25 %, 50 % y 100 % de los RF de prioridad M. | Tres puntos de medición registrados. |
| RH-11 | **Cambios de requisito controlados.** Se introducen al menos 2 (por ejemplo, cambiar los plazos de RN-03 y añadir un estado "Garantía"). | Para cada cambio se registran: pruebas que fallan, archivos modificados e intervenciones. |
| RH-12 | **Revisión humana.** Cada cambio integrado tiene un revisor humano y una lista de comprobación. | 100 % de los cambios integrados con revisor registrado. Los autores pueden explicar cada parte entregada **[HUMANO]**. |
| RH-13 | **Especificación versionada.** Toda medición indica la versión de este documento. | Cada registro de RH-04 incluye la versión. |
| RH-14 | **Presupuesto de uso.** El arnés lleva la cuenta del uso por modelo y se detiene al alcanzar el límite configurado. | Al alcanzar el límite, no se inicia ninguna tarea nueva. |

---

## 11. Trazabilidad

| Necesidad | Requisitos | Reglas |
|---|---|---|
| N-01 | RF-04, RF-06, RF-07, RF-25 | RN-02, RN-03, RN-05 |
| N-02 | RF-25, RF-28, RF-29 | RN-04, RN-13 |
| N-03 | RF-04, RF-07, RF-29 | RN-03, RN-04 |
| N-04 | RF-20, RF-24, RNF-01 | RN-14 |
| N-05 | RNF-01, RNF-02, RF-30 | (CA-N01c) |
| N-06 | RF-01, RF-02, RF-03, RF-40, RF-41 | RN-10, RN-11 |
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
| D-05 | El canal de aviso concreto se elige en la fase de arquitectura; debe entregar mensajes sin aprobación previa de plantillas. WhatsApp oficial queda como FUT. | Confirmado (2026-11-12) |
| D-06 | El editor visual es obligatorio, mínimo y solo para el operador. | Confirmado por el usuario (el profesor pidió una propuesta) |
| D-07 | La generación de flujos por agentes queda fuera del MVP (EXT). | Confirmado por plazo |

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

### Hipótesis a verificar

H-03 El dueño no es técnico. H-15 La API oficial de WhatsApp exige plantillas aprobadas y plazos de verificación. H-16 El dolor principal es el olvido y el seguimiento. H-17 Los clientes aceptan recibir avisos. H-20 La política académica admite código generado por agentes. H-21 Las suscripciones permiten el número de ejecuciones del experimento. H-22 Los equipos no recogidos son un problema frecuente. H-23 Un registro de 15 s compite con la libreta. H-24 Los operadores prefieren plantillas editables a flujos escritos desde cero.

---

## 13. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| RI-01 | Plazo de 20 días insuficiente | Alcance del MVP recortado a un proceso; prioridades M/S/C |
| RI-02 | Las entrevistas contradicen las hipótesis | Revisar la versión 0.2 con los hallazgos antes de congelar la especificación |
| RI-03 | Cambios de requisito contaminan las mediciones | Versionado y congelamiento por etapas (RH-13) |
| RI-04 | Agotar el uso de los modelos | RH-14 |
| RI-05 | El equipo no entiende lo generado por agentes | RH-12 |
| RI-06 | El canal de aviso elegido impone aprobaciones o costos no previstos | D-05; doble de canal en pruebas (RNF-08) |

---

## 14. Historial de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 2026-11-12 | Borrador provisional inicial. Sin validación con usuarios. |
| 0.2 | 2026-11-12 | Confirmadas las decisiones D-01 a D-05. Sin cambios en RF, RN, RNF ni CA. |
