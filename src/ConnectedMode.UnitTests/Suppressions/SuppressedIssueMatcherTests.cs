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
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
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
        public void Ctor_NullProvider_Throws()
        {
            Action act = () => new SuppressedIssueMatcher(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverIssuesStore");
        }

        [TestMethod]
        public void MatchExists_NullIssue_Throws()
        {
            Action act = () => testSubject.SuppressionExists(null);
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
            ConfigureServerIssues(CreateServerIssue(serverRuleId, serverIssueLine, serverHash, isSuppressed: true));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
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
            ConfigureServerIssues(CreateServerIssue(serverRuleId, serverIssueLine, serverHash, expectedResult));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        [TestMethod]
        public void MatchExists_NoServerIssues_ReturnsFalse()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("rule1", 1, "hash1");
            ConfigureServerIssues(Array.Empty<SonarQubeIssue>());

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow("aaa", 222, "aaa hash", true)]
        [DataRow("bbb", 333, "bbb hash", true)]
        [DataRow("ccc", 444, "ccc hash", true)]
        [DataRow("xxx", 111, "xxx hash", false)]
        public void MatchExists_MultipleServerIssues(string localRuleId, int localIssueLine, string localHash, bool expectedResult)
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch(localRuleId, localIssueLine, localHash);

            ConfigureServerIssues(
                CreateServerIssue("aaa", 222, "aaa hash", isSuppressed: true),
                CreateServerIssue("bbb", 333, "bbb hash", isSuppressed: true),
                CreateServerIssue("ccc", 444, "ccc hash", isSuppressed: true));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        [DataTestMethod]
        [DataRow("CorrectRuleId", null, null, true, true)]
        [DataRow("CorrectRuleId", null, null, false, false)]
        public void MatchExists_ResultDependsOnSuppressionState(string serverRuleId, int? serverIssueLine, string serverHash, bool isSuppressed, bool expectedResult)
        {
            // File issues have line number of 0 and an empty hash
            var issueToMatch = CreateIssueToMatch("CorrectRuleId", null, null);
            ConfigureServerIssues(CreateServerIssue(serverRuleId, serverIssueLine, serverHash, isSuppressed));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        private IFilterableIssue CreateIssueToMatch(string ruleId, int? startLine, string lineHash)
            => new TestFilterableIssue
            {
                RuleId = ruleId,
                StartLine = startLine,
                LineHash = lineHash,
                FilePath = "well known file path"
            };

        private SonarQubeIssue CreateServerIssue(string ruleId, int? startLine, string lineHash, bool isSuppressed)
        {
            var sonarQubeIssue = new SonarQubeIssue(null, null, lineHash, null, null, ruleId, false, SonarQubeIssueSeverity.Info,
                 DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                 startLine.HasValue
                     ? new IssueTextRange(startLine.Value, 1, 1, 1)
                     : null,
                 flows: null);

            sonarQubeIssue.IsResolved = isSuppressed;

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
