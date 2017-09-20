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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration
{
    internal class ErrorListInfoBarController : IErrorListInfoBarController, IDisposable
    {
        internal /*for testing purposes*/ static readonly Guid ErrorListToolWindowGuid = new Guid(ToolWindowGuids80.ErrorList);

        private readonly IHost host;
        private readonly ISolutionBindingInformationProvider bindingInformationProvider;
        private IInfoBar currentErrorWindowInfoBar;
        private bool currentErrorWindowInfoBarHandlingClick;
        private BoundSonarQubeProject infoBarBinding;
        private bool isDisposed;

        public ErrorListInfoBarController(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;

            this.bindingInformationProvider = host.GetService<ISolutionBindingInformationProvider>();
            this.bindingInformationProvider.AssertLocalServiceIsNotNull();
        }

        #region IErrorListInfoBarController
        public void Reset()
        {
            this.AssertOnUIThread();

            this.ClearCurrentInfoBar();
        }

        public void Refresh()
        {
            this.AssertOnUIThread();

            // As soon as refresh is called, cancel the ongoing work
            this.CancelQualityProfileProcessing();

            // TODO: part of SVS-72, need to call IProjectSystemFilter.SetTestRegex
            // and specify the regex that you get from IHost.SonarQubeService.GetProperties
            // before calling to ISolutionBindingInformationProvider which will internally
            // use the regex information to determine which projects are filtered and which are not
            // all this needs to be on a background thread!

            // We don't want to slow down solution open, so we delay the processing
            // until idle. There is a possibility that the user might close and open another solution
            // when the delegate will execute, and we should handle those cases
            if (this.IsActiveSolutionBound)
            {
                this.InvokeWhenIdle(this.ProcessSolutionBinding);
            }
        }
        #endregion

        #region Non-public API
        internal /*for testing purposes*/ QualityProfileBackgroundProcessor CurrentBackgroundProcessor
        {
            get;
            private set;
        }

        [Conditional("DEBUG")]
        private void AssertOnUIThread()
        {
            Debug.Assert(this.host.UIDispatcher.CheckAccess(), "The controller needs to run on the UI thread");
        }

        private bool IsActiveSolutionBound
        {
            get
            {
                return this.bindingInformationProvider.IsSolutionBound();
            }
        }

        private void InvokeWhenIdle(Action action)
        {
            Debug.Assert(action != null);

            this.host.UIDispatcher.BeginInvoke(
                  DispatcherPriority.ContextIdle,
                  action);
        }

        private void ClearCurrentInfoBar()
        {
            this.CancelQualityProfileProcessing();

            this.infoBarBinding = null;
            this.currentErrorWindowInfoBarHandlingClick = false;
            if (this.currentErrorWindowInfoBar == null)
            {
                return;
            }

            this.currentErrorWindowInfoBar.Closed -= this.CurrentErrorWindowInfoBar_Closed;
            this.currentErrorWindowInfoBar.ButtonClick -= this.CurrentErrorWindowInfoBar_ButtonClick;

            IInfoBarManager manager = this.host.GetMefService<IInfoBarManager>();
            if (manager == null) // Could be null during shut down
            {
                return;
            }

            manager.DetachInfoBar(this.currentErrorWindowInfoBar);
            this.currentErrorWindowInfoBar = null;
        }

        private void CancelQualityProfileProcessing()
        {
            this.CurrentBackgroundProcessor?.Dispose();
            this.CurrentBackgroundProcessor = null;
        }

        internal /*for testing purposes*/ void ProcessSolutionBinding()
        {
            // No need to do anything if by the time got here the solution was closed (or unbound)
            if (!this.IsActiveSolutionBound)
            {
                return;
            }

            // If the solution is not fully loaded, wait until is fully loaded
            if (!KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive)
            {
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(this.ProcessSolutionBinding);
                return;
            }

            // Due to the non-sequential nature of this code, we want to avoid showing two info bars
            // which could happen if the user had enough time to close and open a solution
            // (after the 1st solution was opened), so need to clear the previous info bar just in case.
            this.ClearCurrentInfoBar();

            this.ProcessSolutionBindingCore();
        }

        private void ProcessSolutionBindingCore()
        {
            this.OutputMessage(Strings.SonarLintCheckingForUnboundProjects);

            Project[] unboundProjects = this.bindingInformationProvider.GetUnboundProjects().ToArray();
            if (unboundProjects.Length > 0)
            {
                this.OutputMessage(Strings.SonarLintFoundUnboundProjects, unboundProjects.Length, String.Join(", ", unboundProjects.Select(p => p.UniqueName)));

                this.UpdateRequired();
            }
            else
            {
                this.OutputMessage(Strings.SonarLintNoUnboundProjectWereFound);

                Debug.Assert(this.CurrentBackgroundProcessor == null);
                this.CurrentBackgroundProcessor = new QualityProfileBackgroundProcessor(this.host);
                this.CurrentBackgroundProcessor.QueueCheckIfUpdateIsRequired(this.UpdateRequired);
            }
        }

        private enum UpdateMessage { OldBindingFile, General };

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="customInfoBarMessage">Optional. If provided than this will be the message that will appear in info bar, otherwise a standard one will appear instead</param>
        private void UpdateRequired(string customInfoBarMessage = null)
        {
            this.AssertOnUIThread();
            IInfoBarManager manager = this.host.GetMefService<IInfoBarManager>();
            if (manager == null)
            {
                Debug.Fail("Cannot find IInfoBarManager");
                return;
            }

            this.currentErrorWindowInfoBar = manager.AttachInfoBar(
                ErrorListToolWindowGuid,
                customInfoBarMessage ?? Strings.SonarLintInfoBarUnboundProjectsMessage,
                Strings.SonarLintInfoBarUpdateCommandText,
                KnownMonikers.RuleWarning);

            if (this.currentErrorWindowInfoBar == null)
            {
                this.OutputMessage(Strings.SonarLintFailedToAttachInfoBarToErrorList);
                Debug.Fail("Failed to add an info bar to the error list tool window");
            }
            else
            {
                TelemetryLoggerAccessor.GetLogger(this.host)?.ReportEvent(TelemetryEvent.ErrorListInfoBarShow);

                this.currentErrorWindowInfoBar.Closed += this.CurrentErrorWindowInfoBar_Closed;
                this.currentErrorWindowInfoBar.ButtonClick += this.CurrentErrorWindowInfoBar_ButtonClick;

                // Need to capture the current binding information since the user can change the binding
                // and running the Update should just no-op in that case.
                var solutionBinding = this.host.GetService<ISolutionBindingSerializer>();
                solutionBinding.AssertLocalServiceIsNotNull();

                this.infoBarBinding = solutionBinding.ReadSolutionBinding();
            }
        }

        private void CurrentErrorWindowInfoBar_Closed(object sender, EventArgs e)
        {
            this.ClearCurrentInfoBar();
        }

        private void CurrentErrorWindowInfoBar_ButtonClick(object sender, EventArgs e)
        {
            if (this.currentErrorWindowInfoBarHandlingClick)
            {
                // Info bar doesn't expose a way to disable the command
                // and since the code is asynchronous the user can click
                // on the button multiple times and get multiple binds
                return;
            }

            // Don't log unprocessed events
            TelemetryLoggerAccessor.GetLogger(this.host)?.ReportEvent(TelemetryEvent.ErrorListInfoBarUpdateCalled);

            var bindingSerialzer = this.host.GetService<ISolutionBindingSerializer>();
            bindingSerialzer.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject binding = bindingSerialzer.ReadSolutionBinding();
            if (binding == null
                || this.infoBarBinding == null
                || binding.ServerUri != this.infoBarBinding.ServerUri
                || !SonarQubeProject.KeyComparer.Equals(binding.ProjectKey, this.infoBarBinding.ProjectKey))
            {
                // Not bound anymore, or bound to something else entirely
                this.ClearCurrentInfoBar();
                this.OutputMessage(Strings.SonarLintInfoBarUpdateCommandInvalidSolutionBindings);
            }
            else
            {
                // Prevent click handling
                this.currentErrorWindowInfoBarHandlingClick = true;
                this.ExecuteUpdate(binding);
            }
        }

        private void ExecuteUpdate(BoundSonarQubeProject binding)
        {
            Debug.Assert(binding != null);

            EventDrivenBindingUpdate binder = new EventDrivenBindingUpdate(this.host, binding);

            EventHandler<BindingRequestResult> onFinished = null;
            onFinished = (o, result) =>
            {
                // Resume click handling (if applicable)
                this.currentErrorWindowInfoBarHandlingClick = false;

                binder.Finished -= onFinished;
                switch (result)
                {
                    case BindingRequestResult.CommandIsBusy:
                        // Might be building/debugging/etc...
                        // Need to click 'Update' again to retry.
                        this.OutputMessage(Strings.SonarLintInfoBarUpdateCommandIsBusyRetry);
                        break;
                    case BindingRequestResult.NoActiveSection:
                        // We drive the process via the active section, we can proceed without it.
                        // Need to click 'Update' again.
                        // This is case is fairly unlikely, so just writing to the output window will be enough
                        this.OutputMessage(Strings.SonarLintInfoBarUpdateCommandRetryNoActiveSection);
                        break;
                    case BindingRequestResult.StartedUpdating:
                    case BindingRequestResult.RequestIsIrrelevant:
                        this.ClearCurrentInfoBar();
                        break;
                    default:
                        Debug.Fail($"Unexpected result: {result}");
                        break;
                }
            };

            binder.Finished += onFinished;
            binder.ConnectAndBind();
        }

        private void OutputMessage(string messageFormat, params object[] args)
        {
            VsShellUtils.WriteToSonarLintOutputPane(this.host, messageFormat, args);
        }

        private enum BindingRequestResult { StartedUpdating, CommandIsBusy, RequestIsIrrelevant, NoActiveSection };

        private class EventDrivenBindingUpdate
        {
            private readonly IHost host;
            private readonly BoundSonarQubeProject binding;

            public EventDrivenBindingUpdate(IHost host, BoundSonarQubeProject binding)
            {
                Debug.Assert(host != null);
                Debug.Assert(binding != null);

                this.host = host;
                this.binding = binding;
            }

            private State.TransferableVisualState State
            {
                get { return this.host.VisualStateManager.ManagedState; }
            }

            private bool IsBusy
            {
                get
                {
                    return this.host.VisualStateManager.IsBusy;
                }
            }

            public event EventHandler<BindingRequestResult> Finished;

            public void ConnectAndBind()
            {
                if (this.host.ActiveSection == null)
                {
                    EventHandler activeSectionChanged = null;
                    activeSectionChanged = (o, e) =>
                    {
                        this.host.ActiveSectionChanged -= activeSectionChanged;
                        this.WhenNotBusy(this.Start);
                    };
                    this.host.ActiveSectionChanged += activeSectionChanged;
                }
                else
                {
                    this.WhenNotBusy(this.Start);
                }

                // Navigating at this point will work in both cases - if there's no active section
                // we will navigate and the ActiveSectionChanged event will be triggered.
                // If there's an active section will navigate (activate TE) and show the connecting/binding process.
                // We drive the process via the ActiveSection, so this step is mandatory
                ITeamExplorerController teController = this.host.GetMefService<ITeamExplorerController>();
                if (teController != null)
                {
                    teController.ShowSonarQubePage();
                }
                else
                {
                    Debug.Fail("Cannot find ITeamExplorerController");
                }

            }

            private void Start()
            {
                if (this.IsConnected(this.binding.ServerUri))
                {
                    // Skip the connection state
                    this.UpdateBinding();
                }
                else
                {
                    // Start from connection state
                    this.RefreshConnection();
                }
            }

            private void RefreshConnection()
            {
                this.WhenNotBusy(this.ExecuteRefreshCommand);
            }

            private void UpdateBinding()
            {
                this.WhenNotBusy(this.ExecuteBindCommand);
            }

            private void ExecuteRefreshCommand()
            {
                if (this.host.ActiveSection == null)
                {
                    this.OnFinished(BindingRequestResult.NoActiveSection);
                    return;
                }

                // If the user was a able to connect to the right server, move on to binding
                if (this.IsConnected(this.binding.ServerUri))
                {
                    // Move to binding state
                    this.UpdateBinding();
                    return;
                }

                if (this.IsConnected() && this.host.ActiveSection.DisconnectCommand.CanExecute(null))
                {
                    this.host.ActiveSection.DisconnectCommand.Execute(null);
                }

                ConnectionInformation connectionInformation = this.binding.CreateConnectionInformation();
                if (this.host.ActiveSection.RefreshCommand.CanExecute(connectionInformation))
                {
                    this.host.ActiveSection.RefreshCommand.Execute(connectionInformation);

                    // Move to binding state
                    this.UpdateBinding();
                }
                else
                {
                    this.OnFinished(BindingRequestResult.CommandIsBusy);
                }
            }

            private void ExecuteBindCommand()
            {
                if (this.host.ActiveSection == null)
                {
                    this.OnFinished(BindingRequestResult.NoActiveSection);
                    return;
                }

                ProjectViewModel boundProject = this.FindProject(this.binding.ServerUri, binding.ProjectKey);
                if (boundProject == null)
                {
                    // The user change binding
                    this.OnFinished(BindingRequestResult.RequestIsIrrelevant);
                    return;
                }

                if (this.host.ActiveSection.BindCommand.CanExecute(boundProject))
                {
                    this.host.ActiveSection.BindCommand.Execute(boundProject);
                    this.OnFinished(BindingRequestResult.StartedUpdating);
                }
                else
                {
                    this.OnFinished(BindingRequestResult.CommandIsBusy);
                }
            }

            private void WhenNotBusy(Action action)
            {
                if (this.IsBusy)
                {
                    EventHandler<bool> isBusyChanged = null;
                    isBusyChanged = (o, isBusy) =>
                    {
                        if (!isBusy)
                        {
                            this.host.VisualStateManager.IsBusyChanged -= isBusyChanged;
                            action();
                        }
                    };
                    this.host.VisualStateManager.IsBusyChanged += isBusyChanged;
                }
                else
                {
                    action();
                }
            }

            private void OnFinished(BindingRequestResult result)
            {
                this.Finished?.Invoke(this, result);
            }

            private bool IsConnected()
            {
                return this.State.ConnectedServers.Any();
            }

            private bool IsConnected(Uri serverUri)
            {
                return this.State.ConnectedServers.Any(s => s.Url == serverUri);
            }

            private ProjectViewModel FindProject(Uri serverUri, string projectKey)
            {
                ServerViewModel serverVM = this.State.ConnectedServers.SingleOrDefault(s => s.Url == serverUri);
                return serverVM?.Projects.SingleOrDefault(p => SonarQubeProject.KeyComparer.Equals(p.Key, projectKey));
            }
        }

        /// <summary>
        /// The class is responsible for quality profile related checks in determining whether
        /// to suggest the user to update his solution with the rule set.
        /// </summary>
        internal /*testing purposes*/ class QualityProfileBackgroundProcessor : IDisposable
        {
            private readonly IHost host;
            private bool isDisposed;

            public QualityProfileBackgroundProcessor(IHost host)
            {
                if (host == null)
                {
                    throw new ArgumentNullException(nameof(host));
                }

                this.host = host;
                this.TokenSource = new CancellationTokenSource();
            }

            internal /*for testing purposes*/ CancellationTokenSource TokenSource
            {
                get;
            }

            internal /*for testing purpose*/ System.Threading.Tasks.Task BackgroundTask
            {
                get;
                private set;
            }

            public void QueueCheckIfUpdateIsRequired(Action<string> updateAction)
            {
                if (updateAction == null)
                {
                    throw new ArgumentNullException(nameof(updateAction));
                }

                Debug.Assert(this.BackgroundTask == null, "Not expecting this method to be called more than once");

                var projectSystem = this.host.GetService<IProjectSystemHelper>();
                projectSystem.AssertLocalServiceIsNotNull();

                IEnumerable<Language> languages = projectSystem.GetFilteredSolutionProjects()
                    .Select(Language.ForProject)
                    .Distinct();

                if (!languages.Any())
                {
                    return;
                }

                var solutionBinding = this.host.GetService<ISolutionBindingSerializer>();
                solutionBinding.AssertLocalServiceIsNotNull();

                var binding = solutionBinding.ReadSolutionBinding();
                if (binding == null)
                {
                    return;
                }

                if (binding.Profiles == null || binding.Profiles.Count == 0)
                {
                    // Old binding, force refresh immediately
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckNoProfiles);
                    updateAction(Strings.SonarLintInfoBarOldBindingFile);
                    return;
                }

                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheck);

                CancellationToken token = this.TokenSource.Token;
                this.BackgroundTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    if (await this.IsUpdateRequiredAsync(binding, languages, token))
                    {
                        this.host.UIDispatcher.BeginInvoke(new Action(() =>
                        {
                            // We mustn't update if was canceled
                            if (!token.IsCancellationRequested)
                            {
                                updateAction(null);
                            }
                        }));
                    }
                }, token);
            }

            private async Task<bool> IsUpdateRequiredAsync(BoundSonarQubeProject binding, IEnumerable<Language> projectLanguages,
                CancellationToken token)
            {
                Debug.Assert(binding != null);

                var newProfiles = await TryGetLatestProfilesAsync(binding, projectLanguages, token); // TODO: Handle errors
                //VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckFailed);
                //return false; // Error, can't proceed

                if (!newProfiles.Keys.All(binding.Profiles.ContainsKey))
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckSolutionRequiresMoreProfiles);
                    return true; // Missing profile, refresh
                }

                foreach (var keyValue in binding.Profiles)
                {
                    Language language = keyValue.Key;
                    ApplicableQualityProfile oldProfileInfo = keyValue.Value;

                    if (!newProfiles.ContainsKey(language))
                    {
                        // Not a relevant profile, we should just ignore it.
                        continue;
                    }

                    QualityProfile newProfileInfo = newProfiles[language];
                    if (this.HasProfileChanged(newProfileInfo, oldProfileInfo))
                    {
                        return true;
                    }
                }

                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckQualityProfileIsUpToDate);
                return false; // Up-to-date
            }

            private bool HasProfileChanged(QualityProfile newProfileInfo, ApplicableQualityProfile oldProfileInfo)
            {
                if (!QualityProfile.KeyComparer.Equals(oldProfileInfo.ProfileKey, newProfileInfo.Key))
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckDifferentProfile);
                    return true; // The profile change to a different one
                }

                if (oldProfileInfo.ProfileTimestamp != newProfileInfo.TimeStamp)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarLintProfileCheckProfileUpdated);
                    return true; // The profile was updated
                }

                return false;
            }

            private async Task<IDictionary<Language, QualityProfile>> TryGetLatestProfilesAsync(BoundSonarQubeProject binding,
                IEnumerable<Language> projectLanguages, CancellationToken token)
            {
                var newProfiles = new Dictionary<Language, QualityProfile>();
                foreach (Language language in projectLanguages)
                {
                    var serverLanguage = language.ToServerLanguage();
                    var profile = await this.host.SonarQubeService.GetQualityProfileAsync(binding.ProjectKey, serverLanguage,
                        token); // TODO: Handle errors

                    newProfiles[language] = profile;
                }

                return newProfiles;
            }

            #region IDisposable Support

            protected virtual void Dispose(bool disposing)
            {
                if (!this.isDisposed)
                {
                    if (disposing)
                    {
                        this.TokenSource.Cancel();
                        this.TokenSource.Dispose();
                    }

                    this.isDisposed = true;
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
            }
            #endregion
        }
        #endregion

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.Reset();
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
