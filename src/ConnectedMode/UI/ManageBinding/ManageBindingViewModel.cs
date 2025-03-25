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
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

internal sealed partial class ManageBindingViewModel(
    IConnectedModeServices connectedModeServices,
    IConnectedModeBindingServices connectedModeBindingServices,
    IConnectedModeUIServices connectedModeUiServices,
    IConnectedModeUIManager connectedModeUiManager,
    IProgressReporterViewModel progressReporterViewModel)
    : ViewModelBase, IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private ServerProject boundProject;
    private ConnectionInfo selectedConnectionInfo;
    private ServerProject selectedProject;
    private SharedBindingConfigModel sharedBindingConfigModel;
    private SolutionInfoModel solutionInfo;
    private bool bindingSucceeded;

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

    public IProgressReporterViewModel ProgressReporter { get; } = progressReporterViewModel;

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

    public bool BindingSucceeded
    {
        get => bindingSucceeded;
        set
        {
            bindingSucceeded = value;
            RaisePropertyChanged();
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

    public void Dispose() => cancellationTokenSource?.Dispose();

    public async Task InitializeDataAsync()
    {
        var loadData = new TaskToPerformParams<ResponseStatus>(LoadDataAsync, UiResources.LoadingConnectionsText,
            UiResources.LoadingConnectionsFailedText) { AfterProgressUpdated = OnProgressUpdated };
        var loadDataResult = await ProgressReporter.ExecuteTaskWithProgressAsync(loadData);

        var displayBindStatus = new TaskToPerformParams<ResponseStatus<BindingResult>>(DisplayBindStatusAsync, UiResources.FetchingBindingStatusText,
            UiResources.FetchingBindingStatusFailedText) { AfterProgressUpdated = OnProgressUpdated };
        var displayBindStatusResult = await ProgressReporter.ExecuteTaskWithProgressAsync(displayBindStatus, clearPreviousState: false);

        BindingSucceeded = loadDataResult.Success && displayBindStatusResult.Success;
        await UpdateSharedBindingStateAsync();
    }

    private async Task UpdateSharedBindingStateAsync()
    {
        var detectSharedBinding = new TaskToPerformParams<ResponseStatus>(CheckForSharedBindingAsync, UiResources.CheckingForSharedBindingText,
            UiResources.CheckingForSharedBindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(detectSharedBinding, clearPreviousState: false);
    }

    public async Task PerformBindingWithProgressAsync(BindingRequest binding)
    {
        var bind = new TaskToPerformParams<ResponseStatus<BindingResult>>(() => PerformBindingInternalAsync(binding), UiResources.BindingInProgressText,
            UiResources.BindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(bind);
    }

    internal async Task<ResponseStatus<BindingResult>> PerformBindingInternalAsync(BindingRequest binding)
    {
        var bindingResult = await ValidateAndBindAsync(binding);
        UpdateBindingTelemetry(binding, bindingResult);
        return new ResponseStatus<BindingResult>(bindingResult.IsSuccessful, bindingResult, bindingResult.ProblemDescription);
    }

    public async Task UnbindWithProgressAsync()
    {
        var unbind = new TaskToPerformParams<ResponseStatus>(UnbindAsync, UiResources.UnbindingInProgressText, UiResources.UnbindingFailedText) { AfterProgressUpdated = OnProgressUpdated };
        await ProgressReporter.ExecuteTaskWithProgressAsync(unbind);
    }

    public async Task ExportBindingConfigurationWithProgressAsync()
    {
        var export = new TaskToPerformParams<ResponseStatus<string>>(ExportBindingConfigurationAsync, UiResources.ExportingBindingConfigurationProgressText,
            UiResources.ExportBindingConfigurationWarningText) { AfterProgressUpdated = OnProgressUpdated };

        var result = await ProgressReporter.ExecuteTaskWithProgressAsync(export);
        if (result.Success)
        {
            connectedModeUiServices.MessageBox.Show(string.Format(UiResources.ExportBindingConfigurationMessageBoxTextSuccess, result.ResponseData),
                UiResources.ExportBindingConfigurationMessageBoxCaptionSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
            await UpdateSharedBindingStateAsync();
        }
    }

    internal Task<ResponseStatus<string>> ExportBindingConfigurationAsync()
    {
        var connection = SelectedConnectionInfo.GetServerConnectionFromConnectionInfo();
        var sharedBindingConfig = new SharedBindingConfigModel
        {
            ProjectKey = selectedProject.Key,
            Uri = connection.ServerUri,
            Organization = (connection as ServerConnection.SonarCloud)?.OrganizationKey,
            Region = (connection as ServerConnection.SonarCloud)?.Region.Name,
        };

        var savePath = connectedModeBindingServices.SharedBindingConfigProvider.SaveSharedBinding(sharedBindingConfig);

        return Task.FromResult(new ResponseStatus<string>(savePath != null, savePath));
    }

    internal Task<ResponseStatus> CheckForSharedBindingAsync()
    {
        SharedBindingConfigModel = connectedModeBindingServices.SharedBindingConfigProvider.GetSharedBinding();
        return Task.FromResult(new ResponseStatus(true));
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

    internal async Task<ResponseStatus> LoadDataAsync()
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

        return new ResponseStatus(succeeded);
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

    internal async Task<ResponseStatus<BindingResult>> DisplayBindStatusAsync()
    {
        SolutionInfo = await GetSolutionInfoModelAsync();

        var bindingConfiguration = connectedModeServices.ConfigurationProvider.GetConfiguration();
        if (bindingConfiguration == null || bindingConfiguration.Mode == SonarLintMode.Standalone)
        {
            var successResponse = new ResponseStatus<BindingResult>(true, BindingResult.Success);
            UpdateBoundProjectProperties(null, null);
            return successResponse;
        }

        var boundServerProject = bindingConfiguration.Project;
        var serverConnection = boundServerProject?.ServerConnection;
        if (serverConnection == null)
        {
            return new ResponseStatus<BindingResult>(false, BindingResult.ConnectionNotFound);
        }

        var response = await connectedModeServices.SlCoreConnectionAdapter.GetServerProjectByKeyAsync(serverConnection, boundServerProject.ServerProjectKey);
        // even if the response is not successful, we still want to update the UI with the bound project, because the binding does exist
        var selectedServerProject = response.ResponseData ?? new ServerProject(boundServerProject.ServerProjectKey, boundServerProject.ServerProjectKey);
        UpdateBoundProjectProperties(serverConnection, selectedServerProject);
        var projectRetrieved = response.ResponseData != null;

        return new ResponseStatus<BindingResult>(projectRetrieved, projectRetrieved ? BindingResult.Success : BindingResult.Failed);
    }

    internal async Task<ResponseStatus> UnbindAsync()
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

        return new ResponseStatus(succeeded);
    }

    private void UpdateBoundProjectProperties(ServerConnection serverConnection, ServerProject selectedServerProject)
    {
        SelectedConnectionInfo = serverConnection == null ? null : ConnectionInfo.From(serverConnection);
        SelectedProject = selectedServerProject;
        BoundProject = SelectedProject;
    }

    private async Task<SolutionInfoModel> GetSolutionInfoModelAsync()
    {
        var solutionName = await connectedModeBindingServices.SolutionInfoProvider.GetSolutionNameAsync();
        var isFolderWorkspace = await connectedModeBindingServices.SolutionInfoProvider.IsFolderWorkspaceAsync();
        return new SolutionInfoModel(solutionName, isFolderWorkspace ? SolutionType.Folder : SolutionType.Solution);
    }
}
