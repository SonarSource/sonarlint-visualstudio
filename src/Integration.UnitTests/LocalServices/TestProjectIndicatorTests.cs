using System;
using System.Collections.Generic;
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
        private IList<ITestProjectIndicator> otherIndicators;
        private TestProjectIndicator testSubject;
        private Project project;
        private Mock<ITestProjectIndicator> firstOtherIndicator;
        private Mock<ITestProjectIndicator> secondOtherIndicator;

        [TestInitialize]
        public void TestInit()
        {
            project = Mock.Of<Project>();

            buildPropertyIndicator = new Mock<ITestProjectIndicator>();
            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns((bool?) null);

            firstOtherIndicator = new Mock<ITestProjectIndicator>();
            secondOtherIndicator = new Mock<ITestProjectIndicator>();
            otherIndicators = new List<ITestProjectIndicator>();
            
            testSubject = new TestProjectIndicator(buildPropertyIndicator.Object, otherIndicators);
        }

        [TestMethod]
        public void Ctor_NullBuildPropertyIndicator_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(null, otherIndicators);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buildPropertyIndicator");
        }

        [TestMethod]
        public void Ctor_NullIndicatorsCollection_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(buildPropertyIndicator.Object, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("testProjectIndicators");
        }

        [TestMethod]
        public void IsTestProject_CheckPrecedenceOrder_BuildPropertyIndicatorIsTrue_True()
        {
            otherIndicators.Add(firstOtherIndicator.Object);

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();

            firstOtherIndicator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsTestProject_CheckPrecedenceOrder_BuildPropertyIndicatorIsFalse_False()
        {
            otherIndicators.Add(firstOtherIndicator.Object);

            buildPropertyIndicator.Setup(x => x.IsTestProject(project)).Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();

            firstOtherIndicator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsTestProject_BuildPropertyIsNotSet_NoOtherIndicators_False()
        {
            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsTestProject_BuildPropertyIsNotSet_OneOtherIndicator_IndicatorCalled(bool indicatorResponse)
        {
            otherIndicators.Add(firstOtherIndicator.Object);

            firstOtherIndicator.Setup(x => x.IsTestProject(project)).Returns(indicatorResponse);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(indicatorResponse);
        }

        [DataTestMethod]
        [DataRow(true, true)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(false, false)]
        public void IsTestProject_BuildPropertyIsNotSet_TwoOtherIndicators_IndicatorsCalled(bool firstIndicatorResponse, bool secondIndicatorResponse)
        {
            otherIndicators.Add(firstOtherIndicator.Object);
            otherIndicators.Add(secondOtherIndicator.Object);

            firstOtherIndicator.Setup(x => x.IsTestProject(project)).Returns(firstIndicatorResponse);
            secondOtherIndicator.Setup(x => x.IsTestProject(project)).Returns(secondIndicatorResponse);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(firstIndicatorResponse || secondIndicatorResponse);
        }
    }
}
