# Task 4 Report: Shared Poise Payload And Golem Capability

## Implemented

- Added a non-negative poise payload to `DamageInstance` and `DamageContext`.
- Added `IPoiseDamageSource`; `DamageResolver` queries it per target and clamps the result before creating each context.
- Added reusable `EnemyPoise` storage with depletion, regeneration, restoration, and a one-shot depletion event.
- Routed accepted Golem health hits to poise, preserving card-enhanced melee and poise fields through the Golem damage policy.
- Wired Golem poise depletion to `Interrupted`, restores poise at `Recovery` and actor restoration, and added the poise debug bar.
- Updated the idempotent Golem prefab setup tool to add, configure, and reference `EnemyPoise` and `EnemyPoiseDebugPresentation` with maximum `10` and regeneration `0.33` per gameplay second.

## Verification

- `dotnet build Temp/CodexCompile/TicGame.Architecture.Task4.csproj --no-restore -v:minimal` succeeded with 0 warnings and 0 errors.
- `dotnet build Temp/CodexCompile/TicGame.Architecture.Editor.Task4.csproj --no-restore -v:minimal` succeeded with 0 warnings and 0 errors.
- `git diff --check` passed.
- Unit tests were not written or run, per the task instruction.

## Deferred Unity Editor Actions

1. Open Unity and allow the new scripts to import.
2. Run `TIC > Setup > Create Or Update Golem Charger`.
3. Save the Golem prefab and inspect that it contains `EnemyPoise` and `EnemyPoiseDebugPresentation`, with the brain and damage-policy poise references set to the root component.
4. In Play Mode, confirm the existing tag-based interruption still works. Card-poise behavior is deferred to Task 5.

## Commit

42ae9c2 (amended below to include this report).
