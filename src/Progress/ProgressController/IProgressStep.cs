//-----------------------------------------------------------------------
// <copyright file="IProgressStep.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Read-only information about a step which is executed by <see cref="IProgressController"/> 
    /// <seealso cref="ProgressControllerStep"/>
    /// <seealso cref="IProgressStepExecutionEvents"/>
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    public interface IProgressStep
    {
        /// <summary>
        /// Execution state change event
        /// </summary>
        event EventHandler<StepExecutionChangedEventArgs> StateChanged;

        /// <summary>
        /// The display text for a step. Can be null.
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// The progress (0..1) during execution. Can change over time.
        /// <remarks>Can be double.NaN in case the step has no intra step progress reporting ability</remarks>
        /// </summary>
        double Progress { get; }

        /// <summary>
        /// The progress details text during executing. Can change over time. 
        /// </summary>
        string ProgressDetailText { get; }

        /// <summary>
        /// The execution state of the step. Can change over time.
        /// </summary>
        StepExecutionState ExecutionState { get; }

        /// <summary>
        /// Whether the step is supposed to be visible or an internal detail which needs to be hidden
        /// </summary>
        bool Hidden { get; }

        /// <summary>
        /// Whether cancellable. Can change over time.
        /// </summary>
        bool Cancellable { get; }

        /// <summary>
        /// Whether the progress is indeterminate
        /// </summary>
        bool Indeterminate { get; }

        /// <summary>
        /// Whether impacts the progress calculations
        /// </summary>
        bool ImpactsProgress { get; }
    }
}
