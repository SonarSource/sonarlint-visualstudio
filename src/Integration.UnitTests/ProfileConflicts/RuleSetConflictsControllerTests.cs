/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RuleSetConflictsControllerTests
    {
        private ConfigurableHost host;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableRuleSetInspector ruleSetInspector;
        private ConfigurableSourceControlledFileSystem sccFS;
        private ConfigurableRuleSetSerializer rsSerializer;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            this.ruleSetInspector = null;
            this.sccFS = null;
            this.rsSerializer = null;

            // Instead of ignored unexpected service, register one (for telemetry)
            this.serviceProvider.RegisterService(typeof(SComponentModel), new ConfigurableComponentModel());
        }

        #region Tests

        [TestMethod]
        public void RuleSetConflictsController_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetConflictsController(null));

            var testSubject = new RuleSetConflictsController(this.host);
            testSubject.FixConflictsCommand.Should().NotBeNull("Command instance is expected");
        }

        [TestMethod]
        public void RuleSetConflictsController_Clear()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host);

            // Case 1: No active section (should not crash)
            testSubject.Clear();

            // Case 2: Has active section
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            section.UserNotifications.ShowNotificationWarning("message", NotificationIds.RuleSetConflictsId, null);

            // Act
            testSubject.Clear();

            // Assert
            notifications.AssertNoNotification(NotificationIds.RuleSetConflictsId);
        }

        [TestMethod]
        public void RuleSetConflictsController_CheckForConflicts()
        {
            // Arrange
            var conflictsMananger = new ConfigurableConflictsManager();
            this.serviceProvider.RegisterService(typeof(IConflictsManager), conflictsMananger);
            var testSubject = new RuleSetConflictsController(this.host);
            bool result;

            // Case 1: No conflicts
            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeFalse("Not expecting any conflicts");
            this.outputWindowPane.AssertOutputStrings(0);

            // Case 2: Has conflicts, no active section
            ProjectRuleSetConflict conflict = conflictsMananger.AddConflict();

            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeTrue("Conflicts expected");
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0, new[] { conflict.Conflict.MissingRules.Single().FullId });

            // Case 3: Has conflicts, has active section
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);

            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeTrue("Conflicts expected");
            ((ConfigurableUserNotification)section.UserNotifications).AssertNotification(NotificationIds.RuleSetConflictsId);
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { conflict.Conflict.MissingRules.Single().FullId });
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandStatus()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host);

            // Case 1: Nulls
            testSubject.FixConflictsCommand.CanExecute(null).Should().BeFalse();

            // Case 2: Empty collections
            testSubject.FixConflictsCommand.CanExecute(new ProjectRuleSetConflict[0]).Should().BeFalse();

            // Valid input
            ProjectRuleSetConflict[] conflicts = new[] { ConfigurableConflictsManager.CreateConflict() };

            // Case 3: Valid input, busy, has bound project
            this.host.VisualStateManager.IsBusy = true;
            this.host.VisualStateManager.SetBoundProject(new SonarQubeProject("", ""));
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeFalse();

            // Case 4: Valid input, not busy, not bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.ClearBoundProject();
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeFalse();

            // Case 5: Valid input, not busy, has bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new SonarQubeProject("", ""));
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeTrue();
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandExecution()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host);
            this.ConfigureServiceProviderForFixConflictsCommandExecution();
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new SonarQubeProject("", ""));
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;

            ProjectRuleSetConflict[] conflicts = new[] { ConfigurableConflictsManager.CreateConflict() };

            RuleSet fixedRuleSet = TestRuleSetHelper.CreateTestRuleSet(3);
            fixedRuleSet.FilePath = "MyFixedRules.ruleset";

            RuleSetInspectorTestDataProvider inspectorData = new RuleSetInspectorTestDataProvider();
            var weakenedRulesMap = new Dictionary<RuleReference, RuleAction>();
            inspectorData.FindConflictsResult = new RuleConflictInfo(new RuleReference[0], weakenedRulesMap);
            inspectorData.FixConflictsResult = new FixedRuleSetInfo(fixedRuleSet, new[] { "reset.ruleset" }, new[] { "deletedRuleId1" });
            this.ruleSetInspector.FindConflictingRulesAction = inspectorData.FindConflictingRulesAction;
            this.ruleSetInspector.FixConflictingRulesAction = inspectorData.FixConflictingRulesAction;

            ICommand fixMeCommand = testSubject.CreateFixConflictsCommand(conflicts);
            section.UserNotifications.ShowNotificationWarning("fix me", NotificationIds.RuleSetConflictsId, fixMeCommand);

            // Act
            fixMeCommand.Execute(null);

            // Assert
            this.sccFS.files.Should().ContainKey(fixedRuleSet.FilePath);
            this.rsSerializer.AssertRuleSetsAreSame(fixedRuleSet.FilePath, fixedRuleSet);
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0,
                words: new[] { fixedRuleSet.FilePath, "deletedRuleId1", "reset.ruleset" },
                splitter:new[] {'\n', '\r', '\t', '\'', ':' });
            notifications.AssertNoNotification(NotificationIds.RuleSetConflictsId);
        }

        #endregion Tests

        #region Helpers

        private void ConfigureServiceProviderForFixConflictsCommandExecution()
        {
            this.ruleSetInspector = new ConfigurableRuleSetInspector();
            this.serviceProvider.RegisterService(typeof(IRuleSetInspector), this.ruleSetInspector);

            this.sccFS = new ConfigurableSourceControlledFileSystem();
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFS);

            this.rsSerializer = new ConfigurableRuleSetSerializer(sccFS);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.rsSerializer);
        }

        private class RuleSetInspectorTestDataProvider
        {
            #region Called by product

            public RuleConflictInfo FindConflictingRulesAction(string baseline, string project, string[] directories)
            {
                VerifyInputForDefaultConflictInstance(baseline, project, directories);

                return this.FindConflictsResult;
            }

            public FixedRuleSetInfo FixConflictingRulesAction(string baseline, string project, string[] directories)
            {
                VerifyInputForDefaultConflictInstance(baseline, project, directories);

                return this.FixConflictsResult;
            }

            #endregion Called by product

            #region Helpers

            public RuleConflictInfo FindConflictsResult
            {
                get;
                set;
            }

            public FixedRuleSetInfo FixConflictsResult
            {
                get;
                set;
            }

            private static void VerifyInputForDefaultConflictInstance(string baseline, string project, string[] directories)
            {
                var expectedConflict = ConfigurableConflictsManager.CreateConflict();
                baseline.Should().Be(expectedConflict.RuleSetInfo.BaselineFilePath, "baseline argument is not as expected");
                project.Should().Be(expectedConflict.RuleSetInfo.RuleSetFilePath, "project argument is not as expected");
                CollectionAssert.AreEqual(expectedConflict.RuleSetInfo.RuleSetDirectories, directories, "directories argument is not as expected");
            }

            #endregion Helpers
        }

        #endregion Helpers
    }
}