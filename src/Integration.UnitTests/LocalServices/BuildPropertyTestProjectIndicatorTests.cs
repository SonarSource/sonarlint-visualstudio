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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class BuildPropertyTestProjectIndicatorTests
    {
        private Mock<IProjectSystemHelper> projectSystemHelper;
        private Mock<IProjectPropertyManager> projectPropertyManager;
        private BuildPropertyTestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            projectSystemHelper = new Mock<IProjectSystemHelper>();
            projectPropertyManager = new Mock<IProjectPropertyManager>();

            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(projectPropertyManager.Object);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider.Setup(x => x.GetService(typeof(IProjectSystemHelper))).Returns(projectSystemHelper.Object);
            serviceProvider.Setup(x => x.GetService(typeof(SComponentModel))).Returns(mefModel);

            projectSystemHelper
                .Setup(x => x.GetIVsHierarchy(It.IsAny<EnvDTE.Project>()))
                .Returns(new ProjectMock(""));

            testSubject = new BuildPropertyTestProjectIndicator(serviceProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new BuildPropertyTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void IsTestProject_NoVsHierarchy_ArgumentException()
        {
            var project = Mock.Of<Project>();
            projectSystemHelper.Setup(x => x.GetIVsHierarchy(project)).Returns(null as IVsHierarchy);

            Action act = () => testSubject.IsTestProject(project);
            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void IsTestProject_HierarchyIsNotVsBuildPropertyStorage_ArgumentException()
        {
            var project = Mock.Of<Project>();
            projectSystemHelper.Setup(x => x.GetIVsHierarchy(project)).Returns(Mock.Of<IVsHierarchy>());

            Action act = () => testSubject.IsTestProject(project);
            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void IsTestProject_TestProjectBuildPropertyExistsAndSetToTrue_True()
        {
            var project = Mock.Of<Project>();

            projectPropertyManager
                .Setup(x => x.GetBooleanProperty(project, Constants.SonarQubeTestProjectBuildPropertyKey))
                .Returns(true);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void IsTestProject_TestProjectBuildPropertyExistsAndSetToFalse_False()
        {
            var project = Mock.Of<Project>();

            projectPropertyManager
                .Setup(x => x.GetBooleanProperty(project, Constants.SonarQubeTestProjectBuildPropertyKey))
                .Returns(false);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void IsTestProject_TestProjectBuildPropertyDoesNotExist_Null()
        {
            var project = Mock.Of<Project>();

            projectPropertyManager
                .Setup(x => x.GetBooleanProperty(project, Constants.SonarQubeTestProjectBuildPropertyKey))
                .Returns((bool?) null);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }
    }
}
