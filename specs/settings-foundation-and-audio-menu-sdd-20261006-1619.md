# Settings Foundation And Audio Menu SDD - 20261006-1619

## Contexto

New settings slice following the completed audio baseline. The user requested
an audio settings menu and a baseline settings structure for controls later,
then explicitly deferred input configuration and chose immediate audio changes.
The preferred checkout remains the current checkout, preserving existing work.

Sources:

- `specs/audio-service-and-mixer-design-20261006-1430.md`
- `specs/audio-service-verification-20261006-1542.md`
- `specs/external-playtest-build-design-20260929-1147.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- `specs/code-conventions-20260526-0014.md`
- current PlaytestMenuView, PlaytestFrontendSetup, GameplayServicesRoot,
  AudioService, and IAudioSettingsService implementations

This is a new subsystem design for review. No settings implementation has begun.

## Intent And Scope

Let players adjust audio from title and pause menus, hear changes immediately,
and retain preferences across application restarts. Establish clear ownership
and storage boundaries so a later controls settings feature can join the same
settings structure without changing gameplay saves or mixing UI with storage.

This slice includes audio preferences, a shared settings service, persistent
storage, menu navigation, and deterministic prefab setup. Input presets,
automatic device switching, rebinding, and changes to current control schemes
are excluded. No nonfunctional Controls tab is introduced.

## User Experience

- Add Settings to both the title and pause menus. Preserve their existing
  Controls help, Card Time guide, resume/start, return-to-title, and quit flows.
- Settings opens on Audio. The panel provides five sliders in this order:
  Master, SFX, UI, Music, Ambience. Display the current integer percentage.
- Slider values span 0-100%, with 1% steps. The service stores linear gain
  from 0 to 1 and uses the existing audio service's decibel conversion.
- Changes apply immediately. There are no Apply/Cancel buttons and Back does
  not revert changes.
- Reset Audio Defaults restores all five categories to 100% immediately and
  marks preferences dirty; it does not alter cue tuning or gameplay progress.
- Save dirty preferences when leaving Settings, including Back/Escape/gamepad
  cancel and any menu transition that closes the panel. Also flush dirty
  preferences on application pause, focus loss, and orderly application quit.
  Do not synchronously flush storage for every slider tick.
- Back returns to the parent title/pause menu and restores selection to its
  Settings entry. Closing Settings while paused does not resume gameplay;
  another Back from the pause root follows the existing resume behavior.
- Keep the existing visual style. Make the five rows and Reset/Back usable at
  the current 1280x720 target resolution and existing canvas scaling.
- Mouse drag/click, keyboard, and gamepad navigation work: up/down selects
  rows/buttons, left/right adjusts the selected slider, submit activates a
  button, and cancel goes back one level. Menu timers use unscaled time.
- Audio and preferences are available on the title screen without an active
  player. World pause and UI audio behavior remain as in the completed baseline.

Music and Ambience sliders are functional mixer controls even though their
playback/content systems are not implemented yet. No sample audio or looping
preview is required for this slice.

## Settings Ownership And Persistence

Use one persistent UserSettingsService under the existing GameplayServices
root. It participates in explicit module lifecycle and exposes a focused
IUserSettingsService through the existing service facade. The menu binds the
service and does not read/write PlayerPrefs or manipulate AudioMixer directly.

Keep these responsibilities separate:

- AudioSettingsValues: typed runtime preferences for the five categories,
  with 100% defaults; no Unity scene/asset references.
- UserSettingsService: owns current preferences, immediate application,
  change notifications, dirty tracking, defaults, and save coordination.
- ISettingsStore and PlayerPrefsSettingsStore: storage access behind a small
  replaceable boundary, using stable per-category keys.
- AudioSettingsPresenter/view: binds sliders and labels, requests changes,
  and refreshes controls without recursively triggering slider callbacks.
- PlaytestMenuView: owns panel navigation and parent focus restoration.

Use namespaced keys `TIC.Settings.Audio.v1.Master`, `.Sfx`, `.Ui`, `.Music`,
and `.Ambience`. Keys do not incorporate application/build version. Future
controls preferences use their own namespace and validation through the same
storage boundary; this slice does not create unused control-setting fields.

Defaults are applied for missing or non-finite saved values; finite values
are clamped to [0,1]. This prevents malformed preferences from producing NaN
mixer values or accidental silence from missing fields. Changing a category
to its current value does not mark preferences dirty. Never delete unrelated
PlayerPrefs or mix user settings with per-run progression data.

If storage save fails, retain the dirty state and in-memory/applied values,
log an actionable diagnostic, and allow a later save retry. Do not interrupt
gameplay or silently reset settings.

## Audio Readiness And Lifecycle

Saved preferences must apply once on startup after the native mixer is ready,
before ordinary menu interaction. Coordinate this explicitly:

- Extend the audio settings contract with a readiness property and a ready
  notification, emitted after AudioService's initial mixer defaults are applied
  in the first runtime update, rather than during Awake.
- UserSettingsService loads its preferences during Initialize and subscribes
  to audio readiness through an explicitly configured persistent dependency.
  If audio is already ready, apply immediately; otherwise apply on notification.
- Applying loaded preferences must not mark them dirty. The saved profile
  supersedes the audio module's initial 100% defaults; do not reapply defaults
  each frame or overwrite a loaded setting when the menu opens.
- If a mixer parameter is unavailable, retain the desired preference and
  report the failure. Do not claim an unapplied mixer value was applied.
- Unsubscribe during Shutdown and reset readiness during audio Shutdown.
  Reinitialization and domain-reload-disabled Play sessions must remain safe.

Global service lifetime means settings survive return-to-title, gameplay area
changes, and player respawn. Menu views can come and go without owning the
preferences. Persistence is separate from the playtest's unsaved progress.

## Editor Integration

Use a narrow idempotent Editor upgrade command to edit the existing
`Assets/Resources/Runtime/PlaytestSession.prefab` and
`Assets/Resources/Runtime/GameplayServices.prefab` through Unity APIs.
Add the settings module, panel, sliders, and the necessary extra menu button;
preserve existing artwork, authored menu content, references, and other modules.
Do not rebuild scenes or overwrite Build Settings to add a settings panel.

Extend the existing frontend generation path so deliberately regenerating the
frontend later also includes the settings UI. Existing authored prefab upgrades
remain a separate safe command. Save assets and inspect serialized output.

## Validation And Acceptance

- Missing, out-of-range, and non-finite storage values resolve safely; keys
  remain stable, categories independent, and unrelated preferences untouched.
- Immediate changes reach the correct mixer category; Master affects all
  routes via existing hierarchy, and Reset restores all five values.
- Readiness applies loaded preferences once without a subsequent default
  overwrite; title-screen startup works without a player.
- Dirty changes persist on panel exit and lifecycle flushes, reload on a new
  session, and a failed store flush retains dirty state for retry.
- Open/close from title and pause, verify parent focus and gameplay pause,
  and ensure Escape/gamepad cancel does not skip levels or resume too early.
- Keyboard/mouse/gamepad sliders and buttons work without callback loops,
  duplicate listeners, gameplay input leaking through, or stale percentage text.
- Repeated setup adds no duplicate components/rows/buttons and preserves
  existing menu behavior. Scene changes and two Play sessions without domain
  reload do not duplicate settings ownership or subscriptions.
- Render/preview the menu for layout verification; Play Mode user feedback
  confirms legibility, navigation, and immediate audio changes.

## Review Status

Confirmed: audio-focused scope, input deferred, immediate slider changes.
Proposed here for approval: saving on exit/lifecycle flushes, 100% defaults,
stable PlayerPrefs storage behind an interface, and the shared settings owner.
After written-spec approval, create and review the implementation plan using
the writing-plans skill. The existing native-execution/current-checkout
preference remains available; no implementation approval is inferred yet.
