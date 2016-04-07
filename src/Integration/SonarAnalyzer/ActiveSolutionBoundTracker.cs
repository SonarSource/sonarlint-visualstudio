//-----------------------------------------------------------------------
// <copyright file="ActiveSolutionBoundTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Persistence;
using System;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.SonarAnalyzer
{
    [Export(typeof(IActiveSolutionBoundTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker
    {
        private readonly IHost extensionHost;
        private readonly IActiveSolutionTracker solutionTracker;

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

            this.extensionHost.VisualStateManager.BindingStateChanged += this.SolutionTracker_ActiveSolutionChanged;
            this.solutionTracker.ActiveSolutionChanged += this.SolutionTracker_ActiveSolutionChanged;
            this.CalculateSolutionBinding();
        }

        private void SolutionTracker_ActiveSolutionChanged(object sender, EventArgs e)
        {
            this.CalculateSolutionBinding();
        }

        private void CalculateSolutionBinding()
        {
            ISolutionBinding solutionBinding = this.extensionHost.GetService<ISolutionBinding>();
            solutionBinding.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject bindingInfo = solutionBinding.ReadSolutionBinding();
            this.IsActiveSolutionBound = bindingInfo != null;
        }

        public bool IsActiveSolutionBound { get; private set; } = false;

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.solutionTracker != null)
                {
                    this.solutionTracker.ActiveSolutionChanged -= this.SolutionTracker_ActiveSolutionChanged;
                }

                if (this.extensionHost?.VisualStateManager != null)
                {
                    this.extensionHost.VisualStateManager.BindingStateChanged -= this.SolutionTracker_ActiveSolutionChanged;
                }
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
