using System;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class TestProjectIndicatorTests
    {
        private Mock<ITestProjectIndicator> buildPropertyIndicator;
        private Mock<ITestProjectIndicator> projectKindIndicator;
        private Mock<ITestProjectIndicator> projectNameIndicator;
        private TestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInit()
        {
            buildPropertyIndicator = new Mock<ITestProjectIndicator>();
            projectKindIndicator = new Mock<ITestProjectIndicator>();
            projectNameIndicator = new Mock<ITestProjectIndicator>();

            testSubject = new TestProjectIndicator(buildPropertyIndicator.Object,
                projectKindIndicator.Object,
                projectNameIndicator.Object);
        }

        [TestMethod]
        public void Ctor_NullBuildPropertyIndicator_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(null, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buildPropertyIndicator");
        }

        [TestMethod]
        public void Ctor_NullProjectKindIndicator_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(buildPropertyIndicator.Object, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectKindIndicator");
        }

        [TestMethod]
        public void Ctor_NullProjectNameIndicator_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(buildPropertyIndicator.Object, projectKindIndicator.Object, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectNameIndicator");
        }

        [TestMethod]
        public void IsTestProject_CheckPrecedenceOrder_BuildPropertyIndicatorIsTrue_True()
        {
            var project = Mock.Of<Project>();

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();

            projectKindIndicator.VerifyNoOtherCalls();
            projectNameIndicator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsTestProject_CheckPrecedenceOrder_BuildPropertyIndicatorIsFalse_False()
        {
            var project = Mock.Of<Project>();

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();

            projectKindIndicator.VerifyNoOtherCalls();
            projectNameIndicator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsTestProject_BuildPropertyIsNotSet_ProjectKindIsTrue_True()
        {
            var project = Mock.Of<Project>();

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns((bool?)null);
            projectKindIndicator.Setup(x => x.IsTestProject(project)).Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_BuildPropertyIsNotSet_ProjectKindIsFalse_ProjectNameIsTrue_True()
        {
            var project = Mock.Of<Project>();

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns((bool?)null);
            projectKindIndicator.Setup(x => x.IsTestProject(project)).Returns(false);
            projectNameIndicator.Setup(x => x.IsTestProject(project)).Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_BuildPropertyIsNotSet_ProjectKindIsFalse_ProjectNameIsFalse_False()
        {
            var project = Mock.Of<Project>();

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns((bool?)null);
            projectKindIndicator.Setup(x => x.IsTestProject(project)).Returns(false);
            projectNameIndicator.Setup(x => x.IsTestProject(project)).Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }
    }
}
