/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
