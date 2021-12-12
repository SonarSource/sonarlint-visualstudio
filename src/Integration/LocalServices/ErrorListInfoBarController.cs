/*
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration
{
    internal sealed class ErrorListInfoBarController : IErrorListInfoBarController, IDisposable
    {
        internal /*for testing purposes*/ static readonly Guid ErrorListToolWindowGuid = new Guid(ToolWindowGuids80.ErrorList);

        private readonly IHost host;
        private readonly IUnboundProjectFinder unboundProjectFinder;
        private readonly ILogger logger;
        private readonly IKnownUIContexts knownUIContexts;
        private readonly IThreadHandling threadHandling;

        private readonly IConfigurationProviderService configProvider;
        private IInfoBar currentErrorWindowInfoBar;
        private bool currentErrorWindowInfoBarHandlingClick;
        private BoundSonarQubeProject infoBarBinding;
        private bool isDisposed;

        public ErrorListInfoBarController(IHost host, IUnboundProjectFinder unboundProjectFinder, ILogger logger)
            : this(host, unboundProjectFinder, logger, new KnownUIContextsWrapper(), new ThreadHandling())
        {
        }

        internal /* for testing */ ErrorListInfoBarController(IHost host, IUnboundProjectFinder unboundProjectFinder, ILogger logger,
            IKnownUIContexts knownUIContexts, IThreadHandling threadHandling)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.unboundProjectFinder = unboundProjectFinder ?? throw new ArgumentNullException(nameof(unboundProjectFinder));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.knownUIContexts = knownUIContexts;
            this.threadHandling = threadHandling ?? throw new ArgumentNullException(nameof(threadHandling));

            this.configProvider = host.GetService<IConfigurationProviderService>();
            this.configProvider.AssertLocalServiceIsNotNull();
        }

        #region IErrorListInfoBarController
        public void Reset()
        {
            threadHandling.ThrowIfNotOnUIThread();

            this.ClearCurrentInfoBar();
        }

        public void Refresh()
        {
            logger.LogDebug("[ErrorListInfoBarController] In Refresh");

            // This is public method, so it's the caller's responsibility to handle uncaught exception
            threadHandling.ThrowIfNotOnUIThread();

            // As soon as refresh is called, cancel the ongoing work
            this.CancelQualityProfileProcessing();

            // TODO: part of SVS-72, need to call IProjectSystemFilter.SetTestRegex
            // and specify the regex that you get from IHost.SonarQubeService.GetProperties
            // before calling to IUnboundProjectFinder which will internally
            // use the regex information to determine which projects are filtered and which are not
            // all this needs to be on a background thread!

            // We don't want to slow down solution open, so we delay the processing
            // until idle. There is a possibility that the user might close and open another solution
            // when the delegate will execute, and we should handle those cases
            if (this.IsActiveSolutionBound)
            {
                logger.LogDebug("[ErrorListInfoBarController] Queuing binding check to run on idle");
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

        private bool IsActiveSolutionBound
        {
            get
            {
                return this.configProvider.GetConfiguration().Mode.IsInAConnectedMode();
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
            ClearCurrentInfoBarAsync().Forget();
        }

        private async Task ClearCurrentInfoBarAsync()
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

            // The service call and detaching the info bar must be done on the UI thread.
            await threadHandling.RunOnUIThread(() =>
            {
                IInfoBarManager manager = this.host.GetMefService<IInfoBarManager>();
                if (manager == null) // Could be null during shut down
                {
                    return;
                }

                manager.DetachInfoBar(this.currentErrorWindowInfoBar);
                this.currentErrorWindowInfoBar = null;
            });
        }

        private void CancelQualityProfileProcessing()
        {
            this.CurrentBackgroundProcessor?.Dispose();
            this.CurrentBackgroundProcessor = null;
        }

        private void ProcessSolutionBinding()
        {
            ProcessSolutionBindingAsync().Forget();
        }

        internal/* for testing */ async Task ProcessSolutionBindingAsync()
        {
            logger.LogDebug("[ErrorListInfoBarController] Processing solution binding...");

            ETW.CodeMarkers.Instance.ErrorListControllerProcessStart();

            await threadHandling.SwitchToBackgroundThread();
            
            try
            {
                threadHandling.ThrowIfOnUIThread();

                // No need to do anything if by the time got here the solution was closed (or unbound)
                var mode = this.configProvider.GetConfiguration().Mode;
                if (!mode.IsInAConnectedMode())
                {
                    return;
                }

                // If the solution is not fully loaded, wait until is fully loaded
                if (!knownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive)
                {
                    knownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(this.ProcessSolutionBinding);
                    return;
                }

                // Due to the non-sequential nature of this code, we want to avoid showing two info bars
                // which could happen if the user had enough time to close and open a solution
                // (after the 1st solution was opened), so need to clear the previous info bar just in case.
                await ClearCurrentInfoBarAsync();

                await ProcessSolutionBindingCoreAsync();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.UnexpectedErrorMessageFormat, typeof(ErrorListInfoBarController), ex, Constants.SonarLintIssuesWebUrl);
            }
            finally
            {
                ETW.CodeMarkers.Instance.ErrorListControllerProcessStop();
            }
        }

        private async Task ProcessSolutionBindingCoreAsync()
        {
            threadHandling.ThrowIfOnUIThread();

            this.OutputMessage(Strings.SonarLintCheckingForUnboundProjects);

            Project[] unboundProjects = this.unboundProjectFinder.GetUnboundProjects().ToArray();
            if (unboundProjects.Length > 0)
            {
                this.OutputMessage(Strings.SonarLintFoundUnboundProjects, unboundProjects.Length, string.Join(", ", unboundProjects.Select(p => p.UniqueName)));

                await threadHandling.RunOnUIThread(() => this.UpdateRequired());
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
            threadHandling.ThrowIfNotOnUIThread();
            IInfoBarManager manager = this.host.GetMefService<IInfoBarManager>();
            if (manager == null)
            {
                Debug.Fail("Cannot find IInfoBarManager");
                return;
            }

            this.currentErrorWindowInfoBar = manager.AttachInfoBarWithButton(
                ErrorListToolWindowGuid,
                customInfoBarMessage ?? Strings.SonarLintInfoBarUnboundProjectsMessage,
                Strings.SonarLintInfoBarUpdateCommandText,
                new SonarLintImageMoniker(KnownMonikers.RuleWarning.Guid, KnownMonikers.RuleWarning.Id));

            if (this.currentErrorWindowInfoBar == null)
            {
                this.OutputMessage(Strings.SonarLintFailedToAttachInfoBarToErrorList);
                Debug.Fail("Failed to add an info bar to the error list tool window");
            }
            else
            {
                this.currentErrorWindowInfoBar.Closed += this.CurrentErrorWindowInfoBar_Closed;
                this.currentErrorWindowInfoBar.ButtonClick += this.CurrentErrorWindowInfoBar_ButtonClick;

                // Need to capture the current binding information since the user can change the binding
                // and running the Update should just no-op in that case.
                this.infoBarBinding = configProvider.GetConfiguration().Project;
            }
        }

        private void CurrentErrorWindowInfoBar_Closed(object sender, EventArgs e)
        {
            // Called on the UI thread -> must handle exceptions
            try
            {
                ClearCurrentInfoBar();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                LogUnexpectedError(ex);
            }
        }

        internal /* for testing */ void CurrentErrorWindowInfoBar_ButtonClick(object sender, EventArgs e)
        {
            // Called on the UI thread -> must handle exceptions
            try
            {
                if (this.currentErrorWindowInfoBarHandlingClick)
                {
                    // Info bar doesn't expose a way to disable the command
                    // and since the code is asynchronous the user can click
                    // on the button multiple times and get multiple binds
                    return;
                }

                BindingConfiguration binding = configProvider.GetConfiguration();
                if (binding == null
                    || !binding.Mode.IsInAConnectedMode()
                    || this.infoBarBinding == null
                    || binding.Project.ServerUri != this.infoBarBinding.ServerUri
                    || !SonarQubeProject.KeyComparer.Equals(binding.Project.ProjectKey, this.infoBarBinding.ProjectKey))
                {
                    // Not bound anymore, or bound to something else entirely
                    this.ClearCurrentInfoBar();
                    this.OutputMessage(Strings.SonarLintInfoBarUpdateCommandInvalidSolutionBindings);
                }
                else
                {
                    // Prevent click handling
                    this.currentErrorWindowInfoBarHandlingClick = true;
                    this.ExecuteUpdate(binding.Project);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                LogUnexpectedError(ex);
            }
        }

        private void LogUnexpectedError(Exception ex) =>
            logger.WriteLine(Strings.UnexpectedErrorMessageFormat, typeof(ErrorListInfoBarController), ex, Constants.SonarLintIssuesWebUrl);

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
            this.host.Logger.WriteLine(messageFormat, args);
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

                BindCommandArgs bindingArgs = new BindCommandArgs(boundProject.Project?.Key, boundProject.Project?.Name, binding.CreateConnectionInformation());
                if (this.host.ActiveSection.BindCommand.CanExecute(bindingArgs))
                {
                    this.host.ActiveSection.BindCommand.Execute(bindingArgs);
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
                var matchingServers = this.State.ConnectedServers.Where(s => s.Url == serverUri).ToList();

                if (matchingServers.Count == 0)
                {
                    return null;
                }
                else if (matchingServers.Count > 1)
                {
                    Debug.Fail($"Not expecting to find multiple connected servers with url '{serverUri}'");
                    return null;
                }

                var matchingProjects = matchingServers[0]?.Projects
                    .Where(p => SonarQubeProject.KeyComparer.Equals(p.Key, projectKey)).ToList();

                if (matchingProjects == null || matchingProjects.Count == 0)
                {
                    return null;
                }
                else if (matchingProjects.Count > 1)
                {
                    Debug.Fail($"Not expecting to find multiple projects with kye '{projectKey}' on url '{serverUri}'");
                    return null;
                }
                else
                {
                    return matchingProjects[0];
                }
            }
        }

        /// <summary>
        /// The class is responsible for quality profile related checks in determining whether
        /// to suggest the user to update his solution with the rule set.
        /// </summary>
        internal /*testing purposes*/ sealed class QualityProfileBackgroundProcessor : IDisposable
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

                var configProvider = this.host.GetService<IConfigurationProviderService>();
                configProvider.AssertLocalServiceIsNotNull();

                var bindingConfig = configProvider.GetConfiguration();

                if (!bindingConfig.Mode.IsInAConnectedMode())
                {
                    return;
                }
                Debug.Assert(bindingConfig.Project != null, "Bound project should not be null if in legacy connected mode");

                var projectSystem = this.host.GetService<IProjectSystemHelper>();
                projectSystem.AssertLocalServiceIsNotNull();

                var projectToLanguageMapper = host.GetMefService<IProjectToLanguageMapper>();

                IEnumerable<Language> languages = projectSystem.GetFilteredSolutionProjects()
                    .SelectMany(projectToLanguageMapper.GetAllBindingLanguagesForProject)
                    .Distinct()
                    .ToArray();

                if (!languages.Any())
                {
                    return;
                }

                if (bindingConfig.Project.Profiles == null || bindingConfig.Project.Profiles.Count == 0)
                {
                    // Old binding, force refresh immediately
                    this.host.Logger.WriteLine(Strings.SonarLintProfileCheckNoProfiles);
                    updateAction(Strings.SonarLintInfoBarOldBindingFile);
                    return;
                }

                this.host.Logger.WriteLine(Strings.SonarLintProfileCheck);

                CancellationToken token = this.TokenSource.Token;
                this.BackgroundTask = System.Threading.Tasks.Task.Run(() =>
                {
                    if (this.IsUpdateRequired(bindingConfig.Project, languages, token))
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

            private bool IsUpdateRequired(BoundSonarQubeProject binding, IEnumerable<Language> projectLanguages,
                CancellationToken token)
            {
                Debug.Assert(binding != null);

                IDictionary<Language, SonarQubeQualityProfile> newProfiles = null;
                try
                {
                    newProfiles = TryGetLatestProfilesAsync(binding, projectLanguages, token).GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    this.host.Logger.WriteLine(Strings.SonarLintProfileCheckFailed);
                    return false; // Error, can't proceed
                }

                if (!newProfiles.Keys.All(binding.Profiles.ContainsKey))
                {
                    this.host.Logger.WriteLine(Strings.SonarLintProfileCheckSolutionRequiresMoreProfiles);
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

                    SonarQubeQualityProfile newProfileInfo = newProfiles[language];
                    if (this.HasProfileChanged(newProfileInfo, oldProfileInfo))
                    {
                        return true;
                    }
                }

                this.host.Logger.WriteLine(Strings.SonarLintProfileCheckQualityProfileIsUpToDate);
                return false; // Up-to-date
            }

            private bool HasProfileChanged(SonarQubeQualityProfile newProfileInfo, ApplicableQualityProfile oldProfileInfo)
            {
                if (!SonarQubeQualityProfile.KeyComparer.Equals(oldProfileInfo.ProfileKey, newProfileInfo.Key))
                {
                    this.host.Logger.WriteLine(Strings.SonarLintProfileCheckDifferentProfile);
                    return true; // The profile change to a different one
                }

                if (oldProfileInfo.ProfileTimestamp != newProfileInfo.TimeStamp)
                {
                    this.host.Logger.WriteLine(Strings.SonarLintProfileCheckProfileUpdated);
                    return true; // The profile was updated
                }

                return false;
            }

            private async Task<IDictionary<Language, SonarQubeQualityProfile>> TryGetLatestProfilesAsync(BoundSonarQubeProject binding,
                IEnumerable<Language> projectLanguages, CancellationToken token)
            {
                var newProfiles = new Dictionary<Language, SonarQubeQualityProfile>();
                foreach (Language language in projectLanguages)
                {
                    var serverLanguage = language.ServerLanguage;
                    Debug.Assert(serverLanguage != null, $"Not expecting the server language to be null. Language id: {language.Id}");
                    newProfiles[language] = await this.host.SonarQubeService.GetQualityProfileAsync(binding.ProjectKey,
                        binding.Organization?.Key, serverLanguage, token);
                }

                return newProfiles;
            }

            #region IDisposable Support

            private void Dispose(bool disposing)
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
        private void Dispose(bool disposing)
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
