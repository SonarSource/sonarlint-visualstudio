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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressionIssueMatcherTests
    {
        private SuppressionIssueMatcher testSubject;
        private Mock<ISonarQubeIssuesProvider> mockIssuesProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            mockIssuesProvider = new Mock<ISonarQubeIssuesProvider>();
            testSubject = new SuppressionIssueMatcher(mockIssuesProvider.Object);
        }

        [TestMethod]
        public void Ctor_NullProvider_Throws()
        {
            Action act = () => new SuppressionIssueMatcher(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issuesProvider");
        }

        [TestMethod]
        public void MatchExists_NullIssue_Throws()
        {
            Action act = () => testSubject.SuppressionExists(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issue");
        }

        [TestMethod]
        public void MatchExists_NoServerIssues_ReturnsFalse()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("rule1", 1, "hash1");
            ConfigureServerIssues(issueToMatch /* no server issues */);

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeFalse();
        }

        [TestMethod]
        public void MatchExists_NoMatches_ReturnsFalse()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("RightRuleId", 1, "RightHash");

            ConfigureServerIssues(issueToMatch,
                CreateServerIssue("WrongRuleId", 1, "RightHash"),  // wrong rule id
                CreateServerIssue("RightRuleId", 2, "wrong hash"), // wrong line and hash
                CreateServerIssue("RightRuleId", 3, "RIGHTHASH")); // wrong hash and wrong-case hash

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeFalse();
        }

        [TestMethod]
        public void MatchExists_MatchingIdAndLine_ReturnsTrue()
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch("RightRuleId", 1, "RightHash");
            ConfigureServerIssues(issueToMatch,
                CreateServerIssue("wrong rule ID", 2, "RightHash"),
                CreateServerIssue("RIGHTRULEID", 1, "WrongHash") // rule id comparison is case-insensitive -> match on line
                );

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeTrue();
        }

        [TestMethod]
        public void MatchExists_MatchingIdAndLineHash_ReturnsTrue()
        {
            var issueToMatch = CreateIssueToMatch("YYY", 999, "correct hash");
            ConfigureServerIssues(issueToMatch,
                CreateServerIssue("YYY", 1, "incorrect hash"),      // wrong line and hash
                CreateServerIssue("xxx", 999, "wrong hash"),        // wrong rule
                CreateServerIssue("YYY", 9999, "incorrect hash"),   // wrong hash
                CreateServerIssue("yyy", 9999999, "correct hash")); // rule id comparison is case-insensitive -> match on hash

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeTrue();
        }

        private IFilterableIssue CreateIssueToMatch(string ruleId, int startLine, string lineHash)
            => new TestFilterableIssue
            {
                RuleId = ruleId,
                StartLine = startLine,
                LineHash = lineHash,
                ProjectGuid = "well known project guid",
                FilePath = "well known file path"
            };

        private SonarQubeIssue CreateServerIssue(string ruleId, int startLine, string lineHash)
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
