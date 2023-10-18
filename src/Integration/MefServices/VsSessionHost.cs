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
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IHost))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class VsSessionHost : IHost, IProgressStepRunnerWrapper, IDisposable
    {
        private readonly IActiveSolutionTracker solutionTracker;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ISharedBindingConfigProvider sharedBindingConfigProvider;
        private readonly ICredentialStoreService credentialStoreService;

        private readonly IProgressStepRunnerWrapper progressStepRunner;


        private bool isDisposed;
        private bool resetBindingWhenAttaching = true;

        [ImportingConstructor]
        public VsSessionHost(ISonarQubeService sonarQubeService,
            IActiveSolutionTracker solutionTracker,
            IConfigurationProvider configurationProvider,
            ISharedBindingConfigProvider sharedBindingConfigProvider,
            ICredentialStoreService credentialStoreService,
            ILogger logger)
            : this(
                null,
                null,
                sonarQubeService,
                solutionTracker,
                configurationProvider,
                sharedBindingConfigProvider,
                credentialStoreService,
                logger)
        {
        }

        internal /*for test purposes*/ VsSessionHost(IStateManager state,
            IProgressStepRunnerWrapper progressStepRunner,
            ISonarQubeService sonarQubeService,
            IActiveSolutionTracker solutionTracker,
            IConfigurationProvider configurationProvider,
            ISharedBindingConfigProvider sharedBindingConfigProvider,
            ICredentialStoreService credentialStoreService,
            ILogger logger)
        {
            this.VisualStateManager = state ?? new StateManager(this, new TransferableVisualState());
            this.progressStepRunner = progressStepRunner ?? this;
            this.SonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            this.solutionTracker = solutionTracker ?? throw new ArgumentNullException(nameof(solutionTracker));
            this.configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            this.sharedBindingConfigProvider = sharedBindingConfigProvider;
            this.credentialStoreService = credentialStoreService;
            this.solutionTracker.ActiveSolutionChanged += this.OnActiveSolutionChanged;
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IProgressStepRunnerWrapper
        void IProgressStepRunnerWrapper.AbortAll()
        {
            ProgressStepRunner.AbortAll();
        }

        void IProgressStepRunnerWrapper.ChangeHost(IProgressControlHost host)
        {
            ProgressStepRunner.ChangeHost(host);
        }
        #endregion

        #region IHost
        public event EventHandler ActiveSectionChanged;

        public IStateManager VisualStateManager { get; }
        public SharedBindingConfigModel SharedBindingConfig { get; private set; }

        public ISonarQubeService SonarQubeService { get; }

        public Credential GetCredentialsForSharedConfig()
        {
            return SharedBindingConfig == null 
                ? null 
                : credentialStoreService.ReadCredentials(SharedBindingConfig.ServerUri);
        }

        public ISectionController ActiveSection { get; private set; }

        public ILogger Logger { get; }

        public void SetActiveSection(ISectionController section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            Debug.Assert(this.ActiveSection == null, "Already attached. Detach first");
            this.ActiveSection = section;
            this.VisualStateManager.SyncCommandFromActiveSection();

            this.TransferState();

            if (this.resetBindingWhenAttaching)
            {
                this.resetBindingWhenAttaching = false;

                // The connect section activated after the solution is opened,
                // so reset the binding if applicable. No reason to abort since
                // this is the first time after the solution was opened so that
                // we switched to the connect section.
                this.ResetBinding(abortCurrentlyRunningWorklows: false, clearCurrentBinding: false);
            }

            this.OnActiveSectionChanged();
        }

        public void ClearActiveSection()
        {
            if (this.ActiveSection == null) // Can be called multiple times
            {
                return;
            }

            this.ActiveSection.ViewModel.State = null;
            this.ActiveSection = null;
            this.VisualStateManager.SyncCommandFromActiveSection();

            this.OnActiveSectionChanged();
        }

        private void TransferState()
        {
            Debug.Assert(this.ActiveSection != null, "Not attached to any section attached");

            if (this.ActiveSection != null)
            {
                this.ActiveSection.ViewModel.State = this.VisualStateManager.ManagedState;

                IProgressControlHost progressHost = this.ActiveSection.ProgressHost;
                Debug.Assert(progressHost != null, "IProgressControlHost is expected");
                this.progressStepRunner.ChangeHost(progressHost);
            }
        }

        private void OnActiveSectionChanged()
        {
            this.ActiveSectionChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Active solution changed event handler
        private void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs args)
        {
            // TODO: simplifying the eventing model.
            // Both this class and the ActiveSolutionBoundTracker both listen for solution closing events.
            // The ASBT can set the current configuration to Standalone, and this class reads the
            // configuration. However, we can't control the order they receive the "solution closing"
            // notification, so this class might try to read the configuration before it has been
            // updated by the ASBT.
            // To work round this, we have specifically check whether there is an open solution
            // so we do the right thing here.

            // Reset, and abort workflows
            this.ResetBinding(abortCurrentlyRunningWorklows: true, clearCurrentBinding: !args.IsSolutionOpen);
        }

        private void ResetBinding(bool abortCurrentlyRunningWorklows, bool clearCurrentBinding)
        {
            if (abortCurrentlyRunningWorklows)
            {
                // We may have running workflows, abort them before proceeding
                this.progressStepRunner.AbortAll();
            }

            var bindingConfig = this.SafeGetBindingConfig();
            if (clearCurrentBinding || bindingConfig == null || bindingConfig.Mode == SonarLintMode.Standalone)
            {
                this.ClearCurrentBinding();
                SharedBindingConfig = sharedBindingConfigProvider.GetSharedBinding();
                VisualStateManager.BoundProjectKey = this.SharedBindingConfig?.ProjectKey;
            }
            else
            {
                Debug.Assert(bindingConfig.Project != null, "Project should not be null unless in standalone mode");
                if (this.ActiveSection == null)
                {
                    // In case the connect section is not active, make it so that next time it activates
                    // it will reset the binding then.
                    this.resetBindingWhenAttaching = true;
                }
                else
                {
                    this.ApplyBindingInformation(bindingConfig);
                }
            }
        }

        private void ClearCurrentBinding()
        {
            this.VisualStateManager.BoundProjectKey = null;
            this.VisualStateManager.BoundProjectName = null;

            this.VisualStateManager.ClearBoundProject();
        }

        private void ApplyBindingInformation(BindingConfiguration bindingConfig)
        {
            SharedBindingConfig = null;

            // Set the project key that should become bound once the connection workflow has completed
            this.VisualStateManager.BoundProjectKey = bindingConfig.Project.ProjectKey;
            this.VisualStateManager.BoundProjectName = bindingConfig.Project.ProjectName;

            Debug.Assert(this.ActiveSection != null, "Expected ActiveSection to be set");
            Debug.Assert(this.ActiveSection?.RefreshCommand != null, "Refresh command is not set");
            // Run the refresh workflow, passing the connection information
            var refreshCmd = this.ActiveSection.RefreshCommand;

            var connection = bindingConfig.Project.CreateConnectionInformation();
            if (refreshCmd.CanExecute(connection))
            {
                refreshCmd.Execute(connection); // start the workflow
            }
        }

        private BindingConfiguration SafeGetBindingConfig()
        {
            BindingConfiguration bindingConfig = null;
            try
            {
                bindingConfig = configurationProvider.GetConfiguration();
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                Debug.Fail("Unexpected exception: " + ex.ToString());
            }

            return bindingConfig;
        }
        #endregion

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.solutionTracker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
