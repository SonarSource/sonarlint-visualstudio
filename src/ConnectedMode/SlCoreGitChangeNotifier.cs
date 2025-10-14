/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Branch;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(ISlCoreGitChangeNotifier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SlCoreGitChangeNotifier : ISlCoreGitChangeNotifier
    {
        private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
        private readonly ISLCoreServiceProvider serviceProvider;
        private readonly IBoundSolutionGitMonitor gitMonitor;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private bool disposed;

        [ImportingConstructor]
        public SlCoreGitChangeNotifier(
            IActiveConfigScopeTracker activeConfigScopeTracker,
            ISLCoreServiceProvider serviceProvider,
            IBoundSolutionGitMonitor gitMonitor,
            ILogger logger,
            IThreadHandling threadHandling,
            IInitializationProcessorFactory initializationProcessorFactory)
        {
            this.activeConfigScopeTracker = activeConfigScopeTracker;
            this.serviceProvider = serviceProvider;
            this.gitMonitor = gitMonitor;
            this.logger = logger;
            this.threadHandling = threadHandling;

            InitializationProcessor = initializationProcessorFactory.Create<SlCoreGitChangeNotifier>([gitMonitor],
                _ => threadHandling.RunOnUIThreadAsync(() =>
            {
                if (disposed)
                {
                    return;
                }
                gitMonitor.HeadChanged += GitMonitor_OnHeadChanged;
                activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_OnCurrentConfigurationScopeChanged;
            }));
        }

        private void ActiveConfigScopeTracker_OnCurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
        {
            if (e.DefinitionChanged)
            {
                gitMonitor.Refresh();
            }
        }

        private void GitMonitor_OnHeadChanged(object sender, EventArgs e) =>
            threadHandling.RunOnBackgroundThread(() =>
            {
                if (!serviceProvider.TryGetTransientService(out ISonarProjectBranchSlCoreService sonarProjectBranchSlCoreService))
                {
                    logger.LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
                    return;
                }
                if (activeConfigScopeTracker.Current == null)
                {
                    return;
                }
                sonarProjectBranchSlCoreService.DidVcsRepositoryChange(new DidVcsRepositoryChangeParams(activeConfigScopeTracker.Current.Id));
            }).Forget();

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (InitializationProcessor.IsFinalized)
            {
                gitMonitor.HeadChanged -= GitMonitor_OnHeadChanged;
                activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTracker_OnCurrentConfigurationScopeChanged;
            }
            disposed = true;
        }

        public IInitializationProcessor InitializationProcessor { get; }
    }
}
