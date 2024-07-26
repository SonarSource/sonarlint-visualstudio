/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using ThreadHelper = SonarLint.VisualStudio.TestInfrastructure.ThreadHelper;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class DisableRuleCommandTests
    {
        private const int VisibleAndEnabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
        private const int VisibleButDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED);
        private const int InvisbleAndDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE);

        [TestMethod]
        public void Ctor_NullArguments()
        {
            var menuCommandService = new DummyMenuCommandService();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = new Mock<IActiveSolutionBoundTracker>().Object;
            var logger = new TestLogger();
            var errorListHelper = Mock.Of<IErrorListHelper>();

            Action act = () => new DisableRuleCommand(null, userSettingsProvider, solutionTracker, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, solutionTracker, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsProvider");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, null, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, solutionTracker, null, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, solutionTracker, logger, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorListHelper");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorListHelper = Mock.Of<IErrorListHelper>();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(userSettingsProvider, solutionTracker, new TestLogger(), errorListHelper);

            // Assert
            command.CommandID.ID.Should().Be(DisableRuleCommand.CommandId);
            command.CommandID.Guid.Should().Be(DisableRuleCommand.CommandSet);
        }

        [TestMethod]
        [DataRow("cpp:S111")]
        [DataRow("c:S222")]
        [DataRow("javascript:S333")]
        [DataRow("typescript:S444")]
        [DataRow("css:S777")]
        [DataRow("secrets:S555")]
        public void CheckStatusAndExecute_SingleIssue_SupportedRepo_StandaloneMode_VisibleAndEnabled(string errorCode)
        {
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

            // 1. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            result.Should().Be(VisibleAndEnabled);
            command.Enabled.Should().BeTrue();
            command.Visible.Should().BeTrue();

            // 2. Invoke the command
            command.Invoke();

            mockUserSettingsProvider.Verify(x => x.DisableRule(errorCode), Times.Once);
        }

        [TestMethod]
        [DataRow("c:S111", SonarLintMode.Connected, false)]
        [DataRow("c:S111", SonarLintMode.LegacyConnected, false)]
        [DataRow("cpp:S111", SonarLintMode.Connected, false)]
        [DataRow("cpp:S111", SonarLintMode.LegacyConnected, false)]
        [DataRow("javascript:S111", SonarLintMode.Connected, false)]
        [DataRow("javascript:S111", SonarLintMode.LegacyConnected, false)]
        [DataRow("typescript:S111", SonarLintMode.Connected, false)]
        [DataRow("typescript:S111", SonarLintMode.LegacyConnected, false)]
        [DataRow("css:S111", SonarLintMode.Connected, false)]
        [DataRow("css:S111", SonarLintMode.LegacyConnected, false)]
        public void CheckStatus_SingleIssue_SupportedRepo_ConnectedMode_HasExpectedEnabledStatus(string errorCode, SonarLintMode bindingMode,
            bool expectedEnabled)
        {
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(bindingMode);
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

            // Act. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            var expectedOleStatus = expectedEnabled ? VisibleAndEnabled : VisibleButDisabled;
            result.Should().Be(expectedOleStatus);

            // Should always be visible, but not necessarily enabled
            command.Visible.Should().BeTrue();
            command.Enabled.Should().Be(expectedEnabled);
        }

        [TestMethod]
        public void CheckStatus_SingleIssue_Secrets_ConnectedMode_HasExpectedEnabledStatus()
        {
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Connected);
            var errorListHelper = CreateErrorListHelper("secrets:S111", ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

            // Act. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            var expectedOleStatus = VisibleButDisabled;
            result.Should().Be(expectedOleStatus);
            
            command.Visible.Should().BeTrue();
            command.Enabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void CheckStatus_NotASupportedSonarRepo(SonarLintMode mode)
        {
            var errorListHelper = CreateErrorListHelper("unsupportedRepo:S123", ruleExists: true);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(mode);

            // Act
            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

            // 1. Trigger the query status check
            var result = command.OleStatus;
            result.Should().Be(InvisbleAndDisabled);

            command.Enabled.Should().BeFalse();
            command.Visible.Should().BeFalse();
        }

        [TestMethod]
        public void QueryStatus_NonCriticalErrorSuppressed()
        {
            // Arrange
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);

            // Act - should not throw
            var _ = command.OleStatus;

            testLogger.AssertPartialOutputStringExists("exception xxx");
        }

        [TestMethod]
        public void QueryStatus_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);
            Action act = () => _ = command.OleStatus;

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        [TestMethod]
        public void Execute_NonCriticalErrorSuppressed()
        {
            // Arrange
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);

            // Act - should not throw
            command.Invoke();

            testLogger.AssertPartialOutputStringExists("exception xxx", AnalysisStrings.DisableRule_UnknownErrorCode);
        }

        [TestMethod]
        public void Execute_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);
            Action act = () => command.Invoke();

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateDisableRuleMenuCommand(IUserSettingsProvider userSettingsProvider, IActiveSolutionBoundTracker solutionTracker, ILogger logger, 
            IErrorListHelper errorListHelper)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new DisableRuleCommand(dummyMenuService, userSettingsProvider, solutionTracker, logger, errorListHelper);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        private static IActiveSolutionBoundTracker CreateSolutionTracker(SonarLintMode bindingMode)
        {
            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), bindingMode, null);
            var tracker = new Mock<IActiveSolutionBoundTracker>();
            tracker.Setup(x => x.CurrentConfiguration).Returns(bindingConfiguration);
            return tracker.Object;
        }

        private static IErrorListHelper CreateErrorListHelper(string errorCode, bool ruleExists)
        {
            var errorListHelper = new Mock<IErrorListHelper>();

            SonarCompositeRuleId.TryParse(errorCode, out SonarCompositeRuleId ruleId);
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Returns(ruleExists);

            return errorListHelper.Object;
        }
    }
}
