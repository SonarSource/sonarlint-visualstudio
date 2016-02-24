//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressVisualizer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressVisualizer"/>
    /// </summary>
    public partial class ConfigurableProgressVisualizer : IProgressVisualizer
    {
        private bool isShown;
        private ProgressControllerViewModel viewModel;

        public ConfigurableProgressVisualizer()
        {
            this.Reset();
        }

        #region Customization properties
        public ProgressControllerViewModel Root
        {
            get { return this.viewModel; }
        }

        public bool ThrowIfAccessedNotFromUIThread
        {
            get;
            set;
        }
        #endregion

        #region Verification methods
        public void Reset()
        {
            this.viewModel = new ProgressControllerViewModel();
            this.isShown = false;
        }

        public void AssertIsShown()
        {
            Assert.IsTrue(this.isShown, "Expected to be shown");
        }

        public void AssertIsHidden()
        {
            Assert.IsFalse(this.isShown, "Expected to be hidden");
        }
        #endregion
    }
}
