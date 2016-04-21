//-----------------------------------------------------------------------
// <copyright file="ActiveSolutionBoundTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Persistence;
using System;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionBoundTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker
    {
        private readonly IHost extensionHost;
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IErrorListInfoBarController errorListInfoBarController;

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(IHost host, IActiveSolutionTracker activeSolutionTracker)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (activeSolutionTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionTracker));
            }

            this.extensionHost = host;
            this.solutionTracker = activeSolutionTracker;

            this.extensionHost.VisualStateManager.BindingStateChanged += this.OnBindingStateChanged;
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            this.errorListInfoBarController = this.extensionHost.GetService<IErrorListInfoBarController>();
            this.errorListInfoBarController.AssertLocalServiceIsNotNull();

            this.CalculateSolutionBinding();
        }

        public bool IsActiveSolutionBound { get; private set; } = false;

        private void OnActiveSolutionChanged(object sender, EventArgs e)
        {
            this.CalculateSolutionBinding();
        }

        private void OnBindingStateChanged(object sender, EventArgs e)
        {
            this.CalculateCurrentBinding();
        }

        private void CalculateSolutionBinding()
        {
            this.CalculateCurrentBinding();
            this.errorListInfoBarController.Refresh();
        }

        private void CalculateCurrentBinding()
        {
            ISolutionBindingSerializer solutionBinding = this.extensionHost.GetService<ISolutionBindingSerializer>();
            solutionBinding.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject bindingInfo = solutionBinding.ReadSolutionBinding();
            this.IsActiveSolutionBound = bindingInfo != null;
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.errorListInfoBarController.Reset();
                this.solutionTracker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                this.extensionHost.VisualStateManager.BindingStateChanged -= this.OnBindingStateChanged;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }
        #endregion
    }
}
