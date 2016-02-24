//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressStepExecutionNotifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper class to monitor the progress change notification (implements <see cref="IProgressStepExecutionEvents"/>)
    /// </summary>
    public class ConfigurableProgressStepExecutionNotifier : IProgressStepExecutionEvents
    {
        public ConfigurableProgressStepExecutionNotifier()
        {
            this.ProgressChanges = new List<Tuple<string, double>>();
        }

        #region Verification
        public List<Tuple<string, double>> ProgressChanges
        {
            get;
            set;
        }
        #endregion

        #region Configuration
        public Action<string, double> ProgressChangedAction
        {
            get;
            set;
        }
        #endregion

        #region Test implementation of IProgressStepExecutionEvents  (not to be used explicitly by the test code)
        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.ProgressChanges.Add(Tuple.Create(progressDetailText, progress));

            if (this.ProgressChangedAction != null)
            {
                this.ProgressChangedAction(progressDetailText, progress);
            }
        }
        #endregion
    }
}
