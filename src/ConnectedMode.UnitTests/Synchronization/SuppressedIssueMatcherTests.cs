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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Synchronization
{
    [TestClass]
    public class SuppressedIssueMatcherTests
    {
        private SuppressedIssueMatcher testSubject;
        private Mock<IServerIssuesStore> mockServerIssuesStore;

        [TestInitialize]
        public void TestInitialize()
        {
            mockServerIssuesStore = new Mock<IServerIssuesStore>();
            testSubject = new SuppressedIssueMatcher(mockServerIssuesStore.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SuppressedIssueMatcher, IIssueMatcher>(
                MefTestHelpers.CreateExport<IServerIssuesStore>());
        }

        [TestMethod]
        public void MatchExists_NullIssue_Throws()
        {
            Action act = () => testSubject.Match(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issue");
        }

        [DataTestMethod]
        [DataRow("CorrectRuleId", 1, "CorrectHash", true)]    // exact matches
        [DataRow("correctRULEID", 1, "CorrectHash", true)]    // rule-id is case-insensitive
        [DataRow("CorrectRuleId", 1, "wrong hash", true)]   // matches on line
        [DataRow("CorrectRuleId", 9999, "CorrectHash", true)] // matches on hash only
        [DataRow("CorrectRuleId", 2, "correcthash", false)]   // hash is case-sensitive
        [DataRow("CorrectRuleId", 2, "wrong hash", false)]  // wrong line and hash
        [DataRow("CorrectRuleId", null, null, false)]       // server file issue
        [DataRow("wrong rule Id", 1, "CorrectHash", false)]
        public void MatchExists_LocalNonFileIssue_SingleServerIssue(string serverRuleId, int? serverIssueLine, string serverHash, bool expectedResult)
        {
            var issueToMatch = CreateIssueToMatch("CorrectRuleId", 1, "CorrectHash");
            var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);
            ConfigureServerIssues(serverIssue);

            // Act and assert
            testSubject.Match(issueToMatch).Should().Be(expectedResult ? serverIssue : null);
        }

        [DataTestMethod]
        [DataRow("CorrectRuleId", null, null, true)]      // exact matches
        [DataRow("CorrectRuleId", null, "hash", true)]    // hash should be ignored for file-level issues
        [DataRow("WrongRuleId", null, null, false)]     // wrong rule
        [DataRow("CorrectRuleId", 1, "hash", false)]      // not a file issue
        [DataRow("CorrectRuleId", 999, null, false)]      // not a file issue - should not match a file issue, even though the hash is the same
        public void MatchExists_LocalFileIssue_SingleServerIssue(string serverRuleId, int? serverIssueLine, string serverHash, bool expectedResult)
        {
            // File issues have line number of 0 and an empty hash
            var issueToMatch = CreateIssueToMatch("CorrectRuleId", null, null);
            var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);
            ConfigureServerIssues(serverIssue);

            // Act and assert
            testSubject.Match(issueToMatch).Should().Be(expectedResult ? serverIssue : null);
        }

        [TestMethod]
        public void MatchExists_NoServerIssues_ReturnsFalse()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("rule1", 1, "hash1");
            ConfigureServerIssues(Array.Empty<SonarQubeIssue>());

            // Act and assert
            testSubject.Match(issueToMatch).Should().BeNull();
        }

        [DataTestMethod]
        [DataRow("aaa", 222, "aaa hash", 0)]
        [DataRow("bbb", 333, "bbb hash", 1)]
        [DataRow("ccc", 444, "ccc hash", 2)]
        [DataRow("xxx", 111, "xxx hash", -1)]
        public void MatchExists_MultipleServerIssues(string localRuleId, int localIssueLine, string localHash, int expectedServerIssue)
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch(localRuleId, localIssueLine, localHash);

            var serverIssues = new []{CreateServerIssue("aaa", 222, "aaa hash"),
                CreateServerIssue("bbb", 333, "bbb hash"),
                CreateServerIssue("ccc", 444, "ccc hash")};

            ConfigureServerIssues(serverIssues);

            // Act and assert
            testSubject.Match(issueToMatch).Should().Be(expectedServerIssue >= 0 ? serverIssues[expectedServerIssue] : null);
        }

        [DataTestMethod]
        [DataRow("CorrectRuleId", null, null, true)]
        [DataRow("CorrectRuleId", null, null, false)]
        public void MatchExists_ResultDependsOnSuppressionState(string serverRuleId, int? serverIssueLine, string serverHash, bool expectedResult)
        {
            // File issues have line number of 0 and an empty hash
            var issueToMatch = CreateIssueToMatch("CorrectRuleId", null, null);
            var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);
            ConfigureServerIssues(serverIssue);

            // Act and assert
            testSubject.Match(issueToMatch).Should().Be(serverIssue);
        }

        [TestMethod]
        // Module-level issues i.e. no file
        [DataRow(null, null, true)]
        [DataRow(null, "", true)]
        [DataRow("", null, true)]
        [DataRow("", "", true)]

        // Module-level issues should not match non-module-level issues
        [DataRow(@"any.txt", "", false)]
        [DataRow(@"any.txt", null, false)]
        [DataRow("", @"c:\any.txt", false)]
        [DataRow(null, @"c:\any.txt", false)]
        
        // File issues
        [DataRow(@"same.txt", @"c:\same.txt", true)]
        [DataRow(@"SAME.TXT", @"c:\same.txt", true)]
        [DataRow(@"same.TXT", @"c:\XXXsame.txt", false)]  // partial file name -> should not match
        [DataRow(@"differentExt.123", @"a:\differentExt.999", false)] // different extension -> should not match
        [DataRow(@"aaa\partial\file.cs", @"d:\partial\file.cs", false)]
        // Only matching the local path tail, so the same server path can match multiple local files
        [DataRow(@"partial\file.cs", @"c:\aaa\partial\file.cs", true)]
        [DataRow(@"partial\file.cs", @"c:\aaa\bbb\partial\file.cs", true)]
        [DataRow(@"partial\file.cs", @"c:\aaa\bbb\ccc\partial\file.cs", true)]
        public void SuppressionExists_CheckFileComparisons(string serverFilePath, string localFilePath, bool expected)
        {
            var issueToMatch = CreateIssueToMatch("111", 0, "hash", filePath: localFilePath);

            var serverIssue = CreateServerIssue("111", 0, "hash", filePath: serverFilePath);
            ConfigureServerIssues(serverIssue);

            // Act and assert
            testSubject.Match(issueToMatch).Should().Be(expected ? serverIssue : null);
        }

        private IFilterableIssue CreateIssueToMatch(string ruleId, int? startLine, string lineHash,
            string filePath = null)
            => new TestFilterableIssue
            {
                RuleId = ruleId,
                StartLine = startLine,
                LineHash = lineHash,
                FilePath = filePath
            };

        private SonarQubeIssue CreateServerIssue(string ruleId, int? startLine, string lineHash,
            string filePath = null)
        {
            var sonarQubeIssue = new SonarQubeIssue(null, filePath, lineHash, null, null, ruleId, default, SonarQubeIssueSeverity.Info,
                 DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                 startLine.HasValue
                     ? new IssueTextRange(startLine.Value, 1, 1, 1)
                     : null,
                 flows: null);

            return sonarQubeIssue;
        }

        private void ConfigureServerIssues(
            params SonarQubeIssue[] issuesToReturn)
        {
            mockServerIssuesStore
                .Setup(x => x.Get()).Returns(issuesToReturn);
        }

        private class TestFilterableIssue : IFilterableIssue
        {
            public string RuleId { get; set; }
            public string LineHash { get; set; }
            public int? StartLine { get; set; }
            public string FilePath { get; set; }

            // Not expecting the other property to be used
            public string WholeLineText => throw new NotImplementedException();
        }
    }
}
