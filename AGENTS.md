# AGENTS.md

Motor de flujos web: editor de nodos → validación y compilación a DAG → motor asíncrono con esperas y reintentos → webhooks y simulador de correo, SMS y HTTP POST, con inicio de sesión y dos roles (Editor y Lector). El alcance es el de [project-idea.txt](project-idea.txt) más el acceso con roles. Plazo: 2026-12-02.

## Qué leer
- [docs/requisitos.md](docs/requisitos.md): qué construir. Los **CA** son el contrato. Lee los RF/CA de tu tarea más §4 (glosario), §5 (estados) y §6 (reglas).
- [docs/arquitectura.md](docs/arquitectura.md): cómo. ADR vigentes, estructura de la solución (ADR-07) y reglas **AR** verificables (§3).
- [docs/diseno/diagramas/](docs/diseno/diagramas/): vistas UML. Si contradicen los dos documentos anteriores, mandan los documentos.
- `docs/humanos/`: contexto, decisiones e historial para el equipo. **No lo leas** para implementar.

## Reglas duras
- Tu tarea nombra los RF y CA objetivo (RH-03). Sin CA objetivo, pide aclaración.
- Nada fuera de `docs/requisitos.md`. Si algo no está ahí, no se implementa.
- No modifiques `tests/Acceptance/`, `docs/requisitos.md`, la sección 3 de `docs/arquitectura.md` ni `harness/scope.json` sin aprobación humana (línea `Approved-By:` en el commit con la persona que aprobó el PR). No escribas tú esa línea.
- El alcance comprometido está en `harness/scope.json`.
- Registra cada tarea en `harness/tasks.ndjson` con el comando `task` del arnés (RH-04).
- Todo test de aceptación lleva el ID de su CA (`[Ac("CA-07a")]`). Un fallo debe citar el CA.
- Contraseñas y clave de integración nunca a logs (AR-05).
- El tiempo solo por `TimeProvider` (AR-03). Nunca `DateTime.Now`/`UtcNow`.
- Dependencias de un solo sentido: `Api → Infrastructure → Core` y `Api → Core`. `Core` no conoce base de datos, HTTP ni ASP.NET (AR-01). Nadie referencia `Simulator` (AR-16).
- Ningún archivo de código supera 400 líneas (AR-07).

## Pila
.NET (LTS) + ASP.NET Core, PostgreSQL (EF Core + Npgsql), React + TypeScript (React Flow). Monolito modular, una instancia; sin microservicios, Saga ni Redis (ADR-18). Verifica versiones de paquetes al instalarlas.

## Cambios en la documentación
Un cambio en `docs/requisitos.md` o `docs/arquitectura.md` describe solo el estado vigente, sin historial ni "antes decía". El historial va a `docs/humanos/historial.md`; los IDs nunca se reutilizan.
