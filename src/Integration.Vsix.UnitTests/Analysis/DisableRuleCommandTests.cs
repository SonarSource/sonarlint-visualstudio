/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
            var globalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>().Object;
            var solutionTracker = new Mock<IActiveSolutionBoundTracker>().Object;
            var logger = new TestLogger();
            var errorListHelper = Mock.Of<IErrorListHelper>();
            var languageProvider = Mock.Of<ILanguageProvider>();

            Action act = () => new DisableRuleCommand(null, globalUserSettingsUpdater, solutionTracker, logger, errorListHelper, languageProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, solutionTracker, logger, errorListHelper, languageProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("globalUserSettingsUpdater");

            act = () => new DisableRuleCommand(menuCommandService, globalUserSettingsUpdater, null, logger, errorListHelper, languageProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");

            act = () => new DisableRuleCommand(menuCommandService, globalUserSettingsUpdater, solutionTracker, null, errorListHelper, languageProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new DisableRuleCommand(menuCommandService, globalUserSettingsUpdater, solutionTracker, logger, null, languageProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorListHelper");

            act = () => new DisableRuleCommand(menuCommandService, globalUserSettingsUpdater, solutionTracker, logger, errorListHelper, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("languageProvider");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorListHelper = Mock.Of<IErrorListHelper>();
            var globalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>().Object;
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(globalUserSettingsUpdater, solutionTracker, new TestLogger(), errorListHelper, MockLanguageProvider().Object);

            // Assert
            command.CommandID.ID.Should().Be(DisableRuleCommand.CommandId);
            command.CommandID.Guid.Should().Be(DisableRuleCommand.CommandSet);
        }

        [TestMethod]
        [DataRow("tsql:S222")]
        [DataRow("java:S111")]
        public void CheckStatusAndExecute_SingleIssue_UnsupportedRepo_StandaloneMode_VisibleAndEnabled(string errorCode)
        {
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper, MockLanguageProvider().Object);

            // 1. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            result.Should().Be(InvisbleAndDisabled);
            command.Enabled.Should().BeFalse();
            command.Visible.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("cpp:S111")]
        [DataRow("c:S222")]
        [DataRow("javascript:S333")]
        [DataRow("typescript:S444")]
        [DataRow("css:S777")]
        [DataRow("Web:S787")]
        [DataRow("secrets:S555")]
        public void CheckStatusAndExecute_SingleIssue_SupportedRepo_StandaloneMode_VisibleAndEnabled(string errorCode)
        {
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper, LanguageProvider.Instance);

            // 1. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            result.Should().Be(VisibleAndEnabled);
            command.Enabled.Should().BeTrue();
            command.Visible.Should().BeTrue();

            // 2. Invoke the command
            command.Invoke();

            mockGlobalUserSettingsUpdater.Verify(x => x.DisableRule(errorCode), Times.Once);
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
        [DataRow("Web:S111", SonarLintMode.Connected, false)]
        [DataRow("Web:S111", SonarLintMode.LegacyConnected, false)]
        public void CheckStatus_SingleIssue_SupportedRepo_ConnectedMode_HasExpectedEnabledStatus(
            string errorCode,
            SonarLintMode bindingMode,
            bool expectedEnabled)
        {
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(bindingMode);
            var errorListHelper = CreateErrorListHelper(errorCode, ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper, LanguageProvider.Instance);

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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Connected);
            var errorListHelper = CreateErrorListHelper("secrets:S111", ruleExists: true);

            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper, LanguageProvider.Instance);

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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(mode);

            // Act
            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper, MockLanguageProvider().Object);

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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, testLogger, errorListHelper.Object, MockLanguageProvider().Object);

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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, testLogger, errorListHelper.Object, MockLanguageProvider().Object);
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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, testLogger, errorListHelper.Object, MockLanguageProvider().Object);

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
            var mockGlobalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(mockGlobalUserSettingsUpdater.Object, solutionTracker, testLogger, errorListHelper.Object, MockLanguageProvider().Object);
            Action act = () => command.Invoke();

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        [TestMethod]
        public void SupportedRepos_LanguagesInStandaloneModeAreSupported()
        {
            var languageProvider = new Mock<ILanguageProvider>();
            languageProvider.Setup(m => m.LanguagesInStandaloneMode).Returns([Language.CSharp, Language.Cpp,]); // does not have to be the real list
            languageProvider.Setup(m => m.RoslynLanguages).Returns([Language.CSharp]);

            var testSubject = CreateDisableRuleMenuCommand(languageProvider.Object);

            languageProvider.Verify(x => x.LanguagesInStandaloneMode, Times.Once);
            testSubject.SupportedRepos.Should().BeEquivalentTo([Language.CSharp.RepoInfo.Key, Language.Cpp.RepoInfo.Key]);
        }

        private static MenuCommand CreateDisableRuleMenuCommand(
            IGlobalRawSettingsService globalUserSettingsUpdater,
            IActiveSolutionBoundTracker solutionTracker,
            ILogger logger,
            IErrorListHelper errorListHelper,
            ILanguageProvider languageProvider)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new DisableRuleCommand(dummyMenuService, globalUserSettingsUpdater, solutionTracker, logger, errorListHelper, languageProvider);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        private static IActiveSolutionBoundTracker CreateSolutionTracker(SonarLintMode bindingMode)
        {
            var bindingConfiguration = new BindingConfiguration(new BoundServerProject("solution", "server project", new ServerConnection.SonarCloud("org")), bindingMode, null);
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

        private static Mock<ILanguageProvider> MockLanguageProvider()
        {
            var languageProviderMock = new Mock<ILanguageProvider>();
            languageProviderMock.Setup(m => m.LanguagesInStandaloneMode).Returns([]);
            languageProviderMock.Setup(m => m.RoslynLanguages).Returns([]);
            return languageProviderMock;
        }

        private static DisableRuleCommand CreateDisableRuleMenuCommand(ILanguageProvider languageProvider)
        {
            var dummyMenuService = new DummyMenuCommandService();
            var globalUserSettingsUpdater = new Mock<IGlobalRawSettingsService>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);
            var errorListHelper = new Mock<IErrorListHelper>();
            return new DisableRuleCommand(dummyMenuService, globalUserSettingsUpdater.Object, solutionTracker, new TestLogger(), errorListHelper.Object, languageProvider);
        }
    }
}
