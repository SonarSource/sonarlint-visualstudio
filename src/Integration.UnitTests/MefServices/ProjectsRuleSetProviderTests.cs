/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectsRuleSetProviderTests
    {
        private ConfigurableVsProjectSystemHelper projectSystemHelperMock;
        private Mock<IVsFileChangeEx> fileChangeServiceMock;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private Mock<IConfigurationProvider> configurationProviderMock;
        private Mock<ILogger> loggerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            var serviceProvider = new ConfigurableServiceProvider();
            projectSystemHelperMock = new ConfigurableVsProjectSystemHelper(serviceProvider);
            fileChangeServiceMock = new Mock<IVsFileChangeEx>();
            activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            configurationProviderMock = new Mock<IConfigurationProvider>();
            loggerMock = new Mock<ILogger>();
        }

        [TestMethod]
        public void Ctor_WhenIProjectSystemHelperNull_Throws()
        {
            // Arrange
            Action act = () => new ProjectsRuleSetProvider(null, fileChangeServiceMock.Object, activeSolutionTracker,
                configurationProviderMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectSystemHelper");
        }

        [TestMethod]
        public void Ctor_WhenIVsFileChangeExNull_Throws()
        {
            // Arrange
            Action act = () => new ProjectsRuleSetProvider(projectSystemHelperMock, null, activeSolutionTracker,
                configurationProviderMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileChangeService");
        }

        [TestMethod]
        public void Ctor_WhenIActiveSolutionTrackerNull_Throws()
        {
            // Arrange
            Action act = () => new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object, null,
                configurationProviderMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionTracker");
        }

        [TestMethod]
        public void Ctor_WhenIConfigurationProviderNull_Throws()
        {
            // Arrange
            Action act = () => new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object,
                activeSolutionTracker, null, loggerMock.Object);

            // Assert
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configurationProvider");
        }

        [TestMethod]
        public void Ctor_WhenILoggerNull_Throws()
        {
            // Arrange
            Action act = () => new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object,
                activeSolutionTracker, configurationProviderMock.Object, null);

            // Assert
            act.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void HasRuleSetWithSonarAnalyzerRules_WhenFilePathIsNotCached_ReturnsFalse()
        {
            // Arrange
            var testSubject = new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object,
                activeSolutionTracker, configurationProviderMock.Object, loggerMock.Object);
            testSubject.projectPathToCachedData.Clear();

            // Act
            var result = testSubject.HasRuleSetWithSonarAnalyzerRules("foo");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasRuleSetWithSonarAnalyzerRules_WhenFilePathIsCachedAndDoesNotContainSonarRule_ReturnsFalse()
        {
            // Arrange
            var testSubject = new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object,
                activeSolutionTracker, configurationProviderMock.Object, loggerMock.Object);
            testSubject.projectPathToCachedData.Add("foo", new ProjectsRuleSetProvider.ProjectData("bar.ruleset", false));

            // Act
            var result = testSubject.HasRuleSetWithSonarAnalyzerRules("foo");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasRuleSetWithSonarAnalyzerRules_WhenFilePathIsCachedAndContainsSonarRule_ReturnsTrue()
        {
            // Arrange
            var testSubject = new ProjectsRuleSetProvider(projectSystemHelperMock, fileChangeServiceMock.Object,
                activeSolutionTracker, configurationProviderMock.Object, loggerMock.Object);
            testSubject.projectPathToCachedData.Add("foo", new ProjectsRuleSetProvider.ProjectData("bar.ruleset", true));

            // Act
            var result = testSubject.HasRuleSetWithSonarAnalyzerRules("foo");

            // Assert
            result.Should().BeTrue();
        }
    }
}
