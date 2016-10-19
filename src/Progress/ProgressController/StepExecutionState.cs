//-----------------------------------------------------------------------
// <copyright file="StepExecutionState.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
