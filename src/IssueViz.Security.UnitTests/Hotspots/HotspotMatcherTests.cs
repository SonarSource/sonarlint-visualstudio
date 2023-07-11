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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class HotspotMatcherTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<HotspotMatcher, IHotspotMatcher>();
    }

    [TestMethod]
    public void CheckIsSharedMefComponent()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<HotspotMatcher>();
    }

    [TestMethod]
    public void IsMatch_HotspotsDifferOnlyInHash_ReturnsTrue()
    {
        const string ruleId = "rule1";
        const string filePath = "A:\\ny\\p\\ath";
        const string serverPath = "p\\ath";
        const string message1 = "message1";
        const int startLine = 10;
        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot(ruleId, message1, "abchash", filePath, startLine),
                CreateServerHotspot(ruleId, message1, "defhash", serverPath, startLine))
            .Should()
            .BeTrue();
    }

    [DataTestMethod]
    [DataRow(null, null, false)] // null server has -> no match
    [DataRow(null, "any", false)] // null server hash -> no match
    [DataRow("", "", false)] // empty server -> no match
    [DataRow("", "any", false)] // empty server -> no match
    [DataRow("aaa", "AAA", false)] // different case - no match
    [DataRow("sameHash", "sameHash", true)]
    public void IsMatch_SameFileAndRuleId_VaryLineHashes(string serverHash, string localHash, bool shouldMatch)
    {
        // Different lines and messages.
        // RuleIds and paths do match.
        const string ruleId = "rule1";
        const string filePath = "A:\\ny\\p\\ath";
        const string serverPath = "p\\ath";

        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot(ruleId, "message 1", localHash, filePath, 10),
        CreateServerHotspot(ruleId, "message 2", serverHash, serverPath, 20))
            .Should()
            .Be(shouldMatch);
    }

    [DataTestMethod]
    [DataRow(0, 1, false)]
    [DataRow(1, 0, false)]
    [DataRow(0, 0, true)]
    [DataRow(1, 1, true)]
    public void IsMatch_SameFileAndRuleId_VaryLineNumber(int serverLineNumber, int localLineNumber, bool shouldMatch)
    {
        // Different hashes and messages.
        // RuleIds and paths do match.
        const string ruleId = "rule1";
        const string filePath = "A:\\ny\\p\\ath";
        const string serverPath = "p\\ath";

        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot(ruleId, "message 1", "localHash", filePath, serverLineNumber),
        CreateServerHotspot(ruleId, "message 2", "serverHash", serverPath, localLineNumber))
            .Should()
            .Be(shouldMatch);
    }

    [DataTestMethod]
    [DataRow(null, null, true)]
    [DataRow("", "", true)]
    [DataRow("aaa", "AAA", false)] // different case -> no match
    [DataRow("sameMessage", "sameMessage", true)]
    public void IsMatch_SameFileAndRuleId_VaryMessages(string serverMessage, string localMessage, bool shouldMatch)
    {
        // Different lines and hashes.
        // RuleIds and paths do match.
        const string ruleId = "rule1";
        const string filePath = "A:\\ny\\p\\ath";
        const string serverPath = "p\\ath";

        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot(ruleId, localMessage, "localHash", filePath, 10),
        CreateServerHotspot(ruleId, serverMessage, "serverHash", serverPath, 20))
            .Should()
            .Be(shouldMatch);
    }

    [TestMethod]
    public void IsMatch_DifferentRuleId_ReturnsFalse()
    {
        const string message = "some message";
        const string filePath = "A:\\ny\\p\\ath";
        const string serverPath = "p\\ath";
        const string lineHash = null;
        const int startLine = 1;
        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot("rule2", message, lineHash, filePath, startLine),
                CreateServerHotspot("rule1", message, lineHash, serverPath, startLine))
            .Should()
            .BeFalse();
    }
    
    [DataTestMethod]
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
    public void IsMatch_IdenticalIssuesExceptFilePath_ReturnsBasedOnFilePathMatch(string serverHotspotFilePath, string localHotspotFilePath, bool expectedResult)
    {
        const string ruleId = "rule123";
        const string message = "some message";
        const string lineHash = null;
        const int startLine = 1;
        var testSubject = CreateTestSubject();

        testSubject.IsMatch(CreateLocalHotspot(ruleId, message, lineHash, localHotspotFilePath, startLine),
                CreateServerHotspot(ruleId, message, lineHash, serverHotspotFilePath, startLine))
            .Should()
            .Be(expectedResult);
    }

    private IHotspotMatcher CreateTestSubject()
    {
        return new HotspotMatcher();
    }

    private IAnalysisIssueVisualization CreateLocalHotspot(string ruleId,
        string message,
        string lineHash,
        string filePath,
        int startLine)
    {
        var mock = new Mock<IAnalysisIssueVisualization>();
        mock.SetupGet(x => x.RuleId).Returns(ruleId);
        mock.SetupGet(x => x.LineHash).Returns(lineHash);
        mock.SetupGet(x => x.FilePath).Returns(filePath);
        mock.SetupGet(x => x.StartLine).Returns(startLine);
        var issueMock = new Mock<IAnalysisIssueBase>();
        var locationMock = new Mock<IAnalysisIssueLocation>();
        locationMock.SetupGet(x => x.Message).Returns(message);
        issueMock.SetupGet(x => x.PrimaryLocation).Returns(locationMock.Object);
        mock.SetupGet(x => x.Issue).Returns(issueMock.Object);
        return mock.Object;
    }

    private SonarQubeHotspot CreateServerHotspot(string ruleId,
        string message,
        string lineHash,
        string filePath,
        int startLine)
    {
        return new SonarQubeHotspot(null,
            message,
            lineHash,
            null,
            null,
            null,
            null,
            null,
            null,
            filePath,
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new SonarQubeHotspotRule(ruleId,
                null,
                null,
                null,
                null,
                null,
                null),
            new IssueTextRange(startLine,
                0,
                0,
                0));
    }
}
