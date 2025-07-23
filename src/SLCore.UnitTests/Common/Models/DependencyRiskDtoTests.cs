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

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Models;

[TestClass]
public class DependencyRiskDtoTests
{
    [TestMethod]
    public void Deserialize_ReturnsCorrectObject()
    {
        const string json =
            """
            {
              "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
              "type": "VULNERABILITY",
              "severity": "HIGH",
              "status": "OPEN",
              "packageName": "testPackage",
              "packageVersion": "1.0.0",
              "transitions": ["CONFIRM", "SAFE"]
            }
            """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        var expected = new DependencyRiskDto(
            new Guid("89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD"),
            DependencyRiskType.VULNERABILITY,
            DependencyRiskSeverity.HIGH,
            DependencyRiskStatus.OPEN,
            "testPackage",
            "1.0.0",
            [DependencyRiskTransition.CONFIRM, DependencyRiskTransition.SAFE]
        );

        result.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<DependencyRiskDto>());
    }

    [TestMethod]
    [DynamicData(nameof(ScaSeverities))]
    public void Deserialize_ReturnsCorrectSeverity(DependencyRiskSeverity expectedSeverity)
    {
        var json =
            $$"""
              {
                "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
                "type": "VULNERABILITY",
                "severity": "{{expectedSeverity}}",
                "status": "OPEN",
                "packageName": "testPackage",
                "packageVersion": "1.0.0",
                "transitions": []
              }
              """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.severity.Should().Be(expectedSeverity);
    }

    [TestMethod]
    [DynamicData(nameof(ScaTypes))]
    public void Deserialize_ReturnsCorrectType(DependencyRiskType expectedType)
    {
        var json =
            $$"""
              {
                "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
                "type": "{{expectedType}}",
                "severity": "HIGH",
                "status": "OPEN",
                "packageName": "testPackage",
                "packageVersion": "1.0.0",
                "transitions": []
              }
              """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.type.Should().Be(expectedType);
    }

    [TestMethod]
    [DynamicData(nameof(ScaStatuses))]
    public void Deserialize_ReturnsCorrectStatuses(DependencyRiskStatus expectedStatus)
    {
        var json =
            $$"""
              {
                "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
                "type": "VULNERABILITY",
                "severity": "HIGH",
                "status": "{{expectedStatus}}",
                "packageName": "testPackage",
                "packageVersion": "1.0.0",
                "transitions": []
              }
              """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.status.Should().Be(expectedStatus);
    }

    [TestMethod]
    public void Deserialize_EmptyTransitions_ReturnsEmptyList()
    {
        const string json =
            """
            {
              "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
              "type": "VULNERABILITY",
              "severity": "HIGH",
              "status": "OPEN",
              "packageName": "testPackage",
              "packageVersion": "1.0.0",
              "transitions": []
            }
            """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.Should().NotBeNull();
        result.transitions.Should().NotBeNull();
        result.transitions.Should().BeEmpty();
    }

    [TestMethod]
    [DynamicData(nameof(ScaTransitions))]
    public void Deserialize_ReturnsCorrectTransition(DependencyRiskTransition expectedTransition)
    {
        var json =
            $$"""
              {
                "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
                "type": "VULNERABILITY",
                "severity": "HIGH",
                "status": "OPEN",
                "packageName": "testPackage",
                "packageVersion": "1.0.0",
                "transitions": ["{{expectedTransition}}"]
              }
              """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.transitions.Should().BeEquivalentTo(expectedTransition);
    }

    [TestMethod]
    public void Deserialize_MultipleValidTransitions_ReturnsAllTransitions()
    {
        const string json =
            """
            {
              "id": "89F33AFC-E00F-4C87-AC8E-16C2EC6A51CD",
              "type": "VULNERABILITY",
              "severity": "HIGH",
              "status": "OPEN",
              "packageName": "testPackage",
              "packageVersion": "1.0.0",
              "transitions": ["CONFIRM", "FIXED", "ACCEPT"]
            }
            """;

        var result = JsonConvert.DeserializeObject<DependencyRiskDto>(json);

        result.transitions.Should().BeEquivalentTo([
            DependencyRiskTransition.CONFIRM,
            DependencyRiskTransition.FIXED,
            DependencyRiskTransition.ACCEPT
        ]);
    }

    public static IEnumerable<object[]> ScaSeverities => Enum.GetValues(typeof(DependencyRiskSeverity)).Cast<DependencyRiskSeverity>().Select(x => new object[] { x });
    public static IEnumerable<object[]> ScaTypes => Enum.GetValues(typeof(DependencyRiskType)).Cast<DependencyRiskType>().Select(x => new object[] { x });
    public static IEnumerable<object[]> ScaStatuses => Enum.GetValues(typeof(DependencyRiskStatus)).Cast<DependencyRiskStatus>().Select(x => new object[] { x });
    public static IEnumerable<object[]> ScaTransitions => Enum.GetValues(typeof(DependencyRiskTransition)).Cast<DependencyRiskTransition>().Select(x => new object[] { x });
}
