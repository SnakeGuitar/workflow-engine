# 4. Clases

Modelo de dominio y puertos, no clases de implementación. Los nombres van sin tildes porque serán identificadores de código. `Usuario` lo gestiona ASP.NET Core Identity en `Api` (ADR-08); `Core` no lo conoce.

## 4.1 Modelo

```mermaid
classDiagram
  direction LR
  class Flujo {
    +Guid Id
    +string Nombre
    +EstadoDeFlujo Estado
    +Diagrama Borrador
    +GuardarBorrador(Diagrama)
    +Activar() VersionDeFlujo
    +Desactivar()
  }
  class EstadoDeFlujo {
    <<enumeration>>
    Borrador
    Activo
    Inactivo
  }
  class Diagrama {
    +List~NodoDeDiagrama~ Nodos
    +List~Conexion~ Conexiones
  }
  class VersionDeFlujo {
    +int Numero
    +DocumentoCompilado Dag
    +DateTimeOffset ActivadaEn
  }
  class DocumentoCompilado {
    +string TipoDeEvento
    +string NodoInicio
    +List~NodoCompilado~ Nodos
    +List~Arista~ Aristas
    +List~string~ OrdenTopologico
  }
  class Evento {
    +string Id
    +string Tipo
    +JsonObject Datos
    +DateTimeOffset RecibidoEn
  }
  class Ejecucion {
    +Guid Id
    +EstadoDeEjecucion Estado
    +string NodoActual
    +int Intento
    +DateTimeOffset? VenceEn
    +DateTimeOffset? ConcesionHasta
    +JsonObject Contexto
  }
  class EstadoDeEjecucion {
    <<enumeration>>
    Pendiente
    EnCurso
    Esperando
    Completada
    Fallida
  }
  class Paso {
    +string Nodo
    +int Intento
    +DateTimeOffset Inicio
    +DateTimeOffset Fin
    +ResultadoDePaso Resultado
    +string EntradaResumida
    +string? Error
  }
  class CambioDeEstado {
    +EstadoDeEjecucion Origen
    +EstadoDeEjecucion Destino
    +DateTimeOffset Instante
  }
  class Envio {
    +string Referencia
    +string Nodo
    +DateTimeOffset GuardadoEn
    +int? CodigoDeRespuesta
  }
  class Usuario {
    +string Nombre
    +Rol Rol
    +int IntentosFallidos
    +DateTimeOffset? BloqueadoHasta
  }
  class Rol {
    <<enumeration>>
    Editor
    Lector
  }

  Usuario --> Rol

  Flujo "1" *-- "1" Diagrama : borrador
  Flujo "1" *-- "0..*" VersionDeFlujo
  Flujo --> EstadoDeFlujo
  VersionDeFlujo "1" *-- "1" DocumentoCompilado
  Ejecucion "*" --> "1" VersionDeFlujo : usa
  Ejecucion "*" --> "1" Evento : disparada por
  Ejecucion --> EstadoDeEjecucion
  Ejecucion "1" *-- "*" Paso : historial
  Ejecucion "1" *-- "*" CambioDeEstado
  Ejecucion "1" *-- "*" Envio
```

## 4.2 Motor y puertos

```mermaid
classDiagram
  direction TB
  namespace Core {
    class Validador {
      +Validar(Diagrama) List~ErrorDeValidacion~
    }
    class Compilador {
      +Compilar(Diagrama) ResultadoDeCompilacion
    }
    class Interprete {
      +EjecutarPasoAsync(Ejecucion, DocumentoCompilado) Avance
    }
    class MaquinaDeEstados {
      +Transicionar(Ejecucion, EstadoDeEjecucion, DateTimeOffset)
    }
    class RegistroDeAcciones {
      +Obtener(string nombre) IAccion
    }
    class IAccion {
      <<interface>>
      +string Nombre
      +EsquemaDeParametros Esquema
      +EjecutarAsync(parametros, referencia) ResultadoDeAccion
    }
    class IAlmacenDeEjecuciones {
      <<interface>>
      +RegistrarEventoAsync(Evento) bool
      +ReclamarAsync(int maximo, TimeSpan concesion) List~Ejecucion~
      +GuardarEnvioAsync(Envio)
      +GuardarAvanceAsync(Avance)
    }
    class IRepositorioDeFlujos {
      <<interface>>
      +GuardarAsync(Flujo)
      +ObtenerAsync(Guid) Flujo
      +ActivasPorTipoDeEvento(string) List~VersionDeFlujo~
    }
  }
  namespace Infrastructure {
    class Procesador {
      <<BackgroundService>>
    }
    class AlmacenPostgres
    class RepositorioDeFlujosPostgres
    class AccionEnviarCorreo
    class AccionEnviarSms
    class AccionLlamarWebhook
  }

  Compilador --> Validador : usa
  Validador --> RegistroDeAcciones : acciones válidas
  Interprete --> RegistroDeAcciones
  Interprete --> MaquinaDeEstados
  RegistroDeAcciones o-- IAccion
  IAccion <|.. AccionEnviarCorreo
  IAccion <|.. AccionEnviarSms
  IAccion <|.. AccionLlamarWebhook
  IAlmacenDeEjecuciones <|.. AlmacenPostgres
  IRepositorioDeFlujos <|.. RepositorioDeFlujosPostgres
  Procesador --> IAlmacenDeEjecuciones : reclama
  Procesador --> Interprete : ejecuta
```

`ResultadoDeAccion` es **Correcto** (con código y cuerpo), **Reintentable** o **Definitivo** (RN-29). `Avance` reúne lo que un paso confirma en una sola transacción: el paso registrado, el nuevo estado, el siguiente nodo o el instante de la espera, y la respuesta que se agrega al contexto (ADR-03).
