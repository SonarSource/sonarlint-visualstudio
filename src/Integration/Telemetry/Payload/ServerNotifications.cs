/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
    public sealed class ServerNotifications
    {
        [JsonProperty("disabled")]
        public bool IsDisabled { get; set; }

        [JsonProperty("count_by_type")]
        public ServerNotificationCounters ServerNotificationCounters { get; set; }
    }

    public sealed class ServerNotificationCounters
    {
        [JsonProperty("QUALITY_GATE")]
        public ServerNotificationCounter QualityGateNotificationCounter { get; set; }

        [JsonProperty("NEW_ISSUES")]
        public ServerNotificationCounter NewIssuesNotificationCounter { get; set; }
    }

    public sealed class ServerNotificationCounter
    {
        [JsonProperty("received")]
        public int ReceivedCount { get; set; }

        [JsonProperty("clicked")]
        public int ClickedCount { get; set; }
    }
}
