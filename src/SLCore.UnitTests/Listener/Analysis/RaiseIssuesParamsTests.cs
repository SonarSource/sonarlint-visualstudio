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
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class RaiseIssuesParamsTests
{

    [TestMethod]
    public void DeserializedCorrectly()
    {
        var expected = new RaiseFindingParams<RaisedIssueDto>("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                {
                    new FileUri("file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [new RaisedIssueDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                        null,
                        "secrets:S6336",
                        "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                        IssueSeverity.BLOCKER,
                        RuleType.VULNERABILITY,
                        CleanCodeAttribute.TRUSTWORTHY,
                        [new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH)],
                        DateTimeOffset.FromUnixTimeMilliseconds(1718182975467), 
                        true,
                        false,
                        new TextRangeDto(14, 24, 14, 54),
                        [],
                        [],
                        null)]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));
        
        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "issuesByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severity": "BLOCKER",
                    "type": "VULNERABILITY",
                    "cleanCodeAttribute": "TRUSTWORTHY",
                    "impacts": [
                      {
                        "softwareQuality": "SECURITY",
                        "impactSeverity": "HIGH"
                      }
                    ],
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseFindingParams<RaisedIssueDto>>(serialized);
        
        deserialized.Should().BeEquivalentTo(expected, options => 
            options.ComparingByMembers<RaiseFindingParams<RaisedIssueDto>>().ComparingByMembers<RaisedIssueDto>());
    }
}
