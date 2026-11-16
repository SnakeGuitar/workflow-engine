# Riesgos

Los riesgos propios del arnés están en [arnes-e-hitos.md](arnes-e-hitos.md), sección 1.5.

| ID | Riesgo | Mitigación |
|---|---|---|
| RI-01 | Plazo corto (17 días) | Alcance limitado al enunciado más el acceso (D-23, D-25); lista de recortes en requisitos.md §1; criterios de muerte por hito |
| RI-03 | Los cambios de requisito contaminan las mediciones | Especificación versionada y medición con su versión (RH-13) |
| RI-04 | Agotar el uso de los modelos | Presupuesto con corte por tarea (RH-14) |
| RI-05 | El equipo no entiende lo generado por agentes | Revisión humana de cada cambio (RH-12); oráculo separado para los CA comprometidos (ADR-09) |
| RI-13 | La rúbrica valora la arquitectura recomendada aunque no la exija | ADR-18 justifica cada punto; CA-13c demuestra que no hace falta Redis |
| AR-R1 | El equipo no domina React; el editor es lo más nuevo | Editor mínimo (D-06) sobre React Flow; contrato OpenAPI; pruebas con Playwright |
| AR-R2 | Concurrencia y reanudación son difíciles de acertar | CA-10, CA-11 y CA-13 en H2, antes del flujo de ejemplo; criterio de muerte el 24 de noviembre |
| AR-R3 | "Exactamente una vez" no existe entre sistemas | ADR-03: al menos una vez más idempotencia, con referencia única que el receptor deduplica |
| AR-R4 | Los agentes violan la estructura sin notarlo | Reglas AR automáticas con mensajes de corrección (RH-07) |
