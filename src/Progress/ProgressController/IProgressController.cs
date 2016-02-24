//-----------------------------------------------------------------------
// <copyright file="IProgressController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface represents a progress controller that can execute steps
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2302:FlagServiceProviders", Justification = "Service provider for future use and only forwarding by the current APIs")]
    public interface IProgressController : IServiceProvider
    {
        /// <summary>
        /// Returns <see cref="IProgressController"/> events which are exposed via <see cref="IProgressEvents"/>
        /// </summary>
        IProgressEvents Events { get; }

        /// <summary>
        /// Returns <see cref="IErrorNotificationManager"/> used by the controller
        /// </summary>
        IErrorNotificationManager ErrorNotificationManager { get; }

        /// <summary>
        /// Initializes a controller with fixed set of steps. The set of steps cannot be changed once it has been set.
        /// </summary>
        /// <param name="stepFactory">An instance of <see cref="IProgressStepFactory"/>. Required.</param>
        /// <param name="stepsDefinition">Set of <see cref="IProgressStepDefinition"/> to construct the steps from</param>
        void Initialize(IProgressStepFactory stepFactory, IEnumerable<IProgressStepDefinition> stepsDefinition);

        /// <summary>
        /// Starts execution
        /// </summary>
        /// <returns>An await-able object with a <see cref="ProgressControllerResult"/> result</returns>
        Task<ProgressControllerResult> Start();

        /// <summary>
        /// Tries to abort the current execution
        /// </summary>
        /// <returns>Whether was able to abort or not</returns>
        bool TryAbort();
    }
}
