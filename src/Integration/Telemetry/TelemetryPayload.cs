/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

        [JsonProperty("days_since_installation")]
        public long NumberOfDaysSinceInstallation { get; set; }

        [JsonProperty("days_of_use")]
        public long NumberOfDaysOfUse { get; set; }

        [JsonProperty("connected_mode_used")]
        public bool IsUsingConnectedMode { get; set; }

        [JsonProperty("connected_mode_sonarcloud")]
        public bool IsUsingSonarCloud { get; set; }

        [JsonProperty("install_time"), JsonConverter(typeof(ShortIsoDateTimeOffsetConverter))]
        public DateTimeOffset InstallDate { get; set; }

        [JsonProperty("system_time"), JsonConverter(typeof(ShortIsoDateTimeOffsetConverter))]
        public DateTimeOffset SystemDate { get; set; }

        [JsonProperty("analyses")]
        public List<Analysis> Analyses { get; set; }
    }

    // We want to produce the same format as for IntelliJ and Eclipse (although
    // currently we are only recording the languages used, not the analysis durations).
    //
    // Example from Eclipse:
    //   "analyses": [
    //  {
    //    "language": "java",
    //    "rate_per_duration": {
    //      "0-300": 0,
    //      "300-500": 0,
    //      "500-1000": 0,
    //      "1000-2000": 28.57,
    //      "2000-4000": 14.29,
    //      "4000+": 57.14
    //    }
    //  }
    //]

    // Note that this class is also used when serializing data to the users machine (in XML format)
    public sealed class Analysis
    {
        [JsonProperty("language")]
        public string Language { get; set; }
    }
}
