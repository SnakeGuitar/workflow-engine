# Diagramas

Vistas UML del sistema derivadas de [requisitos.md](../../requisitos.md) v1.0 y [arquitectura.md](../../arquitectura.md) v1.0. **Si un diagrama contradice esos documentos, mandan ellos** y el diagrama se corrige.

Notación: [Mermaid](https://mermaid.js.org/), que GitHub renderiza dentro del Markdown. Mermaid no tiene diagramas UML de casos de uso, componentes, comunicación ni despliegue: se dibujan con `flowchart` siguiendo las convenciones de cada archivo.

| # | Archivo | Contenido |
|---|---|---|
| 1 | [01-casos-de-uso.md](01-casos-de-uso.md) | Catálogo de CU, diagrama y fichas de los CU clave |
| 2 | [02-componentes.md](02-componentes.md) | Componentes e interfaces |
| 3 | [03-arquitectura.md](03-arquitectura.md) | Módulos, contenedores, procesamiento asíncrono y arquitectura recomendada frente a la adoptada |
| 4 | [04-clases.md](04-clases.md) | Modelo, motor y puertos |
| 5 | [05-estados.md](05-estados.md) | Ejecución y flujo |
| 6 | [06-secuencia-y-comunicacion.md](06-secuencia-y-comunicacion.md) | Secuencia de los tres CU clave y comunicación del motor |
| 7 | [07-despliegue.md](07-despliegue.md) | Instalación con Docker Compose |

## CU clave

| Clave | CU | Tarea del enunciado |
|---|---|---|
| A | CU-02 a CU-04 Diseñar, validar y activar un flujo | T1 editor, T2 analizador y DAG |
| B | CU-06 a CU-08 Ejecutar un flujo | T3 motor asíncrono, T4 webhooks y simulador |
| C | CU-09 Reanudar tras un reinicio | T3 (durabilidad); sostiene la decisión de no usar Redis (ADR-18) |

El acceso (CU-01, RF-30) aparece en los casos de uso, los componentes, los contenedores, el modelo de clases y la secuencia A.

## Imágenes

En [img/](img/), una por bloque Mermaid, con el nombre `<archivo>-<n>-<sección>.jpg`. Se generan con `@mermaid-js/mermaid-cli` (`mmdc -s 2 -b white`) y se convierten a JPG. Si se cambia un diagrama, hay que regenerar su imagen.

| Archivo | Imágenes |
|---|---|
| 1 | [diagrama](img/01-casos-de-uso-01-diagrama.jpg) |
| 2 | [componentes](img/02-componentes-01.jpg) |
| 3 | [módulos](img/03-arquitectura-01-modulos-y-dependencias-permitidas.jpg) · [contenedores](img/03-arquitectura-02-contenedores.jpg) · [procesamiento](img/03-arquitectura-03-procesamiento-sincrono-y-asincrono.jpg) · [recomendada frente a adoptada](img/03-arquitectura-04-arquitectura-recomendada-frente-a-la-adoptada.jpg) |
| 4 | [modelo](img/04-clases-01-modelo.jpg) · [motor y puertos](img/04-clases-02-motor-y-puertos.jpg) |
| 5 | [ejecución](img/05-estados-01-ejecucion.jpg) · [flujo](img/05-estados-02-flujo.jpg) |
| 6 | [A secuencia](img/06-secuencia-y-comunicacion-01-disenar-validar-y-activar-un-flujo.jpg) · [B secuencia](img/06-secuencia-y-comunicacion-02-ejecutar-un-flujo-secuencia.jpg) · [B comunicación](img/06-secuencia-y-comunicacion-03-ejecutar-un-flujo-comunicacion.jpg) · [C secuencia](img/06-secuencia-y-comunicacion-04-reanudar-tras-un-reinicio.jpg) |
| 7 | [instalación](img/07-despliegue-01.jpg) |
