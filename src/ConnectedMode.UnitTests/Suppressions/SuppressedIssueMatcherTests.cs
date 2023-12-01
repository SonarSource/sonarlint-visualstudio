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
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class SuppressedIssueMatcherTests
    {
        private SuppressedIssueMatcher testSubject;
        private Mock<IServerIssuesStore> mockServerIssuesStore;
        private Mock<IIssueMatcher> issueMatcherMock;

        [TestInitialize]
        public void TestInitialize()
        {
            mockServerIssuesStore = new Mock<IServerIssuesStore>();
            issueMatcherMock = new Mock<IIssueMatcher>();
            testSubject = new SuppressedIssueMatcher(mockServerIssuesStore.Object, issueMatcherMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SuppressedIssueMatcher, ISuppressedIssueMatcher>(
                MefTestHelpers.CreateExport<IServerIssuesStore>(),
                MefTestHelpers.CreateExport<IIssueMatcher>());
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
            var issueToMatch = CreateIssueToMatch();
            ConfigureServerIssues(Array.Empty<SonarQubeIssue>());

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().BeFalse();
            
            issueMatcherMock.VerifyNoOtherCalls();
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(-1)]
        public void MatchExists_MultipleServerIssues(int indexOfServerIssue)
        {
            // Arrange
            var issueToMatch = CreateIssueToMatch();
            var sonarQubeIssues = new []
            {
                CreateServerIssue(),
                CreateServerIssue(),
                CreateServerIssue()
            };
            var hasMatch = indexOfServerIssue != -1;
            if (hasMatch)
            {
                issueMatcherMock.Setup(x => x.IsLikelyMatch(issueToMatch, sonarQubeIssues[indexOfServerIssue])).Returns(true);
            }

            ConfigureServerIssues(sonarQubeIssues);

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(hasMatch);
        }

        [DataTestMethod]
        [DataRow("CorrectRuleId", null, null, true, true)]
        [DataRow("CorrectRuleId", null, null, false, false)]
        public void MatchExists_ResultDependsOnSuppressionState(string serverRuleId, int? serverIssueLine,
            string serverHash, bool isSuppressed, bool expectedResult)
        {
            // File issues have line number of 0 and an empty hash
            var issueToMatch = CreateIssueToMatch();
            ConfigureServerIssues(CreateServerIssue(isSuppressed));
            issueMatcherMock
                .Setup(x => x.IsLikelyMatch(It.IsAny<IFilterableIssue>(), It.IsAny<SonarQubeIssue>()))
                .Returns(true);

            // Act and assert
            testSubject.SuppressionExists(issueToMatch).Should().Be(expectedResult);
        }

        private IFilterableIssue CreateIssueToMatch() => Mock.Of<IFilterableIssue>();

        private SonarQubeIssue CreateServerIssue(bool isSuppressed = true)
        {
            var sonarQubeIssue = new SonarQubeIssue(null, default, default, null, null, default, false, SonarQubeIssueSeverity.Info,
                 DateTimeOffset.MinValue, DateTimeOffset.MinValue,
                  null,
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
    }
}
