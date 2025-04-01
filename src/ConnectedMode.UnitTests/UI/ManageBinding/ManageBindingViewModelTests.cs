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

using System.ComponentModel;
using System.Security;
using System.Windows;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.ConnectedMode.UI.BindingRequest;
using IConnectionCredentials = SonarLint.VisualStudio.Core.Binding.IConnectionCredentials;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageBinding;

[TestClass]
public class ManageBindingViewModelTests
{
    private const string ALocalProjectKey = "local-project-key";
    private static readonly ServerProject ServerProject = new("a-project", "A Project");
    private static readonly ConnectionInfo SonarQubeConnectionInfo = new("http://localhost:9000", ConnectionServerType.SonarQube);
    private static readonly ConnectionInfo SonarCloudConnectionInfo = new("organization", ConnectionServerType.SonarCloud);

    private readonly SolutionInfoModel defaultSolution = new("Any.sln", default);
    private readonly SolutionInfoModel noSolution = new(null, default);
    private readonly SharedBindingConfigModel sonarCloudSharedBindingConfigModel = new() { Organization = "myOrg", ProjectKey = "myProj" };
    private readonly UsernameAndPasswordCredentials validCredentials = new("TOKEN", new SecureString());
    private IConnectedModeBindingServices connectedModeBindingServices;
    private IConnectedModeServices connectedModeServices;
    private IConnectedModeUIManager connectedModeUIManager;
    private IConnectedModeUIServices connectedModeUIServices;
    private ILogger logger;
    private IMessageBox messageBox;
    private IProgressReporterViewModel progressReporterViewModel;
    private IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter;
    private ISharedBindingConfigProvider sharedBindingConfigProvider;
    private ISolutionInfoProvider solutionInfoProvider;

    private ManageBindingViewModel testSubject;
    private IThreadHandling threadHandling;

    public static object[][] SonarCloudRegions =>
    [
        [CloudServerRegion.Eu],
        [CloudServerRegion.Us]
    ];

    [TestInitialize]
    public void TestInitialize()
    {
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();
        connectedModeUIServices = Substitute.For<IConnectedModeUIServices>();
        connectedModeUIManager = Substitute.For<IConnectedModeUIManager>();

        testSubject = new ManageBindingViewModel(connectedModeServices, connectedModeBindingServices, connectedModeUIServices, connectedModeUIManager, progressReporterViewModel);

        testSubject.SolutionInfo = defaultSolution;
        MockServices();
    }

    [TestMethod]
    public void SolutionInfo_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SolutionInfo = defaultSolution;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SolutionInfo)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSolutionOpen)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsOpenSolutionBound)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsOpenSolutionStandalone)));
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
    public void BoundProject_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.BoundProject = ServerProject;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.BoundProject)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsOpenSolutionBound)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsOpenSolutionStandalone)));
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
    public void BoundProject_Set_NoSolutionOpen_SetsNull()
    {
        testSubject.SolutionInfo = noSolution;

        testSubject.BoundProject = ServerProject;

        testSubject.BoundProject.Should().BeNull();
        testSubject.IsOpenSolutionBound.Should().BeFalse();
        testSubject.IsOpenSolutionStandalone.Should().BeFalse();
    }

    [TestMethod]
    public void BoundProject_Set_OpenSolutionBound_SetsValue()
    {
        testSubject.SolutionInfo = defaultSolution;

        testSubject.BoundProject = ServerProject;

        testSubject.BoundProject.Should().BeSameAs(ServerProject);
        testSubject.IsOpenSolutionBound.Should().BeTrue();
        testSubject.IsOpenSolutionStandalone.Should().BeFalse();
    }

    [TestMethod]
    public void BoundProject_Set_OpenSolutionStandalone_SetsNull()
    {
        testSubject.SolutionInfo = defaultSolution;

        testSubject.BoundProject = null;

        testSubject.BoundProject.Should().BeNull();
        testSubject.IsOpenSolutionBound.Should().BeFalse();
        testSubject.IsOpenSolutionStandalone.Should().BeTrue();
    }

    [TestMethod]
    public void IsProjectSelected_ProjectIsSelected_ReturnsTrue()
    {
        testSubject.SelectedProject = ServerProject;

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

        testSubject.SelectedProject = ServerProject;

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
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

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
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;
        testSubject.SelectedProject = ServerProject;

        testSubject.SelectedConnectionInfo = SonarCloudConnectionInfo;

        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void SelectedConnection_SameConnectionIsSet_DoesNotClearSelectedProject()
    {
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;
        testSubject.SelectedProject = ServerProject;

        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

        testSubject.SelectedProject.Should().Be(ServerProject);
    }

    [TestMethod]
    public void SelectedConnection_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedConnectionInfo)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelected)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSelectProjectButtonEnabled)));
    }

    [TestMethod]
    public void BindingSucceeded_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.BindingSucceeded = !testSubject.BindingSucceeded;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.BindingSucceeded)));
    }

    [TestMethod]
    public async Task UnbindWithProgressAsync_BindsProjectAndReportsProgress()
    {
        await testSubject.UnbindWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.TaskToPerform == testSubject.UnbindAsync &&
                    x.ProgressStatus == UiResources.UnbindingInProgressText &&
                    x.WarningText == UiResources.UnbindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }

    [TestMethod]
    public async Task UnbindAsync_UnbindsCurrentSolution()
    {
        await InitializeBoundProject();
        connectedModeServices.ThreadHandling.Returns(new NoOpThreadHandler());

        await testSubject.UnbindAsync();

        connectedModeBindingServices.BindingControllerAdapter.Received(1).Unbind(null);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task UnbindAsync_ReturnsResponseOfUnbinding(bool expectedResponse)
    {
        await InitializeBoundProject();
        connectedModeBindingServices.BindingControllerAdapter.Unbind(Arg.Any<string>()).Returns(expectedResponse);

        var adapterResponse = await testSubject.UnbindAsync();

        adapterResponse.Success.Should().Be(expectedResponse);
    }

    [TestMethod]
    public async Task UnbindAsync_UnbindingThrows_ReturnsFalse()
    {
        await InitializeBoundProject();
        var exceptionMsg = "Failed to load connections";
        connectedModeBindingServices.BindingControllerAdapter.When(x => x.Unbind(Arg.Any<string>())).Do(_ => throw new Exception(exceptionMsg));

        var adapterResponse = await testSubject.UnbindAsync();

        adapterResponse.Success.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public async Task UnbindAsync_ClearsBindingProperties()
    {
        await InitializeBoundProject();
        SetupConfigurationProvider(new BindingConfiguration(null, SonarLintMode.Standalone, null));

        await testSubject.UnbindAsync();

        testSubject.BoundProject.Should().BeNull();
        testSubject.SelectedConnectionInfo.Should().BeNull();
        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.SelectedProject = ServerProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);

        testSubject.IsBindButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsInProgress_ReturnsFalse()
    {
        testSubject.SelectedProject = ServerProject;
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
    public void IsUseSharedBindingButtonVisible_SharedBindingConfigExistsAndProjectIsBound_ReturnsFalse()
    {
        testSubject.SharedBindingConfigModel = new SharedBindingConfigModel();
        testSubject.BoundProject = ServerProject;

        testSubject.IsUseSharedBindingButtonVisible.Should().BeFalse();
    }

    [TestMethod]
    public void IsUseSharedBindingButtonVisible_SharedBindingConfigExistsAndProjectIsUnbound_ReturnsTrue()
    {
        testSubject.SharedBindingConfigModel = new SharedBindingConfigModel();
        testSubject.BoundProject = null;

        testSubject.IsUseSharedBindingButtonVisible.Should().BeTrue();
    }

    [TestMethod]
    public void IsUseSharedBindingButtonVisible_SharedBindingConfigDoesNotExistAndProjectIsBound_ReturnsFalse()
    {
        testSubject.SharedBindingConfigModel = null;
        testSubject.BoundProject = ServerProject;

        testSubject.IsUseSharedBindingButtonVisible.Should().BeFalse();
    }

    [TestMethod]
    public void IsUseSharedBindingButtonVisible_SharedBindingConfigDoesNotExistAndProjectIsUnbound_ReturnsFalse()
    {
        testSubject.SharedBindingConfigModel = null;
        testSubject.BoundProject = null;

        testSubject.IsUseSharedBindingButtonVisible.Should().BeFalse();
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
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

        testSubject.IsSelectProjectButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_NoSolution_ReturnsFalse()
    {
        testSubject.SolutionInfo = noSolution;
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(true);
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

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
        testSubject.BoundProject = ServerProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        testSubject.SelectedConnectionInfo = SonarQubeConnectionInfo;

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
        testSubject.BoundProject = ServerProject;
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
    public void IsConnectionSelectionEnabled_NoSolution_ReturnsFalse()
    {
        testSubject.SolutionInfo = noSolution;
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        MockTryGetAllConnectionsInfo([SonarCloudConnectionInfo]);
        testSubject.ReloadConnectionData();

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_ProjectIsNotBoundAndBindingIsNotInProgressAndConnectionsExist_ReturnsTrue()
    {
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(false);
        MockTryGetAllConnectionsInfo([SonarCloudConnectionInfo]);
        testSubject.ReloadConnectionData();

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
        testSubject.BoundProject = ServerProject;
        progressReporterViewModel.IsOperationInProgress.Returns(true);

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    public void IsExportButtonEnabled_ProjectIsNotBound_ReturnsFalse(bool isSolutionOpen, bool isBindingInProgress)
    {
        testSubject.SolutionInfo = isSolutionOpen ? defaultSolution : noSolution;
        testSubject.BoundProject = null;
        progressReporterViewModel.IsOperationInProgress.Returns(isBindingInProgress);

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsExportButtonEnabled_ProjectIsBoundAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.SolutionInfo = defaultSolution;
        testSubject.BoundProject = ServerProject;
        progressReporterViewModel.IsOperationInProgress.Returns(false);

        testSubject.IsExportButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void ReloadConnectionData_FillsConnections()
    {
        List<ConnectionInfo> existingConnections = [SonarQubeConnectionInfo, SonarCloudConnectionInfo];
        MockTryGetAllConnectionsInfo(existingConnections);

        testSubject.ReloadConnectionData();

        testSubject.Connections.Should().BeEquivalentTo(existingConnections);
    }

    [TestMethod]
    public void ReloadConnectionData_ClearsPreviousConnections()
    {
        MockTryGetAllConnectionsInfo([SonarQubeConnectionInfo]);
        testSubject.Connections.Add(SonarCloudConnectionInfo);

        testSubject.ReloadConnectionData();

        testSubject.Connections.Should().BeEquivalentTo([SonarQubeConnectionInfo]);
    }

    [TestMethod]
    public void ReloadConnectionData_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.ReloadConnectionData();

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelectionEnabled)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.ConnectionSelectionCaptionText)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReloadConnectionData_ReturnsResponseFromAdapter(bool expectedStatus)
    {
        serverConnectionsRepositoryAdapter.TryGetAllConnectionsInfo(out Arg.Any<List<ConnectionInfo>>()).Returns(expectedStatus);

        var succeeded = testSubject.ReloadConnectionData();

        succeeded.Should().BeEquivalentTo(new ResponseStatus(expectedStatus));
    }

    [TestMethod]
    public async Task InitializeDataAsync_InitializesDataAndReportsProgress()
    {
        MockProgressReporter();

        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.ProgressStatus == UiResources.LoadingConnectionsText &&
                    x.WarningText == UiResources.LoadingConnectionsFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                true);
    }

    [TestMethod]
    public async Task InitializeDataAsync_DisplaysBindStatusAndReportsProgress()
    {
        MockProgressReporter();

        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatusWithData<BindingResult>>>(x =>
                    x.TaskToPerform == testSubject.DisplayBindStatusAsync &&
                    x.ProgressStatus == UiResources.FetchingBindingStatusText &&
                    x.WarningText == UiResources.FetchingBindingStatusFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                false);
    }

    [TestMethod]
    public async Task InitializeDataAsync_WhenStandalone_ChecksForAutomaticBindingAndReportsProgress()
    {
        MockProgressReporter();
        SetupUnboundProject();

        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.TaskToPerform == testSubject.CheckForSharedBindingAsync &&
                    x.ProgressStatus == UiResources.CheckingForSharedBindingText &&
                    x.WarningText == UiResources.CheckingForSharedBindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                false);
    }

    [TestMethod]
    public async Task InitializeDataAsync_WhenBound_ChecksForAutomaticBindingAndReportsProgress()
    {
        MockProgressReporter();
        testSubject.BoundProject = ServerProject;

        await testSubject.InitializeDataAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.TaskToPerform == testSubject.CheckForSharedBindingAsync &&
                    x.ProgressStatus == UiResources.CheckingForSharedBindingText &&
                    x.WarningText == UiResources.CheckingForSharedBindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                false);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public async Task InitializeDataAsync_WhenBindingExistsButBindingProcessFails_SetsBindingSucceededOnlyWhenBothTasksSucceed(bool task1Response, bool task2Response)
    {
        testSubject.BoundProject = ServerProject;
        MockProgressReporter(task1Response, task2Response);

        await testSubject.InitializeDataAsync();

        testSubject.BindingSucceeded.Should().Be(task1Response && task2Response);
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsNotBound_Succeeds()
    {
        SetupUnboundProject();

        var response = await testSubject.DisplayBindStatusAsync();

        response.Should().BeEquivalentTo(new ResponseStatus(true));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundAndBindingStatusIsFetched_Succeeds()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(sonarCloudConnection, ServerProject);

        var response = await testSubject.DisplayBindStatusAsync();

        testSubject.BoundProject.Should().NotBeNull();
        response.Should().BeEquivalentTo(new ResponseStatus(true));
    }

    /// <summary>
    ///     Even if the project can not be found on the server, we still want to update the UI with the bound project, because the binding does exist
    /// </summary>
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundButBindingStatusIsNotFetched_FailsButStillShowsProject()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        var expectedSeverProject = new ServerProject("a-server-project", "a-server-project");
        SetupBoundProjectThatDoesNotExistOnServer(sonarCloudConnection, "a-server-project");

        var response = await testSubject.DisplayBindStatusAsync();

        testSubject.BoundProject.Should().BeEquivalentTo(expectedSeverProject);
        testSubject.SelectedProject.Should().BeEquivalentTo(expectedSeverProject);
        response.Should().BeEquivalentTo(new ResponseStatus(false));
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
        SetupBoundProject(sonarCloudConnection, ServerProject);

        await testSubject.DisplayBindStatusAsync();

        testSubject.SelectedConnectionInfo.Should().BeEquivalentTo(new ConnectionInfo("organization", ConnectionServerType.SonarCloud));
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundToSonarQube_SelectsBoundSonarQubeConnection()
    {
        var sonarQubeConnection = new ServerConnection.SonarQube(new Uri("http://localhost:9000/"), credentials: validCredentials);
        SetupBoundProject(sonarQubeConnection, ServerProject);

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

    /// <summary>
    ///     Even if the project can not be found on the server, we still want to update the UI with the bound project, because the binding does exist
    /// </summary>
    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectIsBoundButProjectNotFoundOnServer_SelectedProjectShouldNotBeEmpty()
    {
        var sonarCloudConnection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        var expectedSeverProject = new ServerProject("a-server-project", "a-server-project");
        SetupBoundProjectThatDoesNotExistOnServer(sonarCloudConnection, "a-server-project");

        await testSubject.DisplayBindStatusAsync();

        testSubject.SelectedProject.Should().BeEquivalentTo(expectedSeverProject);
        testSubject.BoundProject.Should().BeEquivalentTo(expectedSeverProject);
    }

    [TestMethod]
    public async Task DisplayBindStatusAsync_WhenProjectWasBoundAndBecomesUnbound_UpdatesCurrentProjectAndBindingInfoToNull()
    {
        await InitializeBoundProject();
        SetupUnboundProject();

        await testSubject.DisplayBindStatusAsync();

        testSubject.BoundProject.Should().BeNull();
        testSubject.SelectedConnectionInfo.Should().BeNull();
        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void ConnectionSelectionCaptionText_ConnectionsExists_ReturnsSelectConnectionToBindDescription()
    {
        testSubject.Connections.Add(SonarCloudConnectionInfo);

        testSubject.ConnectionSelectionCaptionText.Should().Be(UiResources.SelectConnectionToBindDescription);
    }

    [TestMethod]
    public void ConnectionSelectionCaptionText_NoConnectionExists_ReturnsNoConnectionExistsLabel()
    {
        testSubject.SelectedConnectionInfo = null;

        testSubject.ConnectionSelectionCaptionText.Should().Be(UiResources.NoConnectionExistsLabel);
    }

    [TestMethod]
    public async Task PerformBindingWithProgressAsync_BindsProjectAndReportsProgress()
    {
        await testSubject.PerformBindingWithProgressAsync(new Manual("any", "any"));

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatusWithData<BindingResult>>>(x =>
                    x.ProgressStatus == UiResources.BindingInProgressText
                    && x.WarningText == UiResources.BindingFailedText
                    && x.AfterProgressUpdated == testSubject.OnProgressUpdated));
    }

    [TestMethod]
    public async Task PerformManualBindingWithProgressAsync_CallsBindingControllerAdapterWithCorrectRequest()
    {
        var connection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupConnectionAndProjectToBind(connection, ServerProject);
        connectedModeBindingServices.BindingControllerAdapter
            .ValidateAndBindAsync(Arg.Is<Manual>(x => x.ProjectKey == ServerProject.Key && x.ConnectionId == connection.Id), connectedModeUIManager, Arg.Any<CancellationToken>())
            .Returns(BindingResult.Success);

        await testSubject.PerformManualBindingWithProgressAsync();
        var taskToPerformParams = (TaskToPerformParams<ResponseStatusWithData<BindingResult>>)progressReporterViewModel.ReceivedCalls().Single().GetArguments()[0];
        await taskToPerformParams.TaskToPerform.Invoke();

        connectedModeBindingServices.BindingControllerAdapter.Received(1).ValidateAndBindAsync(Arg.Is<Manual>(x => x.ProjectKey == ServerProject.Key && x.ConnectionId == connection.Id),
            connectedModeUIManager, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PerformSharedBindingWithProgressAsync_CallsBindingControllerAdapterWithCorrectRequest()
    {
        var bindingConfig = sonarCloudSharedBindingConfigModel;
        testSubject.SharedBindingConfigModel = bindingConfig;
        SetupBoundProject(bindingConfig.CreateConnectionInfo().GetServerConnectionFromConnectionInfo(), new ServerProject(bindingConfig.ProjectKey, default));
        connectedModeBindingServices.BindingControllerAdapter
            .ValidateAndBindAsync(Arg.Is<BindingRequest.Shared>(x => x.Model == bindingConfig), connectedModeUIManager, Arg.Any<CancellationToken>())
            .Returns(BindingResult.Success);

        await testSubject.PerformSharedBindingWithProgressAsync();
        var taskToPerformParams = (TaskToPerformParams<ResponseStatusWithData<BindingResult>>)progressReporterViewModel.ReceivedCalls().Single().GetArguments()[0];
        await taskToPerformParams.TaskToPerform.Invoke();

        connectedModeBindingServices.BindingControllerAdapter.Received(1)
            .ValidateAndBindAsync(Arg.Is<BindingRequest.Shared>(x => x.Model == bindingConfig), connectedModeUIManager, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task PerformBindingAsync_Manual_Succeeds()
    {
        var bindingRequest = new Manual("any", "any");

        await TestSuccessfulBinding(bindingRequest);

        connectedModeServices.TelemetryManager.Received().AddedManualBindings();
    }

    [TestMethod]
    public async Task PerformBindingAsync_Shared_Succeeds()
    {
        var bindingRequest = new BindingRequest.Shared(new SharedBindingConfigModel());

        await TestSuccessfulBinding(bindingRequest);

        connectedModeServices.TelemetryManager.Received().AddedFromSharedBindings();
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public async Task PerformBindingAsync_Assisted_Succeeds(bool isFromShared)
    {
        var bindingRequest = new Assisted(new("any", "any", default, isFromShared));

        await TestSuccessfulBinding(bindingRequest);

        if (isFromShared)
        {
            connectedModeServices.TelemetryManager.Received().AddedFromSharedBindings();
        }
        else
        {
            connectedModeServices.TelemetryManager.Received().AddedAutomaticBindings();
        }
    }

    private async Task TestSuccessfulBinding(BindingRequest bindingRequest)
    {
        var connection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(connection, ServerProject);
        SetUpBindingAdapter(bindingRequest, BindingResult.Success);

        var response = await testSubject.PerformBindingAsync(bindingRequest);

        VerifyBindingAdapterCalled(bindingRequest);
        VerifyBindingSucceeded(response, bindingRequest, ServerProject.Key, connection);
    }

    [DynamicData(nameof(FailedBindingResults))]
    [DataTestMethod]
    public async Task PerformBindingAsync_Fails_ReturnsResult(object resultObject)
    {
        var bindingResult = (BindingResult)resultObject;
        var connection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(connection, ServerProject);
        var bindingRequest = new Manual("any", "any");
        SetUpBindingAdapter(bindingRequest, bindingResult);

        var response = await testSubject.PerformBindingAsync(bindingRequest);

        VerifyBindingNotPerformed(response, bindingResult, bindingRequest);
    }

    [TestMethod]
    public async Task PerformBindingAsync_PostBindingDisplayFailed_ReturnsFailedResult()
    {
        var connection = new ServerConnection.SonarCloud("organization", credentials: validCredentials);
        SetupBoundProject(connection, ServerProject);
        connectedModeServices.SlCoreConnectionAdapter.GetServerProjectByKeyAsync(default, default).ReturnsForAnyArgs(new ResponseStatusWithData<ServerProject>(false, null));
        var bindingRequest = new Manual("any", "any");
        SetUpBindingAdapter(bindingRequest, BindingResult.Success);

        var response = await testSubject.PerformBindingAsync(bindingRequest);

        response.Success.Should().BeFalse();
        response.ResponseData.Should().BeSameAs(BindingResult.Failed);
        VerifyBindingAdapterCalled(bindingRequest);
        VerifyConnectionsRefreshed();
        VerifyBindingTelemetryNotSent();
    }

    [TestMethod]
    public async Task CheckForSharedBindingAsync_WhenSharedBindingExists_SetsSharedBindingConfigModel()
    {
        var sharedBindingModel = new SharedBindingConfigModel();
        sharedBindingConfigProvider.GetSharedBinding().Returns(sharedBindingModel);

        await testSubject.CheckForSharedBindingAsync();

        sharedBindingConfigProvider.Received(1).GetSharedBinding();
        testSubject.SharedBindingConfigModel.Should().Be(sharedBindingModel);
    }

    [TestMethod]
    public async Task CheckForSharedBindingAsync_WhenSharedBindingDoesNotExist_SetsNullSharedBindingConfigModel()
    {
        sharedBindingConfigProvider.GetSharedBinding().ReturnsNull();

        await testSubject.CheckForSharedBindingAsync();

        sharedBindingConfigProvider.Received(1).GetSharedBinding();
        testSubject.SharedBindingConfigModel.Should().Be(null);
    }

    [TestMethod]
    public async Task ExportBindingConfigurationWithProgressAsync_Fails_DelegatesWarningToProgressViewModel()
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatusWithData<string>>>()).Returns(new ResponseStatusWithData<string>(false, null));

        await testSubject.ExportBindingConfigurationWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatusWithData<string>>>(x =>
                    x.ProgressStatus == UiResources.ExportingBindingConfigurationProgressText &&
                    x.WarningText == UiResources.ExportBindingConfigurationWarningText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated));
        messageBox.DidNotReceiveWithAnyArgs().Show(default, default, default, default);
    }

    [TestMethod]
    public async Task ExportBindingConfigurationWithProgressAsync_Success_ShowsMessageAndHasUpToDateState()
    {
        const string filePath = "file path";
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatusWithData<string>>>()).Returns(new ResponseStatusWithData<string>(true, filePath));

        await testSubject.ExportBindingConfigurationWithProgressAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatusWithData<string>>>(x =>
                    x.ProgressStatus == UiResources.ExportingBindingConfigurationProgressText &&
                    x.WarningText == UiResources.ExportBindingConfigurationWarningText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                true);
        messageBox.Received().Show(string.Format(UiResources.ExportBindingConfigurationMessageBoxTextSuccess, filePath),
            UiResources.ExportBindingConfigurationMessageBoxCaptionSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<ResponseStatus>>(x =>
                    x.TaskToPerform == testSubject.CheckForSharedBindingAsync &&
                    x.ProgressStatus == UiResources.CheckingForSharedBindingText &&
                    x.WarningText == UiResources.CheckingForSharedBindingFailedText &&
                    x.AfterProgressUpdated == testSubject.OnProgressUpdated),
                false);
    }

    [DynamicData(nameof(SonarCloudRegions))]
    [DataTestMethod]
    public async Task ExportBindingConfigurationAsync_SonarCloud_Success_SavesBinding(CloudServerRegion region)
    {
        const string serverProjectKey = "SomeServerProject";
        const string organizationKey = "SomeOrganization";
        const string exportedPath = "some exported path";
        SetupConnectionAndProjectToBind(new ServerConnection.SonarCloud(organizationKey, region), new ServerProject(serverProjectKey, "any name"));
        connectedModeBindingServices.SharedBindingConfigProvider.SaveSharedBinding(Arg.Is<SharedBindingConfigModel>(x =>
            x.ProjectKey == serverProjectKey
            && x.Organization == organizationKey
            && x.Region == region.Name
            && x.Uri == region.Url)).Returns(exportedPath);

        var result = await testSubject.ExportBindingConfigurationAsync();

        result.Should().BeEquivalentTo(new ResponseStatusWithData<string>(true, exportedPath));
    }

    [TestMethod]
    public async Task ExportBindingConfigurationAsync_SonarQube_Success_SavesBinding()
    {
        const string serverProjectKey = "SomeServerProject";
        const string exportedPath = "some exported path";
        var serverUri = new Uri("http://anyhost");
        SetupConnectionAndProjectToBind(new ServerConnection.SonarQube(serverUri), new ServerProject(serverProjectKey, "any name"));
        connectedModeBindingServices.SharedBindingConfigProvider.SaveSharedBinding(Arg.Is<SharedBindingConfigModel>(x =>
            x.ProjectKey == serverProjectKey
            && x.Organization == null
            && x.Region == null
            && x.Uri == serverUri)).Returns(exportedPath);

        var result = await testSubject.ExportBindingConfigurationAsync();

        result.Should().BeEquivalentTo(new ResponseStatusWithData<string>(true, exportedPath));
    }

    [TestMethod]
    public async Task ExportBindingConfigurationAsync_SaveFails_ReturnsNonSuccess()
    {
        SetupConnectionAndProjectToBind(new ServerConnection.SonarQube(new Uri("http://anyhost")), new ServerProject("any key", "any name"));
        connectedModeBindingServices.SharedBindingConfigProvider.SaveSharedBinding(Arg.Any<SharedBindingConfigModel>()).Returns(null as string);

        var result = await testSubject.ExportBindingConfigurationAsync();

        result.Should().BeEquivalentTo(new ResponseStatusWithData<string>(false, null));
    }

    private void VerifyBindingTelemetryNotSent()
    {
        connectedModeServices.TelemetryManager.DidNotReceive().AddedAutomaticBindings();
        connectedModeServices.TelemetryManager.DidNotReceive().AddedFromSharedBindings();
        connectedModeServices.TelemetryManager.DidNotReceive().AddedManualBindings();
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
        connectedModeUIServices.MessageBox.Returns(messageBox);

        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        sharedBindingConfigProvider = Substitute.For<ISharedBindingConfigProvider>();
        connectedModeBindingServices.SolutionInfoProvider.Returns(solutionInfoProvider);
        connectedModeBindingServices.SharedBindingConfigProvider.Returns(sharedBindingConfigProvider);

        MockTryGetAllConnectionsInfo([]);
    }

    private void MockTryGetAllConnectionsInfo(List<ConnectionInfo> connectionInfos) =>
        connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnectionsInfo(out _).Returns(callInfo =>
        {
            callInfo[0] = connectionInfos;
            return true;
        });

    private void SetupConnectionAndProjectToBind(ServerConnection selectedServerConnection, ServerProject selectedServerProject)
    {
        SetupBoundProject(selectedServerConnection, selectedServerProject);
        testSubject.SelectedConnectionInfo = ConnectionInfo.From(selectedServerConnection);
        testSubject.SelectedProject = selectedServerProject;
    }

    private void SetupBoundProject(ServerConnection serverConnection, ServerProject expectedServerProject)
    {
        var boundServerProject = new BoundServerProject(ALocalProjectKey, expectedServerProject.Key, serverConnection);
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(new BindingConfiguration(boundServerProject, SonarLintMode.Connected, "binding-dir"));
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
        solutionInfoProvider.GetSolutionNameAsync().Returns(ALocalProjectKey);

        MockGetServerProjectByKey(true, expectedServerProject);
    }

    private void SetupUnboundProject()
    {
        SetupConfigurationProvider(new BindingConfiguration(null, SonarLintMode.Standalone, null));

        MockGetServerProjectByKey(false, null);
    }

    private void SetupConfigurationProvider(BindingConfiguration bindingConfiguration)
    {
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.GetConfiguration().Returns(bindingConfiguration);
        connectedModeServices.ConfigurationProvider.Returns(configurationProvider);
    }

    private void SetupBoundProjectThatDoesNotExistOnServer(ServerConnection serverConnection, string serverProjectKey)
    {
        var boundServerProject = new BoundServerProject(ALocalProjectKey, serverProjectKey, serverConnection);
        SetupConfigurationProvider(new BindingConfiguration(boundServerProject, SonarLintMode.Connected, "binding-dir"));

        MockGetServerProjectByKey(false, null);
    }

    private void MockGetServerProjectByKey(bool success, ServerProject responseData)
    {
        var slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        slCoreConnectionAdapter.GetServerProjectByKeyAsync(Arg.Any<ServerConnection>(), Arg.Any<string>())
            .Returns(Task.FromResult(new ResponseStatusWithData<ServerProject>(success, responseData)));
        connectedModeServices.SlCoreConnectionAdapter.Returns(slCoreConnectionAdapter);
    }

    private async Task InitializeBoundProject()
    {
        SetupBoundProject(new ServerConnection.SonarCloud("my org"), ServerProject);
        await testSubject.DisplayBindStatusAsync();
    }

    public static object[][] FailedBindingResults =>
    [
        [BindingResult.Failed],
        [BindingResult.ConnectionNotFound],
        [BindingResult.CredentialsNotFound],
        [BindingResult.ProjectKeyNotFound],
    ];

    private void VerifyBindingSucceeded(
        ResponseStatusWithData<BindingResult> actualResponse,
        BindingRequest request,
        string expectedProjectKey,
        ServerConnection expectedServerConnection)
    {
        actualResponse.Success.Should().BeTrue();
        actualResponse.ResponseData.Should().BeSameAs(BindingResult.Success);
        connectedModeBindingServices.BindingControllerAdapter.Received(1).ValidateAndBindAsync(request, connectedModeUIManager, Arg.Any<CancellationToken>());
        VerifyConnectionsRefreshed();
        connectedModeServices.SlCoreConnectionAdapter.Received().GetServerProjectByKeyAsync(expectedServerConnection, expectedProjectKey);
    }

    private void VerifyBindingNotPerformed(
        ResponseStatusWithData<BindingResult> response,
        BindingResult expectedResult,
        BindingRequest request)
    {
        response.Success.Should().BeFalse();
        response.ResponseData.Should().Be(expectedResult);
        VerifyBindingAdapterCalled(request);
        VerifyConnectionsRefreshed();
        connectedModeServices.SlCoreConnectionAdapter.DidNotReceiveWithAnyArgs().GetServerProjectByKeyAsync(default, default);
        VerifyBindingTelemetryNotSent();
    }

    private void VerifyConnectionsRefreshed() => serverConnectionsRepositoryAdapter.Received().TryGetAllConnectionsInfo(out Arg.Any<List<ConnectionInfo>>());

    private void SetUpBindingAdapter(BindingRequest bindingRequest, BindingResult bindingResult) =>
        connectedModeBindingServices.BindingControllerAdapter.ValidateAndBindAsync(bindingRequest, connectedModeUIManager, Arg.Any<CancellationToken>())
            .Returns(bindingResult);

    private void VerifyBindingAdapterCalled(BindingRequest bindingRequest) => connectedModeBindingServices.BindingControllerAdapter.Received(1).ValidateAndBindAsync(bindingRequest, connectedModeUIManager, Arg.Any<CancellationToken>());

    private void MockProgressReporter(bool task1Response = true, bool task2Response = true)
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatusWithData<BindingResult>>>(), Arg.Any<bool>())
            .Returns(Task.FromResult(new ResponseStatusWithData<BindingResult>(task1Response, BindingResult.Success)));
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<TaskToPerformParams<ResponseStatus>>(), Arg.Any<bool>())
            .Returns(Task.FromResult(new ResponseStatus(task2Response)));
    }
}
