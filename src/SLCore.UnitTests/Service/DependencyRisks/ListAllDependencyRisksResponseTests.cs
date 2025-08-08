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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.DependencyRisks;

[TestClass]
public class ListAllDependencyRisksResponseTests
{
    [TestMethod]
    public void Deserialize_ShouldCorrectlyDeserializeJson()
    {
        const string json =
            """
            {
                "dependencyRisks": [
                    {
                        "id": "71814600-2924-4B88-9E1A-1AF0B46F8D48",
                        "type": "VULNERABILITY",
                        "severity": "HIGH",
                        "status": "OPEN",
                        "packageName": "vulnerable-package",
                        "packageVersion": "1.0.0",
                        "transitions": ["CONFIRM", "SAFE"]
                    },
                    {
                        "id": "EA425B61-AF22-48AC-98E1-78B644D34876",
                        "type": "PROHIBITED_LICENSE",
                        "severity": "MEDIUM",
                        "status": "CONFIRM",
                        "packageName": "license-issue-package",
                        "packageVersion": "2.1.0",
                        "transitions": ["REOPEN", "ACCEPT"]
                    }
                ]
            }
            """;

        var result = JsonConvert.DeserializeObject<ListAllDependencyRisksResponse>(json);

        var expected = new ListAllDependencyRisksResponse(
            [
                new(
                    Guid.Parse("71814600-2924-4B88-9E1A-1AF0B46F8D48"),
                    DependencyRiskType.VULNERABILITY,
                    DependencyRiskSeverity.HIGH,
                    DependencyRiskStatus.OPEN,
                    "vulnerable-package",
                    "1.0.0",
                    [DependencyRiskTransition.CONFIRM, DependencyRiskTransition.SAFE]),
                new(
                    Guid.Parse("EA425B61-AF22-48AC-98E1-78B644D34876"),
                    DependencyRiskType.PROHIBITED_LICENSE,
                    DependencyRiskSeverity.MEDIUM,
                    DependencyRiskStatus.CONFIRM,
                    "license-issue-package",
                    "2.1.0",
                    [DependencyRiskTransition.REOPEN, DependencyRiskTransition.ACCEPT])
            ]);

        result.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<ListAllDependencyRisksResponse>().ComparingByMembers<DependencyRiskDto>());
    }
}
