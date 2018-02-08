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

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStep"/>
    /// </summary>
    internal partial class ProgressControllerStep : IProgressStep
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
