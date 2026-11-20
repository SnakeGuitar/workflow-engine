# Diagramas

Vistas UML del sistema derivadas de [requirements.md](../../requirements.md) v1.3 y [architecture.md](../../architecture.md) v1.3. **Si un diagrama contradice esos documentos, mandan ellos** y el diagrama se corrige.

Notación: [Mermaid](https://mermaid.js.org/), que GitHub renderiza dentro del Markdown. Mermaid no tiene diagrams UML de casos de uso, componentes, comunicación ni despliegue: se dibujan con `flowchart` siguiendo las convenciones de cada archivo.

| # | Archivo | Contenido |
|---|---|---|
| 1 | [01-use-cases.md](01-use-cases.md) | Catálogo de CU, diagrama y fichas de los CU clave |
| 2 | [02-components.md](02-components.md) | Componentes e interfaces |
| 3 | [03-architecture.md](03-architecture.md) | Módulos, contenedores, procesamiento asíncrono y arquitectura recomendada frente a la adoptada |
| 4 | [04-classes.md](04-classes.md) | Modelo, motor y puertos |
| 5 | [05-states.md](05-states.md) | Ejecución y flujo |
| 6 | [06-sequence-and-communication.md](06-sequence-and-communication.md) | Secuencias del enunciado, comunicación del motor y conexiones personales |
| 7 | [07-deployment.md](07-deployment.md) | Instalación con Docker Compose |

## CU clave

| Clave | CU | Tarea del enunciado |
|---|---|---|
| A | CU-02 a CU-04 Diseñar, validar y activar un flujo | T1 editor, T2 analizador y DAG |
| B | CU-06 a CU-08 Ejecutar un flujo | T3 motor asíncrono, T4 webhooks y simulador |
| C | CU-09 Reanudar tras un reinicio | T3 (durabilidad); sostiene la decisión de no usar Redis (ADR-18) |

El acceso (CU-01, RF-30) aparece en los casos de uso, los componentes, los contenedores, el modelo de clases y la secuencia A.

## Imágenes

Generadas desde los bloques Mermaid con `@mermaid-js/mermaid-cli`, escala 2 y fondo blanco, y convertidas a JPG. Se regeneran cuando cambia un diagrama. Los identificadores técnicos usan inglés; las explicaciones están en español.

| Fuente | Imágenes |
|---|---|
| [01-use-cases.md](01-use-cases.md) | [1](img/01-use-cases-01.jpg) |
| [02-components.md](02-components.md) | [1](img/02-components-01.jpg) |
| [03-architecture.md](03-architecture.md) | [1](img/03-architecture-01.jpg) · [2](img/03-architecture-02.jpg) · [3](img/03-architecture-03.jpg) · [4](img/03-architecture-04.jpg) |
| [04-classes.md](04-classes.md) | [1](img/04-classes-01.jpg) · [2](img/04-classes-02.jpg) |
| [05-states.md](05-states.md) | [1](img/05-states-01.jpg) · [2](img/05-states-02.jpg) |
| [06-sequence-and-communication.md](06-sequence-and-communication.md) | [1](img/06-sequence-and-communication-01.jpg) · [2](img/06-sequence-and-communication-02.jpg) · [3](img/06-sequence-and-communication-03.jpg) · [4](img/06-sequence-and-communication-04.jpg) · [5](img/06-sequence-and-communication-05.jpg) |
| [07-deployment.md](07-deployment.md) | [1](img/07-deployment-01.jpg) |

La secuencia de proveedores ilustra RF-100. ADR-101 también cubre flujos que se ejecutan enteramente en el host del motor y flujos que intercambian artefactos con servicios registrados.
