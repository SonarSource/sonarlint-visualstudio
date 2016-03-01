//-----------------------------------------------------------------------
// <copyright file="BindCommand.cs" company="SonarSource SA and Microsoft Corporation">
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

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class BindCommand : HostedCommandBase, IBindingWorkflow
    {
        private readonly IBindingWorkflow workflow;
        private readonly IProjectSystemHelper projectSystemHelper;

        public BindCommand(ConnectSectionController controller, ISonarQubeServiceWrapper sonarQubeService)
            :this(controller, sonarQubeService, null, null)
        {
        }

        internal /*for testing purposes*/ BindCommand(ConnectSectionController controller, ISonarQubeServiceWrapper sonarQubeService, IBindingWorkflow workflow, IProjectSystemHelper projectSystemHelper)
            : base(controller)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            this.Controller = controller;
            this.SonarQubeService = sonarQubeService;
            this.WpfCommand = new RelayCommand<ProjectViewModel>(this.ExecuteBind, this.CanExecuteBind);
            this.workflow = workflow ?? this;
            this.projectSystemHelper = projectSystemHelper ?? new ProjectSystemHelper(this.Controller);
        }

        public RelayCommand<ProjectViewModel> WpfCommand
        {
            get;
        }

        internal ISonarQubeServiceWrapper SonarQubeService
        {
            get;
        }

        internal ConnectSectionController Controller
        {
            get;
        }

        #region Command
        internal bool IsBindingInProgress
        {
            get
            {
                return this.Controller.State.IsBusy;
            }
            private set
            {
                if (this.Controller.State.IsBusy != value)
                {
                    this.Controller.State.IsBusy = value;
                    this.WpfCommand.RequeryCanExecute();
                }
            }
        }

        protected override int OnQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Using just as a means that indicates that the status was invalidated and it needs to be recalculate
            // in response to IVsUIShell.UpdateCommandUI which is triggered for the various UI context changes
            this.WpfCommand.RequeryCanExecute();

            return base.OnQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "S1067:Expressions should not be too complex", 
            Justification = "We need all those conditions to determine whether the command is enabled", 
            Scope = "member", 
            Target = "~M:SonarLint.VisualStudio.Integration.Binding.BindCommand.CanExecuteBind(SonarLint.VisualStudio.Integration.TeamExplorer.ProjectViewModel)~System.Boolean")]
        private bool CanExecuteBind(ProjectViewModel projectVM)
        {
            return this.SonarQubeService.CurrentConnection != null
                && !this.Controller.State.IsBusy
                && this.ProgressControlHost != null
                && projectVM?.ProjectInformation != null
                && VsShellUtils.IsSolutionExistsAndNotBuildingAndNotDebugging()
                && (this.projectSystemHelper.GetSolutionManagedProjects()?.Any() ?? false);
        }

        private void ExecuteBind(ProjectViewModel projectVM)
        {
            Debug.Assert(this.CanExecuteBind(projectVM));
            this.workflow.BindProject(projectVM);
        }
        #endregion

        #region IBindingWorkflow

        void IBindingWorkflow.BindProject(ProjectViewModel projectVM)
        {
            BindingWorkflow workflowExecutor = new BindingWorkflow(this, projectVM.ProjectInformation);
            IProgressEvents progressEvents = workflowExecutor.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, projectVM);
        }

        internal /*for testing purposes*/ void SetBindingInProgress(IProgressEvents progressEvents, ProjectViewModel projectVM)
        {
            this.OnBindingStarted();

            ProgressNotificationListener progressListener = new ProgressNotificationListener(this.ServiceProvider, progressEvents);
            progressListener.MessageFormat = Strings.BindingSolutionPrefixMessageFormat;

            progressEvents.RunOnFinished(r =>
            {
                progressListener.Dispose();

                this.OnBindingFinished(projectVM, r == ProgressControllerResult.Succeeded);
            });
        }

        private void OnBindingStarted()
        {
            this.IsBindingInProgress = true;
            this.UserNotification?.HideNotification(NotificationIds.FailedToBindId);
        }

        private void OnBindingFinished(ProjectViewModel projectVM, bool isFinishedSuccessfully)
        {
            this.IsBindingInProgress = false;
            this.Controller.ClearBoundProject();

            if (isFinishedSuccessfully)
            {
                this.Controller.SetBoundProject(projectVM);
                VsShellUtils.ActivateSolutionExplorer(this.ServiceProvider);
            }
            else
            {
                this.UserNotification?.ShowNotificationError(Strings.FailedToToBindSolution,
                    NotificationIds.FailedToBindId,
                    new ContextualCommandViewModel(projectVM, this.WpfCommand).Command);
            }
        }
        #endregion
    }
}
