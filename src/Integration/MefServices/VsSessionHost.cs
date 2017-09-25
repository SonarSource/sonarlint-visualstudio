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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IHost))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class VsSessionHost : IHost, IProgressStepRunnerWrapper, IDisposable
    {
        internal /*for testing purposes*/ static readonly Type[] SupportedLocalServices = new Type[]
        {
                typeof(ISolutionRuleSetsInformationProvider),
                typeof(IRuleSetSerializer),
                typeof(ISolutionBindingSerializer),
                typeof(IProjectSystemHelper),
                typeof(ISourceControlledFileSystem),
                typeof(IFileSystem),
                typeof(IConflictsManager),
                typeof(IRuleSetInspector),
                typeof(IRuleSetConflictsController),
                typeof(IProjectSystemFilter),
                typeof(IErrorListInfoBarController),
                typeof(ISolutionBindingInformationProvider)
        };

        private readonly IServiceProvider serviceProvider;
        private readonly IActiveSolutionTracker solutionTacker;
        private readonly IProgressStepRunnerWrapper progressStepRunner;
        private readonly Dictionary<Type, Lazy<ILocalService>> localServices = new Dictionary<Type, Lazy<ILocalService>>();

        private bool isDisposed;
        private bool resetBindingWhenAttaching = true;

        [ImportingConstructor]
        public VsSessionHost([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISonarQubeService sonarQubeService, IActiveSolutionTracker solutionTacker)
            : this(serviceProvider, null, null, sonarQubeService, solutionTacker, Dispatcher.CurrentDispatcher)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be created on the UI thread");
        }

        internal /*for test purposes*/ VsSessionHost(IServiceProvider serviceProvider,
                                    IStateManager state,
                                    IProgressStepRunnerWrapper progressStepRunner,
                                    ISonarQubeService sonarQubeService,
                                    IActiveSolutionTracker solutionTacker,
                                    Dispatcher uiDispatcher)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            if (solutionTacker == null)
            {
                throw new ArgumentNullException(nameof(solutionTacker));
            }

            if (uiDispatcher == null)
            {
                throw new ArgumentNullException(nameof(uiDispatcher));
            }

            this.serviceProvider = serviceProvider;
            this.VisualStateManager = state ?? new StateManager(this, new TransferableVisualState());
            this.progressStepRunner = progressStepRunner ?? this;
            this.UIDispatcher = uiDispatcher;
            this.SonarQubeService = sonarQubeService;
            this.solutionTacker = solutionTacker;
            this.solutionTacker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            this.RegisterLocalServices();
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

        public Dispatcher UIDispatcher { get; }

        public IStateManager VisualStateManager { get; }

        public ISonarQubeService SonarQubeService { get; }

        public ISectionController ActiveSection { get; private set; }

        public ISet<Language> SupportedPluginLanguages { get; } = new HashSet<Language>();

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
                this.ResetBinding(abortCurrentlyRunningWorklows: false);
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
        private void OnActiveSolutionChanged(object sender, EventArgs e)
        {
            // Reset, and abort workflows
            this.ResetBinding(abortCurrentlyRunningWorklows: true);
        }

        private void ResetBinding(bool abortCurrentlyRunningWorklows)
        {
            if (abortCurrentlyRunningWorklows)
            {
                // We may have running workflows, abort them before proceeding
                this.progressStepRunner.AbortAll();
            }

            // Get the binding info (null if there's none i.e. when solution is closed or not bound)
            BoundSonarQubeProject bound = this.SafeReadBindingInformation();
            if (bound == null)
            {
                this.ClearCurrentBinding();
            }
            else
            {
                if (this.ActiveSection == null)
                {
                    // In case the connect section is not active, make it so that next time it activates
                    // it will reset the binding then.
                    this.resetBindingWhenAttaching = true;
                }
                else
                {
                    this.ApplyBindingInformation(bound);
                }
            }
        }

        private void ClearCurrentBinding()
        {
            this.VisualStateManager.BoundProjectKey = null;

            this.VisualStateManager.ClearBoundProject();
        }

        private void ApplyBindingInformation(BoundSonarQubeProject bound)
        {
            // Set the project key that should become bound once the connection workflow has completed
            this.VisualStateManager.BoundProjectKey = bound.ProjectKey;

            // Recreate the connection information from what was persisted
            ConnectionInformation connectionInformation = bound.CreateConnectionInformation();

            Debug.Assert(this.ActiveSection != null, "Expected ActiveSection to be set");
            Debug.Assert(this.ActiveSection?.RefreshCommand != null, "Refresh command is not set");
            // Run the refresh workflow, passing the connection information
            var refreshCmd = this.ActiveSection.RefreshCommand;
            if (refreshCmd.CanExecute(connectionInformation))
            {
                refreshCmd.Execute(connectionInformation); // start the workflow
            }
        }

        private BoundSonarQubeProject SafeReadBindingInformation()
        {
            ISolutionBindingSerializer solutionBinding = this.GetService<ISolutionBindingSerializer>();
            solutionBinding.AssertLocalServiceIsNotNull();

            BoundSonarQubeProject bound = null;
            try
            {
                bound = solutionBinding.ReadSolutionBinding();
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                Debug.Fail("Unexpected exception: " + ex.ToString());
            }

            return bound;
        }
        #endregion

        #region IServiceProvider
        private void RegisterLocalServices()
        {
            this.localServices.Add(typeof(ISolutionRuleSetsInformationProvider), new Lazy<ILocalService>(() => new SolutionRuleSetsInformationProvider(this)));
            this.localServices.Add(typeof(IRuleSetSerializer), new Lazy<ILocalService>(() => new RuleSetSerializer(this)));
            this.localServices.Add(typeof(ISolutionBindingSerializer), new Lazy<ILocalService>(() => new SolutionBindingSerializer(this)));
            this.localServices.Add(typeof(IProjectSystemHelper), new Lazy<ILocalService>(() => new ProjectSystemHelper(this)));
            this.localServices.Add(typeof(IConflictsManager), new Lazy<ILocalService>(() => new ConflictsManager(this)));
            this.localServices.Add(typeof(IRuleSetInspector), new Lazy<ILocalService>(() => new RuleSetInspector(this)));
            this.localServices.Add(typeof(IRuleSetConflictsController), new Lazy<ILocalService>(() => new RuleSetConflictsController(this)));
            this.localServices.Add(typeof(IProjectSystemFilter), new Lazy<ILocalService>(() => new ProjectSystemFilter(this)));
            this.localServices.Add(typeof(ISolutionBindingInformationProvider), new Lazy<ILocalService>(() => new SolutionBindingInformationProvider(this)));
            this.localServices.Add(typeof(IErrorListInfoBarController), new Lazy<ILocalService>(() => new ErrorListInfoBarController(this)));

            // Use Lazy<object> to avoid creating instances needlessly, since the interfaces are serviced by the same instance
            var sccFs = new Lazy<ILocalService>(() => new SourceControlledFileSystem(this));
            this.localServices.Add(typeof(ISourceControlledFileSystem), sccFs);
            this.localServices.Add(typeof(IFileSystem), sccFs);

            Debug.Assert(SupportedLocalServices.Length == this.localServices.Count, "Unexpected number of local services");
            Debug.Assert(SupportedLocalServices.All(t => this.localServices.ContainsKey(t)), "Not all the LocalServices are registered");
        }

        public object GetService(Type type)
        {
            // We don't expect COM types, otherwise the dictionary would have to use a custom comparer
            Lazy<ILocalService> instanceFactory;
            if (typeof(ILocalService).IsAssignableFrom(type) && this.localServices.TryGetValue(type, out instanceFactory))
            {
                return instanceFactory.Value;
            }

            return this.serviceProvider.GetService(type);
        }

        internal void ReplaceInternalServiceForTesting<T>(T instance)
            where T : ILocalService
        {
            if (this.localServices.ContainsKey(typeof(T)))
            {
                this.localServices[typeof(T)] = new Lazy<ILocalService>(() => instance);
            }
        }
        #endregion

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.solutionTacker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;

                    this.localServices.Values
                        .Where(v => v.IsValueCreated)
                        .Select(v => v.Value)
                        .OfType<IDisposable>()
                        .ToList()
                        .ForEach(d => d.Dispose());

                    this.localServices.Clear();
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
