/*
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
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;

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
            var sonarQubeService = new Mock<ISonarQubeService>().Object;
            var logger = new TestLogger();
            var errorListHelper = Mock.Of<IErrorListHelper>();

            Action act = () => new DisableRuleCommand(null, userSettingsProvider, sonarQubeService, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, sonarQubeService, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsProvider");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, null, logger, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, sonarQubeService, null, errorListHelper);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new DisableRuleCommand(menuCommandService, userSettingsProvider, sonarQubeService, logger, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorListHelper");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorListHelper = Mock.Of<IErrorListHelper>();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var sonarQuberService = CreateSonarQubeService(isConnected: false);

            // Act
            var command = CreateDisableRuleMenuCommand(userSettingsProvider, sonarQuberService, new TestLogger(), errorListHelper);

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
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var sonarQubeService = CreateSonarQubeService(isConnected: false);

            // Act
            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, sonarQubeService, new TestLogger(), errorListHelper);

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
        [DataRow("c:S111")]
        [DataRow("c:S111")]
        [DataRow("cpp:S111")]
        [DataRow("cpp:S111")]
        [DataRow("javascript:S111")]
        [DataRow("javascript:S111")]
        [DataRow("typescript:S111")]
        [DataRow("typescript:S111")]
        public void CheckStatus_SingleIssue_SupportedRepo_ConnectedMode_IsDisabled(string errorCode)
        {
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var sonarQubeService = CreateSonarQubeService(isConnected: true);
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, sonarQubeService, new TestLogger(), errorListHelper);

            // Act. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            var expectedOleStatus = VisibleButDisabled;
            result.Should().Be(expectedOleStatus);

            // Should always be visible, but not necessarily enabled
            command.Visible.Should().BeTrue();
            command.Enabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("secrets:S111", "9.9", false)]
        [DataRow("secrets:S111", "9.8", true)]
        public void CheckStatus_SingleIssue_Secrets_ConnectedToSonarQube_ExpectedResult(string errorCode, string version, bool expectedEnablec)
        {
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var serverInfo = CreateServerInfo(ServerType.SonarQube, new Version(version));
            var sonarQubeService = CreateSonarQubeService(isConnected: true, serverInfo: serverInfo);
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, sonarQubeService, new TestLogger(), errorListHelper);

            // Act. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            var expectedOleStatus = expectedEnablec ? VisibleAndEnabled : VisibleButDisabled;
            result.Should().Be(expectedOleStatus);

            // Should always be visible, but not necessarily enabled
            command.Visible.Should().BeTrue();
            command.Enabled.Should().Be(expectedEnablec);
        }

        [TestMethod]
        public void CheckStatus_SingleIssue_Secrets_ConnectedToSonarCloud_IsDisabled()
        {
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var serverInfo = CreateServerInfo(ServerType.SonarCloud);
            var sonarQubeService = CreateSonarQubeService(isConnected: true, serverInfo: serverInfo);
            var errorListHelper = CreateErrorListHelper("secrets:S111", ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, sonarQubeService, new TestLogger(), errorListHelper);

            // Act. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            var expectedOleStatus = VisibleButDisabled;
            result.Should().Be(expectedOleStatus);

            // Should always be visible, but not necessarily enabled
            command.Visible.Should().BeTrue();
            command.Enabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        [DataRow(true)]
        public void CheckStatus_NotASupportedSonarRepo(bool isConnected)
        {
            var errorListHelper = CreateErrorListHelper("unsupportedRepo:S123", ruleExists: true);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSonarQubeService(isConnected);

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
            var solutionTracker = CreateSonarQubeService(isConnected: false);

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
            var solutionTracker = CreateSonarQubeService(isConnected: false);

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
            var solutionTracker = CreateSonarQubeService(isConnected: false);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);

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
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSonarQubeService(isConnected: false);

            var command = CreateDisableRuleMenuCommand(mockUserSettingsProvider.Object, solutionTracker, testLogger, errorListHelper.Object);
            Action act = () => command.Invoke();

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateDisableRuleMenuCommand(IUserSettingsProvider userSettingsProvider, ISonarQubeService sonarQubeservice, ILogger logger, IErrorListHelper errorListHelper)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new DisableRuleCommand(dummyMenuService, userSettingsProvider, sonarQubeservice, logger, errorListHelper);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        private static ISonarQubeService CreateSonarQubeService(bool isConnected, ServerInfo serverInfo = null)
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(isConnected);
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            return sonarQubeService.Object;
        }

        private static ServerInfo CreateServerInfo(ServerType serverType, Version version = null)
        {
            version ??= new Version();
            var serverInfo = new ServerInfo(version, serverType);

            return serverInfo;
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
