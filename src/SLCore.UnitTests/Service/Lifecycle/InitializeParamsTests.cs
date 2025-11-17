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
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Lifecycle;

[TestClass]
public class InitializeParamsTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var testSubject = new InitializeParams(
            new ClientConstantInfoDto("TESTname", "TESTagent"),
            new HttpConfigurationDto(new SslConfigurationDto()),
            [
                BackendCapability.PROJECT_SYNCHRONIZATION,
                BackendCapability.EMBEDDED_SERVER,
                BackendCapability.SECURITY_HOTSPOTS,
                BackendCapability.SERVER_SENT_EVENTS,
                BackendCapability.FULL_SYNCHRONIZATION,
                BackendCapability.TELEMETRY,
                BackendCapability.MONITORING,
                BackendCapability.ISSUE_STREAMING,
            ],
            "storageRoot",
            "workDir",
            ["myplugin1", "myplugin2"],
            new() { { "myplugin3", "location" } },
            [Language.JS],
            [Language.CPP],
            ["csharp"],
            [new SonarQubeConnectionConfigurationDto("con1", true, "localhost")],
            [
                new SonarCloudConnectionConfigurationDto("con2", false, "organization1"),
                new SonarCloudConnectionConfigurationDto("con3", true, "organization2", SonarCloudRegion.US)
            ],
            "userHome",
            new Dictionary<string, StandaloneRuleConfigDto>
            {
                { "javascript:S1940", new StandaloneRuleConfigDto(true, new Dictionary<string, string> { { "prop", "val" } }) },
                { "typescript:S1940", new StandaloneRuleConfigDto(false, new Dictionary<string, string>()) },
            },
            false,
            new TelemetryClientConstantAttributesDto("TESTkey", "TESTname", "TESTversion", "TESTde", new Dictionary<string, object> { { "telemetryObj", new { field = 10 } } }),
            new TelemetryMigrationDto(true, new DateTimeOffset(2024, 07, 30, 14, 46, 28, TimeSpan.FromHours(1)), 123),
            new LanguageSpecificRequirements(new JsTsRequirementsDto("node", "bundlePath")),
            automaticAnalysisEnabled: true
        );

        const string expectedString = """
                                      {
                                        "clientConstantInfo": {
                                          "name": "TESTname",
                                          "userAgent": "TESTagent"
                                        },
                                        "httpConfiguration": {
                                          "sslConfiguration": {}
                                        },
                                        "backendCapabilities": [
                                          "PROJECT_SYNCHRONIZATION",
                                          "EMBEDDED_SERVER",
                                          "SECURITY_HOTSPOTS",
                                          "SERVER_SENT_EVENTS",
                                          "FULL_SYNCHRONIZATION",
                                          "TELEMETRY",
                                          "MONITORING",
                                          "ISSUE_STREAMING"
                                        ],
                                        "storageRoot": "storageRoot",
                                        "workDir": "workDir",
                                        "embeddedPluginPaths": [
                                          "myplugin1",
                                          "myplugin2"
                                        ],
                                        "connectedModeEmbeddedPluginPathsByKey": {
                                          "myplugin3": "location"
                                        },
                                        "enabledLanguagesInStandaloneMode": [
                                          "JS"
                                        ],
                                        "extraEnabledLanguagesInConnectedMode": [
                                          "CPP"
                                        ],
                                        "disabledPluginKeysForAnalysis": [
                                          "csharp"
                                        ],
                                        "sonarQubeConnections": [
                                          {
                                            "serverUrl": "localhost",
                                            "connectionId": "con1",
                                            "disableNotification": true
                                          }
                                        ],
                                        "sonarCloudConnections": [
                                          {
                                            "organization": "organization1",
                                            "region": "EU",
                                            "connectionId": "con2",
                                            "disableNotification": false
                                          },
                                          {
                                            "organization": "organization2",
                                            "region": "US",
                                            "connectionId": "con3",
                                            "disableNotification": true
                                          }
                                        ],
                                        "sonarlintUserHome": "userHome",
                                        "standaloneRuleConfigByKey": {
                                          "javascript:S1940": {
                                            "isActive": true,
                                            "paramValueByKey": {
                                              "prop": "val"
                                            }
                                          },
                                          "typescript:S1940": {
                                            "isActive": false,
                                            "paramValueByKey": {}
                                          }
                                        },
                                        "isFocusOnNewCode": false,
                                        "telemetryConstantAttributes": {
                                          "productKey": "TESTkey",
                                          "productName": "TESTname",
                                          "productVersion": "TESTversion",
                                          "ideVersion": "TESTde",
                                          "additionalAttributes": {
                                            "telemetryObj": {
                                              "field": 10
                                            }
                                          }
                                        },
                                        "telemetryMigration": {
                                          "isEnabled": true,
                                          "installTime": "2024-07-30T14:46:28+01:00",
                                          "numUseDays": 123
                                        },
                                        "languageSpecificRequirements": {
                                          "jsTsRequirements": {
                                            "clientNodeJsPath": "node",
                                            "bundlePath": "bundlePath"
                                          }
                                        },
                                        "automaticAnalysisEnabled": true,
                                        "logLevel": "DEBUG"
                                      }
                                      """; // todo: SLVS-2625 Provide logging level to SLCore to avoid client-side filtering

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
