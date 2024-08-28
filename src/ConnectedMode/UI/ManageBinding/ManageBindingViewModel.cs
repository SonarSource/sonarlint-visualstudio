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

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

public class ManageBindingViewModel(IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter, SolutionInfoModel solutionInfo) : ViewModelBase
{
    private ServerProject boundProject;
    private ConnectionInfo selectedConnectionInfo;
    private ServerProject selectedProject;
    private bool isSharedBindingConfigurationDetected;

    public SolutionInfoModel SolutionInfo { get; } = solutionInfo;
    public ProgressReporterViewModel ProgressReporter { get; } = new();

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

    public ConnectionInfo SelectedConnectionInfo    
    {
        get => selectedConnectionInfo;
        set
        {
            if(value == selectedConnectionInfo)
            {
                return;
            }
            selectedConnectionInfo = value;
            SelectedProject = null;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsConnectionSelected));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
        }
    }

    public ObservableCollection<ConnectionInfo> Connections { get; } = [];

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
    public bool IsConnectionSelected => SelectedConnectionInfo != null;
    public bool IsConnectionSelectionEnabled => !ProgressReporter.IsOperationInProgress && !IsCurrentProjectBound;
    public bool IsBindButtonEnabled => IsProjectSelected && !ProgressReporter.IsOperationInProgress;
    public bool IsSelectProjectButtonEnabled => IsConnectionSelected && !ProgressReporter.IsOperationInProgress && !IsCurrentProjectBound;
    public bool IsUnbindButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsManageConnectionsButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsUseSharedBindingButtonEnabled => !ProgressReporter.IsOperationInProgress && IsSharedBindingConfigurationDetected;
    public bool IsExportButtonEnabled => !ProgressReporter.IsOperationInProgress && IsCurrentProjectBound;

    public void InitializeConnections()
    {
       Connections.Clear();
       var slCoreConnections = serverConnectionsRepositoryAdapter.GetAllConnectionsInfo();
       slCoreConnections.ForEach(conn => Connections.Add(conn));
    }

    public async Task BindAsync()
    {
        try
        {
            UpdateProgress(UiResources.BindingInProgressText);
            // this is only for demo purposes. When it will be replaced with real SlCore binding logic, it can be removed
            await Task.Delay(3000);
            BoundProject = SelectedProject;
        }
        finally
        {
            UpdateProgress(null);
        }
    }

    public void Unbind()
    {
        BoundProject = null;
        SelectedConnectionInfo = null;
        SelectedProject = null;
    }

    public async Task UseSharedBindingAsync()
    {
        // this is only for demo purposes. It should be replaced with real SlCore binding logic
        SelectedConnectionInfo = Connections.FirstOrDefault();
        SelectedProject = new ServerProject("Myproj", "My proj");
        await BindAsync();
    }

    public async Task ExportBindingConfigurationAsync()
    {
        try
        {
            UpdateProgress(UiResources.ExportingBindingConfigurationProgressText);
            // this is only for demo purposes. When it will be replaced with real SlCore binding logic, it can be removed
            await Task.Delay(3000);
        }
        finally
        {
            UpdateProgress(null);
        }
    }

    internal void UpdateProgress(string status)
    {
        ProgressReporter.ProgressStatus = status;
        RaisePropertyChanged(nameof(IsBindButtonEnabled));
        RaisePropertyChanged(nameof(IsUnbindButtonEnabled));
        RaisePropertyChanged(nameof(IsUseSharedBindingButtonEnabled));
        RaisePropertyChanged(nameof(IsManageConnectionsButtonEnabled));
        RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
        RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
        RaisePropertyChanged(nameof(IsExportButtonEnabled));
    }
}
