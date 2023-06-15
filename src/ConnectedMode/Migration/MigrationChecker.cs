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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(MigrationChecker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class MigrationChecker : IDisposable
    {
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IMefFactory mefFactory;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IObsoleteConfigurationProvider obsoleteConfigurationProvider;
        private IMigrationPrompt migrationPrompt;

        [ImportingConstructor]
        public MigrationChecker(
            IActiveSolutionTracker activeSolutionTracker,
            IMefFactory mefFactory,
            IConfigurationProvider configurationProvider,
            IObsoleteConfigurationProvider obsoleteConfigurationProvider)
        {
            this.activeSolutionTracker = activeSolutionTracker;
            this.mefFactory = mefFactory;
            this.configurationProvider = configurationProvider;
            this.obsoleteConfigurationProvider = obsoleteConfigurationProvider;

            activeSolutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
        }

        private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs args)
        {
            if (args.IsSolutionOpen)
            {
                DisplayMigrationPromptIfMigrationIsNeededAsync().Forget();
            }
            else
            {
                ClearMigrationPrompt();
            }
        }

        public async Task DisplayMigrationPromptIfMigrationIsNeededAsync()
        {
            // If the user has the old files but not the new files it means they are bound and a goldbar should be shown to initiate migration.
            if (obsoleteConfigurationProvider.GetConfiguration()?.Mode != SonarLintMode.Standalone
                && configurationProvider.GetConfiguration()?.Mode == SonarLintMode.Standalone)
            {
               migrationPrompt = await mefFactory.CreateAsync<IMigrationPrompt>();
               migrationPrompt.ShowAsync().Forget();
            }
        }

        private void ClearMigrationPrompt()
        {
            migrationPrompt?.Dispose();
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
            ClearMigrationPrompt();
        }
    }
}
