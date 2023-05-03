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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(PreSolutionBoundUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PreSolutionBoundUpdateHandler
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IImportBeforeFileGenerator importBeforeFileGenerator;

        private bool disposed;

        [ImportingConstructor]
        public PreSolutionBoundUpdateHandler(IActiveSolutionBoundTracker activeSolutionBoundTracker, IImportBeforeFileGenerator importBeforeFileGenerator)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.importBeforeFileGenerator = importBeforeFileGenerator;

            this.activeSolutionBoundTracker.PreSolutionBindingChanged += OnPreSolutionBindingChanged;
            this.activeSolutionBoundTracker.PreSolutionBindingUpdated += OnPreSolutionBindingUpdated;
        }

        private void OnPreSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            if (e.Configuration.Mode != SonarLintMode.Standalone)
            {
                TriggerUpdate();
            }
        }

        private void OnPreSolutionBindingUpdated(object sender, EventArgs e) => TriggerUpdate();

        private void TriggerUpdate()
        {
            // This should only happen once which is why it gets disposed after.
            importBeforeFileGenerator.WriteTargetsFileToDiskIfNotExists();
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                this.activeSolutionBoundTracker.PreSolutionBindingChanged -= OnPreSolutionBindingChanged;
                this.activeSolutionBoundTracker.PreSolutionBindingUpdated -= OnPreSolutionBindingUpdated;
                disposed = true;
            }
        }
    }
}
