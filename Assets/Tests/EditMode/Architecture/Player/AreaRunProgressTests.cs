using System;
using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class AreaRunProgressTests
    {
        [Test]
        public void GateKeysAreScopedToTheirAreaAndDoNotSurviveANewRun()
        {
            var state = new RunProgress(new SpawnAddress("blue", "start"));

            state.OpenGate("blue", "seal-1");

            Assert.IsTrue(state.IsGateOpen("blue", "seal-1"));
            Assert.IsFalse(state.IsGateOpen("pink", "seal-1"));

            var nextRun = new RunProgress(new SpawnAddress("blue", "start"));
            Assert.IsFalse(nextRun.IsGateOpen("blue", "seal-1"));
        }

        [Test]
        public void DuplicateGateWritesAreIdempotent()
        {
            var state = new RunProgress(new SpawnAddress("blue", "start"));

            state.OpenGate("blue", "seal-1");
            state.OpenGate("blue", "seal-1");

            Assert.IsTrue(state.IsGateOpen("blue", "seal-1"));
        }

        [Test]
        public void GuideDiscoveryIsIdempotent()
        {
            var state = new RunProgress(new SpawnAddress("blue", "start"));

            Assert.IsFalse(state.GuideDiscovered);
            state.DiscoverGuide();
            state.DiscoverGuide();

            Assert.IsTrue(state.GuideDiscovered);
        }

        [Test]
        public void RespawnStartsAtTheInitialAddressAndCanBeUpdated()
        {
            var initial = new SpawnAddress("blue", "start");
            var state = new RunProgress(initial);

            Assert.AreEqual(initial, state.Respawn);

            var next = new SpawnAddress("pink", "checkpoint");
            state.SetRespawn(next);

            Assert.AreEqual(next, state.Respawn);
        }

        [Test]
        public void SpawnAddressAndGateOperationsRejectEmptyIdentifiers()
        {
            Assert.Throws<ArgumentException>(() => new SpawnAddress("", "start"));
            Assert.Throws<ArgumentException>(() => new SpawnAddress("blue", " "));
            Assert.Throws<ArgumentException>(() => new RunProgress(default));

            var state = new RunProgress(new SpawnAddress("blue", "start"));
            Assert.Throws<ArgumentException>(() => state.OpenGate(" ", "seal-1"));
            Assert.Throws<ArgumentException>(() => state.OpenGate("blue", "\t"));
            Assert.Throws<ArgumentException>(() => state.IsGateOpen("", "seal-1"));
            Assert.Throws<ArgumentException>(() => state.IsGateOpen("blue", " "));
            Assert.Throws<ArgumentException>(() => state.SetRespawn(default));
        }
    }
}
