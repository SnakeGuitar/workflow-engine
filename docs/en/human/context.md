# Context

## What is being built

The scope in [project-idea.txt](../project-idea.txt), plus sign-in with two roles (D-23, D-25). [requirements.md](../requirements.md), section 1, maps assignment tasks to requirements. The recommended architecture is not adopted; see [ADR-18](../architecture.md#adr-18-the-assignments-recommended-architecture-is-not-adopted).

**Demo:** the assignment example as an installed workflow (RF-19). An Editor signs in; a `user.registered` event arrives; execution waits; the verification POST reaches the simulator; depending on its response, email and SMS are sent or omitted. Failure injection demonstrates retries, an abrupt restart demonstrates recovery, and a Reader account demonstrates read-only access.

**Experiment:** build with agents under a measured harness (requirements section 9). See [harness-and-milestones.md](harness-and-milestones.md) for the harness, calendar and committed scope.

## Open questions

| ID | Question |
|---|---|
| OQ-07 | What rubric does the instructor use, and what does it say about agents? |
| OQ-08 | How many tasks and models fit the usage budget (RH-09, RH-14)? |
| AD-04 | Where will the final demo run (local computer or server)? |
