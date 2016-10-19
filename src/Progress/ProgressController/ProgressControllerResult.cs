//-----------------------------------------------------------------------
// <copyright file="ProgressControllerResult.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// The execution results for a <see cref="IProgressController"/>
    /// </summary>
    public enum ProgressControllerResult
    {
        /// <summary>
        /// Execution was canceled
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Canceled", Justification = "The preferred term has a typo")]
        Cancelled,

        /// <summary>
        /// Execution succeeded
        /// </summary>
        Succeeded,

        /// <summary>
        /// Execution failed
        /// </summary>
        Failed
    }
}
