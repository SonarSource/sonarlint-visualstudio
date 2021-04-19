/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
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
            var errorList = CreateErrorList();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = new Mock<IActiveSolutionBoundTracker>().Object;
            var logger = new TestLogger();

            Action act = () => new DisableRuleCommand(null, errorList, userSettingsProvider, solutionTracker, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, userSettingsProvider, solutionTracker, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorList");

            act = () => new DisableRuleCommand(menuCommandService, errorList, null, solutionTracker, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsProvider");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, null, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, solutionTracker, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorList = CreateErrorList();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, userSettingsProvider, solutionTracker, new TestLogger());

            // Assert
            command.CommandID.ID.Should().Be(DisableRuleCommand.CommandId);
            command.CommandID.Guid.Should().Be(DisableRuleCommand.CommandSet);
        }

        [TestMethod]
        [DataRow("cpp:S111")]
        [DataRow("c:S222")]
        [DataRow("javascript:S333")]
        public void CheckStatusAndExecute_SingleIssue_SupportedRepo_StandaloneMode_VisibleAndEnabled(string errorCode)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode,  errorCode}
            });
            var errorList = CreateErrorList(issueHandle);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger());

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
        [DataRow("javascript:S111", SonarLintMode.Connected, true)]
        [DataRow("javascript:S111", SonarLintMode.LegacyConnected, true)]
        public void CheckStatus_SingleIssue_SupportedRepo_ConnectedMode_HasExpectedEnabledStatus(string errorCode, SonarLintMode bindingMode,
            bool expectedEnabled)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, errorCode }
            });
            var errorList = CreateErrorList(issueHandle);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(bindingMode);

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger());

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
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "unsupportedRepo:S123" }
            });
            var errorList = CreateErrorList(issueHandle);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(mode);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, solutionTracker, new TestLogger());

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
            var errorList = new Mock<IErrorList>();
            errorList.Setup(x => x.TableControl).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            // Act
            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, solutionTracker, testLogger);

            // Act - should not throw
            var _ = command.OleStatus;

            testLogger.AssertPartialOutputStringExists("exception xxx");
        }

        [TestMethod]
        public void QueryStatus_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorList = new Mock<IErrorList>();
            errorList.Setup(x => x.TableControl).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, solutionTracker, testLogger);
            Action act = () => _ = command.OleStatus;

            // Act 
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        [TestMethod]
        public void Execute_NonCriticalErrorSuppressed()
        {
            // Arrange
            var errorList = new Mock<IErrorList>();
            errorList.Setup(x => x.TableControl).Throws(new InvalidOperationException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, solutionTracker, testLogger);

            // Act - should not throw
            command.Invoke();

            testLogger.AssertPartialOutputStringExists("exception xxx", AnalysisStrings.DisableRule_UnknownErrorCode);
        }

        [TestMethod]
        public void Execute_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorList = new Mock<IErrorList>();
            errorList.Setup(x => x.TableControl).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();
            var solutionTracker = CreateSolutionTracker(SonarLintMode.Standalone);

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, solutionTracker, testLogger);
            Action act = () => command.Invoke();

            // Act 
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateDisableRuleMenuCommand(IErrorList errorList, IUserSettingsProvider userSettingsProvider, IActiveSolutionBoundTracker solutionTracker, ILogger logger)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new DisableRuleCommand(dummyMenuService, errorList, userSettingsProvider, solutionTracker, logger);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        #region TryGetErrorCode tests

        [TestMethod]
        [DataRow("c:S111", "c", "S111")]
        [DataRow("cpp:S222", "cpp", "S222")]
        [DataRow("javascript:S333", "javascript", "S333")]
        [DataRow("foo:bar", "foo", "bar")]
        public void GetErrorCode_SingleSonarIssue_ErrorCodeReturned(string fullRuleKey, string expectedRepo, string expectedRule)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, fullRuleKey }
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetRuleId(mockErrorList, out var ruleId);

            // Assert
            result.Should().BeTrue();
            ruleId.RepoKey.Should().Be(expectedRepo);
            ruleId.RuleKey.Should().Be(expectedRule);
        }

        [TestMethod]
        public void GetErrorCode_NonStandardErrorCode_NoException_ErrorCodeNotReturned()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, ":" } // should not happen
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetRuleId(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        public void GetErrorCode_MultipleItemsSelected_ErrorCodeNotReturned()
        {
            var cppIssueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S222" }
            });
            var jsIssueHandle = CreateIssueHandle(222, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S222" }
            });

            var mockErrorList = CreateErrorList(cppIssueHandle, jsIssueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetRuleId(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        public void GetErrorCode_NotSonarLintIssue()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, new object() },
                { StandardTableKeyNames.ErrorCode, "cpp:S333" }
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetRuleId(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        #endregion TryGetErrorCode tests

        private static IErrorList CreateErrorList(params ITableEntryHandle[] entries)
        {
            var mockWpfTable = new Mock<IWpfTableControl>();
            mockWpfTable.Setup(x => x.SelectedEntries).Returns(entries);

            var mockErrorList = new Mock<IErrorList>();
            mockErrorList.Setup(x => x.TableControl).Returns(mockWpfTable.Object);
            return mockErrorList.Object;
        }

        private static ITableEntryHandle CreateIssueHandle(int index, IDictionary<string, object> issueProperties)
        {
            // Snapshots would normally have multiple versions; each version would have a unique
            // index, with a corresponding handle.
            // Here, just create a dummy snapshot with a single version using the specified index
            var issueSnapshot = (ITableEntriesSnapshot) new DummySnapshot
            {
                Index = index,
                Properties = issueProperties
            };

            var mockHandle = new Mock<ITableEntryHandle>();
            mockHandle.Setup(x => x.TryGetSnapshot(out issueSnapshot, out index)).Returns(true);
            return mockHandle.Object;
        }

        private static IActiveSolutionBoundTracker CreateSolutionTracker(SonarLintMode bindingMode)
        {
            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), bindingMode, null);
            var tracker = new Mock<IActiveSolutionBoundTracker>();
            tracker.Setup(x => x.CurrentConfiguration).Returns(bindingConfiguration);
            return tracker.Object;
        }

        #region Helper classes

        private sealed class DummySnapshot : ITableEntriesSnapshot
        {
            public int Index { get; set; }
            public IDictionary<string, object> Properties { get; set; }

            #region ITableEntriesSnapshot methods

            public int Count => throw new NotImplementedException();
            public int VersionNumber => throw new NotImplementedException();
            public void Dispose() => throw new NotImplementedException();
            public int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot) => throw new NotImplementedException();
            public void StartCaching() => throw new NotImplementedException();
            public void StopCaching() => throw new NotImplementedException();

            public bool TryGetValue(int index, string keyName, out object content)
            {
                if (index == Index)
                {
                    return Properties.TryGetValue(keyName, out content);
                }
                content = null;
                return false;
            }

            #endregion ITableEntriesSnapshot methods
        }

        #endregion Helper classes
    }
}
