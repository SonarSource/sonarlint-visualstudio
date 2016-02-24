//-----------------------------------------------------------------------
// <copyright file="ProgressControllerFinishedEventArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Event argument for <see cref="IProgressController"/> execution completion
    /// <seealso cref="IProgressEvents"/>
    /// </summary>
    public class ProgressControllerFinishedEventArgs : ProgressEventArgs
    {
        public ProgressControllerFinishedEventArgs(ProgressControllerResult result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Execution result
        /// </summary>
        public ProgressControllerResult Result
        {
            get;
            private set;
        }
    }
}
