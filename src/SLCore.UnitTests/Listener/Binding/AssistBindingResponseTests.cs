﻿/*
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
using SonarLint.VisualStudio.SLCore.Listener.Binding;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Binding;

[TestClass]
public class AssistBindingResponseTests
{
    [TestMethod]
    public void AssistBindingResponse_DeserializesCorrectly()
    {
        var expected = new AssistBindingResponse("configScopeId");
        const string serialized = """
                                  {
                                      "configurationScopeId": "configScopeId"
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<AssistBindingResponse>(serialized);

        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<AssistBindingResponse>());
    }

    [TestMethod]
    public void AssistBindingResponse_SerializesCorrectly()
    {
        var assistBindingResponse = new AssistBindingResponse("configScopeId");
        const string expected = """
                                {
                                  "configurationScopeId": "configScopeId"
                                }
                                """;

        var actual = JsonConvert.SerializeObject(assistBindingResponse, Formatting.Indented);

        actual.Should().BeEquivalentTo(expected);
    }
}
