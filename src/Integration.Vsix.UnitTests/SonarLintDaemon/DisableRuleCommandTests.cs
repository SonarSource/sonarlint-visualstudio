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
using System.Collections.Generic;
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DisableRuleCommandTests
    {
        [TestMethod]
        public void Ctor_NullArguments()
        {
            var menuCommandService = new DummyMenuCommandService();
            var errorList = CreateErrorList();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            var logger = new TestLogger();

            Action act = () => new DisableRuleCommand(null, errorList, userSettingsProvider, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new DisableRuleCommand(menuCommandService, null, userSettingsProvider, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("errorList");

            act = () => new DisableRuleCommand(menuCommandService, errorList, null, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsProvider");

            act = () => new DisableRuleCommand(menuCommandService, errorList, userSettingsProvider, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            // Arrange
            var errorList = CreateErrorList();
            var userSettingsProvider = new Mock<IUserSettingsProvider>().Object;
            
            // Act
            var command = CreateDisableRuleMenuCommand(errorList, userSettingsProvider, new TestLogger());
            
            // Assert
            command.CommandID.ID.Should().Be(DisableRuleCommand.CommandId);
            command.CommandID.Guid.Should().Be(DisableRuleCommand.CommandSet);
        }

        [TestMethod]
        public void CheckStatusAndExecute_SingleCFamilyIssue()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S123" }
            });
            var errorList = CreateErrorList(issueHandle);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, new TestLogger());

            // 1. Trigger the query status check
            ThreadHelper.SetCurrentThreadAsUIThread();
            var result = command.OleStatus;

            result.Should().Be((int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED));
            command.Enabled.Should().BeTrue();
            command.Visible.Should().BeTrue();

            // 2. Invoke the command
            command.Invoke();

            mockUserSettingsProvider.Verify(x => x.DisableRule("cpp:S123"), Times.Once);
        }

        [TestMethod]
        public void CheckStatusAndExecute_NotACFamilyIssue()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "xxx:S123" }
            });
            var errorList = CreateErrorList(issueHandle);
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();

            var command = CreateDisableRuleMenuCommand(errorList, mockUserSettingsProvider.Object, new TestLogger());

            // 1. Trigger the query status check
            var result = command.OleStatus;
            result.Should().Be((int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE));

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

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, testLogger);

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

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, testLogger);
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

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, testLogger);

            // Act - should not throw
            command.Invoke();

            testLogger.AssertPartialOutputStringExists("exception xxx", DaemonStrings.DisableRule_UnknownErrorCode);
        }

        [TestMethod]
        public void Execute_CriticalErrorNotSuppressed()
        {
            // Arrange
            var errorList = new Mock<IErrorList>();
            errorList.Setup(x => x.TableControl).Throws(new StackOverflowException("exception xxx"));

            var testLogger = new TestLogger();
            var mockUserSettingsProvider = new Mock<IUserSettingsProvider>();

            var command = CreateDisableRuleMenuCommand(errorList.Object, mockUserSettingsProvider.Object, testLogger);
            Action act = () => command.Invoke();

            // Act 
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");

            testLogger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateDisableRuleMenuCommand(IErrorList errorList, IUserSettingsProvider userSettingsProvider, ILogger logger)
        {
            var dummyMenuService = new DummyMenuCommandService();
            var testSubject = new DisableRuleCommand(dummyMenuService, errorList, userSettingsProvider, logger);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        #region TryGetErrorCode tests

        [TestMethod]
        public void GetErrorCode_SingleCppIssue_ErrorCodeReturned()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S123" }
            });
            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeTrue();
            errorCode.Should().Be("cpp:S123");
        }

        [TestMethod]
        public void GetErrorCode_SingleCIssue_ErrorCodeReturned()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "c:S333" }
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeTrue();
            errorCode.Should().Be("c:S333");
        }

        [TestMethod]
        public void GetErrorCode_SingleJavaScriptIssue_ErrorCodeNotReturned()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S123" }
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
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
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

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
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

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
            bool result = DisableRuleCommand.TryGetErrorCodeSync(mockErrorList, out var errorCode);

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

        #region Helper classes

        private class DummySnapshot : ITableEntriesSnapshot
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

        private class DummyMenuCommandService : IMenuCommandService
        {
            public IList<MenuCommand> AddedMenuCommands { get; } = new List<MenuCommand>();

            #region IMenuCommandService methods 

            public DesignerVerbCollection Verbs => throw new NotImplementedException();

            public void AddCommand(MenuCommand command) => AddedMenuCommands.Add(command);

            public void AddVerb(DesignerVerb verb) => throw new NotImplementedException();
            public MenuCommand FindCommand(CommandID commandID) => throw new NotImplementedException();
            public bool GlobalInvoke(CommandID commandID) => throw new NotImplementedException();
            public void RemoveCommand(MenuCommand command) => throw new NotImplementedException();
            public void RemoveVerb(DesignerVerb verb) => throw new NotImplementedException();
            public void ShowContextMenu(CommandID menuID, int x, int y) => throw new NotImplementedException();

            #endregion IMenuCommandService methods 
        }

        #endregion Helper classes
    }
}
