<!--
Sync Impact Report
- Version change: none (no prior) → 1.0.0
- Modified principles: N/A (template placeholders replaced)
- Added sections: "Additional Constraints", "Development Workflow"
- Removed sections: None
- Templates requiring updates:
	✅ .specify/templates/plan-template.md (Constitution Check aligns generically)
	✅ .specify/templates/spec-template.md (No conflicting mandates)
	✅ .specify/templates/tasks-template.md (Task grouping aligns with independent stories)
	⚠ README.md (optional reference to constitution and principles)
- Deferred items:
	TODO(RATIFICATION_DATE): Original adoption date unknown; needs confirmation
-->

# Uno.Extensions Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

### Library-First, Cross-Platform
<!-- Example: I. Library-First -->
MUST design features as reusable, standalone libraries that compile and run
across supported Uno target platforms (WinUI, MAUI, WebAssembly, Skia, iOS,
Android). Libraries require a clear, documented purpose, stable contracts, and
independent test coverage. Platform-specific shims are allowed only behind
well-defined interfaces.
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### Contracts Over Implementation
<!-- Example: II. CLI Interface -->
MUST define public-facing contracts first (APIs, DI registrations, navigation
routes, configuration keys). Breaking changes require deprecation notices,
changelogs, and migration notes. Prefer protocol stability over internal
refactors.
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### Test-First Discipline (Non-Negotiable)
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
MUST write tests before or alongside implementation. Red-Green-Refactor cycle is
expected. Unit and integration tests cover primary contracts and supported
platforms. New features are blocked until tests exist and fail prior to
implementation.
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### Integration and Runtime Validation
<!-- Example: IV. Integration Testing -->
MUST include integration tests for navigation flows, configuration binding,
authentication, and cross-library interactions. Runtime samples or playgrounds
are maintained to validate real app flows across platforms.
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### Observability, Versioning, Simplicity
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
MUST provide structured logging hooks and diagnostics where appropriate.
Semantic versioning is enforced: MAJOR for breaking changes, MINOR for new
capabilities, PATCH for fixes. Favor simple designs, minimal abstractions, and
avoid speculative complexity.
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## Additional Constraints
<!-- Example: Additional Constraints, Security Requirements, Performance Standards, etc. -->

MUST adhere to .NET coding guidelines in the repository (`stylecop.json`) and
respect banned symbols lists. Security practices in `SECURITY.md` are required
for reporting and mitigation. Public APIs MUST be documented; samples in
`samples/` SHOULD demonstrate typical usage. Performance-sensitive paths SHOULD
be measured with realistic workloads; regressions require justification.
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## Development Workflow
<!-- Example: Development Workflow, Review Process, Quality Gates, etc. -->

MUST link features to specifications under `/doc` or project-level specs.
Pull requests MUST include:
- Tests for new or changed contracts
- Changelog entries for user-facing changes
- Migration notes when behavior changes materially

Reviews MUST verify constitution compliance and flag unjustified complexity.
CI MUST run tests across applicable target frameworks; failures block merges.
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

This constitution supersedes informal practices for Uno.Extensions. Amendments
require: documented proposal, version bump per policy, and a migration plan for
affected libraries or apps. Compliance is reviewed during PRs and periodically
each quarter. Runtime guidance files (e.g., `doc/GettingStarted.md`,
`doc/ExtensionsOverview.md`) SHOULD reflect principle updates.

**Version**: 1.0.0 | **Ratified**: [TODO(RATIFICATION_DATE)] | **Last Amended**: 2025-12-03
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->

# [PROJECT_NAME] Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

### [PRINCIPLE_1_NAME]
<!-- Example: I. Library-First -->
[PRINCIPLE_1_DESCRIPTION]
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### [PRINCIPLE_2_NAME]
<!-- Example: II. CLI Interface -->
[PRINCIPLE_2_DESCRIPTION]
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### [PRINCIPLE_3_NAME]
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
[PRINCIPLE_3_DESCRIPTION]
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### [PRINCIPLE_4_NAME]
<!-- Example: IV. Integration Testing -->
[PRINCIPLE_4_DESCRIPTION]
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### [PRINCIPLE_5_NAME]
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
[PRINCIPLE_5_DESCRIPTION]
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## [SECTION_2_NAME]
<!-- Example: Additional Constraints, Security Requirements, Performance Standards, etc. -->

[SECTION_2_CONTENT]
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## [SECTION_3_NAME]
<!-- Example: Development Workflow, Review Process, Quality Gates, etc. -->

[SECTION_3_CONTENT]
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

[GOVERNANCE_RULES]
<!-- Example: All PRs/reviews must verify compliance; Complexity must be justified; Use [GUIDANCE_FILE] for runtime development guidance -->

**Version**: [CONSTITUTION_VERSION] | **Ratified**: [RATIFICATION_DATE] | **Last Amended**: [LAST_AMENDED_DATE]
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->
