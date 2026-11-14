# Riesgos

## Del producto

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
| RI-14 | El alcance del MVP creció (segundo paquete, temporizadores anclados, respuestas, Messenger, Marketplace) sobre el mismo plazo | Escalera de recortes ampliada (consultas de Marketplace primero); RF-17, RF-36 y el paquete «Citas» no se recortan; el calendario pone lo nuevo después del motor |
| RI-15 | Las APIs reales de Meta no entregan los mensajes de Marketplace a un tercero (H-41) | En el MVP es simulado y está declarado en ADR-19; si se confirma, RF-38 queda solo como entrada manual o se retira |
| RI-16 | Un cliente responde algo que el flujo no reconoce | Respuestas cerradas («1», «2»), toda respuesta no reconocida va a la bandeja (RN-34) y el dueño puede registrarla a mano (RF-23) |

## De arquitectura

| ID | Riesgo | Mitigación |
|---|---|---|
| AR-R1 | El equipo no domina React; el editor es lo más nuevo para ustedes | Editor mínimo (D-06); contrato OpenAPI; pruebas de interfaz; los agentes escriben el frontend y el equipo lo revisa contra los CA |
| AR-R2 | Concurrencia y reanudación son difíciles de acertar | Pruebas CA-10, CA-13 y CA-N04a desde el primer día, antes que los casos de reparación |
| AR-R3 | "Exactamente una vez" no existe entre sistemas | ADR-03 lo declara: al menos una vez más idempotencia; el requisito de referencia única se vuelve explícito para el canal |
| AR-R4 | Los agentes violan la estructura sin notarlo | Sección 6, reglas automáticas con mensajes de remediación |
| AR-R5 | El plazo es corto | Ver sección 7; el orden prioriza el motor y el arnés |
| AR-R6 | La vinculación de Telegram introduce un flujo de entrada (sondeo) y estados nuevos del caso | Aislarlo en `Infrastructure`; implementarlo después del correo; es lo primero que se recorta (D-08) |
| AR-R7 | El descubrimiento (EXT) podría reclamar tiempo del MVP | Está fuera del MVP (D-14); el núcleo no depende de `Discovery` |
| AR-R8 | Salidas no deterministas de los agentes | Esquema validado, respuestas grabadas, confirmación humana, evaluación aparte (ADR-12 a ADR-14) |
| AR-R9 | Salida de datos del negocio y de sus clientes a un tercero | ADR-13: habilitación por fuente, enmascarado, registro de salida; límite declarado con el texto libre |
| AR-R10 | Los campos propios en JSON limitan las consultas | Solo los campos comunes son buscables en el MVP (ADR-11) |
| AR-R11 | El simulador diverge de la plataforma real, sobre todo en WhatsApp | Se declara como modelo (H-40, CA-90f); el adaptador real es EXT y se validaría contra la plataforma |
| AR-R12 | Los plugins corren sin aislamiento | Se declara como código de confianza (ADR-17); un modelo seguro queda fuera de alcance |
| AR-R13 | La rúbrica podría valorar la arquitectura recomendada | ADR-18 justifica lo que se adopta y lo que no; alternativa de menor costo documentada |
