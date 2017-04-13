/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using SonarLint.VisualStudio.Progress.Threading;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressEvents"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
    "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"",
        Justification = "cancellationTokenSource is being disposed OnFinish whish is guaranteed (tested) to be called in the end",
        Scope = "type",
        Target = "~T:SonarLint.VisualStudio.Progress.Controller.SequentialProgressController")]
    public partial class SequentialProgressController : IProgressEvents
    {
        /* Dev notes: Events are raised on UI thread, the reason is for simpler management of the order in which could be processed
           using an observer. Raising them on the UI thread means that they will be processed in the same order as raised and will be processed
           one by one and not several at once. Removing this restriction will require some kind of queuing mechanism which is an overkill since in practice
           the main consumer of those events are UI-based observers which will need to report those changes and action them on the UI thread anyway.*/

        public event EventHandler<ProgressEventArgs> Started
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StartedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StartedPrivate -= value;
            }
        }

        public event EventHandler<ProgressControllerFinishedEventArgs> Finished
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.FinishedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.FinishedPrivate -= value;
            }
        }

        public event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StepExecutionChangedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.StepExecutionChangedPrivate -= value;
            }
        }

        public event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged
        {
            add
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.CancellationSupportChangedPrivate += value;
            }

            remove
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this.CancellationSupportChangedPrivate -= value;
            }
        }

        private event EventHandler<ProgressEventArgs> StartedPrivate;

        private event EventHandler<ProgressControllerFinishedEventArgs> FinishedPrivate;

        private event EventHandler<StepExecutionChangedEventArgs> StepExecutionChangedPrivate;

        private event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChangedPrivate;

        public IEnumerable<IProgressStep> Steps
        {
            get { return this.progressStepOperations.Select(s => s.Step); }
        }

        private void OnStarted()
        {
            this.IsStarted = true;
            this.ThreadSafeCreateCancellationTokenSource();

            VsThreadingHelper.RunInline(this, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                ConfigureStepEventListeners(true);

                var delegates = this.StartedPrivate;
                if (delegates != null)
                {
                    ProgressEventArgs args = new ProgressEventArgs();
                    delegates(this, args);
                    // Verify that the observer handled it since now easy way of testing
                    // serialized raising and handling of the event across the classes
                    args.CheckHandled();
                }
            });
        }

        private void OnFinished(ProgressControllerResult result)
        {
            this.IsFinished = true;
            this.ThreadSafeDisposeCancellationTokenSource();

            VsThreadingHelper.RunInline(this, VsTaskRunContext.UIThreadNormalPriority, () =>
            {
                ConfigureStepEventListeners(false);

                var delegates = this.FinishedPrivate;
                if (delegates != null)
                {
                    ProgressControllerFinishedEventArgs args = new ProgressControllerFinishedEventArgs(result);
                    delegates(this, args);
                    // Verify that the observer handled it since now easy way of testing
                    // serialized raising and handling of the event across the classes
                    args.CheckHandled();
                }
            });
        }

        private void OnCancellableChanged(bool cancellable)
        {
            if (this.CancellationSupportChangedPrivate != null)
            {
                VsThreadingHelper.RunInline(this, VsTaskRunContext.UIThreadNormalPriority,
                    () =>
                    {
                        var delegates = this.CancellationSupportChangedPrivate;
                        if (delegates != null)
                        {
                            CancellationSupportChangedEventArgs args = new CancellationSupportChangedEventArgs(cancellable);
                            delegates(this, args);
                            // Verify that the observer handled it since now easy way of testing
                            // serialized raising and handling of the event across the classes
                            args.CheckHandled();
                        }
                    });
            }
        }

        private void OnStepStateChanged(object sender, StepExecutionChangedEventArgs args)
        {
            if (this.StepExecutionChangedPrivate != null)
            {
                VsThreadingHelper.RunInline(this, VsTaskRunContext.UIThreadNormalPriority,
                    () =>
                    {
                        var delegates = this.StepExecutionChangedPrivate;
                        if (delegates != null)
                        {
                            delegates(sender, args);
                            // Verify that the observer handled it since now easy way of testing
                            // serialized raising and handling of the event across the classes
                            args.CheckHandled();
                        }
                    });
            }
        }
    }
}
