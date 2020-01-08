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
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

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

        #endregion IProgressEvents

        #region Test helpers

        public void AssertNoFinishedEventHandlers()
        {
            this.Finished.Should().BeNull("Not expecting any handler for Finished event");
        }

        public void SimulateFinished(ProgressControllerResult result)
        {
            this.Finished?.Invoke(this, new ProgressControllerFinishedEventArgs(result));
        }

        public void SimulateStepExecutionChanged(string progressDetails, double progress)
        {
            this.StepExecutionChanged?.Invoke(this, new StepExecutionChangedEventArgs(new TestStep(progressDetails, progress)));
        }

        #endregion Test helpers

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

        #endregion Helpers
    }
}