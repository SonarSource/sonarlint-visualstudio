/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(IStatefulServerBranchProvider))]
    internal sealed class StatefulServerBranchProvider : IStatefulServerBranchProvider, IDisposable
    {
        private readonly IServerBranchProvider branchProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private bool disposedValue;
        private string selectedBranch;

        [ImportingConstructor]
        public StatefulServerBranchProvider(IServerBranchProvider serverBranchProvider, IActiveSolutionBoundTracker activeSolutionBoundTracker)
        {
            this.branchProvider = serverBranchProvider;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;

            activeSolutionBoundTracker.PreSolutionBindingChanged += OnPreSolutionBindingChanged; 
        }

        private void OnPreSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            selectedBranch = null;
        }

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            if(selectedBranch == null)
            {
                selectedBranch = await branchProvider.GetServerBranchNameAsync(token);
            }

            return selectedBranch;
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    activeSolutionBoundTracker.PreSolutionBindingChanged -= OnPreSolutionBindingChanged;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}
