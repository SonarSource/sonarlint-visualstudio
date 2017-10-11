/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
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
        private readonly ISolutionBindingInformationProvider solutionBindingInformationProvider;
        private readonly ISonarLintOutput sonarLintOutput;

        public event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;

        public bool IsActiveSolutionBound { get; private set; }

        public string ProjectKey { get; private set; }

        [ImportingConstructor]
        public ActiveSolutionBoundTracker(IHost host, IActiveSolutionTracker activeSolutionTracker,
            ISonarLintOutput sonarLintOutput)
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

            this.solutionBindingInformationProvider = this.extensionHost.GetService<ISolutionBindingInformationProvider>();
            this.solutionBindingInformationProvider.AssertLocalServiceIsNotNull();

            this.errorListInfoBarController = this.extensionHost.GetService<IErrorListInfoBarController>();
            this.errorListInfoBarController.AssertLocalServiceIsNotNull();

            //TODO: check whether the errorListInfobarController needs to be refreshed

            // The user changed the binding through the Team Explorer
            this.extensionHost.VisualStateManager.BindingStateChanged += this.OnBindingStateChanged;

            // The solution changed inside the IDE
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            this.IsActiveSolutionBound = this.solutionBindingInformationProvider.IsSolutionBound();
            this.ProjectKey = this.solutionBindingInformationProvider.GetProjectKey();
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
            if (sonarQubeService.IsConnected &&
                this.extensionHost.ActiveSection?.DisconnectCommand.CanExecute(null) == true)
            {
                this.extensionHost.ActiveSection.DisconnectCommand.Execute(null);
            }

            bool isSolutionCurrentlyBound = this.solutionBindingInformationProvider.IsSolutionBound();

            if (isSolutionCurrentlyBound)
            {
                var connectionInformation = GetConnectionInformation();
                await SafeServiceCall(async () =>
                    await sonarQubeService.ConnectAsync(connectionInformation, CancellationToken.None));
            }
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
                sonarLintOutput.Write(string.Format(Strings.SonarQubeRequestFailed, e.Message, innerException?.Message));
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                sonarLintOutput.Write(Strings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception e)
            {
                sonarLintOutput.Write(string.Format(Strings.SonarQubeRequestFailed, e.Message, null));
            }
        }

        private ConnectionInformation GetConnectionInformation()
        {
            var solutionBindingStorage = this.extensionHost.GetService<ISolutionBindingSerializer>();
            solutionBindingStorage.AssertLocalServiceIsNotNull();

            return solutionBindingStorage
                .ReadSolutionBinding()
                ?.CreateConnectionInformation();
        }

        private void OnBindingStateChanged(object sender, EventArgs e)
        {
            this.RaiseAnalyzersChangedIfBindingChanged();
        }

        private void RaiseAnalyzersChangedIfBindingChanged()
        {
            bool isSolutionCurrentlyBound = this.solutionBindingInformationProvider.IsSolutionBound();
            string projectKey = this.solutionBindingInformationProvider.GetProjectKey();

            if (this.IsActiveSolutionBound != isSolutionCurrentlyBound ||
                this.ProjectKey != projectKey)
            {
                this.IsActiveSolutionBound = isSolutionCurrentlyBound;
                this.ProjectKey = projectKey;

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
