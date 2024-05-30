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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ErrorListHelperTests
    {
        internal enum TestVsSuppressionState
        {
            Active,
            Suppressed,
            NotApplicable,
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ErrorListHelper, IErrorListHelper>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        => MefTestHelpers.CheckIsSingletonMefComponent<ErrorListHelper>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            _ = new ErrorListHelper(serviceOp.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void TryGetIssueFromSelectedRow_SingleSonarIssue_IssueReturned()
        {
            var issueMock = Mock.Of<IAnalysisIssueVisualization>();
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { SonarLintTableControlConstants.IssueVizColumnName, issueMock }
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetIssueFromSelectedRow(out var issue);

            result.Should().BeTrue();
            issue.Should().BeSameAs(issueMock);
        }

        [TestMethod]
        public void TryGetIssueFromSelectedRow_SingleItemButNoAnalysisIssue_IssueNotReturned()
        {
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { SonarLintTableControlConstants.IssueVizColumnName, null }
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            var result = testSubject.TryGetIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetIssueFromSelectedRow_MultipleItemsSelected_IssueNotReturned()
        {
            var cppIssueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S222" },
                { SonarLintTableControlConstants.IssueVizColumnName, Mock.Of<IAnalysisIssueVisualization>() }
            });
            var jsIssueHandle = CreateIssueHandle(222, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint.CSharp" },
                { StandardTableKeyNames.ErrorCode, "csharpsquid:S222" },
                { SonarLintTableControlConstants.IssueVizColumnName, Mock.Of<IAnalysisIssueVisualization>() }
            });

            var errorList = CreateErrorList(cppIssueHandle, jsIssueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            var result = testSubject.TryGetIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetRoslynIssueFromSelectedRow_SingleRoslynIssue_IssueReturned()
        {
            var path = "filepath";
            var line = 12;
            var column = 101;
            var errorCode = "javascript:S333";
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, errorCode},
                { StandardTableKeyNames.DocumentName, path },
                { StandardTableKeyNames.Line, line },
                { StandardTableKeyNames.Column, column }
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRoslynIssueFromSelectedRow(out var issue);

            result.Should().BeTrue();
            issue.RuleId.Should().BeSameAs(errorCode);
            issue.FilePath.Should().BeSameAs(path);
            issue.StartLine.Should().Be(line + 1);
            issue.RoslynStartLine.Should().Be(line + 1);
            issue.RoslynStartColumn.Should().Be(column + 1);
            issue.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void TryGetRoslynIssueFromSelectedRow_NonSonarIssue_NothingReturned()
        {
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "Not SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { StandardTableKeyNames.DocumentName, "filepath" },
                { StandardTableKeyNames.Line, 1 },
                { StandardTableKeyNames.Column, 2 }
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRoslynIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetRoslynIssueFromSelectedRow_NoFilePath_NothingReturned()
        {
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { StandardTableKeyNames.Line, 1 },
                { StandardTableKeyNames.Column, 2 }
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRoslynIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetRoslynIssueFromSelectedRow_NoStartLine_NothingReturned()
        {
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { StandardTableKeyNames.DocumentName, "filepath" },
                { StandardTableKeyNames.Column, 2 },
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRoslynIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetRoslynIssueFromSelectedRow_NoStartColumn_NothingReturned()
        {
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "javascript:S333"},
                { StandardTableKeyNames.DocumentName, "filepath" },
                { StandardTableKeyNames.Line, 1 },
            });
            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRoslynIssueFromSelectedRow(out _);

            result.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("S666", "csharpsquid", "S666", "SonarAnalyzer.CSharp", null)]
        [DataRow("S666", "vbnet", "S666", "SonarAnalyzer.VisualBasic", null)]
        [DataRow("S234", "vbnet", "S234", "SonarAnalyzer.VisualBasic", null)]
        [DataRow("c:S111", "c", "S111", "SonarLint", null)]
        [DataRow("cpp:S222", "cpp", "S222", "SonarLint", null)]
        [DataRow("javascript:S333", "javascript", "S333", "SonarLint", null)]
        [DataRow("typescript:S444", "typescript", "S444", "SonarLint", null)]
        [DataRow("secrets:S555", "secrets", "S555", "SonarLint", null)]
        [DataRow("foo:bar", "foo", "bar", "SonarLint", null)]
        [DataRow("S666", "csharpsquid", "S666", null, "https://rules.sonarsource.com/csharp/RSPEC-666/")]
        [DataRow("S666", "vbnet", "S666", null, "https://rules.sonarsource.com/vbnet/RSPEC-666/")]
        [DataRow("S234", "vbnet", "S234", null, "https://rules.sonarsource.com/vbnet/RSPEC-234/")]
        public void TryGetRuleIdFromSelectedRow_SingleSonarIssue_ErrorCodeReturned(string fullRuleKey, string expectedRepo, string expectedRule, string buildTool, string helpLink)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, buildTool },
                { StandardTableKeyNames.HelpLink, helpLink },
                { StandardTableKeyNames.ErrorCode, fullRuleKey }
            });

            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdFromSelectedRow(out var ruleId);

            // Assert
            result.Should().BeTrue();
            ruleId.RepoKey.Should().Be(expectedRepo);
            ruleId.RuleKey.Should().Be(expectedRule);
        }

        [TestMethod]
        [DataRow("S666", "csharpsquid", "S666", "SonarAnalyzer.CSharp", null)]
        [DataRow("S666", "vbnet", "S666", "SonarAnalyzer.VisualBasic", null)]
        [DataRow("S234", "vbnet", "S234", "SonarAnalyzer.VisualBasic", null)]
        [DataRow("c:S111", "c", "S111", "SonarLint", null)]
        [DataRow("cpp:S222", "cpp", "S222", "SonarLint", null)]
        [DataRow("javascript:S333", "javascript", "S333", "SonarLint", null)]
        [DataRow("typescript:S444", "typescript", "S444", "SonarLint", null)]
        [DataRow("secrets:S555", "secrets", "S555", "SonarLint", null)]
        [DataRow("foo:bar", "foo", "bar", "SonarLint", null)]
        [DataRow("S666", "csharpsquid", "S666", null, "https://rules.sonarsource.com/csharp/RSPEC-666/")]
        [DataRow("S666", "vbnet", "S666", null, "https://rules.sonarsource.com/vbnet/RSPEC-666/")]
        [DataRow("S234", "vbnet", "S234", null, "https://rules.sonarsource.com/vbnet/RSPEC-234/")]
        public void TryGetRuleId_FromHandle_ErrorCodeReturned(string fullRuleKey, string expectedRepo, string expectedRule, string buildTool, string helpLink)
        {
            // Note: this is a copy of TryGetRuleIdFromSelectedRow_SingleSonarIssue_ErrorCodeReturned,
            //       but without the serviceProvider and IErrorList setup

            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, buildTool },
                { StandardTableKeyNames.HelpLink, helpLink },
                { StandardTableKeyNames.ErrorCode, fullRuleKey }
            });

            // Act
            var testSubject = new ErrorListHelper(Mock.Of<IVsUIServiceOperation>());
            bool result = testSubject.TryGetRuleId(issueHandle, out var ruleId);

            // Assert
            result.Should().BeTrue();
            ruleId.RepoKey.Should().Be(expectedRepo);
            ruleId.RuleKey.Should().Be(expectedRule);
        }

        [TestMethod]
        public void TryGetRuleIdFromSelectedRow_NonStandardErrorCode_NoException_ErrorCodeNotReturned()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, ":" } // should not happen
            });

            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdFromSelectedRow(out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        public void TryGetRuleIdFromSelectedRow_MultipleItemsSelected_ErrorCodeNotReturned()
        {
            var cppIssueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S222" }
            });
            var jsIssueHandle = CreateIssueHandle(222, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint.CSharp" },
                { StandardTableKeyNames.ErrorCode, "csharpsquid:S222" }
            });

            var errorList = CreateErrorList(cppIssueHandle, jsIssueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdFromSelectedRow(out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        public void TryGetRuleIdFromSelectedRow_NotSonarLintIssue()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, new object() },
                { StandardTableKeyNames.ErrorCode, "cpp:S333" }
            });

            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdFromSelectedRow(out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        [DataRow("cpp:S333", "AnotherAnalyzer", null)]
        [DataRow("S666", "AnotherAnalyzerWithSonarHelpLink", "https://rules.sonarsource.com/csharp/RSPEC-666/")]
        [DataRow("S234",  "SomeOtherAnalyzer", "https://rules.sonarsource.com/vbnet/RSPEC-234/")]
        public void TryGetRuleId_FromHandle_NotSonarLintIssue(string fullRuleKey, object buildTool, string helpLink)
        {
            // Note: this is a copy of TryGetRuleIdFromSelectedRow_SingleSonarIssue_ErrorCodeReturned,
            //       but without the serviceProvider and IErrorList setup

            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, buildTool },
                { StandardTableKeyNames.HelpLink, helpLink },
                { StandardTableKeyNames.ErrorCode, fullRuleKey }
            });

            // Act
            var testSubject = new ErrorListHelper(Mock.Of<IVsUIServiceOperation>());
            bool result = testSubject.TryGetRuleId(issueHandle, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
        }

        [TestMethod]
        public void TryGetRuleIdAndSuppressionStateFromSelectedRow_NoSuppressionState_ReturnsIsNotSuppressed()
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S222" }
            });

            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdAndSuppressionStateFromSelectedRow(out var ruleId, out var isSuppressed);

            // Assert
            result.Should().BeTrue();
            isSuppressed.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow(SuppressionState.Suppressed, true)]
        [DataRow(SuppressionState.NotApplicable, false)]
        [DataRow(SuppressionState.Active, false)]
        public void TryGetRuleIdAndSuppressionStateFromSelectedRow_NoSuppressionState_ReturnsIsNotSuppressed(SuppressionState suppressionState, bool expectedSuppression)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, "SonarLint" },
                { StandardTableKeyNames.ErrorCode, "cpp:S222" },
                { StandardTableKeyNames.SuppressionState, suppressionState },
            });

            var errorList = CreateErrorList(issueHandle);
            var serviceProvider = CreateServiceOperation(errorList);

            // Act
            var testSubject = new ErrorListHelper(serviceProvider);
            bool result = testSubject.TryGetRuleIdAndSuppressionStateFromSelectedRow(out var ruleId, out var isSuppressed);

            // Assert
            result.Should().BeTrue();
            isSuppressed.Should().Be(expectedSuppression);
        }

        private IVsUIServiceOperation CreateServiceOperation(IErrorList svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsErrorList, IErrorList, bool>(It.IsAny<Func<IErrorList, bool>>()))
                .Returns<Func<IErrorList, bool>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }

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
            var issueSnapshot = (ITableEntriesSnapshot)new DummySnapshot
            {
                Index = index,
                Properties = issueProperties
            };

            var mockHandle = new Mock<ITableEntryHandle>();
            mockHandle.Setup(x => x.TryGetSnapshot(out issueSnapshot, out index)).Returns(true);
            return mockHandle.Object;
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
