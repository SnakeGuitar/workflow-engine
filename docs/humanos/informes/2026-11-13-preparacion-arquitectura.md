# Preparación para detallar la arquitectura

| Campo | Valor |
|---|---|
| Fecha | 2026-11-13 |
| Revisa | `docs/requisitos.md` v0.12 y `docs/arquitectura.md` v0.10 |
| Resultado aplicado | `docs/requisitos.md` v0.13 y `docs/arquitectura.md` v0.11 |
| Pregunta | ¿Están los requisitos y las ADR lo bastante cerrados para detallar la arquitectura (esquemas, modelo de datos, algoritmo del motor, contratos)? |

## 1. Veredicto

Sí, tras las correcciones de este informe. Antes de ellas, dos huecos impedían escribir el modelo de datos y el esquema de paquete: el consentimiento no tenía dueño (¿caso o contacto?) y «Reparaciones» no declaraba sus campos propios. Otros dos impedían escribir los flujos incluidos: RF-25 necesitaba dos disparadores con un solo evento de inicio, y RN-38 metía las respuestas del cliente en el flujo de Agendada.

Lo que sigue abierto (sección 4) no bloquea el detalle del motor, de los flujos ni de los paquetes.

## 2. Huecos encontrados y cómo se resolvieron

| # | Hueco | Por qué bloqueaba | Resolución | Dónde |
|---|---|---|---|---|
| P1 | El consentimiento no tenía modelo: RF-20 lo registra al crear el caso, pero RN-35 lo retira «al cliente» | No se puede decidir la tabla ni la consulta de envío | Pertenece al contacto en un canal, con fecha, origen y retirada; lo comparten todos sus casos | glosario, ADR-11 |
| P2 | «Reparaciones» no declaraba campos propios; CA-20a y RF-22 usan marca, modelo, falla y monto | El paquete `packs/reparaciones.json` no se puede escribir | `marca`, `modelo`, `falla` (texto, obligatorios) y `monto` (número, opcional) | requisitos §6 |
| P3 | RF-25 es un solo flujo con dos disparadores; RN-10 exige un solo evento de inicio | El flujo incluido no validaría | El evento «el caso entra a…» admite varios estados; una condición sobre el estado elige la rama | RF-04, RF-25, ADR-06 |
| P4 | RN-38 metía las respuestas «1» y «2» en el flujo que arranca en Agendada | Con «una ejecución en curso por flujo y caso» (RN-05), cada respuesta reemplazaría el seguimiento | Segundo flujo, «Respuesta de cita», disparado por «el cliente responde» | RN-38, RF-52, RF-53 |
| P5 | RF-27 no decía qué parámetros crean versión | CA-27a cita RN-11 pero la ventana o la zona horaria no son de un flujo | Plazos y textos: versión nueva del flujo. Ventana, zona y reintentos: configuración de la instalación | RF-27, ADR-21 |
| P6 | Cambio de horario sin decidir (OQ-06) | Determina cómo se guardan y calculan los vencimientos | UTC en base de datos; ventana y anclados en zona IANA del negocio; duraciones fijas en tiempo transcurrido | RN-08, ADR-04, OQ-06 |
| P7 | La escalera recortaba RF-38 en (2), pero un caso de Messenger solo nace de una consulta (CA-37c) | El recorte (2) dejaba sin uso RF-37, que es M | RF-38 va con Messenger en (6); el recorte (2) pasa a RF-27 | requisitos §2 |
| P8 | Las entradas de bandeja con opciones (RF-28, RN-38) no tenían acción que las creara | Faltaba un parámetro en el catálogo de acciones | «Notificar al dueño» admite opciones de destino; elegir una es un cambio de estado hecho por una persona | ADR-06, glosario |
| P9 | Variables de plantilla sin definir; CA-27c rechaza las desconocidas | El validador de plantillas no tiene lista | Campos comunes, campos propios del paquete y `negocio` | ADR-11 |
| P10 | RN-05 (b) no decía si el flujo que sobrevive conserva sus esperas | Ambigüedad en el algoritmo de cancelación | Aclarado: no pierde sus esperas | RN-05 |

## 3. Decisiones tomadas por defecto (revisar)

1. **Consentimiento por canal y contacto** (P1). La alternativa, por caso, obligaría a pedirlo en cada caso nuevo del mismo cliente y haría ambiguo «baja».
2. **Zona horaria** (P6). México ya no cambia de horario en casi todo el país; la regla general cubre ambos casos.
3. **RF-27 como recorte (2)** (P7). Sin él quedan los valores por defecto, que ya cumplen RN-03, RN-04 y RN-38.
4. **`marca` y `modelo` sin marcar como buscables** (P2). Ningún CA lo pide; añadirlo es una versión nueva del paquete.

## 4. Lo que sigue abierto y no bloquea

| Tema | Por qué no bloquea | Cuándo hace falta |
|---|---|---|
| Política de IDs (§0.1 frente al historial) | No afecta al diseño | Antes del siguiente cambio de significado |
| OQ-05 cómo se registra el consentimiento | En el MVP es una casilla con fecha (RF-20) | Producto real |
| OQ-17 tope y tarifas de WhatsApp | Configuración del simulador con valores por defecto | H3 |
| OQ-18 horario de atención y días no laborables | El MVP solo usa la ventana de avisos | EXT |
| OQ-01 a OQ-04 (garantía, abandono, cancelaciones) | Son datos del paquete, no del motor | Nueva versión del paquete |

## 5. Qué detallar y en qué orden

`arquitectura.md` sigue siendo el índice de decisiones (ADR) y reglas (AR). El detalle va en documentos aparte, en `docs/diseno/`, que `AGENTS.md` citará como fuente de verdad para implementar.

| Orden | Documento | Contenido | Lo necesita |
|---|---|---|---|
| 1 | `esquemas/flujo.schema.json`, `dag.schema.json`, `paquete.schema.json` | JSON Schema del diagrama, del DAG compilado y del paquete; ejemplos válidos e inválidos | H0–H1 (validador, compilador, paquetes, oráculo) |
| 2 | `datos.md` | Tablas del motor y de casos: flujos y versiones, ejecuciones, pasos, temporizadores, eventos, avisos (bandeja de salida), consentimientos, casos, bandeja; claves, índices y restricciones únicas | H0–H2 |
| 3 | `motor.md` | Reclamo con concesión, paso transaccional, cancelación y reemplazo (RN-05), reintentos, ventana, anclados y reanudación; diagramas de secuencia | H2 |
| 4 | `api/openapi.yaml` por hito | Contrato HTTP; se fija antes de que el oráculo escriba pruebas | H0 en adelante |
| 5 | `simulador.md` | API del simulador y de inyección de fallos | H0 (correo, HTTP POST) y H3 |
| 6 | `arnes.md` | Formato de `harness/scope.json`, trazabilidad, freeze y registro de tareas | H0 |

Los dos primeros desbloquean a la vez el arnés (el oráculo prueba contra esquemas y contrato) y la implementación de H0 y H1.
