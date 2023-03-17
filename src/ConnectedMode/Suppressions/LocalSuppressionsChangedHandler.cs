/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    /// <summary>
    /// Listens for notifications that the set of locally-suppressed issues has changed
    /// and triggers an update of the UI (Error List and editor)
    /// </summary>
    [Export(typeof(LocalSuppressionsChangedHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocalSuppressionsChangedHandler : IDisposable
    {
        private readonly IClientSuppressionSynchronizer clientSuppressionSynchronizer;
        private readonly IIssueLocationStoreAggregator issueLocationStore;

        [ImportingConstructor]
        public LocalSuppressionsChangedHandler(
            IClientSuppressionSynchronizer clientSuppressionSynchronizer,
            IIssueLocationStoreAggregator issueLocationStore)
        {
            this.clientSuppressionSynchronizer = clientSuppressionSynchronizer;
            this.issueLocationStore = issueLocationStore;

            clientSuppressionSynchronizer.LocalSuppressionsChanged += OnLocalSuppressionsChanged;
        }

        private void OnLocalSuppressionsChanged(object sender, LocalSuppressionsChangedEventArgs e)
            => issueLocationStore.Refresh(e.ChangedFiles);

        public void Dispose()
            => clientSuppressionSynchronizer.LocalSuppressionsChanged -= OnLocalSuppressionsChanged;
    }
}
