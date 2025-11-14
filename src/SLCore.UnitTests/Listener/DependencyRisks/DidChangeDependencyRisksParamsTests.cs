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
using SonarLint.VisualStudio.SLCore.Listener.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.DependencyRisks;

[TestClass]
public class DidChangeDependencyRisksParamsTests
{
    [TestMethod]
    public void Deserialize_ShouldCorrectlyDeserializeJson()
    {
        const string json =
            """
            {
                "configurationScopeId": "test-scope-id",
                "closedDependencyRiskIds": [
                    "1A3A6FE9-F984-4A77-A06A-D824FE10F319",
                    "3BAAE21A-7C43-44BC-A675-DEB71D2B3E18"
                ],
                "addedDependencyRisks": [
                    {
                        "id": "71814600-2924-4B88-9E1A-1AF0B46F8D48",
                        "type": "VULNERABILITY",
                        "severity": "HIGH",
                        "status": "OPEN",
                        "packageName": "vulnerable-package",
                        "vulnerabilityId": "CVE-2023-12345",
                        "cvssScore": "8.5",
                        "packageVersion": "1.0.0",
                        "transitions": ["CONFIRM", "SAFE"]
                    }
                ],
                "updatedDependencyRisks": [
                    {
                        "id": "EA425B61-AF22-48AC-98E1-78B644D34876",
                        "type": "PROHIBITED_LICENSE",
                        "severity": "MEDIUM",
                        "status": "CONFIRM",
                        "packageName": "license-issue-package",
                        "packageVersion": "2.1.0",
                        "vulnerabilityId": null,
                        "cvssScore": null,
                        "transitions": ["REOPEN", "ACCEPT"]
                    },
                    {
                        "id": "247010FE-26DE-4BF3-BED6-B12A8E8B13C6",
                        "type": "VULNERABILITY",
                        "severity": "BLOCKER",
                        "status": "ACCEPT",
                        "packageName": "critical-package",
                        "packageVersion": "0.9.2",
                        "vulnerabilityId": "CVE-2023-67890",
                        "cvssScore": "9.8",
                        "transitions": ["REOPEN", "FIXED"]
                    }
                ]
            }
            """;

        var result = JsonConvert.DeserializeObject<DidChangeDependencyRisksParams>(json);

        var expected = new DidChangeDependencyRisksParams(
            "test-scope-id",
            [Guid.Parse("1A3A6FE9-F984-4A77-A06A-D824FE10F319"), Guid.Parse("3BAAE21A-7C43-44BC-A675-DEB71D2B3E18")],
            [
                new(
                    Guid.Parse("71814600-2924-4B88-9E1A-1AF0B46F8D48"),
                    DependencyRiskType.VULNERABILITY,
                    DependencyRiskSeverity.HIGH,
                    DependencyRiskStatus.OPEN,
                    "vulnerable-package",
                    "1.0.0",
                    "CVE-2023-12345",
                    "8.5",
                    [DependencyRiskTransition.CONFIRM, DependencyRiskTransition.SAFE])
            ],
            [
                new(
                    Guid.Parse("EA425B61-AF22-48AC-98E1-78B644D34876"),
                    DependencyRiskType.PROHIBITED_LICENSE,
                    DependencyRiskSeverity.MEDIUM,
                    DependencyRiskStatus.CONFIRM,
                    "license-issue-package",
                    "2.1.0",
                    null,
                    null,
                    [DependencyRiskTransition.REOPEN, DependencyRiskTransition.ACCEPT]),

                new(
                    Guid.Parse("247010FE-26DE-4BF3-BED6-B12A8E8B13C6"),
                    DependencyRiskType.VULNERABILITY,
                    DependencyRiskSeverity.BLOCKER,
                    DependencyRiskStatus.ACCEPT,
                    "critical-package",
                    "0.9.2",
                    "CVE-2023-67890",
                    "9.8",
                    [DependencyRiskTransition.REOPEN, DependencyRiskTransition.FIXED])
            ]);

        result.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<DidChangeDependencyRisksParams>().ComparingByMembers<DependencyRiskDto>());
    }
}
