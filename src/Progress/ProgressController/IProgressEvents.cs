/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
        /// Changes in <see cref="IProgressController"/> cancellability
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
