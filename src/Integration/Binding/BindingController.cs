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
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client.CommandTarget;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// A dedicated controller for the <see cref="BindCommand"/>
    /// </summary>
    internal class BindingController : HostedCommandControllerBase, IBindingWorkflowExecutor
    {
        private readonly IHost host;
        private readonly IBindingWorkflowExecutor workflow;
        private readonly IProjectSystemHelper projectSystemHelper;

        public BindingController(IHost host)
            : this(host, null)
        {
        }

        internal /*for testing purposes*/ BindingController(IHost host, IBindingWorkflowExecutor workflow)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;

            this.BindCommand = new RelayCommand<ProjectViewModel>(this.OnBind, this.OnBindStatus);
            this.workflow = workflow ?? this;
            this.projectSystemHelper = this.ServiceProvider.GetService<IProjectSystemHelper>();
            this.projectSystemHelper.AssertLocalServiceIsNotNull();
        }

        #region Commands
        public RelayCommand<ProjectViewModel> BindCommand { get; }

        internal /*for testing purposes*/ bool IsBindingInProgress
        {
            get
            {
                return this.host.VisualStateManager.IsBusy;
            }
            private set
            {
                if (this.host.VisualStateManager.IsBusy != value)
                {
                    this.host.VisualStateManager.IsBusy = value;
                    this.BindCommand.RequeryCanExecute();
                }
            }
        }

        protected override int OnQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Using just as a means that indicates that the status was invalidated and it needs to be recalculate
            // in response to IVsUIShell.UpdateCommandUI which is triggered for the various UI context changes
            this.BindCommand.RequeryCanExecute();

            return base.OnQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private bool OnBindStatus(ProjectViewModel projectVM)
        {
            return this.OnBindStatus(projectVM?.SonarQubeProject);
        }

        private void OnBind(ProjectViewModel projectVM)
        {
            this.OnBind(projectVM?.SonarQubeProject);
        }

        private bool OnBindStatus(SonarQubeProject projectInformation)
        {
            return projectInformation != null
                && this.host.VisualStateManager.IsConnected
                && !this.host.VisualStateManager.IsBusy
                && VsShellUtils.IsSolutionExistsAndFullyLoaded()
                && VsShellUtils.IsSolutionExistsAndNotBuildingAndNotDebugging()
                && (this.projectSystemHelper.GetSolutionProjects()?.Any() ?? false);
        }

        private void OnBind(SonarQubeProject projectInformation)
        {
            Debug.Assert(this.OnBindStatus(projectInformation));

            TelemetryLoggerAccessor.GetLogger(this.host)?.ReportEvent(TelemetryEvent.BindCommandCommandCalled);

            this.workflow.BindProject(projectInformation);
        }

        #endregion

        #region IBindingWorkflowExecutor

        void IBindingWorkflowExecutor.BindProject(SonarQubeProject projectInformation)
        {
            ConnectionInformation connection = this.host.VisualStateManager.GetConnectedServer(projectInformation);
            Debug.Assert(connection != null, "Could not find a connected server for project: " + projectInformation?.Key);

            BindingWorkflow workflowExecutor = new BindingWorkflow(this.host, connection, projectInformation);
            IProgressEvents progressEvents = workflowExecutor.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, projectInformation);
        }

        internal /*for testing purposes*/ void SetBindingInProgress(IProgressEvents progressEvents, SonarQubeProject projectInformation)
        {
            this.OnBindingStarted();

            ProgressNotificationListener progressListener = new ProgressNotificationListener(this.ServiceProvider, progressEvents);
            progressListener.MessageFormat = Strings.BindingSolutionPrefixMessageFormat;

            progressEvents.RunOnFinished(result =>
            {
                progressListener.Dispose();

                this.OnBindingFinished(projectInformation, result == ProgressControllerResult.Succeeded);
            });
        }

        private void OnBindingStarted()
        {
            this.IsBindingInProgress = true;
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToBindId);
        }

        private void OnBindingFinished(SonarQubeProject projectInformation, bool isFinishedSuccessfully)
        {
            this.IsBindingInProgress = false;
            this.host.VisualStateManager.ClearBoundProject();

            if (isFinishedSuccessfully)
            {
                this.host.VisualStateManager.SetBoundProject(projectInformation);

                var conflictsController = this.host.GetService<IRuleSetConflictsController>();
                conflictsController.AssertLocalServiceIsNotNull();

                if (conflictsController.CheckForConflicts())
                {
                    // In some cases we will end up navigating to the solution explorer, this will make sure that
                    // we're back in team explorer to view the conflicts
                    this.ServiceProvider.GetMefService<ITeamExplorerController>()?.ShowSonarQubePage();
                }
                else
                {
                    VsShellUtils.ActivateSolutionExplorer(this.ServiceProvider);
                }
            }
            else
            {
                IUserNotification notifications = this.host.ActiveSection?.UserNotifications;
                if (notifications != null)
                {
                    // Create a command with a fixed argument with the help of ContextualCommandViewModel that creates proxy command for the contextual (fixed) instance and the passed in ICommand that expects it
                    ICommand rebindCommand = new ContextualCommandViewModel(projectInformation, new RelayCommand<SonarQubeProject>(this.OnBind, this.OnBindStatus)).Command;
                    notifications.ShowNotificationError(Strings.FailedToToBindSolution, NotificationIds.FailedToBindId, rebindCommand);
                }
            }
        }
        #endregion
    }
}
