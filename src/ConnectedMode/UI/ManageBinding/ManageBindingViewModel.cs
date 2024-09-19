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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

public sealed class ManageBindingViewModel : ViewModelBase, IDisposable
{
    private SolutionInfoModel solutionInfo;
    private ServerProject boundProject;
    private ConnectionInfo selectedConnectionInfo;
    private ServerProject selectedProject;
    private bool isSharedBindingConfigurationDetected;
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IBindingController bindingController;
    private readonly ISolutionInfoProvider solutionInfoProvider;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public SolutionInfoModel SolutionInfo
    {
        get => solutionInfo;
        private set
        {
            solutionInfo = value;
            RaisePropertyChanged();
        }
    }
    public IProgressReporterViewModel ProgressReporter { get; }

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
    public bool IsConnectionSelectionEnabled => !ProgressReporter.IsOperationInProgress && !IsCurrentProjectBound && Connections.Any();
    public bool IsBindButtonEnabled => IsProjectSelected && !ProgressReporter.IsOperationInProgress;
    public bool IsSelectProjectButtonEnabled => IsConnectionSelected && !ProgressReporter.IsOperationInProgress && !IsCurrentProjectBound;
    public bool IsUnbindButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsManageConnectionsButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsUseSharedBindingButtonEnabled => !ProgressReporter.IsOperationInProgress && IsSharedBindingConfigurationDetected;
    public bool IsExportButtonEnabled => !ProgressReporter.IsOperationInProgress && IsCurrentProjectBound;
    public string ConnectionSelectionCaptionText => Connections.Any() ? UiResources.SelectConnectionToBindDescription : UiResources.NoConnectionExistsLabel;

     public ManageBindingViewModel(
         IConnectedModeServices connectedModeServices,
         IBindingController bindingController,
         ISolutionInfoProvider solutionInfoProvider,
         IProgressReporterViewModel progressReporterViewModel)
    {
        this.connectedModeServices = connectedModeServices;
        this.bindingController = bindingController;
        this.solutionInfoProvider = solutionInfoProvider;
        ProgressReporter = progressReporterViewModel;
    }
    
    public async Task InitializeDataAsync()
    {
        var loadData = new TaskToPerformParams<AdapterResponse>(LoadDataAsync, UiResources.LoadingConnectionsText, UiResources.LoadingConnectionsFailedText){AfterProgressUpdated = OnProgressUpdated};
        await ProgressReporter.ExecuteTaskWithProgressAsync(loadData);
        var displayBindStatus = new TaskToPerformParams<AdapterResponse>(DisplayBindStatusAsync, UiResources.FetchingBindingStatusText, UiResources.FetchingBindingStatusFailedText){AfterProgressUpdated = OnProgressUpdated};
        await ProgressReporter.ExecuteTaskWithProgressAsync(displayBindStatus);
    }

    public async Task BindWithProgressAsync()
    {
        var bind = new TaskToPerformParams<AdapterResponse>(BindAsync, UiResources.BindingInProgressText, UiResources.BindingFailedText){AfterProgressUpdated = OnProgressUpdated};
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
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
        await BindWithProgressAsync();
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
        OnProgressUpdated();
    }

    internal void OnProgressUpdated()
    {
        RaisePropertyChanged(nameof(IsBindButtonEnabled));
        RaisePropertyChanged(nameof(IsUnbindButtonEnabled));
        RaisePropertyChanged(nameof(IsUseSharedBindingButtonEnabled));
        RaisePropertyChanged(nameof(IsManageConnectionsButtonEnabled));
        RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
        RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
        RaisePropertyChanged(nameof(IsExportButtonEnabled));
    }

    internal async Task<AdapterResponse> LoadDataAsync()
    {
        var succeeded = false;
        try
        {
            await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => succeeded = LoadConnections());
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine(ex.Message);
            succeeded = false;
        }

        return new AdapterResponse(succeeded);
    }

    internal bool LoadConnections()
    {
        Connections.Clear();
        var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnectionsInfo(out var slCoreConnections);
        slCoreConnections?.ForEach(Connections.Add);

        RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
        RaisePropertyChanged(nameof(ConnectionSelectionCaptionText));
        return succeeded;
    }
    
    internal /* for testing */ async Task<AdapterResponse> DisplayBindStatusAsync()
    {
        var solutionName = await solutionInfoProvider.GetSolutionNameAsync();
        var isFolderWorkspace = await solutionInfoProvider.IsFolderWorkspaceAsync();
        SolutionInfo = new SolutionInfoModel(solutionName, isFolderWorkspace ? SolutionType.Folder : SolutionType.Solution);

        var bindingConfiguration = connectedModeServices.ConfigurationProvider.GetConfiguration();
        if (bindingConfiguration == null || bindingConfiguration.Mode == SonarLintMode.Standalone)
        {
            return new AdapterResponse(true);
        }
        
        var boundServerProject = connectedModeServices.ConfigurationProvider.GetConfiguration()?.Project;
        var serverConnection = boundServerProject?.ServerConnection;
        if (serverConnection == null)
        {
            return new AdapterResponse(false);
        }
        SelectedConnectionInfo = ConnectionInfo.From(serverConnection);

        var response = await connectedModeServices.SlCoreConnectionAdapter.GetServerProjectByKeyAsync(serverConnection.Credentials, SelectedConnectionInfo, boundServerProject.ServerProjectKey);
        SelectedProject = response.ResponseData;
        BoundProject = SelectedProject;
        return new AdapterResponse(BoundProject != null);
    }

    internal /* for testing */ async Task<AdapterResponse> BindAsync()
    {
        if (!connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetServerConnectionById(SelectedConnectionInfo?.Id, out var serverConnection))
        {
            return new AdapterResponse(false);
        }
        
        try
        {
            var localBindingKey = await solutionInfoProvider.GetSolutionNameAsync();
            var serverBindingKey = SelectedProject.Key;
            var boundServerProject = new BoundServerProject(localBindingKey, serverBindingKey, serverConnection);
            await bindingController.BindAsync(boundServerProject, cancellationTokenSource.Token);
            return await DisplayBindStatusAsync();
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine($"{SonarLint.VisualStudio.ConnectedMode.Resources.Binding_Fails}", ex.Message);
            return new AdapterResponse(false);
        }
    }

    public void Dispose()
    {
        cancellationTokenSource?.Dispose();
    }
}
