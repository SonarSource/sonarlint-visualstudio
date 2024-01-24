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

namespace SonarLint.VisualStudio.Integration.Telemetry.Payload
{
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
