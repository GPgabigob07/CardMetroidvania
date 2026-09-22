# Five-card baseline verification after review — 2026-09-21 21:04

## Contexto

This note supersedes the implementation-state check in [the earlier verification note](neutral-chain-card-baseline-verification-20260921-2053.md) after whole-branch review found three runtime issues. The [design spec](neutral-chain-card-baseline-sdd-20260921-1107.md) remains the behavioral source.

## Review fixes

- A melee attack now selects one receiver per EnemyActor, preferring an overlapping head hurtbox over body and root. Root-only Golem hits route through GolemChargerDamagePolicy, preserving armor and poise handling. This prevents one Golem from spending several poise charges or adding several dash/reach increments in one swing.
- Transient reset cancels an unfinished attack without emitting a completed-miss event, preserving Growing Reach across a world hold or area stream.
- The dash HUD refreshes when the displayed tenth of a second changes.

## Verification

The complete runtime and Editor assemblies built with zero warnings/errors using temporary Unity project files that include every new source file. Unity 6000.3.16f1 also imported and compiled the reviewed runtime fixes in batch mode and exited successfully. The full branch diff passes git diff --check; the worktree is clean. The earlier note records the authored-asset audit and byte-identical Unity setup rerun.

Unity unit tests remain skipped at the user's request. Play Mode behavior has not been claimed as verified. In addition to the earlier matrix, the focused Play Mode pass should strike the Golem where root/body/head colliders overlap and confirm one health hit and one card charge per swing; interrupt an attack during windup with a world hold or transient reset and confirm Growing Reach remains; and watch the dash HUD count down.
