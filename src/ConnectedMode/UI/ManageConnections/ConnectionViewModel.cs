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

using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;

public class ConnectionViewModel : ViewModelBase
{
    public Connection Connection { get; }

    public string Name => Connection.Info.Id;

    public string ServerType => Connection.Info.ServerType.ToString();

    public bool EnableSmartNotifications
    {
        get => Connection.EnableSmartNotifications;
        set
        {
            Connection.EnableSmartNotifications = value;
            RaisePropertyChanged();
        }
    }

    public ConnectionViewModel(Connection connection)
    {
        Connection = connection;
        EnableSmartNotifications = connection.EnableSmartNotifications;
    }
}
