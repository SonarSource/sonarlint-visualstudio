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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.OpenInIDE;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class IssueDetailDtoToAnalysisIssueConverterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<IssueDetailDtoToAnalysisIssueConverter, IIssueDetailDtoToAnalysisIssueConverter>(
            MefTestHelpers.CreateExport<IChecksumCalculator>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<IssueDetailDtoToAnalysisIssueConverter>();
    }
    
    [TestMethod]
    public void Convert_CalculatesChecksumForCodeSnippet()
    {
        const string codeSnippet = "code snippet; 123";
        const string checksum = "checksum123";
        var checksumCalculator = Substitute.For<IChecksumCalculator>();
        checksumCalculator.Calculate(codeSnippet).Returns(checksum);
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(checksumCalculator);

        var issue = testSubject.Convert(new IssueDetailDto("key",
                "ruleKey",
                "ide\\path",
                "branch",
                "pr",
                "msg",
                "today",
                codeSnippet,
                false,
                null,
                new TextRangeDto(1,
                    1,
                    1,
                    1)),
            "some\\path");

        issue.PrimaryLocation.TextRange.LineHash.Should().BeSameAs(checksum);
    }
    
    [TestMethod]
    public void Convert_PathTranslated()
    {
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(Substitute.For<IChecksumCalculator>());

        var issue = testSubject.Convert(new IssueDetailDto("key",
                "ruleKey",
                "ide\\path",
                "branch",
                "pr",
                "msg",
                "today",
                "codeSnippet",
                false,
                null,
                new TextRangeDto(1,
                    1,
                    1,
                    1)),
            "some\\path");

        issue.PrimaryLocation.FilePath.Should().Be("some\\path\\ide\\path");
    }
    
    [TestMethod]
    public void Convert_PrimaryRangeAndMessagePreserved()
    {
        const int startLine = 1;
        const int startLineOffset = 2;
        const int endLine = 11;
        const int endLineOffset = 22;
        const string message = "msg";
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(Substitute.For<IChecksumCalculator>());

        var issue = testSubject.Convert(new IssueDetailDto("key",
                "ruleKey",
                "ide\\path",
                "branch",
                "pr",
                message,
                "today",
                "codeSnippet",
                false,
                null,
                new TextRangeDto(startLine,
                    startLineOffset,
                    endLine,
                    endLineOffset)),
            "some\\path");

        issue.PrimaryLocation.Message.Should().BeSameAs(message);
        issue.PrimaryLocation.TextRange.Should().BeEquivalentTo(new TextRange(startLine, endLine, startLineOffset, endLineOffset, "hash"), options => options.Excluding(info => info.LineHash));
    }
    
    [TestMethod]
    public void Convert_RuleKeyPreserved()
    {
        const string ruleKey = "ruleKey:123";
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(Substitute.For<IChecksumCalculator>());

        var issue = testSubject.Convert(new IssueDetailDto("key",
                ruleKey,
                "ide\\path",
                "branch",
                "pr",
                "msg",
                "today",
                "codeSnippet",
                false,
                null,
                new TextRangeDto(1, 2, 3, 4)),
            "some\\path");

        issue.RuleKey.Should().BeSameAs(ruleKey);
    }
    
    [TestMethod]
    public void Convert_RuleDescriptionContextSetToDefault()
    {
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(Substitute.For<IChecksumCalculator>());

        var issue = testSubject.Convert(new IssueDetailDto("key",
                "rule",
                "ide\\path",
                "branch",
                "pr",
                "msg",
                "today",
                "codeSnippet",
                false,
                null,
                new TextRangeDto(1, 2, 3, 4)),
            "some\\path");

        issue.RuleDescriptionContextKey.Should().BeNull();
    }
    
    [TestMethod]
    public void Convert_FlowsPreservedWithPathTranslation()
    {
        var testSubject = new IssueDetailDtoToAnalysisIssueConverter(Substitute.For<IChecksumCalculator>());

        var issue = testSubject.Convert(new IssueDetailDto("key",
                "rule",
                "ide\\path",
                "branch",
                "pr",
                "msg",
                "today",
                "codeSnippet",
                false,
                new List<FlowDto>
                {
                    new FlowDto(new List<LocationDto>
                    {
                        new LocationDto(new TextRangeWithHashDto(1, 11, 111, 1111, "11111"), "message1", "flow\\path\\1"),
                        new LocationDto(new TextRangeWithHashDto(2, 22, 222, 2222, "22222"), "message2", "flow\\path\\2"),
                    }),
                    new FlowDto(new List<LocationDto>
                    {
                        new LocationDto(new TextRangeWithHashDto(3, 33, 333, 3333, "33333"), "message3", "flow\\path\\3")
                    })
                },
                new TextRangeDto(1, 2, 3, 4)),
            "some\\path");

        issue.Flows.Should().BeEquivalentTo(new List<IAnalysisIssueFlow>
        {
            new AnalysisIssueFlow(new List<IAnalysisIssueLocation>
            {
                new AnalysisIssueLocation("message1", "some\\path\\flow\\path\\1", new TextRange(1, 111, 11, 1111, "11111")),
                new AnalysisIssueLocation("message2", "some\\path\\flow\\path\\2", new TextRange(2, 222, 22, 2222, "22222"))
            }),
            new AnalysisIssueFlow(new List<IAnalysisIssueLocation>
            {
                new AnalysisIssueLocation("message3", "some\\path\\flow\\path\\3", new TextRange(3, 333, 33, 3333, "33333"))
            })
        });
    }
}
