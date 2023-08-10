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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    /// <summary>
    /// Listens for changes the local hotspot store and sets the <see cref="LocalHotspotIssuesExistUIContext"/>
    /// appropriately (which in turn will trigger showing/hiding the local hotspots tool window)
    /// </summary>
    [Export(typeof(ILocalHotspotStoreMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocalHotspotStoreMonitor : ILocalHotspotStoreMonitor, IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILocalHotspotsStore localHotspotsStore;
        private readonly IThreadHandling threadHandling;

        private IVsMonitorSelection vsMonitorSelection;
        private uint contextCookie;

        [ImportingConstructor]
        public LocalHotspotStoreMonitor([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
            ILocalHotspotsStore localHotspotsStore,
            IThreadHandling threadHandling)
        {
            this.serviceProvider = serviceProvider;
            this.localHotspotsStore = localHotspotsStore;
            this.threadHandling = threadHandling;
        }

        public async Task InitializeAsync()
        {
            localHotspotsStore.IssuesChanged += OnIssuesChanged;

            await threadHandling.RunOnUIThreadAsync(() =>
            {
                vsMonitorSelection = (IVsMonitorSelection)serviceProvider.GetService(typeof(SVsShellMonitorSelection));
                Guid localGuid = LocalHotspotIssuesExistUIContext.Guid;
                vsMonitorSelection.GetCmdUIContextCookie(ref localGuid, out contextCookie);

                Refresh();
            });
        }

        private void OnIssuesChanged(object sender, IssuesStore.IssuesChangedEventArgs e) => Refresh();

        private void Refresh()
        {
            Debug.Assert(contextCookie != 0, "Instance has not been initialized");

            var hotspotIssuesExist = localHotspotsStore.GetAllLocalHotspots().Any();
            UpdateLocalHotpotIssuesUIContextAsync(hotspotIssuesExist).Forget();
        }

        private async Task UpdateLocalHotpotIssuesUIContextAsync(bool hasIssues)
        {
            await threadHandling.RunOnUIThreadAsync(() =>
            {
                vsMonitorSelection.SetCmdUIContext(contextCookie, hasIssues ? 1 : 0);
            });
        }

        public void Dispose() => localHotspotsStore.IssuesChanged -= OnIssuesChanged;
    }
}
