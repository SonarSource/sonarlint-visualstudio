//-----------------------------------------------------------------------
// <copyright file="CancellationSupportChangedEventArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Event arguments for cancellation support changes
    /// </summary>
    public class CancellationSupportChangedEventArgs : ProgressEventArgs
    {
        /// <summary>
        /// Constructs event arguments used to update cancellable state of the controller
        /// </summary>
        /// <param name="cancellable">Latest cancellability state</param>
        public CancellationSupportChangedEventArgs(bool cancellable)
        {
            this.Cancellable = cancellable;
        }

        /// <summary>
        /// The current cancellability state
        /// </summary>
        public bool Cancellable
        {
            get;
            private set;
        }
    }
}
