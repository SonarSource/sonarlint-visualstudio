/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
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
        private readonly IHost host;
        private readonly IBindingWorkflowExecutor workflowExecutor;
        private readonly IProjectSystemHelper projectSystemHelper;

        public BindingController(IHost host)
            : this(host, null)
        {
        }

        internal /*for testing purposes*/ BindingController(IHost host, IBindingWorkflowExecutor workflowExecutor)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;

            this.BindCommand = new RelayCommand<BindCommandArgs>(this.OnBind, this.OnBindStatus);
            this.workflowExecutor = workflowExecutor ?? this;
            this.projectSystemHelper = this.host.GetService<IProjectSystemHelper>();
            this.projectSystemHelper.AssertLocalServiceIsNotNull();
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
                && this.host.VisualStateManager.IsConnected
                && !this.host.VisualStateManager.IsBusy
                && VsShellUtils.IsSolutionExistsAndFullyLoaded()
                && VsShellUtils.IsSolutionExistsAndNotBuildingAndNotDebugging()
                && (this.projectSystemHelper.GetSolutionProjects()?.Any() ?? false);
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
            var bindingProcess = CreateBindingProcess(host, bindingArgs);
            var workflow = new BindingWorkflow(host, bindingProcess);

            IProgressEvents progressEvents = workflow.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, bindingArgs);
        }

        internal static /* for testing purposes */ IBindingProcess CreateBindingProcess(IHost host, BindCommandArgs bindingArgs)
        {
            // Choose the type of binding
            var configProvider = host.GetService<IConfigurationProvider>();
            configProvider.AssertLocalServiceIsNotNull();

            var currentConfiguration = configProvider.GetConfiguration();

            SonarLintMode modeToBind;
            INuGetBindingOperation nugetBindingOp;

            // If we are currently in standalone then the project is being bound for the first time.
            // Otherwise, we are updating an existing binding
            var isFirstBinding = currentConfiguration.Mode == SonarLintMode.Standalone;

            if (currentConfiguration.Mode == SonarLintMode.LegacyConnected)
            {
                host.Logger.WriteLine(Strings.Bind_UpdatingLegacyBinding);
                modeToBind = SonarLintMode.LegacyConnected;
                nugetBindingOp = new NuGetBindingOperation(host, host.Logger);
            }
            else
            {
                host.Logger.WriteLine(
                    isFirstBinding ?
                        Strings.Bind_FirstTimeBinding :
                        Strings.Bind_UpdatingNewStyleBinding);

                modeToBind = SonarLintMode.Connected;
                nugetBindingOp = new NoOpNuGetBindingOperation(host.Logger);
            }

            var solutionBindingOp = new SolutionBindingOperation(
                host,
                bindingArgs.Connection,
                bindingArgs.ProjectKey,
                bindingArgs.ProjectName,
                modeToBind,
                host.Logger,
                new ConfigProjectBinderFactory());

            var unboundProjectFinder = new UnboundProjectFinder(host, new ConfigProjectBinderFactory());

            var dotNetConfigProvider = new DotNetBindingConfigProvider(host.SonarQubeService, nugetBindingOp,
                bindingArgs.Connection.ServerUri.ToString(), bindingArgs.ProjectName,
                host.Logger);

            var cppConfigProvider = new CFamilyBindingConfigProvider(host.SonarQubeService, host.Logger);
            var ruleConfigProvider = new CompositeBindingConfigProvider(dotNetConfigProvider, cppConfigProvider);

            var bindingProcess = new BindingProcessImpl(host, bindingArgs, solutionBindingOp, nugetBindingOp, unboundProjectFinder, ruleConfigProvider, isFirstBinding);

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

                // The conflicts controller is only applicable in legacy connected mode
                // However, it is safe to call it regardless - in new connected mode it will
                // not return any conflicts.
                var conflictsController = this.host.GetService<IRuleSetConflictsController>();
                conflictsController.AssertLocalServiceIsNotNull();

                if (conflictsController.CheckForConflicts())
                {
                    // In some cases we will end up navigating to the solution explorer, this will make sure that
                    // we're back in team explorer to view the conflicts
                    this.host.GetMefService<ITeamExplorerController>()?.ShowSonarQubePage();
                }
                else
                {
                    VsShellUtils.ActivateSolutionExplorer(this.host);
                }
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
