using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoylePresentationTests
    {
        private GargoylePresentationSO presentation;

        [SetUp]
        public void SetUp() => presentation = ScriptableObject.CreateInstance<GargoylePresentationSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(presentation);

        [Test]
        public void Defaults_PreserveApprovedPixelAndWorldScaleContract()
        {
            Assert.AreEqual(new Vector2Int(200, 200), presentation.FrameSize);
            Assert.AreEqual(new Vector2Int(128, 128), presentation.OrdinaryPoseEnvelope);
            Assert.AreEqual(new Vector2(100, 36), presentation.PixelPivot);
            Assert.AreEqual(new Vector2(.5f, .18f), presentation.NormalizedPivot);
            Assert.AreEqual(48, presentation.PixelsPerUnit);
            Assert.AreEqual(1.5f, presentation.MinimumBodyAreaRatio);
            Assert.AreEqual(new Vector2(1.8f, 2.1f), presentation.BodySize);
            Assert.IsEmpty(presentation.GetValidationErrors());
        }

        [TestCase("{\"pixelsPerUnit\":0}")]
        [TestCase("{\"frameSize\":{\"x\":0,\"y\":200}}")]
        [TestCase("{\"ordinaryPoseEnvelope\":{\"x\":201,\"y\":128}}")]
        [TestCase("{\"pixelPivot\":{\"x\":100,\"y\":201}}")]
        [TestCase("{\"bodySize\":{\"x\":-1,\"y\":2.1}}")]
        [TestCase("{\"minimumBodyAreaRatio\":0}")]
        public void InvalidPresentationValues_AreRejected(string edit)
        {
            JsonUtility.FromJsonOverwrite(edit, presentation);
            Assert.IsNotEmpty(presentation.GetValidationErrors());
        }

        [Test]
        public void ReadingGeometry_ObservesTheEditedAsset_WithoutMutatingIt()
        {
            JsonUtility.FromJsonOverwrite("{\"bodySize\":{\"x\":2,\"y\":3}}", presentation);
            var authored = JsonUtility.ToJson(presentation);
            Assert.AreEqual(new Vector2(2, 3), presentation.BodySize);
            Assert.IsEmpty(presentation.GetValidationErrors());
            Assert.AreEqual(authored, JsonUtility.ToJson(presentation));
        }
    }
}
