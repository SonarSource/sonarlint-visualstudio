//-----------------------------------------------------------------------
// <copyright file="BindingController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

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
            : this(host, null, null)
        {
        }

        internal /*for testing purposes*/ BindingController(IHost host, IBindingWorkflowExecutor workflow, IProjectSystemHelper projectSystemHelper)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;

            this.BindCommand = new RelayCommand<ProjectViewModel>(this.OnBind, this.OnBindStatus);
            this.workflow = workflow ?? this;
            this.projectSystemHelper = projectSystemHelper ?? new ProjectSystemHelper(this.host);
        }

        public RelayCommand<ProjectViewModel> BindCommand
        {
            get;
        }

        #region Command
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
            return this.OnBindStatus(projectVM?.ProjectInformation);
        }

        private void OnBind(ProjectViewModel projectVM)
        {
            this.OnBind(projectVM?.ProjectInformation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "S1067:Expressions should not be too complex",
            Justification = "We need all those conditions to determine whether the command is enabled",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.Binding.BindCommand.OnBindStatus(SonarLint.VisualStudio.Integration.Service.ProjectInformation)~System.Boolean")]
        private bool OnBindStatus(ProjectInformation projectInformation)
        {
            return projectInformation != null
                && this.host.SonarQubeService.CurrentConnection != null
                && !this.host.VisualStateManager.IsBusy
                && VsShellUtils.IsSolutionExistsAndFullyLoaded()
                && VsShellUtils.IsSolutionExistsAndNotBuildingAndNotDebugging()
                && (this.projectSystemHelper.GetSolutionManagedProjects()?.Any() ?? false);
        }

        private void OnBind(ProjectInformation projectInformation)
        {
            Debug.Assert(this.OnBindStatus(projectInformation));
            this.workflow.BindProject(projectInformation);
        }
        #endregion

        #region IBindingWorkflowExecutor

        void IBindingWorkflowExecutor.BindProject(ProjectInformation projectInformation)
        {
            BindingWorkflow workflowExecutor = new BindingWorkflow(this.host, projectInformation);
            IProgressEvents progressEvents = workflowExecutor.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, projectInformation);
        }

        internal /*for testing purposes*/ void SetBindingInProgress(IProgressEvents progressEvents, ProjectInformation projectInformation)
        {
            this.OnBindingStarted();

            ProgressNotificationListener progressListener = new ProgressNotificationListener(this.ServiceProvider, progressEvents);
            progressListener.MessageFormat = Strings.BindingSolutionPrefixMessageFormat;

            progressEvents.RunOnFinished(r =>
            {
                progressListener.Dispose();

                this.OnBindingFinished(projectInformation, r == ProgressControllerResult.Succeeded);
            });
        }

        private void OnBindingStarted()
        {
            this.IsBindingInProgress = true;
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToBindId);
        }

        private void OnBindingFinished(ProjectInformation projectInformation, bool isFinishedSuccessfully)
        {
            this.IsBindingInProgress = false;
            this.host.VisualStateManager.ClearBoundProject();

            if (isFinishedSuccessfully)
            {
                this.host.VisualStateManager.SetBoundProject(projectInformation);
                VsShellUtils.ActivateSolutionExplorer(this.ServiceProvider);
            }
            else
            {
                IUserNotification notifications = this.host.ActiveSection?.UserNotifications;
                if (notifications !=null)
                {
                    // Create a command with a fixed argument with the help of ContextualCommandViewModel that creates proxy command for the contextual (fixed) instance and the passed in ICommand that expects it
                    ICommand rebindCommand = new ContextualCommandViewModel(projectInformation, new RelayCommand<ProjectInformation>(this.OnBind, this.OnBindStatus)).Command;
                    notifications.ShowNotificationError(Strings.FailedToToBindSolution, NotificationIds.FailedToBindId, rebindCommand);
                }
            }
        }
        #endregion
    }
}
