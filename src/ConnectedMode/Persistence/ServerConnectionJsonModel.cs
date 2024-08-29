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

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

public class ServerConnectionsListJsonModel
{
    [JsonProperty("serverConnections")]
    public List<ServerConnectionJsonModel> ServerConnections { get; set; } = new();
}

public record ServerConnectionJsonModel
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("setting")]
    public ServerConnectionSettings Settings { get; set; }

    [JsonProperty("organizationKey")]
    public string OrganizationKey { get; set; }

    [JsonProperty("serverUri")]
    public string ServerUri { get; set; }
}
