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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ConnectionDisplay;

public class ConnectionNameViewModel : ViewModelBase
{
    public ConnectionNameViewModel() : this(DogfoodingService.Instance){ }

    internal /* for testing */ ConnectionNameViewModel(IDogfoodingService dogfoodingService)
    {
        this.dogfoodingService = dogfoodingService ?? throw new ArgumentNullException(nameof(dogfoodingService));
    }

    private ConnectionInfo connectionInfo;
    private readonly IDogfoodingService dogfoodingService;

    public ConnectionInfo ConnectionInfo
    {
        get => connectionInfo;
        set
        {
            connectionInfo = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(ShouldDisplayRegion));
            RaisePropertyChanged(nameof(DisplayRegion));
        }
    }

    public string DisplayName =>
        connectionInfo is { Id: null, ServerType: ConnectionServerType.SonarCloud }
            ? connectionInfo.CloudServerRegion.Url.ToString()
            : connectionInfo?.Id ?? string.Empty;

    public bool ShouldDisplayRegion => dogfoodingService.IsDogfoodingEnvironment && connectionInfo is { Id: not null, ServerType: ConnectionServerType.SonarCloud };
    public string DisplayRegion => ShouldDisplayRegion ? connectionInfo.CloudServerRegion.Name : string.Empty;
}
