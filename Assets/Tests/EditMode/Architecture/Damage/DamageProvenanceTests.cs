using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class DamageProvenanceTests
    {
        [Test]
        public void Converted_RetainsOriginalInstanceAsRootAndMarksConvertedOrigin()
        {
            var provenance = DamageProvenance.Converted("projectile-1", "projectile-1", "card.deflect");

            Assert.AreEqual(DamageOriginKind.Converted, provenance.OriginKind);
            Assert.AreEqual("projectile-1", provenance.RootInstanceId);
        }
    }
}
