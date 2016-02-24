//-----------------------------------------------------------------------
// <copyright file="IProgressStepOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        Task<StepExecutionState> Run(CancellationToken cancellationToken, IProgressStepExecutionEvents progressCallback);
    }
}
