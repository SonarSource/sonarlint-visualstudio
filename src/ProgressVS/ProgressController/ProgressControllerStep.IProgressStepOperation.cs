/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Progress.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStepOperation"/>
    /// </summary>
    public partial class ProgressControllerStep : IProgressStepOperation
    {
        IProgressStep IProgressStepOperation.Step
        {
            get
            {
                return this;
            }
        }

        async Task<StepExecutionState> IProgressStepOperation.RunAsync(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback)
        {
            if (this.ExecutionState != StepExecutionState.NotStarted)
            {
                throw new InvalidOperationException(ProgressResources.StepOperationWasAlreadyExecuted);
            }

            if (this.Cancellable && cancellationToken.IsCancellationRequested)
            {
                return this.ExecutionState = StepExecutionState.Cancelled;
            }

            VsTaskRunContext context = GetContext(this.Execution);

            StepExecutionState stepState = await VsThreadingHelper.RunTaskAsync<StepExecutionState>(this.controller, context,
                () =>
                {
                    DoStatefulExecution(progressCallback, cancellationToken);
                    return this.ExecutionState;
                },

                cancellationToken);

            return stepState;
        }
    }
}
