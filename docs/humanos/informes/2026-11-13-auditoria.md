# Auditoría de las correcciones del informe de arnés e hitos

| Campo | Valor |
|---|---|
| Fecha | 2026-11-13 |
| Revisa | [2026-11-13-arnes-e-hitos.md](2026-11-13-arnes-e-hitos.md) y sus correcciones aplicadas |
| Fuentes auditadas | `docs/requisitos.md` v0.11, `docs/arquitectura.md` v0.9, `AGENTS.md`, `docs/humanos/*` |
| Resultado aplicado | `docs/requisitos.md` v0.12, `docs/arquitectura.md` v0.10, `harness/scope.json` |

> Las referencias `archivo:línea` de la sección 2 apuntan a las versiones auditadas (requisitos v0.11, arquitectura v0.9), no a las vigentes. La sección 6 registra qué se corrigió y dónde.

---

## 1. Veredicto

- Los cambios estaban aplicados y la integridad de IDs era buena: sin duplicados, ADR-01…20 y AR-01…19 sin huecos, y un solo ID citado que no existía (`RF-94c/d`). Pero introducían **cuatro incoherencias nuevas**: ADR-06 contradecía el nuevo RN-05, AR-08 contradecía el nuevo RH-01, RH-07 seguía diciendo «AR-01 a AR-18» y el quinto recorte volvía a quitar parte de un RF de prioridad M.
- Quedaba una contradicción de fondo que el informe no recogía: RN-05 cancelaba todas las esperas al cambiar el estado, pero el flujo de «Citas» (RN-38, CA-53c, CA-53g) necesita que sus esperas sigan vivas tras pasar de Agendada a Confirmada.
- El plan solo era viable congelando **un único** alcance. El MVP-0, ampliado para unir editor y motor, es alcanzable; los hitos H0–H5 originales no, por dependencias invertidas y criterios de muerte que incumplían la §2.

## 2. Hallazgos

| # | Severidad | archivo:línea | Hallazgo | Corrección propuesta |
|---|---|---|---|---|
| 1 | bloqueante | requisitos.md:182 vs :215, :470, :474 | RN-05 cancela todas las esperas del estado anterior. RN-38, CA-53c («el recordatorio de 2 h sí se envía») y CA-53g necesitan que sobrevivan al paso de Agendada a Confirmada. | RN-05 admite flujos que declaran que sobreviven al cambio de estado; RN-38 lo declara. |
| 2 | bloqueante | arquitectura.md:59, :220 | ADR-06 y §2 cancelan la ejecución entera con `cancelarAlSalirDelEstado`; el nuevo RN-05 dice que el paso que hace el cambio continúa. CA-25b falla por diseño. | Excepción explícita para la ejecución que origina el cambio. |
| 3 | bloqueante | arquitectura.md:260 vs requisitos.md:600 | AR-08 exige prueba para todo CA M (153 CA M/MVP); RH-01 ya se limita al alcance comprometido. O el CI está en rojo desde el día 1 o AR-08 no se ejecuta. | AR-08 contra un manifiesto de alcance versionado. |
| 4 | bloqueante | requisitos.md:600 | RH-01 remite a «la propuesta del equipo», que no está en el repositorio ni es legible por máquina. | `harness/scope.json` protegido. |
| 5 | bloqueante | informe:237, :244, :250-251 | Dependencias invertidas: H2 usa estados y cambios de estado de «Reparaciones» que llegan en H3; H3.5 necesita respuestas del cliente que llegan en H4. | Paquete mínimo en H1; respuestas antes de «Citas». |
| 6 | contradicción | requisitos.md:48 | El recorte 5 cita «RF-94c y RF-94d», que no existen, y RF-94 es M: repite el defecto A8. | Recorte 5 sobre un RF S (RF-41). |
| 7 | contradicción | requisitos.md:243 | RF-04 justifica la emisión de eventos con RN-30, que trata de los estados de la ejecución. | Citar RN-01 y RF-21. |
| 8 | contradicción | requisitos.md:606 | RH-07 dice «AR-01 a AR-18»; AR-19 queda fuera. | «Todas las reglas AR». |
| 9 | contradicción | historial.md:17 vs requisitos.md:15 | CA-06c, RH-01 y RH-02 se registran «sin cambio de significado», pero cambian. §0.1 dice que un ID nunca cambia de significado. | Registrarlos como cambios de significado; decidir la política de IDs. |
| 10 | contradicción | informe:326 vs requisitos.md:48 | El MVP-0 no incluye nada de la lista «No se recortan» de §2. | Separar en §2 alcance comprometido y objetivo. |
| 11 | contradicción | informe:239, :244, :269, :271 | Los criterios de muerte y H3 recortan lo que §2 prohíbe recortar («Citas», WhatsApp, Messenger, SMS, parte de RF-51). | Alinear con la escalera, o cambiar la escalera con aprobación antes de la crisis. |
| 12 | contradicción | informe:239 vs :270 | H2 «congela el alcance aquí»; C.4 «congela en H3». | Una sola regla. |
| 13 | contradicción | requisitos.md:464, :182, :210 | En CA-52d, Confirmada → Agendada es cambio de estado (cancelar), cambio de ancla (recalcular) y nueva entrada en Agendada: riesgo de duplicados. | Una regla que diga qué gana. |
| 14 | contradicción | arquitectura.md:129 | ADR-11 no dice dónde viven los efectos automáticos; un agente pondrá los estados como literales en `Cases` (AR-10). | Declararlos en el paquete. |
| 15 | contradicción | informe:94 vs :146 | El freeze dice «por hash, no por convención», pero la aprobación es una línea que un agente puede escribir. | CODEOWNERS y aprobador del PR. |
| 16 | contradicción | informe:169-172 vs :290; arquitectura.md:116 | Oráculo: «nunca ve las pruebas» frente a «después, mismo agente»; «primer corte» sin definir. | Definirlo como los CA comprometidos. |
| 17 | ambigüedad | requisitos.md:126-130, :157-161 | La cabecera de §6 promete declarar qué estados puede asignar una persona, pero ningún paquete lo hace; Recibido no está marcado como inicial. | Declararlo. |
| 18 | ambigüedad | requisitos.md:182 | «Incluidas las de la ejecución que originó» solo es observable con ramas paralelas, que no están definidas. | Definir el fork. |
| 19 | ambigüedad | requisitos.md:182 × :186, :205 | Un aviso en reintento o diferido que se cancela no tiene estado en RN-28. | Estado «cancelado». |
| 20 | ambigüedad | arquitectura.md:201, :271, :63 | ADR-20 no define el anidamiento; `SaveChanges` abre transacción implícita y AR-19 no es binaria; ADR-07 no lista `IUnitOfWork`. | Ámbito ambiental y lista de llamadas permitidas. |
| 21 | ambigüedad | AGENTS.md:15 | Faltan `Api → Infrastructure`, `Api → Cases` y `Sdk`. | Ampliar. |
| 22 | ambigüedad | informe:221 | El «botón Ejecutar» de H0 es un disparo manual que ningún RF define. | Evento sintético con paquete de prueba. |
| 23 | ambigüedad | requisitos.md:243 vs :44 | «caso creado» declarado EXT sin entrada en §2. | Añadirlo. |
| 24 | higiene | requisitos.md:251 | RF-06 usa la terminología anterior («permite al sistema»). | Terminología nueva. |
| 25 | higiene | informe:18 | Cuenta 18 AR; ya son 19. | Actualizar. |
| 26 | higiene | contexto.md:86-90 vs informe C.2 | Dos calendarios distintos. | Uno solo. |
| 27 | higiene | informe B.2 vs arquitectura.md:98 | `Fixtures` con dos significados; proteger la infraestructura de pruebas crea un cuello de botella. | `tests/Support/` sin proteger. |
| 28 | higiene | requisitos.md:48 | El recorte 1 incluye RF-82, que es EXT. | Quitarlo. |
| 29 | higiene | arquitectura.md:63 vs :44; decisiones.md:40 | `Core` con «puerto de canal» frente a `IChannel` en `Sdk`; «RF-11b» y «RNF-08b» no existen. | `Core` usa `Sdk`; CA-11b y CA-N08b. |

## 3. Respuestas

**CA afectados.** CA-08a/b, CA-53b: compatibles. CA-06c: coherente, pero su significado cambió. CA-25b: incompatible con ADR-06 (#2). CA-36c: compatible solo, pero junto con RN-05 rompe CA-53c (#1). CA-52d: choca con RN-05 y RN-33 (#13, #14).

**La creación no emite evento.** No rompe RF-25, RF-53, CA-04a/b, CA-11a, CA-25e ni CA-38b. Faltaba citar bien (#7), decir que un caso siempre nace en el estado inicial y que los efectos automáticos sí emiten evento (CA-52d lo necesita).

**Asignable y efecto automático.** No cubría todo: faltaban los estados asignables por una persona (por ejemplo, el dueño confirma por teléfono), el autor y el evento de los efectos automáticos y su sitio en el paquete.

**RN-05 binario.** Sí para flujos lineales, una vez corregido ADR-06. Prueba:

```csharp
[Ac("CA-25b")]  // RN-05: el paso que origina el cambio no se cancela
public async Task Cambio_propio_a_las_120h_continua_con_el_nodo_siguiente()
{
    Clock.SetBusinessTime(Lunes, 10, 0);
    var caso = await Api.CrearCaso(Pack.Reparaciones, canal: "correo", consentimiento: true);
    await Api.CambiarEstado(caso, "En diagnóstico");
    await Api.EnviarCotizacion(caso, monto: 1500);               // t0: entra a Cotización enviada
    var ej = await Api.EjecucionUnica(caso, "Seguimiento de reparación");

    await Engine.AdvanceAndDrain(Clock, TimeSpan.FromHours(120));

    var paso = (await Api.Historial(ej)).Single(p => p.Accion == "CambiarEstado");
    Ca.Expect("CA-25b", "correcto", paso.Resultado);              // no "cancelado"
    Ca.Expect("CA-25b", "Sin respuesta", (await Api.Caso(caso)).Estado);
    Ca.Expect("CA-25b", 1, (await Api.Bandeja(caso)).Count);      // el nodo siguiente sí corrió
    Ca.Expect("CA-25b", "Completada", (await Api.Ejecucion(ej)).Estado);

    await Engine.AdvanceAndDrain(Clock, TimeSpan.FromDays(30));
    Ca.Expect("CA-25b", 3, Simulator.Correo.Count(caso));         // t0, 24 h y 72 h; nada más
}

[Ac("CA-08b")]  // contraste: un cambio externo sí cancela
public async Task Cambio_externo_cancela_la_ejecucion()
{
    /* mismo arranque */ await Engine.AdvanceAndDrain(Clock, TimeSpan.FromHours(10));
    await Api.RegistrarRespuesta(caso, aprobada: true);
    Ca.Expect("CA-08b", ("Cancelada", "cambio de estado"), await Api.EstadoYMotivo(ej));
}
```

**MVP-0.** Cubría las cuatro tareas pieza a pieza, pero ningún CA comprometido unía editor, compilador y motor (activar, disparar, reanudar). Ampliado a 16 comprometidos, es alcanzable en unos 10 días hábiles con 2 personas y agentes. Los 153 CA M/MVP no caben en 19 días.

**Si el 21 de noviembre CA-10a/b están en rojo.** Recortes 1–5 de la escalera, las dos personas al motor y plan B técnico: reclamo por sondeo de la tabla de temporizadores, sin concesión, con una instancia (ADR-01). Si el 23 sigue en rojo, cambio aprobado de requisitos para los recortes 6 y 7. No se tocan ADR-03, RN-06, CA-10c, CA-11a/b, el validador y compilador, CA-25b ni el arnés.

**Contradicción no recogida en el informe.** RN-05 frente a RN-38, CA-53c y CA-53g (#1), agravada porque ADR-06 ya permitía no cancelar. También: el recorte 5 repetía A8 (#6), AR-08 frente a RH-01 (#3) y criterios de muerte contra §2 (#11).

## 4. Arnés e hitos

- **Trazabilidad:** bien planteada; sin manifiesto de alcance el CI quedaba en rojo desde el día 1.
- **Freeze:** la línea `Approved-By:` sola es falsificable.
- **Registro de tareas:** correcto.
- **CI:** CA-10 no puede ir solo de noche si es criterio de muerte; reinicio simulado en aceptación y `kill -9` real de noche.
- **Oráculo:** necesita el contrato OpenAPI antes de las pruebas; la revisión humana de cada prueba escala hasta unos 30 CA con 2 personas.
- **Hitos:** H1 no ejecutaba el flujo dibujado; H0 en 2 días no era realista; RH-09, RH-10 y RH-11 no caben al final; los criterios de muerte no tenían responsable y medían CA por día, que un agente puede inflar.

## 5. Riesgos y lo que no se cambia

**Riesgos para el 2 de diciembre**
1. Alcances que compiten (especificación, hitos, MVP-0) y una meta del experimento sin definir.
2. Semántica de cancelación y temporizadores contradictoria justo en la parte más difícil del motor y en «Citas».
3. El arnés se come la primera semana.

**No se cambia:** ADR-03 (fila por ejecución, transacción por paso, bandeja de salida con referencia única, `SKIP LOCKED` con concesión), ADR-04, ADR-16, ADR-15, paquetes como datos con AR-10, ADR-18, ADR-20 como idea, los CA como contrato con el formato de fallo de RH-06 y la escalera como mecanismo.

---

## 6. Registro de aplicación

Aplicado en requisitos v0.12, arquitectura v0.10, `harness/scope.json`, `AGENTS.md` y `humanos/*`.

| # | Problema | Corrección | Dónde |
|---|---|---|---|
| D1 | RN-05 cancelaba todas las esperas al cambiar de estado; RN-38, CA-53c y CA-53g necesitan que el flujo de «Citas» sobreviva a Agendada → Confirmada | RN-05 (b): un flujo puede declarar que sobrevive al cambio de estado; RN-38 lo declara | requisitos RN-05, RN-38 |
| D2 | ADR-06 y el ciclo de vida cancelaban la ejecución que origina el cambio (CA-25b fallaba por diseño) | Excepción explícita | arquitectura ADR-06, §2 |
| D3 | AR-08 exigía prueba para todo CA M y contradecía RH-01 | AR-08 y RH-01 contra `harness/scope.json` | arquitectura §3, requisitos RH-01 |
| D4 | RH-01 apuntaba a «la propuesta del equipo», fuera del repositorio | `harness/scope.json`, protegido | `harness/scope.json`, RH-02, AR-09 |
| D5 | Hitos con dependencias invertidas | Calendario rehecho (H0–H6) | [arnés e hitos](2026-11-13-arnes-e-hitos.md), Parte C |
| D6 | El recorte 5 citaba «RF-94c/d» (IDs inexistentes) y recortaba un RF M | Recorte 5 = RF-41; recortes 6 y 7 solo con cambio aprobado que rebaje la prioridad | requisitos §2 |
| D7 | RF-04 citaba RN-30 (estados de la ejecución) | RN-01 y RF-21; el caso nace en el estado inicial; los efectos automáticos emiten evento | requisitos RF-04 |
| D8 | RH-07 decía «AR-01 a AR-18» | «todas las reglas AR» | requisitos RH-07 |
| D9 | El historial clasificaba CA-06c, RH-01 y RH-02 como «sin cambio de significado» | Reclasificados | historial 0.11 |
| D10 | MVP-0 incompatible con la lista «No se recortan» de §2 | §2 separa alcance comprometido y escalera extendida | requisitos §2 |
| D11 | Criterios de muerte que incumplían §2 y dos reglas de congelación | Puntos de control alineados con la escalera, con responsable | arnés e hitos, C.3 y C.4 |
| D12 | CA-52d: ¿cancelar o recalcular?, con riesgo de recordatorios duplicados | Una ejecución en curso por flujo y caso; la anterior queda «reemplazada» | RN-05, ADR-06 |
| D13 | Los efectos automáticos no tenían sitio en el paquete (riesgo de literales, AR-10) | Declarados en el paquete | arquitectura ADR-11 |
| D14 | `Approved-By:` lo podía escribir un agente | CODEOWNERS; la línea nombra a quien aprobó el PR | ADR-09, AR-09, AGENTS.md |
| D15 | Oráculo: «primer corte» sin definir y sin contrato previo | CA comprometidos; contrato OpenAPI antes de las pruebas | ADR-09, arnés e hitos B.3 |
| D16 | Estados asignables por una persona sin declarar; Recibido sin «inicial» | Declarados | requisitos §6, §6.2 |
| D17 | Ramas paralelas sin definir | RN-10: un nodo que no es Condición tiene como máximo una salida; nuevo CA-02i | requisitos RN-10, CA-02i; ADR-06 |
| D18 | Avisos en reintento o diferidos sin estado al cancelarse | RN-28 añade «cancelado» | requisitos RN-28 |
| D19 | ADR-20 sin anidamiento; AR-19 no binaria | Ámbito anidado se une al exterior; llamadas permitidas solo en `IUnitOfWork` | ADR-20, AR-19 |
| D20 | AGENTS.md sin `Api → Infrastructure`, `Api → Cases` ni `Sdk` | Ampliado | AGENTS.md, ADR-07 |
| D21 | H0 dependía de un «botón Ejecutar» que ningún RF define | Disparo por evento con paquete sintético | arnés e hitos, C.3 |
| D22 | Evento «caso creado» declarado EXT sin entrada en §2 | Añadido | requisitos §2 |
| D23 | Higiene: RF-06, recuento de AR, dos calendarios, `Fixtures` con dos significados, RF-82 en la escalera, puertos de `Core` frente a `Sdk`, IDs inexistentes en `decisiones.md` | Corregidos | varios |
| D24 | MVP-0 sin CA que unan editor y motor | CA-03b, CA-40e, CA-04a y CA-10b pasan a comprometidos (16 en total) | `harness/scope.json` |

## 7. Decisiones abiertas

1. **Política de IDs.** §0.1 de requisitos dice que un ID «nunca cambia de significado», pero el historial registra cambios de significado desde la versión 0.6 (CA-06c tres veces). Decidir si §0.1 admite cambios registrados o si un cambio retira el ID y crea otro.
2. **Sin ramas paralelas** (RN-10, CA-02i): confirmar. Es coherente con «un nodo actual» de ADR-03 y con los flujos de RN-03, RN-04 y RN-38, que son secuenciales.
3. **CA-36c** sigue diciendo «se cancelan sus recordatorios de confirmación pendientes (RN-05)». Con RN-05 (b), el recordatorio de 24 h no se cancela: lo omite la condición de RN-38. El resultado observable (no se envía) es el mismo y el CA no se toca.
4. **Recortes 6 y 7**: «Citas», RF-17 y Messenger dejan la lista de no recortables, pero solo salen con un cambio aprobado de requisitos.
