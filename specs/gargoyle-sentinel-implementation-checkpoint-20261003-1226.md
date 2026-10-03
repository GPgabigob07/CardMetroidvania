# Gargoyle Sentinel Implementation Checkpoint

## Contexto

Continuation checkpoint for the user's quota-limited implementation request.
Sources are the approved `gargoyle-sentinel-implementation-plan-20261003-1205.md`,
`ward-implementation-plan-20261003-1205.md`, their 1128 SDDs and the executing-plans
ledger under `.superpowers/sdd/gargoyle-sentinel-implementation-plan-20261003-1205/`.
The author explicitly approved implementation in this checkout with one final
independent review. No further plan/worktree approval is pending.

## Implemented and verified

- Asset-backed GargoyleAttackSelector: deterministic Fisher-Yates bags, all six
  orders, no repeats across bag boundaries, consumption on commit, stable Peek,
  independent volley-count memory, explicit reset, refill-time asset edits and
  last-valid coherent selector configuration after invalid edits.
- EnemyAttackStep and EnemyAttackDefinitionSO: ordered stable identities,
  asset-owned phase durations and DamageProfileSO references, validation of
  empty/duplicate/missing steps, invalid durations and missing payload profiles.
- GargoylePresentationSO: approved 200x200 frame, 128x128 ordinary envelope,
  pixel foot pivot(100,36), provisional48PPU, body1.8x2.1 and minimum body-area
  ratio1.5. These are authored settings; actual art import/body-mask acceptance
  remains pending.

Evidence under `.utmp/gargoyle/` (ignored local test artifacts):

- `baseline.xml`:417 total,394 passed,20 failed,3 skipped; exact failure identities
  match the prior recovery baseline.
- `selector-red.xml`:7 intended NotImplementedException failures; then
  `selector-green.xml`:7 passed and `selector-live.xml`:12 passed.
- `configuration-red.xml`:9 intended stub failures; corresponding configuration
  fixture has9 passes in the full checkpoint.
- `presentation-red.xml`:8 intended stub failures; corresponding presentation
  fixture has8 passes in the full checkpoint.
- `checkpoint-final.xml`:446 total,423 passed,20 failed,3 skipped. All29 new
  Gargoyle tests pass. Failed test identities exactly match baseline; no new
  failures or compiler errors. `git diff --check` is clean.

No PlayMode or human encounter acceptance has been performed for this slice.
The full suite still has the20 inherited failures; do not describe it as green.

## Next work

Task1 is partially implemented, **not complete**. Resume with its task brief and
existing tests, not by reimplementing the selector. Finish tuning/repertoire/seed
references, payload kind/geometry/aim/motion/cast-budget/variant fields, presentation
regions and coherent validation/live-edit tests. The current tuning asset only
owns selection lists; it is not a finished encounter configuration. Then proceed
through Tasks2-8 and the Ward dependency in their planned order. No brain, attack
runner, damaging payloads, Ward card, prefab or arena exists yet.

## Execution rulings

The bundled Bash task-start helper cannot locate dirname on this Windows Git
installation. Equivalent PowerShell task extraction preserves the prescribed
workspace, task identity, base and ledger; reconcile artifact formatting if the
final review helper needs it. Cost if wrong: review helper adaptation, no gameplay
change. Presentation tests are in a focused additional fixture, alongside the
planned configuration fixture; include both in all Task1 completion checks.

## Quota and continuation

Heartbeat `continue-gargoyle-sentinel-after-quota-reset` is active in this chat,
first continuation scheduled14:02 SãoPaulo on2026-10-03, after the reported
13:59:47 reset. It must check the300-minute usage window before work and between
meaningful slices. Stop ordinary work at usedPercent>=95 (remaining<=5%), preserve
this ledger/checkpoint, and retime the same heartbeat shortly after the latest
reported resetsAt. Never consume the account's reset credits.

Preserve the user's approved current-checkout/native execution preference.
Continue autonomously through implementation and verification; request only the
short human Editor acceptance when the actual encounter is ready. Final review,
artist animation/body-mask judgment and actual PlayMode observations cannot be
invented. Disable the heartbeat once implementation, required verification and
human acceptance are finished; never automatically merge master or publish.
