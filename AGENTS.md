# AGENTS.md

Motor de flujos web (editor de nodos → DAG → motor asíncrono) con simulador de terceros. Primer caso: seguimiento de reparaciones. Plazo MVP: 2026-12-02.

## Qué leer
- [docs/requisitos.md](docs/requisitos.md): qué construir. Los **CA** son el contrato. Lee solo los RF/CA de tu tarea más §5 (glosario), §6 (estados) y §7 (reglas).
- [docs/arquitectura.md](docs/arquitectura.md): cómo. ADR vigentes, estructura de la solución (ADR-07) y reglas **AR** verificables (§3).
- `docs/humanos/`: contexto, decisiones, historial y riesgos para el equipo. **No lo leas** para implementar; no es fuente de verdad.

## Reglas duras
- Tu tarea nombra los RF y CA objetivo (RH-03). Sin CA objetivo, pide aclaración.
- No modifiques `tests/Acceptance/`, `tests/Fixtures/`, `docs/requisitos.md` ni la sección 3 de `docs/arquitectura.md` sin aprobación humana (línea `Approved-By:` en el commit).
- Todo test de aceptación lleva el ID de su CA (`[Ac("CA-08a")]`). Un fallo debe citar el CA.
- El tiempo solo por `TimeProvider` (AR-03). Nunca `DateTime.Now`/`UtcNow`.
- Dependencias de un solo sentido: `Api → Discovery → Cases → Core`; `Infrastructure → Cases, Discovery, Core`. `Core` no conoce ningún dominio. En el MVP `Discovery` no se crea y `Api` no lo referencia. Los estados salen del paquete en `packs/`, nunca de literales (AR-10).
- Ningún archivo de código supera 400 líneas (AR-07).
- Contactos y credenciales nunca a logs (AR-05).
- Alcance: solo `MVP`. Lo marcado `EXT` o `FUT` no se implementa sin pedido explícito.

## Pila
.NET (LTS) + ASP.NET Core, PostgreSQL (EF Core + Npgsql), React + TypeScript (React Flow). Monolito modular, una instancia. Verifica versiones de paquetes al instalarlas.

## Cambios en la documentación
Un cambio en `docs/requisitos.md` o `docs/arquitectura.md` describe solo el estado vigente, sin historial ni "antes decía". El historial va a `docs/humanos/historial.md`; los IDs nunca se reutilizan.
