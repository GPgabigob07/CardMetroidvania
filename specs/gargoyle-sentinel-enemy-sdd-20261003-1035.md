# Gargoyle Sentinel Enemy SDD - Review Draft

## Contexto and status

Created 2026-10-03 from the author's request to specify a solo gargoyle elite
after accepting its compact visual draft. No gameplay code is changed by this
document. Review this spec together with the encounter design and Ward dependency
before preparing the implementation plan.

Sources:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `gdd/gargoyle-sentinel-design-20261003-1035.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/state-machine-owned-state-evolution-sdd-20260806-2146.md`
- `specs/golem-charger-enemy-sdd-20260806-2146.md`
- `specs/bat-machine-enemy-sdd-20260901-1202.md`
- `specs/neutral-chain-card-baseline-sdd-20260921-1107.md`
- `specs/card-time-opportunity-identity-and-neutral-rearm-sdd-20260922-1010.md`
- `specs/player-recovery-economy-verification-20260930-0014.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/testing-conventions-20260526-0122.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- Current runtime source at `13e0138`, especially Enemy, Damage, StateMachines,
  PlayerCombatEffects, PlayerAttackHitDetector2D and PlayerCardRuntime.

## Scope and success

Implement one grounded solo elite with a basic melee combo, three shuffled
attack families, 1/3/5-projectile patterns, beam, interruptible nova, stun and
one bounded feint. Its difficulty comes from reading attacks and using cards,
not adds, inflated contact damage or perfect reactive tracking.

Use the existing typed owned-state FSM. Keep selection and attack execution in
small collaborators. No BT, utility-AI package, flight system, global boss
framework, new player attack combo, deck editor or progression gate is required.

## Architecture choice

| Approach | Assessment |
| --- | --- |
| Existing FSM plus selector and attack phases | Recommended. Explicit commitments and interruptions; data varies attacks without permutation states |
| One flat state per attack permutation | Rejected. Duplicates transitions and timing as combinations grow |
| Behavior tree plus execution layer | Deferred. Can help shared tactical behaviors later, but still needs the attack runner, shuffle memory and interrupt rules |

The existing FSM has owner-aware states and animation callbacks; it has no
automatic hierarchy, transition priority or queued transition mechanism. A
single GargoyleBrain coordinates priority explicitly; a new general HFSM is not
needed. Animation renders authoritative gameplay phases rather than deciding
damage timing.

## Reuse versus construction

| Existing capability | Reuse boundary |
| --- | --- |
| EnemyActor, EnemyDefinitionSO, EnemyHealth | Identity, health, lifecycle, defeat rewards |
| EnemyPoise | Meter, depletion event and explicit restoration; brain owns its tick and reaction |
| StateMachine<TStateId>, OwnedState | State lifecycle; new GargoyleState and owned states |
| DamageResolver, DamageInstance, profiles and proc policy | All outgoing/incoming accepted damage transactions |
| Golem/Bat damage policies | Reference pattern only; their armor/fall rules remain enemy-specific |
| GroundedEnemyPatrolMotor2D | Safe MoveTowards/Stop and wall/ledge probes, configured for this body's footprint |
| EnemyProjectile2D | Single projectile motion/damage. Add only cast-budget/lifetime/terrain hooks needed for this encounter |
| BatProjectileLauncher and BatShotPredictor | Timing/aim examples; Bat launcher fires in one direction, not a spread |
| Card Time, inventory, prepared commits, feedback | Existing selection and effect lifecycle; Ward needs an explicit new operation |

New components and plain types, all under `TicGame.Architecture`:

| Unit | Responsibility and dependencies |
| --- | --- |
| GargoyleBrain | Owned FSM, target lifecycle, pass progression, nova scheduling and interrupt priority |
| GargoyleTuningSO | Poise, movement, timing, range and presentation scale defaults |
| EnemyAttackDefinitionSO | Immutable ordered steps; phase durations, payload shape, aim/advance rules and damage profile |
| EnemyAttackRunner | Plain runtime phase/execution state; unique execution token, step advancement and cancellation |
| GargoyleAttackSelector | Plain seeded shuffle bags for A/B/C and volley counts; no Unity/physics dependency |
| EnemyMeleeAttack2D | Active overlap shapes and per-strike canonical target budget |
| EnemyProjectilePatternLauncher | Explicit angular shot directions, shared cast hit budget and owned-projectile tracking |
| EnemyBeamAttack2D | Thick line query, terrain clipping, Ward interception and one accepted hit per cast |
| EnemyNovaAttack2D | Separate reactor stability, finite radial overlap and cancellation |
| GargoyleDamagePolicy / GargoyleHurtbox | Health/poise routing and body/core/head semantics |
| GargoyleAnimationPresenter | Frame/phase/facing presentation; no gameplay decision ownership |
| GargoylePrefabSetup | Idempotent asset/prefab/test-scene wiring and pixel/import validation |

Concrete MonoBehaviours and ScriptableObjects each use a same-named `.cs` file.
Keep runtime state out of shared assets. XML-document new interface/virtual
contracts and annotate Inspector fields per project conventions.

## State and execution contracts

```text
Dormant -> player valid and in encounter range -> Engage
Engage -> spacing ready -> Windup
Windup -> phase completes -> Active
Active -> step completes -> Recovery
Recovery -> another step/family -> Windup
Recovery -> pass ends -> Engage (or scheduled nova Windup)
Any living state -> global poise depleted / nova core broken -> Stunned
Beam opening successfully intercepted -> Staggered
Stunned / Staggered -> timed recovery -> Engage
Any state -> health depleted -> Dead
```

Nova is an attack kind on the same Windup/Active/Recovery pipeline, not a second
brain. States expose current attack ID, step ID, phase, remaining duration,
aim lock, feint branch and interrupt cause for debug and presentation.

Priority: death > core-break/global-poise stun > beam stagger > normal completion.
Damage resolution can raise defeat/poise events synchronously. Record pending
interrupts and reconcile once before normal advancement, immediately suppressing
damage/emission while pending. Recheck operational state after every damage
callback. Duplicate causes cannot create repeated stuns or restore poise twice.

Runner contract: Begin(definition, executionToken), Tick(scaledDelta), Cancel.
Every damaging step has a distinct step token. Begin during an active execution
rejects. Cancel is idempotent and invalidates all old callbacks/emission commands.
Carry surplus delta across non-damaging phase boundaries without dropping time;
do not consume an entire active interval before physics can sample its hitbox.
Due emissions are processed in order once, after interrupt checks. Zero-duration
loops, negative/non-finite durations and invalid step graphs are authoring errors.

Physics movement/overlaps occur in FixedUpdate using the current authoritative
phase. Queries cannot damage outside Active. Aim/facing locks at the end of
windup; no tracking, direction reversal or feint after that commitment. Basic
combo steps have separate windups, so later strikes may choose a new facing.

## Selection and positioning

- Fisher-Yates shuffle A/B/C with an injectable seeded random source. Each bag
  contains each family exactly once; new bags cannot start with the last
  committed family. All six permutations remain reachable over seeds/passes.
- Start a fresh pass with Basic. Draw/consume a family only on entering its
  Windup. A feint counts as the chosen attack, not a replacement family draw.
- Cancelled attacks stay consumed. Preserve unconsumed entries through stun;
  restart with Basic, finish the remaining bag, recover, then refill. Never
  reset the bag to obtain a more convenient move.
- Crystal Volley has a separate shuffled 1/3/5 count bag, one count per volley.
- Reposition for the queued family through safe grounded movement; do not
  teleport or reroll it because of range. After 1.5 seconds blocked, stop and
  end the pass into 0.8-second recovery, retaining an uncommitted selection.
- Missing/dead/held player: cancel offense and stop movement. Reacquire through
  the existing gameplay composition. Area unload resets this encounter instance.
- Nova becomes due after two completed family bags and at least 12 gameplay
  seconds since its previous attempt. Execute at the next pass boundary. Any
  attempt consumes this cadence even if countered. Only exhausting a family bag
  advances the completed-bag count; an interruption alone does not. No nova
  immediately after every stun.

## Attack data and initial tuning proposals

Durations below are gameplay seconds and starting values for Play Mode tuning.
Every next step gets its own anticipation; chaining does not remove telegraphs.

| Attack | Windup / active / recovery | Payload |
| --- | --- | --- |
| Basic sweep | 0.35 / 0.12 / 0.18 | Forward claw, 1 HP |
| Basic backhand | 0.30 / 0.12 / 0.20 | Wider horizontal claw, 1 HP |
| Basic slam | 0.55 / 0.15 / 0.45 | Downward front strike, 1 HP |
| Wingbreaker advance | 0.45 / 0.20 / 0.20 | Wing hit, advance <= 0.8 units, 1 HP |
| Wingbreaker upward claw | 0.30 / 0.15 / 0.40 | Front upper arc, 1 HP |
| Volley 1 / 3 / 5 | 0.45 / 0.55 / 0.65 windup; one release; 0.40 recovery | 1 HP per cast budget; spread offsets [0], [-12,0,12], [-30,-15,0,15,30] degrees |
| Beam | 0.80 / 0.70 / 0.75 | 0.3-unit thick straight beam, 1 HP once, opening counter window 0.20 |
| Nova | 2.40 / 0.15 / 0.90 | Radius 3.5 units, 2 HP once; core stable until active begins |

Between families add 0.25 seconds of repositionable recovery; after a full bag
add 0.80 seconds. Beam tracks only during its first 0.5 seconds of windup;
its final 0.3 seconds displays locked aim. Volley also locks for its final
0.15 seconds. No lead prediction in the first baseline. Projectiles travel at
6 units/second for at most 3 gameplay seconds and stop on Environment.

At most one feint per pass, proposed probability 20% on Basic's opening strike.
Sample once at windup entry; do not poll the player's input. At 0.15 seconds,
show recoil/wing-tuck cue and change to delayed claw. Maintain at least 0.35
seconds between that cue and the eventual active hit. No invisible cancellation,
beam/nova feint or branch after execution begins.

Initial health 45, global poise 12, regeneration 0. Ordinary melee already has
zero poise; default Poise Damage supplies 2.4 per hit for five hits. Restore full
poise on leaving Stunned/Staggered; pause poise ticking while either is active.
Stun 1.25, beam stagger 0.65, post-response poise resistance 0.75. During
resistance, accept health hits normally but do not drain global poise; special
core and beam counter rules remain usable. Body damage multiplier 1, exposed
head multiplier 1.5 during beam stagger only. No full-body invulnerable armor.

## Incoming damage and nova interaction

The current resolver chooses the first IDamageable on the target GameObject.
Do not rely on component order with EnemyHealth and GargoyleDamagePolicy on
the root. Queryable child hurtboxes must route explicitly to the gargoyle policy.
No root attack-target collider may permit a health-only bypass.

PlayerAttackHitDetector2D already groups hits by EnemyActor but knows only Golem
regions. Add a small documented region contract implemented by new gargoyle
hurtboxes, with owner identity, explicit damage recipient and priority. Give
core priority while open and head priority while exposed. Preserve Golem's
existing routing and Bat behavior with regressions; do not refactor all brains.
One primary strike applies health/poise to this actor at most once even when
body/core/head overlap.

DamageContext currently lacks DamageInstance provenance and attack execution
identity. Add optional provenance and execution identity to its constructor,
forwarded by DamageResolver, without breaking existing call sites. This is the
narrow evidence needed to distinguish primary melee from converted/supplemental
hits; generic Card tags and a positive poise value alone are insufficient.
Preserve those fields when policies rebuild an adjusted DamageContext. Default
directly constructed contexts can still apply health damage, but absent
provenance/execution evidence never qualifies a nova core counter. Keep poise
charge consumption restricted to the originating primary melee; the new
provenance regression must include mixed primary/supplemental packets.

On nova windup entry, enable core hurtbox and set reactor stability to 4.8. After
an accepted positive-health hit, drain global poise if permitted. Also drain
reactor stability only for core hits with primary provenance, a non-empty melee
execution ID, IsCardEnhancedMelee and positive poise payload. Clamp at zero.
Converted shots, supplemental packets, ordinary melee, body hits and card
selection alone cannot break the core. Default counter requires two 2.4 hits.
This identifies the Poise Damage-capable card type through its actual primary
melee payload; future cards supplying the same type may qualify. It is not an
inventory/name check for only one exact card asset.

Core break cancels nova and requests Stunned once. A lethal hit yields Dead.
Close/reset stability on every exit from nova windup. Keep ordinary health
damage accepted to establish Chain access. Do not reset global poise at nova
entry. Global depletion during nova also cancels it normally.

## Outgoing damage, counters and cleanup

Use DamageResolver with enemy-source identity and DamageProcPolicy.None; no
player reward/chain/supplemental procs on enemy attacks. Normalize player
colliders to one canonical player identity for hit budgets.

- Melee: one accepted hit per target per strike token.
- Volley: all 1/3/5 projectiles share a cast-level accepted-hit budget. Additional
  colliding shots expire without inflicting extra HP loss. This is necessary
  because SimpleHealth has no generic damage-invulnerability interval.
- Beam: thick segment clipped by the nearest terrain hit; evaluate a live
  intersecting Ward before player health damage. One accepted hit per player
  per cast; no frame-rate-scaled damage. Counter details are in the Ward spec.
- Nova: one radial release and one accepted hit per player; finite radius permits
  movement escape. Display the actual radius before release. Terrain does not
  shield the baseline nova; the test arena makes that behavior legible.

Stun, death, target loss and disable cancel queued shots, disable melee/beam/nova
queries and return/disable all projectiles still owned by this encounter, so
the opening is safe. Cleanup may not erase another enemy's shots. Normal attack
completion leaves released shots alive until collision/lifetime. Area unload
destroys owned projectiles with the encounter root. If projectile deflection is
used in future, it transfers ownership out of this cleanup registry.

## Card Time and energy feasibility

Existing world-owned selection sessions continue using their unscaled session
clock. Enemy timers, Ward duration, nova stability window and projectiles use
scaled gameplay delta; explicit pause/world-hold/death guards prevent damage
queries even when delta is zero. No new enemy-owned Card Time session/category.

The solo test scene equips Poise Damage, movement escape tools and Ward through
an authored test profile. Preserve current recovery cards and ordinary game
loadouts. At nova's start the player must be able to hit the core, enter Chain,
select Poise Damage and land two empowered hits before release. Measure this
with the actual player animations and Card Time settings; increase telegraph
time if the two-hit route does not fit. Do not assume a spreadsheet timer alone
proves the route. Missing energy/card still permits escaping the blast.

## Pixel and Unity acceptance contract

- Persist exact draft under `gdd/art-references/`; avoid an ephemeral external
  reference. Production art later belongs in `Assets/Art/Enemies/GargoyleSentinel`.
- Every production sprite rect is exactly 200 x 200, including padding. Ordinary
  poses fit within 128 x 128 alpha bounds. Extended pose art stays inside 200.
- Artist draft visible bounds: 128 x 107, alpha binary. Source-generation image
  was 1254 x 1254; it must never be imported as the production-sized sprite.
- Frame pivot is the same foot anchor: 100 pixels from left, 36 from bottom,
  normalized (0.5, 0.18). Mirror visuals around this anchor. Runtime root has
  unit scale; collider/attack geometry is authored independently of padding.
- Point filtering, no mipmaps or lossy compression; preserve alpha; no runtime
  upscaling of textures masquerading as additional pixel detail.
- Current player physical bounds are 1 x 2 units; visible imported rect is
  141 x 299 at 128 PPU. The hypothetical 32 x 64 player is not yet in the prefab.
- Provisional gargoyle PPU is 48 against this current player. The draft envelope
  then spans approximately 2.67 x 2.23 units; its full canvas spans 4.17 x 4.17.
  These describe bounds, not filled body area. Set a provisional body collider
  1.8 x 2.1 units, adjusted to actual solid anatomy, not wings/padding.
- For final visual acceptance, measure an agreed idle body mask excluding wings,
  effects and transparent pixels. Require gargoyle body mask world area >= 1.5
  times the player's, using occupiedPixels / PPU^2 at unit scale. Tooling reports
  the ratio; the human confirms what counts as body. Adjust gargoyle PPU/artist
  anatomy if it fails, preserving the 200/128 pixel constraints and player scale.
- If the player later adopts a true 32 x 64 sprite, remeasure and retune gargoyle
  PPU; do not assume equal PPU with the current assets. 64 PPU is an example
  yielding a 2 x 2-unit 128 envelope, not a confirmed current import requirement.

## Editor setup and verification

Idempotent `TIC/Setup/Create Or Update Gargoyle Sentinel` creates definitions,
attack assets, profiles, prefab and a standalone `Test_GargoyleSentinel` scene.
One gargoyle, no other enemies, enough floor to leave nova radius on either
side, nearby reset/checkpoint and visible attack debug. Preserve existing scenes
and run loadout. Setup reports missing references, invalid timings, impossible
attack ranges, unsafe probes, malformed sprite rects and card-slot conflicts.

Automated checks initialize dependencies explicitly:

1. Selector: all permutations, deterministic seeds, no repeat within/between
   bags, volley counts, interruption/blocked-target preservation and nova cadence.
2. Runner/brain: phase boundaries, large/zero deltas, no skipped damaging phase,
   single emissions, cancellation, death priority and stale animation callbacks.
3. Damage: root-policy bypass prevention, overlapping hurtboxes, one poise/card
   charge per primary strike, core-only qualifying hits, simultaneous lethal
   depletion, stun restoration and resistance.
4. Payloads: exact 1/3/5 counts and angles, common hit budget, terrain clipping,
   once-per-cast beam/nova damage, cleanup and no damage during pause/hold.
5. Ward integration as specified separately; existing Golem/Bat/card/recovery
   tests remain unchanged unless a deliberate contract extension requires updates.
6. Serialization: exact 200 frames, alpha envelopes, fixed pivots, PPU/root scale,
   references/layers and identical setup hashes after a second run.

Compile runtime/Editor assemblies and run focused EditMode and PlayMode fixtures.
The latest recorded baseline has 20 inherited EditMode failures and 3 skips;
compare exact identities and report new failures separately. No tests were run
for this documentation-only spec. Human Play Mode acceptance covers readable
feints, every order, reaching the core/Chain route, beam guard timing, movement
fallbacks, pixel/world scale, stun fairness and keyboard/gamepad feedback.

## Delivery boundaries

Implement in reviewable slices: melee/stun and routing; selector/volley; Ward
dependency and beam; nova; feint/presentation/arena acceptance. This is delivery
ordering, not the detailed implementation plan. The written spec and proposed
defaults require review before that plan and any gameplay implementation.
