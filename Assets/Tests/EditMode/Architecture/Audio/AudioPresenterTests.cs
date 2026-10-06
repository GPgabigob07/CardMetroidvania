using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioPresenterTests
    {
        private sealed class RecordingAudio : IAudioService
        {
            public readonly List<Vector3> Positions = new();
            public int UiCount;
            public void SetWorldPaused(bool paused) { }
            public bool TryPlayWorld(SoundCueSO cue, Vector3 position, GameObject owner) { Positions.Add(position); return true; }
            public bool TryPlayUi(SoundCueSO cue, GameObject owner) { UiCount++; return true; }
        }

        private sealed class Services : IGameplayServices
        {
            public IUserSettingsService Settings => null;
            public IAudioService Audio { get; set; }
            public IAudioSettingsService AudioSettings => null;
            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime => null;
            public CardTimeSessionEventChannelSO CardTimeTransitions { get; set; }
            public ICardFeedbackService CardFeedback => null;
            public IGameStateService GameState => null;
        }

        private sealed class Catalog : ICardCatalog
        {
            public CardDefinitionSO First;
            public CardDefinitionSO Second;
            public bool TryGetCard(string id, out CardDefinitionSO card)
            { card = id == First.Id ? First : id == Second.Id ? Second : null; return card != null; }
        }

        [Test]
        public void CardTime_ChannelRebindingAndEnableCycles_EmitOnePositionedCuePerTransition()
        {
            var owner = new GameObject("card time emitter");
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            var channel = ScriptableObject.CreateInstance<CardTimeSessionEventChannelSO>();
            try
            {
                var audio = new RecordingAudio();
                var services = new Services { Audio = audio, CardTimeTransitions = channel };
                var presenter = owner.AddComponent<CardTimeAudioPresenter>();
                presenter.Configure(cue, cue);
                presenter.BindGameplayServices(services);
                presenter.BindGameplayServices(services);
                var inactive = new CardTimeSessionSnapshot(CardTimeSessionState.Available, PlayerCardTimeState.Neutral, PlayerCardTimeState.None, 0, 1);
                var active = new CardTimeSessionSnapshot(CardTimeSessionState.Active, PlayerCardTimeState.Neutral, PlayerCardTimeState.Neutral, 0, 1);
                owner.transform.position = new Vector3(2, 3, 0);
                channel.Raise(new CardTimeSessionTransition(inactive, active));
                channel.Raise(new CardTimeSessionTransition(active, active));
                presenter.enabled = false;
                channel.Raise(new CardTimeSessionTransition(active, inactive));
                Assert.AreEqual(1, audio.Positions.Count);
                presenter.enabled = true;
                owner.transform.position = new Vector3(4, 5, 0);
                channel.Raise(new CardTimeSessionTransition(active, inactive));
                Assert.AreEqual(2, audio.Positions.Count);
                Assert.AreEqual(new Vector3(2, 3, 0), audio.Positions[0]);
                Assert.AreEqual(new Vector3(4, 5, 0), audio.Positions[1]);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(cue); Object.DestroyImmediate(channel); }
        }

        [Test]
        public void SelectionTransaction_NotifiesOnlyActualSuccessfulSelectionChanges()
        {
            var first = ScriptableObject.CreateInstance<CardDefinitionSO>();
            var second = ScriptableObject.CreateInstance<CardDefinitionSO>();
            try
            {
                first.Configure("first", "First", "", PlayerCardTimeState.Neutral, null, null);
                second.Configure("second", "Second", "", PlayerCardTimeState.Neutral, null, null);
                Assert.IsTrue(CardTimeSelectionTransaction.TryCreate(PlayerCardTimeState.Neutral, 1,
                    new[] { "first", "second" }, new Catalog { First = first, Second = second }, out var selection));
                var changes = 0;
                selection.SelectionChanged += () => changes++;
                Assert.IsTrue(selection.SelectIndex(0));
                Assert.IsFalse(selection.SelectIndex(5));
                Assert.IsTrue(selection.MoveSelection(1));
                Assert.IsFalse(selection.MoveSelection(1));
                Assert.IsTrue(selection.SelectIndex(0));
                selection.Dispose();
                Assert.IsFalse(selection.MoveSelection(1));
                Assert.AreEqual(2, changes);
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void Damage_UsesImpactPosition_RejectsIneffectiveAndSupplementalHits()
        {
            var owner = new GameObject("recipient");
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                var audio = new RecordingAudio();
                var presenter = owner.AddComponent<DamageAudioPresenter>();
                presenter.Configure(cue);
                presenter.BindAudio(audio);
                owner.transform.position = new Vector3(50, 50, 0);
                var context = new DamageContext(null, owner, null, 1, new Vector2(2, 3), Vector2.right);
                presenter.OnDamageReceived(context, new DamageResult(true, false, 1, 4));
                presenter.OnDamageReceived(context, new DamageResult(false, false, 0, 4));
                presenter.OnDamageReceived(context, new DamageResult(true, false, 0, 4));
                var supplemental = new DamageContext(null, owner, null, 1, Vector2.zero, Vector2.right,
                    provenance: DamageProvenance.Supplemental("p", "r", "effect"));
                presenter.OnDamageReceived(supplemental, new DamageResult(true, false, 1, 3));
                Assert.AreEqual(1, audio.Positions.Count);
                Assert.AreEqual(new Vector3(2, 3, 0), audio.Positions[0]);
                presenter.enabled = false;
                presenter.OnDamageReceived(context, new DamageResult(true, false, 1, 3));
                Assert.AreEqual(1, audio.Positions.Count);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(cue); }
        }

        [Test]
        public void PlayerActions_KeepInvocationPositions_AndUiStaysNonPositional()
        {
            var owner = new GameObject("presenter owner");
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                var audio = new RecordingAudio();
                var player = owner.AddComponent<PlayerAudioPresenter>();
                player.Configure(cue, cue, cue, cue);
                player.BindAudio(audio);
                player.PresentAction(PlayerActionState.Attack1, new Vector3(2, 3, 0));
                player.PresentAction(PlayerActionState.Dash, new Vector3(4, 5, 0));
                player.PresentJump(new Vector3(6, 7, 0));
                player.PresentLanding(new Vector3(8, 9, 0));
                owner.transform.position = Vector3.one * 100;
                Assert.AreEqual(new Vector3(2, 3, 0), audio.Positions[0]);
                Assert.AreEqual(4, audio.Positions.Count);
                var ui = owner.AddComponent<UiAudioPresenter>();
                ui.Configure(cue, cue);
                ui.BindAudio(audio);
                ui.PresentSelection();
                ui.PresentConfirmation();
                Assert.AreEqual(2, audio.UiCount);
                Assert.AreEqual(4, audio.Positions.Count);
                ui.enabled = false;
                ui.PresentSelection();
                Assert.AreEqual(2, audio.UiCount);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(cue); }
        }

        [Test]
        public void ServiceFacade_ExposesFocusedAudioContracts()
        {
            Assert.NotNull(typeof(IGameplayServices).GetProperty("Audio"));
            Assert.NotNull(typeof(IGameplayServices).GetProperty("AudioSettings"));
        }
    }
}

