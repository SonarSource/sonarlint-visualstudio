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

using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class IssueMatcherTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<IssueMatcher, IIssueMatcher>();
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<IssueMatcher>();
    }
    
    [DataTestMethod]
    [DataRow("CorrectRuleId", 1, "CorrectHash", true)] // exact matches
    [DataRow("correctRULEID", 1, "CorrectHash", true)] // rule-id is case-insensitive
    [DataRow("CorrectRuleId", 1, "wrong hash", true)] // matches on line
    [DataRow("CorrectRuleId", 9999, "CorrectHash", true)] // matches on hash only
    [DataRow("CorrectRuleId", 2, "correcthash", false)] // hash is case-sensitive
    [DataRow("CorrectRuleId", 2, "wrong hash", false)] // wrong line and hash
    [DataRow("CorrectRuleId", null, null, false)] // server file issue
    [DataRow("wrong rule Id", 1, "CorrectHash", false)]
    public void IsMatch_MatchesBasedOnAllParameters(string serverRuleId, int? serverIssueLine,
        string serverHash, bool expectedResult)
    {
        var issueToMatch = CreateIssueToMatch("CorrectRuleId", 1, "CorrectHash");
        var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);

        CreateTestSubject().IsLikelyMatch(issueToMatch, serverIssue).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow("CorrectRuleId", null, null, true)] // exact matches
    [DataRow("CorrectRuleId", null, "hash", true)] // hash should be ignored for file-level issues
    [DataRow("WrongRuleId", null, null, false)] // wrong rule
    [DataRow("CorrectRuleId", 1, "hash", false)] // not a file issue
    [DataRow("CorrectRuleId", 999, null, false)] // not a file issue - should not match a file issue, even though the hash is the same
    public void IsMatch_FileLevelIssue(string serverRuleId, int? serverIssueLine,
        string serverHash, bool expectedResult)
    {
        // File issues have line number of 0 and an empty hash
        var issueToMatch = CreateIssueToMatch("CorrectRuleId", null, null);
        var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);

        CreateTestSubject().IsLikelyMatch(issueToMatch, serverIssue).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow("CorrectRuleId", null, null, true)] // exact matches
    [DataRow("CorrectRuleId", null, "hash", true)] // hash should be ignored for file-level issues
    [DataRow("WrongRuleId", null, null, false)] // wrong rule
    [DataRow("CorrectRuleId", 20, "hash", true)] // roslyn issue is not actually a file issue, so it should match
    [DataRow("CorrectRuleId", 1, null, true)] // roslyn issue is not actually a file issue, so it should match
    [DataRow("CorrectRuleId", 999, null, false)] // not a file issue - should not match a file issue, even though the hash is the same
    public void IsMatch_PotentialRoslynFileLevelIssue_ServerIssueVariation(string serverRuleId, int? serverIssueLine,
        string serverHash, bool expectedResult)
    {
        // File issues for roslyn have line and column number equal to 1
        var issueToMatch = new FilterableRoslynIssue("CorrectRuleId",  null, 1, 1);
        issueToMatch.SetLineHash("hash"); // hash is always calculated since we don't know if it's a file issue or not
        var serverIssue = CreateServerIssue(serverRuleId, serverIssueLine, serverHash);

        CreateTestSubject().IsLikelyMatch(issueToMatch, serverIssue).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow("CorrectRuleId", 1, 1, true)] // potential file issue matches server file issue
    [DataRow("CorrectRuleId", 1, 2, false)] // not a file issue
    [DataRow("WrongRuleId", 1, 1, false)] // wrong rule
    public void IsMatch_PotentialRoslynFileLevelIssue_RoslynIssueVariation(string roslynIssueId, int startLine, int startColumn, bool expectedResult)
    {
        var issueToMatch = new FilterableRoslynIssue(roslynIssueId, null, startLine, startColumn);
        issueToMatch.SetLineHash("hash"); // hash is always calculated since we don't know if it's a file issue or not
        var serverIssue = CreateServerIssue("CorrectRuleId", null, null);

        CreateTestSubject().IsLikelyMatch(issueToMatch, serverIssue).Should().Be(expectedResult);
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
    [DataRow(@"same.TXT", @"c:\XXXsame.txt", false)] // partial file name -> should not match
    [DataRow(@"differentExt.123", @"a:\differentExt.999", false)] // different extension -> should not match
    [DataRow(@"aaa\partial\file.cs", @"d:\partial\file.cs", false)]
    // Only matching the local path tail, so the same server path can match multiple local files
    [DataRow(@"partial\file.cs", @"c:\aaa\partial\file.cs", true)]
    [DataRow(@"partial\file.cs", @"c:\aaa\bbb\partial\file.cs", true)]
    [DataRow(@"partial\file.cs", @"c:\aaa\bbb\ccc\partial\file.cs", true)]
    public void IsMatch_CheckFileComparisons(string serverFilePath, string localFilePath, bool expected)
    {
        var issueToMatch = CreateIssueToMatch("111", 0, "hash", filePath: localFilePath);

        var serverIssue = CreateServerIssue("111", 0, "hash", filePath: serverFilePath);

        CreateTestSubject().IsLikelyMatch(issueToMatch, serverIssue).Should().Be(expected);
    }

    [TestMethod]
    public void FindMatchOrDefault_FindsFirstMatch()
    {
        var ruleId = "111";
        var startLine = 0;
        var issueToMatch = CreateIssueToMatch(ruleId, startLine, null, @"c:\root\dir\file.cs");
        var serverPath = @"dir\file.cs";

        var correctServerIssue = CreateServerIssue(ruleId, startLine, null, serverPath);

        CreateTestSubject().GetFirstLikelyMatchFromSameFileOrNull(issueToMatch, new[]
        {
            CreateServerIssue("222", startLine, null, serverPath),
            CreateServerIssue(ruleId, 111, null, serverPath),
            correctServerIssue,
            CreateServerIssue(ruleId, startLine, null, serverPath) // finds only the firs match
        }).Should().BeSameAs(correctServerIssue);
    }

    [TestMethod]
    public void FindMatchOrDefault_NoServerIssues_ReturnsNull()
    {
        var issueToMatch = CreateIssueToMatch("1", 1, "1");

        CreateTestSubject().GetFirstLikelyMatchFromSameFileOrNull(issueToMatch, Array.Empty<SonarQubeIssue>()).Should().BeNull();
    }

    private IssueMatcher CreateTestSubject()
    {
        return new IssueMatcher();
    }
    
    private IFilterableIssue CreateIssueToMatch(string ruleId, int? startLine, string lineHash, string filePath = null) =>
        new TestFilterableIssue
        {
            RuleId = ruleId,
            StartLine = startLine,
            LineHash = lineHash,
            FilePath = filePath
        };
    
    private SonarQubeIssue CreateServerIssue(string ruleId, int? startLine, string lineHash,
        string filePath = null)
    {
        var sonarQubeIssue = new SonarQubeIssue(null, filePath, lineHash, null, null, ruleId, false, SonarQubeIssueSeverity.Info,
            DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            startLine.HasValue
                ? new IssueTextRange(startLine.Value, 1, 1, 1)
                : null,
            flows: null);

        return sonarQubeIssue;
    }
}
