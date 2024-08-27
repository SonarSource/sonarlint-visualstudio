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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.State;
using SonarQube.Client;
using Task = System.Threading.Tasks.Task;

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
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker, IDisposable, IPartImportsSatisfiedNotification
    {
        private readonly IHost extensionHost;
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsMonitorSelection vsMonitorSelection;
        private readonly IBoundSolutionGitMonitor gitEventsMonitor;
        private readonly IConfigScopeUpdater configScopeUpdater;
        private readonly ILogger logger;
        private readonly uint boundSolutionContextCookie;

        public event EventHandler<ActiveSolutionBindingEventArgs> PreSolutionBindingChanged;
        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;
        public event EventHandler PreSolutionBindingUpdated;
        public event EventHandler SolutionBindingUpdated;

        public BindingConfiguration CurrentConfiguration { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IHost host,
            IActiveSolutionTracker activeSolutionTracker,
            IConfigScopeUpdater configScopeUpdater,
            ILogger logger,
            IBoundSolutionGitMonitor gitEventsMonitor,
            IConfigurationProvider configurationProvider)
        {
            extensionHost = host;
            solutionTracker = activeSolutionTracker;
            this.gitEventsMonitor = gitEventsMonitor;
            this.logger = logger;

            // TODO - MEF-ctor should be free-threaded -> we might be on a background thread ->
            // calling serviceProvider.GetService is dangerous.
            vsMonitorSelection = serviceProvider.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();
            vsMonitorSelection.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out boundSolutionContextCookie);

            this.configurationProvider = configurationProvider;
            this.configScopeUpdater = configScopeUpdater;

            // The user changed the binding through the Team Explorer
            extensionHost.VisualStateManager.BindingStateChanged += OnBindingStateChanged;

            // The solution changed inside the IDE
            solutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
            
            CurrentConfiguration = GetBindingConfiguration();

            SetBoundSolutionUIContext();

            this.gitEventsMonitor.HeadChanged += GitEventsMonitor_HeadChanged;
        }

        private BindingConfiguration GetBindingConfiguration()
        {
            var oldConfig = configurationProvider.GetConfiguration();

            if (oldConfig.Mode == SonarLintMode.Standalone)
            {
                return BindingConfiguration.Standalone;
            }

            ServerConnection connection = oldConfig.Project switch
            {
                { Organization: not null } => new ServerConnection.SonarCloud(oldConfig.Project.Organization.Key, credentials: oldConfig.Project.Credentials),
                { ServerUri: not null } => new ServerConnection.SonarQube(oldConfig.Project.ServerUri, credentials: oldConfig.Project.Credentials),
                _ => null
            };
            
            return new BindingConfiguration(
                new BoundServerProject("Solution Name Placeholder", // todo https://sonarsource.atlassian.net/browse/SLVS-1402
                    oldConfig.Project?.ProjectKey,
                    connection)
                {
                    Profiles = oldConfig.Project?.Profiles
                },
                oldConfig.Mode,
                oldConfig.BindingConfigDirectory);
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
                
                await UpdateConnectionAsync(newBindingConfiguration);

                gitEventsMonitor.Refresh();

                this.RaiseAnalyzersChangedIfBindingChanged(newBindingConfiguration);
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Error handling solution change: {ex.Message}");
            }
        }

        private async Task UpdateConnectionAsync(BindingConfiguration bindingConfiguration)
        {
            ISonarQubeService sonarQubeService = this.extensionHost.SonarQubeService;

            if (sonarQubeService.IsConnected)
            {
                if (this.extensionHost.ActiveSection?.DisconnectCommand.CanExecute(null) == true)
                {
                    this.extensionHost.ActiveSection.DisconnectCommand.Execute(null);
                }
                else
                {
                    sonarQubeService.Disconnect();
                }
            }

            Debug.Assert(!sonarQubeService.IsConnected,
                "SonarQube service should always be disconnected at this point");

            var boundProject = bindingConfiguration.Project;

            if (boundProject != null)
            {
                var connectionInformation = boundProject.CreateConnectionInformation();
                await Core.WebServiceHelper.SafeServiceCallAsync(() => sonarQubeService.ConnectAsync(connectionInformation,
                    CancellationToken.None), this.logger);
            }
        }

        private void OnBindingStateChanged(object sender, BindingStateEventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged(GetBindingConfiguration(), e.IsBindingCleared);
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
            {
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
                this.solutionTracker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                this.extensionHost.VisualStateManager.BindingStateChanged -= this.OnBindingStateChanged;
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
