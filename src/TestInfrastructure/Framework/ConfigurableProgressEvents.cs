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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressEvents : IProgressEvents
    {
        #region IProgressEvents
        public IEnumerable<IProgressStep> Steps
        {
            get;
            set;
        }

#pragma warning disable 67
        public event EventHandler<CancellationSupportChangedEventArgs> CancellationSupportChanged;
        public event EventHandler<ProgressControllerFinishedEventArgs> Finished;
        public event EventHandler<ProgressEventArgs> Started;
        public event EventHandler<StepExecutionChangedEventArgs> StepExecutionChanged;
#pragma warning restore 67
        #endregion

        #region Test helpers
        public void AssertNoFinishedEventHandlers()
        {
            Assert.IsNull(this.Finished, "Not expecting any handler for Finished event");
        }

        public void SimulateFinished(ProgressControllerResult result)
        {
            this.Finished?.Invoke(this, new ProgressControllerFinishedEventArgs(result));
        }

        public void SimulateStepExecutionChanged(string progressDetails, double progress)
        {
            this.StepExecutionChanged?.Invoke(this, new StepExecutionChangedEventArgs(new TestStep(progressDetails, progress)));
        }
        #endregion

        #region Helpers
        private class TestStep : IProgressStep
        {
            public TestStep(string progressDetails, double progress)
            {
                this.ProgressDetailText = progressDetails;
                this.Progress = progress;
            }

            public bool Cancellable
            {
                get;
                set;
            }

            public string DisplayText
            {
                get;
                set;
            }

            public StepExecutionState ExecutionState
            {
                get;
                set;
            }

            public bool Hidden
            {
                get;
                set;
            }

            public bool ImpactsProgress
            {
                get;
                set;
            }

            public bool Indeterminate
            {
                get;
                set;
            }

            public double Progress
            {
                get;
                set;
            }

            public string ProgressDetailText
            {
                get;
                set;
            }

#pragma warning disable 67
            public event EventHandler<StepExecutionChangedEventArgs> StateChanged;
#pragma warning restore 67
        }
        #endregion
    }
}
