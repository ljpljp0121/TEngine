# Changelog

## [Unreleased]
### Changed
- **Breaking:** Refactored PFGAS runtime boundaries around narrower ability, effect, tag, cue, and attribute collaborators instead of preserving broad container-owned responsibilities.
- **Breaking:** Public runtime collection surfaces now return snapshots where they previously exposed mutable backing collections through read-only interfaces.
- Reworked ability execution around an activation-entry model with concrete runtime tasks.
- `GameplayAbility` now activates through `Activate(AbilityExecutionContext)`.
- `AbilityExecution` now owns active runtime `AbilityTask` instances, starts tasks through `StartTask(...)`, ticks only active ticking tasks, and cleans active tasks during end/cancel.
- `AbilityTask` now exposes completion, failure, cancellation, and finished callbacks for continuation-style ability flow.
- `WaitGameplayEventTask` can deliver received gameplay events directly through payload callbacks.
- `AbilityExecutionContext` now carries only PFGAS-owned runtime data.
- **Breaking:** PFGAS runtime now uses a fail-fast error policy for battle-domain programmer, configuration, and lifecycle errors.
- `ActionTask`, `ApplyGameplayEffectTask`, task completion callbacks, gameplay event handlers, and gameplay cue handlers now propagate unexpected exceptions instead of converting them into normal task failure or log-and-continue behavior.
- Ability activation, task ticking, ASC disable, and GAS ticking now use cleanup-only `finally` paths to keep runtime state coherent while preserving exception visibility.

### Added
- Added explicit `AbilityExecution.EndAbility(...)` and `CancelAbility()` lifecycle APIs.
- Added scene-attached tests for activation entry, concurrent active tasks, adversarial task lifecycle behavior, task callbacks, and end/cancel/ASC-disable cleanup.
- Added scene-attached fail-fast coverage for task callback exceptions, effect configuration exceptions, event/cue handler exceptions, activation rollback, tick cache cleanup, and ASC disable cleanup.

### Removed
- Removed obsolete prototype flow APIs and task wrapper compatibility paths.
- Removed the unused graph-authoring shell and the PFGAS runtime dependency on external graph runtime assemblies.

## [1.0.1] - 2025-03-29
### Fixed
- Resolved a bug causing crashes when initializing the package on Unity 2021.3.
- Fixed incorrect behavior in the custom editor window.

## [1.0.0] - 2025-03-15
### Added
- Initial release of the Unity package.
- Added core functionality for A, B, and C.
- Included documentation and example scenes.
