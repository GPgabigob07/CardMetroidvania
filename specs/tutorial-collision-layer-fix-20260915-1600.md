# Tutorial Collision Layer Fix

## Contexto

Follow-up to blue-area-tutorial-corridor-20260911-1500.md after the user
reported walking through card seals and failing to trigger the Card Time unlock.
Inspection of BlueArea_Tutorial.unity and Physics2DSettings.asset established
that the player's layer 6 interacts only with Environment (9). Gates were on
Enemy (8); tutorial and energy triggers were on Default (0). Neither pair
generated the required contacts with the player.

## Correction

Generate gates and tutorial/refill triggers on Environment. Include Environment
in the player melee overlap mask so damageable doors remain valid targets.
Other environment geometry has no IDamageable and is ignored by the hit detector.
Do not expand the global collision matrix: that would change enemy/player body
interactions beyond this fix.

Running TIC > Setup > Create Or Open Blue Area Tutorial on an existing scene
now repairs these component layers and detector masks, records prefab overrides,
and saves the scene without reconstructing the layout. The same setup choices
apply to newly generated scenes.

## Validation

Editor assembly compiled with zero warnings/errors. Unity Play Mode follow-up:
the cyan doors must stop movement, crossing the discovery trigger must check
Player Controller > Progression > Card Time Unlocked, standing in a well must
restore Energy, and a qualifying enhanced melee hit must still open a door.
