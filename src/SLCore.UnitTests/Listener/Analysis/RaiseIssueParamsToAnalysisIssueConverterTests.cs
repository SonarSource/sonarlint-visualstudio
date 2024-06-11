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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using CleanCodeAttribute = SonarLint.VisualStudio.SLCore.Common.Models.CleanCodeAttribute;
using SoftwareQuality = SonarLint.VisualStudio.SLCore.Common.Models.SoftwareQuality;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis
{
    [TestClass]
    public class RaiseIssueParamsToAnalysisIssueConverterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RaiseIssueParamsToAnalysisIssueConverter, IRaiseIssueParamsToAnalysisIssueConverter>();
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<RaiseIssueParamsToAnalysisIssueConverter>();
        }

        [TestMethod]
        public void GetAnalysisIssues_HasNoIssues_ReturnsEmpty()
        {
            var raiseIssueParams = new RaiseIssuesParams("configurationScopeId", new Dictionary<FileUri, List<RaisedIssueDto>>(), false, Guid.NewGuid());

            RaiseIssueParamsToAnalysisIssueConverter testSubject = CreateTestSubject();

            var result = testSubject.GetAnalysisIssues(raiseIssueParams);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetAnalysisIssues_HasIssues_Converts()
        {
            var analysisID = Guid.NewGuid();
            var dateTimeOffset = DateTimeOffset.Now;

            var issue1 = new RaisedIssueDto(Guid.NewGuid(), "serverKey1", "ruleKey1", "PrimaryMessage1", IssueSeverity.MAJOR, RuleType.CODE_SMELL, CleanCodeAttribute.EFFICIENT, new List<ImpactDto>(), dateTimeOffset, true, false, new TextRangeDto(1, 2, 3, 4), null, null, "context1");

            var issue2impact1 = new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.LOW);
            var issue2impact2 = new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM);
            var issue2impact3 = new ImpactDto(SoftwareQuality.RELIABILITY, ImpactSeverity.HIGH);
            var issue2impacts = new List<ImpactDto>() { issue2impact1, issue2impact2, issue2impact3 };

            var issue2flow1Location1 = new IssueLocationDto(new TextRangeDto(11, 12, 13, 14), "Flow1Location1Message", new FileUri("C:\\flowFile1.cs"));
            var issue2flow1Location2 = new IssueLocationDto(new TextRangeDto(21, 22, 23, 24), "Flow1Location2Message", new FileUri("C:\\flowFile1.cs"));

            var issue2flow1 = new IssueFlowDto(new List<IssueLocationDto> { issue2flow1Location1, issue2flow1Location2 });

            var issue2flow2Location1 = new IssueLocationDto(new TextRangeDto(31, 32, 33, 34), "Flow2Location1Message", new FileUri("C:\\flowFile2.cs"));
            var issue2flow2Location2 = new IssueLocationDto(new TextRangeDto(41, 42, 43, 44), "Flow2Location2Message", new FileUri("C:\\flowFile2.cs"));

            var issue2flow2 = new IssueFlowDto(new List<IssueLocationDto> { issue2flow2Location1, issue2flow2Location2 });

            var issue2fix1fileedit1 = new FileEditDto(new FileUri("C:\\DifferentFile.cs"), new List<TextEditDto>());
            var issue2fix1 = new QuickFixDto(new List<FileEditDto> { issue2fix1fileedit1 }, "issue 2 fix 1");

            var issue2fix2fileedit1Textedit1 = new TextEditDto(new TextRangeDto(51, 52, 53, 54), "new text");
            var issue2fix2fileedit1 = new FileEditDto(new FileUri("C:\\IssueFile.cs"), new List<TextEditDto> { issue2fix2fileedit1Textedit1 });
            var issue2fix2 = new QuickFixDto(new List<FileEditDto> { issue2fix2fileedit1 }, "issue 2 fix 2");

            var issue2 = new RaisedIssueDto(Guid.NewGuid(),
                "serverKey2",
                "ruleKey2",
                "PrimaryMessage2",
                IssueSeverity.CRITICAL,
                RuleType.BUG,
                CleanCodeAttribute.LOGICAL,
                issue2impacts,
                dateTimeOffset,
                true,
                false,
                new TextRangeDto(61, 62, 63, 64),
                new List<IssueFlowDto> { issue2flow1, issue2flow2 },
                new List<QuickFixDto> { issue2fix1, issue2fix2 },
                "context2");

            var issues = new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                { new FileUri("C:\\IssueFile.cs"), new List<RaisedIssueDto> { issue1, issue2 } }
            };

            var raisedIssueParams = new RaiseIssuesParams("configurationScopeId", issues, false, analysisID);

            var testSubject = CreateTestSubject();

            var result = testSubject.GetAnalysisIssues(raisedIssueParams).ToList();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].RuleKey.Should().Be("ruleKey1");
            result[0].Severity.Should().Be(AnalysisIssueSeverity.Major);
            result[0].Type.Should().Be(AnalysisIssueType.CodeSmell);
            result[0].HighestSoftwareQualitySeverity.Should().BeNull();
            result[0].RuleDescriptionContextKey.Should().Be("context1");

            result[0].PrimaryLocation.FilePath.Should().Be("C:\\IssueFile.cs");
            result[0].PrimaryLocation.Message.Should().Be("PrimaryMessage1");
            result[0].PrimaryLocation.TextRange.StartLine.Should().Be(1);
            result[0].PrimaryLocation.TextRange.StartLineOffset.Should().Be(2);
            result[0].PrimaryLocation.TextRange.EndLine.Should().Be(3);
            result[0].PrimaryLocation.TextRange.EndLineOffset.Should().Be(4);
            result[0].PrimaryLocation.TextRange.LineHash.Should().BeNull();

            result[0].Flows.Should().BeEmpty();
            result[0].Fixes.Should().BeEmpty();

            result[1].RuleKey.Should().Be("ruleKey2");
            result[1].Severity.Should().Be(AnalysisIssueSeverity.Critical);
            result[1].Type.Should().Be(AnalysisIssueType.Bug);
            result[1].HighestSoftwareQualitySeverity.Should().Be(SoftwareQualitySeverity.High);
            result[1].RuleDescriptionContextKey.Should().Be("context2");

            result[1].PrimaryLocation.FilePath.Should().Be("C:\\IssueFile.cs");
            result[1].PrimaryLocation.Message.Should().Be("PrimaryMessage2");
            result[1].PrimaryLocation.TextRange.StartLine.Should().Be(61);
            result[1].PrimaryLocation.TextRange.StartLineOffset.Should().Be(62);
            result[1].PrimaryLocation.TextRange.EndLine.Should().Be(63);
            result[1].PrimaryLocation.TextRange.EndLineOffset.Should().Be(64);
            result[1].PrimaryLocation.TextRange.LineHash.Should().BeNull();

            result[1].Flows.Should().HaveCount(2);
            result[1].Flows[0].Locations.Should().HaveCount(2);

            result[1].Flows[0].Locations[0].FilePath.Should().Be("C:\\flowFile1.cs");
            result[1].Flows[0].Locations[0].Message.Should().Be("Flow1Location1Message");
            result[1].Flows[0].Locations[0].TextRange.StartLine.Should().Be(11);
            result[1].Flows[0].Locations[0].TextRange.StartLineOffset.Should().Be(12);
            result[1].Flows[0].Locations[0].TextRange.EndLine.Should().Be(13);
            result[1].Flows[0].Locations[0].TextRange.EndLineOffset.Should().Be(14);
            result[1].Flows[0].Locations[0].TextRange.LineHash.Should().BeNull();
            result[1].Flows[0].Locations[1].FilePath.Should().Be("C:\\flowFile1.cs");
            result[1].Flows[0].Locations[1].Message.Should().Be("Flow1Location2Message");
            result[1].Flows[0].Locations[1].TextRange.StartLine.Should().Be(21);
            result[1].Flows[0].Locations[1].TextRange.StartLineOffset.Should().Be(22);
            result[1].Flows[0].Locations[1].TextRange.EndLine.Should().Be(23);
            result[1].Flows[0].Locations[1].TextRange.EndLineOffset.Should().Be(24);
            result[1].Flows[0].Locations[1].TextRange.LineHash.Should().BeNull();

            result[1].Flows[1].Locations[0].FilePath.Should().Be("C:\\flowFile2.cs");
            result[1].Flows[1].Locations[0].Message.Should().Be("Flow2Location1Message");
            result[1].Flows[1].Locations[0].TextRange.StartLine.Should().Be(31);
            result[1].Flows[1].Locations[0].TextRange.StartLineOffset.Should().Be(32);
            result[1].Flows[1].Locations[0].TextRange.EndLine.Should().Be(33);
            result[1].Flows[1].Locations[0].TextRange.EndLineOffset.Should().Be(34);
            result[1].Flows[1].Locations[0].TextRange.LineHash.Should().BeNull();
            result[1].Flows[1].Locations[1].FilePath.Should().Be("C:\\flowFile2.cs");
            result[1].Flows[1].Locations[1].Message.Should().Be("Flow2Location2Message");
            result[1].Flows[1].Locations[1].TextRange.StartLine.Should().Be(41);
            result[1].Flows[1].Locations[1].TextRange.StartLineOffset.Should().Be(42);
            result[1].Flows[1].Locations[1].TextRange.EndLine.Should().Be(43);
            result[1].Flows[1].Locations[1].TextRange.EndLineOffset.Should().Be(44);
            result[1].Flows[1].Locations[1].TextRange.LineHash.Should().BeNull();

            result[1].Fixes.Should().HaveCount(1);
            result[1].Fixes[0].Message.Should().Be("issue 2 fix 2");
            result[1].Fixes[0].Edits.Should().HaveCount(1);
            result[1].Fixes[0].Edits[0].RangeToReplace.StartLine.Should().Be(51);
            result[1].Fixes[0].Edits[0].RangeToReplace.StartLineOffset.Should().Be(52);
            result[1].Fixes[0].Edits[0].RangeToReplace.EndLine.Should().Be(53);
            result[1].Fixes[0].Edits[0].RangeToReplace.EndLineOffset.Should().Be(54);
            result[1].Fixes[0].Edits[0].RangeToReplace.LineHash.Should().BeNull();
        }

        private static RaiseIssueParamsToAnalysisIssueConverter CreateTestSubject()
        {
            return new RaiseIssueParamsToAnalysisIssueConverter();
        }
    }
}
