# Environmental Hazard And Checkpoint Recovery Timing — 20260928-1822

## Contexto

This note updates the recovery timing in
`specs/environmental-hazard-checkpoint-recovery-sdd-20260922-1616.md` after
Play Mode testing. The earlier spec set a two-second minimum movement lock;
the author found that too slow and chose 750 milliseconds on 2026-09-28.

## Current Decision

- `GameplayAreaCoordinator` exposes **Minimum Recovery Seconds** in the
  Inspector, defaulting to `0.75` seconds.
- The value is the minimum total time from recovery contact through cover
  fade-out and movement release. The existing fade duration remains separately
  configurable on `RespawnCoverUI`.
- Area loading or other readiness work can make recovery longer than the
  configured minimum; the cover and movement hold remain until it is safe to
  resume.
