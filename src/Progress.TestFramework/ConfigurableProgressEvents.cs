//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressEvents"/>
    /// </summary>
    public class ConfigurableProgressEvents : IProgressEvents
    {
        public ConfigurableProgressEvents()
        {
            this.Steps = new IProgressStep[0];
        }

        #region Test implementation of IProgressEvents
        public event EventHandler<ProgressEventArgs> Started;

        public event EventHandler<ProgressControllerFinishedEventArgs> Finished;

        public event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged;

        public event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged;

        public IEnumerable<IProgressStep> Steps
        {
            get;
            set;
        }
        #endregion

        #region Simulation
        public void InvokeStarted()
        {
            if (this.Started != null)
            {
                this.Started(this, new ProgressEventArgs());
            }
        }

        public void InvokeFinished(ProgressControllerResult result)
        {
            if (this.Finished != null)
            {
                this.Finished(this, new ProgressControllerFinishedEventArgs(result));
            }
        }

        public void InvokeStepExecutionChanged(StepExecutionChangedEventArgs args)
        {
            if (this.StepExecutionChanged != null)
            {
                this.StepExecutionChanged(this, args);
            }
        }

        public void InvokeCancellationSupportChanged(bool cancellable)
        {
            if (this.CancellationSupportChanged != null)
            {
                this.CancellationSupportChanged(this, new CancellationSupportChangedEventArgs(cancellable));
            }
        }
        #endregion

        #region Verification
        public void AssertAllEventsAreRegistered()
        {
            Assert.IsNotNull(this.Started, "The Started event isn't registered");
            Assert.IsNotNull(this.Finished, "The Finished event wasn't registered");
            Assert.IsNotNull(this.StepExecutionChanged, "The StepExecutionChanged event isn't registered");
            Assert.IsNotNull(this.CancellationSupportChanged, "The CancellationSupportChanged event isn't registered");
        }

        public void AssertAllEventsAreUnregistered()
        {
            Assert.IsNull(this.Started, "The Started event is registered");
            Assert.IsNull(this.Finished, "The Finished event is registered");
            Assert.IsNull(this.StepExecutionChanged, "The StepExecutionChanged event is registered");
            Assert.IsNull(this.CancellationSupportChanged, "The CancellationSupportChanged event is registered");
        }
        #endregion
    }
}
