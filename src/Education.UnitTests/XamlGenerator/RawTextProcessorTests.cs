using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class RawTextProcessorTests
    {
        [TestMethod]
        public void ProcessRawText_OneCrossReference_WithSurroundingText_ReturnsListWithCorrectItems()
        {
            string text = " {rule:javascript:S1656} - Implements a check on";

            var list = RawTextProcessor.ProcessRawText(text);

            list.Count.Should().Be(3);

            var firstElement = list[0];
            firstElement.Text.Should().Be(" ");
            firstElement.IsCrossReference.Should().BeFalse();

            var secondElement = list[1];
            secondElement.Text.Should().Be("{rule:javascript:S1656}");
            secondElement.IsCrossReference.Should().BeTrue();

            var lastElement = list[2];
            lastElement.Text.Should().Be(" - Implements a check on");
            lastElement.IsCrossReference.Should().BeFalse();
        }

        [TestMethod]
        public void ProcessRawText_OneCrossReference_WithoutSurroundingText_ReturnsListWithCorrectItems()
        {
            string text = "{rule:cpp:S165}";

            var list = RawTextProcessor.ProcessRawText(text);

            list.Count.Should().Be(1);

            var firstElement = list[0];
            firstElement.Text.Should().Be("{rule:cpp:S165}");
            firstElement.IsCrossReference.Should().BeTrue();
        }

        [TestMethod]
        public void ProcessRawText_TwoCrossReferences_WithSurroundingText_ReturnsListWithCorrectItems()
        {
            string text = "also found {rule:cpp:S165} hello this is a test {rule:c:S1111}";

            var list = RawTextProcessor.ProcessRawText(text);

            list.Count.Should().Be(4);

            var firstElement = list[0];
            firstElement.Text.Should().Be("also found ");
            firstElement.IsCrossReference.Should().BeFalse();

            var secondElement = list[1];
            secondElement.Text.Should().Be("{rule:cpp:S165}");
            secondElement.IsCrossReference.Should().BeTrue();

            var thirdElement = list[2];
            thirdElement.Text.Should().Be(" hello this is a test ");
            thirdElement.IsCrossReference.Should().BeFalse();

            var lastElement = list[3];
            lastElement.Text.Should().Be("{rule:c:S1111}");
            lastElement.IsCrossReference.Should().BeTrue();
        }

        [TestMethod]
        public void ProcessRawText_NoCrossReferences_ReturnsListWithOneItem()
        {
            string text = "this is a text without any cross references";

            var list = RawTextProcessor.ProcessRawText(text);

            list.Count.Should().Be(1);

            var firstElement = list[0];
            firstElement.Text.Should().Be(text);
            firstElement.IsCrossReference.Should().BeFalse();
        }

        [TestMethod]
        public void ProcessRawText_EmptyString_ReturnsEmptyList()
        {
            string text = "";

            var list = RawTextProcessor.ProcessRawText(text);

            list.Count.Should().Be(0);
        }
    }
}
