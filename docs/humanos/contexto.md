# Contexto

## Qué se construye

Lo que pide [project-idea.txt](../../project-idea.txt) más el inicio de sesión con dos roles (D-23, D-25). La trazabilidad de cada tarea del enunciado a sus requisitos está en [requisitos.md](../requisitos.md), sección 1. La arquitectura recomendada por el enunciado no se adopta; la justificación está en [ADR-18](../arquitectura.md#adr-18-no-se-adopta-la-arquitectura-recomendada-por-el-enunciado).

**Demostración:** el ejemplo del enunciado como flujo instalado (RF-19). Un Editor inicia sesión, se envía un evento `usuario.registrado`, se ve la ejecución esperar, el POST de verificación llega al simulador y, según lo que responda, llegan o no el correo y el SMS. Con la inyección de fallos se ven los reintentos; con un reinicio abrupto, la reanudación; con una cuenta de Lector, que no se puede modificar nada.

**Experimento:** el proyecto se construye con agentes bajo un arnés y se mide (requisitos.md, sección 9). El arnés, el calendario de hitos y el alcance comprometido están en [arnes-e-hitos.md](arnes-e-hitos.md).

## Preguntas abiertas

| ID | Pregunta |
|---|---|
| OQ-07 | ¿Qué rúbrica aplica el profesor y qué dice sobre el uso de agentes? |
| OQ-08 | ¿Cuántas tareas y modelos caben en el presupuesto de uso (RH-09, RH-14)? |
| AD-04 | ¿Dónde se ejecuta la demostración final (máquina local o servidor)? |
