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

using FluentAssertions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http.Models;

[TestClass]
public class AnalysisResponseTests
{
    [TestMethod]
    public void Serialization_ProducesExpectedJson()
    {
        const string expectedJson =
            """
            {
              "RoslynIssues": [
                {
                  "RuleId": "rule-id",
                  "PrimaryLocation": {
                    "FileUri": "file:///c:/temp/test.cs",
                    "Message": "primary message",
                    "TextRange": {
                      "StartLine": 1,
                      "EndLine": 2,
                      "StartLineOffset": 3,
                      "EndLineOffset": 4
                    }
                  },
                  "Flows": [
                    {
                      "Locations": [
                        {
                          "FileUri": "file:///c:/temp/test.cs",
                          "Message": "secondary message",
                          "TextRange": {
                            "StartLine": 1,
                            "EndLine": 2,
                            "StartLineOffset": 3,
                            "EndLineOffset": 4
                          }
                        }
                      ]
                    }
                  ],
                  "QuickFixes": [
                    {
                      "Value": "fix value"
                    }
                  ]
                }
              ]
            }
            """;
        var issueTextRange = new RoslynIssueTextRange(1, 2, 3, 4);
        var fileUri = new FileUri("file:///c:/temp/test.cs");
        var primaryLocation = new RoslynIssueLocation("primary message", fileUri, issueTextRange);
        var secondaryLocation = new RoslynIssueLocation("secondary message", fileUri, issueTextRange);
        var flow = new RoslynIssueFlow([secondaryLocation]);
        var quickFix = new RoslynIssueQuickFix("fix value");
        var issue = new RoslynIssue("rule-id", primaryLocation, [flow], [quickFix]);
        var originalResponse = new AnalysisResponse { RoslynIssues = [issue] };

        var actualJson = JsonConvert.SerializeObject(originalResponse, Formatting.Indented);

        actualJson.Should().Be(expectedJson);
    }
}
