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
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.DependencyRisks;

[TestClass]
public class OpenDependencyRiskInBrowserParamsTests
{
    [TestMethod]
    [DataRow("config-scope-1", "71814600-2924-4b88-9e1a-1af0b46f8d48")]
    [DataRow("other-config-scope", "ea425b61-af22-48ac-98e1-78b644d34876")]
    public void Serialized_AsExpected(string configScopeId, string dependencyRiskKeyString)
    {
        var dependencyRiskKey = Guid.Parse(dependencyRiskKeyString);
        var expected = $$"""{"configScopeId":"{{configScopeId}}","dependencyRiskKey":"{{dependencyRiskKeyString}}"}""";

        var openDependencyRiskParams = new OpenDependencyRiskInBrowserParams(configScopeId, dependencyRiskKey);

        JsonConvert.SerializeObject(openDependencyRiskParams).Should().Be(expected);
    }
}
