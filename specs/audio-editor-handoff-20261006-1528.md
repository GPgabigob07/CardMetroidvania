# Audio Editor Handoff - 20261006-1528

## Contexto

Runtime audio and seeded native assets are prepared from the approved
`specs/audio-service-and-mixer-design-20261006-1430.md` and
`specs/audio-service-implementation-plan-20261006-1431.md`.
Unity's mixer-group authoring classes are internal. Following the repository
Editor workflow, finish this native authoring step in Unity rather than using
private reflection APIs or handwritten mixer YAML. Existing scenes/prefabs
have not been changed by audio setup yet; the final menu wires them safely.

## 1. Create The Mixer Groups

In Unity, let the scripts/assets finish importing. Double-click:

`Assets/Data/Audio/PrototypeAudio.mixer`

The mixer currently has its native Master group. In the Audio Mixer window,
select Master and use the plus button in Groups to create and rename its
children to SFX, UI, Music, and Ambience. Select SFX and create Combat and
Movement beneath it. Match this hierarchy exactly:

```text
Master
|- SFX
|  |- Combat
|  `- Movement
|- UI
|- Music
`- Ambience
```

Keep each name unique and leave the group volume faders at 0 dB initially.

## 2. Expose Five Volume Parameters

For each group below, select it and right-click its Volume control in the
Inspector; choose the option to expose its volume to scripts. In the mixer's
Exposed Parameters list, rename the resulting entry exactly as follows:

| Group | Exposed parameter |
|---|---|
| Master | MasterVolume |
| SFX | SfxVolume |
| UI | UiVolume |
| Music | MusicVolume |
| Ambience | AmbienceVolume |

Combat and Movement do not need exposed volume parameters: they inherit SFX.
Save the mixer/project assets (Ctrl+S).

## 3. Run Deterministic Setup

Run `TIC > Audio > Setup Prototype Audio` from Unity's main menu.

The tool validates group names, hierarchy, and exposed parameters before
wiring the cues. It then adds the audio module to
`Assets/Resources/Runtime/GameplayServices.prefab`, adds the player audio
presenters to `Assets/Prefabs/Player/Player.prefab`, and adds damage audio
presenters to enemy damage recipients, including child hurtboxes.

The tool fills missing references and preserves existing non-null authored
references and cue tuning. A rerun must not add duplicate modules/presenters.
Save assets. If Console reports a missing name/parameter, correct it and rerun.
No Inspector assignment of individual clips is required.

## 4. Listen In Play Mode

Use the normal playtest entry flow so the configured player prefab and global
services are instantiated. Existing scene-local player overrides may require
checking inherited prefab changes before testing.

- Jump, land, dash, and attack: one cue per successful action.
- Hit Golem/Bat/Gargoyle child hurtboxes: one impact cue at the contact point.
- Invoke a sound then move: the one-shot stays at its original world position.
- Listen to emitters on the left/right and near/far. Initial attenuation uses
  min distance 12, max 40, linear rolloff, Doppler disabled; tune against the
  actual listener/camera depth. Confirm exactly one active AudioListener.
- Open/close Card Time, change selection, and successfully confirm a card.
- Pause: existing world sounds pause, new world requests are suppressed,
  and UI playback remains available; resume restores paused world voices.
- Change scenes: the service survives; voices owned by unloaded scenes stop.
- Try rapid hits and multiple enemies to check overall loudness and overlap.

Clips are provisional candidates. Volume/clip variation quality requires your
listening judgment. Music and Ambience have mixer routes but no player/content
in this first slice. There is no settings screen or preference persistence yet.

## Follow-up Evidence

After this setup, the saved mixer and prefab changes let Codex verify routing,
run mixer settings and setup idempotency checks, and finish the implementation
checkpoint. Report the first Console error if any, and any sounds that feel
too quiet/loud, delayed, overly repetitive, or incorrectly positioned.
