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
using SonarLint.VisualStudio.SLCore.Listener.SmartNotification;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.SmartNotification;

[TestClass]
public class ShowSmartNotificationParamsTests
{
    [TestMethod]
    public void Deserialized_AsExpected()
    {
        const string text = "Quality Gate changed to Green";
        const string link = "http://localhost:9000/project/overview";
        const string configurationScopeId = "configScope";
        const string category = "QualityGate";
        const string connectionId = "connectionId";
        var expected = new ShowSmartNotificationParams(text, link, [configurationScopeId], category, connectionId);
        const string serialized = $$"""
                                    {
                                      "text": "{{text}}",
                                      "link": "{{link}}",
                                      "scopeIds": [
                                        "{{configurationScopeId}}"
                                      ],
                                      "category": "{{category}}",
                                      "connectionId": "{{connectionId}}"
                                    }
                                    """;

        var actual = JsonConvert.DeserializeObject<ShowSmartNotificationParams>(serialized);

        actual.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<ShowSmartNotificationParams>());
    }

    [TestMethod]
    public void Serialized_AsExpected()
    {
        const string text = "Quality Gate changed to Green";
        const string link = "http://localhost:9000/project/overview";
        const string configurationScopeId = "configScope";
        const string category = "QualityGate";
        const string connectionId = "connectionId";
        var testSubject = new ShowSmartNotificationParams(text, link, [configurationScopeId], category, connectionId);
        const string expected = $$"""
                                  {
                                    "text": "{{text}}",
                                    "link": "{{link}}",
                                    "scopeIds": [
                                      "{{configurationScopeId}}"
                                    ],
                                    "category": "{{category}}",
                                    "connectionId": "{{connectionId}}"
                                  }
                                  """;

        var actual = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        actual.Should().BeEquivalentTo(expected);
    }
}
