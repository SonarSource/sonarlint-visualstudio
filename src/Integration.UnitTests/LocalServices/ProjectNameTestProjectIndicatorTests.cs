using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectNameTestProjectIndicatorTests
    {
        private Mock<ILogger> logger;
        private Project project;
        private ProjectNameTestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInit()
        {
            project = Mock.Of<Project>();
            project.Name = "a.test.b";

            logger = new Mock<ILogger>();
            testSubject = new ProjectNameTestProjectIndicator(logger.Object);
        }

        [TestMethod]
        public void SetTestRegex_Null_DefaultRegexUsed()
        {
            testSubject.SetTestRegex(null);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void SetTestRegex_InvalidRegex_DefaultRegexUsed()
        {
            testSubject.SetTestRegex(null);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void SetTestRegex_ValidRegex_NewRegexUsed()
        {
            testSubject.SetTestRegex("^b");

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow("", false)]
        [DataRow("test\\", false)]
        [DataRow("\\asd\\.!@#$%sdftest234234@~!sdasd\\", false)]
        [DataRow(".!@#$%sdftest", true)]
        [DataRow(".!@#$%sdftest234234@~!sdasd", true)]
        [DataRow("\\asd\\.!@#$%sdftest234234@~!sdasd", true)]
        [DataRow("test", true)]
        [DataRow("thetestisalie", true)]
        public void IsTestProject_RegexTests(string projectName, bool shouldMatch)
        {
            project.Name = projectName;

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(shouldMatch);
        }
    }
}
