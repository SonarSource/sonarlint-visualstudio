/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The <see cref="IProgressStep"/> execution state
    /// </summary>
    public enum StepExecutionState
    {
        /// <summary>
        /// Execution has not been started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Executing an <see cref="IProgressStepOperation"/>
        /// </summary>
        Executing,

        /// <summary>
        /// An error was thrown while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        Failed,

        /// <summary>
        /// The execution was canceled while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Cancelled", Justification = "The preferred term has a typo")]
        Cancelled,

        /// <summary>
        /// No error had occurred while executing <see cref="IProgressStepOperation"/>
        /// </summary>
        Succeeded
    }
}
