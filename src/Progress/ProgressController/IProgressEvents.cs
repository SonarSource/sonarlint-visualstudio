//-----------------------------------------------------------------------
// <copyright file="IProgressEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface is used for notification of <see cref="IProgressController"/> progress and state
    /// </summary>
    /// <remarks>
    /// Registering/Unregistering to the events needs to be done on UIThread.
    /// All the events will be raised on the UI thread</remarks>
    public interface IProgressEvents
    {
        /// <summary>
        /// <see cref="IProgressController"/> started to execute. Always raised if the <see cref="IProgressController"/> was started.
        /// </summary>
        event EventHandler<ProgressEventArgs> Started;

        /// <summary>
        /// <see cref="IProgressController"/> finished to execute. Always raised if the <see cref="IProgressController"/> was started.
        /// </summary>
        event EventHandler<ProgressControllerFinishedEventArgs> Finished;

        /// <summary>
        /// Changes in <see cref="IProgressController"/> execution of <see cref="IProgressStep"/>
        /// </summary>
        event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged;

        /// <summary>
        /// Changes in <see cref="IProgressController"/> cancelability
        /// </summary>
        event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged;

        /// <summary>
        /// The steps associated with the <see cref="IProgressController"/>. 
        /// May be null until the <see cref="IProgressController"/> is initialized.
        /// The set of steps cannot be changed once initialized.
        /// </summary>
        IEnumerable<IProgressStep> Steps { get; }
    }
}
