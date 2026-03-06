
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
using SonarLint.VisualStudio.SLCore.Service.Plugin;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Plugin;

[TestClass]
public class GetPluginStatusesResponseTests
{
    [TestMethod]
    public void Deserialized_AsExpected()
    {
        var expected = new GetPluginStatusesResponse(
            pluginStatuses:
            [
                new PluginStatusDto(
                    pluginName: "Java",
                    state: PluginStateDto.ACTIVE,
                    source: ArtifactSourceDto.EMBEDDED,
                    actualVersion: "1.2.3",
                    overriddenVersion: null),
                new PluginStatusDto(
                    pluginName: "C/C++/Objective-C",
                    state: PluginStateDto.SYNCED,
                    source: ArtifactSourceDto.SONARQUBE_SERVER,
                    actualVersion: "4.5.6",
                    overriddenVersion: "3.0.0")
            ]);

        const string serialized = """
                                  {
                                    pluginStatuses: [
                                      {
                                        pluginName: "Java",
                                        state: "ACTIVE",
                                        source: "EMBEDDED",
                                        actualVersion: "1.2.3",
                                        overriddenVersion: null
                                      },
                                      {
                                        pluginName: "C/C++/Objective-C",
                                        state: "SYNCED",
                                        source: "SONARQUBE_SERVER",
                                        actualVersion: "4.5.6",
                                        overriddenVersion: "3.0.0"
                                      }
                                    ]
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<GetPluginStatusesResponse>(serialized);

        actual
            .Should()
            .BeEquivalentTo(expected,
                options =>
                    options
                        .ComparingByMembers<GetPluginStatusesResponse>());
    }
}
