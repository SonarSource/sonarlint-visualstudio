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

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class ProgressReporterViewModelTests
{
    private ProgressReporterViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ProgressReporterViewModel();
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
    }

    [TestMethod]
    public void IsOperationInProgress_ProgressStatusIsSet_ReturnsTrue()
    {
        testSubject.ProgressStatus = "In progress...";

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
    public void Warning_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.Warning = "Process failed";

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Warning)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.HasWarning)));
    }

    [TestMethod]
    public void HasWarning_WarningIsSet_ReturnsTrue()
    {
        testSubject.Warning = "Process failed";

        testSubject.HasWarning.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void HasWarning_WarningsIsNull_ReturnsFalse(string warning)
    {
        testSubject.Warning = warning;

        testSubject.HasWarning.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExecuteTaskWithProgressAsync_ReturnsReceivedResponse(bool success)
    {
        var parameters = GetTaskWithResponse(success);
        var taskResponse = new AdapterResponseWithData<IResponseStatus>(true, null);
        parameters.TaskToPerform().Returns(taskResponse);

        var response = await testSubject.ExecuteTaskWithProgressAsync(parameters);

        response.Should().Be(taskResponse);
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskWithSuccessResponse_WorkflowIsCorrect()
    {
        var parameters = GetTaskWithResponse(true);

        await testSubject.ExecuteTaskWithProgressAsync(parameters);

        Received.InOrder(() =>
        {
            _ = parameters.ProgressStatus;
            parameters.AfterProgressUpdated();
            parameters.TaskToPerform();
            parameters.AfterSuccess(Arg.Any<IResponseStatus>());
            parameters.AfterProgressUpdated();
        });
        testSubject.ProgressStatus.Should().BeNull();
        _ = parameters.DidNotReceive().WarningText;
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskWithSuccessResponse_ClearsPreviousWarning()
    {
        var parameters = GetTaskWithResponse(true);
        testSubject.Warning = "warning";

        await testSubject.ExecuteTaskWithProgressAsync(parameters);

        testSubject.Warning.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskWithFailureResponse_WorkflowIsCorrect()
    {
        var warningText = "warning";
        var parameters = GetTaskWithResponse(false);
        parameters.WarningText.Returns(warningText);

        await testSubject.ExecuteTaskWithProgressAsync(parameters);

        Received.InOrder(() =>
        {
            _ = parameters.ProgressStatus;
            parameters.AfterProgressUpdated();
            parameters.TaskToPerform();
            _ = parameters.WarningText;
            parameters.AfterFailure(Arg.Any<IResponseStatus>());
            parameters.AfterProgressUpdated();
        });
        testSubject.Warning.Should().Be(warningText);
        testSubject.ProgressStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskThrowsException_SetsProgressToNull()
    {
        testSubject.ProgressStatus = "In progress...";

        await ExecuteTaskThatThrows();

        testSubject.ProgressStatus.Should().BeNull();
    }

    private static ITaskToPerformParams<IResponseStatus> GetTaskWithResponse(bool success)
    {
        var parameters = Substitute.For<ITaskToPerformParams<IResponseStatus>>();
        var taskResponse = Substitute.For<IResponseStatus>();
        parameters.TaskToPerform().Returns(taskResponse);
        taskResponse.Success.Returns(success);

        return parameters;
    }

    private async Task ExecuteTaskThatThrows()
    {
        var parameters = Substitute.For<ITaskToPerformParams<IResponseStatus>>();
        parameters.TaskToPerform.Returns(x => throw new Exception("test"));

        try
        {
            await testSubject.ExecuteTaskWithProgressAsync(parameters);
        }
        catch (Exception)
        {
           // this is for testing only
        }
    }
}
