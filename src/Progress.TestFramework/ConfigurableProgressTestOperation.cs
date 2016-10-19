//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressTestOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressStep"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStep
    {
        private readonly Action<CancellationToken, IProgressStepExecutionEvents> operation;
        private bool executed;

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

        #endregion

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
        #endregion

        #region Verification methods
        public void AssertExecuted()
        {
            Assert.IsTrue(this.executed, "The operation was not executed");
        }

        public void AssertNotExecuted()
        {
            Assert.IsFalse(this.executed, "The operation was executed");
        }
        #endregion
    }
}
