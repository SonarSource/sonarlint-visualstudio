using System;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectCapabilityTestProjectIndicatorTests
    {
        private Mock<IVsHierarchy> vsHierarchy;
        private Mock<IProjectSystemHelper> projectSystemHelper;
        private ProjectCapabilityTestProjectIndicator testSubject;

        private Project project;

        [TestInitialize]
        public void TestInit()
        {
            projectSystemHelper = new Mock<IProjectSystemHelper>();
            vsHierarchy = new Mock<IVsHierarchy>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IProjectSystemHelper))).Returns(projectSystemHelper.Object);

            project = new ProjectMock("proj.csproj");
            projectSystemHelper.Setup(x => x.GetIVsHierarchy(project)).Returns(vsHierarchy.Object);

            testSubject = new ProjectCapabilityTestProjectIndicator(serviceProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new ProjectCapabilityTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void IsTestProject_NotTestCapability_False()
        {
            // vsHierarchy.Setup(x => x.IsCapabilityMatch("TestContainer")).Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void IsTestProject_IsTestCapability_False()
        {
            // vsHierarchy.Setup(x => x.IsCapabilityMatch("TestContainer")).Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }
    }
}
