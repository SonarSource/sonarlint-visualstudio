//-----------------------------------------------------------------------
// <copyright file="ProgressControllerStep.IProgressStep.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStep"/>
    /// </summary>
    public partial class ProgressControllerStep : IProgressStep
    {
        /// <summary>
        /// Step execution change event
        /// </summary>
        public event EventHandler<StepExecutionChangedEventArgs> StateChanged
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StateChangedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StateChangedPrivate -= value;
            }
        }

        public string DisplayText
        {
            get;
            private set;
        }

        public double Progress
        {
            get;
            private set;
        }

        public string ProgressDetailText
        {
            get;
            private set;
        }

        public StepExecutionState ExecutionState
        {
            get
            {
                return this.state;
            }

            protected set
            {
                Debug.Assert(this.state != value, "Unexpected transition to the same state");
                if (this.state != value)
                {
                    this.state = value;
                    this.OnExecutionStateChanged();
                }
            }
        }

        public bool Hidden
        {
            get;
            private set;
        }

        public bool Indeterminate
        {
            get;
            private set;
        }

        public bool Cancellable
        {
            get;
            private set;
        }

        public bool ImpactsProgress
        {
            get;
            private set;
        }
    }
}
