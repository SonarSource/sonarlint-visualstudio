/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Threading;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    // New connected mode:
    // Handles binding a solution for new connected mode i.e. writes the
    // SQ config file to the solution level. No changes are made to 
    // individual projects, or to the solution file itself.

    /// <summary>
    /// Workflow execution for the bind command for the new connected mode
    /// </summary>
    internal class NewBindingWorkflow : IBindingWorkflow
    {
        private readonly IHost host;
        private readonly BindCommandArgs bindingArgs;
        private readonly IConfigurationProvider writer;

        public NewBindingWorkflow(IHost host, BindCommandArgs bindingArgs, IConfigurationProvider configWriter)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (bindingArgs == null)
            {
                throw new ArgumentNullException(nameof(bindingArgs));
            }

            if (configWriter == null)
            {
                throw new ArgumentNullException(nameof(configWriter));
            }

            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            this.host = host;
            this.bindingArgs = bindingArgs;
            this.writer = configWriter;
        }

        #region Workflow state

        internal bool Succeeded { get; set; }

        #endregion

        #region Workflow startup

        public IProgressEvents Run()
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");

            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host,
                this.host.ActiveSection.ProgressHost,
                controller => this.CreateWorkflowSteps(controller));

            this.DebugOnly_MonitorProgress(progress);

            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => this.host.Logger.WriteLine("DEBUGONLY: New binding workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateWorkflowSteps(IProgressController controller)
        {
            return new ProgressStepDefinition[]
            {
                new ProgressStepDefinition(Strings.Bind_SavingBindingConfiguration, StepAttributes.Indeterminate,
                        (token, notifications) => this.SaveBindingInfo(controller, notifications, token)),
            };
        }

        #endregion

        #region Workflow steps

        internal /* for testing */ void SaveBindingInfo(IProgressController controller, IProgressStepExecutionEvents notifications, CancellationToken token)
        {
            notifications.ProgressChanged(Strings.StartedSolutionBindingWorkflow);
            notifications.ProgressChanged(Strings.Bind_SavingBindingConfiguration);

            var connInfo = this.bindingArgs.Connection;
            BasicAuthCredentials credentials = connInfo.UserName == null ? null : new BasicAuthCredentials(connInfo.UserName, connInfo.Password);

            var boundProject = new BoundSonarQubeProject(connInfo.ServerUri, this.bindingArgs.ProjectKey, credentials, connInfo.Organization);
            var config = BindingConfiguration.CreateBoundConfiguration(boundProject, false);

            if (this.writer.WriteConfiguration(config))
            {
                notifications.ProgressChanged(Strings.FinishedSolutionBindingWorkflowSuccessful);
            }
            else
            {
                this.host.Logger.WriteLine(Strings.Bind_FailedToSaveConfiguration);
                AbortWorkflow(controller, token);
            }
        }

        #endregion

        private void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }
    }
}