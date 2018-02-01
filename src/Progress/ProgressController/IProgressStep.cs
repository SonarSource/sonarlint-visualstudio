/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Read-only information about a step which is executed by <see cref="IProgressController"/>
    /// <seealso cref="ProgressControllerStep"/>
    /// <seealso cref="IProgressStepExecutionEvents"/>
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    internal interface IProgressStep
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
