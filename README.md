# Workflow Engine

An event-driven workflow platform with a visual node editor, DAG compilation, durable asynchronous execution, timers, retries and an HTTP-based third-party simulator. Editors can design workflows for services with an accessible interface through extensible operations (RF-101), and run offline tasks on the engine host that publish reusable artifacts (RF-102). Shopify, OpenAI and Google Docs in Drive form one reference flow (RF-100). Access uses fixed Editor and Reader roles.

The repository contains the specification, architecture, diagrams, committed scope and an executable delivery gate. Run `python3 harness/verify.py contract` for structural consistency or `python3 harness/verify.py release` for delivery status. The application, acceptance project, full experiment CLI and Docker Compose setup are pending.

## Documentation

- [English documentation](docs/en/README.md)
- [Documentación en español](docs/es/README.md)
- [Agent instructions](AGENTS.md)
- [Assignment](project-idea.txt)
- [Committed acceptance criteria](harness/scope.json)

English is the language for the product UI, code, identifiers, comments, API contracts, configuration and file names. Documentation is maintained in English and Spanish; code examples use English in both. English requirements are authoritative. Stable requirement and acceptance IDs remain unchanged across languages.

Target stack: .NET/ASP.NET Core, PostgreSQL, React/TypeScript and React Flow. Deadline: 2026-12-02.
