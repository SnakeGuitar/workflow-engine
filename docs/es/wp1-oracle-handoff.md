# Encargo de WP1 para el agente independiente de aceptación

Empieza en una sesión nueva sin historial de implementación. Entrega el contrato público OpenAPI y un proyecto .NET de aceptación ejecutable en `tests/Acceptance/`. La persona revisora aprueba y congela el oráculo. Este encargo se preparó sobre `7837577`, especificación v1.3. Registra el SHA real de partida al comenzar.

## Fuentes e independencia

Lee [AGENTS.md](../../AGENTS.md), el glosario, los estados, las reglas y los CA objetivo en [requirements.md](requirements.md), y ADR-08 y ADR-09 en [architecture.md](architecture.md). Consulta [scope.json](../../harness/scope.json) para la cobertura y [verify.py](../../harness/verify.py) para las convenciones de ejecución. Deriva el comportamiento esperado únicamente de los CA y del contrato público. Mantén fuera de la sesión el código del producto, los PR de implementación, las pruebas unitarias y el historial del implementador. Usa las interfaces públicas del host de pruebas y del reloj de `tests/Support/` para conectar el entorno. Si falta una capacidad de prueba, describe qué necesita observar o controlar el CA. Consulta con la persona revisora las decisiones de contrato pendientes.

## Cobertura de v1.3

| Ejecución | Criterios |
|---|---|
| Local · diagramas y versiones | CA-02a, CA-02c, CA-02g, CA-14a, CA-14b, CA-14d, CA-03b |
| Local · ejecución y entregas | CA-04a, CA-07a, CA-09a, CA-10b, CA-16a, CA-16b, CA-19b |
| Local · acceso y editor | CA-30a, CA-40a, CA-40d, CA-40e |
| Local · conexiones y extensibilidad | CA-100a, CA-100b, CA-100c, CA-100d, CA-100e, CA-100f, CA-101a, CA-101b |
| Local · artefactos | CA-102a, CA-102b, CA-102c, CA-102d, CA-102e |
| Live · cuentas dedicadas | CA-100g, CA-101c |
| Escenarios de apoyo, con reporte separado | CA-10a, CA-12a, CA-90a |

## Secuencia de trabajo

1. Redacta OpenAPI en inglés a partir de los requisitos públicos. Describe las solicitudes, respuestas, esquemas, autenticación y errores necesarios para ejecutar los escenarios. Incluye cookies de sesión, permisos Editor/Reader, recepción de eventos con clave de integración, workflows, ejecuciones, conexiones, catálogo y descarga de artefactos. Identifica las decisiones que requieren resolución humana antes de convertirlas en aserciones.
2. Escribe escenarios de caja negra en un único proyecto directamente bajo `tests/Acceptance/`. Para la suite local usa la factoría de la aplicación, PostgreSQL real de prueba, reloj controlable y servicios controlados. Usa Playwright para los criterios de UI. Adelanta el reloj para probar temporizadores y reintentos. Comprueba resultados observables, incluida la ausencia de efectos secundarios cuando el CA lo exija.
3. Etiqueta cada método con el CA exacto y un nombre correspondiente, por ejemplo `[Ac("CA-07a")]` y `CA_07a_TimerResumesAfterDelay`. Configura la categoría para que el ejecutor seleccione `TestCategory=local` o `TestCategory=live`. Cada fallo identifica el CA, el resultado esperado y el observado. Ejercita el comportamiento incluso si está pendiente de implementación.
4. Para cada CA live, lee el resultado de forma independiente en el proveedor. Si pasa, escribe `<CA-ID>.json` en el directorio `LIVE_ACCEPTANCE_EVIDENCE` provisto por el ejecutor. Usa las claves de evidencia declaradas en ese CA y una referencia redactada de 64 caracteres hexadecimales en minúsculas por clave. Excluye credenciales y cargas útiles de los registros e informes. Registra los prerrequisitos live que falten como bloqueo.
5. Publica commits pequeños que compilen tan pronto como compruebes cada paso. Antes de cada commit ejecuta el build con advertencias como errores, Unit, Architecture, el contrato y la comprobación de espacios de Git. Documenta en el PR los fallos reales de aceptación para que el implementador pueda atenderlos. Registra la tarea conforme a RH-04. Una persona revisa el contrato y las pruebas protegidas, aporta la aprobación exigida por AR-09 y congela el oráculo.

## Ejecución y reporte

Desde la raíz del repositorio, comprueba el contrato antes y después del trabajo. Con el proyecto y sus prerrequisitos locales disponibles, ejecuta la suite local.

```sh
python3 harness/verify.py contract
python3 harness/verify.py local
```

La suite local debe descubrir los 31 CA locales comprometidos y terminar en un máximo de 600 segundos. Informa por separado cuántos se requieren, cuántos se cubren y cuántos pasan. Identifica cada CA fallido o ausente. Distingue los fallos del ejecutor o del descubrimiento de pruebas de los fallos observados en el producto. Con cuentas reales dedicadas y autorizadas, ejecuta `RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py live`. Usa `python3 harness/verify.py release` para el veredicto conjunto. Los códigos de salida son 0 para aprobado, 1 para fallo y 2 para bloqueo. Incluye el SHA de partida, la versión de la especificación, los veredictos obtenidos y el estado de la revisión humana. WP1 entrega el instrumento de evaluación revisado. Los paquetes siguientes implementan el comportamiento que evalúa.
