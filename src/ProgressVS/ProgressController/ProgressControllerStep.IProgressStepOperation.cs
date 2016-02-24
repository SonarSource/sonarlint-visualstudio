//-----------------------------------------------------------------------
// <copyright file="ProgressControllerStep.IProgressStepOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        async Task<StepExecutionState> IProgressStepOperation.Run(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback)
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

            StepExecutionState stepState = await VsThreadingHelper.RunTask<StepExecutionState>(this.controller, context,
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
