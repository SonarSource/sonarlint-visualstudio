/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.State;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionBoundTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionBoundTracker : IActiveSolutionBoundTracker, IDisposable, IPartImportsSatisfiedNotification
    {
        private readonly IHost extensionHost;
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IErrorListInfoBarController errorListInfoBarController;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ILogger logger;

        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;
        public event EventHandler SolutionBindingUpdated;

        public BindingConfiguration CurrentConfiguration { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(IHost host, IActiveSolutionTracker activeSolutionTracker, ILogger logger)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (activeSolutionTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionTracker));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.extensionHost = host;
            this.solutionTracker = activeSolutionTracker;
            this.logger = logger;

            this.configurationProvider = this.extensionHost.GetService<IConfigurationProvider>();
            this.configurationProvider.AssertLocalServiceIsNotNull();

            this.errorListInfoBarController = this.extensionHost.GetService<IErrorListInfoBarController>();
            this.errorListInfoBarController.AssertLocalServiceIsNotNull();

            // The user changed the binding through the Team Explorer
            this.extensionHost.VisualStateManager.BindingStateChanged += this.OnBindingStateChanged;

            // The solution changed inside the IDE
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            CurrentConfiguration = this.configurationProvider.GetConfiguration();
        }

        private async void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs args)
        {
            // An exception here will crash VS
            try
            {
                await UpdateConnectionAsync();

                this.RaiseAnalyzersChangedIfBindingChanged();
                this.errorListInfoBarController.Refresh();
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
                await WebServiceHelper.SafeServiceCallAsync(() => sonarQubeService.ConnectAsync(connectionInformation,
                    CancellationToken.None), this.logger);
            }

            Debug.Assert((boundProject != null) == sonarQubeService.IsConnected,
                $"Inconsistent connection state: Solution bound={boundProject != null}, service connected={sonarQubeService.IsConnected}");
        }

        private void OnBindingStateChanged(object sender, BindingStateEventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged(e.IsBindingCleared);
        }

        private void RaiseAnalyzersChangedIfBindingChanged(bool? isBindingCleared = null)
        {
            var newBindingConfiguration = this.configurationProvider.GetConfiguration();

            if (!CurrentConfiguration.Equals(newBindingConfiguration))
            {
                CurrentConfiguration = newBindingConfiguration;
                this.SolutionBindingChanged?.Invoke(this, new ActiveSolutionBindingEventArgs(newBindingConfiguration));
            }
            else if (isBindingCleared == false)
            {
                SolutionBindingUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // do nothing
            }
        }

        #region IPartImportsSatisfiedNotification

        public async void OnImportsSatisfied()
        {
            await UpdateConnectionAsync();
        }

        #endregion

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.errorListInfoBarController.Reset();
                this.solutionTracker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                this.extensionHost.VisualStateManager.BindingStateChanged -= this.OnBindingStateChanged;
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
