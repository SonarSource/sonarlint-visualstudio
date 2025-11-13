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
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using FileEditDto = SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models.FileEditDto;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.FixSuggestion;

[TestClass]
public class ShowFixSuggestionParamsTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var listOfChanges = new List<ChangesDto>
        {
            new(new LineRangeDto(10, 10), "public void test()", "private void test()")
        };
        var fileEditDto = new FileEditDto(@"C:\Users\test\TestProject\AFile.cs", listOfChanges);
        var fixSuggestionDto = new FixSuggestionDto("SUGGESTION_ID", "AN EXPLANATION", fileEditDto);
        var testSubject = new ShowFixSuggestionParams("CONFIG_SCOPE_ID", "S1234", fixSuggestionDto);

        const string expectedString = """
                                      {
                                        "configurationScopeId": "CONFIG_SCOPE_ID",
                                        "issueKey": "S1234",
                                        "fixSuggestion": {
                                          "suggestionId": "SUGGESTION_ID",
                                          "explanation": "AN EXPLANATION",
                                          "fileEdit": {
                                            "idePath": "C:\\Users\\test\\TestProject\\AFile.cs",
                                            "changes": [
                                              {
                                                "beforeLineRange": {
                                                  "startLine": 10,
                                                  "endLine": 10
                                                },
                                                "before": "public void test()",
                                                "after": "private void test()"
                                              }
                                            ]
                                          }
                                        }
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
