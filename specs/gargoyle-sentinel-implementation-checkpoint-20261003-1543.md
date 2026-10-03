# Gargoyle Implementation Checkpoint - 20261003-1543

## Contexto

Supersedes the1534 implementation checkpoint. Approved1205 plans and1128 design/
SDDs remain sources. Native checkout execution and one independent final review
are authorized; no worktree/plan gate remains pending.

## Completed

Tasks1-4 remain complete (a9f1910,05d69f8,3f60b72,548c6fa). Task5 now verified:
exact authored1/3/5 spreads, one release per token, shared atomic canonical-target
hit reservations, procNone damage, launch motion/lifetime snapshots and live
profile damage. Added an optional authored EnemyProjectile2D launch path while
preserving existing Bat/deflection callers. Terrain collision and swept trigger
shape stop shots; default Rigidbody sweep ignored attached triggers, confirmed
by direct shape-query diagnostics before fixing.

Launcher owns only its registry; normal completion preserves shots. Stun/death/
target-loss/disable cancels owned shots; deflection transfers out of encounter
ownership and shared player-hit budget. Launch callbacks recheck cancellation
and actor/target validity before emitting the next shot. Actor unload destroys
remaining registry objects still owned by it. Pause/hold suppress queries.

Evidence: task5-volley-red.xml12 intended missing behavior failures;
task5-volley-green.xml29/29 including existing projectile and brain fixtures.
Swept-terrain regression initially failed; diagnostic confirmed simulated body,
one attached trigger and a direct circle sweep hittingWall. Final full EditMode
task5-regression-editmode2.xml529 total506 passed20 inherited failures3 skipped;
exact failed identities match baseline. All15 additional Task5 checks pass,
including actual runner/brain-to-volley once-only release and lifecycle cleanup.

## Resume

Next enemyTask6 depends on WardTasks1-2: start companion guard geometry/lifetime,
then atomic card transaction/live-cost tests, then beam integration. WardTask3
catalog/test-profile/feedback can accompany encounter setup. Enemy nova/feint and
presentation/arena are still unimplemented. No PlayMode, final independent review
or human visual/input acceptance has been performed; none is claimed.

CheckoutG:/UnityProjects/My project; branchcodex/playable-build-menu-20260929.
Quota last read35% used in300-minute window; reset1791064976 (2026-10-03 around
19:02:56 America/Sao_Paulo). Check between slices and before expensive review;
at95% used checkpoint/push and retime the same continuation heartbeat after the
latest reset. Never consume reset credits, merge master or publish a build.
