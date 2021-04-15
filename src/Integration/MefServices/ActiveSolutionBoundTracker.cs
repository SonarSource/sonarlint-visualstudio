﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.State;
using SonarQube.Client;

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
        private readonly IErrorListInfoBarController errorListInfoBarController;
        private readonly IConfigurationProviderService configurationProvider;
        private readonly IVsMonitorSelection vsMonitorSelection;
        private readonly ILogger logger;
        private readonly uint boundSolutionContextCookie;

        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;
        public event EventHandler SolutionBindingUpdated;

        public BindingConfiguration CurrentConfiguration { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(IHost host, IActiveSolutionTracker activeSolutionTracker, ILogger logger)
        {
            extensionHost = host ?? throw new ArgumentNullException(nameof(host));
            solutionTracker = activeSolutionTracker ?? throw new ArgumentNullException(nameof(activeSolutionTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            vsMonitorSelection = host.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();
            vsMonitorSelection.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out boundSolutionContextCookie);

            configurationProvider = extensionHost.GetService<IConfigurationProviderService>();
            configurationProvider.AssertLocalServiceIsNotNull();

            errorListInfoBarController = extensionHost.GetService<IErrorListInfoBarController>();
            errorListInfoBarController.AssertLocalServiceIsNotNull();

            // The user changed the binding through the Team Explorer
            extensionHost.VisualStateManager.BindingStateChanged += OnBindingStateChanged;

            // The solution changed inside the IDE
            solutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;

            CurrentConfiguration = configurationProvider.GetConfiguration();

            SetBoundSolutionUIContext();
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
                await Core.WebServiceHelper.SafeServiceCallAsync(() => sonarQubeService.ConnectAsync(connectionInformation,
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
            var newBindingConfiguration = configurationProvider.GetConfiguration();

            if (!CurrentConfiguration.Equals(newBindingConfiguration))
            {
                CurrentConfiguration = newBindingConfiguration;
                SolutionBindingChanged?.Invoke(this, new ActiveSolutionBindingEventArgs(newBindingConfiguration));
            }
            else if (isBindingCleared == false)
            {
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
