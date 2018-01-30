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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
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
        private readonly ILogger sonarLintOutput;

        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;

        public bool IsActiveSolutionBound { get; private set; }

        public string ProjectKey { get; private set; }

        public SonarLintMode CurrentMode { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(IHost host,
            IActiveSolutionTracker activeSolutionTracker,
            ILogger sonarLintOutput)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (activeSolutionTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionTracker));
            }
            if (sonarLintOutput == null)
            {
                throw new ArgumentNullException(nameof(sonarLintOutput));
            }

            this.extensionHost = host;
            this.solutionTracker = activeSolutionTracker;
            this.sonarLintOutput = sonarLintOutput;

            this.configurationProvider = this.extensionHost.GetService<IConfigurationProvider>();
            this.configurationProvider.AssertLocalServiceIsNotNull();

            this.errorListInfoBarController = this.extensionHost.GetService<IErrorListInfoBarController>();
            this.errorListInfoBarController.AssertLocalServiceIsNotNull();

            // The user changed the binding through the Team Explorer
            this.extensionHost.VisualStateManager.BindingStateChanged += this.OnBindingStateChanged;

            // The solution changed inside the IDE
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            BoundSonarQubeProject project = this.configurationProvider.GetBoundProject();

            this.IsActiveSolutionBound = project != null;
            this.ProjectKey = project?.ProjectKey;
            CurrentMode = this.configurationProvider.GetMode();
        }

        private async void OnActiveSolutionChanged(object sender, EventArgs e)
        {
            await UpdateConnection();

            this.RaiseAnalyzersChangedIfBindingChanged();
            this.errorListInfoBarController.Refresh();
        }

        private async Task UpdateConnection()
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

            var boundProject = this.configurationProvider.GetBoundProject();

            if (boundProject != null)
            {
                var connectionInformation = boundProject.CreateConnectionInformation();
                await SafeServiceCall(async () =>
                    await sonarQubeService.ConnectAsync(connectionInformation, CancellationToken.None));
            }

            Debug.Assert((boundProject != null) == sonarQubeService.IsConnected,
                $"Inconsistent connection state: Solution bound={boundProject != null}, service connected={sonarQubeService.IsConnected}");
        }

        private async Task SafeServiceCall(Func<Task> call)
        {
            try
            {
                await call();
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                sonarLintOutput.WriteLine(string.Format(Strings.SonarQubeRequestFailed, e.Message, innerException?.Message));
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                sonarLintOutput.WriteLine(Strings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception e)
            {
                sonarLintOutput.WriteLine(string.Format(Strings.SonarQubeRequestFailed, e.Message, null));
            }
        }

        private void OnBindingStateChanged(object sender, EventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged();
        }

        private void RaiseAnalyzersChangedIfBindingChanged()
        {
            var boundProject = this.configurationProvider.GetBoundProject();

            bool isSolutionCurrentlyBound = boundProject != null;
            string projectKey = boundProject?.ProjectKey;

            if (this.IsActiveSolutionBound != isSolutionCurrentlyBound ||
                this.ProjectKey != projectKey)
            {
                this.IsActiveSolutionBound = isSolutionCurrentlyBound;
                this.ProjectKey = projectKey;
                CurrentMode = this.configurationProvider.GetMode();

                this.SolutionBindingChanged?.Invoke(this, new ActiveSolutionBindingEventArgs(IsActiveSolutionBound, ProjectKey));
            }
        }

        #region IPartImportsSatisfiedNotification

        public async void OnImportsSatisfied()
        {
            await UpdateConnection();
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
