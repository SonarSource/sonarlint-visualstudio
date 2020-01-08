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

using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface represents the actual operation that needed to be executed in a <see cref="IProgressStep"/>
    /// <seealso cref="ProgressControllerStep"/>
    /// </summary>
    public interface IProgressStepOperation
    {
        /// <summary>
        /// The <see cref="IProgressStep"/> associated with this operation
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Step", Justification = "NA")]
        IProgressStep Step { get; }

        /// <summary>
        /// Execute the operation
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progressCallback">Allows to update the <see cref="IProgressController"/> with the execution progress and cancellation support. <seealso cref="IProgressEvents"/></param>
        /// <returns>An awaitable task that returns a <see cref="StepExecutionState"/> result</returns>
        Task<StepExecutionState> RunAsync(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback);
    }
}
