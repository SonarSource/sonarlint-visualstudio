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
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class DeprecatedSonarRuleSetManagerTests
    {
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private Mock<ILogger> loggerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            var serviceProvider = new ConfigurableServiceProvider();
            projectSystemHelper = new ConfigurableVsProjectSystemHelper(serviceProvider);
            loggerMock = new Mock<ILogger>();
        }

        [TestMethod]
        public void Ctor_WhenIProjectSystemHelperNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(null, loggerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectSystemHelper");
        }

        [TestMethod]
        public void Ctor_WhenILoggerNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(projectSystemHelper, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void WarnIfAnyProjectHasSonarRuleSet_WhenNoProjects_DoesNothing()
        {
            // Arrange
            var testSubject = new DeprecatedSonarRuleSetManager(projectSystemHelper, loggerMock.Object);

            // Act
            testSubject.WarnIfAnyProjectHasSonarRuleSet();

            // Assert
            loggerMock.Verify(x => x.WriteLine(Strings.ProjectWithSonarRules), Times.Never);
        }

        [TestMethod]
        public void WarnIfAnyProjectHasSonarRuleSet_WhenProjectsDoNotHaveRuleSet_DoesNothing()
        {
            // Arrange
            var testSubject = new DeprecatedSonarRuleSetManager(projectSystemHelper, loggerMock.Object);
            var projectMock = new ProjectMock(Path.GetTempFileName());
            projectSystemHelper.Projects = new[] { projectMock };

            // Act
            testSubject.WarnIfAnyProjectHasSonarRuleSet();

            // Assert
            loggerMock.Verify(x => x.WriteLine(Strings.ProjectWithSonarRules), Times.Never);
        }

        [TestMethod]
        public void WarnIfAnyProjectHasSonarRuleSet_WhenProjectsHaveRuleSetWhichDoesNotExist_DoesNothing()
        {
            // Arrange
            var testSubject = new DeprecatedSonarRuleSetManager(projectSystemHelper, loggerMock.Object);
            var projectMock = new ProjectMock(Path.GetTempFileName());
            projectMock.SetBuildProperty(Constants.CodeAnalysisRuleSetPropertyKey, "foo1.ruleset");
            projectSystemHelper.Projects = new[] { projectMock };

            // Act
            testSubject.WarnIfAnyProjectHasSonarRuleSet();

            // Assert
            loggerMock.Verify(x => x.WriteLine(Strings.ProjectWithSonarRules), Times.Never);
        }

        [TestMethod]
        public void WarnIfAnyProjectHasSonarRuleSet_WhenProjectsHaveRuleSetWithoutSonarRules_DoesNothing()
        {
            // Arrange
            var testSubject = new DeprecatedSonarRuleSetManager(projectSystemHelper, loggerMock.Object);

            var projectPath = Path.GetTempFileName();

            var rulesetName = "foo2.ruleset";
            var ruleset = TestRuleSetHelper.CreateTestRuleSet(Path.GetDirectoryName(projectPath), rulesetName);
            ruleset.WriteToFile(ruleset.FilePath);

            var projectMock = new ProjectMock(projectPath);
            projectSystemHelper.Projects = new[] { projectMock };
            projectMock.SetBuildProperty(Constants.CodeAnalysisRuleSetPropertyKey, rulesetName);

            // Act
            testSubject.WarnIfAnyProjectHasSonarRuleSet();

            // Assert
            loggerMock.Verify(x => x.WriteLine(Strings.ProjectWithSonarRules), Times.Never);
        }

        [TestMethod]
        public void WarnIfAnyProjectHasSonarRuleSet_WhenProjectsHaveRuleSetWithSonarRules_LogWarning()
        {
            // Arrange
            var testSubject = new DeprecatedSonarRuleSetManager(projectSystemHelper, loggerMock.Object);

            var projectPath = Path.GetTempFileName();

            var rulesetName = "foo3.ruleset";
            var ruleset = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "S110" }, "SonarAnalyzer.Foo");
            ruleset.WriteToFile(Path.Combine(Path.GetDirectoryName(projectPath), rulesetName));

            var projectMock = new ProjectMock(projectPath);
            projectSystemHelper.Projects = new[] { projectMock };
            projectMock.SetBuildProperty(Constants.CodeAnalysisRuleSetPropertyKey, rulesetName);

            // Act
            testSubject.WarnIfAnyProjectHasSonarRuleSet();

            // Assert
            loggerMock.Verify(x => x.WriteLine(Strings.ProjectWithSonarRules), Times.Once);
        }
    }
}
