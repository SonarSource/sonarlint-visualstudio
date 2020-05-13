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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.LocalServices.TestProjectIndicators;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ServiceGuidTestProjectIndicatorTests
    {
        private Mock<ILogger> logger;
        private Mock<IProjectSystemHelper> projectSystemHelperMock;
        private ProjectMock project;
        private ServiceGuidTestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new Mock<ILogger>();
            projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            project = new ProjectMock("test.csproj");

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            testSubject = new ServiceGuidTestProjectIndicator(serviceProviderMock.Object, logger.Object);
        }

        [TestMethod]
        public void Ctor_NullLogger_ArgumentNullException()
        {
            Action act = () => new ServiceGuidTestProjectIndicator(Mock.Of<IServiceProvider>(), null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new ServiceGuidTestProjectIndicator(null, logger.Object);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void IsTestProject_HasCorrectServiceInclude_True()
        {
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_NoCorrectServiceInclude_Null()
        {
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ExceptionOccurs_Null()
        {
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Throws<ArgumentException>();

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ExceptionOccurs_ErrorIsWrittenToLog()
        {
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Throws<ArgumentException>();

            testSubject.IsTestProject(project);

            logger.Verify(x => x.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void IsTestProject_CriticalExceptionOccurs_NotSuppressedOrLogged()
        {
            var critialException = new StackOverflowException("BANG!");
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Throws(critialException);

            Action act = () => testSubject.IsTestProject(project);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("BANG!");
            logger.Verify(x => x.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void IsTestProject_NoException_NoErrorIsWrittenToLog()
        {
            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(project, "Service", ServiceGuidTestProjectIndicator.TestServiceGuid))
                .Returns(false);

            testSubject.IsTestProject(project);

            logger.Verify(x => x.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }
    }
}
