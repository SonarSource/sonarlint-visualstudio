/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Raises an event after the bound solution state has finished changing
    /// i.e. the server connection has been opened/closed as appropriate.
    /// </summary>
    /// <remarks>
    /// In addition to raising an event, this class will also set/clear the <see cref="BoundSolutionUIContext"/>
    /// UIContext.
    /// </remarks>
    [Export(typeof(IActiveSolutionBoundTracker))]
    [Export(typeof(IActiveSolutionChangedHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker, IActiveSolutionChangedHandler, IDisposable, IPartImportsSatisfiedNotification
    {
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IVsMonitorSelection vsMonitorSelection;
        private readonly IBoundSolutionGitMonitor gitEventsMonitor;
        private readonly IConfigScopeUpdater configScopeUpdater;
        private readonly ILogger logger;
        private readonly uint boundSolutionContextCookie;
        private bool disposed;

        public event EventHandler<ActiveSolutionBindingEventArgs> PreSolutionBindingChanged;
        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;
        public event EventHandler PreSolutionBindingUpdated;
        public event EventHandler SolutionBindingUpdated;
        public BindingConfiguration CurrentConfiguration { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IActiveSolutionTracker activeSolutionTracker,
            IConfigScopeUpdater configScopeUpdater,
            ILogger logger,
            IBoundSolutionGitMonitor gitEventsMonitor,
            IConfigurationProvider configurationProvider,
            ISonarQubeService sonarQubeService)
        {
            solutionTracker = activeSolutionTracker;
            this.gitEventsMonitor = gitEventsMonitor;
            this.logger = logger;

            // TODO - MEF-ctor should be free-threaded -> we might be on a background thread ->
            // calling serviceProvider.GetService is dangerous.
            vsMonitorSelection = serviceProvider.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();
            vsMonitorSelection.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out boundSolutionContextCookie);

            this.configurationProvider = configurationProvider;
            this.sonarQubeService = sonarQubeService;
            this.configScopeUpdater = configScopeUpdater;

            // The solution changed inside the IDE
            solutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;

            CurrentConfiguration = GetBindingConfiguration();

            SetBoundSolutionUIContext();

            this.gitEventsMonitor.HeadChanged += GitEventsMonitor_HeadChanged;
        }

        public void HandleBindingChange(bool isBindingCleared)
        {
            if (disposed)
            {
                return;
            }

            this.RaiseAnalyzersChangedIfBindingChanged(GetBindingConfiguration(), isBindingCleared);
        }

        private BindingConfiguration GetBindingConfiguration()
        {
            return configurationProvider.GetConfiguration();
        }

        private void GitEventsMonitor_HeadChanged(object sender, EventArgs e)
        {
            var boundProject = this.configurationProvider.GetConfiguration().Project;

            if (boundProject != null)
            {
                PreSolutionBindingUpdated?.Invoke(this, EventArgs.Empty);
                SolutionBindingUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs args)
        {
            // An exception here will crash VS
            try
            {
                var newBindingConfiguration = GetBindingConfiguration();

                var connectionUpdatedSuccessfully = await UpdateConnectionAsync(newBindingConfiguration);

                gitEventsMonitor.Refresh();

                RaiseAnalyzersChangedIfBindingChanged(connectionUpdatedSuccessfully ? newBindingConfiguration : BindingConfiguration.Standalone);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Error handling solution change: {ex.Message}");
            }
        }

        private async Task<bool> UpdateConnectionAsync(BindingConfiguration bindingConfiguration)
        {
            if (sonarQubeService.IsConnected)
            {
                sonarQubeService.Disconnect();
            }

            Debug.Assert(!sonarQubeService.IsConnected,
                "SonarQube service should always be disconnected at this point");

            if (!bindingConfiguration.Mode.IsInAConnectedMode())
            {
                // The Standalone mode has no connection so there is nothing to update, thus nothing to fail
                return true;
            }

            var boundProject = bindingConfiguration.Project;
            var connectionInformation = boundProject.CreateConnectionInformation();
            var isConnected = await WebServiceHelper.SafeServiceCallAsync(async () =>
            {
                await sonarQubeService.ConnectAsync(connectionInformation, CancellationToken.None);
                return sonarQubeService.IsConnected;
            }, logger);

            return isConnected;
        }

        private void RaiseAnalyzersChangedIfBindingChanged(BindingConfiguration newBindingConfiguration, bool? isBindingCleared = null)
        {
            configScopeUpdater.UpdateConfigScopeForCurrentSolution(newBindingConfiguration.Project);

            if (!CurrentConfiguration.Equals(newBindingConfiguration))
            {
                CurrentConfiguration = newBindingConfiguration;

                var args = new ActiveSolutionBindingEventArgs(newBindingConfiguration);
                PreSolutionBindingChanged?.Invoke(this, args);
                SolutionBindingChanged?.Invoke(this, args);
            }
            else if (isBindingCleared == false)
            { // todo remove unreachable code & cleanup https://sonarsource.atlassian.net/browse/SLVS-1532
                PreSolutionBindingUpdated?.Invoke(this, EventArgs.Empty);
                SolutionBindingUpdated?.Invoke(this, EventArgs.Empty);
            }

            SetBoundSolutionUIContext();
        }

        private void SetBoundSolutionUIContext()
        {
            var isContextActive = !CurrentConfiguration.Equals(BindingConfiguration.Standalone);
            vsMonitorSelection.SetCmdUIContext(boundSolutionContextCookie, isContextActive ? 1 : 0);
        }

        #region IPartImportsSatisfiedNotification

        public async void OnImportsSatisfied()
        {
            await UpdateConnectionAsync(GetBindingConfiguration());
        }

        #endregion

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.disposed = true;
                this.solutionTracker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                this.gitEventsMonitor.HeadChanged -= GitEventsMonitor_HeadChanged;
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
