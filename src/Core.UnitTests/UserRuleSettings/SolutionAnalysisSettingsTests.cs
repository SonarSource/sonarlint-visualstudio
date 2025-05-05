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
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Core.UnitTests.UserRuleSettings;

[TestClass]
public class SolutionAnalysisSettingsTests
{
    [TestMethod]
    public void SolutionAnalysisSettings_SerializesCorrectly()
    {
        var settings = new SolutionAnalysisSettings(
            new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } });
        const string expectedJson =
            """
            {
              "sonarlint.analyzerProperties": {
                "key2": "value2",
                "key1": "value1"
              }
            }
            """;

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        json.Should().Be(expectedJson);
    }

    [TestMethod]
    public void SolutionAnalysisSettings_DeserializesCorrectly()
    {
        const string json = """
                            {
                              "sonarlint.analyzerProperties": {
                                "key1": "value1",
                                "key2": "value2"
                              }
                            }
                            """;

        var settings = JsonConvert.DeserializeObject<SolutionAnalysisSettings>(json);

        settings.AnalysisProperties.Should().BeEquivalentTo(
            new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } });
    }
}
