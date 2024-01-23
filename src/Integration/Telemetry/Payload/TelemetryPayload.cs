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
using SonarLint.VisualStudio.Integration.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration
{
    [DebuggerDisplay("Product: {SonarLintProduct}, SonarLintVersion: {SonarLintVersion}, " +
        "VisualStudioVersion: {VisualStudioVersion}, DaysInstall: {NumberOfDaysSinceInstallation}, " +
        "DaysOfUse: {NumberOfDaysOfUse}, IsConnected: {IsUsingConnectedMode}")]
    public sealed class TelemetryPayload
    {
        [JsonProperty("sonarlint_product")]
        public string SonarLintProduct { get; set; }

        [JsonProperty("sonarlint_version")]
        public string SonarLintVersion { get; set; }

        [JsonProperty("ide_version")]
        public string VisualStudioVersion { get; set; }

        [JsonProperty("slvs_ide_info")]
        public IdeVersionInformation VisualStudioVersionInformation { get; set; }

        [JsonProperty("days_since_installation")]
        public long NumberOfDaysSinceInstallation { get; set; }

        [JsonProperty("days_of_use")]
        public long NumberOfDaysOfUse { get; set; }

        [JsonProperty("connected_mode_used")]
        public bool IsUsingConnectedMode { get; set; }

        [JsonProperty("legacy_connected_mode_used")]
        public bool IsUsingLegacyConnectedMode { get; set; }

        [JsonProperty("connected_mode_sonarcloud")]
        public bool IsUsingSonarCloud { get; set; }

        [JsonProperty("install_time"), JsonConverter(typeof(ShortIsoDateTimeOffsetConverter))]
        public DateTimeOffset InstallDate { get; set; }

        [JsonProperty("system_time"), JsonConverter(typeof(ShortIsoDateTimeOffsetConverter))]
        public DateTimeOffset SystemDate { get; set; }

        [JsonProperty("analyses")]
        public List<Analysis> Analyses { get; set; }

        [JsonProperty("show_hotspot")]
        public ShowHotspot ShowHotspot { get; set; }

        [JsonProperty("taint_vulnerabilities")]
        public TaintVulnerabilities TaintVulnerabilities { get; set; }

        [JsonProperty("server_notifications")]
        public ServerNotifications ServerNotifications { get; set; }

        [JsonProperty("cfamily_project_types")]
        public CFamilyProjectTypes CFamilyProjectTypes { get; set; }

        [JsonProperty("rules")]
        public RulesUsage RulesUsage { get; set; }

        [JsonProperty("nodejs")]
        public string CompatibleNodeJsVersion { get; set; }

        [JsonProperty("max_nodejs_version")]
        public string MaxNodeJsVersion { get; set; }
    }
}
