//-----------------------------------------------------------------------
// <copyright file="RuleSetConflictsControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

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
            Assert.IsNotNull(testSubject.FixConflictsCommand, "Command instance is expected");
        }

        [TestMethod]
        public void RuleSetConflictsController_Clear()
        {
            // Setup
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

            // Verify
            notifications.AssertNoNotification(NotificationIds.RuleSetConflictsId);
        }

        [TestMethod]
        public void RuleSetConflictsController_CheckForConflicts()
        {
            // Setup
            var conflictsMananger = new ConfigurableConflictsManager();
            this.serviceProvider.RegisterService(typeof(IConflictsManager), conflictsMananger);
            var testSubject = new RuleSetConflictsController(this.host);
            bool result;

            // Case 1: No conflicts
            // Act
            result = testSubject.CheckForConflicts();

            // Verify 
            Assert.IsFalse(result, "Not expecting any conflicts");
            this.outputWindowPane.AssertOutputStrings(0);

            // Case 2: Has conflicts, no active section
            ProjectRuleSetConflict conflict = conflictsMananger.AddConflict();

            // Act
            result = testSubject.CheckForConflicts();

            // Verify 
            Assert.IsTrue(result, "Conflicts expected");
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0, new[] { conflict.Conflict.MissingRules.Single().FullId });

            // Case 3: Has conflicts, has active section
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);

            // Act
            result = testSubject.CheckForConflicts();

            // Verify 
            Assert.IsTrue(result, "Conflicts expected");
            ((ConfigurableUserNotification)section.UserNotifications).AssertNotification(NotificationIds.RuleSetConflictsId);
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { conflict.Conflict.MissingRules.Single().FullId });
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandStatus()
        {
            // Setup
            var testSubject = new RuleSetConflictsController(this.host);

            // Case 1: Nulls
            Assert.IsFalse(testSubject.FixConflictsCommand.CanExecute(null));

            // Case 2: Empty collections
            Assert.IsFalse(testSubject.FixConflictsCommand.CanExecute(new ProjectRuleSetConflict[0]));

            // Valid input
            ProjectRuleSetConflict[] conflicts = new[] { ConfigurableConflictsManager.CreateConflict() };
            
            // Case 3: Valid input, busy, has bound project
            this.host.VisualStateManager.IsBusy = true;
            this.host.VisualStateManager.SetBoundProject(new Integration.Service.ProjectInformation());
            Assert.IsFalse(testSubject.FixConflictsCommand.CanExecute(conflicts));

            // Case 4: Valid input, not busy, not bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.ClearBoundProject();
            Assert.IsFalse(testSubject.FixConflictsCommand.CanExecute(conflicts));

            // Case 5: Valid input, not busy, has bound project
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new Integration.Service.ProjectInformation());
            Assert.IsTrue(testSubject.FixConflictsCommand.CanExecute(conflicts));
        }

        [TestMethod]
        public void RuleSetConflictsController_FixConflictsCommandExecution()
        {
            // Setup
            var testSubject = new RuleSetConflictsController(this.host);
            this.ConfigureServiceProviderForFixConflictsCommandExecution();
            this.host.VisualStateManager.IsBusy = false;
            this.host.VisualStateManager.SetBoundProject(new Integration.Service.ProjectInformation());
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

            // Verify
            this.sccFS.AssertFileExists(fixedRuleSet.FilePath);
            this.rsSerializer.AssertRuleSetsAreSame(fixedRuleSet.FilePath, fixedRuleSet);
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0, 
                words: new[] { fixedRuleSet.FilePath, "deletedRuleId1", "reset.ruleset" },
                splitter:new[] {'\n', '\r', '\t', '\'', ':' });
            notifications.AssertNoNotification(NotificationIds.RuleSetConflictsId);
        }
        #endregion

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
            #endregion

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
                Assert.AreEqual(expectedConflict.RuleSetInfo.BaselineFilePath, baseline, "baseline argument is not as expected");
                Assert.AreEqual(expectedConflict.RuleSetInfo.RuleSetFilePath, project, "project argument is not as expected");
                CollectionAssert.AreEqual(expectedConflict.RuleSetInfo.RuleSetDirectories, directories, "directories argument is not as expected");
            }
            #endregion
        }
        #endregion
    }
}
