using System.Collections.Immutable;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SuppressionExecutionContextTests
    {
        [TestMethod]
        [DataRow(@"C:\project\.sonarlint\projectKey1\CSharp\SonarLint.xml", "projectKey1")]
        [DataRow(@"C:\project\.sonarlint\projectKey2\VB\SonarLint.xml", "projectKey2")]
        public void SonarProjectKey_PathIsValid_ReturnsKey(string path, string projectKey)
        {
            var additionalText = new ConcreteAdditionalText(path);

            var testSubject = CreateTestSubject(additionalText);

            testSubject.SonarProjectKey.Should().Be(projectKey);
            testSubject.IsInConnectedMode.Should().BeTrue();
        }

        [TestMethod]
        public void SonarProjectKey_PathIsNotValid_ReturnsNull()
        {
            var additionalText = new ConcreteAdditionalText(@"C:\project\projectKey1\CSharp\SonarLint.xml");

            var testSubject = CreateTestSubject(additionalText);

            testSubject.SonarProjectKey.Should().BeNull();
            testSubject.IsInConnectedMode.Should().BeFalse();
        }

        [TestMethod]
        public void SonarProjectKey_HasMixedPaths_ReturnsKey()
        {
            var additionalText1 = new ConcreteAdditionalText(@"C:\project\projectKey1\CSharp\SonarLint.xml");
            var additionalText2 = new ConcreteAdditionalText(@"C:\project\.sonarlint\projectKey2\CSharp\SonarLint.xml");            

            var testSubject = CreateTestSubject(additionalText1, additionalText2);

            testSubject.SonarProjectKey.Should().Be("projectKey2");
            testSubject.IsInConnectedMode.Should().BeTrue();
        }

        [TestMethod]
        public void SonarProjectKey_HasNoPaths_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            testSubject.SonarProjectKey.Should().BeNull();
            testSubject.IsInConnectedMode.Should().BeFalse();
        }

        private SuppressionExecutionContext CreateTestSubject(params AdditionalText[] existingAdditionalFiles)
        {
            var analyzerOptions = CreateAnalyzerOptions(existingAdditionalFiles);
            return new SuppressionExecutionContext(analyzerOptions);
        }

        private AnalyzerOptions CreateAnalyzerOptions(params AdditionalText[] existingAdditionalFiles)
        {
            return new AnalyzerOptions(existingAdditionalFiles.ToImmutableArray());
        }

    }

    internal class ConcreteAdditionalText : AdditionalText
    {
        private readonly string text;

        internal ConcreteAdditionalText(string path)
        {
            Path = path;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(string.Empty);
        }
    }
}
