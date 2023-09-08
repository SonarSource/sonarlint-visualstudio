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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Install
{
    [Export(typeof(ImportBeforeInstallTrigger))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ImportBeforeInstallTrigger : IDisposable
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IImportBeforeFileGenerator importBeforeFileGenerator;
        private readonly IThreadHandling threadHandling;

        private bool disposed;

        [ImportingConstructor]
        public ImportBeforeInstallTrigger(IActiveSolutionBoundTracker activeSolutionBoundTracker, IImportBeforeFileGenerator importBeforeFileGenerator, IThreadHandling threadHandling)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.importBeforeFileGenerator = importBeforeFileGenerator;
            this.threadHandling = threadHandling;

            this.activeSolutionBoundTracker.PreSolutionBindingChanged += OnPreSolutionBindingChanged;
            this.activeSolutionBoundTracker.PreSolutionBindingUpdated += OnPreSolutionBindingUpdated;
        }

        private void OnPreSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            TriggerUpdateAsync().Forget();
        }

        private void OnPreSolutionBindingUpdated(object sender, EventArgs e) => TriggerUpdateAsync().Forget();

        public async Task TriggerUpdateAsync()
        {
            var config = activeSolutionBoundTracker.CurrentConfiguration;

            if (config.Mode != SonarLintMode.Standalone)
            {
                await threadHandling.SwitchToBackgroundThread();

                importBeforeFileGenerator.WriteTargetsFileToDiskIfNotExists();

                Dispose();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                activeSolutionBoundTracker.PreSolutionBindingChanged -= OnPreSolutionBindingChanged;
                activeSolutionBoundTracker.PreSolutionBindingUpdated -= OnPreSolutionBindingUpdated;
                disposed = true;
            }
        }
    }
}
