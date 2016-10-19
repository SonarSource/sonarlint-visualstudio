//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressVisualizer.IProgressVisualizer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressVisualizer"/>
    /// </summary>
    public partial class ConfigurableProgressVisualizer : IProgressVisualizer
    {
        ProgressControllerViewModel IProgressVisualizer.ViewModel
        {
            get
            {
                this.CheckUIThread();

                return this.viewModel;
            }

            set
            {
                this.CheckUIThread();

                this.viewModel = value;
            }
        }

        void IProgressVisualizer.Show()
        {
            this.CheckUIThread();

            this.isShown = true;
        }

        void IProgressVisualizer.Hide()
        {
            this.CheckUIThread();

            this.isShown = false;
        }

        private void CheckUIThread()
        {
            if (this.ThrowIfAccessedNotFromUIThread)
            {
                Assert.IsTrue(ThreadHelper.CheckAccess(), "Wasn't called on the UI thread");
            }
        }
    }
}
