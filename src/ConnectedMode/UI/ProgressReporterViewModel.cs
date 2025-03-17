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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

public interface IProgressReporterViewModel
{
    string ProgressStatus { get; set; }
    string Warning { get; set; }
    bool IsOperationInProgress { get; }
    bool HasWarning { get; }

    Task<T> ExecuteTaskWithProgressAsync<T>(ITaskToPerformParams<T> parameters, bool clearPreviousState = true) where T : IResponseStatus;
}

public interface IResponseStatus
{
    bool Success { get; }
}

public interface ITaskToPerformParams<T> where T : IResponseStatus
{
    public Action AfterProgressUpdated { get; }
    public Action<T> AfterSuccess { get; }
    public Action<T> AfterFailure { get; }
    public Func<Task<T>> TaskToPerform { get; }
    public string ProgressStatus { get; }
    public string WarningText { get; }
    public T FailureResponse { get; }
}

public class ProgressReporterViewModel(ILogger logger) : ViewModelBase, IProgressReporterViewModel
{
    private string progressStatus;
    private string warning;

    public string ProgressStatus
    {
        get => progressStatus;
        set
        {
            progressStatus = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsOperationInProgress));
        }
    }

    public string Warning
    {
        get => warning;
        set
        {
            warning = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasWarning));
        }
    }

    public bool IsOperationInProgress => !string.IsNullOrEmpty(ProgressStatus);
    public bool HasWarning => !string.IsNullOrEmpty(Warning);

    public async Task<T> ExecuteTaskWithProgressAsync<T>(ITaskToPerformParams<T> parameters, bool clearPreviousState = true) where T : IResponseStatus
    {
        try
        {
            if (clearPreviousState)
            {
                Warning = null;
            }
            UpdateProgress(parameters.ProgressStatus, parameters);
            var response = await parameters.TaskToPerform();

            if (response.Success)
            {
                parameters.AfterSuccess?.Invoke(response);
            }
            else
            {
                OnFailure(parameters, response, clearPreviousState);
            }

            return response;
        }
        catch (Exception ex)
        {
            var response = parameters.FailureResponse;
            logger.WriteLine(ex.ToString());
            OnFailure(parameters, response, clearPreviousState);
            return response;
        }
        finally
        {
            UpdateProgress(null, parameters);
        }
    }

    private void UpdateProgress<T>(string status, ITaskToPerformParams<T> parameters) where T : IResponseStatus
    {
        ProgressStatus = status;
        parameters.AfterProgressUpdated?.Invoke();
    }

    private void OnFailure<T>(ITaskToPerformParams<T> parameters, T response, bool clearPreviousState) where T : IResponseStatus
    {
        UpdateWarning(parameters.WarningText, clearPreviousState);
        parameters.AfterFailure?.Invoke(response);
    }

    private void UpdateWarning(string warningText, bool clearPreviousState)
    {
        if (clearPreviousState)
        {
            Warning = warningText;
        }
        else
        {
            Warning += warningText;
        }
    }
}

public class TaskToPerformParams<T>(Func<Task<T>> taskToPerform, string progressStatus, string warningText) : ITaskToPerformParams<T>
    where T : IResponseStatus, new()
{
    public Action AfterProgressUpdated { get; init; }
    public Action<T> AfterSuccess { get; init; }
    public Action<T> AfterFailure { get; init; }
    public Func<Task<T>> TaskToPerform { get; } = taskToPerform;
    public string ProgressStatus { get; } = progressStatus;
    public string WarningText { get; } = warningText;
    public T FailureResponse { get; } = new T();
}
