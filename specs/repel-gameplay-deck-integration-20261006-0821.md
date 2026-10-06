# Repel Gameplay Deck Integration — 20261006-0821

## Contexto

Follow-up to `specs/repel-chain-card-verification-20261005-1904.md`.
The user confirmed Repel works in Play Mode, requested it in the gameplay deck,
then authorized commit, push, merge into master and a fresh branch from master.

## Gameplay deck

`TestCardInventory.asset`, referenced by the Player prefab, owns one Repel card
and equips it in Chain slot 3. Poise Damage and Growing Reach remain equipped;
Chain capacity remains 6. `CardInventoryProfileSetup` also equips `repel` so
rebuilding the prototype deck preserves this choice. The card remains reusable,
costs 15 Energy and enables melee reflection for 3 scaled gameplay seconds.

## Final verification

Fresh isolated Unity 6000.3.16f1 validation, including the gameplay-deck change:

- 96 focused bat/card/projectile EditMode tests passed.
- 7 real-contact projectile PlayMode tests passed.
- Broad EditMode: 638 total, 615 passed, 20 failed, 3 skipped.
- The 20 failed test identities match the previously verified committed baseline;
  no added failures were found. Integration is explicitly authorized with these
  previously reported baseline failures remaining outside this feature's scope.
- Staged whitespace checks pass after removing two test-file trailing blank lines.

Validation files now reside in the ignored `Builds/RepelValidation-20261006`
directory because Unity cleared the earlier `Temp` validation project.
