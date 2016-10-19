//-----------------------------------------------------------------------
// <copyright file="StepExecution.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Specifies on which thread to execute the <see cref="IProgressStepOperation"/>
    /// </summary>
    public enum StepExecution
    {
        /// <summary>
        /// UI thread
        /// </summary>
        ForegroundThread,

        /// <summary>
        /// Non-UI thread
        /// </summary>
        BackgroundThread
    }
}
