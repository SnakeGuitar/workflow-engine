# Documentación en español

[English](../en/README.md) · [Proyecto](../../README.md)

- [Requisitos y criterios de aceptación](requirements.md)
- [Arquitectura y reglas verificables](architecture.md)
- [Diagramas UML](design/diagrams/README.md)
- [Enunciado de la materia](project-idea.txt)
- Contexto del equipo: [contexto](human/context.md), [decisiones](human/decisions.md), [riesgos](human/risks.md), [arnés e hitos](human/harness-and-milestones.md), [historial](human/changelog.md)

Los requisitos y la arquitectura prevalecen sobre los diagramas. La versión inglesa es la fuente normativa; ambas versiones deben mantenerse equivalentes. Los IDs estables se comparten entre idiomas. La interfaz del producto, el código, los comentarios, las rutas de API, las propiedades JSON y los ejemplos de código usan inglés.

## Verificación de entrega

Ejecutar desde la raíz del repositorio con Python 3.9+.

```sh
python3 harness/verify.py contract
python3 harness/verify.py local
RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py live
RUN_LIVE_ACCEPTANCE=1 python3 harness/verify.py release
```

El control de contrato verifica identificadores, asignaciones del catálogo y versiones. Revisar la equivalencia semántica bilingüe manualmente. Los controles local y real ejecutan de nuevo el proyecto .NET de aceptación de `tests/Acceptance/` y leen su TRX. Etiquetar pruebas con `[Ac("CA-100a")]`, usar el prefijo de método `CA_100a_` y exponer TestCategory `local` o `live`. Preparar cuentas dedicadas antes de habilitar la ejecución real. Los códigos de salida son 0 aprobado, 1 fallido y 2 bloqueado. La entrega exige ambas suites.

Si el oráculo real no puede acceder a un proveedor necesario o a las credenciales de prueba, registra un resultado inconcluso con el mensaje exacto `provider_unavailable`. Las omisiones ordinarias y las aserciones incumplidas hacen fallar el control.

Cada criterio real declara sus claves de evidencia en la especificación. Tras verificar de forma independiente, el oráculo escribe un archivo JSON por criterio (por ejemplo `CA-101c.json`) en el directorio indicado por `LIVE_ACCEPTANCE_EVIDENCE`. Cada archivo asocia sus claves declaradas con hashes SHA-256 en minúsculas de las referencias remotas observadas. La ausencia de evidencia hace fallar el criterio correspondiente.
