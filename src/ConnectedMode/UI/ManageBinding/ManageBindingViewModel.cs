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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.Core.WPF;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

public class ManageBindingViewModel(SolutionInfoModel solutionInfo) : ViewModelBase
{
    private ServerProject boundProject;
    private Connection selectedConnection;
    private ServerProject selectedProject;
    private bool isBindingInProgress;

    public SolutionInfoModel SolutionInfo { get; } = solutionInfo;

    public ServerProject BoundProject      
    {
        get => boundProject;
        set
        {
            boundProject = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsCurrentProjectBound));
        }
    }

    public Connection SelectedConnection    
    {
        get => selectedConnection;
        set
        {
            if(value == selectedConnection)
            {
                return;
            }
            selectedConnection = value;
            SelectedProject = null;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsConnectionSelected));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
        }
    }

    public ObservableCollection<Connection> Connections { get; } = [];

    public ServerProject SelectedProject   
    {
        get => selectedProject;
        set
        {
            selectedProject = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsProjectSelected));
            RaisePropertyChanged(nameof(IsBindButtonEnabled));
        }
    }

    public bool IsBindingInProgress
    {
        get => isBindingInProgress;
        set
        {
            isBindingInProgress = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsBindButtonEnabled));
            RaisePropertyChanged(nameof(IsUnbindButtonEnabled));
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonEnabled));
            RaisePropertyChanged(nameof(IsManageConnectionsButtonEnabled));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
        }
    }

    public bool IsCurrentProjectBound => BoundProject != null;
    public bool IsProjectSelected => SelectedProject != null;
    public bool IsConnectionSelected => SelectedConnection != null;
    public bool IsBindButtonEnabled => IsProjectSelected && !IsBindingInProgress;
    public bool IsSelectProjectButtonEnabled => IsConnectionSelected && !IsBindingInProgress;
    public bool IsUnbindButtonEnabled => !IsBindingInProgress;
    public bool IsManageConnectionsButtonEnabled => !IsBindingInProgress;
    public bool IsUseSharedBindingButtonEnabled => !IsBindingInProgress;

    public void InitializeConnections()
    {
       Connections.Clear();
       List<Connection> slCoreConnections =
       [
           new Connection("http://localhost:9000", ServerType.SonarQube, true),
           new Connection("https://sonarcloud.io/myOrg", ServerType.SonarCloud, false)
       ];
       slCoreConnections.ForEach(conn => Connections.Add(conn));
    }

    public async Task BindAsync()
    {
        try
        {
            IsBindingInProgress = true;
            // this is only for demo purposes. When it will be replaced with real SlCore binding logic, it can be removed
            await Task.Delay(3000);
            BoundProject = SelectedProject;
        }
        finally
        {
            IsBindingInProgress = false;
        }
    }

    public void Unbind()
    {
        BoundProject = null;
    }

    public async Task UseSharedBindingAsync()
    {
        // this is only for demo purposes. It should be replaced with real SlCore binding logic
        SelectedConnection = Connections.FirstOrDefault();
        SelectedProject = new ServerProject("Myproj", "My proj");
        await BindAsync();
    }
}
