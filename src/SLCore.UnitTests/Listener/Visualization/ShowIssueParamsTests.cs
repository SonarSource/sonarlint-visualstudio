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
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Visualization;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Visualization;

[TestClass]
public class ShowIssueParamsTests
{
    [TestMethod]
    public void DeserializesCorrectly()
    {
        var expected = new ShowIssueParams(
            "configScope1",
            new IssueDetailDto("issueKeyValue",
                "rule:S123",
                "idepath",
                "feature/sloop-open-in-ide",
                "pr#123",
                "my message",
                "2024-01-01",
                "a==b",
                true,
                new List<FlowDto>
                {
                    new FlowDto(new List<LocationDto>
                    {
                        new LocationDto(new TextRangeWithHashDto(1, 11, 2, 20, "hashabc"), "additional location", "some other file")
                    })
                },
                new TextRangeDto(3, 30, 4, 44)));

        var serialized = """
                         {
                           "configurationScopeId": "configScope1",
                           "issueDetails": {
                             "issueKey": "issueKeyValue",
                             "ruleKey": "rule:S123",
                             "ideFilePath": "idepath",
                             "branch": "feature/sloop-open-in-ide",
                             "pullRequest": "pr#123",
                             "message": "my message",
                             "creationDate": "2024-01-01",
                             "codeSnippet": "a==b",
                             "isTaint": true,
                             "flows": [
                               {
                                 "locations": [
                                   {
                                     "textRange": {
                                       "hash": "hashabc",
                                       "startLine": 1,
                                       "startLineOffset": 11,
                                       "endLine": 2,
                                       "endLineOffset": 20
                                     },
                                     "message": "additional location",
                                     "filePath": "some other file"
                                   }
                                 ]
                               }
                             ],
                             "textRange": {
                               "startLine": 3,
                               "startLineOffset": 30,
                               "endLine": 4,
                               "endLineOffset": 44
                             }
                           }
                         }
                         """;

        JsonConvert.DeserializeObject<ShowIssueParams>(serialized).Should().BeEquivalentTo(expected,
            options => options
                .ComparingByMembers<ShowIssueParams>()
                .ComparingByMembers<IssueDetailDto>()
                .ComparingByMembers<FlowDto>()
        );
    }
}
