/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Service.Taint;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Taint;

[TestClass]
public class ListAllTaintsResponseTests
{
    [TestMethod]
    public void Deserialized_AsExpected_SmokeTest()
    {
        const string serialized = """
                                  {
                                    "taintVulnerabilities": [
                                      {
                                        "id": "f1276bb9-54a4-4cbd-b4ac-41d2541302ee",
                                        "sonarServerKey": "AXgSTUZl007Zyo8hMhT-",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S5135",
                                        "message": "Change this code to not deserialize user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\XmlSerializerInjectionController.cs",
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
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 20,
                                          "startLineOffset": 32,
                                          "endLine": 20,
                                          "endLineOffset": 58,
                                          "hash": "f677236678ac4b2ab451d66d4b251e8f"
                                        },
                                        "ruleDescriptionContextKey": null,
                                        "cleanCodeAttribute": "COMPLETE",
                                        "impacts": {
                                          "SECURITY": "HIGH"
                                        },
                                        "isOnNewCode": false
                                      },
                                      {
                                        "id": "95c062cf-b30f-4cc4-88db-f9fee7344639",
                                        "sonarServerKey": "AXgSTUbP007Zyo8hMhUG",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S2083",
                                        "message": "Change this code to not construct the path from user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\MixedIssuesController.cs",
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
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 15,
                                          "startLineOffset": 12,
                                          "endLine": 15,
                                          "endLineOffset": 43,
                                          "hash": "75a2a40f1881db4654f6860a1114a0bf"
                                        },
                                        "ruleDescriptionContextKey": null,
                                        "cleanCodeAttribute": "COMPLETE",
                                        "impacts": {
                                          "SECURITY": "HIGH"
                                        },
                                        "isOnNewCode": false
                                      },
                                      {
                                        "id": "a7ef1f6e-523b-49f5-a29a-9b549695b0e0",
                                        "sonarServerKey": "AXgSTUbP007Zyo8hMhUH",
                                        "resolved": true,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S5135",
                                        "message": "Change this code to not deserialize user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\MixedIssuesController.cs",
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
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 34,
                                          "startLineOffset": 32,
                                          "endLine": 34,
                                          "endLineOffset": 58,
                                          "hash": "f677236678ac4b2ab451d66d4b251e8f"
                                        },
                                        "ruleDescriptionContextKey": null,
                                        "cleanCodeAttribute": "COMPLETE",
                                        "impacts": {
                                          "SECURITY": "HIGH"
                                        },
                                        "isOnNewCode": false
                                      },
                                      {
                                        "id": "a7cbd2da-71e4-4a93-80fb-f0cd6ca9da89",
                                        "sonarServerKey": "AXgSTUbP007Zyo8hMhUF",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S2091",
                                        "message": "Change this code to not construct this XPath expression from user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\MixedIssuesController.cs",
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
                                          "startLine": 58,
                                          "startLineOffset": 27,
                                          "endLine": 58,
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
                                        "id": "352ea2a3-e77f-49a8-880b-9549504be448",
                                        "sonarServerKey": "AYfh2w3VueSGJHh8vWDj",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S5146",
                                        "message": "Change this code to not perform redirects based on user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Controllers\\WeatherForecastController.cs",
                                        "introductionDate": 1681210777000,
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
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 43,
                                          "startLineOffset": 12,
                                          "endLine": 43,
                                          "endLineOffset": 34,
                                          "hash": "9e3e6f8af5838423c1b97b7d423cebab"
                                        },
                                        "ruleDescriptionContextKey": null,
                                        "cleanCodeAttribute": "COMPLETE",
                                        "impacts": {
                                          "SECURITY": "HIGH"
                                        },
                                        "isOnNewCode": true
                                      },
                                      {
                                        "id": "abca89aa-c7a7-44c1-92e6-41b7a639e51c",
                                        "sonarServerKey": "AXgSV_UkF9imBvjh6CmG",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S2083",
                                        "message": "Change this code to not construct the path from user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\MultiFlow_IOPathInjectionController.cs",
                                        "introductionDate": 1615215436000,
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
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 17,
                                          "startLineOffset": 12,
                                          "endLine": 17,
                                          "endLineOffset": 43,
                                          "hash": "75a2a40f1881db4654f6860a1114a0bf"
                                        },
                                        "ruleDescriptionContextKey": null,
                                        "cleanCodeAttribute": "COMPLETE",
                                        "impacts": {
                                          "SECURITY": "HIGH"
                                        },
                                        "isOnNewCode": false
                                      },
                                      {
                                        "id": "2f91bf04-f867-413c-8453-8f36d9756001",
                                        "sonarServerKey": "AXgSV_UkF9imBvjh6CmF",
                                        "resolved": false,
                                        "ruleKey": "roslyn.sonaranalyzer.security.cs:S2083",
                                        "message": "Change this code to not construct the path from user-controlled data.",
                                        "ideFilePath": "sonarlint-visualstudio-sampleprojects\\bound\\sonarcloud\\SLVS_Samples_Bound_VS2019\\Taint_CSharp_NetCore_WebAppReact\\Taint\\MultiFlow_IOPathInjectionController.cs",
                                        "introductionDate": 1615215436000,
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
                                          },
                                          {
                                            "locations": []
                                          }
                                        ],
                                        "textRange": {
                                          "startLine": 53,
                                          "startLineOffset": 12,
                                          "endLine": 53,
                                          "endLineOffset": 43,
                                          "hash": "75a2a40f1881db4654f6860a1114a0bf"
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
                                    ]
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<ListAllTaintsResponse>(serialized);

        actual.taintVulnerabilities.Count.Should().Be(8);
        actual.taintVulnerabilities.Should().NotContainNulls();
    }
}
