﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

public interface IConnectedModeServices
{
    public IThreadHandling ThreadHandling { get; }
    public ILogger Logger { get; }
    public ISlCoreConnectionAdapter SlCoreConnectionAdapter { get; }
    public IConfigurationProvider ConfigurationProvider { get; }
    public IServerConnectionsRepositoryAdapter ServerConnectionsRepositoryAdapter { get; }
    public ITelemetryManager TelemetryManager { get; }
}

[Export(typeof(IConnectedModeServices))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class ConnectedModeServices(
    IThreadHandling threadHandling,
    ISlCoreConnectionAdapter slCoreConnectionAdapter,
    IConfigurationProvider configurationProvider,
    IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter,
    ILogger logger,
    ITelemetryManager telemetryManager)
    : IConnectedModeServices
{
    public IServerConnectionsRepositoryAdapter ServerConnectionsRepositoryAdapter { get; } = serverConnectionsRepositoryAdapter;
    public ITelemetryManager TelemetryManager { get; } = telemetryManager;
    public IThreadHandling ThreadHandling { get; } = threadHandling;
    public ILogger Logger { get; } = logger.ForContext(ConnectedMode.Resources.ConnectedModeLogContext);
    public ISlCoreConnectionAdapter SlCoreConnectionAdapter { get; } = slCoreConnectionAdapter;
    public IConfigurationProvider ConfigurationProvider { get; } = configurationProvider;
}
