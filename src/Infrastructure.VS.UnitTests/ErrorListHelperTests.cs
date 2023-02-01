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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ErrorListHelperTests
    {
        [TestMethod]
        [DataRow("S666", "cs", "S666", "SonarAnalyzer.CSharp")]
        [DataRow("S666", "vb", "S666", "SonarAnalyzer.VisualBasic")]
        [DataRow("S234", "vb", "S234", "SonarAnalyzer.VisualBasic")]
        [DataRow("c:S111", "c", "S111", "SonarLint")]
        [DataRow("cpp:S222", "cpp", "S222", "SonarLint")]
        [DataRow("javascript:S333", "javascript", "S333", "SonarLint")]
        [DataRow("typescript:S444", "typescript", "S444", "SonarLint")]
        [DataRow("secrets:S555", "secrets", "S555", "SonarLint")]
        [DataRow("foo:bar", "foo", "bar", "SonarLint")]
        public void GetErrorCode_SingleSonarIssue_ErrorCodeReturned(string fullRuleKey, string expectedRepo, string expectedRule, string buildTool)
        {
            // Arrange
            var issueHandle = CreateIssueHandle(111, new Dictionary<string, object>
            {
                { StandardTableKeyNames.BuildTool, buildTool },
                { StandardTableKeyNames.ErrorCode, fullRuleKey }
            });

            var mockErrorList = CreateErrorList(issueHandle);

            // Act
            var testSubject = new ErrorListHelper();
            bool result = testSubject.TryGetRuleIdFromSelectedRow(mockErrorList, out var ruleId);

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
            var testSubject = new ErrorListHelper();
            bool result = testSubject.TryGetRuleIdFromSelectedRow(mockErrorList, out var errorCode);

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
                { StandardTableKeyNames.BuildTool, "SonarLint.CSharp" },
                { StandardTableKeyNames.ErrorCode, "cs:S222" }
            });

            var mockErrorList = CreateErrorList(cppIssueHandle, jsIssueHandle);

            // Act
            var testSubject = new ErrorListHelper();
            bool result = testSubject.TryGetRuleIdFromSelectedRow(mockErrorList, out var errorCode);

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
            var testSubject = new ErrorListHelper();
            bool result = testSubject.TryGetRuleIdFromSelectedRow(mockErrorList, out var errorCode);

            // Assert
            result.Should().BeFalse();
            errorCode.Should().BeNull();
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
