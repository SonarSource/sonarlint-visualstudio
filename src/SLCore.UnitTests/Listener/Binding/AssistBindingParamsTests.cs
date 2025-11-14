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
using SonarLint.VisualStudio.SLCore.Listener.Binding;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Binding;

[TestClass]
public class AssistBindingParamsTests
{
    [TestMethod]
    public void AssistBindingParams_DeserializesCorrectly()
    {
        var expected = new AssistBindingParams("connectionId", "projectKey", "configScopeId", true);
        const string serialized = """
                                  {
                                      "connectionId": "connectionId",
                                      "projectKey": "projectKey",
                                      "configScopeId": "configScopeId",
                                      "isFromSharedConfiguration": true
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<AssistBindingParams>(serialized);

        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<AssistCreatingConnectionParams>());
    }

    [TestMethod]
    public void AssistBindingParams_SerializesCorrectly()
    {
        var assistBindingParams = new AssistBindingParams("connectionId", "projectKey", "configScopeId", true);
        const string expected = """
                                {
                                  "connectionId": "connectionId",
                                  "projectKey": "projectKey",
                                  "configScopeId": "configScopeId",
                                  "isFromSharedConfiguration": true
                                }
                                """;

        var actual = JsonConvert.SerializeObject(assistBindingParams, Formatting.Indented);

        actual.Should().BeEquivalentTo(expected);
    }
}
