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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface is used to notify <see cref="IProgressController"/> of <see cref="IProgressStep"/> changes during execution
    /// <seealso cref="IProgressStepOperation"/>
    /// </summary>
    internal interface IProgressStepExecutionEvents
    {
        /// <summary>
        /// Progress change notification
        /// </summary>
        /// <param name="progressDetailText">Optional (can be null)</param>
        /// <param name="progress">The execution progress</param>
        void ProgressChanged(string progressDetailText, double progress = double.NaN);
    }
}
