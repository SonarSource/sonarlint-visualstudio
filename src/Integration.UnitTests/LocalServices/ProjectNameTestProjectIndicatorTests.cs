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
        [DataRow("a.tes", false)]
        [DataRow("a.testinfra", true)]
        [DataRow("a.testsinfra", true)]
        [DataRow("a.test", true)]
        [DataRow("a.tests", true)]
        [DataRow("a.test.infra", true)]
        [DataRow("a.tests.infra", true)]
        [DataRow("tests.a", true)]
        [DataRow("test.a", true)]
        public void IsTestProject_RegexTests(string projectName, bool shouldMatch)
        {
            project.Name = projectName;

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(shouldMatch);
        }
    }
}
