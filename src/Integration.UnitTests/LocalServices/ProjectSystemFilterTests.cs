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
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class ProjectSystemFilterTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystem;
        private ConfigurableHost host;
        private Mock<ITestProjectIndicator> testProjectIndicatorMock;
        private ProjectSystemFilter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            serviceProvider = new ConfigurableServiceProvider();

            projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            testProjectIndicatorMock = new Mock<ITestProjectIndicator>();

            host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            serviceProvider.RegisterService(typeof(SComponentModel), mefModel);

            testSubject = new ProjectSystemFilter(host, testProjectIndicatorMock.Object);
        }

        [TestMethod]
        public void Ctor_NullHost_ArgumentNullException()
        {
            Action act = () => new ProjectSystemFilter(null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void Ctor_NullTestIndicator_ArgumentNullException()
        {
            Action act = () => new ProjectSystemFilter(host, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("testProjectIndicator");
        }

        [TestMethod]
        public void IsAccepted_ProjectIsNull_ArgumentNullException()
        {
            Exceptions.Expect<ArgumentNullException>(() => testSubject.IsAccepted(null));
        }

        [TestMethod]
        public void IsAccepted_ProjectLanguageUnsupported_False()
        {
            var project = new ProjectMock("unsupported.proj") {ProjectKind = null};

            var actual = testSubject.IsAccepted(project);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty()
        {
            IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty_Impl(
                ProjectSystemHelper.CSharpCoreProjectKind);
            IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty_Impl(ProjectSystemHelper.CSharpProjectKind);

            IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty_Impl(ProjectSystemHelper.VbCoreProjectKind);
            IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty_Impl(ProjectSystemHelper.VbProjectKind);
        }

        private void IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty_Impl(string projectTypeGuid)
        {
            // Arrange
            var project = new ProjectMock("supported.proj") {ProjectKind = projectTypeGuid};
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "False"); // Should not matter

            // Test case 1: missing property-> is accepted
            // Act
            var result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with missing property SonarQubeExclude should be accepted");

            // Test case 2: property non-bool -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, string.Empty);

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 3: property non-bool, non-empty -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "abc");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 4: property true -> not accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "true");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Project with property SonarQubeExclude=false should NOT be accepted");

            // Test case 5: property false -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with property SonarQubeExclude=true should be accepted");
        }

        [TestMethod]
        public void IsAccepted_UnrecognisedProjectType_ReturnsFalse()
        {
            // Arrange
            var project = new ProjectMock("unsupported.vcxproj");
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "False"); // Should not matter

            // Act and Assert
            testSubject.IsAccepted(project).Should().BeFalse();
        }

        [TestMethod]
        public void IsAccepted_SharedProject_ReturnsFalse()
        {
            // Arrange
            var project = new ProjectMock("shared1.shproj");
            project.SetCSProjectKind();

            project = new ProjectMock("shared1.SHPROJ");
            project.SetCSProjectKind();

            // Act and Assert
            testSubject.IsAccepted(project).Should().BeFalse();
        }

        [TestMethod]
        public void IsAccepted_TestProject_ReturnsFalse()
        {
            // Arrange
            var project = new ProjectMock("supported.proj") {ProjectKind = ProjectSystemHelper.CSharpCoreProjectKind};
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "False"); // Should not matter

            testProjectIndicatorMock.Setup(x => x.IsTestProject(project)).Returns(true);

            // Act and Assert
            testSubject.IsAccepted(project).Should().BeFalse();
        }

        [TestMethod]
        public void IsAccepted_UnknownIfTestProject_ReturnsTrue()
        {
            // Arrange
            var project = new ProjectMock("supported.proj") { ProjectKind = ProjectSystemHelper.CSharpCoreProjectKind };
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "False"); // Should not matter

            testProjectIndicatorMock.Setup(x => x.IsTestProject(project)).Returns((bool?) null);

            // Act and Assert
            testSubject.IsAccepted(project).Should().BeTrue();
        }
    }
}
