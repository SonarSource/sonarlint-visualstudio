//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressStep.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressStep : IProgressStep
    {
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
}
