//-----------------------------------------------------------------------
// <copyright file="ProgressStepDefinition.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Simple data class representing the step definition
    /// </summary>
    public class ProgressStepDefinition : IProgressStepDefinition
    {
        public ProgressStepDefinition(string displayText, StepAttributes attributes, Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            this.DisplayText = displayText;
            this.Attributes = attributes;
            this.Operation = operation;
        }

        /// <summary>
        /// Display text describing the step.
        /// </summary>
        public string DisplayText { get; private set; }

        /// <summary>
        /// Operation to run when executing the step
        /// </summary>
        public Action<CancellationToken, IProgressStepExecutionEvents> Operation { get; private set; }

        /// <summary>
        /// Attributes of the step
        /// </summary>
        public StepAttributes Attributes { get; private set; }
    }
}
