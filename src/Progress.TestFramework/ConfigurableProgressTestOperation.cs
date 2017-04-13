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
using System.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressStep"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStep
    {
        private readonly Action<CancellationToken, IProgressStepExecutionEvents> operation;
        internal bool IsExecuted { get; private set; }

        public ConfigurableProgressTestOperation(Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            this.operation = operation;
            this.CancellableAction = () => true;
            this.ExecutionResult = StepExecutionState.Succeeded;
        }

#pragma warning disable 67

        public event EventHandler<StepExecutionChangedEventArgs> StateChanged;

#pragma warning restore 67

        #region Customization methods

        /// <summary>
        /// Simulate this final execution result after running the operation
        /// </summary>
        public StepExecutionState ExecutionResult
        {
            get;
            set;
        }

        /// <summary>
        /// Delegate that is executed to determine if a step is cancellable
        /// </summary>
        public Func<bool> CancellableAction
        {
            get;
            set;
        }

        #endregion Customization methods

        #region IProgressStep

        public string DisplayText
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

        public bool Indeterminate
        {
            get;
            set;
        }

        public bool Cancellable
        {
            get
            {
                return this.CancellableAction();
            }
        }

        public bool ImpactsProgress
        {
            get;
            set;
        }

        #endregion IProgressStep
    }
}