using System;
using System.Collections.Generic;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectKindTestProjectIndicatorTests
    {
        private Mock<IVsHierarchy> vsHierarchy;
        private Mock<IProjectSystemHelper> projectSystemHelper;
        private ProjectKindTestProjectIndicator testSubject;

        private Project project;
        private IList<Guid> projectKinds;

        [TestInitialize]
        public void TestInit()
        {
            projectSystemHelper = new Mock<IProjectSystemHelper>();
            vsHierarchy = new Mock<IVsHierarchy>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IProjectSystemHelper))).Returns(projectSystemHelper.Object);

            project = Mock.Of<Project>();
            projectKinds = new List<Guid>();
            projectSystemHelper.Setup(x=> x.GetIVsHierarchy(project)).Returns(vsHierarchy.Object);
            projectSystemHelper.Setup(x => x.GetAggregateProjectKinds(vsHierarchy.Object)).Returns(projectKinds);

            testSubject = new ProjectKindTestProjectIndicator(serviceProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new BuildPropertyTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }


        [TestMethod]
        public void IsTestProject_ProjectGuidIsMsTestGuid_True()
        {
            projectKinds.Add(ProjectSystemHelper.TestProjectKindGuid);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ProjectGuidIsExternalTestGuid_True()
        {
            projectKinds.Add(ProjectSystemHelper.ExternalTestProjectKindGuid);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_ProjectGuidIsNotTestGuid_False()
        {
            projectKinds.Add(new Guid(ProjectSystemHelper.CSharpProjectKind));

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }
    }
}
