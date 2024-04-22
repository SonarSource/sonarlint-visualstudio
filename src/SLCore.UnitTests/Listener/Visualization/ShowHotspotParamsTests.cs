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
using SonarLint.VisualStudio.SLCore.Listener.Visualization;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Visualization;

[TestClass]
public class ShowHotspotParamsTests
{
    [TestMethod]
    public void DeserializesCorrectly()
    {
        var expected = new ShowHotspotParams("configscopeid123",
            new HotspotDetailsDto("hotspotkey123",
                "this is a hotspot message",
                "\\some\\path\\inside\\project",
                new TextRangeDto(1, 11, 2, 22),
                "author is me",
                "status123",
                "resolution123",
                new HotspotRuleDto("rulekey:123",
                    "hotspotname123",
                    "securitycategory:typo",
                    "low",
                    "very risky description",
                    "might be vulnerable, not sure how",
                    "just do it"),
                "a==\"b\";"));

        var serialized = """
                         {
                           "configurationScopeId": "configscopeid123",
                           "issueDetails": {
                             "key": "hotspotkey123",
                             "message": "this is a hotspot message",
                             "ideFilePath": "\\some\\path\\inside\\project",
                             "textRange": {
                               "startLine": 1,
                               "startLineOffset": 11,
                               "endLine": 2,
                               "endLineOffset": 22
                             },
                             "author": "author is me",
                             "status": "status123",
                             "resolution": "resolution123",
                             "rule": {
                               "key": "rulekey:123",
                               "name": "hotspotname123",
                               "securityCategory": "securitycategory:typo",
                               "vulnerabilityProbability": "low",
                               "riskDescription": "very risky description",
                               "vulnerabilityDescription": "might be vulnerable, not sure how",
                               "fixRecommendations": "just do it"
                             },
                             "codeSnippet": "a==\"b\";"
                           }
                         }
                         """;

        JsonConvert.DeserializeObject<ShowHotspotParams>(serialized).Should().BeEquivalentTo(expected,
            options => options
                .ComparingByMembers<ShowHotspotParams>()
                .ComparingByMembers<HotspotDetailsDto>()
                .ComparingByMembers<HotspotRuleDto>()
        );
    }   
}
