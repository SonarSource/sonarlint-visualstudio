﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RuleSetConflictsControllerTests
    {
        private ConfigurableHost host;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableRuleSetInspector ruleSetInspector;
        private ConfigurableSourceControlledFileSystem sccFS;
        private ConfigurableRuleSetSerializer rsSerializer;
        private ConfigurableConflictsManager conflictsManager;
        private MockFileSystem fileSystem;
        private TestLogger logger;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            logger = new TestLogger();
            serviceProvider.RegisterService(typeof(ILogger), logger);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            host.Logger = logger;

            this.ruleSetInspector = null;
            this.sccFS = null;
            this.rsSerializer = null;
            this.conflictsManager = new ConfigurableConflictsManager();

            fileSystem = new MockFileSystem();
        }

        #region Tests

        [TestMethod]
        public void RuleSetConflictsController_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetConflictsController(null, this.conflictsManager, logger));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetConflictsController(this.host, null, logger));

            var testSubject = new RuleSetConflictsController(this.host, this.conflictsManager, logger);
            testSubject.FixConflictsCommand.Should().NotBeNull("Command instance is expected");
        }

        [TestMethod]
        public void RuleSetConflictsController_Clear()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host, this.conflictsManager, logger);

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
            var testSubject = new RuleSetConflictsController(this.host, this.conflictsManager, logger);
            bool result;

            // Case 1: No conflicts
            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeFalse("Not expecting any conflicts");
            logger.AssertOutputStrings(0);

            // Case 2: Has conflicts, no active section
            ProjectRuleSetConflict conflict = conflictsManager.AddConflict();

            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeTrue("Conflicts expected");
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists(conflict.Conflict.MissingRules.Single().FullId);

            // Case 3: Has conflicts, has active section
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);

            // Act
            result = testSubject.CheckForConflicts();

            // Assert
            result.Should().BeTrue("Conflicts expected");
            ((ConfigurableUserNotification)section.UserNotifications).AssertNotification(NotificationIds.RuleSetConflictsId);
            logger.AssertOutputStrings(2);

            logger.AssertPartialOutputStringExists(conflict.Conflict.MissingRules.Single().FullId);
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandStatus()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host, this.conflictsManager, logger);

            // Case 1: Nulls
            testSubject.FixConflictsCommand.CanExecute(null).Should().BeFalse();

            // Case 2: Empty collections
            testSubject.FixConflictsCommand.CanExecute(new ProjectRuleSetConflict[0]).Should().BeFalse();

            // Valid input
            ProjectRuleSetConflict[] conflicts = new[] { ConfigurableConflictsManager.CreateConflict() };

            // Case 3: Valid input, busy, has bound project
            this.host.VisualStateManager.IsBusy = true;
            this.host.VisualStateManager.SetBoundProject(new Uri("http://foo"), null, "project123");
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeFalse();

            // Case 4: Valid input, not busy, not bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.ClearBoundProject();
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeFalse();

            // Case 5: Valid input, not busy, has bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new Uri("http://foo"), null, "project123");
            testSubject.FixConflictsCommand.CanExecute(conflicts).Should().BeTrue();
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandExecution()
        {
            // Arrange
            var testSubject = new RuleSetConflictsController(this.host, this.conflictsManager, logger);
            this.ConfigureServiceProviderForFixConflictsCommandExecution();
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new Uri("http://foo"), null, "project123");
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
            fileSystem.GetFile(fixedRuleSet.FilePath).Should().NotBe(null);
            this.rsSerializer.AssertRuleSetsAreSame(fixedRuleSet.FilePath, fixedRuleSet);
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists(fixedRuleSet.FilePath, "deletedRuleId1", "reset.ruleset");

            notifications.AssertNoNotification(NotificationIds.RuleSetConflictsId);
        }

        #endregion Tests

        #region Helpers

        private void ConfigureServiceProviderForFixConflictsCommandExecution()
        {
            this.ruleSetInspector = new ConfigurableRuleSetInspector();
            this.serviceProvider.RegisterService(typeof(IRuleSetInspector), this.ruleSetInspector);

            this.sccFS = new ConfigurableSourceControlledFileSystem(fileSystem);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFS);

            this.rsSerializer = new ConfigurableRuleSetSerializer(fileSystem);
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
