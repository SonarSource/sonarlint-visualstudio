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
using SonarLint.VisualStudio.SLCore.Service.SCA;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.SCA;

[TestClass]
public class GetDependencyRiskDetailsResponseTest
{
    [TestMethod]
    public void Deserialize_AsExpected()
    {
        var json = @"{
            ""key"": ""risk-key"",
            ""severity"": ""HIGH"",
            ""packageName"": ""Example.Package"",
            ""version"": ""1.2.3"",
            ""type"": ""VULNERABILITY"",
            ""vulnerabilityId"": ""CVE-2024-0001"",
            ""description"": ""A sample vulnerability description."",
            ""affectedPackages"": [
                {
                    ""purl"": ""Example.Package"",
                    ""recommendation"": ""1.2.3"",
                    ""recommendationDetails"": {
                        ""impactScore"": 5,
                        ""impactDescription"": ""High impact"",
                        ""realIssue"": true,
                        ""falsePositiveReason"": ""not a false positive"",
                        ""includesDev"": false,
                        ""specificMethodsAffected"": true,
                        ""specificMethodsDescription"": ""MethodA, MethodB"",
                        ""otherConditions"": false,
                        ""otherConditionsDescription"": ""some more condition"",
                        ""workaroundAvailable"": true,
                        ""workaroundDescription"": ""Apply patch 6.6.6"",
                        ""visibility"": ""public""
                    }
                }
            ]
        }";

        var expected = new GetDependencyRiskDetailsResponse(
            "risk-key",
            ScaSeverity.HIGH,
            "Example.Package",
            "1.2.3",
            ScaType.VULNERABILITY,
            "CVE-2024-0001",
            "A sample vulnerability description.",
            new List<AffectedPackageDto>
            {
                new("Example.Package", "1.2.3", new RecommendationDetailsDto(
                    5,
                    "High impact",
                    true,
                    "not a false positive",
                    false,
                    true,
                    "MethodA, MethodB",
                    false,
                    "some more condition",
                    true,
                    "Apply patch 6.6.6",
                    "public"
                ))
            }
        );

        var actual = JsonConvert.DeserializeObject<GetDependencyRiskDetailsResponse>(json);
        actual.Should().BeEquivalentTo(expected,
            options => options.ComparingByMembers<GetDependencyRiskDetailsResponse>().ComparingByMembers<AffectedPackageDto>().ComparingByMembers<RecommendationDetailsDto>());
    }
}
