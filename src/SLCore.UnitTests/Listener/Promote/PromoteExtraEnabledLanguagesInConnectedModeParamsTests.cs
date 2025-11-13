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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Promote;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Promote;

[TestClass]
public class PromoteExtraEnabledLanguagesInConnectedModeParamsTests
{
    [TestMethod]
    public void Deserialize_AsExpected()
    {
        var expectedObject = new PromoteExtraEnabledLanguagesInConnectedModeParams("CONFIG_SCOPE_ID", [Language.TSQL]);

        const string serializedParams = """
                                        {
                                          "configurationScopeId": "CONFIG_SCOPE_ID",
                                          "languagesToPromote": [
                                            "TSQL"
                                          ]
                                        }
                                        """;

        var deserializedObject = JsonConvert.DeserializeObject<PromoteExtraEnabledLanguagesInConnectedModeParams>(serializedParams);

        deserializedObject.configurationScopeId.Should().Be(expectedObject.configurationScopeId);
        deserializedObject.languagesToPromote.Should().BeEquivalentTo(expectedObject.languagesToPromote);
    }
}
