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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// A test implementation of <see cref="IProgressController"/> which is capable of verifying <see cref="IProgressStepExecutionEvents"/>
    /// in addition to executing <see cref="IProgressStepOperation"/> and provide cancellation support
    /// </summary>
    public partial class ConfigurableProgressController : IDisposable
    {
        #region Fields

        private const int DefaultWaitForCompletionMS = 2000;

        private readonly int waitForCompletion;
        private CancellationTokenSource cts;
        public readonly List<Tuple<string, double>> progressChanges = new List<Tuple<string, double>>();
        private readonly List<IProgressStepOperation> stepOperations = new List<IProgressStepOperation>();
        private bool canAbort;

        #endregion Fields

        public ConfigurableProgressController(IServiceProvider serviceProvider, int waitForCompletion = DefaultWaitForCompletionMS)
        {
            this.ServiceProvider = serviceProvider;
            this.waitForCompletion = Debugger.IsAttached ? waitForCompletion * 100 : waitForCompletion;
            this.Reset();
        }

        #region Customization properties

        /// <summary>
        /// An optional delegate to be executed after the controller has started and before the test step is invoked
        /// </summary>
        public Action<IProgressController> ExecuteAfterStarted
        {
            get;
            set;
        }

        public ProgressControllerResult ReturnResult
        {
            get;
            set;
        }

        public Func<bool> TryAbortAction
        {
            get;
            set;
        }

        public ConfigurableProgressEvents Events
        {
            get;
            set;
        }

        public IErrorNotificationManager ErrorNotificationManager
        {
            get;
            private set;
        }

        public IServiceProvider ServiceProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the current step being executed is cancellable
        /// </summary>
        public bool IsCurrentStepCancellable
        {
            get { return this.canAbort; }
        }

        #endregion Customization properties

        #region Configuration and verification methods

        /// <summary>
        /// Resets the configuration
        /// </summary>
        public void Reset()
        {
            this.canAbort = true;
            if (this.cts != null)
            {
                this.cts.Dispose();
            }

            this.cts = new CancellationTokenSource();
            this.progressChanges.Clear();
            this.Events = new ConfigurableProgressEvents();
            this.ErrorNotificationManager = new ErrorNotificationManager();
        }

        /// <summary>
        /// Cancels the execution
        /// </summary>
        public void Cancel()
        {
            this.cts.Cancel();
        }

        /// <summary>
        /// Executes the operation and waits for completion
        /// </summary>
        /// <param name="stepOperation">The operation to execute</param>
        public void Execute(IProgressStepOperation stepOperation)
        {
            this.stepOperations.Clear();
            // Insert a delegate that will be executed after the controller has started and before the actual step operation
            if (this.ExecuteAfterStarted != null)
            {
                this.stepOperations.Add(new ConfigurableProgressTestOperation((c, n) => this.ExecuteAfterStarted(this)));
            }

            this.stepOperations.Add(stepOperation);

            using (ManualResetEventSlim signal = new ManualResetEventSlim())
            {
                ((IProgressController)this).Start().ContinueWith(t => signal.Set());

                signal.Wait(this.waitForCompletion);
            }
        }

        #endregion Configuration and verification methods

        #region IDisposable

        public void Dispose()
        {
            if (this.cts != null)
            {
                this.cts.Dispose();
                this.cts = null;
            }
        }

        #endregion IDisposable
    }
}