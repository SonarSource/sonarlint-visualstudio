/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
