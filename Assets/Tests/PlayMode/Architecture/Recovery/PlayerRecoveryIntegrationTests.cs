using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerRecoveryIntegrationTests
    {
        [UnityTest]
        public IEnumerator RecoverySurvivesPauseRespawnAndResetsOnNewRun()
        {
            yield return SceneManager.LoadSceneAsync(PlaytestSessionController.TitleScene);
            yield return null;
            var session = PlaytestSessionController.Instance;
            var start = session.StartPlaytestAsync(); yield return Await(start);
            Assert.That(start.Result, Is.True, session.LastError);
            yield return null;
            var player = session.Gameplay.Player;
            var wallet = player.GetComponent<PlayerResourceWallet>();
            var health = player.GetComponent<SimpleHealth>();
            var inventory = player.GetComponent<PlayerCardInventoryRuntime>();
            var recovery = player.GetComponent<PlayerRecoveryController>();
            var snapshotSource = player.GetComponent<PlayerCardCommitSnapshotSource>();
            var energy = snapshotSource.Capture(PlayerCardTimeState.Neutral, null, false).Resources[0].Resource;
            var body = player.GetComponent<Rigidbody2D>(); body.simulated = false;
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.NeutralCostBasis, Is.GreaterThan(0));
            wallet.TrySpend(new[] { new ResourceAmount(energy, wallet.GetCurrent(energy)) });
            var pause = session.GetComponent<PlaytestPauseController>();
            Assert.That(pause.TryPause(), Is.True);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(wallet.GetCurrent(energy), Is.Zero);
            pause.Resume();
            player.SetWorldHeld(true);
            yield return new WaitForSeconds(.2f);
            Assert.That(wallet.GetCurrent(energy), Is.Zero);
            player.SetWorldHeld(false);
            yield return new WaitForSeconds(3.3f);
            Assert.That(wallet.GetCurrent(energy), Is.GreaterThan(0));
            recovery.Tick(100, true);
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(recovery.PassiveEnergyCeiling).Within(.01));

            var hud = Object.FindFirstObjectByType<PlayerHudUI>();
            Assert.That(hud.transform.Find("Top Left").localScale, Is.EqualTo(Vector3.one * 2));
            Assert.That(hud.transform.Find("Card Time").localScale, Is.EqualTo(Vector3.one * 2));
            health.TrySpendNonlethal(3);
            wallet.TrySpend(new[] { new ResourceAmount(energy, wallet.GetCurrent(energy)) });
            var cards = inventory.GetEquippedCards(PlayerCardTimeState.Neutral);
            var mend = cards.First(card => card.Id == "mend");
            var catalog = ScriptableObject.CreateInstance<CardCatalogSO>(); catalog.Configure(cards);
            var cardTime = session.Services.GetComponent<CardTimeSessionController>();
            var source = cardTime.RegisterPlayerSource(player);
            source.PublishAvailability(new CardTimeOpportunity(PlayerCardTimeState.Neutral, 900));
            Assert.That(source.RequestActivation(), Is.EqualTo(CardTimeActivationRequestResult.Activated), "Zero energy must allow utility cards");
            Assert.That(CardTimeSelectionTransaction.TryCreate(PlayerCardTimeState.Neutral, cardTime.Current.ActiveSessionId,
                inventory.GetEquippedCardIds(PlayerCardTimeState.Neutral), catalog, out var selection), Is.True);
            selection.SelectIndex(cards.ToList().IndexOf(mend));
            var prepared = player.GetComponent<PlayerCardRuntime>().TryPrepare(mend, selection,
                snapshotSource.Capture(PlayerCardTimeState.Neutral, null, false));
            Assert.That(prepared.Succeeded, Is.True, prepared.Failure.ToString());
            Assert.That(source.TryCommit(prepared.Commit), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(4));
            Assert.That(inventory.GetCount("mend"), Is.EqualTo(1));
            Assert.That(wallet.GetCurrent(energy), Is.Zero);

            yield return Capture(hud.GetComponent<Canvas>(), "hud-2x");
            var selectionHud = Object.FindFirstObjectByType<CardTimeSelectionHudUI>();
            player.SetWorldHeld(true);
            selectionHud.BindSelection(selection);
            yield return Capture(hud.GetComponent<Canvas>(), "recovery-cards");
            selectionHud.ClearSelection();
            player.SetWorldHeld(false);
            var areaLoad = SceneStreamingService.RequestAsync("Assets/Scenes/PinkArea_Perimeters.unity", true);
            yield return Await(areaLoad);
            Assert.That(inventory.GetCount("mend"), Is.EqualTo(1));
            var coordinator = Object.FindFirstObjectByType<GameplayAreaCoordinator>();
            health.ApplyDamage(new DamageContext(player.gameObject, player.gameObject, null, 100, Vector2.zero, Vector2.zero));
            var respawn = coordinator.RespawnAsync(); yield return Await(respawn);
            Assert.That(respawn.Result, Is.True, coordinator.LastError);
            Assert.That(inventory.GetCount("mend"), Is.EqualTo(1));
            var back = session.ReturnToTitleAsync(); yield return Await(back);
            start = session.StartPlaytestAsync(); yield return Await(start);
            Assert.That(session.Gameplay.Player.GetComponent<PlayerCardInventoryRuntime>().GetCount("mend"), Is.EqualTo(2));
            back = session.ReturnToTitleAsync(); yield return Await(back);
            Object.Destroy(catalog);
        }

        private static IEnumerator Await(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 30;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(task.IsCompleted, Is.True, "Operation timed out");
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
        }
        private static IEnumerator Capture(Canvas canvas, string name)
        {
            yield return null;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var camera = Object.FindFirstObjectByType<Camera>();
            var target = new RenderTexture(1280, 720, 24);
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(".utmp/recovery/" + name + ".png"), image.EncodeToPNG());
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            Object.Destroy(image); Object.Destroy(target);
        }
    }
}
