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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

public class ProjectSelectionViewModel(
    ConnectionInfo connectionInfo,
    IConnectedModeServices connectedModeServices,
    IProgressReporterViewModel progressReporterViewModel)
    : ViewModelBase
{
    public ObservableCollection<ServerProject> ProjectResults { get; } = [];

    public ConnectionInfo ConnectionInfo { get; } = connectionInfo;
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;

    public bool NoProjectExists => ProjectResults.Count == 0;

    public string ProjectSearchTerm
    {
        get => projectSearchTerm;
        set
        {
            projectSearchTerm = value;
            SearchForProject();
            RaisePropertyChanged();
        }
    }

    public ServerProject SelectedProject
    {
        get => selectedProject;
        set
        {
            selectedProject = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsProjectSelected));
        }
    }

    public bool IsProjectSelected => SelectedProject != null;
    public ServerConnection ServerConnection { get; private set; }

    private string projectSearchTerm;
    private ServerProject selectedProject;

    public async Task InitializeProjectWithProgressAsync()
    {
        var initializeProjectsParams = new TaskToPerformParams<AdapterResponseWithData<List<ServerProject>>>(
            AdapterGetAllProjectsAsync,
            UiResources.LoadingProjectsProgressText,
            UiResources.LoadingProjectsFailedText)
        {
            AfterSuccess = InitProjects
        };
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(initializeProjectsParams);
    }

    internal async Task<AdapterResponseWithData<List<ServerProject>>> AdapterGetAllProjectsAsync()
    {
        var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(ConnectionInfo, out var serverConnection);
        if (!succeeded)
        {
            connectedModeServices.Logger.WriteLine(UiResources.LoadingProjectsFailedTextForNotFoundServerConnection);
            return new AdapterResponseWithData<List<ServerProject>>(false, null);
        }

        ServerConnection = serverConnection;
        return await connectedModeServices.SlCoreConnectionAdapter.GetAllProjectsAsync(serverConnection);
    }

    internal void InitProjects(AdapterResponseWithData<List<ServerProject>> response)
    {
        ProjectResults.Clear();
        foreach (var serverProject in response.ResponseData.OrderBy(p => p.Name))
        {
            ProjectResults.Add(serverProject);
        }

        RaisePropertyChanged(nameof(NoProjectExists));
    }

    private void SearchForProject()
    {
        if (string.IsNullOrEmpty(ProjectSearchTerm))
        {
            return;
        }

        SearchForProjectWithProgressAsync().Forget();
    }

    private async Task SearchForProjectWithProgressAsync()
    {
        var initializeProjectsParams = new TaskToPerformParams<AdapterResponseWithData<List<ServerProject>>>(
            FuzzySearchProjectsAsync,
            UiResources.SearchingProjectInProgressText,
            UiResources.SearchingProjectFailedText)
        {
            AfterSuccess = InitProjects
        };
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(initializeProjectsParams);
    }

    private async Task<AdapterResponseWithData<List<ServerProject>>> FuzzySearchProjectsAsync()
    {
        return await connectedModeServices.SlCoreConnectionAdapter.FuzzySearchProjectsAsync(ServerConnection, ProjectSearchTerm);
    }

}
