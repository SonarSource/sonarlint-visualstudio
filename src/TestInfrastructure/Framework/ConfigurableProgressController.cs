/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressController : IProgressController, IProgressEvents
    {
        internal int NumberOfAbortRequests { get; private set; } = 0;
        private readonly List<IProgressStep> steps = new List<IProgressStep>();
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

        Task<ProgressControllerResult> IProgressController.StartAsync()
        {
            throw new NotImplementedException();
        }

        bool IProgressController.TryAbort()
        {
            this.NumberOfAbortRequests++;
            return true;
        }

        #endregion IProgressController

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

        #endregion IProgressEvents

        #region Test helpers

        public void AddSteps(params IProgressStep[] progressSteps)
        {
            this.steps.AddRange(progressSteps);
        }

        #endregion Test helpers
    }
}