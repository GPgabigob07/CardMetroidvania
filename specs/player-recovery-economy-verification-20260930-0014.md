# Player recovery economy and HUD verification

## Contexto

Implementation of the user-approved recovery design, plus the requested 2x player HUD.
Sources: `player-recovery-economy-design-20260929-1516.md`,
`player-recovery-economy-plan-20260929-1516.md`, and
`external-playtest-build-verification-20260929-1408.md`. Earlier drafts remain as history.
The user accepted the specs, including their proposed defaults, before implementation.

## Implemented behavior

- C is the highest summed fixed Energy cost among the run's equipped Neutral cards. Owned but unequipped cards and dynamic exchange costs do not contribute. Defensive fallback is 20.
- Guaranteed effective primary enemy-hit recovery: 3 Energy per attack request, 6 with the existing double-energy charge. Supplemental damage and generic damage targets do not grant it; defeat rewards remain independent.
- Passive recovery: 5 Energy per scaled gameplay second after 3 seconds without a successful Energy expenditure, capped at min(wallet capacity, 1.5C). Pause, world holds and death grant no catch-up. Energy above the reserve is retained.
- Blood Charge: spend 1 HP for C Energy, keeping at least 1 HP and requiring room for the full gain. This does not raise damage/death events.
- Reconstitute: spend whole 2C chunks for 1 HP each, only enough for missing health; retain the remainder. A final partial HP can use one chunk.
- Mend: restore up to 2 HP and consume one copy only on success. Start each run with 2 copies. Spent copies stay spent across area loading and respawn. A depleted card keeps its slot.
- Tuning lives in `Assets/Data/Cards/Recovery/PlayerRecoveryTuning.asset`; inventory quantities are copied from the authored profile and never written back at runtime. There is no runtime equipment editor or new disk-save support.
- Recovery cards use existing Neutral activation rules and prepared commit flow. Atomic writes precede notifications; stale amount/revision quotes, reentry, repeat application and cross-session submissions are rejected. Generic cost adjustments are unsupported for exchanges.
- Current authored C = 15: passive reserve 22.5, healing chunk 30, sacrifice gain 15.
- Selection text displays live costs/results, remaining consumable copies and reasons for unavailability. Top Left and Card Time corner HUD groups have scale 2; the full-screen card picker retains its layout.

## Verification

- 36 new EditMode recovery cases pass, covering arithmetic, safe health payment, independent runtime inventory, atomic state observation, prepared and legacy commits, depleted stock, stale results, passive delay/holds and presentation.
- Existing combat reward tests updated for the approved enemy-only 3/6 Energy semantics pass.
- Final complete EditMode run: 417 total, 394 passed, 20 failed, 3 skipped. The exact 20 failed test names match the previously recorded frontend baseline; no new failures. Evidence: `.utmp/recovery/editmode-final.xml` compared against `.utmp/playtest/editmode-full.xml`.
- Final complete PlayMode run: 3 passed, 0 failed. Recovery integration exercises actual passive Update behavior, pause/world holds, zero-energy Card Time activation with Mend, area loading, death/respawn, return to title and fresh-run stock. Existing frontend lifecycle tests also pass. Evidence: `.utmp/recovery/playmode-reviewed.xml`.
- Unity setup executed twice with identical hashes for the player prefab, Gameplay scene and inventory profile; no duplicate cards/components or GUID churn. Setup is available at `TIC/Setup/Create Or Update Player Recovery`.
- Rendered 1280×720 HUD and recovery selection captures inspected: `.utmp/recovery/hud-2x.png` and `recovery-cards.png`. No clipping observed in those captures.
- Independent review by a fresh gpt-6-astra reviewer found a cross-session prepared-commit gap. A regression test failed before the fix and passes afterward; the world-owned Card Time boundary now verifies session ID and category. Its minor authoring-diagnostic finding was also addressed with setup warnings for invalid reference costs and insufficient capacity. Final test runs include the fix.
- Unity serialization also materialized the existing 0.75-second recovery-duration default in Gameplay; its behavior is unchanged.

## Remaining acceptance

The full suite is not green because of the 20 inherited failures documented in the earlier verification record. Actual traversal-route escapability, controller feel, minimum-reserve usefulness and keyboard/gamepad readability still need human playtesting. Rendered screenshots and automated lifecycle tests do not establish those subjective/content outcomes.

This slice updates source and authored Unity assets. The previously packaged Windows test build has not been rebuilt with recovery. No merge, push or external distribution was performed.
