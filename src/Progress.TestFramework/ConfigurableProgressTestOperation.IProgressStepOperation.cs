//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressTestOperation.IProgressStepOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial implementation of <see cref="IProgressStepOperation"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStepOperation
    {
        IProgressStep IProgressStepOperation.Step
        {
            get { return this; }
        }

        Task<StepExecutionState> IProgressStepOperation.Run(CancellationToken cancellationToken, IProgressStepExecutionEvents executionNotify)
        {
            Assert.IsNotNull(cancellationToken, "cancellationToken is not expected to be null");
            Assert.IsNotNull(executionNotify, "executionNotify is not expected to be null");
            return Task.Factory.StartNew(() =>
            {
                this.ExecutionState = StepExecutionState.Executing;
                this.operation(cancellationToken, executionNotify);
                this.executed = true;
                return this.ExecutionState = this.ExecutionResult;
            });
        }
    }
}
