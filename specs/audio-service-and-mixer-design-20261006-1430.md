# Audio Service And Mixer Design - 20261006-1430

## Contexto

This revision supersedes `specs/audio-service-and-mixer-design-20261006-1426.md`, preserved as historical design memory. It records the user requirement that every world SFX plays at its invocation position, replacing the prior proposed 2D-only baseline. The user approved
a thin centralized controller around Unity's native audio tools and requested
audio channels to support volume settings later. This document is a design for
review; implementation and an implementation plan have not yet been approved.

Sources:

- `specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md`
- `specs/gameplay-scene-ownership-sdd-20260920-1853.md`
- `specs/card-effect-feedback-20260625-0929.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- `specs/code-conventions-20260526-0014.md`
- current `IGameplayServices` and `GameplayServicesRoot` implementation
- Unity 6.3 Audio Random Container documentation:
  https://docs.unity3d.com/6000.3/Documentation/Manual/AudioRandomContainer-UI.html

## Intent And Scope

Provide clear prototype combat, movement, Card Time, and UI feedback using
Kenney audio assets, native variation authoring, and centralized playback.
Expose independently adjustable volume categories without building a settings
screen in this slice. Avoid reimplementing native clip randomization.

Initial cues: attack swing, confirmed hit, jump, landing, dash, player hurt,
Card Time entry/exit, and card selection/confirmation. Music and ambience
receive mixer routes now; their content and playback systems are future work.

## Mixer Channels

One AudioMixer asset uses this hierarchy:

```text
Master
|- SFX
|  |- Combat
|  `- Movement
|- UI
|- Music
`- Ambience
```

Expose stable mixer parameters: `MasterVolume`, `SfxVolume`, `UiVolume`,
`MusicVolume`, and `AmbienceVolume`. Combat and Movement inherit SFX volume;
their separate groups permit balancing and effects without extra user sliders.
Master affects every route. All playback must be explicitly routed; no default
unrouted source should bypass these controls.

An audio settings contract accepts normalized values from 0 to 1. Convert
positive linear gain to decibels with `20 * log10(value)` and clamp to the
mixer's supported floor; zero uses a defined silence floor (proposed -80 dB).
Apply defaults after mixer initialization, not during Awake. Parameter names
are configuration owned by the audio subsystem, not strings supplied by UI.

Default all user gains to 1. Mix individual cues to sensible levels. Runtime
volume changes are in scope; persistent preferences and the settings menu are
deferred. Future settings UI calls this contract without editing sound assets.

## Components And Data Flow

- `AudioService`: persistent `IGameplayModule` under the existing services
  root; owns a bounded pool of AudioSources and runtime playback policy.
- `IAudioService`: injected through `IGameplayServices`; requests playback
  through cue references, without public singleton access or scene searches.
- `SoundCueSO`: native AudioResource reference (AudioClip or Audio Random
  Container), mixer group, priority, maximum simultaneous instances, cooldown,
  pause category, and spatial attenuation settings. No duplicate clip lists or random pitch/volume ranges.
- Audio presenters: map authoritative gameplay observations to cues. The
  service does not infer damage, Card Time outcomes, or player actions.
- Audio settings contract: exposes mixer category gains separately from
  playback requests; can share the persistent module implementation initially.

Flow: gameplay observation -> audio presenter -> cue request -> pooled source
positioned at the invocation world position, with native audio resource and mixer route -> mixer -> listener.

## Playback Policies

Initial proposed defaults, pending design review:

- Every world SFX request requires an explicit world-space Vector3 captured at
  invocation. Assign this position to the pooled source before playback. Never
  default to the service root, world origin, camera, or current player position.
- One-shot voices remain at that captured position for their full lifetime,
  even when the invoking actor moves. They do not follow the actor. Owner
  context controls cleanup only; it does not supply a changing position.
- Confirmed hits use the effective impact point; movement and attack swings
  use the relevant actor or authored emitter position captured at invocation.
  When damage data lacks a contact point, the presenter explicitly supplies
  the damaged actor's captured position rather than the service inventing one.
- World SFX use positional playback with configurable min/max distance and
  rolloff. For the 2D world, tune stereo panning/attenuation against the actual
  listener placement; start with Doppler disabled. Establish one active
  AudioListener and verify camera depth does not distort intended distances.
- UI sounds have a separate non-positional playback request. Mixer routing
  and spatial behavior are separate concerns: Combat and Movement remain
  routed under SFX regardless of emitter position.
- Reset source position and all spatial settings on pool reuse, including
  reuse between UI and world voices. Following emitters and loops are deferred.
- Use manual-trigger, single-play native container settings for one-shot cues.
- Cooldowns and source lifetime tracking use unscaled time. Source completion
  detection must account for resource playback and paused sources rather than
  assuming AudioClip.length is available on every resource.
- At a cue's overlap limit, drop the new request. At global capacity, replace
  the oldest lower-priority voice, otherwise drop the new request. Define
  priority ordering explicitly in implementation and verify it with tests.
- World sounds continue normally through hitstop and Card Time initially;
  do not derive source pitch from Time.timeScale.
- Pause menu pauses world voices and suppresses new world requests. UI remains
  audible. Resuming restores the paused voices. Future music/ambience pause
  behavior can be explicitly selected when their playback systems are added.
- Scene teardown stops voices owned by that scene, while the service survives.
  Requests therefore carry owner/lifetime context; sources must not retain
  destroyed actor references indefinitely.
- Missing resources or invalid routing fail gracefully with actionable
  diagnostics and do not interrupt gameplay.

Confirmed hit audio must follow effective damage results, rather than raw
collider contact. Avoid duplicate sound triggers from both damage observation
and visual feedback. Movement cues follow discrete transitions; footsteps and
animation-timed audio are outside the first slice.

## Asset And Editor Workflow

Keep downloaded original audio and included licenses in a clearly identified
third-party folder. Maintain source/license records. Use selected Kenney packs
as source material; audition exact clips before considering the mix final.

Follow the repository Editor collaboration workflow. Prefer idempotent tooling
for source pools, cue assets, and bootstrap wiring. Verify Unity 6.3 supported
APIs before automating native container/mixer authoring. If their authoring
requires Editor interaction, provide a small explicit handoff instead of
unsupported APIs or broad handwritten YAML changes. Preserve existing work.

## Validation And Acceptance

- Verify independent SFX/UI/Music/Ambience gains and Master inheritance;
  zero is silent and returning to 1 restores authored levels.
- Verify every world voice starts at the supplied invocation position and
  remains there after the actor moves; verify impact versus actor positions.
- Verify concurrent voices from distinct emitters retain distinct positions,
  and pool reuse never inherits a previous voice's position/spatial settings.
- Verify left/right and near/far listening behavior with the actual camera and
  listener, while UI sounds remain non-positional.
- Verify cue overlap, cooldown, bounded pool capacity, and priority behavior.
- Verify exactly one service survives scene transitions and Play Mode restart.
- Verify world pause/resume and UI playback while paused.
- Verify owner teardown releases scene-scoped voices.
- Verify native randomized resources play through the configured mixer routes.
- Play Mode listening checks confirm cue timing, variation, repetition, and
  clipping under rapid attacks and multiple enemies; automated tests cannot
  establish subjective audio quality.

## Review Status

The user agreed to native audio tools plus a thin centralized controller and
requested mixer channels and mandatory invocation-position world SFX. The hierarchy above and remaining proposed playback defaults
are presented for review. Next step after written-spec approval: create the
implementation plan using the writing-plans skill, then select execution method.

