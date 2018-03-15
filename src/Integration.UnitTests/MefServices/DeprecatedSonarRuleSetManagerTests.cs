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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class DeprecatedSonarRuleSetManagerTests
    {
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetProvider;
        private Mock<ILogger> loggerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            this.activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            var serviceProvider = new ConfigurableServiceProvider();
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(serviceProvider);
            this.ruleSetProvider = new ConfigurableSolutionRuleSetsInformationProvider();
            loggerMock = new Mock<ILogger>();
        }

        [TestMethod]
        public void Ctor_WhenIActiveSolutionBoundTrackerNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(null, activeSolutionTracker, projectSystemHelper,
                ruleSetProvider, loggerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");
        }

        [TestMethod]
        public void Ctor_WhenIActiveSolutionTrackerNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(activeSolutionBoundTracker, null, projectSystemHelper,
                ruleSetProvider, loggerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionTracker");
        }

        [TestMethod]
        public void Ctor_WhenIProjectSystemHelperNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(activeSolutionBoundTracker, activeSolutionTracker, null,
                ruleSetProvider, loggerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectSystemHelper");
        }

        [TestMethod]
        public void Ctor_WhenISolutionRuleSetsInformationProviderNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(activeSolutionBoundTracker, activeSolutionTracker,
                projectSystemHelper, null, loggerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleSetProvider");
        }

        [TestMethod]
        public void Ctor_WhenILoggerNull_Throws()
        {
            // Arrange
            Action act = () => new DeprecatedSonarRuleSetManager(activeSolutionBoundTracker, activeSolutionTracker,
                projectSystemHelper, ruleSetProvider, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_WhenSolutionInLegacyMode_DoesNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.LegacyConnected);

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void Ctor_WhenNullMode_DoNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = null;

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void Ctor_WhenStandaloneAndProjectHasSonarRules_DoWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = NewConnectedMode.BindingConfiguration.Standalone;
            CreateProjectAndRuleSet();

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Once);
        }

        [TestMethod]
        public void Ctor_WhenStandaloneAndProjectRuleSetDoesNotExist_DoNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = NewConnectedMode.BindingConfiguration.Standalone;
            CreateProjectAndRuleSet(createRuleSet: false);

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void Ctor_WhenNewConnectedAndProjectHasSonarRules_DoWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.Connected);
            CreateProjectAndRuleSet();

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Once);
        }

        [TestMethod]
        public void Ctor_WhenStandaloneAndProjectDoesNotHaveSonarRules_DoesNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.Connected);
            CreateProjectAndRuleSet(analyzerId: "StyleCop");

            // Act
            CreateTestSubject();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void OnActiveSolutionChanged_WhenSolutionClosed_DoesNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.LegacyConnected);
            CreateTestSubject();

            // Act
            this.activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void OnActiveSolutionChanged_WhenSolutionOpened_DoWarn()
        {
            WhenGivenAction_DoesTheExpected(() =>
                this.activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true),
                warns: true);
        }

        [TestMethod]
        public void OnSolutionBindingChanged_WhenSolutionInLegacyMode_DoesNotWarn()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.LegacyConnected);
            CreateTestSubject();

            // Act
            this.activeSolutionBoundTracker.SimulateSolutionBindingChanged(new ActiveSolutionBindingEventArgs(
                new NewConnectedMode.BindingConfiguration(new Persistence.BoundSonarQubeProject(),
                NewConnectedMode.SonarLintMode.LegacyConnected)));

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage), Times.Never);
        }

        [TestMethod]
        public void OnSolutionBindingChanged_WhenStandaloneAndProjectHasSonarRules_DoWarn()
        {
            WhenGivenAction_DoesTheExpected(() =>
                this.activeSolutionBoundTracker.SimulateSolutionBindingChanged(new ActiveSolutionBindingEventArgs(
                    NewConnectedMode.BindingConfiguration.Standalone)),
                    warns:
                true);
        }

        [TestMethod]
        public void OnSolutionBindingChanged_WhenNewConnectedAndProjectHasSonarRules_DoWarn()
        {
            WhenGivenAction_DoesTheExpected(() =>
                this.activeSolutionBoundTracker.SimulateSolutionBindingChanged(new ActiveSolutionBindingEventArgs(
                    new NewConnectedMode.BindingConfiguration(new Persistence.BoundSonarQubeProject(),
                    NewConnectedMode.SonarLintMode.Connected))),
                warns: true);
        }

        [TestMethod]
        public void OnSolutionBindingChanged_WhenEventModeIsNull_DoNotWarn()
        {
            WhenGivenAction_DoesTheExpected(() =>
                this.activeSolutionBoundTracker.SimulateSolutionBindingChanged(new ActiveSolutionBindingEventArgs(null)),
                warns: false);
        }

        private void WhenGivenAction_DoesTheExpected(Action actAction, bool warns)
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = new NewConnectedMode.BindingConfiguration(
                new Persistence.BoundSonarQubeProject(), NewConnectedMode.SonarLintMode.LegacyConnected);
            CreateProjectAndRuleSet();
            CreateTestSubject();

            // Act
            actAction();

            // Assert
            loggerMock.Verify(x => x.WriteLine(DeprecatedSonarRuleSetManager.DeprecationMessage),
                warns ? Times.Once() : Times.Never());
        }

        private void CreateProjectAndRuleSet(bool createRuleSet = true, string analyzerId = "SonarAnalyzer.Foo")
        {
            var projectPath = Path.GetTempFileName();

            var rulesetName = $"{Guid.NewGuid()}.ruleset";
            if (createRuleSet)
            {
                var ruleset = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "S110" }, analyzerId);
                ruleset.WriteToFile(Path.Combine(Path.GetDirectoryName(projectPath), rulesetName));
            }

            var project = new ProjectMock(projectPath);
            projectSystemHelper.Projects = new[] { project };
            project.SetBuildProperty(Constants.CodeAnalysisRuleSetPropertyKey, rulesetName);

            this.ruleSetProvider.RegisterProjectInfo(project, new RuleSetDeclaration(project, new PropertyMock("bar", null),
                rulesetName, "", ""));
        }

        private void CreateTestSubject() =>
            new DeprecatedSonarRuleSetManager(activeSolutionBoundTracker, activeSolutionTracker, projectSystemHelper,
                ruleSetProvider, loggerMock.Object);
    }
}
