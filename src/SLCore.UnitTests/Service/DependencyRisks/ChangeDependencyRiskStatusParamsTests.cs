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
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.DependencyRisks;

[TestClass]
public class ChangeDependencyRiskStatusParamsTests
{
    [TestMethod]
    [DataRow("other-scope", "ea425b61-af22-48ac-98e1-78b644d34876", DependencyRiskTransition.ACCEPT, "Accepting this risk")]
    [DataRow("project-scope", "247010fe-26de-4bf3-bed6-b12a8e8b13c6", DependencyRiskTransition.FIXED, "Fixed in version 2.0")]
    [DataRow("global-scope", "3baae21a-7c43-44bc-a675-deb71d2b3e18", DependencyRiskTransition.REOPEN, "")]
    public void Serialized_AsExpected(
        string configurationScopeId,
        string dependencyRiskKeyStr,
        DependencyRiskTransition transition,
        string comment)
    {
        var dependencyRiskKey = Guid.Parse(dependencyRiskKeyStr);
        var expected = $$"""{"configurationScopeId":"{{configurationScopeId}}","dependencyRiskKey":"{{dependencyRiskKeyStr}}","transition":"{{transition}}","comment":"{{comment}}"}""";
        var testSubject = new ChangeDependencyRiskStatusParams(configurationScopeId, dependencyRiskKey, transition, comment);

        var actual = JsonConvert.SerializeObject(testSubject);

        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("scope-id", "71814600-2924-4b88-9e1a-1af0b46f8d48", DependencyRiskTransition.CONFIRM)]
    [DataRow("folder-scope", "1a3a6fe9-f984-4a77-a06a-d824fe10f319", DependencyRiskTransition.SAFE)]
    [DataRow("other-scope", "ea425b61-af22-48ac-98e1-78b644d34876", DependencyRiskTransition.FIXED)]
    public void Serialized_WithNullComment_AsExpected(string configurationScopeId, string dependencyRiskKeyStr, DependencyRiskTransition transition)
    {
        var dependencyRiskKey = Guid.Parse(dependencyRiskKeyStr);
        var expected = $$"""{"configurationScopeId":"{{configurationScopeId}}","dependencyRiskKey":"{{dependencyRiskKeyStr}}","transition":"{{transition}}","comment":null}""";
        var testSubject = new ChangeDependencyRiskStatusParams(configurationScopeId, dependencyRiskKey, transition, null);

        var actual = JsonConvert.SerializeObject(testSubject);

        actual.Should().Be(expected);
    }
}
