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
using SonarLint.VisualStudio.SLCore.Listener.Taint;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Taint;

[TestClass]
public class DidChangeTaintVulnerabilitiesParamsTests
{
    [TestMethod]
    public void Deserialized_AsExpected_SmokeTest()
    {
        const string serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "closedTaintVulnerabilityIds": [ "62294585-d219-4d07-8e40-6d28d2f2f90e", "62294585-d219-4d07-8e40-6d28d2f2f90e", "62294585-d219-4d07-8e40-6d28d2f2f90e" ],
              "addedTaintVulnerabilities": [
                {
                  "id": "62294585-d219-4d07-8e40-6d28d2f2f90e",
                  "sonarServerKey": "AXgSTUbU007Zyo8hMhUK",
                  "resolved": false,
                  "ruleKey": "roslyn.sonaranalyzer.security.cs:S2091",
                  "message": "Change this code to not construct this XPath expression from user-controlled data.",
                  "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\XPathInjectionController.cs",
                  "introductionDate": 1615214736000,
                  "severityMode": {
                    "cleanCodeAttribute": "COMPLETE",
                    "impacts": [
                      {
                        "softwareQuality": "SECURITY",
                        "impactSeverity": "HIGH"
                      }
                    ]
                  },
                  "severity": "BLOCKER",
                  "type": "VULNERABILITY",
                  "flows": [
                    {
                      "locations": []
                    },
                    {
                      "locations": []
                    }
                  ],
                  "textRange": {
                    "startLine": 23,
                    "startLineOffset": 27,
                    "endLine": 23,
                    "endLineOffset": 59,
                    "hash": "4fdeebd4a19fd4b1c4c5b9b43ea9f71e"
                  },
                  "ruleDescriptionContextKey": null,
                  "cleanCodeAttribute": "COMPLETE",
                  "impacts": {
                    "SECURITY": "HIGH"
                  },
                  "isOnNewCode": false
                },
                {
                  "id": "62294585-d219-4d07-8e40-6d28d2f2f90e",
                  "sonarServerKey": "AXgSTUbU007Zyo8hMhUK",
                  "resolved": false,
                  "ruleKey": "roslyn.sonaranalyzer.security.cs:S2091",
                  "message": "Change this code to not construct this XPath expression from user-controlled data.",
                  "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\XPathInjectionController.cs",
                  "introductionDate": 1615214736000,
                  "severityMode": {
                    "cleanCodeAttribute": "COMPLETE",
                    "impacts": [
                      {
                        "softwareQuality": "SECURITY",
                        "impactSeverity": "HIGH"
                      }
                    ]
                  },
                  "severity": "BLOCKER",
                  "type": "VULNERABILITY",
                  "flows": [
                    {
                      "locations": []
                    },
                    {
                      "locations": []
                    }
                  ],
                  "textRange": {
                    "startLine": 23,
                    "startLineOffset": 27,
                    "endLine": 23,
                    "endLineOffset": 59,
                    "hash": "4fdeebd4a19fd4b1c4c5b9b43ea9f71e"
                  },
                  "ruleDescriptionContextKey": null,
                  "cleanCodeAttribute": "COMPLETE",
                  "impacts": {
                    "SECURITY": "HIGH"
                  },
                  "isOnNewCode": false
                }
              ],
              "updatedTaintVulnerabilities": [
                {
                  "id": "62294585-d219-4d07-8e40-6d28d2f2f90e",
                  "sonarServerKey": "AXgSTUbU007Zyo8hMhUK",
                  "resolved": false,
                  "ruleKey": "roslyn.sonaranalyzer.security.cs:S2091",
                  "message": "Change this code to not construct this XPath expression from user-controlled data.",
                  "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\XPathInjectionController.cs",
                  "introductionDate": 1615214736000,
                  "severityMode": {
                    "cleanCodeAttribute": "COMPLETE",
                    "impacts": [
                      {
                        "softwareQuality": "SECURITY",
                        "impactSeverity": "HIGH"
                      }
                    ]
                  },
                  "severity": "BLOCKER",
                  "type": "VULNERABILITY",
                  "flows": [
                    {
                      "locations": []
                    },
                    {
                      "locations": []
                    }
                  ],
                  "textRange": {
                    "startLine": 23,
                    "startLineOffset": 27,
                    "endLine": 23,
                    "endLineOffset": 59,
                    "hash": "4fdeebd4a19fd4b1c4c5b9b43ea9f71e"
                  },
                  "ruleDescriptionContextKey": null,
                  "cleanCodeAttribute": "COMPLETE",
                  "impacts": {
                    "SECURITY": "HIGH"
                  },
                  "isOnNewCode": false
                }
              ]
            }
            """;

        var actual = JsonConvert.DeserializeObject<DidChangeTaintVulnerabilitiesParams>(serialized);

        actual.configurationScopeId.Should().Be("SLVS_Bound_VS2019");
        actual.closedTaintVulnerabilityIds.Should().HaveCount(3);
        actual.closedTaintVulnerabilityIds.Should().NotContain(Guid.Empty);
        actual.addedTaintVulnerabilities.Should().HaveCount(2);
        actual.addedTaintVulnerabilities.Should().NotContainNulls();
        actual.updatedTaintVulnerabilities.Should().HaveCount(1);
        actual.updatedTaintVulnerabilities.Should().NotContainNulls();
    }
}
