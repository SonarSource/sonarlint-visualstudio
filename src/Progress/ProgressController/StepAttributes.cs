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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Step attribute flags
    /// <seealso cref="ProgressStepDefinition"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "None represents zero in bits")]
    [Flags]
    public enum StepAttributes
    {
        /// <summary>
        /// Cancellable, Visible, Determinate, Foreground thread and impacting on the overall progress
        /// </summary>
        None = 0,

        /// <summary>
        /// Background thread flag
        /// </summary>
        BackgroundThread = 1,

        /// <summary>
        /// The step is not cancellable
        /// </summary>
        /// <seealso cref="IProgressStep.Cancellable"/>
        NonCancellable = 2,

        /// <summary>
        /// Hidden flag
        /// <seealso cref="IProgressStep.Hidden"/>
        /// </summary>
        Hidden = 4,

        /// <summary>
        /// Indeterminate progress flag
        /// </summary>
        /// <seealso cref="IProgressStep.Indeterminate"/>
        Indeterminate = 8,

        /// <summary>
        /// Does not impact the overall progress
        /// </summary>
        /// <seealso cref="IProgressStep.ImpactsProgress"/>
        NoProgressImpact = 16
    }
}
