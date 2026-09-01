using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class BatShotPredictorTests
    {
        private BatShotPredictor predictor;

        [SetUp]
        public void SetUp()
        {
            predictor = new BatShotPredictor();
            predictor.Configure(windupDuration: 0.2f, leadTime: 0.2f, maximumLeadDistance: 1.5f);
        }

        [Test]
        public void Predict_SampledRightwardMotion_LeadsAimRightwardWithinConfiguredLimit()
        {
            predictor.BeginSample(Vector2.zero);
            predictor.Sample(new Vector2(1f, 0f), 0.1f);

            var prediction = predictor.LockPrediction();

            Assert.Greater(prediction.x, 1f);
            Assert.LessOrEqual(prediction.x, 2.5f);
        }

        [Test]
        public void LockPrediction_FurtherSamplesDoNotMoveTheLockedPoint()
        {
            predictor.BeginSample(Vector2.zero);
            predictor.Sample(new Vector2(1f, 0f), 0.1f);
            var lockedPoint = predictor.LockPrediction();

            predictor.Sample(new Vector2(20f, 0f), 0.1f);

            Assert.AreEqual(lockedPoint, predictor.LockPrediction());
        }

        [Test]
        public void LockPrediction_ExcessiveVelocity_ClampsLeadMagnitude()
        {
            predictor.BeginSample(Vector2.zero);
            predictor.Sample(new Vector2(10f, 0f), 0.1f);

            var prediction = predictor.LockPrediction();

            Assert.AreEqual(11.5f, prediction.x, 0.001f);
        }
    }
}
