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
using System.Collections.Generic;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressEvents"/>
    /// </summary>
    internal class ConfigurableProgressEvents : IProgressEvents
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

        #endregion Test implementation of IProgressEvents

        #region Simulation

        public void InvokeStarted()
        {
            this.Started?.Invoke(this, new ProgressEventArgs());
        }

        public void InvokeFinished(ProgressControllerResult result)
        {
            this.Finished?.Invoke(this, new ProgressControllerFinishedEventArgs(result));
        }

        public void InvokeStepExecutionChanged(StepExecutionChangedEventArgs args)
        {
            this.StepExecutionChanged?.Invoke(this, args);
        }

        public void InvokeCancellationSupportChanged(bool cancellable)
        {
            this.CancellationSupportChanged?.Invoke(this, new CancellationSupportChangedEventArgs(cancellable));
        }

        #endregion Simulation

        #region Verification

        public void AssertAllEventsAreRegistered()
        {
            this.Started.Should().NotBeNull("The Started event isn't registered");
            this.Finished.Should().NotBeNull("The Finished event wasn't registered");
            this.StepExecutionChanged.Should().NotBeNull("The StepExecutionChanged event isn't registered");
            this.CancellationSupportChanged.Should().NotBeNull("The CancellationSupportChanged event isn't registered");
        }

        public void AssertAllEventsAreUnregistered()
        {
            this.Started.Should().BeNull("The Started event is registered");
            this.Finished.Should().BeNull("The Finished event is registered");
            this.StepExecutionChanged.Should().BeNull("The StepExecutionChanged event is registered");
            this.CancellationSupportChanged.Should().BeNull("The CancellationSupportChanged event is registered");
        }

        #endregion Verification
    }
}