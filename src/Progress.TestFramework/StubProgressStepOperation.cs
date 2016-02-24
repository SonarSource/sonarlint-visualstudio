//-----------------------------------------------------------------------
// <copyright file="StubProgressStepOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Stub for <see cref="IProgressStepOperation"/> with no implementation
    /// </summary>
    public class StubProgressStepOperation : IProgressStepOperation
    {
        public IProgressStep Step
        {
            get { throw new NotImplementedException(); }
        }

        public Task<StepExecutionState> Run(System.Threading.CancellationToken cancellationToken, IProgressStepExecutionEvents executionNotify)
        {
            throw new NotImplementedException();
        }
    }
}
