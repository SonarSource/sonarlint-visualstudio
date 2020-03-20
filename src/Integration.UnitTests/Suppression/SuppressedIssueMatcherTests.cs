/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressedIssueMatcherTests
    {
        private SuppressedIssueMatcher testSubject;
        private Mock<ISonarQubeIssuesProvider> mockIssuesProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            mockIssuesProvider = new Mock<ISonarQubeIssuesProvider>();
            testSubject = new SuppressedIssueMatcher(mockIssuesProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullProvider_Throws()
        {
            Action act = () => new SuppressedIssueMatcher(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issuesProvider");
        }

        [TestMethod]
        public void MatchExists_NullIssue_Throws()
        {
            Action act = () => testSubject.SuppressionExists(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issue");
        }

        [DataTestMethod]
        [DataRow("RightRuleId", 1, "RightHash", true)]    // exact matches
        [DataRow("rightRULEID", 1, "RightHash", true)]    // rule-id is case-insensitive
        [DataRow("RightRuleId", 1, "wrong hash", true)]   // matches on line
        [DataRow("RightRuleId", 9999, "RightHash", true)] // matches on hash only

        [DataRow("RightRuleId", 2, "righthash", false)]   // hash is case-sensitive
        [DataRow("RightRuleId", 2, "wrong hash", false)]  // wrong line and hash
        [DataRow("RightRuleId", null, null, false)]       // server file issue
        [DataRow("wrong rule Id", 1, "RightHash", false)]
        public void MatchExists_LocalNonFileIssue_SingleServerIssue(string serverRuleId, int? serverIssueLine, string serverHash, bool expectedResult)
        {
            var issueToMatch = CreateIssueToMatch("RightRuleId", 1, "RightHash");
            ConfigureServerIssues(issueToMatch, CreateServerIssue(serverRuleId, serverIssueLine, serverHash));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        [DataTestMethod]
        [DataRow("RightRuleId", null, null, true)]      // exact matches
        [DataRow("RightRuleId", null, "hash", true)]    // hash should be ignored for file-level issues
        [DataRow("WrongRuleId", null, null, false)]     // wrong rule
        [DataRow("RightRuleId", 1, "hash", false)]      // not a file issue
        [DataRow("RightRuleId", 999, null, false)]      // not a file issue - should not match a file issue, even though the hash is the same
        public void MatchExists_LocalFileIssue_SingleServerIssue(string serverRuleId, int? serverIssueLine, string serverHash, bool expectedResult)
        {
            // File issues have line number of 0 and an empty hash
            var issueToMatch = CreateIssueToMatch("RightRuleId", null, null);
            ConfigureServerIssues(issueToMatch, CreateServerIssue(serverRuleId, serverIssueLine, serverHash));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        [TestMethod]
        public void MatchExists_NoServerIssues_ReturnsFalse()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("rule1", 1, "hash1");
            ConfigureServerIssues(issueToMatch, Array.Empty<SonarQubeIssue>());

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

            ConfigureServerIssues(issueToMatch,
                CreateServerIssue("aaa", 222, "aaa hash"),
                CreateServerIssue("bbb", 333, "bbb hash"),
                CreateServerIssue("ccc", 444, "ccc hash"));

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        private IFilterableIssue CreateIssueToMatch(string ruleId, int? startLine, string lineHash)
            => new TestFilterableIssue
            {
                RuleId = ruleId,
                StartLine = startLine,
                LineHash = lineHash,
                ProjectGuid = "well known project guid",
                FilePath = "well known file path"
            };

        private SonarQubeIssue CreateServerIssue(string ruleId, int? startLine, string lineHash)
            => new SonarQubeIssue(null, lineHash, startLine, null, null, ruleId, false);

        private void ConfigureServerIssues(
            IFilterableIssue issueToMatch,
            params SonarQubeIssue[] issuesToReturn)
        {
            mockIssuesProvider
                .Setup(x => x.GetSuppressedIssues(issueToMatch.ProjectGuid, issueToMatch.FilePath)).Returns(issuesToReturn);
        }

        private class TestFilterableIssue : IFilterableIssue
        {
            public string RuleId { get; set; }
            public string LineHash { get; set; }
            public int? StartLine { get; set; }
            public string FilePath { get; set; }
            public string ProjectGuid { get; set; }

            // Not expecting the other property to be used
            public string WholeLineText => throw new NotImplementedException();
        }
    }
}
