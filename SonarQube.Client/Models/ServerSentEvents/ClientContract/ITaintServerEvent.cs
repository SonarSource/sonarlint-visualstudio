/*
 * SonarQube Client
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

using System;

namespace SonarQube.Client.Models.ServerSentEvents.ClientContract
{
    public interface ITaintServerEvent : IServerEvent
    {
        string ProjectKey { get; }
        string Key { get; }
    }

    /// <summary>
    /// Represents TaintVulnerabilityRaised server event information
    /// </summary>
    public interface ITaintVulnerabilityRaisedServerEvent : ITaintServerEvent
    {
        string Branch { get; }
    }

    public class TaintVulnerabilityRaisedServerEvent : ITaintVulnerabilityRaisedServerEvent
    {
        public TaintVulnerabilityRaisedServerEvent(string projectKey, string key, string branch)
        {
            ProjectKey = projectKey ?? throw new ArgumentNullException(nameof(projectKey));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        }

        public string ProjectKey { get; }
        public string Key { get; }
        public string Branch { get; }
    }

    /// <summary>
    /// Represents TaintVulnerabilityClosed server event information
    /// </summary>
    public interface ITaintVulnerabilityClosedServerEvent : ITaintServerEvent
    {
    }

    public class TaintVulnerabilityClosedServerEvent : ITaintVulnerabilityClosedServerEvent
    {
        public TaintVulnerabilityClosedServerEvent(string projectKey, string key)
        {
            ProjectKey = projectKey ?? throw new ArgumentNullException(nameof(projectKey));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string ProjectKey { get; }
        public string Key { get; }
    }
}
