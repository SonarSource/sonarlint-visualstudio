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
using System.Security;
using System.Windows;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageBinding;

[TestClass]
public class ManageBindingViewModelTests
{
    private const string ALocalProjectKey = "local-project-key";
    
    private readonly ServerProject serverProject = new ("a-project", "A Project");
    private readonly ConnectionInfo sonarQubeConnectionInfo = new ("http://localhost:9000", ConnectionServerType.SonarQube);
    private readonly ConnectionInfo sonarCloudConnectionInfo = new ("organization", ConnectionServerType.SonarCloud);
    private readonly BasicAuthCredentials validCredentials = new ("TOKEN", new SecureString());
    private readonly SharedBindingConfigModel sonarQubeSharedBindingConfigModel = new() { Uri = new Uri("http://localhost:9000"), ProjectKey = "myProj" };
    private readonly SharedBindingConfigModel sonarCloudSharedBindingConfigModel = new() { Organization = "myOrg", ProjectKey = "myProj" };

    private ManageBindingViewModel testSubject;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private IConnectedModeServices connectedModeServices;
    private IBindingController bindingController;
    private ISolutionInfoProvider solutionInfoProvider;
    private IProgressReporterViewModel progressReporterViewModel;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private IConnectedModeBindingServices connectedModeBindingServices;
    private ISharedBindingConfigProvider sharedBindingConfigProvider;
    private IMessageBox messageBox;

    [TestInitialize]
    public void TestInitialize()
    {
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();

        testSubject = new ManageBindingViewModel(connectedModeServices, connectedModeBindingServices, progressReporterViewModel);

        MockServices();
    }

    [TestMethod]
    public void IsCurrentProjectBound_ProjectIsBound_ReturnsTrue()
    {
        testSubject.BoundProject = serverProject;

        testSubject.IsCurrentProjectBound.Should().BeTrue();
    }

    [TestMethod]
    public void IsCurrentProjectBound_ProjectIsNotBound_ReturnsFalse()
    {
        testSubject.BoundProject = null;

        testSubject.IsCurrentProjectBound.Should().BeFalse();
    }

    [TestMethod]
    public void BoundProject_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.BoundProject = serverProject;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.BoundProject)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsCurrentProjectBound)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelectionEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSelectProjectButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsExportButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsUseSharedBindingButtonVisible)));
    }

    [TestMethod]
    public void IsProjectSelected_ProjectIsSelected_ReturnsTrue()
    {
        testSubject.SelectedProject = serverProject;

        testSubject.IsProjectSelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsProjectSelected_ProjectIsNotSelected_ReturnsFalse()
    {
        testSubject.SelectedProject = null;

        testSubject.IsProjectSelected.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedProject_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedProject = serverProject;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedProject)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsProjectSelected)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsBindButtonEnabled)));
    }

    [TestMethod]
    public void IsConnectionSelected_ProjectIsSelected_ReturnsTrue()
    {
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        testSubject.IsConnectionSelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsConnectionSelected_ProjectIsNotSelected_ReturnsFalse()
    {
        testSubject.SelectedConnectionInfo = null;

        testSubject.IsConnectionSelected.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedConnection_NewConnectionIsSet_ClearsSelectedProject()
    {
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;
        testSubject.SelectedProject = serverProject;

        testSubject.SelectedConnectionInfo = sonarCloudConnectionInfo;

        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void SelectedConnection_SameConnectionIsSet_DoesNotClearSelectedProject()
    {
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;
        testSubject.SelectedProject = serverProject;

        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        testSubject.SelectedProject.Should().Be(serverProject);
    }

    [TestMethod]
    public void SelectedConnection_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedConnectionInfo)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelected)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSelectProjectButtonEnabled)));
    }

    [TestMethod]
    public void Unbind_SetsBoundProjectToNull()
    {
        testSubject.BoundProject = serverProject;

        testSubject.Unbind();

        testSubject.BoundProject.Should().BeNull();
    }

    [TestMethod]
    public void Unbind_SetsConnectionInfoToNull()
    {
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;
        testSubject.SelectedProject = serverProject;

        testSubject.Unbind();

        testSubject.SelectedConnectionInfo.Should().BeNull();
        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.SelectedProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);

        testSubject.IsBindButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsInProgress_ReturnsFalse()
    {
        testSubject.SelectedProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(true);

        testSubject.IsBindButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsBindButtonEnabled_ProjectIsNotSelected_ReturnsFalse(bool isBindingInProgress)
    {
        testSubject.SelectedProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsBindButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void IsManageConnectionsButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(bool isBindingInProgress, bool expectedResult)
    {
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsManageConnectionsButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void IsUseSharedBindingButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(bool isBindingInProgress, bool expectedResult)
    {
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsUseSharedBindingButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    public void SharedBindingConfigModel_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SharedBindingConfigModel = new SharedBindingConfigModel();

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsUseSharedBindingButtonVisible)));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void IsUnbindButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(bool isBindingInProgress, bool expectedResult)
    {
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsUnbindButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_ConnectionIsSelectedAndNoBindingIsInProgressAndProjectIsNotBound_ReturnsTrue()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        testSubject.IsSelectProjectButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(true);
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsSelectProjectButtonEnabled_ConnectionIsNotSelected_ReturnsFalse(bool isBindingInProgress)
    {
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);
        testSubject.SelectedConnectionInfo = null;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_ProjectIsAlreadyBound_ReturnsFalse()
    {
        testSubject.BoundProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(true);

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IsConnectionSelectionEnabled_ProjectIsBound_ReturnsFalse(bool isBindingInProgress)
    {
        testSubject.BoundProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_NoConnectionsExist_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_ProjectIsNotBoundAndBindingIsNotInProgressAndConnectionsExist_ReturnsTrue()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        MockTryGetAllConnectionsInfo([sonarCloudConnectionInfo]);
        testSubject.LoadConnections();

        testSubject.IsConnectionSelectionEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void UpdateProgress_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.UpdateProgress("In progress...");

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsUseSharedBindingButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsBindButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsUnbindButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsManageConnectionsButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSelectProjectButtonEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelectionEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsExportButtonEnabled)));
    }

    [TestMethod]
    public void IsExportButtonEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(true);

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IsExportButtonEnabled_ProjectIsNotBound_ReturnsFalse(bool isBindingInProgress)
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsExportButtonEnabled_ProjectIsBoundAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.BoundProject = serverProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);

        testSubject.IsExportButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void LoadConnections_FillsConnections()
    {
        List<ConnectionInfo> existingConnections = [sonarQubeConnectionInfo, sonarCloudConnectionInfo];
        MockTryGetAllConnectionsInfo(existingConnections);

        testSubject.LoadConnections();

        testSubject.Connections.Should().BeEquivalentTo(existingConnections);
    }

    [TestMethod]
    public void LoadConnections_ClearsPreviousConnections()
    {
        MockTryGetAllConnectionsInfo([sonarQubeConnectionInfo]);
        testSubject.Connections.Add(sonarCloudConnectionInfo);

        testSubject.LoadConnections();

        testSubject.Connections.Should().BeEquivalentTo([sonarQubeConnectionInfo]);
    }

    [TestMethod]
    public void LoadConnections_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.LoadConnections();

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelectionEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.ConnectionSelectionCaptionText)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void LoadConnectionsAsync_ReturnsResponseFromAdapter(bool expectedStatus)
    {
        serverConnectionsRepositoryAdapter.TryGetAllConnectionsInfo(out Arg.Any<List<ConnectionInfo>>()).Returns(expectedStatus);

        var succeeded = testSubject.LoadConnections();

        succeeded.Should().Be(expectedStatus);
    }

    [TestMethod]
    public async Task InitializeDataAsync_InitializesDataAndReportsProgress()
    {
        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.LoadDataAsync &&
                    x.ProgressStatus == UiResources.LoadingConnectionsText &&
                    x.WarningText == UiResources.LoadingConnectionsFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }
    
    [TestMethod]
    public async Task InitializeDataAsync_DisplaysBindStatusAndReportsProgress()
    {
        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.DisplayBindStatusAsync &&
                    x.ProgressStatus == UiResources.FetchingBindingStatusText &&
                    x.WarningText == UiResources.FetchingBindingStatusFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsNotBound_Succeeds()
    {
        SetupUnboundProject();
        
        var response = await testSubject.DisplayBindStatusAsync();
        
        response.Should().BeEquivalentTo(new AdapterResponse(true));
    }
    
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundAndBindingStatusIsFetched_Succeeds()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(sonarCloudConnection, serverProject);
        
        var response = await testSubject.DisplayBindStatusAsync();
        
        testSubject.BoundProject.Should().NotBeNull();
        response.Should().BeEquivalentTo(new AdapterResponse(true));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundButBindingStatusIsNotFetched_Fails()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProjectThatDoesNotExistOnServer(sonarCloudConnection);
        
        var response = await testSubject.DisplayBindStatusAsync();
        
        testSubject.BoundProject.Should().BeNull();
        response.Should().BeEquivalentTo(new AdapterResponse(false));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenSolutionIsOpen_FetchesSolutionInfo()
    {
        solutionInfoProvider.GetSolutionNameAsync().Returns("Local solution name");
        solutionInfoProvider.IsFolderWorkspaceAsync().Returns(false);
        
        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SolutionInfo.Should().BeEquivalentTo(new SolutionInfoModel("Local solution name", SolutionType.Solution));
    }
    
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenFolderIsOpen_FetchesSolutionInfo()
    {
        solutionInfoProvider.GetSolutionNameAsync().Returns("Local folder name");
        solutionInfoProvider.IsFolderWorkspaceAsync().Returns(true);
        
        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SolutionInfo.Should().BeEquivalentTo(new SolutionInfoModel("Local folder name", SolutionType.Folder));
    }
    
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundToSonarCloud_SelectsBoundSonarCloudConnection()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(sonarCloudConnection);

        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SelectedConnectionInfo.Should().BeEquivalentTo(new ConnectionInfo("organization", ConnectionServerType.SonarCloud));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundToSonarQube_SelectsBoundSonarQubeConnection()
    {
        var sonarQubeConnection = new ServerConnection.SonarQube(new Uri("http://localhost:9000/"), credentials: validCredentials);
        SetupBoundProject(sonarQubeConnection);
        
        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SelectedConnectionInfo.Should().BeEquivalentTo(new ConnectionInfo("http://localhost:9000/", ConnectionServerType.SonarQube));
    }
    
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsNotBound_SelectedConnectionShouldBeEmpty()
    {
        SetupUnboundProject();
        
        await testSubject.DisplayBindStatusAsync();

        testSubject.SelectedConnectionInfo.Should().BeNull();
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBound_SelectsServerProject()
    {
        var expectedServerProject = new ServerProject("server-project-key", "server-project-name");
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(sonarCloudConnection, expectedServerProject);
        
        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SelectedProject.Should().BeEquivalentTo(expectedServerProject);
        testSubject.BoundProject.Should().BeEquivalentTo(testSubject.SelectedProject);
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundButProjectNotFoundOnServer_SelectedProjectShouldBeEmpty()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProjectThatDoesNotExistOnServer(sonarCloudConnection);
        
        await testSubject.DisplayBindStatusAsync();
        
        testSubject.SelectedProject.Should().BeNull();
        testSubject.BoundProject.Should().BeNull();
    }
    
    [TestMethod]
    public async Task LoadDataAsync_LoadsConnectionsOnUIThread()
    {
        await testSubject.LoadDataAsync();

        await threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public async Task LoadDataAsync_LoadingConnectionsThrows_ReturnsFalse()
    {
        var exceptionMsg = "Failed to load connections";
        var mockedThreadHandling = Substitute.For<IThreadHandling>();
        connectedModeServices.ThreadHandling.Returns(mockedThreadHandling);
        mockedThreadHandling.When(x => x.RunOnUIThreadAsync(Arg.Any<Action>())).Do(callInfo=> throw new Exception(exceptionMsg));

        var adapterResponse = await testSubject.LoadDataAsync();

        adapterResponse.Success.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void ConnectionSelectionCaptionText_ConnectionsExists_ReturnsSelectConnectionToBindDescription()
    {
        testSubject.Connections.Add(sonarCloudConnectionInfo);

        testSubject.ConnectionSelectionCaptionText.Should().Be(UiResources.SelectConnectionToBindDescription);
    }

    [TestMethod]
    public void ConnectionSelectionCaptionText_NoConnectionExists_ReturnsNoConnectionExistsLabel()
    {
        testSubject.SelectedConnectionInfo = null;

        testSubject.ConnectionSelectionCaptionText.Should().Be(UiResources.NoConnectionExistsLabel);
    }

    [TestMethod]
    public async Task BindWithProgressAsync_BindsProjectAndReportsProgress()
    {
        await testSubject.BindWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.BindAsync &&
                    x.ProgressStatus == UiResources.BindingInProgressText &&
                    x.WarningText == UiResources.BindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }

    [TestMethod]
    public async Task BindAsync_WhenConnectionNotFound_Fails()
    {
        testSubject.SelectedConnectionInfo = new ConnectionInfo("organization", ConnectionServerType.SonarCloud);
        serverConnectionsRepositoryAdapter.TryGetServerConnectionById("organization", out _).Returns(callInfo =>
        {
            callInfo[1] = null;
            return false;
        });
        
        var response = await testSubject.BindAsync();
        
        response.Success.Should().BeFalse();
    }
    
    [TestMethod]
    public async Task BindAsync_WhenBindingFailsUnexpectedly_FailsAndLogs()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupConnectionAndProjectToBind(sonarCloudConnection, serverProject);
        bindingController.BindAsync(Arg.Any<BoundServerProject>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Failed unexpectedly"));
        
        var response = await testSubject.BindAsync();
        
        response.Success.Should().BeFalse();
        logger.Received(1).WriteLine(Resources.Binding_Fails, "Failed unexpectedly");
    }

    [TestMethod]
    public async Task BindAsync_WhenBindingCompletesSuccessfully_Succeeds()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupConnectionAndProjectToBind(sonarCloudConnection, serverProject);

        var response = await testSubject.BindAsync();
        
        response.Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task BindAsync_WhenBindingCompletesSuccessfully_SetsBoundProjectToSelectedProject()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupConnectionAndProjectToBind(sonarCloudConnection, serverProject);
        
        await testSubject.BindAsync();
        
        testSubject.BoundProject.Should().BeEquivalentTo(serverProject);
    }

    [TestMethod]
    public void DetectSharedBinding_CurrentProjectBound_DoesNothing()
    {
        testSubject.BoundProject = serverProject;

        testSubject.DetectSharedBinding();

        sharedBindingConfigProvider.DidNotReceive().GetSharedBinding();
    }

    [TestMethod]
    public void DetectSharedBinding_CurrentProjectNotBound_UpdatesSharedBindingConfigModel()
    {
        testSubject.BoundProject = null;
        var sharedBindingModel = new SharedBindingConfigModel();
        sharedBindingConfigProvider.GetSharedBinding().Returns(sharedBindingModel);

        testSubject.DetectSharedBinding();

        sharedBindingConfigProvider.Received(1).GetSharedBinding();
        testSubject.SharedBindingConfigModel.Should().Be(sharedBindingModel);
    }

    [TestMethod]
    public async Task UseSharedBindingWithProgressAsync_SharedBindingExistsAndValid_BindsProjectAndReportsProgress()
    {
        testSubject.SharedBindingConfigModel = sonarQubeSharedBindingConfigModel;

        await testSubject.UseSharedBindingWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponse>>(x =>
                    x.TaskToPerform == testSubject.UseSharedBindingAsync &&
                    x.ProgressStatus == UiResources.BindingInProgressText &&
                    x.WarningText == UiResources.BindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForSonarQubeConnection_BindsWithTheCorrectProjectKey()
    {
        testSubject.SelectedProject = serverProject; // this is to make sure the SelectedProject is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarQubeSharedBindingConfigModel;
        SetupBoundProject(new ServerConnection.SonarQube(testSubject.SharedBindingConfigModel.Uri));

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeTrue();
        await bindingController.Received(1)
            .BindAsync(Arg.Is<BoundServerProject>(proj =>
                    proj.ServerProjectKey == testSubject.SharedBindingConfigModel.ProjectKey), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForSonarCloudConnection_BindsWithTheCorrectProjectKey()
    {
        testSubject.SelectedConnectionInfo = sonarQubeConnectionInfo; // this is to make sure the SelectedConnectionInfo is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarCloudSharedBindingConfigModel;
        SetupBoundProject(new ServerConnection.SonarCloud(testSubject.SharedBindingConfigModel.Organization));

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeTrue();
        await bindingController.Received(1)
            .BindAsync(Arg.Is<BoundServerProject>(proj =>
                proj.ServerProjectKey == testSubject.SharedBindingConfigModel.ProjectKey), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForExistSonarQubeConnection_BindsWithTheCorrectConnectionId()
    {
        testSubject.SelectedConnectionInfo = sonarCloudConnectionInfo; // this is to make sure the SelectedConnectionInfo is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarQubeSharedBindingConfigModel;
        var expectedServerConnection = new ServerConnection.SonarQube(testSubject.SharedBindingConfigModel.Uri);
        SetupBoundProject(expectedServerConnection);

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeTrue();
        serverConnectionsRepositoryAdapter.Received(1).TryGetServerConnectionById(testSubject.SharedBindingConfigModel.Uri.ToString(), out _);
        await bindingController.Received(1)
            .BindAsync(Arg.Is<BoundServerProject>(proj => proj.ServerConnection == expectedServerConnection), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForExistingSonarCloudConnection_BindsWithTheCorrectConnectionId()
    {
        testSubject.SelectedProject = serverProject; // this is to make sure the SelectedProject is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarCloudSharedBindingConfigModel;
        var expectedServerConnection  = new ServerConnection.SonarCloud(testSubject.SharedBindingConfigModel.Organization);
        SetupBoundProject(expectedServerConnection);

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeTrue();
        serverConnectionsRepositoryAdapter.Received(1).TryGetServerConnectionById(testSubject.SharedBindingConfigModel.Organization, out _);
        await bindingController.Received(1)
            .BindAsync(Arg.Is<BoundServerProject>(proj => proj.ServerConnection == expectedServerConnection), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForNonExistingSonarQubeConnection_ReturnsFalseAndLogsAndInformsUser()
    {
        testSubject.SelectedProject = serverProject; // this is to make sure the SelectedProject is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarQubeSharedBindingConfigModel;

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeFalse();
        messageBox.Received(1).Show(UiResources.NotFoundConnectionForSharedBindingMessageBoxText, UiResources.NotFoundConnectionForSharedBindingMessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
        logger.WriteLine(Resources.UseSharedBinding_ConnectionNotFound, testSubject.SharedBindingConfigModel.Uri);
        await bindingController.DidNotReceive()
            .BindAsync(Arg.Is<BoundServerProject>(proj =>
                proj.ServerProjectKey == testSubject.SharedBindingConfigModel.ProjectKey), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_SharedBindingForNonExistingSonarCloudConnection_ReturnsFalseAndLogsAndInformsUser()
    {
        testSubject.SelectedProject = serverProject; // this is to make sure the SelectedProject is ignored and the shared config is used instead
        testSubject.SharedBindingConfigModel = sonarCloudSharedBindingConfigModel;

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeFalse();
        logger.WriteLine(Resources.UseSharedBinding_ConnectionNotFound, testSubject.SharedBindingConfigModel.Organization);
        messageBox.Received(1).Show(UiResources.NotFoundConnectionForSharedBindingMessageBoxText, UiResources.NotFoundConnectionForSharedBindingMessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
        await bindingController.DidNotReceive()
            .BindAsync(Arg.Is<BoundServerProject>(proj =>
                proj.ServerProjectKey == testSubject.SharedBindingConfigModel.ProjectKey), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UseSharedBindingAsync_BindingFails_ReturnsFalse()
    {
        MockTryGetServerConnectionId();
        bindingController.When(x => x.BindAsync(Arg.Any<BoundServerProject>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception());
        testSubject.SharedBindingConfigModel = sonarCloudSharedBindingConfigModel;

        var response = await testSubject.UseSharedBindingAsync();

        response.Success.Should().BeFalse();
    }

    private void MockServices()
    {
        serverConnectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = Substitute.For<ILogger>();
        messageBox = Substitute.For<IMessageBox>();

        connectedModeServices.ServerConnectionsRepositoryAdapter.Returns(serverConnectionsRepositoryAdapter);
        connectedModeServices.ThreadHandling.Returns(threadHandling);
        connectedModeServices.Logger.Returns(logger);
        connectedModeServices.MessageBox.Returns(messageBox);

        bindingController = Substitute.For<IBindingController>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        sharedBindingConfigProvider = Substitute.For<ISharedBindingConfigProvider>();
        connectedModeBindingServices.BindingController.Returns(bindingController);
        connectedModeBindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
        connectedModeBindingServices.SharedBindingConfigProvider.Returns(sharedBindingConfigProvider);

        MockTryGetAllConnectionsInfo([]);
    }

    private void MockTryGetAllConnectionsInfo(List<ConnectionInfo> connectionInfos)
    {
        connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnectionsInfo(out _).Returns(callInfo =>
        {
            callInfo[0] = connectionInfos;
            return true;
        });
    }
    
    private void SetupConnectionAndProjectToBind(ServerConnection selectedServerConnection, ServerProject selectedServerProject)
    {
        SetupBoundProject(selectedServerConnection, selectedServerProject);
        testSubject.SelectedConnectionInfo = sonarCloudConnectionInfo;
        testSubject.SelectedProject = selectedServerProject;
    }
    
    private void SetupBoundProject(ServerConnection serverConnection, ServerProject expectedServerProject = null)
    {
        expectedServerProject ??= serverProject;
        
        var boundServerProject = new BoundServerProject(ALocalProjectKey, expectedServerProject.Key, serverConnection);
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(boundServerProject, SonarLintMode.Connected, "binding-dir"));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
        MockTryGetServerConnectionId(serverConnection);
        solutionInfoProvider.GetSolutionNameAsync().Returns(ALocalProjectKey);
        
        MockGetServerProjectByKey(true, expectedServerProject);
    }

    private void MockTryGetServerConnectionId(ServerConnection serverConnection = null)
    {
        serverConnectionsRepositoryAdapter.TryGetServerConnectionById(serverConnection?.Id ?? Arg.Any<string>(), out _).Returns(callInfo =>
        {
            callInfo[1] = serverConnection;
            return true;
        });
    }

    private void SetupUnboundProject()
    {
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(null, SonarLintMode.Standalone, null));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
        
        MockGetServerProjectByKey(false, null);
    }
    
    private void SetupBoundProjectThatDoesNotExistOnServer(ServerConnection serverConnection)
    {
        var boundServerProject = new BoundServerProject(ALocalProjectKey, "a-server-project", serverConnection);
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(boundServerProject, SonarLintMode.Connected, "binding-dir"));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
        
        MockGetServerProjectByKey(false, null);
    }

    private void MockGetServerProjectByKey(bool success, ServerProject responseData)
    {
        var slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        slCoreConnectionAdapter.GetServerProjectByKeyAsync(Arg.Any<ICredentials>(), Arg.Any<ConnectionInfo>(),Arg.Any<string>())
            .Returns(Task.FromResult(new AdapterResponseWithData<ServerProject>(success, responseData)));
        connectedModeServices.SlCoreConnectionAdapter.Returns(slCoreConnectionAdapter);
    }
}
