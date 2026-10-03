using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyAttackRunnerTests
    {
        private EnemyAttackDefinitionSO definition;
        private DamageProfileSO profile;
        private EnemyAttackStep first;
        private EnemyAttackRunner runner;
        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>();
            profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            first = new EnemyAttackStep("sweep", .35f, .12f, .18f, profile);
            definition.SetSteps(new[] { first });
            runner = new EnemyAttackRunner();
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(profile); }

        [Test]
        public void BeginDuringExecution_RejectsWithoutRestartingElapsedTime()
        {
            Assert.IsTrue(runner.Begin(definition, 1)); runner.Tick(.2f);
            Assert.IsFalse(runner.Begin(definition, 2));
            Assert.AreEqual(.2f, runner.Current.Elapsed, .001f);
            Assert.AreEqual(1, runner.Current.ExecutionToken);
        }

        [Test]
        public void LargeDelta_CarriesSurplus_ButCannotSkipDamagingPhysicsSample()
        {
            definition.SetSteps(new[] { first, new EnemyAttackStep("slam", .55f, .15f, .45f, profile) });
            runner.Begin(definition, 1); runner.Tick(10);
            Assert.AreEqual("sweep", runner.Current.StepId);
            Assert.AreEqual(EnemyAttackPhase.Active, runner.Current.Phase);
            runner.Tick(0);
            Assert.AreEqual("sweep", runner.Current.StepId);
            runner.ConfirmPhysicsSample(1, "sweep"); runner.Tick(0);
            Assert.AreEqual("slam", runner.Current.StepId);
            Assert.AreEqual(EnemyAttackPhase.Active, runner.Current.Phase);
            runner.ConfirmPhysicsSample(1, "slam"); runner.Tick(0);
            Assert.AreEqual(EnemyAttackPhase.Completed, runner.Current.Phase);
        }

        [Test]
        public void Release_IsConsumedOnce_AndStaleTokensCannotAcknowledgeIt()
        {
            runner.Begin(definition, 7); runner.Tick(.35f);
            Assert.IsFalse(runner.TryConsumeRelease(6, "sweep"));
            Assert.IsFalse(runner.TryConsumeRelease(7, "other"));
            Assert.IsTrue(runner.Current.ReleaseDue);
            Assert.IsTrue(runner.TryConsumeRelease(7, "sweep"));
            Assert.IsFalse(runner.TryConsumeRelease(7, "sweep"));
        }

        [Test]
        public void Cancel_IsIdempotent_AndOldPhysicsCallbacksCannotAdvanceTheNextExecution()
        {
            runner.Begin(definition, 7); runner.Tick(.35f); runner.Cancel(); runner.Cancel();
            Assert.IsFalse(runner.Current.IsRunning);
            Assert.IsFalse(runner.TryConsumeRelease(7, "sweep"));
            Assert.IsFalse(runner.Begin(definition, 7));
            Assert.IsTrue(runner.Begin(definition, 8)); runner.Tick(10);
            runner.ConfirmPhysicsSample(7, "sweep"); runner.Tick(0);
            Assert.AreEqual(EnemyAttackPhase.Active, runner.Current.Phase);
        }

        [Test]
        public void DurationEdits_KeepElapsed_AndTransitionOnceAtTheNextTick()
        {
            runner.Begin(definition, 1); runner.Tick(.2f);
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":0.5}", first); runner.Tick(.1f);
            Assert.AreEqual(EnemyAttackPhase.Windup, runner.Current.Phase);
            Assert.AreEqual(.3f, runner.Current.Elapsed, .001f);
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":0.25}", first); runner.Tick(0);
            Assert.AreEqual(EnemyAttackPhase.Active, runner.Current.Phase);
            Assert.IsTrue(runner.TryConsumeRelease(1, "sweep"));
            runner.Tick(0); Assert.IsFalse(runner.TryConsumeRelease(1, "sweep"));
        }

        [Test]
        public void FinalAimWindow_LocksOnce_AndLaterAimUpdatesAreIgnored()
        {
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":0.8,\"payload\":{\"lockedAimDuration\":0.3}}", first);
            runner.Begin(definition, 1); runner.SetAim(Vector2.left); runner.Tick(.49f);
            Assert.IsFalse(runner.Current.IsAimLocked);
            runner.SetAim(Vector2.up); runner.Tick(.02f);
            Assert.IsTrue(runner.Current.IsAimLocked);
            runner.SetAim(Vector2.down); runner.Tick(.3f);
            Assert.AreEqual(Vector2.up, runner.Current.Aim);
        }

        [Test]
        public void InvalidLiveDuration_RetainsLastValidPhase_WithoutReplayingOrMutatingAsset()
        {
            runner.Begin(definition, 1); runner.Tick(.2f);
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":-1}", first);
            var authored = JsonUtility.ToJson(definition);
            LogAssert.Expect(LogType.Warning, "Invalid live enemy attack revision; retaining last valid execution data.");
            runner.Tick(.05f); runner.Tick(.05f);
            Assert.AreEqual(EnemyAttackPhase.Windup, runner.Current.Phase);
            Assert.AreEqual(.35f, runner.Current.PhaseDuration);
            Assert.AreEqual(authored, JsonUtility.ToJson(definition));
        }

        [Test]
        public void GraphReplacement_PreservesExistingStepIdentitiesUntilNextExecution()
        {
            runner.Begin(definition, 1);
            definition.SetSteps(new[] { new EnemyAttackStep("replacement", .5f, .2f, .3f, profile) });
            runner.Tick(.35f);
            Assert.AreEqual("sweep", runner.Current.StepId);
            runner.Cancel(); Assert.IsTrue(runner.Begin(definition, 2));
            Assert.AreEqual("replacement", runner.Current.StepId);
        }

        [Test]
        public void PayloadKindEdit_DoesNotTransformTheCommittedAttack()
        {
            runner.Begin(definition, 1);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":3}}", first); runner.Tick(.1f);
            Assert.AreEqual(EnemyAttackPayloadKind.Melee, runner.Current.Kind);
            Assert.AreEqual(EnemyAttackPayloadKind.Melee, runner.Current.Step.Payload.Kind);
        }

        [Test]
        public void VolleyPattern_SelectsItsAuthoredWindup_WithoutDefaultCountFallback()
        {
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":1,\"lockedAimDuration\":0.15}}", first);
            definition.SetPatterns(new[] { new EnemyProjectilePattern(3, .55f, new[] { -12f, 0, 12f }) });
            Assert.IsFalse(runner.Begin(definition, 1));
            Assert.IsTrue(runner.Begin(definition, 1, 3));
            Assert.AreEqual(.55f, runner.Current.PhaseDuration);
            runner.Tick(.5f); Assert.AreEqual(EnemyAttackPhase.Windup, runner.Current.Phase);
        }

        [Test]
        public void ZeroAndInvalidDelta_DoNotAdvanceGameplay()
        {
            runner.Begin(definition, 1); runner.Tick(0);
            Assert.AreEqual(0, runner.Current.Elapsed);
            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Tick(-1));
            Assert.AreEqual(EnemyAttackPhase.Windup, runner.Current.Phase);
        }
    }
}
