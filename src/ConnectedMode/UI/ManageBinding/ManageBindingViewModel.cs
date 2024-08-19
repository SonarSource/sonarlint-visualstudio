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
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

public class ManageBindingViewModel(SolutionInfoModel solutionInfo) : ViewModelBase
{
    private ServerProject boundProject;
    private Connection selectedConnection;
    private ServerProject selectedProject;
    private string progressStatus;
    private bool isSharedBindingConfigurationDetected;

    public SolutionInfoModel SolutionInfo { get; } = solutionInfo;

    public ServerProject BoundProject      
    {
        get => boundProject;
        set
        {
            boundProject = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsCurrentProjectBound));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
            RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
            RaisePropertyChanged(nameof(IsExportButtonEnabled));
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

    public string ProgressStatus    
    {
        get => progressStatus;
        set
        {
            progressStatus = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsOperationInProgress));
            RaisePropertyChanged(nameof(IsBindButtonEnabled));
            RaisePropertyChanged(nameof(IsUnbindButtonEnabled));
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonEnabled));
            RaisePropertyChanged(nameof(IsManageConnectionsButtonEnabled));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
            RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
            RaisePropertyChanged(nameof(IsExportButtonEnabled));
        }
    }

    public bool IsSharedBindingConfigurationDetected    
    {
        get => isSharedBindingConfigurationDetected;
        set
        {
            isSharedBindingConfigurationDetected = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonEnabled));
        }
    }

    public bool IsCurrentProjectBound => BoundProject != null;
    public bool IsProjectSelected => SelectedProject != null;
    public bool IsConnectionSelected => SelectedConnection != null;
    public bool IsConnectionSelectionEnabled => !IsOperationInProgress && !IsCurrentProjectBound;
    public bool IsOperationInProgress => !string.IsNullOrEmpty(ProgressStatus);
    public bool IsBindButtonEnabled => IsProjectSelected && !IsOperationInProgress;
    public bool IsSelectProjectButtonEnabled => IsConnectionSelected && !IsOperationInProgress && !IsCurrentProjectBound;
    public bool IsUnbindButtonEnabled => !IsOperationInProgress;
    public bool IsManageConnectionsButtonEnabled => !IsOperationInProgress;
    public bool IsUseSharedBindingButtonEnabled => !IsOperationInProgress && IsSharedBindingConfigurationDetected;
    public bool IsExportButtonEnabled => !IsOperationInProgress && IsCurrentProjectBound;

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
            ProgressStatus = UiResources.BindingInProgressText;
            // this is only for demo purposes. When it will be replaced with real SlCore binding logic, it can be removed
            await Task.Delay(3000);
            BoundProject = SelectedProject;
        }
        finally
        {
            ProgressStatus = null;
        }
    }

    public void Unbind()
    {
        BoundProject = null;
        SelectedConnection = null;
        SelectedProject = null;
    }

    public async Task UseSharedBindingAsync()
    {
        // this is only for demo purposes. It should be replaced with real SlCore binding logic
        SelectedConnection = Connections.FirstOrDefault();
        SelectedProject = new ServerProject("Myproj", "My proj");
        await BindAsync();
    }

    public async Task ExportBindingConfigurationAsync()
    {
        try
        {
            ProgressStatus = UiResources.ExportingBindingConfigurationProgressText;
            // this is only for demo purposes. When it will be replaced with real SlCore binding logic, it can be removed
            await Task.Delay(3000);
        }
        finally
        {
            ProgressStatus = null;
        }
    }
}
