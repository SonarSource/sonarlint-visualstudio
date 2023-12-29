﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Generic;
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
            new ClientConstantsDto("TESTname", "TESTagent"),
            new FeatureFlagsDto(false, true, false, true, false, true, false),
            "storageRoot",
            "workDir",
            new List<string> { "myplugin1", "myplugin2" },
            new Dictionary<string, string> {{"myplugin3", "location"}},
            new List<Language> { Language.JS },
            new List<Language> { Language.CPP },
            new List<SonarQubeConnectionConfigurationDto> { new("con1", true, "localhost")},
            new List<SonarCloudConnectionConfigurationDto> { new("con2", false, "organization1")},
            "userHome",
            new Dictionary<string, StandaloneRuleConfigDto>
            {
                { "javascript:S1940", new StandaloneRuleConfigDto(true, new Dictionary<string, string>{{"prop", "val"}}) },
                { "typescript:S1940", new StandaloneRuleConfigDto(false, new Dictionary<string, string>()) },
            },
            false,
            new TelemetryClientConstantAttributesDto("TESTkey", "TESTname", "TESTversion", "TESTde", new Dictionary<string, object>{{"telemetryObj", new {field = 10}}}),
            "node"
        );

        const string expectedString = """
                                {
                                  "clientConstantInfo": {
                                    "name": "TESTname",
                                    "userAgent": "TESTagent"
                                  },
                                  "featureFlags": {
                                    "taintVulnerabilitiesEnabled": false,
                                    "shouldSynchronizeProjects": true,
                                    "shouldManageLocalServer": false,
                                    "enableSecurityHotspots": true,
                                    "shouldManageServerSentEvents": false,
                                    "enableDataflowBugDetection": true,
                                    "shouldManageFullSynchronization": false
                                  },
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
                                  "sonarQubeConnections": [
                                    {
                                      "serverUrl": "localhost",
                                      "disableNotification": true,
                                      "connectionId": "con1"
                                    }
                                  ],
                                  "sonarCloudConnections": [
                                    {
                                      "organization": "organization1",
                                      "disableNotification": false,
                                      "connectionId": "con2"
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
                                  "clientNodeJsPath": "node"
                                }
                                """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
