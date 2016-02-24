//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

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

        private int waitForCompletion;
        private CancellationTokenSource cts;
        private List<Tuple<string, double>> progressChanges = new List<Tuple<string, double>>();
        private List<IProgressStepOperation> stepOperations = new List<IProgressStepOperation>();
        private bool canAbort;
        #endregion

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
        #endregion

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

        public void AssertProgressChangeEvents(List<Tuple<string, double>> expectedOrderedProgressEvents)
        {
            Assert.AreEqual(expectedOrderedProgressEvents.Count, this.progressChanges.Count, "Unexpected number of execution change events");
            for (int i = 0; i < expectedOrderedProgressEvents.Count; i++)
            {
                Assert.AreEqual(expectedOrderedProgressEvents[i], this.progressChanges[i], "Unexpected change event");
            }
        }

        public void AssertNoProgressChangeEvents()
        {
            Assert.AreEqual(0, this.progressChanges.Count, "Not expecting any change events");
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (this.cts != null)
            {
                this.cts.Dispose();
                this.cts = null;
            }
        }
        #endregion
    }
}
