/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Automation;

/// <summary>
/// Creates a SonarQube Server connection and binds the open solution without showing any dialog, by driving the
/// view models of the Connected Mode dialogs the way a user does. Only meant for the hidden E2E test commands.
/// </summary>
public interface IConnectedModeAutomation
{
    /// <summary>New Connection: select SonarQube Server and enter its URL, enter the token, then save.</summary>
    Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token);

    /// <summary>Manage Binding: select the connection to <paramref name="serverUrl"/>, select the project, then Bind.</summary>
    Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey);
}

[Export(typeof(IConnectedModeAutomation))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ConnectedModeAutomation(
    IConnectedModeServices connectedModeServices,
    IConnectedModeBindingServices connectedModeBindingServices,
    IConnectedModeUIManager connectedModeUiManager)
    : IConnectedModeAutomation
{
    internal const string NextButtonDisabledText = "The Server Selection dialog would not allow Next with this URL.";
    internal const string ConnectionNotListedText = "The Manage Binding dialog does not list a connection to this server.";
    internal const string SelectProjectButtonDisabledText = "The Manage Binding dialog would not allow selecting a project.";
    internal const string ProjectNotSelectedText = "The Project Selection dialog did not select the project.";
    internal const string BindButtonDisabledText = "The Manage Binding dialog would not allow Bind.";

    public async Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token)
    {
        // Server Selection dialog: choose SonarQube Server, type the URL, Next.
        var serverSelection = SelectSonarQubeServer(serverUrl);
        if (!serverSelection.IsNextButtonEnabled)
        {
            return new ResponseStatus(false, NextButtonDisabledText);
        }
        var connectionInfo = serverSelection.CreateTransientConnectionInfo();

        // Credentials dialog: type the token, OK.
        var credentials = new CredentialsViewModel(connectionInfo, connectedModeServices.SlCoreConnectionAdapter, CreateProgressReporter())
        {
            Token = token.ToSecureString()
        };
        if (!await credentials.ValidateConnectionAsync())
        {
            return Outcome(credentials.ProgressReporterViewModel);
        }

        // What the Manage Connections dialog does once the Credentials dialog closes.
        var manageConnections = new ManageConnectionsViewModel(connectedModeUiManager, connectedModeServices, connectedModeBindingServices, CreateProgressReporter());
        await manageConnections.CreateConnectionsWithProgressAsync(new Connection(connectionInfo), credentials.GetCredentialsModel());
        return Outcome(manageConnections.ProgressReporterViewModel);
    }

    public async Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey)
    {
        // Manage Binding dialog: what it loads when it opens.
        using var manageBinding = new ManageBindingViewModel(connectedModeServices, connectedModeBindingServices, connectedModeUiManager, CreateProgressReporter());
        await manageBinding.InitializeDataAsync();
        if (manageBinding.ProgressReporter.HasWarning)
        {
            return Outcome(manageBinding.ProgressReporter);
        }

        // The connections combo box.
        var serverId = SelectSonarQubeServer(serverUrl).CreateTransientConnectionInfo().GetServerIdFromConnectionInfo();
        manageBinding.SelectedConnectionInfo = manageBinding.Connections.FirstOrDefault(x => x.GetServerIdFromConnectionInfo() == serverId);
        if (manageBinding.SelectedConnectionInfo == null)
        {
            return new ResponseStatus(false, ConnectionNotListedText);
        }
        if (!manageBinding.IsSelectProjectButtonEnabled)
        {
            return new ResponseStatus(false, SelectProjectButtonDisabledText);
        }

        // Select Project: the Project Selection dialog.
        var projectSelection = await SelectProjectAsync(manageBinding.SelectedConnectionInfo, projectKey);
        if (projectSelection.SelectedProject?.Key != projectKey)
        {
            return projectSelection.ProgressReporterViewModel.HasWarning ? Outcome(projectSelection.ProgressReporterViewModel) : new ResponseStatus(false, ProjectNotSelectedText);
        }
        manageBinding.SelectedProject = projectSelection.SelectedProject;

        // The Bind button.
        if (!manageBinding.IsBindButtonEnabled)
        {
            return new ResponseStatus(false, BindButtonDisabledText);
        }
        await manageBinding.PerformManualBindingWithProgressAsync();
        return Outcome(manageBinding.ProgressReporter);
    }

    private static ServerSelectionViewModel SelectSonarQubeServer(string serverUrl) =>
        new() { IsSonarCloudSelected = false, IsSonarQubeSelected = true, SonarQubeUrl = serverUrl };

    /// <summary>
    /// Picks the project from the list the dialog loads when it opens. If it is not listed, uses
    /// "Choose another project" and types the key, as a user would.
    /// </summary>
    private async Task<ProjectSelectionViewModel> SelectProjectAsync(ConnectionInfo connectionInfo, string projectKey)
    {
        var projectSelection = new ProjectSelectionViewModel(connectionInfo, connectedModeServices, CreateProgressReporter());
        await projectSelection.InitializeProjectWithProgressAsync();
        projectSelection.SelectedProject = projectSelection.ProjectResults.FirstOrDefault(x => x.Key == projectKey);
        if (projectSelection.SelectedProject == null)
        {
            await projectSelection.AddManualProjectWithProgressAsync(projectKey);
        }
        return projectSelection;
    }

    private ProgressReporterViewModel CreateProgressReporter() => new(connectedModeServices.Logger);

    // The view models report the result where the dialogs show it: a failure always sets a warning.
    private static ResponseStatus Outcome(IProgressReporterViewModel progressReporter) =>
        progressReporter.HasWarning ? new ResponseStatus(false, progressReporter.Warning) : new ResponseStatus(true);
}
