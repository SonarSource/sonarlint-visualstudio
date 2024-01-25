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
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryDataTests
    {
        [TestMethod]
        public void XmlSerialization_RoundTrips()
        {
            var telemetrySerializer = new XmlSerializer(typeof(TelemetryData));

            var originalData = new TelemetryData
            {
                IsAnonymousDataShared = true,
                NumberOfDaysOfUse = 999,

                // Not serialized directly: converted then saved
                InstallationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(100),
                LastSavedAnalysisDate = DateTimeOffset.UtcNow - TimeSpan.FromHours(200),
                LastUploadDate = DateTimeOffset.UtcNow - -TimeSpan.FromMinutes(300),

                Analyses = new[]
                    {
                        new Analysis { Language = "js" },
                        new Analysis { Language = "csharp" },
                        new Analysis { Language = "xxx" }
                    }.ToList(),

                ShowHotspot = new ShowHotspot
                {
                    NumberOfRequests = 567
                },

                TaintVulnerabilities = new TaintVulnerabilities
                {
                    NumberOfIssuesInvestigatedRemotely = 88,
                    NumberOfIssuesInvestigatedLocally = 99
                },

                ServerNotifications = new ServerNotifications
                {
                    IsDisabled = true,
                    ServerNotificationCounters = new Dictionary<string, ServerNotificationCounter>
                    {
                        {"QUALITY_GATE", new ServerNotificationCounter
                        {
                            ReceivedCount = 11,
                            ClickedCount = 22
                        }},
                        {"NEW_ISSUES", new ServerNotificationCounter
                        {
                            ReceivedCount = 33,
                            ClickedCount = 44
                        }}
                    }
                },

                CFamilyProjectTypes = new CFamilyProjectTypes
                {
                    IsCMakeNonAnalyzable = true,
                    IsCMakeAnalyzable = true,
                    IsVcxAnalyzable = true,
                    IsVcxNonAnalyzable = true
                },

                RulesUsage = new RulesUsage
                {
                    DisabledByDefaultThatWereEnabled = new List<string>{"rule1", "rule2"},
                    EnabledByDefaultThatWereDisabled = new List<string> { "rule3", "rule4" },
                    RulesThatRaisedIssues = new List<string> { "rule5", "rule6" },
                    RulesWithAppliedQuickFixes = new List<string> { "rule7", "rule8" }
                }
            };

            string serializedData = null;
            using (var textWriter = new StringWriter())
            {
                telemetrySerializer.Serialize(textWriter, originalData);
                textWriter.Flush();
                serializedData = textWriter.ToString();
            }

            TelemetryData reloadedData = null;
            using (var textReader = new StringReader(serializedData))
            {
                reloadedData = telemetrySerializer.Deserialize(textReader) as TelemetryData;
            }

            reloadedData.IsAnonymousDataShared.Should().BeTrue();
            reloadedData.NumberOfDaysOfUse.Should().Be(999);

            reloadedData.InstallationDate.Should().Be(originalData.InstallationDate);
            reloadedData.LastSavedAnalysisDate.Should().Be(originalData.LastSavedAnalysisDate);
            reloadedData.LastUploadDate.Should().Be(originalData.LastUploadDate);

            reloadedData.Analyses.Count.Should().Be(3);
            reloadedData.Analyses[0].Language.Should().Be("js");
            reloadedData.Analyses[1].Language.Should().Be("csharp");
            reloadedData.Analyses[2].Language.Should().Be("xxx");

            reloadedData.ShowHotspot.Should().NotBeNull();
            reloadedData.ShowHotspot.NumberOfRequests.Should().Be(567);

            reloadedData.TaintVulnerabilities.Should().NotBeNull();
            reloadedData.TaintVulnerabilities.NumberOfIssuesInvestigatedRemotely.Should().Be(88);
            reloadedData.TaintVulnerabilities.NumberOfIssuesInvestigatedLocally.Should().Be(99);

            reloadedData.ServerNotifications.Should().NotBeNull();
            reloadedData.ServerNotifications.IsDisabled.Should().BeTrue();
            reloadedData.ServerNotifications.ServerNotificationCounters.Should().NotBeNull();
            reloadedData.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].Should().NotBeNull();
            reloadedData.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ReceivedCount.Should().Be(11);
            reloadedData.ServerNotifications.ServerNotificationCounters["QUALITY_GATE"].ClickedCount.Should().Be(22);
            reloadedData.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].Should().NotBeNull();
            reloadedData.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ReceivedCount.Should().Be(33);
            reloadedData.ServerNotifications.ServerNotificationCounters["NEW_ISSUES"].ClickedCount.Should().Be(44);

            reloadedData.CFamilyProjectTypes.IsCMakeNonAnalyzable.Should().BeTrue();
            reloadedData.CFamilyProjectTypes.IsCMakeAnalyzable.Should().BeTrue();
            reloadedData.CFamilyProjectTypes.IsVcxNonAnalyzable.Should().BeTrue();
            reloadedData.CFamilyProjectTypes.IsVcxAnalyzable.Should().BeTrue();

            reloadedData.RulesUsage.DisabledByDefaultThatWereEnabled.Should().BeEquivalentTo("rule1", "rule2");
            reloadedData.RulesUsage.EnabledByDefaultThatWereDisabled.Should().BeEquivalentTo("rule3", "rule4");
            reloadedData.RulesUsage.RulesThatRaisedIssues.Should().BeEquivalentTo("rule5", "rule6");
            reloadedData.RulesUsage.RulesWithAppliedQuickFixes.Should().BeEquivalentTo("rule7", "rule8");
        }

        [TestMethod]
        public void XmlSerialization_MissingValuesInXml_InitializedToEmpty()
        {
            var telemetrySerializer = new XmlSerializer(typeof(TelemetryData));

            var originalData = new TelemetryData();

            string serializedData;
            using (var textWriter = new StringWriter())
            {
                telemetrySerializer.Serialize(textWriter, originalData);
                textWriter.Flush();
                serializedData = textWriter.ToString();
            }

            TelemetryData reloadedData = null;
            using (var textReader = new StringReader(serializedData))
            {
                reloadedData = telemetrySerializer.Deserialize(textReader) as TelemetryData;
            }

            reloadedData.ServerNotifications.Should().NotBeNull();
            reloadedData.CFamilyProjectTypes.Should().NotBeNull();
            reloadedData.Analyses.Should().NotBeNull();
            reloadedData.TaintVulnerabilities.Should().NotBeNull();
            reloadedData.ShowHotspot.Should().NotBeNull();
        }
    }
}
