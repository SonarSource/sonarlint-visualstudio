/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Issue;

[TestClass]
public class GetEffectiveIssueDetailsResponseTests
{
    [TestMethod]
    public void Deserialized_AsExpected()
    {
        var expected = new GetEffectiveIssueDetailsResponse(
            details: new EffectiveIssueDetailsDto(
                key: "S3776",
                name: "Cognitive Complexity of methods should not be too high",
                language: Language.CS,
                vulnerabilityProbability: VulnerabilityProbability.HIGH,
                description: new RuleMonolithicDescriptionDto("<p>Cognitive Complexity is a measure of how hard it is to understand the control flow of a unit of code.</p>"),
                parameters: [new EffectiveRuleParamDto("max", "Maximum cognitive complexity", "15", "15")],
                severityDetails: new StandardModeDetails(IssueSeverity.CRITICAL, RuleType.CODE_SMELL),
                ruleDescriptionContextKey: "key"));

        const string serialized = """
                                  {
                                    details: {
                                      "ruleKey": "S3776",
                                      "name": "Cognitive Complexity of methods should not be too high",
                                      "language": "cs",
                                      "vulnerabilityProbability": "HIGH",
                                      "description": {
                                        "htmlContent": "<p>Cognitive Complexity is a measure of how hard it is to understand the control flow of a unit of code.</p>"
                                      },
                                      "params": [
                                        {
                                          "name": "max",
                                          "description": "Maximum cognitive complexity",
                                          "value": "15",
                                          "defaultValue": "15"
                                        }
                                      ],
                                      "severityDetails": {
                                         "severity": "CRITICAL",
                                          "type": "CODE_SMELL"
                                      },
                                      "ruleDescriptionContextKey": "key"
                                    }
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<GetEffectiveIssueDetailsResponse>(serialized);

        actual
            .Should()
            .BeEquivalentTo(expected,
                options =>
                    options
                        .ComparingByMembers<GetEffectiveIssueDetailsResponse>()
                        .ComparingByMembers<EffectiveIssueDetailsDto>()
                        .ComparingByMembers<RuleMonolithicDescriptionDto>()
                        .ComparingByMembers<EffectiveRuleParamDto>()
                        .ComparingByMembers<StandardModeDetails>());
    }
}
