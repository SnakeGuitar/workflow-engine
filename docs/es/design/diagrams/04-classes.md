# 4. Clases

Modelo de dominio y puertos, no clases de implementación. Los identificadores de código usan inglés. `User` lo gestiona ASP.NET Core Identity en `Api` (ADR-08); `Core` no lo conoce.

## 4.1 Modelo

```mermaid
classDiagram
  direction LR
  class Workflow {
    +Guid Id
    +string Name
    +WorkflowState State
    +Diagram Draft
    +SaveDraft(Diagram)
    +Activate() WorkflowVersion
    +Deactivate()
  }
  class WorkflowState {
    <<enumeration>>
    Draft
    Active
    Inactive
  }
  class Diagram {
    +List~DiagramNode~ Nodes
    +List~Connection~ Connections
  }
  class WorkflowVersion {
    +int Number
    +CompiledDocument Dag
    +DateTimeOffset ActivatedAt
  }
  class CompiledDocument {
    +string EventType
    +string StartNode
    +List~CompiledNode~ Nodes
    +List~Edge~ Edges
    +List~string~ TopologicalOrder
  }
  class Event {
    +string Id
    +string Type
    +JsonObject Data
    +DateTimeOffset ReceivedAt
  }
  class Execution {
    +Guid Id
    +ExecutionState State
    +string CurrentNode
    +int Attempt
    +DateTimeOffset? DueAt
    +DateTimeOffset? LeaseUntil
    +JsonObject Context
  }
  class ExecutionState {
    <<enumeration>>
    Pending
    Running
    Waiting
    Completed
    Failed
  }
  class Step {
    +string Node
    +int Attempt
    +DateTimeOffset StartedAt
    +DateTimeOffset FinishedAt
    +StepResult Result
    +string InputSummary
    +string? Error
  }
  class StateChange {
    +ExecutionState Source
    +ExecutionState Target
    +DateTimeOffset Timestamp
  }
  class Delivery {
    +string Reference
    +string Node
    +DateTimeOffset SavedAt
    +int? ResponseCode
  }
  class User {
    +string Name
    +Role Role
    +int FailedAttempts
    +DateTimeOffset? LockedUntil
  }
  class Role {
    <<enumeration>>
    Editor
    Reader
  }

  User --> Role

  Workflow "1" *-- "1" Diagram : borrador
  Workflow "1" *-- "0..*" WorkflowVersion
  Workflow --> WorkflowState
  WorkflowVersion "1" *-- "1" CompiledDocument
  Execution "*" --> "1" WorkflowVersion : usa
  Execution "*" --> "1" Event : disparada por
  Execution --> ExecutionState
  Execution "1" *-- "*" Step : historial
  Execution "1" *-- "*" StateChange
  Execution "1" *-- "*" Delivery
```

## 4.2 Motor y puertos

```mermaid
classDiagram
  direction TB
  namespace Core {
    class Validator {
      +Validate(Diagram) List~ValidationError~
    }
    class Compiler {
      +Compile(Diagram) CompilationResult
    }
    class Interpreter {
      +ExecuteStepAsync(Execution, CompiledDocument) Progress
    }
    class StateMachine {
      +Transition(Execution, ExecutionState, DateTimeOffset)
    }
    class ActionRegistry {
      +Get(string name) IAction
    }
    class IAction {
      <<interface>>
      +string Name
      +ParameterSchema Schema
      +ExecuteAsync(parameters, reference) ActionResult
    }
    class IExecutionStore {
      <<interface>>
      +RegisterEventAsync(Event) bool
      +ClaimAsync(int maximum, TimeSpan lease) List~Execution~
      +SaveDeliveryAsync(Delivery)
      +SaveProgressAsync(Progress)
    }
    class IWorkflowRepository {
      <<interface>>
      +SaveAsync(Workflow)
      +GetAsync(Guid) Workflow
      +ActiveByEventType(string) List~WorkflowVersion~
    }
  }
  namespace Infrastructure {
    class Worker {
      <<BackgroundService>>
    }
    class PostgresStore
    class PostgresWorkflowRepository
    class SendEmailAction
    class SendSmsAction
    class CallWebhookAction
  }

  Compiler --> Validator : usa
  Validator --> ActionRegistry : acciones válidas
  Interpreter --> ActionRegistry
  Interpreter --> StateMachine
  ActionRegistry o-- IAction
  IAction <|.. SendEmailAction
  IAction <|.. SendSmsAction
  IAction <|.. CallWebhookAction
  IExecutionStore <|.. PostgresStore
  IWorkflowRepository <|.. PostgresWorkflowRepository
  Worker --> IExecutionStore : reclama
  Worker --> Interpreter : ejecuta
```

`ActionResult` es **Success** (con código y cuerpo), **Retryable** o **PermanentFailure** (RN-29). `Progress` reúne lo que un paso confirma en una sola transacción: el paso registrado, el nuevo estado, el siguiente nodo o el instante de la espera, y la respuesta que se agrega al contexto (ADR-03).
