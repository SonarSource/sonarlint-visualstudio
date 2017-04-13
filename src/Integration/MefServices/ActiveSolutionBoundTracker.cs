/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
using System;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionBoundTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker, IDisposable
    {
        private readonly IHost extensionHost;
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IErrorListInfoBarController errorListInfoBarController;
        private readonly ISolutionBindingInformationProvider solutionBindingInformationProvider;

        public event EventHandler<bool> SolutionBindingChanged;

        public bool IsActiveSolutionBound { get; private set; }

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

            this.solutionBindingInformationProvider = this.extensionHost.GetService<ISolutionBindingInformationProvider>();
            this.solutionBindingInformationProvider.AssertLocalServiceIsNotNull();

            this.errorListInfoBarController = this.extensionHost.GetService<IErrorListInfoBarController>();
            this.errorListInfoBarController.AssertLocalServiceIsNotNull();
            this.errorListInfoBarController.Refresh();

            // The user changed the binding through the Team Explorer
            this.extensionHost.VisualStateManager.BindingStateChanged += this.OnBindingStateChanged;

            // The solution changed inside the IDE
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            this.IsActiveSolutionBound = this.solutionBindingInformationProvider.IsSolutionBound();
        }

        private void OnActiveSolutionChanged(object sender, EventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged();
            this.errorListInfoBarController.Refresh();
        }

        private void OnBindingStateChanged(object sender, EventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged();
        }

        private void RaiseAnalyzersChangedIfBindingChanged()
        {
            bool isSolutionCurrentlyBound = this.solutionBindingInformationProvider.IsSolutionBound();
            if (this.IsActiveSolutionBound == isSolutionCurrentlyBound)
            {
                return;
            }

            this.IsActiveSolutionBound = isSolutionCurrentlyBound;
            this.OnAnalyzersChanged(isBound: this.IsActiveSolutionBound);
        }

        private void OnAnalyzersChanged(bool isBound)
        {
            this.SolutionBindingChanged?.Invoke(this, isBound);
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
