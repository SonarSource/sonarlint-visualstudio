//-----------------------------------------------------------------------
// <copyright file="IProgressStepExecutionEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The interface is used to notify <see cref="IProgressController"/> of <see cref="IProgressStep"/> changes during execution
    /// <seealso cref="IProgressStepOperation"/>
    /// </summary>
    public interface IProgressStepExecutionEvents
    {
        /// <summary>
        /// Progress change notification
        /// </summary>
        /// <param name="progressDetailText">Optional (can be null)</param>
        /// <param name="progress">The execution progress</param>
        void ProgressChanged(string progressDetailText, double progress = double.NaN);
    }
}
