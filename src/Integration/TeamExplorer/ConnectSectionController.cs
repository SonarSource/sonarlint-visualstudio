//-----------------------------------------------------------------------
// <copyright file="ConnectSectionController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ConnectSectionController : IServiceProvider, IDisposable
    {
        private readonly ISonarQubeServiceWrapper sonarQubeService;
        private readonly Dispatcher uiDispatcher;
        private readonly IServiceProvider serviceProvider;
        private readonly IActiveSolutionTracker solutionTacker;

        private bool isDisposed;
        private bool resetBindingWhenAttaching = true;
        private string boundSonarQubeProjectKey;

        [ImportingConstructor]
        public ConnectSectionController([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, SonarQubeServiceWrapper sonarQubeService, IActiveSolutionTracker solutionTacker)
            : this(serviceProvider, new TransferableVisualState(), sonarQubeService, solutionTacker, Dispatcher.CurrentDispatcher)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected to be created on the UI thread");
        }

        internal /*for test purposes*/ ConnectSectionController(IServiceProvider serviceProvider,
                                    TransferableVisualState state,
                                    ISonarQubeServiceWrapper sonarQubeService,
                                    IActiveSolutionTracker solutionTacker,
                                    Dispatcher uiDispatcher)
        {
           if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            this.serviceProvider = serviceProvider;
            this.State = state;
            this.State.PropertyChanged += this.OnStatePropertyChanged;
            this.uiDispatcher = uiDispatcher;
            this.sonarQubeService = sonarQubeService;
            this.solutionTacker = solutionTacker;
            this.solutionTacker.ActiveSolutionChanged += this.OnActiveSolutionChanged;

            this.SetConnectCommand();
            this.SetBindCommand();
            this.RefreshCommand = new RelayCommand(this.ExecRefresh, this.CanExecRefresh);
            this.DisconnectCommand = new RelayCommand(this.Disconnect, this.CanDisconnect);
            this.ToggleShowAllProjectsCommand = new RelayCommand<ServerViewModel>(this.ToggleShowAllProjects, this.CanToggleShowAllProjects);
        }

        #region State
        internal TransferableVisualState State
        {
            get;
        }

        private void OnStatePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.State.IsBusy))
            {
                ConnectSectionViewModel vm = this.AttachedSection?.ViewModel;
                if (vm != null)
                {
                    vm.IsBusy = this.State.IsBusy;
                }
            }
        }
        #endregion

        #region Notifications
#pragma warning disable S2333 // Redundant modifiers should be removed, Justification: for test purposes
        /// <summary>
        /// API to notify the user. Can be null when we're not supposed to notify the user.
        /// </summary>
        protected virtual IUserNotification Notification
#pragma warning restore S2333 // Redundant modifiers should be removed
        {
            get
            {
                return this.AttachedSection?.ViewModel;
            }
        }
        #endregion

        #region Initialization
        internal /*for testing purposes*/ void SetConnectCommand(ConnectCommand cmd = null)
        {
            if (this.ConnectCommand != null)
            {
                this.ConnectCommand.ProjectsChanged -= this.SetProjects;
            }

            this.ConnectCommand = cmd ?? new ConnectCommand(this, this.sonarQubeService);
            this.ConnectCommand.ProjectsChanged += this.SetProjects;
        }

        internal /*for testing purposes*/ void SetBindCommand(BindCommand cmd = null)
        {
            this.BindCommand = cmd ?? new BindCommand(this, this.sonarQubeService);
        }
        #endregion  

        #region Controller API

        internal IConnectSection AttachedSection
        {
            get;
            private set;
        }

        public void ClearBoundProject()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(ClearBoundProject)} should only be accessed from the UI thread");
            this.ClearBindingErrorNotifications();
            this.State.ClearBoundProject();
        }

        public void SetBoundProject(ProjectViewModel projectViewModel)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(SetBoundProject)} should only be accessed from the UI thread");
            this.ClearBindingErrorNotifications();
            this.State.SetBoundProject(projectViewModel);
        }

        public void Attach(IConnectSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            Debug.Assert(this.AttachedSection == null, "Already attached. Detach first");

            this.AttachedSection = section;
            this.AttachViewModel(section.ViewModel);
            this.AttachView((IProgressControlHost)section.View);

            this.LoadState();

            if (this.resetBindingWhenAttaching)
            {
                this.resetBindingWhenAttaching = false;

                // The connect section activated after the solution is opened, 
                // so reset the binding if applicable. No reason to abort since
                // this is the first time after the solution was opened so that 
                // we switched to the connect section.
                this.ResetBinding(abortCurrentlyRunningWorklows: false);
            }
        }

        public void Detach(IConnectSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            if (this.AttachedSection == null) // Can be called multiple times
            {
                return;
            }

            this.SaveState();
            this.AttachedSection.ViewModel.State = null;
            this.DetachView();
            this.DetachViewModel();
            this.AttachedSection = null;
        }

        private void LoadState()
        {
            Debug.Assert(this.AttachedSection != null, "Not attached to any section attached");

            if (this.AttachedSection != null)
            {
                this.AttachedSection.ViewModel.State = this.State;

                IProgressControlHost progressHost = this.AttachedSection.View as IProgressControlHost;
                Debug.Assert(progressHost != null, "View is expected to implement IProgressControlHost");
                ProgressStepRunner.ChangeHost(progressHost);
            }
        }

        private void SaveState()
        {
            Debug.Assert(this.AttachedSection != null, "Not attached to any section attached");
            // All the state (ConnectedServers) is currently on the controller
            // or defined elsewhere, so just verifying that at this point
            if (this.AttachedSection != null)
            {
                Debug.Assert(ReferenceEquals(this.State, this.AttachedSection.ViewModel.State), "Broken invariant - the connected servers are different!");
            }
        }

        #endregion

        #region Commands
        internal /*for testing purposes*/ ConnectCommand ConnectCommand
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ BindCommand BindCommand
        {
            get;
            private set;
        }

        internal /*for testing purposes*/ ICommand RefreshCommand
        {
            get;
        }

        internal /*for testing purposes*/ ICommand DisconnectCommand
        {
            get;
        }

        internal /*for testing purposes*/ ICommand ToggleShowAllProjectsCommand
        {
            get;
        }

        private bool CanExecRefresh()
        {
            return !this.State.IsBusy
                && this.sonarQubeService.CurrentConnection != null;
        }

        private void ExecRefresh()
        {
            Debug.Assert(this.CanExecRefresh());
            // Any existing connection will be disconnected and disposed, so create a copy and use it to connect
            this.ConnectCommand.EstablishConnection(this.sonarQubeService.CurrentConnection.Clone());
        }

        private bool CanDisconnect()
        {
            return this.sonarQubeService.CurrentConnection != null;
        }

        private void Disconnect()
        {
            Debug.Assert(this.CanDisconnect());
            var previous = this.sonarQubeService.CurrentConnection;
            this.sonarQubeService.Disconnect();
            this.SetProjectsUIThread(previous, null);
        }

        private bool CanToggleShowAllProjects(ServerViewModel server)
        {
            return server.Projects.Any(x => x.IsBound);
        }

        private void ToggleShowAllProjects(ServerViewModel server)
        {
            server.ShowAllProjects = !server.ShowAllProjects;
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
                ProgressStepRunner.AbortAll();
            }

            // Get the binding info (null if there's none i.e. when solution is closed or not bound)
            BoundSonarQubeProject bound = this.SafeReadBindingInformation();
            if (bound == null)
            {
                this.ClearCurrentBinding();
            }
            else
            {
                if (this.AttachedSection == null)
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
            this.boundSonarQubeProjectKey = null;

            if (this.CanDisconnect())
            {
                this.Disconnect();
            }
        }

        private void ApplyBindingInformation(BoundSonarQubeProject bound)
        {
            Debug.Assert(bound != null);
            Debug.Assert(bound.ServerUri != null, "Will not be able to apply binding without server uri");

            // Set the project key that should become bound once the connection workflow has completed
            this.boundSonarQubeProjectKey = bound.ProjectKey;

            // Recreate the connection information from what was persisted
            ConnectionInformation connectionInformation = bound.Credentials == null ?
                new ConnectionInformation(bound.ServerUri)
                : bound.Credentials.CreateConnectionInformation(bound.ServerUri);

            // Run the connect workflow
            this.ConnectCommand.EstablishConnection(connectionInformation); // start the workflow
        }

        private BoundSonarQubeProject SafeReadBindingInformation()
        {
            BoundSonarQubeProject bound = null;
            try
            {
                SolutionBinding binding = new SolutionBinding(this.serviceProvider);
                bound = binding.ReadSolutionBinding();
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

        #region Workflow event handler
#pragma warning disable S2333 // Redundant modifiers should be removed, Justification: for test purposes
        protected virtual void SetProjects(object sender, ConnectedProjectsEventArgs args)
#pragma warning restore S2333 // Redundant modifiers should be removed
        {
            if (this.uiDispatcher.CheckAccess())
            {
                this.SetProjectsUIThread(args.Connection, args.Projects);
            }
            else
            {
                this.uiDispatcher.BeginInvoke(new Action(() => this.SetProjectsUIThread(args.Connection, args.Projects)));
            }
        }

        private void ClearBindingErrorNotifications()
        {
            this.Notification?.HideNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        private void SetProjectsUIThread(ConnectionInformation connection, IEnumerable<ProjectInformation> projects)
        {
            Debug.Assert(connection != null);
            this.ClearBindingErrorNotifications();

            // !!! Avoid using the service to detect disconnects since it's not thread safe !!!
            if (projects == null)
            {
                // Disconnected, clear all
                this.ClearBoundProject();
                this.State.ConnectedServers.Clear();
            }
            else
            {
                var existingServerVM = this.State.ConnectedServers.SingleOrDefault(serverVM => serverVM.Url == connection.ServerUri);
                ServerViewModel serverViewModel;
                if (existingServerVM == null)
                {
                    // Add new server
                    serverViewModel = new ServerViewModel(connection);
                    this.AddServerVMCommands(serverViewModel);
                    this.State.ConnectedServers.Add(serverViewModel);
                }
                else
                {
                    // Update existing server
                    serverViewModel = existingServerVM;
                }

                serverViewModel.SetProjects(projects);
                Debug.Assert(serverViewModel.ShowAllProjects, "ShowAllProjects should have been set");
                this.SetProjectVMCommands(serverViewModel);
                this.RestoreBoundProject(serverViewModel);
            }
        }

        private void RestoreBoundProject(ServerViewModel serverViewModel)
        {
            if (this.boundSonarQubeProjectKey == null)
            {
                // Nothing to restore
                return;
            }

            // Ordinal comparer should be good enough: http://docs.sonarqube.org/display/SONAR/Project+Administration#ProjectAdministration-AddingaProject 
            ProjectViewModel boundProjectVM = serverViewModel.Projects.FirstOrDefault(pvm => StringComparer.Ordinal.Equals(pvm.Key, this.boundSonarQubeProjectKey));
            if (boundProjectVM == null)
            {
                // Defensive coding: invoked async and it's safer to assume that value could be null
                // and just not do anything since if they are null it means that there's no solution open.
                this.Notification?.ShowNotificationError(string.Format(CultureInfo.CurrentCulture, Strings.BoundProjectNotFound, this.boundSonarQubeProjectKey), NotificationIds.FailedToFindBoundProjectKeyId, null);
            }
            else
            {
                this.SetBoundProject(boundProjectVM);
            }
        }

        private void AddServerVMCommands(ServerViewModel serverVM)
        {
            var refreshContextualCommand = new ContextualCommandViewModel(serverVM, this.RefreshCommand)
            {
                DisplayText = Strings.RefreshCommandDisplayText,
                Tooltip = Strings.RefreshCommandTooltip,
                Icon = new IconViewModel(KnownMonikers.Refresh)
            };

            var disconnectContextualCommand = new ContextualCommandViewModel(serverVM, this.DisconnectCommand)
            {
                DisplayText = Strings.DisconnectCommandDisplayText,
                Tooltip = Strings.DisconnectCommandTooltip,
                Icon = new IconViewModel(KnownMonikers.Disconnect)
            };

            var toggleShowAllProjectsCommand = new ContextualCommandViewModel(serverVM, this.ToggleShowAllProjectsCommand)
            {
                Tooltip = Strings.ToggleShowAllProjectsCommandTooltip
            };
            toggleShowAllProjectsCommand.SetDynamicDisplayText(x =>
            {
                ServerViewModel ctx = x as ServerViewModel;
                Debug.Assert(ctx != null, "Unexpected fixed context for ToggleShowAllProjects context command");
                return ctx?.ShowAllProjects ?? false ? Strings.HideUnboundProjectsCommandText : Strings.ShowAllProjectsCommandText;
            });

            serverVM.Commands.Add(refreshContextualCommand);
            serverVM.Commands.Add(disconnectContextualCommand);
            serverVM.Commands.Add(toggleShowAllProjectsCommand);
        }

        private void SetProjectVMCommands(ServerViewModel serverVM)
        {
            foreach (ProjectViewModel projectVM in serverVM.Projects)
            {
                Debug.Assert(projectVM.Commands.Count == 0, "Not expecting project commands, otherwise would Clear the collection first");

                var bindContextCommand = new ContextualCommandViewModel(projectVM, this.BindCommand.WpfCommand);
                bindContextCommand.SetDynamicDisplayText(x =>
                {
                    var ctx = x as ProjectViewModel;
                    Debug.Assert(ctx != null, "Unexpected fixed context for bind context command");
                    return ctx?.IsBound ?? false ? Strings.SyncButtonText : Strings.BindButtonText;
                });
                bindContextCommand.SetDynamicIcon(x =>
                {
                    var ctx = x as ProjectViewModel;
                    Debug.Assert(ctx != null, "Unexpected fixed context for bind context command");
                    return new IconViewModel(ctx?.IsBound ?? false ? KnownMonikers.Sync : KnownMonikers.Link);
                });

                projectVM.Commands.Add(bindContextCommand);
            }
        }
        #endregion

        #region State management

        private void AttachView(IProgressControlHost view)
        {
            Debug.Assert(view != null);

            this.ConnectCommand.ProgressControlHost = view;
            this.BindCommand.ProgressControlHost = view;
        }

        private void DetachView()
        {
            this.ConnectCommand.ProgressControlHost = null;
            this.BindCommand.ProgressControlHost = null;
        }

        private void AttachViewModel(ConnectSectionViewModel vm)
        {
            Debug.Assert(vm != null);

            vm.ConnectCommand = this.ConnectCommand.WpfCommand;
            vm.BindCommand = this.BindCommand.WpfCommand;
            this.ConnectCommand.UserNotification = vm;
            this.BindCommand.UserNotification = vm;
        }

        private void DetachViewModel()
        {
            this.ConnectCommand.UserNotification = null;
            this.BindCommand.UserNotification = null;
        }
        #endregion

        #region IServiceProvider
        public object GetService(Type type)
        {
            return this.serviceProvider.GetService(type);
        }
        #endregion

        #region IDisposable Support

#pragma warning disable S2333 // Redundant modifiers should be removed, Justification: for test purposes
        protected virtual void Dispose(bool disposing)
#pragma warning restore S2333 // Redundant modifiers should be removed
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.State.PropertyChanged -= this.OnStatePropertyChanged;
                    this.ConnectCommand.ProjectsChanged -= this.SetProjects;
                    this.solutionTacker.ActiveSolutionChanged -= this.OnActiveSolutionChanged;
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
