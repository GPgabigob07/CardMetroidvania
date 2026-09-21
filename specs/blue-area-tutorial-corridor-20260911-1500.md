# Blue Area Tutorial Corridor

## Contexto

Implements the user-approved corridor after the movement-room playtest.
Sources: blue-start-room-movement-revision-20260911-1300.md, the saved
Blockout_BlueStart_Movement scene, damage-system-sdd-20260526-0102.md,
composable-card-effects-and-gated-ability-bridge-sdd-20260614-1841.md,
unity-editor-collaboration-workflow-20260612-1609.md and this conversation.

## Design

Copy the saved movement scene to BlueArea_Tutorial.unity, preserving the source.
The corridor extends left from x=0 to x=-86, with floor y=13 and ceiling y=27.
Stage 1 contains one Charger at x=-9, outside its 4-unit detection radius even
at the nearest point of a 2-unit patrol. Stage 2 begins at x=-25, unlocks Card
Time, and contains one Charger on a broad platform near x=-36. Stage 3 follows
the gate at x=-49, contains two nearby Chargers and low optional platforms.
A second full-height gate at x=-79 leads into a capped next-area vestibule.
This pass does not build the next area or require killing enemies to advance.

Card Time is locked in the starting room and stage 1, unlocked for the rest of
the run on entering stage 2. The existing preset five-card inventory remains
editable for later deck design. The guide explains controls, windows, card
descriptions/costs and feedback without prescribing a card/attack solution.
Use the existing selection HUD. Place repeatable energy refills before both
doors so spending energy after killing enemies cannot block progression.

Gate damage qualification is captured on the primary melee DamageInstance and
propagated into DamageContext. A qualifying hit has an active knockback boost,
earned chain damage bonus, or an overcharge armed for that exact attack.
Energy-only effects, extra jump, ordinary high damage and standalone supplemental
damage do not qualify. Doors reject ordinary hits with feedback and open once
on qualifying positive damage. Their colliders disable permanently for this
scene run. Death resets player transient combat state but not the unlock/doors.
Scene reload starts a fresh tutorial; cross-session persistence is not provided.

## Implementation plan

1. Add focused EditMode tests for hit provenance, rejection, opening and repeated
   hits; demonstrate missing API before implementing it.
2. Extend DamageInstance/Context with optional IsCardEnhancedMelee metadata,
   propagate through DamageResolver, and stamp in PlayerCombatEffects.
3. Add a default-enabled PlayerController Card Time unlock flag, gate both
   published availability and activation, and expose a setter for scene triggers.
4. Add separate CardMeleeGate, CardTimeTutorialZone and TutorialEnergyRefill
   components. Keep concrete MonoBehaviours in matching files.
5. Expose the existing HUD builder for a supplied scene. Add a one-shot corridor
   setup command that copies the saved scene, authors grouped geometry and
   binds runtime references using Unity APIs. Existing output opens unchanged.
6. Compile runtime, editor and tests from current sources; run focused Unity
   tests where possible. Review player preservation, layer masks, full-height
   barriers, patrol spacing, deck wiring and resource recovery.

## Editor validation

Run TIC > Setup > Create Or Open Blue Area Tutorial. Walk to the first Charger:
Card Time must be unavailable. Enter stage 2: read the guide and check normal
Card Time windows and preset deck. Ordinary door hits must fail; qualifying
enhanced melee must open each door. Spend all energy and use each refill.
Die after unlock/opening and verify both persist. Test both keyboard and gamepad
HUD commands. Scene construction and spatial/game-feel checks require Unity.
