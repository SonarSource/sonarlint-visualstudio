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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.Http.Models;

[TestClass]
public class AnalysisRequestTests
{

    [TestMethod]
    public void Deserialization_SmokeTest()
    {
        var json =
            """
            {
                "FileUris": ["file:///C:/test/file1.cs", "file:///C:/test/file2.cs"],
                "ActiveRules": [
                    { "RuleId": "S101", "Parameters": { "threshold": "3" } },
                    { "RuleId": "S102", "Parameters": { "timeout": "60" } }
                ],
                "AnalysisProperties": { "prop1": "value1", "prop2": "value2" },
                "AnalyzerInfo": { "ShouldUseCsharpEnterprise": true, "ShouldUseVbEnterprise": false },
                "AnalysisId": "8171cac6-65cc-4ba0-8804-db38f424f37d"
            }
            """;

        var expected = new AnalysisRequest
        {
            FileUris =
            [
                new FileUri("file:///C:/test/file1.cs"), new FileUri("file:///C:/test/file2.cs")
            ],
            ActiveRules =
            [
                new ActiveRuleDto("S101", new Dictionary<string, string> { { "threshold", "3" } }), new ActiveRuleDto("S102", new Dictionary<string, string> { { "timeout", "60" } })
            ],
            AnalysisProperties = new Dictionary<string, string> { { "prop1", "value1" }, { "prop2", "value2" } },
            AnalyzerInfo = new AnalyzerInfoDto(true, false),
            AnalysisId = Guid.Parse("8171cac6-65cc-4ba0-8804-db38f424f37d")
        };

        var actual = JsonConvert.DeserializeObject<AnalysisRequest>(json);
        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<AnalysisRequest>().ComparingByMembers<ActiveRuleDto>().ComparingByMembers<AnalyzerInfoDto>());
    }
}
