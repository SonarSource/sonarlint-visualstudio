/*
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

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

internal sealed class ManageBindingViewModel : ViewModelBase, IDisposable
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
        set
        {
            solutionInfo = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSolutionOpen));
            RaisePropertyChanged(nameof(IsOpenSolutionBound));
            RaisePropertyChanged(nameof(IsOpenSolutionStandalone));
            RaisePropertyChanged(nameof(IsSelectProjectButtonEnabled));
            RaisePropertyChanged(nameof(IsConnectionSelectionEnabled));
            RaisePropertyChanged(nameof(IsExportButtonEnabled));
            RaisePropertyChanged(nameof(IsUseSharedBindingButtonVisible));
        }
    }

    public IProgressReporterViewModel ProgressReporter { get; }

    public ServerProject BoundProject
    {
        get => boundProject;
        set
        {
            if (IsSolutionOpen)
            {
                boundProject = value;
            }
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsOpenSolutionBound));
            RaisePropertyChanged(nameof(IsOpenSolutionStandalone));
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

    public bool IsSolutionOpen => SolutionInfo is { Name: not null };
    public bool IsOpenSolutionBound => IsSolutionOpen && BoundProject is not null;
    public bool IsOpenSolutionStandalone => IsSolutionOpen && BoundProject is null;
    public bool IsProjectSelected => SelectedProject != null;
    public bool IsConnectionSelected => SelectedConnectionInfo != null;
    public bool IsConnectionSelectionEnabled => !ProgressReporter.IsOperationInProgress && IsOpenSolutionStandalone && Connections.Any();
    public bool IsBindButtonEnabled => IsProjectSelected && !ProgressReporter.IsOperationInProgress;
    public bool IsSelectProjectButtonEnabled => IsConnectionSelected && !ProgressReporter.IsOperationInProgress && IsOpenSolutionStandalone;
    public bool IsUnbindButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsManageConnectionsButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsUseSharedBindingButtonEnabled => !ProgressReporter.IsOperationInProgress;
    public bool IsUseSharedBindingButtonVisible => SharedBindingConfigModel != null && IsOpenSolutionStandalone;
    public bool IsExportButtonEnabled => !ProgressReporter.IsOperationInProgress && IsOpenSolutionBound;
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
            UiResources.CheckingForSharedBindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(detectSharedBinding);
    }

    public async Task PerformManualBindingWithProgressAsync()
    {
        var bind = new TaskToPerformParams<AdapterResponse>(PerformManualBindingAsync, UiResources.BindingInProgressText, UiResources.BindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
    }

    public async Task PerformAutomaticBindingWithProgressAsync(AutomaticBindingRequest automaticBinding)
    {
        var bind = new TaskToPerformParams<AdapterResponse>(() => PerformAutomaticBindingInternalAsync(automaticBinding), UiResources.BindingInProgressText, UiResources.BindingFailedText)
        {
            AfterProgressUpdated = OnProgressUpdated
        };
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
    }

    public async Task UnbindWithProgressAsync()
    {
        var unbind = new TaskToPerformParams<AdapterResponse>(UnbindAsync, UiResources.UnbindingInProgressText, UiResources.UnbindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(unbind);
    }

    [ExcludeFromCodeCoverage]
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

    internal /* for testing */ async Task<AdapterResponse> PerformManualBindingAsync()
    {
        if (!connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(SelectedConnectionInfo, out var serverConnection))
        {
            return new AdapterResponse(false);
        }
        var adapterResponse = await BindAsync(serverConnection, SelectedProject?.Key);
        if (adapterResponse.Success)
        {
            connectedModeServices.TelemetryManager.AddedManualBindings();
        }
        return adapterResponse;
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
            connectedModeServices.Logger.WriteLine(ConnectedMode.Resources.Binding_Fails, ex.Message);
            return new AdapterResponse(false);
        }
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

    internal async Task<AdapterResponse> PerformAutomaticBindingInternalAsync(AutomaticBindingRequest automaticBinding)
    {
        var logContext = new MessageLevelContext { Context = [ConnectedMode.Resources.ConnectedModeAutomaticBindingLogContext, automaticBinding.TypeName], VerboseContext = [automaticBinding.ToString()]};

        if (!SelectAutomaticBindingArguments(logContext, automaticBinding, out var serverConnectionId, out var serverProjectKey)
            || !AutomaticBindingConnectionExists(logContext, serverConnectionId, out var serverConnection)
            || !AutomaticBindingCredentialsExists(logContext, serverConnection))
        {
            return new AdapterResponse(false);
        }

        var response = await BindAsync(serverConnection, serverProjectKey);
        Telemetry(response, automaticBinding);
        return response;
    }

    private void Telemetry(AdapterResponse response, AutomaticBindingRequest automaticBinding)
    {
        if (!response.Success)
        {
            return;
        }

        switch (automaticBinding)
        {
            case AutomaticBindingRequest.Assisted { IsFromSharedBinding: true } or AutomaticBindingRequest.Shared:
                connectedModeServices.TelemetryManager.AddedFromSharedBindings();
                break;
            case AutomaticBindingRequest.Assisted:
                connectedModeServices.TelemetryManager.AddedAutomaticBindings();
                break;
        }
    }

    private bool SelectAutomaticBindingArguments(MessageLevelContext logContext, AutomaticBindingRequest automaticBinding, out string serverConnectionId, out string serverProjectKey)
    {
        Debug.Assert(automaticBinding is not AutomaticBindingRequest.Shared || SharedBindingConfigModel != null,
            "Shared binding should never be called when it's not available");

        switch (automaticBinding)
        {
            case AutomaticBindingRequest.Assisted assistedBinding:
                serverConnectionId = assistedBinding.ServerConnectionId;
                serverProjectKey = assistedBinding.ServerProjectKey;
                return true;
            case AutomaticBindingRequest.Shared when SharedBindingConfigModel != null:
                serverConnectionId = CreateConnectionInfoFromSharedBinding().GetServerIdFromConnectionInfo();
                serverProjectKey = SharedBindingConfigModel.ProjectKey;
                return true;
            default:
                connectedModeServices.Logger.WriteLine(logContext, ConnectedMode.Resources.AutomaticBinding_ConfigurationNotAvailable);
                serverConnectionId = null;
                serverProjectKey = null;
                return false;
        }
    }

    private bool AutomaticBindingConnectionExists(MessageLevelContext logContext, string connectionId, out ServerConnection serverConnection)
    {
        if (connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(connectionId, out serverConnection))
        {
            return true;
        }

        connectedModeServices.Logger.WriteLine(
            logContext,
            ConnectedMode.Resources.AutomaticBinding_ConnectionNotFound,
            connectionId);
        connectedModeServices.MessageBox.Show(
            UiResources.NotFoundConnectionForAutomaticBindingMessageBoxText,
            UiResources.NotFoundConnectionForAutomaticBindingMessageBoxCaption,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private bool AutomaticBindingCredentialsExists(MessageLevelContext logContext, ServerConnection serverConnection)
    {
        if (serverConnection.Credentials != null)
        {
            return true;
        }

        connectedModeServices.Logger.WriteLine(
            logContext,
            ConnectedMode.Resources.AutomaticBinding_CredentiasNotFound,
            serverConnection.Id);
        connectedModeServices.MessageBox.Show(
            UiResources.NotFoundCredentialsForAutomaticBindingMessageBoxText,
            UiResources.NotFoundCredentialsForAutomaticBindingMessageBoxCaption,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }
}
