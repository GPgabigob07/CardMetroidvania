using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardTimeChordRuntimeTests
    {
        [Test]
        public void SecondButtonWithinGrace_TriggersChord()
        {
            var chord = new PlayerCardTimeChordRuntime(graceDuration: 0.2f);

            Assert.False(chord.Tick(
                unscaledDeltaTime: 0f,
                leftPressed: true,
                leftHeld: true,
                rightPressed: false,
                rightHeld: false));

            Assert.True(chord.Tick(
                unscaledDeltaTime: 0.15f,
                leftPressed: false,
                leftHeld: false,
                rightPressed: true,
                rightHeld: true));
        }

        [Test]
        public void SecondButtonAfterGrace_DoesNotTriggerChord()
        {
            var chord = new PlayerCardTimeChordRuntime(graceDuration: 0.2f);
            chord.Tick(
                unscaledDeltaTime: 0f,
                leftPressed: true,
                leftHeld: true,
                rightPressed: false,
                rightHeld: false);

            Assert.False(chord.Tick(
                unscaledDeltaTime: 0.21f,
                leftPressed: false,
                leftHeld: false,
                rightPressed: true,
                rightHeld: true));
        }

        [Test]
        public void HeldFirstButton_TriggersWhenSecondIsPressed()
        {
            var chord = new PlayerCardTimeChordRuntime(graceDuration: 0.2f);

            chord.Tick(
                unscaledDeltaTime: 0.3f,
                leftPressed: true,
                leftHeld: true,
                rightPressed: false,
                rightHeld: false);

            Assert.True(chord.Tick(
                unscaledDeltaTime: 0.3f,
                leftPressed: false,
                leftHeld: true,
                rightPressed: true,
                rightHeld: true));
        }

        [Test]
        public void ChordDoesNotRepeatUntilBothButtonsRelease()
        {
            var chord = new PlayerCardTimeChordRuntime(graceDuration: 0.2f);
            chord.Tick(0f, true, true, false, false);
            Assert.True(chord.Tick(0.05f, false, true, true, true));
            Assert.False(chord.Tick(0.05f, false, true, false, true));

            chord.Tick(0.05f, false, false, false, false);
            chord.Tick(0f, true, true, false, false);

            Assert.True(chord.Tick(0.05f, false, true, true, true));
        }
    }
}
