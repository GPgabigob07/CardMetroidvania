# Bat local detection — 20261005-1630

## Contexto

Follow-up to `specs/flying-enemy-ai-review-and-playtest-20261005-1540.md`
and `specs/bat-machine-enemy-sdd-20260901-1202.md`. The user confirmed
attacks work in the test arena but not the game scene, rejected scene-load
binding, and approved local detection with escape and reacquisition.

## Implemented behavior

`BatThreatMonitor` acquires the nearest available canonical player through a
local Physics2D overlap query on the existing PlayerHitbox layer, using its
authored monitor radius (currently 8 units). The query includes triggers and
reuses a collider list. Candidate root positions must also be inside the radius;
large or offset hurtboxes cannot extend detection beyond the authored range.
Shared `EnemyPlayerTargeting` validates candidates, excluding dead enemies,
dead players, inactive objects, and players held by the world transition system.

`BatMachineBrain.EvaluateThreat` retains an assigned target while available and
in range. Otherwise it replaces that target with a locally detected player or
clears both target references. Existing Patrol/Engage transitions provide
engagement, disengagement and later reacquisition. The detected root Rigidbody2D
provides velocity for existing dodge evaluation. No scene-load search or binding
is needed. Existing explicit test-scene assignments remain supported.

An already committed firing windup or evade completes under the existing state
rules; the next engagement evaluation enforces the detection boundary. Steering,
dodge probability, cooldowns and the separately identified stun-landing gaps are
outside this change.

## Verification

Unity 6000.3.16f1 tests ran in `Temp/FlyingEnemyValidation`, containing copies of
the repository scripts, tests, packages and settings, to preserve the open Editor.
Regression tests failed against the original brain (2 failures, 16 passes), then
passed with the implementation. The final focused brain, threat-monitor and dodge
run passed all 26 tests. Results: `Temp/BatDetectionRed.xml` and
`Temp/BatDetectionGreen.xml`. `git diff --check` passed.

New coverage verifies entry from outside the radius without scene assignment,
target release on escape, reacquisition on return, and rejection of a non-player
collider on the detection layer. Live Play Mode behavior is pending user testing.

## Playtest

After Unity recompiles, open `Assets/Scenes/PinkArea_Perimeters.unity` through the
normal gameplay flow. Approach a bat within 8 units and observe engagement.
Move farther than 8 units from the bat's current position to escape, then return
to check reacquisition. Distance is relative to the moving bat, not its spawn.
A shot already being telegraphed may still finish. No Inspector assignment or
scene/prefab regeneration is required.
