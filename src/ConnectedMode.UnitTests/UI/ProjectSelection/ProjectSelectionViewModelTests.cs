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

using System.ComponentModel;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ProjectSelection;

[TestClass]
public class ProjectSelectionViewModelTests
{
    private static readonly List<ServerProject> AnInitialListOfProjects =
    [
        new ServerProject("a-project", "A Project"),
        new ServerProject("another-project", "Another Project")
    ];

    private static readonly ConnectionInfo AConnectionInfo = new("http://localhost:9000", ConnectionServerType.SonarQube);
    
    private ProjectSelectionViewModel testSubject;
    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private IProgressReporterViewModel progressReporterViewModel;
    private IConnectedModeServices connectedModeServices;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private ILogger logger;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        logger = Substitute.For<ILogger>();
        connectedModeServices.SlCoreConnectionAdapter.Returns(slCoreConnectionAdapter);
        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeServices.Logger.Returns(logger);

        testSubject = new ProjectSelectionViewModel(AConnectionInfo, connectedModeServices, progressReporterViewModel);
    }

    [TestMethod]
    public void IsProjectSelected_NoProjectSelected_ReturnsFalse()
    {
        testSubject.IsProjectSelected.Should().BeFalse();
    }
    
    [TestMethod]
    public void IsProjectSelected_ProjectSelected_ReturnsTrue()
    {
        testSubject.SelectedProject = new ServerProject("a-project", "A Project");
        
        testSubject.IsProjectSelected.Should().BeTrue();
    }
    
    [TestMethod]
    public void InitProjects_ResetsTheProjectResults()
    {
        MockInitializedProjects(AnInitialListOfProjects);
        testSubject.ProjectResults.Should().BeEquivalentTo(AnInitialListOfProjects);
        
        var updatedListOfProjects = new List<ServerProject>
        {
            new("new-project", "New Project")
        };
        MockInitializedProjects(updatedListOfProjects);
        testSubject.ProjectResults.Should().BeEquivalentTo(updatedListOfProjects);
    }

    [TestMethod]
    public void InitProjects_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        MockInitializedProjects(AnInitialListOfProjects);

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoProjectExists)));
    }

    [TestMethod]
    public void ProjectSearchTerm_WithEmptyTerm_ShouldNotUpdateSearchResult()
    {
        MockInitializedProjects(AnInitialListOfProjects);

        testSubject.ProjectSearchTerm = "";

        testSubject.ProjectResults.Should().BeEquivalentTo(AnInitialListOfProjects);
    }

    [TestMethod]
    public void ProjectSearchTerm_WithTerm_ShouldUpdateSearchResult()
    {
        MockInitializedProjects(AnInitialListOfProjects);

        testSubject.ProjectSearchTerm = "My Project";

        testSubject.ProjectResults.Should().NotContain(AnInitialListOfProjects);
    }

    [TestMethod]
    public void SearchForProject_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ProjectSearchTerm = "proj1";

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoProjectExists)));
    }

    [TestMethod]
    public void NoProjectExists_NoProjects_ReturnsTrue()
    {
        MockInitializedProjects([]);

        testSubject.NoProjectExists.Should().BeTrue();
    }

    [TestMethod]
    public void NoProjectExists_HasProjects_ReturnsFalse()
    {
        MockInitializedProjects(AnInitialListOfProjects); 

        testSubject.NoProjectExists.Should().BeFalse();
    }

    [TestMethod]
    public async Task InitializeProjectWithProgressAsync_ExecutesInitializationWithProgress()
    {
        await testSubject.InitializeProjectWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponseWithData<List<ServerProject>>>>(x =>
                    x.TaskToPerform == testSubject.AdapterGetAllProjectsAsync &&
                    x.ProgressStatus == UiResources.LoadingProjectsProgressText &&
                    x.WarningText == UiResources.LoadingProjectsFailedText &&
                    x.AfterSuccess == testSubject.InitProjects));
    }

    [TestMethod]
    public async Task AdapterGetAllProjectsAsync_GettingServerConnectionSucceeded_CallsAdapterWithCredentialsForServerConnection()
    {
        var expectedCredentials = Substitute.For<ICredentials>();
        MockTrySonarQubeConnection(AConnectionInfo, success:true, expectedCredentials);

        await testSubject.AdapterGetAllProjectsAsync();

        serverConnectionsRepositoryAdapter.Received(1).TryGet(AConnectionInfo.Id, out Arg.Any<ServerConnection>());
        await slCoreConnectionAdapter.Received(1).GetAllProjectsAsync(AConnectionInfo, expectedCredentials);
    }

    [TestMethod]
    public async Task AdapterGetAllProjectsAsync_GettingServerConnectionFailed_ReturnsFailure()
    {
        MockTrySonarQubeConnection(AConnectionInfo, success:false);

        var response = await testSubject.AdapterGetAllProjectsAsync();

        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeNull();
        logger.Received(1).WriteLine(Arg.Any<string>());
        await slCoreConnectionAdapter.DidNotReceive().GetAllProjectsAsync(Arg.Any<ConnectionInfo>(), Arg.Any<ICredentials>());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AdapterGetAllProjectsAsync_ReturnsResponseFromAdapter(bool expectedResponse)
    {
        MockTrySonarQubeConnection(AConnectionInfo, success: true);
        var expectedServerProjects = new List<ServerProject>{new("proj1", "name1"), new("proj2", "name2") };
        slCoreConnectionAdapter.GetAllProjectsAsync(AConnectionInfo, Arg.Any<ICredentials>())
            .Returns(new AdapterResponseWithData<List<ServerProject>>(expectedResponse, expectedServerProjects));

        var response = await testSubject.AdapterGetAllProjectsAsync();

        response.Success.Should().Be(expectedResponse);
        response.ResponseData.Should().BeEquivalentTo(expectedServerProjects);
    }

    private void MockInitializedProjects(List<ServerProject> serverProjects)
    {
        testSubject.InitProjects(new AdapterResponseWithData<List<ServerProject>>(true, serverProjects));
    }

    private void MockTrySonarQubeConnection(ConnectionInfo connectionInfo, bool success = true, ICredentials expectedCredentials = null)
    {
        serverConnectionsRepositoryAdapter.TryGet(connectionInfo.Id, out _).Returns(callInfo =>
        {
            callInfo[1] = new ServerConnection.SonarQube(new Uri(connectionInfo.Id), credentials: expectedCredentials);
            return success;
        });
    }
}
