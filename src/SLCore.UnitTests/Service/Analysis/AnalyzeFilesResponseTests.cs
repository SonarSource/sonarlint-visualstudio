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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis.Models;
using IssueSeverity = SonarLint.VisualStudio.Core.IssueSeverity;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Analysis;

[TestClass]
public class AnalyzeFilesResponseTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var failedFile = new Uri("file:///tmp/junit14012097140227905793/Foo.cs");
        var textRange = new TextRangeDto(1, 0, 1, 20);
        var rawIssueFlow = new List<RawIssueFlowDto>
        {
            new([new RawIssueLocationDto(textRange, "MESSAGE", failedFile)])
        };

        var failedAnalysisFiles = new HashSet<Uri> { failedFile };
        var rawIssues = new List<RawIssueDto>
        {
            new RawIssueDto(IssueSeverity.Major, RuleType.BUG, CleanCodeAttribute.IDENTIFIABLE,
                new Dictionary<SoftwareQuality, ImpactSeverity>(), "S123", "PRIMARY MESSAGE", failedFile,
                rawIssueFlow, [], textRange, "RULE DESCRIPTION CONTEXT KEY",
                VulnerabilityProbability.HIGH)
        };

        var testSubject = new AnalyzeFilesResponse(failedAnalysisFiles, rawIssues);

        const string expectedString = """
                                       {
                                         "failedAnalysisFiles": [
                                           "file:///tmp/junit14012097140227905793/Foo.cs"
                                         ],
                                         "rawIssues": [
                                           {
                                             "severity": 2,
                                             "type": 1,
                                             "cleanCodeAttribute": 2,
                                             "impacts": {},
                                             "ruleKey": "S123",
                                             "primaryMessage": "PRIMARY MESSAGE",
                                             "fileUri": "file:///tmp/junit14012097140227905793/Foo.cs",
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
                                                     "fileUri": "file:///tmp/junit14012097140227905793/Foo.cs"
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
        
        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
