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
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
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
        private readonly IBoundSolutionGitMonitor gitEventsMonitor;
        private readonly IVsUIServiceOperation vSServiceOperation;
        private readonly ILogger logger;

        public event EventHandler<ActiveSolutionBindingEventArgs> PreSolutionBindingChanged;

        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;

        public event EventHandler PreSolutionBindingUpdated;

        public event EventHandler SolutionBindingUpdated;

        public BindingConfiguration CurrentConfiguration { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(
            IVsUIServiceOperation vSServiceOperation,
            IHost host,
            IActiveSolutionTracker activeSolutionTracker,
            ILogger logger,
            IBoundSolutionGitMonitor gitEventsMonitor,
            IConfigurationProvider configurationProvider)
        {
            extensionHost = host;
            solutionTracker = activeSolutionTracker;
            this.gitEventsMonitor = gitEventsMonitor;
            this.vSServiceOperation = vSServiceOperation;
            this.logger = logger;
            this.configurationProvider = configurationProvider;

            // The user changed the binding through the Team Explorer
            extensionHost.VisualStateManager.BindingStateChanged += OnBindingStateChanged;

            // The solution changed inside the IDE
            solutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;

            CurrentConfiguration = configurationProvider.GetConfiguration();

            this.gitEventsMonitor.HeadChanged += GitEventsMonitor_HeadChanged;
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
                await UpdateConnectionAsync();

                gitEventsMonitor.Refresh();

                this.RaiseAnalyzersChangedIfBindingChanged();
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Error handling solution change: {ex.Message}");
            }
        }

        private async Task UpdateConnectionAsync()
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

            var boundProject = this.configurationProvider.GetConfiguration().Project;

            if (boundProject != null)
            {
                var connectionInformation = boundProject.CreateConnectionInformation();
                await Core.WebServiceHelper.SafeServiceCallAsync(() => sonarQubeService.ConnectAsync(connectionInformation,
                    CancellationToken.None), this.logger);
            }
        }

        private void OnBindingStateChanged(object sender, BindingStateEventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged(e.IsBindingCleared);
        }

        private void RaiseAnalyzersChangedIfBindingChanged(bool? isBindingCleared = null)
        {
            var newBindingConfiguration = configurationProvider.GetConfiguration();

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
            vSServiceOperation.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(
               monitorSelection =>
               {
                   monitorSelection.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out var boundSolutionContextCookie);

                   var isContextActive = !CurrentConfiguration.Equals(BindingConfiguration.Standalone);
                   monitorSelection.SetCmdUIContext(boundSolutionContextCookie, isContextActive ? 1 : 0);
               });
        }

        #region IPartImportsSatisfiedNotification

        public async void OnImportsSatisfied()
        {
            await UpdateConnectionAsync();
        }

        #endregion IPartImportsSatisfiedNotification

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

        #endregion IDisposable
    }
}
