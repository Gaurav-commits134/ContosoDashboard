<!--
Sync Impact Report
Version change: unversioned scaffold → 1.0.0 (initial adoption)
Modified principles: PRINCIPLE_1_NAME → I. Training-First Purpose;
PRINCIPLE_2_NAME → II. Security and Authorization by Design;
PRINCIPLE_3_NAME → III. Offline-First and Replaceable Infrastructure;
PRINCIPLE_4_NAME → IV. Simple, Layered Architecture;
PRINCIPLE_5_NAME → V. Verifiable Quality
Added sections: Project Constraints; Development Workflow and Quality Gates
Removed sections: none
Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Purpose
ContosoDashboard MUST remain clearly identified as a training application. Documentation,
interfaces, and examples MUST distinguish educational demonstrations from production-ready
controls. Changes MUST preserve the offline training experience unless an approved feature
specification explicitly expands that scope. Rationale: learners need an accurate picture
of what the application demonstrates and what it does not provide.

### II. Security and Authorization by Design
Every feature handling identity, data, or files MUST enforce authentication and authorization
at trusted server-side boundaries; UI visibility alone is not an access control. Access to
user- and project-scoped data MUST be checked for the requested resource, including direct
identifier access. Implementations MUST validate untrusted input and avoid exposing sensitive
data or files through public paths. Training-only mock authentication MUST NOT be described
as suitable for production. Security-sensitive behavior MUST have focused verification.
Rationale: the project teaches security concepts while retaining explicit limits on its mock
security model.

### III. Offline-First and Replaceable Infrastructure
Core application and training workflows MUST run without cloud services or internet access.
Infrastructure that may later be replaced, such as storage or identity, MUST be isolated
behind an interface when the abstraction has a clear use in this repository. Implementations
MUST keep environment-specific details out of business logic and MUST NOT add mandatory
external-service dependencies without an approved specification. Rationale: offline use is
central to training, while narrow abstractions demonstrate practical migration seams.

### IV. Simple, Layered Architecture
Features MUST fit the existing ASP.NET Core, Blazor Server, and Entity Framework Core
architecture and keep presentation, business behavior, and persistence responsibilities
separate. Changes MUST prefer the smallest design that satisfies approved requirements;
new layers, dependencies, and broad rewrites require a documented need. Existing conventions
and data contracts MUST be preserved unless a specification explicitly authorizes a change.
Rationale: a familiar, bounded structure keeps the training code approachable and maintainable.

### V. Verifiable Quality
Each feature MUST define observable acceptance criteria and add or update automated tests for
its important behavior. Tests MUST cover relevant authorization and data-isolation rules,
validation failures, and integration boundaries where those behaviors change. Before merge,
the applicable test suite and build MUST pass, or any unavailable or failing check MUST be
reported with its impact. Documentation and specifications MUST be updated when behavior,
constraints, or setup instructions change. Rationale: reproducible verification helps learners
distinguish intended behavior from accidental behavior.

## Project Constraints

- The application MUST remain an educational, non-production project; repository guidance
  MUST disclose known limitations, especially those involving mock authentication.
- The default development path MUST remain local and offline-capable. A new cloud or network
  dependency requires explicit approval in the feature specification and a usable local path
  when it affects core training workflows.
- Changes MUST use the repository's existing technology stack and architectural boundaries
  unless an approved plan records why a deviation is necessary.
- Requirements for production deployments, regulatory compliance, or production-grade
  security MUST NOT be implied by training implementations or examples.

## Development Workflow and Quality Gates

- Work MUST be described by a specification before implementation when it changes user-visible
  behavior or introduces a substantial capability. Plans and task lists MUST remain consistent
  with that specification.
- Implementation MUST satisfy the acceptance criteria and MUST include regression coverage
  for affected behavior. Security-sensitive changes MUST include checks for unauthorized access
  and resource isolation.
- Reviews MUST check this constitution, the feature specification, relevant tests, and updated
  documentation. Deviations MUST be recorded with their rationale and affected scope.
- Builds and relevant tests MUST be run before merge. If the environment prevents a check, the
  review record MUST identify the skipped check and the reason.

## Governance

This constitution governs repository development practices and MUST be reflected in feature
specifications, plans, implementation, and reviews. Amendments MUST be made in a reviewed
change that explains the motivation and impact. Amendments that affect existing work MUST
identify necessary follow-up or migration steps.

Constitution versions use semantic versioning. A MAJOR increment marks backward-incompatible
governance changes, including removal or redefinition of a principle. A MINOR increment marks
a new principle or section, or materially expanded requirements. A PATCH increment marks
clarifications, wording corrections, or other non-semantic changes.

Contributors and reviewers MUST assess compliance during planning and review. A deviation is
permitted only when the relevant specification or review documents its reason, scope, and
mitigation; it does not silently amend this constitution. The constitution MUST be reviewed
when a project-wide architecture, training purpose, or security boundary changes.

**Version**: 1.0.0 | **Ratified**: 2026-11-23 | **Last Amended**: 2026-11-23
