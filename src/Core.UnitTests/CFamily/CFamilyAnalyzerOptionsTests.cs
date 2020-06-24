using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyAnalyzerOptionsTests
    {
        [TestMethod]
        [DataRow(-1, CFamilyAnalyzerOptions.DefaultAnalysisTimeoutMs)]
        [DataRow(0, CFamilyAnalyzerOptions.DefaultAnalysisTimeoutMs)]
        [DataRow(1, 1)]
        [DataRow(999, 999)]
        public void AnalysisTimeout(int envSettingsResponse, int expectedTimeout)
        {
            var envSettingsMock = new Mock<IEnvironmentSettings>();
            envSettingsMock.Setup(x => x.CFamilyAnalysisTimeoutInMs()).Returns(envSettingsResponse);

            new CFamilyAnalyzerOptions(envSettingsMock.Object).AnalysisTimeoutInMiliseconds.Should().Be(expectedTimeout);
        }

        [TestMethod]
        public void AnalysisTimeoutInMiliseconds_NoEnvironmentSettings_DefaultTimeout()
        {
            new CFamilyAnalyzerOptions().AnalysisTimeoutInMiliseconds.Should().Be(CFamilyAnalyzerOptions.DefaultAnalysisTimeoutMs);
        }
    }
}
