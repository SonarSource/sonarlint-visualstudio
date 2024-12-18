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
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

public sealed class ManageBindingViewModel : ViewModelBase, IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly IConnectedModeBindingServices connectedModeBindingServices;
    private readonly IConnectedModeServices connectedModeServices;
    private ServerProject boundProject;
    private ConnectionInfo selectedConnectionInfo;
    private ServerProject selectedProject;
    private SharedBindingConfigModel sharedBindingConfigModel;
    private SolutionInfoModel solutionInfo;

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
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonVisible));
        }
    }

    public ConnectionInfo SelectedConnectionInfo
    {
        get => selectedConnectionInfo;
        set
        {
            if (value == selectedConnectionInfo)
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

    internal SharedBindingConfigModel SharedBindingConfigModel
    {
        get => sharedBindingConfigModel;
        set
        {
            sharedBindingConfigModel = value;
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonVisible));
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
    public bool IsUseSharedBindingButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsUseSharedBindingButtonVisible => SharedBindingConfigModel != null && !IsCurrentProjectBound;
    public bool IsExportButtonEnabled => !ProgressReporter.IsOperationInProgress && IsCurrentProjectBound;
    public string ConnectionSelectionCaptionText => Connections.Any() ? UiResources.SelectConnectionToBindDescription : UiResources.NoConnectionExistsLabel;

    public ManageBindingViewModel(
        IConnectedModeServices connectedModeServices,
        IConnectedModeBindingServices connectedModeBindingServices,
        IProgressReporterViewModel progressReporterViewModel)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeBindingServices = connectedModeBindingServices;
        ProgressReporter = progressReporterViewModel;
    }

    public void Dispose() => cancellationTokenSource?.Dispose();

    public async Task InitializeDataAsync()
    {
        var loadData = new TaskToPerformParams<AdapterResponse>(LoadDataAsync, UiResources.LoadingConnectionsText,
            UiResources.LoadingConnectionsFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(loadData);

        var displayBindStatus = new TaskToPerformParams<AdapterResponse>(DisplayBindStatusAsync, UiResources.FetchingBindingStatusText,
            UiResources.FetchingBindingStatusFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(displayBindStatus);

        var detectSharedBinding = new TaskToPerformParams<AdapterResponse>(CheckForSharedBindingAsync, UiResources.CheckingForSharedBindingText,
                UiResources.CheckingForSharedBindingFailedText)
            { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(detectSharedBinding);
    }

    public async Task BindWithProgressAsync()
    {
        var bind = new TaskToPerformParams<AdapterResponse>(BindAsync, UiResources.BindingInProgressText, UiResources.BindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
    }

    public async Task UseSharedBindingWithProgressAsync()
    {
        var bind = new TaskToPerformParams<AdapterResponse>(UseSharedBindingAsync, UiResources.BindingInProgressText, UiResources.BindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
    }

    public async Task UnbindWithProgressAsync()
    {
        var unbind = new TaskToPerformParams<AdapterResponse>(UnbindAsync, UiResources.UnbindingInProgressText, UiResources.UnbindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(unbind);
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

    internal Task<AdapterResponse> CheckForSharedBindingAsync()
    {
        SharedBindingConfigModel = connectedModeBindingServices.SharedBindingConfigProvider.GetSharedBinding();
        return Task.FromResult(new AdapterResponse(true));
    }

    internal async Task<AdapterResponse> UseSharedBindingAsync()
    {
        var connectionInfo = CreateConnectionInfoFromSharedBinding();
        if (!ConnectionExists(connectionInfo, out var serverConnection) || !CredentialsExists(connectionInfo, serverConnection))
        {
            return new AdapterResponse(false);
        }

        var response = await BindAsync(serverConnection, SharedBindingConfigModel.ProjectKey);
        return new AdapterResponse(response.Success);
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
        SolutionInfo = await GetSolutionInfoModelAsync();

        var bindingConfiguration = connectedModeServices.ConfigurationProvider.GetConfiguration();
        if (bindingConfiguration == null || bindingConfiguration.Mode == SonarLintMode.Standalone)
        {
            var successResponse = new AdapterResponse(true);
            UpdateBoundProjectProperties(null, null);
            return successResponse;
        }

        var boundServerProject = bindingConfiguration.Project;
        var serverConnection = boundServerProject?.ServerConnection;
        if (serverConnection == null)
        {
            return new AdapterResponse(false);
        }

        var response = await connectedModeServices.SlCoreConnectionAdapter.GetServerProjectByKeyAsync(serverConnection, boundServerProject.ServerProjectKey);
        UpdateBoundProjectProperties(serverConnection, response.ResponseData);

        return new AdapterResponse(BoundProject != null);
    }

    internal /* for testing */ async Task<AdapterResponse> BindAsync()
    {
        if (!connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(SelectedConnectionInfo, out var serverConnection))
        {
            return new AdapterResponse(false);
        }
        return await BindAsync(serverConnection, SelectedProject?.Key);
    }

    internal async Task<AdapterResponse> UnbindAsync()
    {
        bool succeeded;
        try
        {
            succeeded = connectedModeBindingServices.BindingController.Unbind(SolutionInfo.Name);
            await DisplayBindStatusAsync();
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine(ex.Message);
            succeeded = false;
        }

        return new AdapterResponse(succeeded);
    }

    private async Task<AdapterResponse> BindAsync(ServerConnection serverConnection, string serverProjectKey)
    {
        try
        {
            var localBindingKey = await connectedModeBindingServices.SolutionInfoProvider.GetSolutionNameAsync();
            var boundServerProject = new BoundServerProject(localBindingKey, serverProjectKey, serverConnection);
            await connectedModeBindingServices.BindingController.BindAsync(boundServerProject, cancellationTokenSource.Token);
            return await DisplayBindStatusAsync();
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine($"{ConnectedMode.Resources.Binding_Fails}", ex.Message);
            return new AdapterResponse(false);
        }
    }

    private bool ConnectionExists(ConnectionInfo connectionInfo, out ServerConnection serverConnection)
    {
        if (connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(connectionInfo, out serverConnection))
        {
            return true;
        }

        connectedModeServices.Logger.WriteLine(ConnectedMode.Resources.UseSharedBinding_ConnectionNotFound, connectionInfo.Id);
        connectedModeServices.MessageBox.Show(UiResources.NotFoundConnectionForSharedBindingMessageBoxText, UiResources.NotFoundConnectionForSharedBindingMessageBoxCaption, MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private bool CredentialsExists(ConnectionInfo connectionInfo, ServerConnection serverConnection)
    {
        if (serverConnection.Credentials != null)
        {
            return true;
        }
        connectedModeServices.Logger.WriteLine(ConnectedMode.Resources.UseSharedBinding_CredentiasNotFound, connectionInfo.Id);
        connectedModeServices.MessageBox.Show(UiResources.NotFoundCredentialsForSharedBindingMessageBoxText, UiResources.NotFoundCredentialsForSharedBindingMessageBoxCaption, MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private ConnectionInfo CreateConnectionInfoFromSharedBinding() =>
        SharedBindingConfigModel.IsSonarCloud()
            ? new ConnectionInfo(SharedBindingConfigModel.Organization, ConnectionServerType.SonarCloud)
            : new ConnectionInfo(SharedBindingConfigModel.Uri.ToString(), ConnectionServerType.SonarQube);

    private void UpdateBoundProjectProperties(ServerConnection serverConnection, ServerProject serverProject)
    {
        SelectedConnectionInfo = serverConnection == null ? null : ConnectionInfo.From(serverConnection);
        SelectedProject = serverProject;
        BoundProject = SelectedProject;
    }

    private async Task<SolutionInfoModel> GetSolutionInfoModelAsync()
    {
        var solutionName = await connectedModeBindingServices.SolutionInfoProvider.GetSolutionNameAsync();
        var isFolderWorkspace = await connectedModeBindingServices.SolutionInfoProvider.IsFolderWorkspaceAsync();
        return new SolutionInfoModel(solutionName, isFolderWorkspace ? SolutionType.Folder : SolutionType.Solution);
    }
}
