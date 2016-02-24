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
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressController : IProgressController, IProgressEvents
    {
        private int numberOfAbortRequests = 0;
        private List<IProgressStep> steps = new List<IProgressStep>();
        private EventHandler<ProgressEventArgs> started;
        private EventHandler<ProgressControllerFinishedEventArgs> finished;
        private EventHandler<StepExecutionChangedEventArgs> stepExecutionChanged;
        private EventHandler<CancellationSupportChangedEventArgs> cancellationSupportChanged;

        #region IProgressController
        IErrorNotificationManager IProgressController.ErrorNotificationManager
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IProgressEvents IProgressController.Events
        {
            get
            {
                return this;
            }
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        void IProgressController.Initialize(IProgressStepFactory stepFactory, IEnumerable<IProgressStepDefinition> stepsDefinition)
        {
            throw new NotImplementedException();
        }

        Task<ProgressControllerResult> IProgressController.Start()
        {
            throw new NotImplementedException();
        }

        bool IProgressController.TryAbort()
        {
            this.numberOfAbortRequests++;
            return true;
        }
        #endregion

        #region IProgressEvents
        IEnumerable<IProgressStep> IProgressEvents.Steps
        {
            get
            {
                return this.steps;
            }
        }

        event EventHandler<ProgressEventArgs> IProgressEvents.Started
        {
            add
            {
                this.started += value;
            }

            remove
            {
                this.started -= value;
            }
        }

        event EventHandler<ProgressControllerFinishedEventArgs> IProgressEvents.Finished
        {
            add
            {
                this.finished += value;
            }

            remove
            {
                this.finished -= value;
            }
        }

        event EventHandler<StepExecutionChangedEventArgs> IProgressEvents.StepExecutionChanged
        {
            add
            {
                this.stepExecutionChanged += value;
            }

            remove
            {
                this.stepExecutionChanged -= value;
            }
        }

        event EventHandler<CancellationSupportChangedEventArgs> IProgressEvents.CancellationSupportChanged
        {
            add
            {
                this.cancellationSupportChanged += value;
            }

            remove
            {
                this.cancellationSupportChanged -= value;
            }
        }
        #endregion

        #region Test helpers
        public void AssertNumberOfAbortRequests(int expected)
        {
            Assert.AreEqual(expected, this.numberOfAbortRequests, "TryAbort was not called the expected number of times");
        }

        public void AddSteps(params IProgressStep[] progressSteps)
        {
            this.steps.AddRange(progressSteps);
        }
        #endregion
    }
}
