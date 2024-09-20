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
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
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
        var serverConnectionCredentials = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(ConnectionInfo.Id, out var serverConnection);
        if (!serverConnectionCredentials)
        {
            connectedModeServices.Logger.WriteLine(UiResources.LoadingProjectsFailedTextForNotFoundServerConnection);
            return new AdapterResponseWithData<List<ServerProject>>(false, null);
        }

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
        
        ProjectResults.Clear();

        ProjectResults.Add(new ServerProject(Key: $"{ProjectSearchTerm.ToLower().Replace(" ", "_")}_1",
            Name: $"{ProjectSearchTerm}_1"));
        ProjectResults.Add(new ServerProject(Key: $"{ProjectSearchTerm.ToLower().Replace(" ", "_")}_2",
            Name: $"{ProjectSearchTerm}_2"));
        ProjectResults.Add(new ServerProject(Key: $"{ProjectSearchTerm.ToLower().Replace(" ", "_")}_3",
            Name: $"{ProjectSearchTerm}_3"));
        RaisePropertyChanged(nameof(NoProjectExists));
    }
}
