/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

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
        private IList<ITestProjectIndicator> testIndicators;
        private TestProjectIndicator testSubject;
        private Project project;
        private Mock<ITestProjectIndicator> firstIndicator;
        private Mock<ITestProjectIndicator> secondIndicator;

        [TestInitialize]
        public void TestInitialize()
        {
            project = Mock.Of<Project>();

            firstIndicator = new Mock<ITestProjectIndicator>();
            secondIndicator = new Mock<ITestProjectIndicator>();
            testIndicators = new List<ITestProjectIndicator>();
            
            testSubject = new TestProjectIndicator(testIndicators);
        }

        [TestMethod]
        public void Ctor_NullIndicatorsCollection_ArgumentNullException()
        {
            Action act = () => new TestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("testProjectIndicators");
        }

        [TestMethod]
        public void IsTestProject_NoIndicators_False()
        {
            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsTestProject_OneIndicator_IndicatorHasResponse_IndicatorResponse(bool indicatorResponse)
        {
            testIndicators.Add(firstIndicator.Object);
            firstIndicator.Setup(x => x.IsTestProject(project)).Returns(indicatorResponse);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(indicatorResponse);
        }

        [TestMethod]
        public void IsTestProject_OneIndicator_IndicatorHasNoResponse_False()
        {
            testIndicators.Add(firstIndicator.Object);
            firstIndicator.Setup(x => x.IsTestProject(project)).Returns(null as bool?);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(false);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsTestProject_TwoIndicators_FirstIndicatorHasResponse_SecondIndicatorNotCalled(bool firstIndicatorResponse)
        {
            testIndicators.Add(firstIndicator.Object);
            testIndicators.Add(secondIndicator.Object);
            firstIndicator.Setup(x => x.IsTestProject(project)).Returns(firstIndicatorResponse);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(firstIndicatorResponse);

            secondIndicator.Verify(x=> x.IsTestProject(It.IsAny<Project>()), Times.Never());
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsTestProject_TwoIndicators_FirstIndicatorHasNoResponse_SecondIndicatorHasResponse_SecondIndicatorResponse(bool secondIndicatorResponse)
        {
            testIndicators.Add(firstIndicator.Object);
            testIndicators.Add(secondIndicator.Object);
            firstIndicator.Setup(x => x.IsTestProject(project)).Returns(null as bool?);
            secondIndicator.Setup(x => x.IsTestProject(project)).Returns(secondIndicatorResponse);

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(secondIndicatorResponse);
        }

        [TestMethod]
        public void IsTestProject_TwoIndicators_NeitherHasResponse_False()
        {
            testIndicators.Add(firstIndicator.Object);
            testIndicators.Add(secondIndicator.Object);
            firstIndicator.Setup(x => x.IsTestProject(project)).Returns(null as bool?);
            secondIndicator.Setup(x => x.IsTestProject(project)).Returns(null as bool?);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }
    }
}
