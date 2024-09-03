﻿/*
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

using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[ExcludeFromCodeCoverage] // todo remove https://sonarsource.atlassian.net/browse/SLVS-1408 
public static class ConnectionInfoConverter
{
    public static ServerConnection ToServerConnection(this ConnectionInformation connectionInformation) =>
        connectionInformation switch
        {
            { Organization.Key: { } organization } => new ServerConnection.SonarCloud(organization,
                credentials: new BasicAuthCredentials(connectionInformation.UserName, connectionInformation.Password)),
            _ => new ServerConnection.SonarQube(connectionInformation.ServerUri,
                credentials: new BasicAuthCredentials(connectionInformation.UserName, connectionInformation.Password))
        };
}
