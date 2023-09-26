﻿/*
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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.OLE.Interop;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// A dedicated controller for the <see cref="BindCommand"/>
    /// </summary>
    internal class BindingController : HostedCommandControllerBase, IBindingWorkflowExecutor
    {
        private readonly System.IServiceProvider serviceProvider;
        private readonly IHost host;
        private readonly IBindingWorkflowExecutor workflowExecutor;
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IBindingProcessFactory bindingProcessFactory;
        private readonly IKnownUIContexts knownUIContexts;

        public BindingController(System.IServiceProvider serviceProvider, IHost host)
            : this(serviceProvider, host, null, new KnownUIContextsWrapper())
        {
        }

        internal /*for testing purposes*/ BindingController(System.IServiceProvider serviceProvider, IHost host, IBindingWorkflowExecutor workflowExecutor, IKnownUIContexts knownUIContexts)
            : base(serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.host = host ?? throw new ArgumentNullException(nameof(host));

            this.BindCommand = new RelayCommand<BindCommandArgs>(this.OnBind, this.OnBindStatus);
            this.workflowExecutor = workflowExecutor ?? this;
            this.knownUIContexts = knownUIContexts;

            projectSystemHelper = serviceProvider.GetMefService<IProjectSystemHelper>();
            folderWorkspaceService = serviceProvider.GetMefService<IFolderWorkspaceService>();
            bindingProcessFactory = serviceProvider.GetMefService<IBindingProcessFactory>();
        }

        #region Commands

        public RelayCommand<BindCommandArgs> BindCommand { get; }

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

        private bool OnBindStatus(BindCommandArgs args)
        {
            return args != null
                   && args.ProjectKey != null
                   && host.VisualStateManager.IsConnected
                   && !host.VisualStateManager.IsBusy
                   && (folderWorkspaceService.IsFolderWorkspace()
                       || (knownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive
                           && knownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive
                           && (projectSystemHelper.GetSolutionProjects()?.Any() ?? false)));
        }

        private void OnBind(BindCommandArgs args)
        {
            Debug.Assert(this.OnBindStatus(args));

            this.workflowExecutor.BindProject(args);
        }

        #endregion

        #region IBindingWorkflowExecutor

        void IBindingWorkflowExecutor.BindProject(BindCommandArgs bindingArgs)
        {
            var bindingProcess = CreateBindingProcess(bindingArgs, bindingProcessFactory, host.Logger);
            var workflow = new BindingWorkflow(serviceProvider, host, bindingProcess);

            IProgressEvents progressEvents = workflow.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, bindingArgs);
        }

        internal static /* for testing purposes */ IBindingProcess CreateBindingProcess(BindCommandArgs bindingArgs, IBindingProcessFactory bindingProcessFactory, ILogger logger)
        {
            logger.WriteLine(Strings.Bind_UpdatingNewStyleBinding);

            var bindingProcess = bindingProcessFactory.Create(bindingArgs);
            return bindingProcess;
        }

        internal /*for testing purposes*/ void SetBindingInProgress(IProgressEvents progressEvents, BindCommandArgs bindingArgs)
        {
            this.OnBindingStarted();

            ProgressNotificationListener progressListener = new ProgressNotificationListener(progressEvents, this.host.Logger);
            progressListener.MessageFormat = Strings.BindingSolutionPrefixMessageFormat;

            progressEvents.RunOnFinished(result =>
            {
                progressListener.Dispose();

                this.OnBindingFinished(bindingArgs, result == ProgressControllerResult.Succeeded);
            });
        }

        private void OnBindingStarted()
        {
            this.IsBindingInProgress = true;
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToBindId);
        }

        private void OnBindingFinished(BindCommandArgs bindingArgs, bool isFinishedSuccessfully)
        {
            this.IsBindingInProgress = false;
            this.host.VisualStateManager.ClearBoundProject();

            if (isFinishedSuccessfully)
            {
                this.host.VisualStateManager.SetBoundProject(bindingArgs.Connection.ServerUri, bindingArgs.Connection.Organization?.Key, bindingArgs.ProjectKey);

                VsShellUtils.ActivateSolutionExplorer(serviceProvider);
            }
            else
            {
                IUserNotification notifications = this.host.ActiveSection?.UserNotifications;
                if (notifications != null)
                {
                    // Create a command with a fixed argument with the help of ContextualCommandViewModel that creates proxy command for the contextual (fixed) instance and the passed in ICommand that expects it
                    var rebindCommandVM = new ContextualCommandViewModel(
                        bindingArgs,
                        new RelayCommand<BindCommandArgs>(this.OnBind, this.OnBindStatus));
                    notifications.ShowNotificationError(Strings.FailedToToBindSolution, NotificationIds.FailedToBindId, rebindCommandVM.Command);
                }
            }
        }

        #endregion
    }
}
