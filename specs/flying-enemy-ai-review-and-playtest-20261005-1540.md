# Flying enemy AI review and playtest — 20261005-1540

## Contexto

Requested after inconsistent flying enemy behavior was reported. This review
examines the authored Bat Machine directly and reuses its existing test scene
to collect observations before changing AI behavior. No runtime AI, prefab,
or scene settings were changed during this review.

Sources: `AGENTS.md`, `specs/bat-machine-enemy-sdd-20260901-1202.md`,
`specs/unity-editor-collaboration-workflow-20260612-1609.md`, current Bat Machine
brain, motor, threat monitor, tests, prefab, `Test_BatMachine.unity`, and
`PinkArea_Perimeters.unity`.

## Direct code findings

1. **Runtime landing notification is missing.** `ReportTerrainLanding` is called
   by EditMode tests only. No collision callback or other runtime caller connects
   terrain contact to this method. A stunned bat therefore has no authored runtime
   transition from `StunnedFall` to `GroundedRecovery` on landing.
2. **Fall speed is recorded at stun entry only.** `recordedDescendingSpeed` is set
   in `EnterStunnedFall` and never updated during descent. Even after wiring
   landing, damage would use the initial downward speed rather than impact speed.
3. **Level target binding differs from the test arena.** The prefab leaves `target`
   and `targetBody` null. Both bat instances in `PinkArea_Perimeters` have no
   overrides for those properties; the brain only resolves its local dependencies
   and does not discover a player. They therefore cannot engage through the current
   authored path. `Test_BatMachine` explicitly binds both player references.
4. **Overshoot is plausible, pending playtest.** `AerialSteeringMotor2D.MoveTowards`
   requests maximum speed for every nonzero offset, with acceleration-limited
   velocity changes and no stopping-distance braking. Engage has no arrival band.
   Test stationary-player hovering for oscillation and repeated crossings.
5. **Self-motion can create dodge eligibility.** Closing speed uses relative
   velocity, including the bat's velocity. The bat approaching a stationary player
   can satisfy the 0.1 speed threshold. Dodge is considered before fire, and uses
   unseeded Unity random rolls; only patrol waypoints are seeded. Test apparent
   spontaneous evades and differences between repeated runs.
6. **Obstacle handling only validates patrol endpoint centers.** There is no path
   clearance, body-size clearance, or obstacle avoidance for Engage/Evade. Engage
   always seeks player position plus world offset (-2, 1), independent of side.
   Test wall pressure and approaches from both sides.

## Existing scene handoff

Open `Assets/Scenes/Test_BatMachine.unity` directly and enter Play Mode.
The saved scene has the real Player prefab at (-2, 0), BatMachine Training at
(3, 2.5), a camera, floor and walls, and explicit target/target-body bindings.
It starts inside the 8-unit monitoring radius, so initial engagement is expected.
Reuse this scene; do not run the prefab setup command for this investigation,
because it rewrites authored prefab tuning. The scene setup command recreates
the scene and would discard its existing manual additions.

Suggested passes, restarting Play Mode between them:

- Stand still for 15 seconds. Observe hover overshoot, evades, firing intervals.
- Approach from left and right, then jump toward the bat. Note which action looks
  inconsistent, player motion, and whether the bat is near a wall.
- Press it against each wall. Watch for sustained movement into terrain.
- For patrol alone, clear the brain's Target field temporarily in Play Mode.
  Stop Play Mode to restore the saved reference afterward.
- For stun recovery, use available poise-damaging cards or deflected projectiles
  if the current player loadout supports them. Ordinary melee does no poise damage.
  Observe landing and whether it ever resumes flight. If health damage defeats
  it before poise is depleted, record this limitation; the existing arena has no
  debug stun button and poise is not an Inspector-editable runtime field.

Return expected versus observed behavior, approximate frequency, and any first
Console error. A short clip is useful for timing/oscillation observations.

## Verification

Unity 6000.3.16f1 focused EditMode run on 2026-10-05: filter
`TicGame.Architecture.Tests.Bat`; 46 total, 46 passed, zero failed/skipped.
Results: `Temp/FlyingEnemyReviewResults.xml`; log: `Temp/FlyingEnemyReview.log`.
The initial sandboxed Unity launch failed accessing the per-user cache; the
approved retry completed. Existing renderer-member hiding warnings remain.
These tests verify the current covered behavior, not the runtime issues above.
Play Mode feel and live scene behavior still require the user's playtest.
