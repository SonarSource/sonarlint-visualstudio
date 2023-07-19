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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    [Export(typeof(DocumentClosedHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DocumentClosedHandler : IDisposable
    {
        private readonly IDocumentEvents documentEvents;
        private readonly ILocalHotspotsStore localHotspotsStore;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public DocumentClosedHandler(IDocumentEvents documentEvents,
            ILocalHotspotsStore localHotspotsStore,
            IThreadHandling threadHandling)
        {
            this.documentEvents = documentEvents;
            this.localHotspotsStore = localHotspotsStore;
            this.threadHandling = threadHandling;

            this.documentEvents.DocumentClosed += OnDocumentClosed;
        }

        private void OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            UpdateStoreAsync(e.FullPath).Forget();
        }

        private async Task UpdateStoreAsync(string closedFilePath)
        {
            await threadHandling.RunOnBackgroundThread(() =>
            {
                localHotspotsStore.RemoveForFile(closedFilePath);
                return Task.FromResult(true);
            });
        }

        public void Dispose() => documentEvents.DocumentClosed -= OnDocumentClosed;
    }
}
