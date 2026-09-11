# Bat Machine Enemy SDD - 20260901-1202

## Contexto

This specification defines the second authored combat enemy: a half-bat,
half-machine ranged flyer. It is intentionally a moderate threat. Its purpose
is to make the player manage airspace, use damage-capable cards for poise
pressure, choose when to parry/deflect, and exploit a dangerous aerial stun.

Sources used:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/simple-grounded-and-aerial-patrol-implementation-sdd-20260614-0014.md`
- `specs/state-machine-owned-state-evolution-sdd-20260806-2146.md`
- `specs/golem-charger-enemy-sdd-20260806-2146.md`
- current enemy, card, and damage code under `Assets/Scrips/Architecture`

This document exists after the Golem Charger slice to introduce reusable poise
and converted projectile damage without turning the Charger-specific combat
rules into a universal enemy superclass.

## Design Summary

The bat machine patrols a bounded random aerial space after loading. Once it
detects the player, it follows with smooth steering around a preferred combat
distance, telegraphs a predictive projectile, and can evade an approaching
player. It becomes more aggressive below health and poise thresholds, but a
player can create a decisive opening by depleting its poise with suitable
cards.

The enemy is not meant to be a tremendous threat. Its danger comes from
readable, dynamic pressure rather than unavoidable shots, perfect reaction
dodges, or high raw durability.

## State Machine And Decision Model

Use a typed `BatMachineState` with committed state ownership. Do not introduce
a behavior-tree framework or a global utility-AI framework for this slice.

```text
PatrolRandom
  -> player enters monitor radius: Engage

Engage
  -> valid dodge threat and successful poise-scaled roll: Evade
  -> fire ready: WindupFire
  -> player leaves monitor radius: PatrolRandom

WindupFire
  -> prediction sample completes: Fire, then Engage

Evade
  -> maneuver completes: Engage

PatrolRandom / Engage / WindupFire / Evade
  -> poise is depleted: StunnedFall

StunnedFall
  -> terrain landing: Dead when fall damage is lethal; otherwise GroundedRecovery

GroundedRecovery
  -> recovery completes: Engage when player remains monitored; otherwise PatrolRandom
```

The state machine owns hard commitments, timers, transitions, and lifecycle.
Small pure collaborators provide decision facts and action execution:

- `BatThreatMonitor` reports player monitoring, relative approach speed,
  base-melee-reach threat, and line-of-sight facts;
- `BatDodgeEvaluator` decides whether a valid threat passes the cooldown and
  chance rules;
- `BatShotPredictor` samples player movement during the fire windup and returns
  a bounded predicted aim point;
- `AerialSteeringMotor2D` moves toward patrol, follow, or evade targets without
  making state decisions.

The brain must expose explicit `Initialize`, `Tick`, and `FixedTick` methods
for EditMode tests. Animator events remain presentation-only; gameplay timers
and state ownership are authoritative.

## Aerial Movement

### Random patrol

At initialization, `PatrolRandom` chooses a deterministic seeded waypoint
inside serialized horizontal and vertical bounds around its spawn anchor. On
arrival it waits for a serialized short interval, then chooses a new waypoint.
The waypoint generator must reject points inside environment colliders and
points closer than a configured minimum distance to the current position.

### Engage steering

While monitored, `Engage` uses smooth velocity steering toward a preferred
distance and height band around the player. The target is an offset around the
player rather than the player's exact position, preventing the bat from sitting
directly on top of the player. All Rigidbody2D writes are owned by the motor in
`FixedTick`.

### Evade

The evasion action chooses a short lateral/vertical destination away from the
evaluated threat, then commits to it for its authored duration. It may be
started only from `Engage`; it cannot cancel an active firing windup.

## Monitoring And Dodge Rules

The monitor radius only establishes whether the player is relevant. It does
not itself make decisions or cause movement.

A dodge is eligible only when all conditions are true:

1. The brain is in `Engage`.
2. The dodge cooldown has elapsed.
3. The player is inside monitor range.
4. The player is closing on the bat quickly enough from any direction, or is
   near the outer edge of the player's base melee reach.

The base melee reach is deliberately used instead of card-enhanced range. A
range-enhancing card can therefore punish a bat that pre-emptively evades;
the AI must not invalidate that player advantage by using buffed reach for its
own dodge prediction.

For each eligible evaluation, chance is derived from current poise:

```text
current poise >= maximum poise: 70%
current poise <= 10:            20%
otherwise: linear interpolation between those values
```

The evaluator rolls once per eligible threat window, not every frame. This
avoids jitter and makes a failed roll meaningful until the next cooldown or
new threat window.

## Health, Poise, And Stun

Add `EnemyPoise` as a reusable optional enemy capability. It is independent
from `EnemyHealth` and exposes maximum/current normalized poise, explicit
initialization, `Tick`, `ApplyPoiseDamage`, restore/reset operations, and a
single depletion event.

Initial bat tuning:

```text
maximumPoise = 30
poiseRegenerationPerSecond = 0.33
normalAttackPoiseDamage = 0
```

Damage-capable cards may author non-negative `PoiseDamage` at the card site.
Cards with no damage implication must author zero poise damage. The first
implementation extends the existing damage-capable `CardOperationDefinition`
entries (`ModifyDamage` and `ArmSupplementalDamage`) with that authored field;
the player damage construction path forwards the armed/resolved poise amount
into the damage transaction only when its associated hit actually resolves.

`DamageContext` gains a non-negative `PoiseDamage` field. Enemy-specific damage
policies forward accepted health damage as they do today, then route the poise
amount to `EnemyPoise` when equipped. This keeps cards as the authority for
poise tuning while allowing all enemies to opt into the capability gradually.

At zero poise, the bat immediately enters `StunnedFall`. Ordinary stun timers
do not recover it in the air. Its aerial steering stops, gravity is restored,
and it remains helpless until a valid terrain landing.

On landing, calculate fall damage from the recorded downward impact speed using
serialized safe-speed and damage-per-speed-unit tuning. Apply it through the
shared damage path. Lethal landing damage enters `Dead`; otherwise the bat
enters `GroundedRecovery`, restores serialized post-stun poise, then resumes
`Engage` or `PatrolRandom`. This allows a sufficiently high stun to be fatal
while preserving survival at lower falls.

## Fire Rules

`WindupFire` owns a visible, authored delay before any projectile exists. During
that delay, `BatShotPredictor` samples recent player position and velocity. At
the fire marker, it predicts a bounded future point and eases the launch vector
toward it. The aim direction is locked when the projectile launches; it never
homes after launch.

Fire modifiers are independent and stack:

```text
health > 50%: normal fire cooldown
health <= 50%: shorter fire cooldown

poise > 50%: one projectile per firing action
poise <= 50%: two-projectile burst per firing action
```

The second burst projectile uses an authored inter-shot delay and the same
locked initial firing solution. This keeps the burst readable.

## Projectile Deflection And Converted Damage

`EnemyProjectile2D` is responsible for projectile movement, collision, and
single-hit lifetime. The original enemy projectile is aimed but non-homing.

A defense/parry-capable card effect may deflect a projectile. On deflection:

1. preserve the projectile's health-damage amount;
2. change its source and damage provenance to the player;
3. reverse its travel direction from its current flight direction, with no
   target seeking;
4. set its poise damage to exactly `10`, independent of the deflecting card;
5. allow it to damage any valid entity through the usual resolver;
6. expire after its first accepted hit or its normal lifetime.

Converted damage needs an explicit provenance kind rather than masquerading as
a primary player attack. Add `Converted` to `DamageOriginKind` and a factory on
`DamageProvenance` that retains the original projectile instance as parent/root
context while recording the conversion effect. Its proc policy must be explicit
so reflection cannot accidentally advance player attack chains or duplicate
card effects.

## Runtime Composition

```text
BatMachine
|- EnemyActor
|- EnemyHealth
|- EnemyPoise
|- BatMachineBrain
|- AerialSteeringMotor2D
|- BatThreatMonitor
|- BatDamagePolicy
|- BatProjectileLauncher
|- Rigidbody2D
|- BodyCollider2D
|- Hurtbox
|- ProjectileSpawn
`- VisualRoot/SpriteRenderer
```

`EnemyActor` remains identity and lifecycle. `EnemyHealth` remains shared health.
`EnemyPoise` is shared poise storage. `BatMachineBrain` owns state. The policy
owns acceptance and forwarding rules. Movement, projectiles, monitoring, and
presentation remain focused collaborators.

## Test Strategy

EditMode coverage must include:

- deterministic random-patrol waypoint selection and arrival;
- monitor entry/exit transitions;
- smooth follow requests through the aerial motor;
- no dodge outside `Engage`, during fire windup, or while cooldown is active;
- high- and low-poise dodge probability boundaries with injected deterministic
  rolls;
- card-authored poise forwarding only on an accepted damage hit;
- normal attack zero-poise behavior;
- poise regeneration and depletion;
- transition to `StunnedFall`, no airborne recovery, lethal fall landing, and
  non-lethal grounded recovery;
- fire-windup prediction lock and no post-launch homing;
- independent health cooldown and poise burst modifiers, including stacking;
- deflection source/provenance conversion, fixed 10 poise damage, and
  non-targeted reflected direction;
- deflected projectile first-hit expiry and ordinary projectile behavior.

Run the focused fixture during development and all EditMode tests before
prefab setup. Follow with an idempotent Unity Editor setup command that creates
the definition, projectile data, prefab, hurtboxes, layers, and a small test
arena. The user validates visual telegraphs, patrol bounds, evade readability,
fall feel, and collision placement in Play Mode.

## Implementation Order

1. Extend damage transactions and card-authored damage operations for poise.
2. Add and test `EnemyPoise` without changing existing enemies.
3. Add converted-damage provenance and a reusable deflectable projectile.
4. Add and test aerial steering, monitoring, dodge evaluation, and prediction.
5. Add the bat brain, state enum, damage policy, and focused tests.
6. Add health/poise fire modifiers and fall landing damage.
7. Create art/data assets, then idempotent prefab and test-arena setup tooling.
8. Run full EditMode verification and perform Unity Play Mode tuning.

## Scope Boundaries

This slice does not introduce behavior trees, global enemy coordination,
homing projectiles, universal resistance systems, projectile pooling, or a
generic parry framework beyond the minimum deflection contract needed by this
enemy. Those can be extracted only after at least one additional real consumer
demonstrates the same needs.
