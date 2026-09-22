# Five-card baseline verification — 2026-09-21 20:53

## Contexto

This note records the implementation check for [the five-card design](neutral-chain-card-baseline-sdd-20260921-1107.md) and [implementation plan](neutral-chain-card-baseline-implementation-plan-20260921-1119.md). The user asked to defer Unity unit tests. A focused Play Mode pass remains pending.

## Completed checks

- The runtime and Editor assemblies compiled with zero warnings and zero errors using temporary Unity project files that explicitly include every new script. Unity's own batch-mode script compilation also completed before executing setup.
- TIC > Setup > Create Or Update Five Card Prototype ran in Unity 6000.3.16f1 batch mode. It authored five card definitions and effects, curated TestCardInventory, updated TestCardCatalog, and serialized Player and Golem prefabs. A repeat run left all 31 card assets and two prefabs byte-identical.
- A static audit found unique GUIDs across 509 Assets metadata files, verified the five IDs, categories, costs, operation values, three Neutral/two Chain/two existing Finisher equipped cards, one copy of each new prefab component, and retained Player/Golem sprite references.
- Existing demo effect assets gained default serialized chargeCount: 0 fields when Unity saved the new operation structure; their behavior and authored values did not change.
- Git LFS images in the isolated worktree initially appeared as pointer text. Their source files were restored from the original checkout only after SHA-256 verification against every LFS object ID. The worktree's LFS paths compare to the same committed Git blobs and are not part of this feature diff.
- Git diff checks are required before the final commit.

## Focused Play Mode pass pending

Open the isolated worktree's Gameplay scene in Unity, then confirm:

1. All five cards appear in the expected Neutral and Chain categories and each spends exactly 5, 5, 15, 20, or 40 Energy on a valid activation. Reusing an active effect fails without spending.
2. Grounded Double Jump activates only on the ground and grants one midair jump. Jump Boost doubles one grounded takeoff and does not change a midair or coyote jump.
3. Dash is unavailable before its card, lasts five gameplay seconds, extends by 0.3 seconds per accepted enemy melee hit, and does not extend from a blue gate.
4. Ordinary melee deals zero poise. The poise card's next five accepted enemy melee hits deal 2.4 poise each and can interrupt the Charger; blocked armor hits, gates, and supplemental damage consume no charges.
5. Growing Reach rises from 0% to 25% in five 5% steps, does not grow on gate hits, stops at the cap, and clears on a missed attack. The selected attack Gizmo should match actual reach.
6. Death clears the new effects. A Blue/Pink area stream preserves active effects; the dash timer continues through the brief world hold.

Record the first Console error or any observed mismatch. No Play Mode outcome is claimed in this note.

## Deferred validation

Unity unit tests were deliberately skipped at the user's request. The remaining behavioral and visual claims depend on the focused Play Mode pass above.
