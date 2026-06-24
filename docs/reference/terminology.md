# Terminology And Naming

This project touches both ETG runtime internals and player-facing UI, so naming should stay focused on gameplay meaning.

## Preferred Terms

- `character select hub`
  The preferred gameplay term for the out-of-run player hub where character selection happens.
  Use this when describing behavior, transitions, and feature requirements.

- `Foyer`
  The in-game runtime type and API surface used by ETG for hub-specific behavior.
  Use this when referring to ETG classes such as `Foyer.Instance` or methods like `OnDepartedFoyer()`.

## Naming Rules

- Use gameplay semantics for methods and state names.
  Example: `ReturnToCharacterSelect()`

- When a name refers to ETG's runtime type, keep the ETG type name.
  Example: `Foyer.Instance.OnDepartedFoyer()`

## Boss Rush Guidance

- Start conditions should be described as `character select hub only` in code semantics.
- Returning from Boss Rush should prefer the ETG character-select flow, not a hard scene load.
- Boss-room logic should use gameplay terms like `boss room`, `boss staging room`, and `encounter`.
