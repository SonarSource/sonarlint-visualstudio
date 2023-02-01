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
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using ThreadHelper = SonarLint.VisualStudio.Integration.UnitTests.ThreadHelper;

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
            var errorList = Mock.Of<IErrorList>();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = new Mock<IActiveSolutionBoundTracker>().Object;
            var logger = new TestLogger();
            var errorListHelper = Mock.Of<IErrorListHelper>();

            Action act = () => new DisableRuleCommand(null, errorList, userSettingsProvider, solutionTracker, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, userSettingsProvider, solutionTracker, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorList");

            act = () => new DisableRuleCommand(menuCommandService, errorList, null, solutionTracker, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsProvider");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, null, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, solutionTracker, null, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, solutionTracker, logger, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorListHelper");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorListHelper = Mock.Of<IErrorListHelper>();
            var errorList = Mock.Of<IErrorList>();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, userSettingsProvider, solutionTracker, new TestLogger(), errorListHelper);

            // Assert
            command.CommandID.ID.Should().Be(DisableRuleCommand.CommandId);
            command.CommandID.Guid.Should().Be(DisableRuleCommand.CommandSet);
        }

        [TestMethod]
        [DataRow("cpp:S111")]
        [DataRow("c:S222")]
        [DataRow("javascript:S333")]
        [DataRow("typescript:S444")]
        [DataRow("secrets:S555")]
        public void CheckStatusAndExecute_SingleIssue_SupportedRepo_StandaloneMode_VisibleAndEnabled(string errorCode)
        {
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = CreateErrorListHelper(errorList, errorCode, ruleExists: true);

            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

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
        [DataRow("secrets:S111", SonarLintMode.Connected, true)]
        [DataRow("secrets:S111", SonarLintMode.LegacyConnected, true)]
        public void CheckStatus_SingleIssue_SupportedRepo_ConnectedMode_HasExpectedEnabledStatus(string errorCode, SonarLintMode bindingMode,
            bool expectedEnabled)
        {
            var errorList = Mock.Of<IErrorList>();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(bindingMode);
            var errorListHelper = CreateErrorListHelper(errorList, errorCode, ruleExists: true);

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

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
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void CheckStatus_NotASupportedSonarRepo(SonarLintMode mode)
        {
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = CreateErrorListHelper(errorList, "unsupportedRepo:S123", ruleExists: true);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(mode);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger(), errorListHelper);

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
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(errorList, out ruleId)).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);

            // Act - should not throw
            var _ = command.OleStatus;

            testLogger.AssertPartialOutputStringExists("exception xxx");
        }

        [TestMethod]
        public void QueryStatus_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(errorList, out ruleId)).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);
            Action act = () => _ = command.OleStatus;

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        [TestMethod]
        public void Execute_NonCriticalErrorSuppressed()
        {
            // Arrange
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(errorList, out ruleId)).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);

            // Act - should not throw
            command.Invoke();

            testLogger.AssertPartialOutputStringExists("exception xxx", AnalysisStrings.DisableRule_UnknownErrorCode);
        }

        [TestMethod]
        public void Execute_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorList = Mock.Of<IErrorList>();
            var errorListHelper = new Mock<IErrorListHelper>();
            var ruleId = It.IsAny<SonarCompositeRuleId>();
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(errorList, out ruleId)).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);
            Action act = () => command.Invoke();

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateDisableRuleMenuCommand(IErrorList errorList, IUserSettingsProvider userSettingsProvider, IActiveSolutionBoundTracker solutionTracker, ILogger logger, IErrorListHelper errorListHelper)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new DisableRuleCommand(dummyMenuService, errorList, userSettingsProvider, solutionTracker, logger, errorListHelper);

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

        private static IErrorListHelper CreateErrorListHelper(IErrorList errorList, string errorCode, bool ruleExists)
        {
            var errorListHelper = new Mock<IErrorListHelper>();

            SonarCompositeRuleId.TryParse(errorCode, out SonarCompositeRuleId ruleId);
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(errorList, out ruleId)).Returns(ruleExists);

            return errorListHelper.Object;
        }
    }
}
