/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Analysis;

[TestClass]
public class AnalyzeFilesResponseTests
{
    [TestMethod]
    public void Deserialize_AsExpected()
    {
        var failedFile = new FileUri("C:\\tmp\\junit14012097140227905793\\Foo.cs");
        var textRange = new TextRangeDto(1, 0, 1, 20);
        var rawIssueFlow = new List<RawIssueFlowDto>
        {
            new([new RawIssueLocationDto(textRange, "MESSAGE", failedFile)])
        };

        var failedAnalysisFiles = new HashSet<FileUri> { failedFile };
        var rawIssues = new List<RawIssueDto>
        {
            new(IssueSeverity.MAJOR, RuleType.BUG, CleanCodeAttribute.IDENTIFIABLE,  new(){[SoftwareQuality.MAINTAINABILITY] = ImpactSeverity.HIGH}, "S123", "PRIMARY MESSAGE", failedFile,
                rawIssueFlow, [], textRange, "RULE DESCRIPTION CONTEXT KEY",
                VulnerabilityProbability.HIGH)
        };

        var expectedResponse = new AnalyzeFilesResponse(failedAnalysisFiles, rawIssues);

        const string serializedString = """
                                       {
                                         "failedAnalysisFiles": [
                                           "file:///C:/tmp/junit14012097140227905793/Foo.cs"
                                         ],
                                         "rawIssues": [
                                           {
                                             "severity": "MAJOR",
                                             "type": "BUG",
                                             "cleanCodeAttribute": "IDENTIFIABLE",
                                             "impacts": { "MAINTAINABILITY" : "HIGH"},
                                             "ruleKey": "S123",
                                             "primaryMessage": "PRIMARY MESSAGE",
                                             "fileUri": "file:///C:/tmp/junit14012097140227905793/Foo.cs",
                                             "flows": [
                                               {
                                                 "locations": [
                                                   {
                                                     "textRange": {
                                                       "startLine": 1,
                                                       "startLineOffset": 0,
                                                       "endLine": 1,
                                                       "endLineOffset": 20
                                                     },
                                                     "message": "MESSAGE",
                                                     "fileUri": "file:///C:/tmp/junit14012097140227905793/Foo.cs"
                                                   }
                                                 ]
                                               }
                                             ],
                                             "quickFixes": [],
                                             "textRange": {
                                               "startLine": 1,
                                               "startLineOffset": 0,
                                               "endLine": 1,
                                               "endLineOffset": 20
                                             },
                                             "ruleDescriptionContextKey": "RULE DESCRIPTION CONTEXT KEY",
                                             "vulnerabilityProbability": 0
                                           }
                                         ]
                                       }
                                       """;

        var actualResponse = JsonConvert.DeserializeObject<AnalyzeFilesResponse>(serializedString);

        actualResponse.Should().BeEquivalentTo(expectedResponse, options => options.ComparingByMembers<AnalyzeFilesResponse>().ComparingByMembers<RawIssueDto>().ComparingByMembers<RawIssueFlowDto>());
    }
}
