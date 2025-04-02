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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class ProgressReporterViewModelTests
{
    private ILogger logger;
    private ProgressReporterViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        testSubject = new ProgressReporterViewModel(logger);
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
        var taskResponse = new ResponseStatusWithData<IResponseStatus>(true, null);
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
    public async Task ExecuteTaskWithProgressAsync_TaskWithFailureResponseAndCustomWarning_WorkflowIsCorrect()
    {
        var warningText = "warning";
        var taskWarningText = "warning 2";
        var parameters = GetTaskWithResponse(false, warningText, taskWarningText);
        parameters.WarningText.Returns(warningText);

        await testSubject.ExecuteTaskWithProgressAsync(parameters);

        Received.InOrder(() =>
        {
            _ = parameters.ProgressStatus;
            parameters.AfterProgressUpdated();
            parameters.TaskToPerform();
            parameters.AfterFailure(Arg.Any<IResponseStatus>());
            parameters.AfterProgressUpdated();
        });
        testSubject.Warning.Should().Be(taskWarningText);
        testSubject.ProgressStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskThrowsException_SetsProgressToNull()
    {
        var warningText = "warning";
        var parameters = Substitute.For<ITaskToPerformParams<IResponseStatus>>();
        testSubject.ProgressStatus = "In progress...";
        parameters.WarningText.Returns(warningText);

        var response = await ExecuteTaskThatThrows(parameters);

        response.Success.Should().BeFalse();
        parameters.Received(1).AfterFailure(Arg.Any<IResponseStatus>());
        testSubject.Warning.Should().Be(warningText);
        testSubject.ProgressStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TaskThrowsException_CatchesErrorAndLogs()
    {
        var act = async () => await ExecuteTaskThatThrows();

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task ExecuteTaskWithProgressAsync_TwoTasksThatDoNotClearPreviousResponse_OneOfTheTaskFails_ShowsWarning(bool task1Response, bool task2Response)
    {
        var task1Warning = "task1 failed";
        var task2Warning = "task2 failed";
        var parameters1 = GetTaskWithResponse(task1Response, task1Warning);
        var parameters2 = GetTaskWithResponse(task2Response, task2Warning);

        await testSubject.ExecuteTaskWithProgressAsync(parameters1, clearPreviousState: false);
        await testSubject.ExecuteTaskWithProgressAsync(parameters2, clearPreviousState: false);

        testSubject.Warning.Trim().Should().Be(task1Response ? task2Warning : task1Warning);
        testSubject.ProgressStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TwoTasksThatDoNotClearPreviousResponse_BothTasksFail_ShowsWarning()
    {
        var task1Warning = "task1 failed";
        var task2Warning = "task2 failed";
        var parameters1 = GetTaskWithResponse(false, task1Warning);
        var parameters2 = GetTaskWithResponse(false, task2Warning);

        await testSubject.ExecuteTaskWithProgressAsync(parameters1, clearPreviousState: false);
        await testSubject.ExecuteTaskWithProgressAsync(parameters2, clearPreviousState: false);

        testSubject.Warning.Should().Be($"{task1Warning}{task2Warning}");
        testSubject.ProgressStatus.Should().BeNull();
    }

    [TestMethod]
    public async Task ExecuteTaskWithProgressAsync_TwoTasksThatDoNotClearPreviousResponse_BothTasksSucceed_HasCorrectState()
    {
        var parameters1 = GetTaskWithResponse(true, "task1 failed");
        var parameters2 = GetTaskWithResponse(true, "task2 failed");

        await testSubject.ExecuteTaskWithProgressAsync(parameters1, clearPreviousState: false);
        await testSubject.ExecuteTaskWithProgressAsync(parameters2, clearPreviousState: false);

        testSubject.Warning.Should().BeNull();
        testSubject.ProgressStatus.Should().BeNull();
    }

    private static ITaskToPerformParams<IResponseStatus> GetTaskWithResponse(bool success, string warningText = null, string responseWarningText = null)
    {
        var parameters = Substitute.For<ITaskToPerformParams<IResponseStatus>>();
        var taskResponse = Substitute.For<IResponseStatus>();
        parameters.TaskToPerform().Returns(taskResponse);
        parameters.WarningText.Returns(warningText);
        taskResponse.Success.Returns(success);
        taskResponse.WarningText.Returns(responseWarningText);

        return parameters;
    }

    private async Task<IResponseStatus> ExecuteTaskThatThrows(ITaskToPerformParams<IResponseStatus> parameters = null)
    {
        parameters ??= Substitute.For<ITaskToPerformParams<IResponseStatus>>();
        parameters.TaskToPerform.Returns(x => throw new Exception("test"));
        parameters.FailureResponse.WarningText.Returns(null as string);

        return await testSubject.ExecuteTaskWithProgressAsync(parameters);
    }
}
