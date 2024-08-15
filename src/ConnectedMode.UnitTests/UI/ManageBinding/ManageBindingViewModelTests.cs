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

using System.ComponentModel;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageBinding;

[TestClass]
public class ManageBindingViewModelTests
{
    private ManageBindingViewModel testSubject;
    private readonly SolutionInfoModel solutionInfoModel = new("VS Sample 2022", SolutionType.Solution);
    private readonly ServerProject serverProject = new ("a-project", "A Project");
    private readonly Connection SonarQubeConnection = new ("http://localhost:9000", ServerType.SonarQube, true);
    private readonly Connection SonarCloudConnection = new ("http://sonarcloud.io", ServerType.SonarCloud, true);

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ManageBindingViewModel(solutionInfoModel);
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
        testSubject.SelectedConnection = SonarQubeConnection;

        testSubject.IsConnectionSelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsConnectionSelected_ProjectIsNotSelected_ReturnsFalse()
    {
        testSubject.SelectedConnection = null;

        testSubject.IsConnectionSelected.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedConnection_NewConnectionIsSet_ClearsSelectedProject()
    {
        testSubject.SelectedConnection = SonarQubeConnection;
        testSubject.SelectedProject = serverProject;

        testSubject.SelectedConnection = SonarCloudConnection;

        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void SelectedConnection_SameConnectionIsSet_DoesNotClearSelectedProject()
    {
        testSubject.SelectedConnection = SonarQubeConnection;
        testSubject.SelectedProject = serverProject;

        testSubject.SelectedConnection = SonarQubeConnection;

        testSubject.SelectedProject.Should().Be(serverProject);
    }

    [TestMethod]
    public void SelectedConnection_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedConnection = SonarQubeConnection;

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedConnection)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsConnectionSelected)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSelectProjectButtonEnabled)));
    }

    [TestMethod]
    public async Task Bind_SetsBoundProjectToSelectedProject()
    {
        testSubject.BoundProject = null;
        testSubject.SelectedProject = serverProject;

        await testSubject.BindAsync();

        testSubject.BoundProject.Should().Be(testSubject.SelectedProject);
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
        testSubject.SelectedConnection = SonarQubeConnection;
        testSubject.SelectedProject = serverProject;

        testSubject.Unbind();

        testSubject.SelectedConnection.Should().BeNull();
        testSubject.SelectedProject.Should().BeNull();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.SelectedProject = serverProject;
        testSubject.ProgressStatus = null;

        testSubject.IsBindButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsBindButtonEnabled_ProjectIsSelectedAndBindingIsInProgress_ReturnsFalse()
    {
        testSubject.SelectedProject = serverProject;
        testSubject.ProgressStatus = "in progress";

        testSubject.IsBindButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("binding...")]
    [DataRow(null)]
    public void IsBindButtonEnabled_ProjectIsNotSelected_ReturnsFalse(string bindingStatus)
    {
        testSubject.SelectedProject = null;
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsBindButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("binding...", false)]
    [DataRow(null, true)]
    public void IsManageConnectionsButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(string bindingStatus, bool expectedResult)
    {
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsManageConnectionsButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow("binding...", false)]
    [DataRow(null, true)]
    public void IsUseSharedBindingButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(string bindingStatus, bool expectedResult)
    {
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsUseSharedBindingButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow("binding...", false)]
    [DataRow(null, true)]
    public void IsUnbindButtonEnabled_ReturnsTrueOnlyWhenNoBindingIsInProgress(string bindingStatus, bool expectedResult)
    {
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsUnbindButtonEnabled.Should().Be(expectedResult);
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_ConnectionIsSelectedAndNoBindingIsInProgressAndProjectIsNotBound_ReturnsTrue()
    {
        testSubject.BoundProject = null;
        testSubject.ProgressStatus = null;
        testSubject.SelectedConnection = SonarQubeConnection;

        testSubject.IsSelectProjectButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        testSubject.ProgressStatus = "in progress";
        testSubject.SelectedConnection = SonarQubeConnection;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("binding...")]
    [DataRow(null)]
    public void IsSelectProjectButtonEnabled_ConnectionIsNotSelected_ReturnsFalse(string bindingStatus)
    {
        testSubject.ProgressStatus = bindingStatus;
        testSubject.SelectedConnection = null;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSelectProjectButtonEnabled_ProjectIsAlreadyBound_ReturnsFalse()
    {
        testSubject.BoundProject = serverProject;
        testSubject.ProgressStatus = null;
        testSubject.SelectedConnection = SonarQubeConnection;

        testSubject.IsSelectProjectButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = null;
        testSubject.ProgressStatus = "in progress";

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("binding...")]
    public void IsConnectionSelectionEnabled_ProjectIsBound_ReturnsFalse(string bindingStatus)
    {
        testSubject.BoundProject = serverProject;
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsConnectionSelectionEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsConnectionSelectionEnabled_ProjectIsNotBoundAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.BoundProject = null;
        testSubject.ProgressStatus = null;

        testSubject.IsConnectionSelectionEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void ProgressStatus_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.ProgressStatus = "In progress...";

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.ProgressStatus)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsOperationInProgress)));
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
    public void IsOperationInProgress_ProgressStatusIsSet_ReturnsTrue()
    {
        testSubject.ProgressStatus = "In progres...";

        testSubject.IsOperationInProgress.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void IsOperationInProgress_ProgressStatusIsNull_ReturnsFalse(string status)
    {
        testSubject.ProgressStatus = status;

        testSubject.IsOperationInProgress.Should().BeFalse();
    }

    [TestMethod]
    public void IsExportButtonEnabled_BindingIsInProgress_ReturnsFalse()
    {
        testSubject.BoundProject = serverProject;
        testSubject.ProgressStatus = "in progress";

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("binding...")]
    public void IsExportButtonEnabled_ProjectIsNotBound_ReturnsFalse(string bindingStatus)
    {
        testSubject.BoundProject = null;
        testSubject.ProgressStatus = bindingStatus;

        testSubject.IsExportButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsExportButtonEnabled_ProjectIsBoundAndBindingIsNotInProgress_ReturnsTrue()
    {
        testSubject.BoundProject = serverProject;
        testSubject.ProgressStatus = null;

        testSubject.IsExportButtonEnabled.Should().BeTrue();
    }
}
