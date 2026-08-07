# Refactoring Guidelines

This document defines the architectural constraints for maintainability refactoring. It is intentionally separate from the LOC scanner: the scanner reports measurements, while an Agent evaluates responsibilities and chooses refactoring actions after reading the code.

## Goal

Reduce responsibility coupling and improve maintainability by keeping related behavior together. File size is only a supporting signal. A refactoring is not successful merely because it produces more files or lowers the line count of an individual file.

## Target Architecture

The project should remain a modular monolith with explicit layered boundaries:

```text
UI / Command Panel
        |
Application Workflows
        |
Core Domain Logic
        |
Runtime and Infrastructure Adapters
```

The practical ownership rules are:

- `EtgGameplayDashboard.Core/` contains pure models, parsing, selection, validation, and deterministic decisions.
- `EtgGameplayDashboard/Commands/` contains application-facing commands and focused runtime services. It may coordinate a workflow, but should not become a general-purpose container for unrelated features.
- `EtgGameplayDashboard/Runtime/` contains Unity, ETG, Harmony, scene, player, and lifecycle integration.
- `EtgGameplayDashboard/Configuration/` contains file formats, providers, persistence, and configuration translation.
- `EtgGameplayDashboard/Etg/` contains adapters for live ETG pickup and game-object access.
- `EtgGameplayDashboard/Ui/` and `InGameCommandController.*` contain presentation and interaction concerns. UI code should call application services instead of implementing persistence or domain decisions directly.
- `Plugin*.cs` files compose dependencies and own plugin lifecycle. They should not become the home for feature logic.

This is a boundary guide, not a requirement to create every listed folder or class. A new abstraction is justified only when it creates a cohesive responsibility or a useful dependency boundary.

## Refactoring Rules

- Inspect the code, callers, tests, and runtime constraints before choosing an action.
- Describe the observed problem in English before applying a structural change.
- Prefer high cohesion and explicit dependencies over smaller files.
- Keep ETG and Unity types at runtime boundaries whenever the decision logic can remain pure.
- Do not move code only to reduce a LOC threshold.
- Do not split a partial class into more files unless the class-level responsibility also becomes clearer.
- Prefer incremental vertical slices that preserve behavior and are easy to test.
- Update or add tests around extracted decisions and service boundaries.
- Preserve runtime lifecycle, hook, scene, and teardown behavior unless the refactoring explicitly changes it.

## Agent Evaluation Checklist

For each scanner candidate, the Agent should inspect:

1. What responsibilities does the file actually implement?
2. Which responsibilities change for different reasons?
3. Which dependencies are UI, application, core, runtime, or infrastructure concerns?
4. Is the coupling caused by the file, or by a wider class/module boundary?
5. Would moving or splitting the code improve cohesion and testability?
6. What behavior and tests must remain unchanged?

Only after answering these questions should the Agent choose to keep, move, split, merge, or leave the file unchanged.

## Verification

For each refactoring slice:

1. Run the relevant core tests.
2. Run the naming check when source files change.
3. Run a Debug build for runtime or UI code.
4. Re-run the LOC scanner only as a supporting measurement.
5. Compare responsibility boundaries and testability, not just line counts.
