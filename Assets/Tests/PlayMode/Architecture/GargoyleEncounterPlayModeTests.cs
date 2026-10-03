using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleEncounterPlayModeTests
    {
        private GargoyleBrain brain;
        private GameObject player;
        [UnitySetUp] public IEnumerator LoadArena()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Test_GargoyleSentinel.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Authoring acceptance fixture runs in the Unity Editor.");
#endif
            yield return null;
            brain = Object.FindFirstObjectByType<GargoyleBrain>(); player = Object.FindFirstObjectByType<PlayerController>().gameObject;
            player.GetComponent<Rigidbody2D>().position = new Vector2(1.5f, 1.1f); Physics2D.SyncTransforms();
            var deadline = Time.time + 3;
            while (!brain.CanEmit && Time.time < deadline) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            var empty = SceneManager.CreateScene("Gargoyle fixture cleanup"); SceneManager.SetActiveScene(empty);
            var old = brain != null ? brain.gameObject.scene : default;
            if (old.IsValid()) yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator ActualUpdateAndFixedUpdateStartAttackAndConsumeActiveSample()
        {
            Assert.That(brain.IsInitialized, Is.True);
            var deadline = Time.time + 5;
            while (brain.CurrentAttack.Phase != EnemyAttackPhase.Active && Time.time < deadline) yield return null;
            Assert.That(brain.CurrentAttack.Phase, Is.EqualTo(EnemyAttackPhase.Active));
            var token = brain.CurrentAttack.ExecutionToken;
            var step = brain.CurrentAttack.StepId;
            deadline = Time.time + 3;
            while (brain.CurrentAttack.StepId == step && brain.CurrentAttack.Phase == EnemyAttackPhase.Active && Time.time < deadline) yield return null;
            Assert.That(brain.CurrentAttack.ExecutionToken, Is.EqualTo(token));
            Assert.That(brain.CurrentAttack.Phase, Is.Not.EqualTo(EnemyAttackPhase.Active));
        }
        [UnityTest] public IEnumerator PauseAndWorldHoldFreezeAttackAndWardLifetime()
        {
            yield return null;
            var ward = player.GetComponent<PlayerWardRuntime>(); Assert.That(ward.Arm(1), Is.True);
            var elapsed = ward.Elapsed; Time.timeScale = 0;
            for (var frame = 0; frame < 5; frame++) yield return null;
            Assert.That(ward.Elapsed, Is.EqualTo(elapsed));
            Time.timeScale = 1;
            var hold = player.GetComponent<PlayerWorldHold>() ?? player.AddComponent<PlayerWorldHold>();
            using (hold.Acquire())
            {
                var frozen = ward.Elapsed;
                for (var frame = 0; frame < 5; frame++) yield return null;
                Assert.That(ward.Elapsed, Is.EqualTo(frozen)); Assert.That(brain.CanEmit, Is.False);
            }
        }
        [UnityTest] public IEnumerator DefeatCancelsOwnedShotsAndDisablesRegions()
        {
            var launcher = brain.GetComponent<EnemyProjectilePatternLauncher>();
#if UNITY_EDITOR
            var definition = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinitionSO>("Assets/Data/Enemies/GargoyleSentinel/Attack_Volley.asset");
            launcher.Release(definition, 5, 999, brain.transform.position, Vector2.right);
            Assert.That(launcher.OwnedProjectiles.Count, Is.EqualTo(5));
#endif
            brain.GetComponent<EnemyActor>().Health.ApplyDamage(new DamageContext(player, brain.gameObject, null, 1000, Vector2.zero, Vector2.right));
            yield return null;
            Assert.That(brain.CurrentState, Is.EqualTo(GargoyleState.Dead)); Assert.That(launcher.OwnedProjectiles, Is.Empty);
            Assert.That(brain.GetComponentsInChildren<GargoyleHurtbox>().All(h => !h.GetComponent<BoxCollider2D>().enabled), Is.True);
        }
        [UnityTest] public IEnumerator SceneUnloadClearsWardAndOwnedOffense()
        {
            var ward = player.GetComponent<PlayerWardRuntime>(); ward.Arm(1);
            var scene = brain.gameObject.scene;
            var empty = SceneManager.CreateScene("Transition destination"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(ward == null, Is.True); Assert.That(brain == null, Is.True);
            Assert.That(Object.FindObjectsByType<EnemyProjectile2D>(FindObjectsSortMode.None), Is.Empty);
        }
        [UnityTest] public IEnumerator CombatDoesNotMutateAuthoredTuningOrAttackAssets()
        {
#if UNITY_EDITOR
            var assets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data/Enemies/GargoyleSentinel" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();
            var before = assets.Select(JsonUtility.ToJson).ToArray();
            for (var frame = 0; frame < 90; frame++) yield return null;
            for (var i = 0; i < assets.Length; i++) Assert.That(JsonUtility.ToJson(assets[i]), Is.EqualTo(before[i]), assets[i].name);
#else
            yield return null;
#endif
        }
        [UnityTest] public IEnumerator ArenaResetRestoresBothActorsAndClearsTransientOffense()
        {
            var actor = brain.GetComponent<EnemyActor>();
            actor.Health.ApplyDamage(new DamageContext(player, actor.gameObject, null, 1000, Vector2.zero, Vector2.right));
            var ward = player.GetComponent<PlayerWardRuntime>(); ward.Arm(1);
            Object.FindFirstObjectByType<GargoyleDebugPresenter>().ResetArena();
            yield return null;
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(actor.Health.MaximumHealth));
            Assert.That(brain.CurrentState, Is.Not.EqualTo(GargoyleState.Dead)); Assert.That(ward.IsActive, Is.False);
            Assert.That(player.GetComponent<SimpleHealth>().CurrentHealth, Is.EqualTo(player.GetComponent<SimpleHealth>().MaximumHealth));
        }
    }
}
