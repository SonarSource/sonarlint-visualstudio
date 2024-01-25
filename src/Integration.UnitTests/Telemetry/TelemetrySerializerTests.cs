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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetrySerializerTests
    {
        [TestMethod]
        public void Serialize()
        {
            // Check serialization produces json in the expected format
            var payload = new TelemetryPayload
            {
                SonarLintProduct = "my product",
                SonarLintVersion = "1.2.3.4",
                VisualStudioVersion = "15.0.1.2",
                VisualStudioVersionInformation = new IdeVersionInformation
                {
                    DisplayVersion = "16.9.0 Preview 3.0",
                    InstallationVersion = "16.9.30914.41",
                    DisplayName = "Visual Studio Enterprise 2019"
                },
                NumberOfDaysSinceInstallation = 234,
                NumberOfDaysOfUse = 123,
                IsUsingConnectedMode = true,
                IsUsingLegacyConnectedMode = true,
                IsUsingSonarCloud = true,

                // Adding some ticks to ensure that we send just the milliseconds in the serialized payload
                InstallDate = new DateTimeOffset(2017, 12, 23, 8, 25, 35, 456, TimeSpan.FromHours(1)).AddTicks(123),
                SystemDate = new DateTimeOffset(2018, 3, 15, 18, 55, 10, 123, TimeSpan.FromHours(1)).AddTicks(123),

                Analyses = new []
                {
                    new Analysis { Language = "js" },
                    new Analysis { Language = "csharp" },
                    new Analysis { Language = "vbnet" }
                }.ToList(),
                ShowHotspot = new ShowHotspot { NumberOfRequests = 567 },
                TaintVulnerabilities = new TaintVulnerabilities { NumberOfIssuesInvestigatedLocally = 654, NumberOfIssuesInvestigatedRemotely = 321},
                ServerNotifications = new ServerNotifications
                {
                    IsDisabled = true,
                    ServerNotificationCounters = new Dictionary<string, ServerNotificationCounter>()
                    {
                        {"QUALITY_GATE", new ServerNotificationCounter
                        {
                            ReceivedCount = 111,
                            ClickedCount = 222
                        }},
                        {"NEW_ISSUES", new ServerNotificationCounter
                        {
                            ReceivedCount = 333,
                            ClickedCount = 444
                        }}
                    }
                },
                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsCMakeNonAnalyzable = true,
                    IsCMakeAnalyzable = true,
                    IsVcxNonAnalyzable = true,
                    IsVcxAnalyzable = true
                },
                RulesUsage = new RulesUsage
                {
                    DisabledByDefaultThatWereEnabled = new List<string> { "rule1", "rule2" },
                    EnabledByDefaultThatWereDisabled = new List<string> { "rule3", "rule4" },
                    RulesThatRaisedIssues = new List<string> { "rule5", "rule6" },
                    RulesWithAppliedQuickFixes = new List<string> { "rule7", "rule8" }
                },
                CompatibleNodeJsVersion = "some version",
                MaxNodeJsVersion = "some max version"
            };

            var serialized = TelemetrySerializer.Serialize(payload);

            var expected = @"{
  ""sonarlint_product"": ""my product"",
  ""sonarlint_version"": ""1.2.3.4"",
  ""ide_version"": ""15.0.1.2"",
  ""slvs_ide_info"": {
    ""name"": ""Visual Studio Enterprise 2019"",
    ""install_version"": ""16.9.30914.41"",
    ""display_version"": ""16.9.0 Preview 3.0""
  },
  ""days_since_installation"": 234,
  ""days_of_use"": 123,
  ""connected_mode_used"": true,
  ""legacy_connected_mode_used"": true,
  ""connected_mode_sonarcloud"": true,
  ""install_time"": ""2017-12-23T08:25:35.456+01:00"",
  ""system_time"": ""2018-03-15T18:55:10.123+01:00"",
  ""analyses"": [
    {
      ""language"": ""js""
    },
    {
      ""language"": ""csharp""
    },
    {
      ""language"": ""vbnet""
    }
  ],
  ""show_hotspot"": {
    ""requests_count"": 567
  },
  ""taint_vulnerabilities"": {
    ""investigated_locally_count"": 654,
    ""investigated_remotely_count"": 321
  },
  ""server_notifications"": {
    ""disabled"": true,
    ""count_by_type"": {
      ""QUALITY_GATE"": {
        ""received"": 111,
        ""clicked"": 222
      },
      ""NEW_ISSUES"": {
        ""received"": 333,
        ""clicked"": 444
      }
    }
  },
  ""cfamily_project_types"": {
    ""cmake_analyzable"": true,
    ""cmake_non_analyzable"": true,
    ""vcx_analyzable"": true,
    ""vcx_non_analyzable"": true
  },
  ""rules"": {
    ""default_disabled"": [
      ""rule3"",
      ""rule4""
    ],
    ""non_default_enabled"": [
      ""rule1"",
      ""rule2""
    ],
    ""raised_issues"": [
      ""rule5"",
      ""rule6""
    ],
    ""quick_fix_applied"": [
      ""rule7"",
      ""rule8""
    ]
  },
  ""nodejs"": ""some version"",
  ""max_nodejs_version"": ""some max version""
}";
            serialized.Should().Be(expected);
        }
    }
}
