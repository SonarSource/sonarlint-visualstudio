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
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class RaiseIssuesParamsTests
{
    [TestMethod]
    public void RaisedIssueDto_MqrMode_DeserializedCorrectly()
    {
        var expected = new RaiseFindingParams<RaisedIssueDto>("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedIssueDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            new MQRModeDetails(CleanCodeAttribute.COMPLETE, [new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.LOW)]))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "issuesByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "cleanCodeAttribute": "COMPLETE",
                       "impacts": [
                         {
                           "softwareQuality": "MAINTAINABILITY",
                           "impactSeverity": "LOW"
                         }
                       ]
                     },
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseFindingParams<RaisedIssueDto>>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseFindingParams<RaisedIssueDto>>().ComparingByMembers<RaisedIssueDto>().ComparingByMembers<MQRModeDetails>());
    }

    [TestMethod]
    public void RaisedIssueDto_StandardMode_DeserializedCorrectly()
    {
        var expected = new RaiseFindingParams<RaisedIssueDto>("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedIssueDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            new StandardModeDetails(IssueSeverity.BLOCKER, RuleType.BUG))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "issuesByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "severity": "BLOCKER",
                       "type": "BUG"
                     },
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseFindingParams<RaisedIssueDto>>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseFindingParams<RaisedIssueDto>>().ComparingByMembers<RaisedIssueDto>().ComparingByMembers<MQRModeDetails>());
    }

    [TestMethod]
    public void RaiseHotspotParams_MqrMode_DeserializedCorrectly()
    {
        var expected = new RaiseHotspotParams("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedHotspotDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedHotspotDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            VulnerabilityProbability.HIGH,
                            HotspotStatus.TO_REVIEW,
                            new MQRModeDetails(CleanCodeAttribute.CLEAR, [new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH)]))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "hotspotsByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "cleanCodeAttribute": "CLEAR",
                       "impacts": [
                         {
                           "softwareQuality": "SECURITY",
                           "impactSeverity": "HIGH"
                         }
                       ]
                     },
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null,
                    "vulnerabilityProbability": "HIGH",
                    "status": "TO_REVIEW"
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseHotspotParams>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseHotspotParams>().ComparingByMembers<RaisedHotspotDto>().ComparingByMembers<MQRModeDetails>());
    }

    [TestMethod]
    public void RaiseHotspotParams_StandardMode_DeserializedCorrectly()
    {
        var expected = new RaiseHotspotParams("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedHotspotDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedHotspotDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            VulnerabilityProbability.HIGH,
                            HotspotStatus.TO_REVIEW,
                            new StandardModeDetails(IssueSeverity.MINOR, RuleType.VULNERABILITY))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "hotspotsByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "severity": "MINOR",
                       "type": "VULNERABILITY"
                     },
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null,
                    "vulnerabilityProbability": "HIGH",
                    "status": "TO_REVIEW"
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseHotspotParams>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseHotspotParams>().ComparingByMembers<RaisedHotspotDto>().ComparingByMembers<MQRModeDetails>());
    }

    /// <summary>
    ///     The fields severity, type are still set by SlCore, but they are deprecated and should be ignored.
    ///     Instead, the values from <see cref="StandardModeDetails" /> of <see cref="RaisedIssueDto.severityMode" /> should be used
    /// </summary>
    [TestMethod]
    public void RaisedIssueDto_StandardMode_IgnoresDeprecatedFields()
    {
        var expected = new RaiseFindingParams<RaisedIssueDto>("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedIssueDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            new StandardModeDetails(IssueSeverity.BLOCKER, RuleType.BUG))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "issuesByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "severity": "BLOCKER",
                       "type": "BUG"
                     },
                    "severity": "MINOR",
                    "type": "CODE_SMELL",
                    "cleanCodeAttribute": "TRUSTWORTHY",
                    "impacts": [
                      {
                        "softwareQuality": "SECURITY",
                        "impactSeverity": "HIGH"
                      }
                    ],
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseFindingParams<RaisedIssueDto>>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseFindingParams<RaisedIssueDto>>().ComparingByMembers<RaisedIssueDto>().ComparingByMembers<MQRModeDetails>());
    }

    /// <summary>
    ///     The fields cleanCodeAttribute, impacts are still set by SlCore, but they are deprecated and should be ignored.
    ///     Instead, the values from <see cref="MQRModeDetails" /> of <see cref="RaisedIssueDto.severityMode" /> should be used
    /// </summary>
    [TestMethod]
    public void RaisedIssueDto_MqrMode_IgnoresDeprecatedFields()
    {
        var expected = new RaiseFindingParams<RaisedIssueDto>("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedIssueDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedIssueDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            new MQRModeDetails(CleanCodeAttribute.COMPLETE, [new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.LOW)]))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "issuesByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "cleanCodeAttribute": "COMPLETE",
                       "impacts": [
                         {
                           "softwareQuality": "MAINTAINABILITY",
                           "impactSeverity": "LOW"
                         }
                       ]
                     },
                     "severity": "MINOR",
                     "type": "CODE_SMELL",
                     "cleanCodeAttribute": "TRUSTWORTHY",
                     "impacts": [
                       {
                         "softwareQuality": "SECURITY",
                         "impactSeverity": "HIGH"
                       }
                     ],
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseFindingParams<RaisedIssueDto>>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseFindingParams<RaisedIssueDto>>().ComparingByMembers<RaisedIssueDto>().ComparingByMembers<MQRModeDetails>());
    }

    /// <summary>
    ///     The fields cleanCodeAttribute, impacts are still set by SlCore, but they are deprecated and should be ignored.
    ///     Instead, the values from <see cref="MQRModeDetails" /> of <see cref="RaisedIssueDto.severityMode" /> should be used
    /// </summary>
    [TestMethod]
    public void RaiseHotspotParams_MqrMode_IgnoresDeprecatedFields()
    {
        var expected = new RaiseHotspotParams("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedHotspotDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedHotspotDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            VulnerabilityProbability.HIGH,
                            HotspotStatus.TO_REVIEW,
                            new MQRModeDetails(CleanCodeAttribute.CLEAR, [new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH)]))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "hotspotsByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "cleanCodeAttribute": "CLEAR",
                       "impacts": [
                         {
                           "softwareQuality": "SECURITY",
                           "impactSeverity": "HIGH"
                         }
                       ]
                     },
                     "severity": "MINOR",
                     "type": "CODE_SMELL",
                     "cleanCodeAttribute": "TRUSTWORTHY",
                     "impacts": [
                       {
                         "softwareQuality": "RELIABILITY",
                         "impactSeverity": "LOW"
                       }
                     ],
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null,
                    "vulnerabilityProbability": "HIGH",
                    "status": "TO_REVIEW"
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseHotspotParams>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseHotspotParams>().ComparingByMembers<RaisedHotspotDto>().ComparingByMembers<MQRModeDetails>());
    }

    /// <summary>
    ///     The fields severity, type are still set by SlCore, but they are deprecated and should be ignored.
    ///     Instead, the values from <see cref="StandardModeDetails" /> of <see cref="RaisedIssueDto.severityMode" /> should be used
    /// </summary>
    [TestMethod]
    public void RaiseHotspotParams_StandardMode_IgnoresDeprecatedFields()
    {
        var expected = new RaiseHotspotParams("SLVS_Bound_VS2019",
            new Dictionary<FileUri, List<RaisedHotspotDto>>
            {
                {
                    new FileUri(
                        "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml"),
                    [
                        new RaisedHotspotDto(Guid.Parse("10bd4422-7d55-402f-889c-e080dbe4c781"),
                            null,
                            "secrets:S6336",
                            "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                            DateTimeOffset.FromUnixTimeMilliseconds(1718182975467),
                            true,
                            false,
                            new TextRangeDto(14, 24, 14, 54),
                            [],
                            [],
                            null,
                            VulnerabilityProbability.HIGH,
                            HotspotStatus.TO_REVIEW,
                            new StandardModeDetails(IssueSeverity.MINOR, RuleType.VULNERABILITY))
                    ]
                }
            },
            false,
            Guid.Parse("11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"));

        var serialized =
            """
            {
              "configurationScopeId": "SLVS_Bound_VS2019",
              "hotspotsByFileUri": {
                "file:///C:/Users/developer/Documents/Repos/sonarlint-visualstudio-sampleprojects%20AAA%20ЖЖЖЖ/bound/sonarcloud/SLVS_Samples_Bound_VS2019/Secrets/ShouldExclude/Excluded.yml": [
                  {
                    "id": "10bd4422-7d55-402f-889c-e080dbe4c781",
                    "serverKey": null,
                    "ruleKey": "secrets:S6336",
                    "primaryMessage": "Make sure this Alibaba Cloud Access Key Secret gets revoked, changed, and removed from the code.",
                    "severityMode": {
                       "severity": "MINOR",
                       "type": "VULNERABILITY"
                     },
                     "severity": "INFO",
                     "type": "SECURITY_HOTSPOT",
                     "cleanCodeAttribute": "TRUSTWORTHY",
                     "impacts": [
                       {
                         "softwareQuality": "RELIABILITY",
                         "impactSeverity": "LOW"
                       }
                     ],
                    "introductionDate": 1718182975467,
                    "isOnNewCode": true,
                    "resolved": false,
                    "textRange": {
                      "startLine": 14,
                      "startLineOffset": 24,
                      "endLine": 14,
                      "endLineOffset": 54
                    },
                    "flows": [],
                    "quickFixes": [],
                    "ruleDescriptionContextKey": null,
                    "vulnerabilityProbability": "HIGH",
                    "status": "TO_REVIEW"
                  }
                ]
              },
              "isIntermediatePublication": false,
              "analysisId": "11ec4b5a-8ff6-4211-ab95-8c16eb8c7f0a"
            }
            """;

        var deserialized = JsonConvert.DeserializeObject<RaiseHotspotParams>(serialized);

        deserialized.Should().BeEquivalentTo(expected, options =>
            options.ComparingByMembers<RaiseHotspotParams>().ComparingByMembers<RaisedHotspotDto>().ComparingByMembers<MQRModeDetails>());
    }
}
