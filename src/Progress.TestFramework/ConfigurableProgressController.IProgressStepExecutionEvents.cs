//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressController.IProgressStepExecutionEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressStepExecutionEvents"/>
    /// </summary>
    public partial class ConfigurableProgressController : IProgressStepExecutionEvents
    {
        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.progressChanges.Add(Tuple.Create(progressDetailText, progress));
        }
    }
}
